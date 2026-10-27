using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Common.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain.Networking;

// Shares performed rites through the server, which always has the mod (see RequireMod); single player and hosting
// are the server themselves. Players ask the server to perform a rite; it checks who may, then tells everyone with the
// mod. Players joining later ask it what's going on and how long each has left. Only players whose version matched the
// server's take part.
// Blessings aren't kept by the server: it tells everyone where one was performed, and the players standing there
// receive it (see Blessings).
internal static class WeatherSync
{
    private const float StateRequestRetry = 10f;
    // A joining player waits this long after their character appears before asking, so the server has answered the
    // version check by then.
    private const float AskDelay = 3f;

    private enum Refusal
    {
        AdminsOnly = 1,
        Disabled = 2
    }

    private static bool _stateReceived;
    private static float _nextStateRequest;
    private static float _playerSeenAt = -1f;

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<ZPackage>(Rpcs.Request, RPC_Request);
        rpc.Register<ZPackage>(Rpcs.StateRequest, RPC_StateRequest);
        rpc.Register<ZPackage>(Rpcs.State, RPC_State);
        rpc.Register<ZPackage>(Rpcs.Refused, RPC_Refused);
        rpc.Register<ZPackage>(Rpcs.Blessing, RPC_Blessing);
    }

    internal static void ClearSession()
    {
        _stateReceived = false;
        _nextStateRequest = 0f;
        _playerSeenAt = -1f;
    }

    // Every frame: a player who joined asks what's going on.
    internal static void Update()
    {
        if (!ZNet.instance || ZNet.instance.IsServer() || ZRoutedRpc.instance == null || !Player.m_localPlayer) return;
        if (_playerSeenAt < 0f) _playerSeenAt = Time.time;
        if (Time.time - _playerSeenAt < AskDelay || !RpcHandlers.ServerMatches) return;
        if (_stateReceived || Time.time < _nextStateRequest) return;
        _nextStateRequest = Time.time + StateRequestRetry;
        ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.StateRequest, new ZPackage());
    }

    // Whether the local player may perform rites, as far as this game knows. The server has the last word.
    internal static bool LocalPlayerMayChange() =>
        WeatherPermissions.Allowed(PluginConfig.WhoCanChange.Value, ZNet.instance && ZNet.instance.LocalPlayerIsAdminOrHost());

    // The local player burnt a key item in the obliterator at position.
    internal static void RequestChange(WeatherKind kind, Vector3 position)
    {
        if (!ZNet.instance || ZRoutedRpc.instance == null || !PluginConfig.IsEnabled(kind)) return;

        if (!ZNet.instance.IsServer())
        {
            ZPackage package = new();
            package.Write((int)kind);
            package.Write(position);
            ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.Request, package);
            return;
        }

        // Single player or hosting.
        if (!LocalPlayerMayChange())
        {
            PlayerMessages.Warning(Messages.RefusedAdminsOnly);
            return;
        }
        Perform(kind, position, ZRoutedRpc.instance.m_id, Player.m_localPlayer ? Player.m_localPlayer.GetPlayerName() : string.Empty);
    }

    // ---- Server ----

    private static void RPC_Request(long sender, ZPackage package)
    {
        if (!IsServer() || !IsValidatedPeer(sender, out ZNetPeer? peer)) return;
        WeatherKind kind = (WeatherKind)package.ReadInt();
        Vector3 position = package.ReadVector3();

        if (!PluginConfig.IsEnabled(kind))
        {
            Refuse(sender, Refusal.Disabled);
            return;
        }
        if (!WeatherPermissions.Allowed(PluginConfig.WhoCanChange.Value, IsAdmin(peer!)))
        {
            Plugin.ModLogger.LogInfo($"Refused {kind} performed by {peer!.m_playerName}: admins only.");
            Refuse(sender, Refusal.AdminsOnly);
            return;
        }
        Perform(kind, position, sender, peer!.m_playerName);
    }

    // On the server: performs the rite and tells everyone with the mod.
    private static void Perform(WeatherKind kind, Vector3 position, long callerId, string callerName)
    {
        bool byHost = callerId == ZRoutedRpc.instance.m_id;
        if (WeatherKinds.Personal(kind))
        {
            Plugin.ModLogger.LogInfo($"{callerName} performed the {WeatherKinds.Name(kind)} at ({position.x:0}, {position.z:0}).");
            ZPackage blessing = BlessingPackage(kind, position, callerId, callerName);
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
                if (peer.m_uid != 0 && RpcHandlers.ValidatedPeers.Contains(peer.m_rpc))
                    ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, Rpcs.Blessing, blessing);
            // The host's own player too.
            if (Player.m_localPlayer) Blessings.Performed(kind, position, byHost, callerName);
            return;
        }

        WeatherState.Start(kind, callerName, position);
        CrimsonRaids.Performed(kind);
        Log(callerName, kind);
        if (Player.m_localPlayer) WeatherState.Announce(kind, byLocalPlayer: byHost, callerName);
        WeatherStore.Save();
        SendStateToValidatedPeers(kind, callerId, callerName);
    }

    private static void RPC_StateRequest(long sender, ZPackage _)
    {
        if (!IsServer() || !IsValidatedPeer(sender, out ZNetPeer? _)) return;
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, Rpcs.State, StatePackage(WeatherKind.None, callerId: 0L, string.Empty));
    }

    private static void Refuse(long target, Refusal refusal)
    {
        ZPackage package = new();
        package.Write((int)refusal);
        ZRoutedRpc.instance.InvokeRoutedRPC(target, Rpcs.Refused, package);
    }

    // announced: the rite just performed, which players are told about (None: nothing to tell).
    private static void SendStateToValidatedPeers(WeatherKind announced, long callerId, string callerName)
    {
        ZPackage package = StatePackage(announced, callerId, callerName);
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            if (peer.m_uid != 0 && RpcHandlers.ValidatedPeers.Contains(peer.m_rpc))
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, Rpcs.State, package);
    }

    private static ZPackage StatePackage(WeatherKind announced, long callerId, string callerName)
    {
        ZPackage package = new();
        WriteEntries(package, WeatherState.Entries());
        package.Write((int)announced);
        package.Write(callerId);
        package.Write(callerName);
        return package;
    }

    private static ZPackage BlessingPackage(WeatherKind kind, Vector3 position, long callerId, string callerName)
    {
        ZPackage package = new();
        package.Write((int)kind);
        package.Write(position);
        package.Write(callerId);
        package.Write(callerName);
        return package;
    }

    // ---- Players ----

    private static void RPC_State(long sender, ZPackage package)
    {
        if (!ZNet.instance || ZNet.instance.IsServer() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
        List<WeatherState.Entry> entries = ReadEntries(package);
        WeatherKind announced = (WeatherKind)package.ReadInt();
        long callerId = package.ReadLong();
        string callerName = package.ReadString();

        _stateReceived = true;
        WeatherState.SetAll(entries);
        if (announced != WeatherKind.None) WeatherState.Announce(announced, byLocalPlayer: callerId == ZRoutedRpc.instance.m_id, callerName);
    }

    private static void RPC_Blessing(long sender, ZPackage package)
    {
        if (!ZNet.instance || ZNet.instance.IsServer() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
        WeatherKind kind = (WeatherKind)package.ReadInt();
        Vector3 position = package.ReadVector3();
        long callerId = package.ReadLong();
        string callerName = package.ReadString();
        if (WeatherKinds.Personal(kind)) Blessings.Performed(kind, position, callerId == ZRoutedRpc.instance.m_id, callerName);
    }

    private static void RPC_Refused(long sender, ZPackage package)
    {
        if (!ZNet.instance || ZNet.instance.IsServer() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
        PlayerMessages.Warning((Refusal)package.ReadInt() == Refusal.Disabled ? Messages.RefusedDisabled : Messages.RefusedAdminsOnly);
    }

    // ---- Shared helpers ----

    private static void Log(string who, WeatherKind kind) => Plugin.ModLogger.LogInfo(WeatherKinds.Lasts(kind)
        ? $"{who} performed the {WeatherKinds.Name(kind)} for {TimeText.Duration(WeatherState.Remaining(kind))}{Where(kind)}."
        : $"{who} performed the {WeatherKinds.Name(kind)}.");

    // For located rites: " at (x, z), within r m".
    private static string Where(WeatherKind kind)
    {
        if (!WeatherKinds.Located(kind)) return string.Empty;
        Vector3 at = WeatherState.Position(kind);
        return $" at ({at.x:0}, {at.z:0}), within {PluginConfig.Radius(kind):0} m";
    }

    private static void WriteEntries(ZPackage package, List<WeatherState.Entry> entries)
    {
        package.Write(entries.Count);
        foreach (WeatherState.Entry entry in entries)
        {
            package.Write((int)entry.Kind);
            package.Write(entry.Remaining);
            package.Write(entry.Duration);
            package.Write(entry.CalledBy);
            package.Write(entry.Position);
        }
    }

    private static List<WeatherState.Entry> ReadEntries(ZPackage package)
    {
        int count = package.ReadInt();
        List<WeatherState.Entry> entries = new(count);
        for (int i = 0; i < count; i++)
        {
            WeatherKind kind = (WeatherKind)package.ReadInt();
            float remaining = package.ReadSingle();
            float duration = package.ReadSingle();
            string calledBy = package.ReadString();
            Vector3 position = package.ReadVector3();
            entries.Add(new WeatherState.Entry(kind, remaining, duration, calledBy, position));
        }
        return entries;
    }

    private static bool IsServer() => ZNet.instance && ZNet.instance.IsServer();

    private static bool IsValidatedPeer(long sender, out ZNetPeer? peer)
    {
        peer = ZNet.instance.GetPeer(sender);
        return peer != null && RpcHandlers.ValidatedPeers.Contains(peer.m_rpc);
    }

    private static bool IsAdmin(ZNetPeer peer) =>
        ZNet.instance.ListContainsId(ZNet.instance.m_adminList, peer.m_rpc.GetPeerHostName());
}
