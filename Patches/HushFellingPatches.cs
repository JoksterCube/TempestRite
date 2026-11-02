using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// The Hush-Felling Rite (see HushedToil).
internal static class HushFellingPatches
{
    private const string Feature = "Hush-Felling Rite";

    // Noise a blessed player would get is dropped while the game works out toil, or for their own quiet tool swings.
    [HarmonyPatch(typeof(Character), nameof(Character.AddNoise))]
    private static class NoisePatch
    {
        private static bool Prefix(Character __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                if (__instance is not Player player) return true;
                if (HushedToil.ToilDepth > 0 && HushedToil.Hushed(player)) return false;
                return player != Player.m_localPlayer || !HushedToil.Silent();
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    // A tree, log, rock, ore vein or bush being hit or broken, in the game that owns it: the noise it gives is toil.
    // Spawners (nests, body piles...) are left out.
    [HarmonyPatch]
    private static class ToilPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Destructible), nameof(Destructible.RPC_Damage));
            yield return AccessTools.Method(typeof(Destructible), nameof(Destructible.Destroy));
            yield return AccessTools.Method(typeof(TreeBase), nameof(TreeBase.RPC_Damage));
            yield return AccessTools.Method(typeof(TreeLog), nameof(TreeLog.RPC_Damage));
            yield return AccessTools.Method(typeof(MineRock), nameof(MineRock.RPC_Hit));
            yield return AccessTools.Method(typeof(MineRock5), nameof(MineRock5.RPC_Damage));
        }

        private static void Prefix(MonoBehaviour __instance, out bool __state)
        {
            __state = false;
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (__instance is Destructible && __instance.GetComponentInChildren<SpawnArea>(true)) return;
                __state = true;
                HushedToil.ToilDepth++;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static Exception? Finalizer(Exception? __exception, bool __state)
        {
            if (__state) HushedToil.ToilDepth = Math.Max(0, HushedToil.ToilDepth - 1);
            return __exception;
        }
    }

    // The local player hitting a tree, log, rock or bush (sent from their own game): they gather.
    [HarmonyPatch]
    private static class GatherHitPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Destructible), nameof(Destructible.Damage));
            yield return AccessTools.Method(typeof(TreeBase), nameof(TreeBase.Damage));
            yield return AccessTools.Method(typeof(TreeLog), nameof(TreeLog.Damage));
            yield return AccessTools.Method(typeof(MineRock), nameof(MineRock.Damage));
            yield return AccessTools.Method(typeof(MineRock5), nameof(MineRock5.Damage));
        }

        private static void Prefix(MonoBehaviour __instance, HitData hit)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (hit == null || !Player.m_localPlayer || hit.GetAttacker() != Player.m_localPlayer) return;
                if (__instance is Destructible && __instance.GetComponentInChildren<SpawnArea>(true)) return;
                HushedToil.Gathered();
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // The local player picking something (by hand or with a scythe): they gather.
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
    private static class PickPatch
    {
        private static void Postfix(Humanoid character, bool __result)
        {
            if (__result && character && character == Player.m_localPlayer) HushedToil.Gathered();
        }
    }

    // The local player hitting a creature, or being hit by one: a fight, not toil.
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    private static class HitCreaturePatch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                Player? me = Player.m_localPlayer;
                if (!me || hit == null || __instance == me) return;
                if (hit.GetAttacker() == me)
                {
                    HushedToil.HitCreature = true;
                    HushedToil.Fought();
                }
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    private static class HurtPatch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            if (__instance == Player.m_localPlayer && hit?.GetAttacker() is { } attacker && attacker != __instance) HushedToil.Fought();
        }
    }

    // The local player's attack: starting it (its swing noise), and working out what it hit (its hit noise).
    [HarmonyPatch(typeof(Attack), nameof(Attack.Update))]
    private static class SwingPatch
    {
        private static void Prefix(Attack __instance)
        {
            if (__instance.m_character == Player.m_localPlayer) HushedToil.Starting = __instance;
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            HushedToil.Starting = null;
            return __exception;
        }
    }

    [HarmonyPatch]
    private static class HitsPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Attack), nameof(Attack.DoMeleeAttack));
            yield return AccessTools.Method(typeof(Attack), nameof(Attack.DoAreaAttack));
        }

        private static void Prefix(Attack __instance, out Attack? __state)
        {
            __state = HushedToil.Hitting;
            if (__instance.m_character != Player.m_localPlayer) return;
            HushedToil.Hitting = __instance;
            HushedToil.HitCreature = false;
        }

        private static Exception? Finalizer(Exception? __exception, Attack? __state)
        {
            HushedToil.Hitting = __state;
            return __exception;
        }
    }

    // Wild creatures see a gathering blessed player only from part as far.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.CanSeeTarget),
        new[] { typeof(Transform), typeof(Vector3), typeof(float), typeof(float), typeof(bool), typeof(bool), typeof(Character) })]
    private static class SightPatch
    {
        private static void Prefix(Transform me, ref float viewRange, Character target)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (target is Player player && HushedToil.GatheringHushed(player) && HushedToil.Wild(me))
                    viewRange = HushedToil.SightRange(viewRange);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}
