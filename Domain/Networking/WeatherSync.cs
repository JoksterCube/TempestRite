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
// One burn can perform several rites: they are asked for, performed and told together.
// Blessings aren't kept by the server: it tells everyone where one was performed, and the players standing there
// receive it (see Blessings). Kinship rites go out the same way, and everyone receives them (see KinshipRites).
internal static class WeatherSync
{
    private const float StateRequestRetry = 10f;
    // A joining player waits this long after their character appears before asking, so the server has answered the
    // version check by then.
    private const float AskDelay = 3f;

    private enum Refusal
    {
        AdminsOnly = 1,
        Disabled = 2,
        // A Crimson or Ragnarök Rite going on forbids it; the reason follows.
        Incursion = 3
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

    // The local player burnt key items in the obliterator at position: the rites they perform, in order.
    internal static void RequestChange(List<WeatherKind> kinds, Vector3 position)
    {
        if (!ZNet.instance || ZRoutedRpc.instance == null) return;
        kinds = kinds.FindAll(PluginConfig.IsEnabled);
        if (kinds.Count == 0) return;

        if (!ZNet.instance.IsServer())
        {
            ZPackage package = new();
            WriteKinds(package, kinds);
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
        Perform(kinds, position, ZRoutedRpc.instance.m_id, Player.m_localPlayer ? Player.m_localPlayer.GetPlayerName() : string.Empty);
    }

    // ---- Server ----

    private static void RPC_Request(long sender, ZPackage package)
    {
        if (!IsServer() || !IsValidatedPeer(sender, out ZNetPeer? peer)) return;
        List<WeatherKind> kinds = ReadKinds(package);
        Vector3 position = package.ReadVector3();

        List<WeatherKind> enabled = kinds.FindAll(PluginConfig.IsEnabled);
        if (enabled.Count == 0)
        {
            Refuse(sender, Refusal.Disabled);
            return;
        }
        if (!WeatherPermissions.Allowed(PluginConfig.WhoCanChange.Value, IsAdmin(peer!)))
        {
            Plugin.ModLogger.LogInfo($"Refused {string.Join(", ", kinds)} performed by {peer!.m_playerName}: admins only.");
            Refuse(sender, Refusal.AdminsOnly);
            return;
        }
        Perform(enabled, position, sender, peer!.m_playerName);
    }

    // On the server: performs the rites of one burn, in order, and tells everyone with the mod. A rite clashing with
    // one before it is left out, as the obliterator picks them (see KeyItemPicker), and so is one a Crimson or Ragnarök
    // Rite going on forbids (the player pulling the lever was told already, and nothing burnt; this is for the rare
    // case it came about in between).
    private static void Perform(List<WeatherKind> kinds, Vector3 position, long callerId, string callerName)
    {
        bool byHost = callerId == ZRoutedRpc.instance.m_id;
        List<WeatherKind> rites = [];
        List<WeatherKind> blessings = [];
        List<WeatherKind> done = [];
        string? refused = null;
        foreach (WeatherKind kind in kinds)
        {
            if (!WeatherKinds.IsKnown(kind) || done.Exists(d => WeatherKinds.Clashes(d, kind))) continue;
            if (IncursionArena.RiteRefused(kind, position) is { } why)
            {
                Plugin.ModLogger.LogInfo($"Refused the {WeatherKinds.Name(kind)} performed by {callerName}: {why}.");
                refused ??= why;
                continue;
            }
            done.Add(kind);
            if (WeatherKinds.Personal(kind) || WeatherKinds.ForEveryone(kind))
            {
                Plugin.ModLogger.LogInfo($"{callerName} performed the {WeatherKinds.Name(kind)} at ({position.x:0}, {position.z:0}).");
                blessings.Add(kind);
                continue;
            }
            WeatherState.Start(kind, callerName, position);
            CrimsonRaids.Performed(kind);
            RagnarokRush.Performed(kind);
            Log(callerName, kind);
            rites.Add(kind);
        }
        if (refused != null)
        {
            if (byHost) PlayerMessages.Warning(refused);
            else Refuse(callerId, Refusal.Incursion, refused);
        }
        if (done.Count == 0) return;

        // The host's own player too.
        if (Player.m_localPlayer) Announce(rites, blessings, position, byLocalPlayer: byHost, callerName);
        if (rites.Count > 0) WeatherStore.Save();
        ZPackage package = StatePackage(rites, blessings, position, callerId, callerName);
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            if (peer.m_uid != 0 && RpcHandlers.ValidatedPeers.Contains(peer.m_rpc))
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, Rpcs.State, package);
    }

