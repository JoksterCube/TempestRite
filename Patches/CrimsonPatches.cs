using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Patches;

// The Crimson, Ragnarök and Hoard-Bane Rites: on the server, no raid but the Crimson Rite's own starts while one goes on,
// nor once its lever is pulled (see IncursionPending; ending one is always allowed). Players' games only follow the server's raid, so they are left alone: blocking it there kept
// raids from ever reaching them, and raid creatures are spawned by players' games.
[HarmonyPatch(typeof(RandEventSystem), nameof(RandEventSystem.SetRandomEvent))]
internal static class CrimsonPatch
{
    private static bool Prefix(RandomEvent? ev) =>
        ev == null || !ZNet.instance || !ZNet.instance.IsServer() || CrimsonRaids.Starting ||
        (!CrimsonRaids.Active && !RagnarokRush.Active && !HoardBaneRush.Active && !IncursionPending.Now(out _, out _));
}

// The Crimson Rite: the game's scaling of creature health and damage by the number of players within 100 m counts,
// within the Blood Grounds, only the players inside it (not those running around outside, however near), with the game's own
// limits (at least 1, at most its maximum, a forced player count still wins). Outside it, as usual. The Ragnarök Rite
// does the same for its Blood Grounds (see RagnarokPatches.PlayerCountPatch).
[HarmonyPatch(typeof(Game), nameof(Game.GetPlayerDifficulty))]
internal static class CrimsonDifficultyPatch
{
    private const string Feature = "Crimson Rite player scaling";

    private static bool Prefix(Game __instance, Vector3 pos, ref int __result)
    {
        if (__instance.m_forcePlayers > 0 || PatchGuard.IsDisabled(Feature)) return true;
        try
        {
            int? inside = IncursionArena.CrimsonPlayersInside(pos);
            if (inside == null) return true;
            __result = Mathf.Clamp(inside.Value, 1, Mathf.Max(1, __instance.m_difficultyScaleMaxPlayers));
            return false;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return true;
        }
    }
}

// The Crimson Rite's raid reaches the whole Blood Grounds, also when its radius is bigger than the raid's own range.
[HarmonyPatch(typeof(RandEventSystem), nameof(RandEventSystem.IsInsideRandomEventArea))]
internal static class CrimsonRaidAreaPatch
{
    private static bool Prefix(RandomEvent re, Vector3 position, ref bool __result)
    {
        if (re == null || !CrimsonRaids.Active) return true;
        Vector3 center = WeatherState.Position(WeatherKind.Crimson);
        if (Utils.DistanceXZ(re.m_pos, center) >= CrimsonRaids.SamePlace) return true;
        float range = Mathf.Max(re.m_eventRange, PluginConfig.Radius(WeatherKind.Crimson) + CrimsonRaids.SamePlace);
        __result = position.y <= 3000f && Utils.DistanceXZ(position, re.m_pos) < range;
        return false;
    }
}

// The Crimson Rite's raid creatures spawn only within its radius: around the obliterator rather than around players,
// still not right next to a player (a little closer than the game's 40 m, so small Blood Grounds get raids too).
internal static class CrimsonSpawnPatches
{
    private const string Feature = "Crimson Rite spawns";
    private const float NearPlayerShare = 0.35f;
    private const int Tries = 30;

    // While the game spawns the Crimson Rite's raid creatures.
    private static bool _confining;

    // A raid spawner's own values (each raid's own copies, forgotten with the raid), and what it was last given.
    private sealed class Original(SpawnSystem.SpawnData spawner)
    {
        internal readonly int MaxSpawned = spawner.m_maxSpawned;
        internal readonly float Interval = spawner.m_spawnInterval;
        internal readonly int GroupMin = spawner.m_groupSizeMin;
        internal readonly int GroupMax = spawner.m_groupSizeMax;
        internal readonly int MinLevel = spawner.m_minLevel;
        internal readonly int MaxLevel = spawner.m_maxLevel;
        internal readonly float LevelUpChance = spawner.m_overrideLevelupChance;
        internal readonly float LevelUpDistance = spawner.m_levelUpMinCenterDistance;
        internal float GivenMore = -1f;
        internal int GivenSteps = -1;
    }

