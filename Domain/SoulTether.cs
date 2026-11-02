using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Common.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The Soul-Tether Rite: a blessing that happens at once. Every tombstone of each blessed player, wherever it lies, is
// pulled to the obliterator.
// - Each blessed player's game has the obliterator's surroundings loaded, so it picks the free spots: on rings around
//   the obliterator, at the height of the ground it stands on, where nothing (pieces, items, rocks, creatures) is in
//   the way and there is ground right under. It sends them to the server.
// - Only the server has every tombstone in the world (most lie in places no one has loaded). It finds the player's
//   tombstones and moves them onto those spots, with their spawn point too (a tombstone moved more than 4 m from it
//   jumps back). Whoever's game had one loaded is told at once, and the game of a player near the obliterator takes it
//   over from there.
// The server only moves tombstones for a player who asks shortly after the rite was performed near where they ask, so a
// player can't pull tombstones whenever they like.
internal static class SoulTether
{
    private const string Feature = "Soul-Tether Rite";
    private static readonly int TombstoneHash = "Player_tombstone".GetStableHashCode();

    // The spots: rings this far apart, from the first ring out to the last, this far apart along each ring.
    private const float FirstRing = 3f;
    private const float RingStep = 1.6f;
    private const int Rings = 7;
    private const float Spacing = 1.6f;
    private const int MaxSpots = 40;
    // The ground under a spot may be this much lower or higher than the obliterator's (a floor, level ground).
    private const float GroundBelow = 0.6f;
    private const float GroundAbove = 0.35f;
    // On a hill: the terrain under a spot may be this much lower or higher than the obliterator, and may rise this much
    // across the spot (steeper and the tombstone would roll away).
    private const float HillReach = 8f;
    private const float HillRise = 1.5f;
    // A tombstone is set down this far above the ground and drops onto it.
    private const float DropLift = 0.3f;
    // The room a tombstone needs, from just above the ground.
    private static readonly Vector3 Room = new(0.55f, 0.7f, 0.55f);
    private const float RoomLift = 0.08f;
    // Tombstones beyond the free spots are stacked over the roomiest one, this much higher each: they fall and settle.
    private const float StackHeight = 1.4f;

    // How long after the rite the server waits for its players, and how far from it they may ask.
    private const float AskWindow = 120f;
    private const float AskReach = 6f;
    private const float SpotReach = FirstRing + RingStep * Rings + 2f;

    private sealed class Performance(Vector3 position, float time)
    {
        internal Vector3 Position { get; } = position;
        internal float Time { get; } = time;
        internal HashSet<long> Pulled { get; } = [];
    }

