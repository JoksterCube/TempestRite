using System;
using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// While a Crimson, Ragnarök or Hoard-Bane Rite goes on, its bubble walls the fight off:
// - no projectile gets through it, in or out: one crossing it hits the wall there and is gone, as at a shield
//   generator's dome (in the game that owns it, which moves it);
// - what is inside fights only what is inside, and what is outside only what is outside: creatures inside don't go
//   after animals or players beyond the wall, nor those outside after anything within;
// - no bed within it can be claimed or slept in.
internal static class IncursionFightPatches
{
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.FixedUpdate))]
    private static class Projectiles
    {
        private const string Feature = "Incursion projectile wall";
        private const float PruneInterval = 10f;

        // Each projectile seen while it goes on: whether it started inside.
        private static readonly Dictionary<Projectile, bool> Sides = [];
        private static float _nextPrune;

        private static void Postfix(Projectile __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (IncursionArena.Kind == WeatherKind.None)
                {
                    if (Sides.Count > 0) Sides.Clear();
                    return;
                }
                ZNetView nview = __instance.m_nview;
                if (!nview || !nview.IsValid() || !nview.IsOwner()) return;
                Prune();
                bool inside = IncursionArena.InBubble(__instance.transform.position);
                if (!Sides.TryGetValue(__instance, out bool started))
                {
                    Sides[__instance] = inside;
                    return;
                }
                if (started == inside) return;
                Sides.Remove(__instance);
                __instance.OnHit(null, __instance.transform.position, water: false, -__instance.transform.forward);
                if (nview.IsValid()) ZNetScene.instance.Destroy(__instance.gameObject);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Prune()
        {
            if (Time.time < _nextPrune) return;
            _nextPrune = Time.time + PruneInterval;
            List<Projectile> gone = [];
            foreach (Projectile known in Sides.Keys)
                if (!known) gone.Add(known!); // Destroyed by Unity, but still a real key.
            foreach (Projectile known in gone) Sides.Remove(known);
        }
    }

    private const string SidesFeature = "Incursion sides";

    // Whether two are on different sides of the wall.
    private static bool Apart(Character a, Character b)
    {
        if (PatchGuard.IsDisabled(SidesFeature)) return false;
        try
        {
            return a && b && IncursionArena.Kind != WeatherKind.None &&
                IncursionArena.InBubble(a.transform.position) != IncursionArena.InBubble(b.transform.position);
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(SidesFeature, exception);
            return false;
        }
    }

    // Nothing on the other side of the wall is an enemy: not looked for, not gone after.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsEnemy), typeof(Character), typeof(Character))]
    private static class Enemies
    {
        private static void Postfix(Character a, Character b, ref bool __result)
        {
            if (__result && Apart(a, b)) __result = false;
        }
    }

    // A creature hunting players with no enemy in sight goes for the nearest player: on its own side of the wall.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FindEnemy))]
    private static class Hunted
    {
        // As far as the game's own hunt looks.
        private const float HuntRange = 200f;

        private static void Postfix(BaseAI __instance, ref Character __result)
        {
            if (!__result || !__instance.m_character || !Apart(__instance.m_character, __result)) return;
            Vector3 from = __instance.transform.position;
            bool inside = IncursionArena.InBubble(from);
            Player? nearest = null;
            float best = HuntRange;
            foreach (Player player in Player.GetAllPlayers())
            {
                if (!player || player.IsDead() || player.InGhostMode() || player.InDebugFlyMode()) continue;
                if (IncursionArena.InBubble(player.transform.position) != inside) continue;
                float distance = Vector3.Distance(player.transform.position, from);
                if (distance >= best) continue;
                best = distance;
                nearest = player;
            }
            __result = nearest!;
        }
    }

    // A creature already after something on the other side (from before it began, or it crossed) lets it go.
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateTarget))]
    private static class Target
    {
        private static void Postfix(MonsterAI __instance)
        {
            Character target = __instance.m_targetCreature;
            if (target && Apart(__instance.m_character, target)) __instance.m_targetCreature = null;
        }
    }

    // In the game of the player using it: refused before anything happens.
    [HarmonyPatch(typeof(Bed), nameof(Bed.Interact))]
    private static class Beds
    {
        private const string Feature = "Incursion beds";

        private static bool Prefix(Bed __instance, Humanoid human, ref bool __result)
        {
            if (PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                if (IncursionArena.BedRefused(__instance.transform.position) is not { } why) return true;
                human.Message(MessageHud.MessageType.Center, why);
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
}
