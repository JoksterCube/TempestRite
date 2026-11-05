using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Patches;

// While a Crimson, Ragnarök or Hoard-Bane Rite goes on, its Blood Grounds belong to the fight, in the game that owns each thing:
// - spawners within it (nests, bone piles, the creature spawners of locations and dungeons) stay still, and nothing
//   spawns there on its own: only what the rite brings. They go on as before once it ends;
// - no boss altar whose boss could rise within it answers.
internal static class IncursionSpawnPatches
{
    private const string Feature = "Incursion spawners";

    private static bool Within(Vector3 point)
    {
        if (PatchGuard.IsDisabled(Feature)) return false;
        try
        {
            WeatherKind kind = IncursionArena.Kind;
            return kind != WeatherKind.None && WeatherState.Reaches(kind, point, IncursionArena.SamePlace);
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return false;
        }
    }

    [HarmonyPatch(typeof(SpawnArea), nameof(SpawnArea.UpdateSpawn))]
    private static class Nests
    {
        private static bool Prefix(SpawnArea __instance) => !__instance || !Within(__instance.transform.position);
    }

    [HarmonyPatch(typeof(CreatureSpawner), nameof(CreatureSpawner.UpdateSpawner))]
    private static class Spawners
    {
        private static bool Prefix(CreatureSpawner __instance) => !__instance || !Within(__instance.transform.position);
    }

    // The world's own spawns (not raids: the Crimson Rite's come that way).
    private static bool _natural;

    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawnList))]
    private static class SpawnList
    {
        private static void Prefix(bool eventSpawners) => _natural = !eventSpawners && IncursionArena.Kind != WeatherKind.None;

        private static Exception? Finalizer(Exception? __exception)
        {
            _natural = false;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.IsSpawnPointGood))]
    private static class GoodPoint
    {
        private static void Postfix(ref Vector3 spawnPoint, ref bool __result)
        {
            if (_natural && __result && Within(spawnPoint)) __result = false;
        }
    }

    private static bool AltarBlocked(OfferingBowl bowl)
    {
        if (!bowl || !bowl.m_bossPrefab || PatchGuard.IsDisabled(Feature)) return false;
        try
        {
            return IncursionArena.Sealed(out WeatherKind kind, out Vector3 center) && IncursionSite.AltarReaches(bowl, center, PluginConfig.Radius(kind));
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return false;
        }
    }

    // In the game of the player offering: refused before anything is taken.
    [HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.InitiateSpawnBoss))]
    private static class Summon
    {
        private static bool Prefix(OfferingBowl __instance)
        {
            if (!AltarBlocked(__instance)) return true;
            Humanoid? user = __instance.m_interactUser;
            if (user && user == Player.m_localPlayer)
            {
                IncursionArena.Sealed(out WeatherKind kind, out _);
                user!.Message(MessageHud.MessageType.Center, string.Format(Messages.IncursionNoSummon, WeatherKinds.Name(kind)));
            }
            return false;
        }
    }

    // A boss already called as the rite began doesn't rise.
    [HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.DelayedSpawnBoss))]
    private static class Rise
    {
        private static bool Prefix(OfferingBowl __instance)
        {
            if (!AltarBlocked(__instance)) return true;
            IncursionArena.Sealed(out WeatherKind kind, out _);
            Plugin.ModLogger.LogInfo($"{WeatherKinds.Name(kind)}: the boss of {__instance.name} didn't rise, the rite goes on too near.");
            return false;
        }
    }
}

// While a Crimson, Ragnarök or Hoard-Bane Rite goes on, nothing carries the local player into, out of or within its
// Blood Grounds: portals, dungeon doors, the map or console teleports, other mods. Coming up out of a dungeon still works.
[HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
internal static class IncursionTeleportPatch
{
    private const string Feature = "Incursion teleports";

    private static bool Prefix(Player __instance, Vector3 pos, ref bool __result)
    {
        // Another player's character: their own game decides.
        if (__instance != Player.m_localPlayer || !__instance.m_nview || !__instance.m_nview.IsOwner() || PatchGuard.IsDisabled(Feature))
            return true;
        try
        {
            string? why = IncursionArena.TeleportBlocked(__instance.transform.position, pos);
            if (why == null) return true;
            __instance.Message(MessageHud.MessageType.Center, why);
            __result = false;
            return false;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return true;
        }
    }
}

// While a Crimson, Ragnarök or Hoard-Bane Rite goes on, the edge of its Blood Grounds stops the local player and the creatures
// as a wall: right after the game sets a character's speed for the physics step (only in the game that owns it), the
// part that would carry it through the edge is taken away (see IncursionArena.AtWall).
[HarmonyPatch(typeof(Character), nameof(Character.UpdateMotion))]
internal static class IncursionWallPatch
{
    private static void Postfix(Character __instance, float dt) => IncursionArena.AtWall(__instance, dt);
}