    private static readonly List<Performance> Recent = [];
    private static readonly List<ZDO> Graves = [];
    private static readonly List<Vector3> Spots = [];
    // Where tombstones lie by the obliterator, and those placed there just now.
    private static readonly List<Vector3> Taken = [];
    // Tombstones by the obliterator stand at least this far apart.
    private const float Gap = 1.5f;
    // Where each tombstone was last set down. One that is not by the obliterator at the next rite didn't arrive there
    // (it fell through, or was held back): that spot is marked bad and not used again this session.
    private static readonly Dictionary<ZDOID, Vector3> SetDown = [];
    private static readonly List<Vector3> BadSpots = [];
    private const int MaxBadSpots = 64;
    // Where the tombstones just pulled lay before.
    private static readonly List<Vector3> Left = [];
    // A death pin this close to where a pulled tombstone lay is its own (a tombstone stays within 4 m of where it fell).
    private const float PinReach = 6f;
    private static readonly Collider[] Hits = new Collider[8];
    private static int _roomMask;
    private static int _groundMask;

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<ZPackage>(Rpcs.SoulTether, RPC_Pull);
        rpc.Register<ZPackage>(Rpcs.SoulTetherDone, RPC_Done);
    }

    internal static void ClearSession()
    {
        Recent.Clear();
        SetDown.Clear();
        BadSpots.Clear();
    }

    // ---- Server ----

    // The server performed the rite at position: its blessed players may now ask for their tombstones.
    internal static void PerformedOnServer(Vector3 position)
    {
        Recent.RemoveAll(p => Time.time - p.Time > AskWindow);
        Recent.Add(new Performance(position, Time.time));
    }

    private static void RPC_Pull(long sender, ZPackage package)
    {
        if (!ZNet.instance || !ZNet.instance.IsServer() || PatchGuard.IsDisabled(Feature)) return;
        try
        {
            ZNetPeer? peer = ZNet.instance.GetPeer(sender);
            if (peer == null || !RpcHandlers.ValidatedPeers.Contains(peer.m_rpc)) return;
            ZDO? character = ZDOMan.instance.GetZDO(peer.m_characterID);
            long playerId = character?.GetLong(ZDOVars.s_playerID) ?? 0L;
            if (playerId == 0L) return;
            Vector3 center = package.ReadVector3();
            List<Vector3> spots = ReadSpots(package);
            if (Utils.DistanceXZ(peer.GetRefPos(), center) > PluginConfig.BlessingRange.Value + SpotReach) return;
            bool admin = ZNet.instance.ListContainsId(ZNet.instance.m_adminList, peer.m_rpc.GetPeerHostName());
            if (!Pull(playerId, peer.m_playerName, center, spots, admin, out int moved, out int there)) return;
            ZPackage done = new();
            done.Write(moved);
            done.Write(there);
            done.Write(Left.Count);
            foreach (Vector3 left in Left) done.Write(left);
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, Rpcs.SoulTetherDone, done);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Moves the player's tombstones onto the spots, noting in Left where each lay (its spawn point: where the player
    // died, so where its death pin is). False when the request doesn't follow a performance near center, unless trusted
    // (an admin, who may also call it from the console): then around center itself.
    private static bool Pull(long playerId, string playerName, Vector3 center, List<Vector3> spots, bool trusted, out int moved, out int there)
    {
        moved = there = 0;
        Left.Clear();
        Recent.RemoveAll(p => Time.time - p.Time > AskWindow);
        Performance? performance = Recent.Find(p => Vector3.Distance(p.Position, center) <= AskReach && !p.Pulled.Contains(playerId));
        if (performance == null && !trusted) return false;
        if (performance != null)
        {
            performance.Pulled.Add(playerId);
            center = performance.Position;
        }
        Graves.Clear();
        Taken.Clear();
        foreach (ZDO zdo in ZDOMan.instance.m_objectsByID.Values)
        {
            if (zdo.GetPrefab() != TombstoneHash) continue;
            // Every tombstone already by the obliterator, anyone's (also those just pulled for another player), keeps
            // its place: the new ones go around it.
            bool mine = zdo.GetLong(ZDOVars.s_owner) == playerId;
            // The player's own tombstones are left only within the configured distance; closer ones are pulled too.
            if (Near(zdo.GetPosition(), center) && (!mine || Utils.DistanceXZ(zdo.GetPosition(), center) <= PluginConfig.TetherLeaveWithin.Value))
            {
                Taken.Add(zdo.GetPosition());
                if (mine) there++;
            }
            else if (mine) Graves.Add(zdo);
        }

        // A tombstone set down here before that isn't here now: its spot failed, so the next tries use others.
        foreach (ZDO grave in Graves)
        {
            if (!SetDown.TryGetValue(grave.m_uid, out Vector3 failed)) continue;
            SetDown.Remove(grave.m_uid);
            if (BadSpots.Count >= MaxBadSpots) BadSpots.RemoveAt(0);
            BadSpots.Add(failed);
            Plugin.ModLogger.LogInfo($"{Feature}: a tombstone of {playerName} didn't stay at ({failed.x:0}, {failed.y:0}, {failed.z:0}); that spot is skipped from now on.");
        }
        spots.RemoveAll(s => Utils.DistanceXZ(s, center) > SpotReach || Mathf.Abs(s.y - center.y) > HillReach + HillRise + 1f || Bad(s));
        if (spots.Count == 0) spots.Add(Fallback(center));

        foreach (ZDO grave in Graves)
        {
            Vector3 spot = PickSpot(spots, center);
            Taken.Add(spot);
            Left.Add(grave.GetVec3(ZDOVars.s_spawnPoint, grave.GetPosition()));
            Move(grave, spot, center);
            SetDown[grave.m_uid] = spot;
            moved++;
        }
        Plugin.ModLogger.LogInfo($"{Feature}: pulled {moved} tombstones of {playerName} to the obliterator at ({center.x:0}, {center.z:0}).");
        return true;
    }

    private static bool Bad(Vector3 spot)
    {
        foreach (Vector3 bad in BadSpots)
            if (Utils.DistanceXZ(spot, bad) < 1f && Mathf.Abs(spot.y - bad.y) < 3f) return true;
        return false;
    }

    private static bool Near(Vector3 position, Vector3 center) =>
        Utils.DistanceXZ(position, center) <= SpotReach && Mathf.Abs(position.y - center.y) < HillReach + HillRise + 2f;

    // No free spot came: beside the obliterator, above the terrain there when it is loaded (never inside a hill), to
    // drop onto it.
    private static Vector3 Fallback(Vector3 center)
    {
        // The first side of the obliterator whose spot hasn't failed before.
        Vector3 spot = center + new Vector3(FirstRing, 0f, 0f);
        for (int k = 0; k < 8; k++)
        {
            float angle = k * Mathf.PI / 4f;
            Vector3 side = center + new Vector3(Mathf.Cos(angle) * FirstRing, 0f, Mathf.Sin(angle) * FirstRing);
            if (Bad(side)) continue;
            spot = side;
            break;
        }
        float ground = center.y;
        if (ZoneSystem.instance && ZoneSystem.instance.GetGroundHeight(spot, out float terrain) && Mathf.Abs(terrain - center.y) < 50f)
            ground = Mathf.Max(ground, terrain);
        spot.y = ground + 0.5f;
        return spot;
    }

    // The free spot on the innermost ring that still has room, as far as it can be from the tombstones already there,
    // so tombstones spread evenly around the obliterator and stay close to it. When every spot is taken, the one with
    // the most room gets the tombstone on top, to fall and settle.
    private static Vector3 PickSpot(List<Vector3> spots, Vector3 center)
    {
        int bestRing = int.MaxValue;
        float bestRoom = -1f;
        Vector3 best = spots[0];
        Vector3 roomiest = spots[0];
        float roomiestRoom = -1f;
        foreach (Vector3 spot in spots)
        {
            float room = float.MaxValue;
            foreach (Vector3 taken in Taken) room = Mathf.Min(room, Utils.DistanceXZ(spot, taken));
            if (room > roomiestRoom)
            {
                roomiestRoom = room;
                roomiest = spot;
            }
            if (room < Gap) continue;
            int ring = Mathf.RoundToInt((Utils.DistanceXZ(spot, center) - FirstRing) / RingStep);
            if (ring > bestRing || (ring == bestRing && room <= bestRoom)) continue;
            bestRing = ring;
            bestRoom = room;
            best = spot;
        }
        if (bestRoom >= 0f) return best;
        int under = 0;
        foreach (Vector3 taken in Taken)
            if (Utils.DistanceXZ(roomiest, taken) < Gap) under++;
        return roomiest + Vector3.up * (StackHeight * under);
    }

    private static void Move(ZDO zdo, Vector3 spot, Vector3 center)
    {
        Vector3 facing = center - spot;
        facing.y = 0f;
        Quaternion rotation = facing.sqrMagnitude > 0.01f ? Quaternion.LookRotation(-facing.normalized) : zdo.GetRotation();

        // Only the owner's changes go out: the server takes it, moves it, and lets it go to whoever is near.
        zdo.SetOwner(ZDOMan.GetSessionID());
        zdo.SetPosition(spot);
        zdo.SetRotation(rotation);
        zdo.Set(ZDOVars.s_spawnPoint, spot);
        zdo.Set(ZDOVars.s_inWater, false);
        // The host's own game may have it loaded: its copy is moved too.
        if (ZNetScene.instance && ZNetScene.instance.FindInstance(zdo) is { } instance)
        {
            instance.transform.SetPositionAndRotation(spot, rotation);
            if (instance.GetComponent<Rigidbody>() is { } body)
            {
                body.position = spot;
                body.rotation = rotation;
                body.linearVelocity = Vector3.zero;
            }
        }
        zdo.SetOwner(0L);
        // Also to a game that had it loaded far away, so it lets go of it.
        ZDOMan.instance.ForceSendZDO(zdo.m_uid);
    }

    // ---- Players ----

    // The local player received the rite at the obliterator at position: finds the free spots and asks for the pull.
    internal static void Received(Vector3 position) => Ask(position, summoned: false);

    // From the console (addstatus): the local player's tombstones are pulled to where they stand. The server does this
    // only for an admin, as it does the console's other cheats.
    internal static void Summoned()
    {
        if (Player.m_localPlayer) Ask(Player.m_localPlayer.transform.position, summoned: true);
    }

    private static void Ask(Vector3 position, bool summoned)
    {
        if (PatchGuard.IsDisabled(Feature) || !ZNet.instance || ZRoutedRpc.instance == null || !Player.m_localPlayer) return;
        try
        {
            FindSpots(position, Spots);
            if (ZNet.instance.IsServer())
            {
                Player me = Player.m_localPlayer;
                if (Pull(me.GetPlayerID(), me.GetPlayerName(), position, new List<Vector3>(Spots), summoned, out int moved, out int there))
                    Done(moved, there, Left);
                return;
            }
            ZPackage package = new();
            package.Write(position);
            package.Write(Spots.Count);
            foreach (Vector3 spot in Spots) package.Write(spot);
            ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.SoulTether, package);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void RPC_Done(long sender, ZPackage package)
    {
        if (!ZNet.instance || ZNet.instance.IsServer() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
        try
        {
            int moved = package.ReadInt();
            int there = package.ReadInt();
            List<Vector3> left = ReadSpots(package, int.MaxValue);
            Done(moved, there, left);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Done(int moved, int there, List<Vector3> left)
    {
        Tell(moved, there);
        CrossOutDeathPins(left);
    }

    // Crosses out the death pin of each tombstone pulled away, as clicking it on the map does: it no longer lies there.
    // A tombstone whose pin was taken off the map has none to cross out. Each pin is crossed out for one tombstone only.
    private static void CrossOutDeathPins(List<Vector3> left)
    {
        Minimap? map = Minimap.instance;
        if (!map || left.Count == 0) return;
        int crossed = 0;
        List<Minimap.PinData> used = [];
        foreach (Vector3 at in left)
        {
            Minimap.PinData? closest = null;
            float best = PinReach;
            foreach (Minimap.PinData pin in map!.m_pins)
            {
                if (pin.m_type != Minimap.PinType.Death || used.Contains(pin)) continue;
                float distance = Utils.DistanceXZ(pin.m_pos, at);
                if (distance > best) continue;
                best = distance;
                closest = pin;
            }
            if (closest == null) continue;
            used.Add(closest);
            if (closest.m_checked) continue;
            closest.m_checked = true;
            crossed++;
        }
        if (crossed > 0) map.m_pinUpdateRequired = true;
    }

    private static void Tell(int moved, int there)
    {
        string what = WeatherKinds.Colored(WeatherKind.SoulTether);
        if (moved > 0) PlayerMessages.TopLeft(string.Format(Messages.SoulTetherPulled, what));
        else PlayerMessages.TopLeft(string.Format(there > 0 ? Messages.SoulTetherAllThere : Messages.SoulTetherNone, what));
    }

    // Free spots around the obliterator for a tombstone, nearest first: ring after ring, evenly along each.
    internal static void FindSpots(Vector3 center, List<Vector3> spots) => FindSpots(center, spots, Room);

    // The same for something else: room is half its size (width, height, depth). Also used by the Rally-Horn Rite.
    internal static void FindSpots(Vector3 center, List<Vector3> spots, Vector3 room)
    {
        spots.Clear();
        if (_roomMask == 0)
        {
            _groundMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
            _roomMask = _groundMask | LayerMask.GetMask("item", "piece_nonsolid", "character", "character_net", "character_noenv");
        }
        for (int ring = 0; ring < Rings && spots.Count < MaxSpots; ring++)
        {
            float radius = FirstRing + ring * RingStep;
            int count = Mathf.Max(6, Mathf.FloorToInt(2f * Mathf.PI * radius / Spacing));
            // Each ring a little turned, so the spots of neighboring rings don't line up.
            float turn = ring * 0.5f * (2f * Mathf.PI / count);
            for (int k = 0; k < count && spots.Count < MaxSpots; k++)
            {
                float angle = turn + k * 2f * Mathf.PI / count;
                Vector3 at = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (Free(at, center.y, room, out Vector3 spot)) spots.Add(spot);
            }
        }
    }

    private static bool Free(Vector3 at, float level, Vector3 room, out Vector3 spot)
    {
        spot = at;
        // Level ground or a floor near the obliterator's height.
        Vector3 from = new(at.x, level + GroundAbove + 0.05f, at.z);
        if (Physics.Raycast(from, Vector3.down, out RaycastHit ground, GroundAbove + GroundBelow + 0.05f, _groundMask,
                QueryTriggerInteraction.Ignore))
            spot = new Vector3(at.x, ground.point.y + DropLift, at.z);
        // Else a hillside: the highest terrain under the tombstone's footprint, so it starts above the slope, never in it.
        else if (!OnHill(at, level, room, out spot)) return false;
        Vector3 middle = spot + Vector3.up * (RoomLift + room.y);
        return Physics.OverlapBoxNonAlloc(middle, room, Hits, Quaternion.identity, _roomMask, QueryTriggerInteraction.Ignore) == 0;
    }

    private static bool OnHill(Vector3 at, float level, Vector3 room, out Vector3 spot)
    {
        spot = at;
        if (!ZoneSystem.instance) return false;
        float low = float.MaxValue, high = float.MinValue;
        for (int i = 0; i < 5; i++)
        {
            Vector3 point = i == 0 ? at : at + new Vector3(i % 2 == 0 ? room.x : -room.x, 0f, i < 3 ? room.z : -room.z);
            if (!ZoneSystem.instance.GetGroundHeight(point, out float height)) return false;
            low = Mathf.Min(low, height);
            high = Mathf.Max(high, height);
        }
        if (high - low > HillRise || Mathf.Abs(high - level) > HillReach) return false;
        spot = new Vector3(at.x, high + DropLift, at.z);
        return true;
    }

    private static List<Vector3> ReadSpots(ZPackage package, int max = MaxSpots)
    {
        int count = Mathf.Clamp(package.ReadInt(), 0, max);
        List<Vector3> spots = new(count);
        for (int i = 0; i < count; i++) spots.Add(package.ReadVector3());
        return spots;
    }
}
