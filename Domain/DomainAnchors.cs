using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// A Domain rite ends when the obliterator it was performed at is destroyed. The server checks now and then, ends it for
// everyone and tells the players within its area.
internal static class DomainAnchors
{
    private const string Feature = "Domain rites' obliterators";
    private const string ObliteratorPrefab = "incinerator";
    private const float CheckInterval = 2f;

    // The obliterator found for each Domain slot, and where the rite there was performed.
    private sealed class Anchor
    {
        internal WeatherKind Kind;
        internal Vector3 At;
        internal ZDOID Obliterator;
    }

    private static readonly Dictionary<EffectSlot, Anchor> Anchors = [];
    private static float _nextCheck;

    internal static void Register(ZRoutedRpc rpc) => rpc.Register<int, Vector3>(Rpcs.DomainBroken, RPC_Broken);

    internal static void ClearSession()
    {
        Anchors.Clear();
        _nextCheck = 0f;
    }

    // Every frame on the server.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || !ZNet.instance || !ZNet.instance.IsServer() || ZDOMan.instance == null) return;
        if (Time.time < _nextCheck) return;
        _nextCheck = Time.time + CheckInterval;
        try { Check(); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Check()
    {
        bool ended = false;
        foreach (EffectSlot slot in WeatherKinds.Slots)
        {
            if (!WeatherKinds.IsDomainSlot(slot)) continue;
            WeatherKind kind = WeatherState.Active(slot);
            if (kind == WeatherKind.None)
            {
                Anchors.Remove(slot);
                continue;
            }
            Vector3 at = WeatherState.Position(kind);
            if (!Anchors.TryGetValue(slot, out Anchor anchor) || anchor.Kind != kind || Utils.DistanceXZ(anchor.At, at) > CrimsonRaids.SamePlace)
            {
                anchor = new Anchor { Kind = kind, At = at, Obliterator = Find(at) };
                Anchors[slot] = anchor;
                if (anchor.Obliterator.IsNone())
                    Plugin.ModLogger.LogWarning($"{WeatherKinds.Name(kind)}: found no obliterator at ({at.x:0}, {at.z:0}); it won't end if one is destroyed.");
            }
            if (anchor.Obliterator.IsNone() || ZDOMan.instance.GetZDO(anchor.Obliterator) != null) continue;

            Anchors.Remove(slot);
            WeatherState.End(kind);
            ended = true;
            Plugin.ModLogger.LogInfo($"{WeatherKinds.Name(kind)} ended: its obliterator at ({at.x:0}, {at.z:0}) is destroyed.");
            ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.DomainBroken, (int)kind, at);
        }
        if (ended) WeatherSync.Broadcast();
    }

    private static ZDOID Find(Vector3 at)
    {
        List<ZDO> found = [];
        ZDOMan.instance.FindObjects(ZoneSystem.GetZone(at), found, []);
        int prefab = ObliteratorPrefab.GetStableHashCode();
        foreach (ZDO zdo in found)
            if (zdo.GetPrefab() == prefab && Utils.DistanceXZ(zdo.GetPosition(), at) < CrimsonRaids.SamePlace) return zdo.m_uid;
        return ZDOID.None;
    }

    // In every game: the rite is over (the state that follows says so too), told to the players within its area.
    private static void RPC_Broken(long sender, int kind, Vector3 at)
    {
        if (!IncursionArena.FromServer(sender)) return;
        WeatherKind rite = (WeatherKind)kind;
        if (!ZNet.instance.IsServer()) WeatherState.End(rite);
        Player? player = Player.m_localPlayer;
        if (!player || Utils.DistanceXZ(player!.transform.position, at) > PluginConfig.Radius(rite)) return;
        PlayerMessages.Warning(string.Format(Messages.DomainBroken, WeatherKinds.Name(rite)));
    }
}