    // On the server, after it changed what is going on by itself (the Crimson Rite breaking off): saves it and tells
    // everyone with the mod.
    internal static void Broadcast()
    {
        if (!IsServer() || ZRoutedRpc.instance == null) return;
        WeatherStore.Save();
        ZPackage package = StatePackage([], [], Vector3.zero, callerId: 0L, string.Empty);
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            if (peer.m_uid != 0 && RpcHandlers.ValidatedPeers.Contains(peer.m_rpc))
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, Rpcs.State, package);
    }

    private static void RPC_StateRequest(long sender, ZPackage _)
    {
        if (!IsServer() || !IsValidatedPeer(sender, out ZNetPeer? _)) return;
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, Rpcs.State, StatePackage([], [], Vector3.zero, callerId: 0L, string.Empty));
    }

    private static void Refuse(long target, Refusal refusal, string reason = "")
    {
        ZPackage package = new();
        package.Write((int)refusal);
        package.Write(reason);
        ZRoutedRpc.instance.InvokeRoutedRPC(target, Rpcs.Refused, package);
    }

    // The rites going on, and what was just performed, which players are told about (nothing when they only ask what's
    // going on): rites everyone shares, and blessings for the players standing at position (kinship rites for all).
    private static ZPackage StatePackage(List<WeatherKind> rites, List<WeatherKind> blessings, Vector3 position, long callerId, string callerName)
    {
        ZPackage package = new();
        WriteEntries(package, WeatherState.Entries());
        WriteKinds(package, rites);
        WriteKinds(package, blessings);
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
        List<WeatherKind> rites = ReadKinds(package);
        List<WeatherKind> blessings = ReadKinds(package);
        Vector3 position = package.ReadVector3();
        long callerId = package.ReadLong();
        string callerName = package.ReadString();

        _stateReceived = true;
        WeatherState.SetAll(entries);
        Announce(rites, blessings, position, byLocalPlayer: callerId == ZRoutedRpc.instance.m_id, callerName);
    }

    // Tells the local player what one burn performed, all in one message.
    private static void Announce(List<WeatherKind> rites, List<WeatherKind> blessings, Vector3 position, bool byLocalPlayer, string callerName)
    {
        using PlayerMessages.Gathering _ = PlayerMessages.Together();
        foreach (WeatherKind kind in rites) WeatherState.Announce(kind, byLocalPlayer, callerName);
        Blessings.Performed(blessings.FindAll(WeatherKinds.Personal), position, byLocalPlayer, callerName);
        KinshipRites.Performed(blessings.FindAll(WeatherKinds.ForEveryone), byLocalPlayer, callerName);
    }

    private static void RPC_Refused(long sender, ZPackage package)
    {
        if (!ZNet.instance || ZNet.instance.IsServer() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
        Refusal refusal = (Refusal)package.ReadInt();
        string reason = package.ReadString();
        PlayerMessages.Warning(refusal switch
        {
            Refusal.Disabled => Messages.RefusedDisabled,
            Refusal.Incursion => reason,
            _ => Messages.RefusedAdminsOnly
        });
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

    private static void WriteKinds(ZPackage package, List<WeatherKind> kinds)
    {
        package.Write(kinds.Count);
        foreach (WeatherKind kind in kinds) package.Write((int)kind);
    }

    private static List<WeatherKind> ReadKinds(ZPackage package)
    {
        int count = package.ReadInt();
        List<WeatherKind> kinds = new(count);
        for (int i = 0; i < count; i++) kinds.Add((WeatherKind)package.ReadInt());
        return kinds;
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
