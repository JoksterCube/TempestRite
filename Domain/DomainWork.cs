using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Amber-Mend Rite (the Vat-Boil and Pitch-Bellows Rites are in StationWork).
// - Amber-Mend: damaged building pieces are repaired, a couple each second, by every game near them (a repair is
//   asked of the piece's owner, as the hammer does), each showing the hammer's repair effect and a puff of amber on the
//   piece. Damaged pieces are gathered by a sweep through the base and mended in a random order, favoring ones far from
//   the last, so repairs jump around instead of creeping along a wall.
internal static class DomainWork
{
    private const string Feature = "Amber-Mend Rite";
    private const float MendInterval = 0.5f;
    private const int MendsPerTick = 1;
    // Pieces looked at per tick, so a large base is gone through over a few seconds without a hitch.
    private const int ScanPerTick = 400;
    private const float MendRange = 80f;

    // Random picks compared each mend; the one farthest from the last mended piece wins, so mending jumps around the base.
    private const int PickTries = 3;

    private static float _nextMend;
    private static int _scanIndex;
    private static Vector3? _lastMended;
    // Damaged pieces in reach found so far by the sweep; checked again when picked.
    private static readonly List<WearNTear> Damaged = [];
    private static readonly HashSet<WearNTear> DamagedSet = [];

    internal static void ClearSession()
    {
        _nextMend = 0f;
        _scanIndex = 0;
        _lastMended = null;
        Damaged.Clear();
        DamagedSet.Clear();
    }

    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || !ZNet.instance || !ZNetScene.instance) return;
        try
        {
            Mend();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // ---- Amber-Mend ----

    private static void Mend()
    {
        if (Time.time < _nextMend) return;
        _nextMend = Time.time + MendInterval;
        Player? player = Player.m_localPlayer;
        if (!player || !WeatherState.IsActive(WeatherKind.AmberMend))
        {
            if (Damaged.Count > 0) ClearSession();
            return;
        }

        Vector3 here = player!.transform.position;
        Sweep(here);
        for (int mended = 0; mended < MendsPerTick && Damaged.Count > 0; mended++)
        {
            WearNTear? piece = Pick(here);
            if (!piece) break;
            Vector3 position = piece!.transform.position;
            if (!piece.Repair()) continue;
            _lastMended = position;
            // The hammer's repair effect (the piece's own placing effect) and a puff of amber.
            if (piece.GetComponent<Piece>() is { } built) built.m_placeEffect.Create(position, piece.transform.rotation);
            if (PluginConfig.ShowDomainEffects.IsOn())
                DomainVisuals.Burst(WeatherKind.AmberMend, position + Vector3.up * 0.5f, DomainVisuals.RiteColor(WeatherKind.AmberMend));
        }
    }

    // Looks through the next stretch of the game's pieces and keeps the damaged ones in reach.
    private static void Sweep(Vector3 here)
    {
        List<WearNTear> pieces = WearNTear.GetAllInstances();
        for (int i = 0; i < ScanPerTick && i < pieces.Count; i++)
        {
            _scanIndex = (_scanIndex + 1) % pieces.Count;
            WearNTear piece = pieces[_scanIndex];
            if (!DamagedSet.Contains(piece) && Mendable(piece, here))
            {
                Damaged.Add(piece);
                DamagedSet.Add(piece);
            }
        }
    }

    // A few random damaged pieces, the one farthest from the last mended; null when none is left. Pieces no longer
    // damaged or in reach are dropped, and the picked one too (the sweep finds it again if the repair didn't take).
    private static WearNTear? Pick(Vector3 here)
    {
        WearNTear? best = null;
        int bestIndex = -1;
        float bestDistance = -1f;
        for (int tries = 0; tries < PickTries && Damaged.Count > 0;)
        {
            int index = UnityEngine.Random.Range(0, Damaged.Count);
            WearNTear piece = Damaged[index];
            if (!Mendable(piece, here))
            {
                Drop(index);
                if (bestIndex == Damaged.Count) bestIndex = index;
                continue;
            }
            tries++;
            float distance = _lastMended is { } last ? Utils.DistanceXZ(piece.transform.position, last) : 0f;
            if (distance <= bestDistance) continue;
            best = piece;
            bestIndex = index;
            bestDistance = distance;
        }
        if (bestIndex >= 0) Drop(bestIndex);
        return best;
    }

    private static bool Mendable(WearNTear piece, Vector3 here)
    {
        if (!piece || !piece.m_nview || !piece.m_nview.IsValid()) return false;
        Vector3 position = piece.transform.position;
        if (Utils.DistanceXZ(position, here) > MendRange || !WeatherState.Reaches(WeatherKind.AmberMend, position)) return false;
        return piece.m_nview.GetZDO().GetFloat(ZDOVars.s_health, piece.m_health) < piece.m_health;
    }

    // Removes by swapping in the last one.
    private static void Drop(int index)
    {
        WearNTear piece = Damaged[index];
        int last = Damaged.Count - 1;
        Damaged[index] = Damaged[last];
        Damaged.RemoveAt(last);
        DamagedSet.Remove(piece);
    }
}
