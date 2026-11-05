using System.Collections.Generic;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// Whether the creatures of a Crimson, Ragnarök or Hoard-Bane Rite have room to rise on its Blood Grounds:
// - a spot has room for a creature of a size when its ground (the land, or a floor built there) is above the sea, not
//   lava, not indoors, and nothing (a wall, a rock, a tree, a building) stands in an upright capsule of that size on it;
// - the Crimson Rite needs a dozen such spots for its raid creatures, the Ragnarök Rite half a dozen for its bosses
//   where they rise (away from the obliterator and the edge), the Hoard-Bane Rite a turn of its layout (three elite
//   lairs and the miniboss's post, see HoardBaneLoot) where all of them have room.
// Without room the rite is refused (see IncursionSite); the spawns themselves only use spots with room (see
// RagnarokArena, HoardBaneArena, CrimsonSpawnPatches), and the Ley-Survey Rite shows the spots (see LeySurvey).
// Where this game has nothing loaded (the server, far from any player) it can't tell, and that isn't held against it.
internal static class ArenaRoom
{
    internal readonly struct Size(float radius, float height)
    {
        internal float Radius { get; } = radius;
        internal float Height { get; } = height;
    }

    internal static readonly Size Raider = new(0.8f, 2f);
    internal static readonly Size Elite = new(1.2f, 2.5f);
    internal static readonly Size Boss = new(2f, 4f);

    // How far apart the Blood Grounds are looked at.
    internal const float Step = 4f;
    internal const int CrimsonNeeded = 12;
    internal const int RagnarokNeeded = 6;
    // How many turns of the Hoard-Bane layout are tried.
    private const int Turns = 36;
    // How far a blocked creature is moved to find room.
    private static readonly float[] NudgeRings = [1.5f, 3f, 4.5f, 6f];
    private const int NudgeDirections = 8;

    private static int _blockMask;

    // What the Ley-Survey Rite shows and says about room.
    internal sealed class Survey
    {
        internal bool Crimson = true, Ragnarok = true, HoardBane = true;
        internal int CrimsonSpots;
        // The spots with room for a raid creature (all of them).
        internal readonly List<Vector3> CrimsonAt = [];
        internal readonly List<Vector3> RagnarokSpots = [];
        // The Hoard-Bane layout's turn with the most room: its lairs and the miniboss's post, and which have room.
        internal readonly List<(Vector3 At, bool Room, bool Boss)> HoardBanePosts = [];

        internal bool Clear => Crimson && Ragnarok && HoardBane;
    }

    // Whether a creature this big can rise here; at: on its ground. Where nothing is loaded: unknown (counted as room
    // when asked to).
    internal static bool Fits(Vector3 point, Size size, out Vector3 at, bool unknownFits = false, bool lavaFits = false)
    {
        at = point;
        if (!IncursionArena.GroundAt(point, out float ground)) return unknownFits;
        at.y = ground;
        ZoneSystem zones = ZoneSystem.instance;
        if (!zones || ground < zones.m_waterLevel) return false;
        if (!lavaFits && zones.IsLava(at)) return false;
        if (Character.InInterior(at)) return false;
        if (_blockMask == 0) _blockMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
        // Lifted a little off the ground, so a gentle slope or the floor itself isn't in the way.
        float lift = size.Radius * 0.6f + 0.1f;
        Vector3 bottom = at + Vector3.up * (size.Radius + lift);
        Vector3 top = at + Vector3.up * Mathf.Max(size.Radius + lift, size.Height - size.Radius + lift);
        return !Physics.CheckCapsule(bottom, top, size.Radius, _blockMask, QueryTriggerInteraction.Ignore);
    }

    // The nearest spot with room around this one, within the arena (the spot itself first); else the spot itself.
    internal static Vector3 Nearby(Vector3 point, Size size, Vector3 center, float radius)
    {
        if (Fits(point, size, out Vector3 at)) return at;
        foreach (float ring in NudgeRings)
            for (int i = 0; i < NudgeDirections; i++)
            {
                float angle = i * Mathf.PI * 2f / NudgeDirections;
                Vector3 spot = point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring;
                if (Utils.DistanceXZ(spot, center) > radius - size.Radius) continue;
                if (Fits(spot, size, out at)) return at;
            }
        return point;
    }

    // ---- Each rite ----

    // Why the rite can't be performed for lack of room here, or false.
    internal static bool Cramped(WeatherKind kind, Vector3 center) => kind switch
    {
        WeatherKind.Crimson => CrimsonSpots(center, CrimsonNeeded, unknownFits: true) < CrimsonNeeded,
        WeatherKind.Ragnarok => RagnarokSpots(center, null, RagnarokNeeded, unknownFits: true) < RagnarokNeeded,
        WeatherKind.HoardBane => !HoardBaneTurn(center, 0f, unknownFits: true, out _, null),
        _ => false
    };

