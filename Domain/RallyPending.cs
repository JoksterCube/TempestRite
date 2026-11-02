using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Common.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// Only one Rally-Horn call waits for answers at a time, server-wide. The server keeps who still owes an answer: every
// player with the mod when the call was sounded. Each game tells it once its player is done with the call: passed by
// (at the obliterator already, waiting, dead...), answered, refused, or let it fade. Once nobody owes an answer (or
// they left, or the call is long over), the call is done and another can be sounded. Until then the server tells
// every game a call is pending, and the obliterator refuses the anniversary tankard, which stays in it (see
// ObliteratorPull); the server refuses it too, for a burn that raced the news.
internal static class RallyPending
{
    private const string Feature = "Rally-Horn pending call";
    // After the call itself ends, this long more before the server stops waiting for answers that never came.
    private const float GraceSeconds = 15f;
    private const float CheckInterval = 1f;

    // In every game: whether a call waits for answers somewhere on the server.
    internal static bool Active { get; private set; }

    // On the server: who still owes an answer, and when it stops waiting for them anyway.
    private static readonly HashSet<long> Owing = [];
    private static float _until;
    private static float _nextCheck;

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<bool>(Rpcs.RallyPending, RPC_Pending);
        rpc.Register<bool>(Rpcs.RallyDone, RPC_Done);
    }

    internal static void ClearSession()
    {
        Active = false;
        Owing.Clear();
        _until = 0f;
        _nextCheck = 0f;
    }

    // ---- Every game ----

    // The local player is done with the call (passed by, answered, refused, or let it fade).
    internal static void Done()
    {
        if (!ZNet.instance || ZRoutedRpc.instance == null) return;
        if (ZNet.instance.IsServer()) Answered(ZRoutedRpc.instance.m_id);
        else ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), Rpcs.RallyDone, true);
    }

    private static void RPC_Pending(long sender, bool active)
    {
        if (!ZNet.instance || ZNet.instance.IsServer() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
        Active = active;
    }

    // ---- Server ----

    // A call was just sounded: everyone with the mod (the host's own player too) owes an answer.
    internal static void Sounded()
    {
        if (!ZNet.instance || !ZNet.instance.IsServer() || ZRoutedRpc.instance == null) return;
        Owing.Clear();
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            if (peer.m_uid != 0 && RpcHandlers.ValidatedPeers.Contains(peer.m_rpc)) Owing.Add(peer.m_uid);
        if (!ZNet.instance.IsDedicated() && Player.m_localPlayer) Owing.Add(ZRoutedRpc.instance.m_id);
        _until = Time.time + PluginConfig.RallyCallSeconds.Value + GraceSeconds;
        Set(true);
    }

    private static void RPC_Done(long sender, bool _)
    {
        if (!ZNet.instance || !ZNet.instance.IsServer()) return;
        Answered(sender);
    }

    private static void Answered(long who)
    {
        if (!Active || !Owing.Remove(who) || Owing.Count > 0) return;
        Plugin.ModLogger.LogInfo("The Rally-Horn Rite's call is done: everyone answered, refused or let it fade.");
        Set(false);
    }

    // On the server, every frame: those who left owe nothing; a call long over is done anyway.
    internal static void Update()
    {
        if (!Active || !ZNet.instance || !ZNet.instance.IsServer() || Time.time < _nextCheck) return;
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            _nextCheck = Time.time + CheckInterval;
            long self = ZRoutedRpc.instance?.m_id ?? 0L;
            Owing.RemoveWhere(id => id == self ? !Player.m_localPlayer : ZNet.instance.GetPeer(id) == null);
            if (Owing.Count > 0 && Time.time < _until) return;
            if (Owing.Count > 0) Plugin.ModLogger.LogInfo($"The Rally-Horn Rite's call is done: {Owing.Count} never answered.");
            Owing.Clear();
            Set(false);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // A player who just joined (asking what's going on) is told whether a call is pending.
    internal static void Tell(long peer)
    {
        if (ZRoutedRpc.instance != null) ZRoutedRpc.instance.InvokeRoutedRPC(peer, Rpcs.RallyPending, Active);
    }

    private static void Set(bool active)
    {
        Active = active;
        if (ZRoutedRpc.instance == null) return;
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            if (peer.m_uid != 0 && RpcHandlers.ValidatedPeers.Contains(peer.m_rpc))
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, Rpcs.RallyPending, active);
    }

    // Why a Rally-Horn Rite can't be sounded now, or null.
    internal static string? Refused(WeatherKind kind) => kind == WeatherKind.RallyHorn && Active ? Messages.RallyPendingRefused : null;
}