    private static readonly ConditionalWeakTable<SpawnSystem.SpawnData, Original> Originals = new();

    private static RandomEvent? OurRaid()
    {
        if (PatchGuard.IsDisabled(Feature) || !CrimsonRaids.Active || !RandEventSystem.instance) return null;
        RandomEvent? raid = RandEventSystem.instance.GetActiveEvent();
        return raid != null && Utils.DistanceXZ(raid.m_pos, WeatherState.Position(WeatherKind.Crimson)) < CrimsonRaids.SamePlace ? raid : null;
    }

    // More creatures, more often (Raid Spawn Multiplier; 1: the game's own), and for a raid below the players stars
    // (Raid Catch-Up, see RaidCatchUp). Always from the spawner's own values, so a catch-up told late still counts.
    private static void Give(SpawnSystem.SpawnData spawner, Original original, float more, int steps)
    {
        original.GivenMore = more;
        original.GivenSteps = steps;
        RaidCatchUp.Boost boost = RaidCatchUp.For(steps, PluginConfig.CrimsonCatchUp.Value);
        spawner.m_maxSpawned = original.MaxSpawned > 0 && more > 1f
            ? Mathf.Max(original.MaxSpawned + 1, Mathf.RoundToInt(original.MaxSpawned * more))
            : original.MaxSpawned;
        spawner.m_spawnInterval = more > 1f ? Mathf.Max(1f, original.Interval / more) : original.Interval;
        spawner.m_groupSizeMin = original.GroupMin;
        spawner.m_groupSizeMax = original.GroupMax;
        if (!boost.Any)
        {
            spawner.m_minLevel = original.MinLevel;
            spawner.m_maxLevel = original.MaxLevel;
            spawner.m_overrideLevelupChance = original.LevelUpChance;
            spawner.m_levelUpMinCenterDistance = original.LevelUpDistance;
            return;
        }
        // Never more stars than the game ever gives the creature (no starred ghosts).
        int most = Mathf.Max(original.MaxLevel, VanillaStars.MostLevel(spawner.m_prefab ? spawner.m_prefab.name : ""));
        spawner.m_maxLevel = Mathf.Max(original.MaxLevel, Mathf.Min(most, boost.MaxLevel));
        spawner.m_minLevel = Mathf.Min(spawner.m_maxLevel, Mathf.Max(original.MinLevel, boost.MinLevel));
        spawner.m_overrideLevelupChance = spawner.m_maxLevel > original.MaxLevel
            ? Mathf.Max(original.LevelUpChance, boost.LevelUpChance) : original.LevelUpChance;
        // Stars even near the world's center.
        spawner.m_levelUpMinCenterDistance = 0f;
    }

    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawnList))]
    private static class SpawnList
    {
        private static void Prefix(List<SpawnSystem.SpawnData> spawners, bool eventSpawners)
        {
            _confining = false;
            RandomEvent? raid = eventSpawners ? OurRaid() : null;
            if (raid == null) return;
            try
            {
                // The raid's own copy of its spawn list (see RandomEvent.Clone): changing it changes no other raid.
                float near = PluginConfig.Radius(WeatherKind.Crimson) * NearPlayerShare;
                float more = Mathf.Max(1f, PluginConfig.CrimsonSpawnMultiplier.Value);
                int steps = CrimsonArena.CatchUpSteps(raid.m_name);
                foreach (SpawnSystem.SpawnData spawner in spawners)
                {
                    float min = spawner.m_spawnRadiusMin > 0f ? spawner.m_spawnRadiusMin : 40f;
                    if (min > near) spawner.m_spawnRadiusMin = near;
                    // This list is gone through again and again: given anew only when something changed.
                    Original original = Originals.GetValue(spawner, s => new Original(s));
                    if (original.GivenMore != more || original.GivenSteps != steps) Give(spawner, original, more, steps);
                }
                _confining = true;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            _confining = false;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.FindBaseSpawnPoint))]
    private static class BasePoint
    {
        private static bool Prefix(SpawnSystem __instance, SpawnSystem.SpawnData spawn, List<Player> allPlayers,
            out Vector3 spawnCenter, out Player? targetPlayer, ref bool __result)
        {
            spawnCenter = Vector3.zero;
            targetPlayer = null;
            if (!_confining) return true;
            Vector3 center = WeatherState.Position(WeatherKind.Crimson);
            float reach = Mathf.Max(5f, PluginConfig.Radius(WeatherKind.Crimson) - spawn.m_groupRadius - CrimsonRaids.SamePlace);
            for (int i = 0; i < Tries; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * reach;
                Vector3 point = center + new Vector3(offset.x, 0f, offset.y);
                if (!__instance.IsSpawnPointGood(spawn, ref point)) continue;
                spawnCenter = point;
                targetPlayer = allPlayers.Count > 0 ? allPlayers[0] : null;
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }
    }

    // Each creature of a group too: the rite's own check instead of the game's. On its Blood Grounds, on the ground there
    // (the land, or a floor built on it) above the sea, not in lava (unless the creature belongs there), not indoors, and
    // not right next to a player. Nothing else keeps them away: no workbench, fire or other base, no building over the
    // spot, no biome, height or slope, and no other mod's check, since the rite calls them.
    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.IsSpawnPointGood))]
    private static class GoodPoint
    {
        private static bool _result;

        [HarmonyPriority(Priority.First)]
        private static bool Prefix(SpawnSystem.SpawnData spawn, ref Vector3 spawnPoint, ref bool __result)
        {
            if (!_confining) return true;
            try { _result = OnGrounds(spawn, ref spawnPoint); }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
            __result = _result;
            return false;
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ref bool __result)
        {
            if (_confining && !PatchGuard.IsDisabled(Feature)) __result = _result;
        }
    }

    private static bool OnGrounds(SpawnSystem.SpawnData spawn, ref Vector3 point)
    {
        if (Utils.DistanceXZ(point, WeatherState.Position(WeatherKind.Crimson)) > PluginConfig.Radius(WeatherKind.Crimson)) return false;
        // Dry ground with room for it, not indoors (see ArenaRoom).
        if (!ArenaRoom.Fits(point, ArenaRoom.Raider, out Vector3 at, lavaFits: spawn.m_inLava)) return false;
        point = at;
        if (!spawn.m_outsideLava && !ZoneSystem.instance.IsLava(point)) return false;
        float near = spawn.m_spawnRadiusMin > 0f ? spawn.m_spawnRadiusMin : 40f;
        return spawn.m_canSpawnCloseToPlayer || !Player.IsPlayerInRange(point, near);
    }
}

