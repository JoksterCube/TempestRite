using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Amber-Mend Rite (the Vat-Boil and Pitch-Bellows Rites are in StationWork).
// - Amber-Mend: damaged building pieces are repaired, a couple each second, by every game near them (a repair is
//   asked of the piece's owner, as the hammer does), each showing the hammer's repair effect on the piece.
internal static class DomainWork
{
    private const string Feature = "Amber-Mend Rite";
    private const float MendInterval = 0.5f;
    private const int MendsPerTick = 1;
    // Pieces looked at per tick, so a large base is gone through over a few seconds without a hitch.
    private const int ScanPerTick = 400;
    private const float MendRange = 80f;

    private static float _nextMend;
    private static int _scanIndex;

    internal static void ClearSession()
    {
        _nextMend = 0f;
        _scanIndex = 0;
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
        if (!player || !WeatherState.IsActive(WeatherKind.AmberMend)) return;

        List<WearNTear> pieces = WearNTear.GetAllInstances();
        if (pieces.Count == 0) return;
        Vector3 here = player!.transform.position;
        int mended = 0;
        for (int i = 0; i < ScanPerTick && i < pieces.Count && mended < MendsPerTick; i++)
        {
            _scanIndex = (_scanIndex + 1) % pieces.Count;
            WearNTear piece = pieces[_scanIndex];
            if (!piece || !piece.m_nview || !piece.m_nview.IsValid()) continue;
            Vector3 position = piece.transform.position;
            if (Utils.DistanceXZ(position, here) > MendRange || !WeatherState.Reaches(WeatherKind.AmberMend, position)) continue;
            if (piece.m_nview.GetZDO().GetFloat(ZDOVars.s_health, piece.m_health) >= piece.m_health) continue;
            if (!piece.Repair()) continue;
            mended++;
            // The hammer's repair effect: the piece's own placing effect.
            if (piece.GetComponent<Piece>() is { } built) built.m_placeEffect.Create(position, piece.transform.rotation);
        }
    }
}
