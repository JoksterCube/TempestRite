using System;
using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// The Hoard-Bane Rite's elites and minibosses (see HoardBaneArena):
// - an elite asleep in its lair doesn't move nor take damage: the first hit, from anything and from anywhere, only
//   wakes it (so no sneak attack). A player stepping into its lair wakes it too;
// - a shielded miniboss takes no damage, and its shield shows the hit in the game of the player hitting it;
// - everything the rite brings fights on one side: none of it attacks the rest;
// - elites go by their own names, and look their strength: beyond the levels the game draws, the highest it draws,
//   bigger (and darker, see HoardBaneArena.Decorate), with as many stars over them as they have; minibosses a little
//   bigger and darker too; their corpses as they were.
internal static class HoardBanePatches
{
    // Asleep: no thinking, no moving. Checked in the game that owns it, which runs its AI.
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class DormantPatch
    {
        private const string Feature = "Hoard-Bane Rite sleeping elites";

        private static bool Prefix(MonsterAI __instance, ref bool __result)
        {
            if (PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                Character character = __instance.m_character;
                if (!character || !HoardBaneArena.IsDormant(character)) return true;
                __instance.StopMoving();
                __result = true;
                return false;
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    // Hits worked out in the game that owns what was hit.
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    internal static class DamagePatch
    {
        private const string Feature = "Hoard-Bane Rite shields";

        private static bool Prefix(Character __instance, HitData hit)
        {
            if (PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                if (hit == null || !__instance.m_nview || !__instance.m_nview.IsOwner()) return true;
                if (HoardBaneArena.IsDormant(__instance))
                {
                    HoardBaneArena.Wake(__instance, hit.GetAttacker());
                    return false;
                }
                return !HoardBaneArena.IsShielded(__instance);
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    // In the game of whoever hits it.
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    internal static class HitPatch
    {
        private const string Feature = "Hoard-Bane Rite shield flash";

        private static void Prefix(Character __instance, HitData hit)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (hit != null && hit.GetAttacker() == Player.m_localPlayer && HoardBaneArena.IsShielded(__instance))
                    HoardBaneArena.Hit(__instance, hit.m_point);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // In every game, as the game draws a creature's level.
    [HarmonyPatch(typeof(LevelEffects), nameof(LevelEffects.SetupLevelVisualization))]
    internal static class EliteLookPatch
    {
        private const string Feature = "Hoard-Bane Rite elite look";

        // How much bigger than its level makes it: elites and minibosses (not their corpses: see CorpsePatch).
        private static float Scale(LevelEffects effects)
        {
            Character? character = CharacterOf(effects);
            return character ? HoardBaneArena.LookScale(character) : 1f;
        }

        private static Character? CharacterOf(LevelEffects effects) =>
            effects.m_character ? effects.m_character : effects.GetComponentInParent<Character>();

        // Beyond the levels the game draws: the highest it draws.
        private static void Prefix(LevelEffects __instance, ref int level)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                int drawn = __instance.m_levelSetups.Count;
                if (drawn > 0 && level - 1 > drawn && Scale(__instance) != 1f) level = drawn + 1;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        // Its size is on the whole creature, collider and all (see HoardBaneArena.Resize): set again, as the game may just
        // have set its scale for its level.
        private static void Postfix(LevelEffects __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                Character? character = CharacterOf(__instance);
                if (character) HoardBaneArena.Resize(character!);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // An elite's or miniboss's corpse looks as it did: the game makes it with no colors for levels beyond those it draws,
    // and none of the rite's size and colors.
    internal static class CorpsePatch
    {
        private const string Feature = "Hoard-Bane Rite corpses";

        // The game makes the corpse as the creature dies, in the game that owns it.
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class Dying
        {
            private static void Prefix(Character __instance)
            {
                if (!PatchGuard.IsDisabled(Feature)) HoardBaneArena.Dying = __instance;
            }

            private static void Finalizer() => HoardBaneArena.Dying = null;
        }

        [HarmonyPatch(typeof(Ragdoll), nameof(Ragdoll.Setup))]
        private static class Made
        {
            private static void Prefix(ref float hue, ref float saturation, ref float value, ref int level)
            {
                if (PatchGuard.IsDisabled(Feature) || !HoardBaneArena.Dying) return;
                try { HoardBaneArena.CorpseLevel(ref level, ref hue, ref saturation, ref value); }
                catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
            }

            private static void Postfix(Ragdoll __instance)
            {
                if (PatchGuard.IsDisabled(Feature) || !HoardBaneArena.Dying) return;
                try { HoardBaneArena.CorpseMade(__instance); }
                catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
            }
        }

        // In the other games, as it appears.
        [HarmonyPatch(typeof(Ragdoll), nameof(Ragdoll.Awake))]
        private static class Appears
        {
            private static void Postfix(Ragdoll __instance)
            {
                if (PatchGuard.IsDisabled(Feature)) return;
                try { HoardBaneArena.DressCorpse(__instance); }
                catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
            }
        }
    }

    // One side: none of what the rite brings is the enemy of the rest of it.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsEnemy), typeof(Character), typeof(Character))]
    internal static class AlliesPatch
    {
        private const string Feature = "Hoard-Bane Rite allies";

        private static bool Prefix(Character a, Character b, ref bool __result)
        {
            if (PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                if (IncursionArena.Kind != WeatherKind.HoardBane || a == b) return true;
                if (HoardBaneArena.RoleOf(a) == 0 || HoardBaneArena.RoleOf(b) == 0) return true;
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

    // Elites go by their own names, over them and wherever the game names them.
    [HarmonyPatch(typeof(Character), nameof(Character.GetHoverName))]
    internal static class NamePatch
    {
        private const string Feature = "Hoard-Bane Rite elite names";

        private static void Postfix(Character __instance, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (HoardBaneArena.NameOf(__instance) is { } name) __result = name;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // The game shows at most two stars over a creature (levels 2 and 3): elites above that get as many as they have, more
    // copies of the last star, as far apart.
    [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.UpdateHuds))]
    internal static class StarsPatch
    {
        private const string Feature = "Hoard-Bane Rite elite stars";
        private const string Copy = "TempestRite_Star";

        private static void Postfix(EnemyHud __instance)
        {
            if (PatchGuard.IsDisabled(Feature) || IncursionArena.Kind != WeatherKind.HoardBane) return;
            try
            {
                foreach (KeyValuePair<Character, EnemyHud.HudData> pair in __instance.m_huds)
                {
                    Character character = pair.Key;
                    EnemyHud.HudData hud = pair.Value;
                    if (!character || !hud.m_gui || !hud.m_gui.activeSelf || !hud.m_level3) continue;
                    int stars = character.GetLevel() - 1;
                    if (stars <= 2 || HoardBaneArena.RoleOf(character) != HoardBaneArena.EliteRole) continue;
                    hud.m_level3.gameObject.SetActive(true);
                    AddStars(hud.m_level3, stars - 2);
                }
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void AddStars(RectTransform two, int more)
        {
            List<RectTransform> own = [];
            foreach (Transform child in two)
                if (child is RectTransform rect && !child.name.StartsWith(Copy)) own.Add(rect);
            if (own.Count == 0) return;
            RectTransform last = own[own.Count - 1];
            Vector2 step = own.Count >= 2 ? last.anchoredPosition - own[own.Count - 2].anchoredPosition : new Vector2(last.rect.width, 0f);
            for (int i = 1; i <= more; i++)
            {
                string name = Copy + i;
                Transform? star = two.Find(name);
                if (!star)
                {
                    star = UnityEngine.Object.Instantiate(last.gameObject, two).transform;
                    star!.name = name;
                }
                ((RectTransform)star!).anchoredPosition = last.anchoredPosition + step * i;
                star.gameObject.SetActive(true);
            }
        }
    }
}