// The Crimson and Ragnarök Rites: portals within the Blood Grounds, used by a player inside it, or leading into it, don't work.
[HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
internal static class IncursionPortalPatch
{
    private const string Feature = "Incursion portals";

    private static bool Prefix(TeleportWorld __instance, Player player)
    {
        if (!player || PatchGuard.IsDisabled(Feature)) return true;
        try
        {
            Vector3? exit = null;
            ZDO? self = __instance.m_nview && __instance.m_nview.IsValid() ? __instance.m_nview.GetZDO() : null;
            ZDO? target = self == null ? null : ZDOMan.instance.GetZDO(self.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal));
            if (target != null) exit = target.GetPosition();
            string? why = IncursionArena.PortalBlocked(__instance.transform.position, player.transform.position, exit);
            if (why == null) return true;
            player.Message(MessageHud.MessageType.Center, why);
            return false;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return true;
        }
    }
}

// The Crimson Rite counts creatures dying within its Blood Grounds, the Ragnarök Rite its bosses falling and the Hoard-Bane Rite
// its elites and minibosses, told by the game that owns them.
[HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
internal static class CrimsonKillPatch
{
    private const string Feature = "Crimson and Ragnarök Rite kills";

    private static void Prefix(Character __instance)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            CrimsonArena.CreatureDied(__instance);
            RagnarokArena.CreatureDied(__instance);
            HoardBaneArena.CreatureDied(__instance);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// The obliterator's owner takes the Crimson, Ragnarök and Hoard-Bane Rites' treasure chests from the server (see
// TreasureChests), and what goes back inside: a refused rite's key items, a failed Hoard-Bane Rite's chest.
[HarmonyPatch(typeof(Incinerator), nameof(Incinerator.Awake))]
internal static class CrimsonRewardPatch
{
    private static void Postfix(Incinerator __instance)
    {
        ZNetView nview = __instance.m_nview;
        if (!nview || nview.GetZDO() == null) return;
        nview.Register<ZPackage>(Rpcs.TreasureChests, (sender, treasure) =>
        {
            try { TreasureChests.Give(__instance, sender, treasure); }
            catch (Exception exception) { Plugin.ModLogger.LogError($"Treasure chests failed: {exception}"); }
        });
        nview.Register<ZPackage>(Rpcs.ObliteratorRefund, (sender, keys) =>
        {
            try { RagnarokArena.Reward(__instance, sender, keys, "Refused rite"); }
            catch (Exception exception) { Plugin.ModLogger.LogError($"Refused rite's key items failed: {exception}"); }
        });
        nview.Register<ZPackage>(Rpcs.HoardBaneReward, (sender, chest) =>
        {
            try { RagnarokArena.Reward(__instance, sender, chest, Messages.HoardBaneName); }
            catch (Exception exception) { Plugin.ModLogger.LogError($"Hoard-Bane Rite chest back failed: {exception}"); }
        });
    }
}

// Inside a Crimson, Ragnarök or Hoard-Bane Rite's Blood Grounds, fight music plays when the game has no raid or boss music of
// its own (see ArenaMusic). Only the music player asks this.
[HarmonyPatch(typeof(RandEventSystem), nameof(RandEventSystem.GetMusicOverride))]
internal static class IncursionMusicPatch
{
    private const string Feature = "Incursion music";

    private static void Postfix(ref string? __result)
    {
        if (!string.IsNullOrEmpty(__result) || PatchGuard.IsDisabled(Feature)) return;
        try { __result = ArenaMusic.Now(); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// The Crimson and Ragnarök Rites' areas on the map are tinted after the map draws its pins white.
[HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePins))]
internal static class IncursionPinPatch
{
    private const string Feature = "Incursion map marks";

    private static void Postfix()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { IncursionArena.TintPins(); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// The Crimson, Ragnarök and Hoard-Bane Rites: within the Blood Grounds nothing can be built, repaired or torn down with the
// hammer (hitting pieces with weapons and tools still works). Checked in the game of the player doing it.
internal static class IncursionBuildPatches
{
    private const string Feature = "Incursion building";

    private static bool Allowed(Player player, Vector3 at)
    {
        if (PatchGuard.IsDisabled(Feature)) return true;
        try
        {
            string? why = IncursionArena.BuildRefused(at);
            if (why == null) return true;
            if (player == Player.m_localPlayer) player.Message(MessageHud.MessageType.Center, why);
            return false;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return true;
        }
    }

    // Where the piece would go, or where the piece hovered over is; else where the player stands.
    private static Vector3 Target(Player player, GameObject? at) => at ? at!.transform.position : player.transform.position;

    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    private static class Place
    {
        private static bool Prefix(Player __instance, ref bool __result)
        {
            if (Allowed(__instance, Target(__instance, __instance.m_placementGhost))) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Repair))]
    private static class Repair
    {
        private static bool Prefix(Player __instance)
        {
            Piece? piece = __instance.GetHoveringPiece();
            return Allowed(__instance, Target(__instance, piece ? piece!.gameObject : null));
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.RemovePiece))]
    private static class Remove
    {
        private static bool Prefix(Player __instance, ref bool __result)
        {
            Piece? piece = __instance.GetHoveringPiece();
            if (Allowed(__instance, Target(__instance, piece ? piece!.gameObject : null))) return true;
            __result = false;
            return false;
        }
    }
}