    // Everything the Ley-Survey Rite shows about room, for the rites that are on.
    internal static void Look(Vector3 center, Survey survey)
    {
        survey.RagnarokSpots.Clear();
        survey.HoardBanePosts.Clear();
        survey.CrimsonAt.Clear();
        survey.CrimsonSpots = PluginConfig.IsEnabled(WeatherKind.Crimson) ? CrimsonSpots(center, int.MaxValue, false, survey.CrimsonAt) : CrimsonNeeded;
        survey.Crimson = survey.CrimsonSpots >= CrimsonNeeded;
        survey.Ragnarok = !PluginConfig.IsEnabled(WeatherKind.Ragnarok)
            || RagnarokSpots(center, survey.RagnarokSpots, int.MaxValue, false) >= RagnarokNeeded;
        survey.HoardBane = !PluginConfig.IsEnabled(WeatherKind.HoardBane)
            || HoardBaneTurn(center, 0f, unknownFits: false, out _, survey.HoardBanePosts);
    }

    // Spots on a grid across the Crimson Rite's Blood Grounds with room for a raid creature (stops at enough).
    private static int CrimsonSpots(Vector3 center, int enough, bool unknownFits, List<Vector3>? all = null)
    {
        float radius = PluginConfig.Radius(WeatherKind.Crimson);
        int found = 0;
        foreach (Vector3 spot in Grid(center, 0f, radius - Raider.Radius))
        {
            if (!Fits(spot, Raider, out Vector3 at, unknownFits)) continue;
            all?.Add(at);
            if (++found >= enough) break;
        }
        return found;
    }

    // What a rite lacks room for, for its refusal: its raid creatures, bosses, or elites and miniboss.
    internal static string What(WeatherKind kind) => kind switch
    {
        WeatherKind.Ragnarok => Settings.Constants.Messages.RoomBosses,
        WeatherKind.HoardBane => Settings.Constants.Messages.RoomElites,
        _ => Settings.Constants.Messages.RoomRaiders
    };

    // Spots on a grid where the Ragnarök Rite's bosses rise (see RagnarokArena) with room for a boss.
    private static int RagnarokSpots(Vector3 center, List<Vector3>? all, int enough, bool unknownFits)
    {
        (float clear, float reach) = RagnarokArena.SpawnRing();
        int found = 0;
        foreach (Vector3 spot in Grid(center, clear, reach))
        {
            if (!Fits(spot, Boss, out Vector3 at, unknownFits)) continue;
            all?.Add(at);
            if (++found >= enough) break;
        }
        return found;
    }

    // The turn of the Hoard-Bane layout nearest from (degrees) where its lairs and the miniboss's post all have room.
    // False when no turn has; then angle is the one with the most room. posts: that turn's posts, if wanted.
    internal static bool HoardBaneTurn(Vector3 center, float from, bool unknownFits, out float angle,
        List<(Vector3 At, bool Room, bool Boss)>? posts)
    {
        float radius = PluginConfig.Radius(WeatherKind.HoardBane);
        float lair = HoardBaneArena.LairRadius;
        const int lairs = 3;
        angle = from;
        int most = -1;
        for (int turn = 0; turn < Turns; turn++)
        {
            float tryAngle = from + turn * 360f / Turns;
            int room = 0;
            foreach (Vector2 post in HoardBaneLoot.LairPosts(radius, lair, tryAngle, lairs))
                if (Fits(center + new Vector3(post.x, 0f, post.y), Elite, out _, unknownFits)) room++;
            Vector2 boss = HoardBaneLoot.BossPost(radius, tryAngle, lairs);
            if (Fits(center + new Vector3(boss.x, 0f, boss.y), Boss, out _, unknownFits)) room++;
            if (room <= most) continue;
            most = room;
            angle = tryAngle;
            if (room == lairs + 1) break;
        }
        if (posts != null)
        {
            posts.Clear();
            foreach (Vector2 post in HoardBaneLoot.LairPosts(radius, lair, angle, lairs))
            {
                bool room = Fits(center + new Vector3(post.x, 0f, post.y), Elite, out Vector3 at);
                posts.Add((at, room, false));
            }
            Vector2 boss = HoardBaneLoot.BossPost(radius, angle, lairs);
            bool bossRoom = Fits(center + new Vector3(boss.x, 0f, boss.y), Boss, out Vector3 bossAt);
            posts.Add((bossAt, bossRoom, true));
        }
        return most == lairs + 1;
    }

    // Grid points between two distances from the center.
    private static IEnumerable<Vector3> Grid(Vector3 center, float inner, float outer)
    {
        int steps = Mathf.CeilToInt(outer / Step);
        for (int x = -steps; x <= steps; x++)
            for (int z = -steps; z <= steps; z++)
            {
                Vector3 offset = new Vector3(x, 0f, z) * Step;
                float distance = offset.magnitude;
                if (distance < inner || distance > outer) continue;
                yield return center + offset;
            }
    }
}
