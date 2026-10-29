using System;
using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Patches;

// The Crimson and Ragnarök Rites: on the server, no raid but the Crimson Rite's own starts while either goes on (ending
// one is always allowed). Players' games only follow the server's raid, so they are left alone: blocking it there kept
// raids from ever reaching them, and raid creatures are spawned by players' games.
[HarmonyPatch(typeof(RandEventSystem), nameof(RandEventSystem.SetRandomEvent))]
internal static class CrimsonPatch
{
    private static bool Prefix(RandomEvent? ev) =>
        ev == null || !ZNet.instance || !ZNet.instance.IsServer() || CrimsonRaids.Starting ||
        (!CrimsonRaids.Active && !RagnarokRush.Active);
}

// The Crimson Rite's raid reaches the whole bubble, also when its radius is bigger than the raid's own range.
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
// still not right next to a player (a little closer than the game's 40 m, so small bubbles get raids too).
internal static class CrimsonSpawnPatches
{
    private const string Feature = "Crimson Rite spawns";
    private const float NearPlayerShare = 0.35f;
    private const int Tries = 30;

    // While the game spawns the Crimson Rite's raid creatures.
    private static bool _confining;

    private static bool Confines()
    {
        if (PatchGuard.IsDisabled(Feature) || !CrimsonRaids.Active || !RandEventSystem.instance) return false;
        RandomEvent? raid = RandEventSystem.instance.GetActiveEvent();
        return raid != null && Utils.DistanceXZ(raid.m_pos, WeatherState.Position(WeatherKind.Crimson)) < CrimsonRaids.SamePlace;
    }

    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawnList))]
    private static class SpawnList
    {
        private static void Prefix(List<SpawnSystem.SpawnData> spawners, bool eventSpawners)
        {
            _confining = false;
            if (!eventSpawners || !Confines()) return;
            try
            {
                // The raid's own copy of its spawn list (see RandomEvent.Clone): changing it changes no other raid.
                float near = PluginConfig.Radius(WeatherKind.Crimson) * NearPlayerShare;
                foreach (SpawnSystem.SpawnData spawner in spawners)
                {
                    float min = spawner.m_spawnRadiusMin > 0f ? spawner.m_spawnRadiusMin : 40f;
                    if (min > near) spawner.m_spawnRadiusMin = near;
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

    // Each creature of a group too.
    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.IsSpawnPointGood))]
    private static class GoodPoint
    {
        private static void Postfix(ref Vector3 spawnPoint, ref bool __result)
        {
            if (!_confining || !__result) return;
            if (Utils.DistanceXZ(spawnPoint, WeatherState.Position(WeatherKind.Crimson)) > PluginConfig.Radius(WeatherKind.Crimson))
                __result = false;
        }
    }
}

// The Crimson and Ragnarök Rites: portals within the bubble, used by a player inside it, or leading into it, don't work.
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

// The Crimson Rite counts creatures dying within its bubble, and the Ragnarök Rite its bosses falling, told by the game
// that owns them.
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
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// The obliterator takes the Crimson Rite's reward and the Ragnarök Rite's treasure from the server, in the game that
// owns it.
[HarmonyPatch(typeof(Incinerator), nameof(Incinerator.Awake))]
internal static class CrimsonRewardPatch
{
    private static void Postfix(Incinerator __instance)
    {
        ZNetView nview = __instance.m_nview;
        if (!nview || nview.GetZDO() == null) return;
        nview.Register<int>(Rpcs.CrimsonReward, (sender, amount) =>
        {
            try { CrimsonArena.Reward(__instance, sender, amount); }
            catch (Exception exception) { Plugin.ModLogger.LogError($"Crimson Rite reward failed: {exception}"); }
        });
        nview.Register<ZPackage>(Rpcs.RagnarokReward, (sender, treasure) =>
        {
            try { RagnarokArena.Reward(__instance, sender, treasure); }
            catch (Exception exception) { Plugin.ModLogger.LogError($"Ragnarök Rite treasure failed: {exception}"); }
        });
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

// The Crimson and Ragnarök Rites: within the bubble nothing can be built, repaired or torn down, and players can't
// break pieces (only creatures can, the obliterator too). Checked in the game of the player doing it.
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

    // A player's hit on a piece within the bubble does nothing.
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage))]
    private static class Hit
    {
        private static bool Prefix(WearNTear __instance, HitData hit)
        {
            if (hit == null || IncursionArena.Kind == WeatherKind.None) return true;
            if (hit.GetAttacker() is not Player player) return true;
            return Allowed(player, __instance.transform.position);
        }
    }
}
