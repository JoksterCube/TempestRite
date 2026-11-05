using System.Collections.Generic;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// What only the server sees, told to every game every few seconds:
// - who was inside the Crimson, Ragnarök or Hoard-Bane Rite's Blood Grounds as it began (the players inside in its first
//   seconds): one of them disconnected during the fight may log back in inside it; anyone else logging in there is put
//   outside (see IncursionArena.Hold);
// - whether a boss fight goes on anywhere in the world (a boss near a player): no such rite can be performed then (see
//   IncursionSite), checked before anything burns.
internal static class IncursionWatch
{
    private const float TellInterval = 2f;
    // Inside in these first seconds: in the fight.
    internal const float JoinWindow = 10f;
    // A boss this near a player is being fought.
    private const float BossFightReach = 100f;

    private static float _nextTell;
    // The server's.
    private static WeatherKind _kind;
    private static Vector3 _at;
    private static readonly HashSet<long> Inside = [];
    // As the server last said.
    private static bool _bossFight;
    private static bool _heard;
    private static WeatherKind _heardKind;
    private static Vector3 _heardAt;
    private static readonly HashSet<long> Heard = [];

    internal static void Register(ZRoutedRpc rpc) => rpc.Register<ZPackage>(Rpcs.IncursionWatch, RPC_Watch);

    internal static void ClearSession()
    {
        _nextTell = 0f;
        _kind = WeatherKind.None;
        _at = Vector3.zero;
        Inside.Clear();
        _bossFight = false;
        _heard = false;
        _heardKind = WeatherKind.None;
        Heard.Clear();
    }

    // Whether a boss fight goes on in the world, as the server last said.
    internal static bool BossFight => _bossFight;

    // How long the rite has gone on.
    private static float Elapsed(WeatherKind kind) => WeatherState.Duration(kind) - WeatherState.Remaining(kind);

    // Joining it now is joining late: past its first seconds.
    internal static bool Late(WeatherKind kind) => Elapsed(kind) > JoinWindow;

    // Whether the local player was inside as this rite began; null until the server has said.
    internal static bool? Participant(WeatherKind kind, Vector3 center)
    {
        if (!_heard || _heardKind != kind || Utils.DistanceXZ(_heardAt, center) > IncursionArena.SamePlace) return null;
        Player? player = Player.m_localPlayer;
        return player && Heard.Contains(player!.GetPlayerID());
    }

    // Every frame on the server.
    internal static void Update()
    {
        if (!ZNet.instance || !ZNet.instance.IsServer() || ZDOMan.instance == null || ZRoutedRpc.instance == null) return;
        if (Time.time < _nextTell) return;
        _nextTell = Time.time + TellInterval;
        WeatherKind kind = IncursionArena.Kind;
        Vector3 center = kind == WeatherKind.None ? Vector3.zero : WeatherState.Position(kind);
        if (kind != _kind || Utils.DistanceXZ(center, _at) > IncursionArena.SamePlace)
        {
            Inside.Clear();
            _kind = kind;
            _at = center;
        }
        List<ZDO> players = ZNet.instance.GetAllCharacterZDOS();
        if (kind != WeatherKind.None && !Late(kind))
        {
            float radius = PluginConfig.Radius(kind);
            foreach (ZDO zdo in players)
            {
                if (zdo.GetBool(ZDOVars.s_dead) || Character.InInterior(zdo.GetPosition()) ||
                    Utils.DistanceXZ(zdo.GetPosition(), center) > radius) continue;
                long id = zdo.GetLong(ZDOVars.s_playerID);
                if (id != 0L) Inside.Add(id);
            }
        }
        ZPackage package = new();
        package.Write(BossFightNow(players));
        package.Write((int)kind);
        package.Write(center);
        package.Write(Inside.Count);
        foreach (long id in Inside) package.Write(id);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.IncursionWatch, package);
    }

    private static void RPC_Watch(long sender, ZPackage package)
    {
        if (ZRoutedRpc.instance == null || (sender != ZRoutedRpc.instance.GetServerPeerID() && sender != ZRoutedRpc.instance.m_id)) return;
        _bossFight = package.ReadBool();
        _heardKind = (WeatherKind)package.ReadInt();
        _heardAt = package.ReadVector3();
        Heard.Clear();
        int count = package.ReadInt();
        for (int i = 0; i < count; i++) Heard.Add(package.ReadLong());
        _heard = true;
    }

    // A boss (not one a rite brought) near any player, in the world or down in a dungeon.
    private static bool BossFightNow(List<ZDO> players)
    {
        if (!ZNetScene.instance) return false;
        List<ZDO> found = [];
        HashSet<ZDOID> seen = [];
        foreach (ZDO player in players)
        {
            found.Clear();
            IncursionArena.FindAround(player.GetPosition(), BossFightReach, found);
            foreach (ZDO zdo in found)
            {
                if (!seen.Add(zdo.m_uid) || !IncursionSite.IsBoss(zdo.GetPrefab())) continue;
                if (zdo.GetBool(IncursionArena.BossMark) || zdo.GetInt(HoardBaneArena.Role) != 0) continue;
                if (Vector3.Distance(zdo.GetPosition(), player.GetPosition()) <= BossFightReach) return true;
            }
        }
        return false;
    }
}
