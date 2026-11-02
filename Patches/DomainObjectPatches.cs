using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Patches;

// The Sentry-Sight, Taproot-Surge, Pitch-Seal, Primal-Hearth and Hearth-Kindle Rites: what they change on the objects
// they reach, and the line those objects' hover text gets while they do ("Sentry-Sight Rite (ballistas shoot every foe)").
internal static class DomainObjectPatches
{
    private const string HoverFeature = "Domain rite hover";

    // Adds the rite's line to an object's hover text while the rite reaches it.
    private static void Hover(WeatherKind kind, Component? thing, ref string result)
    {
        if (PatchGuard.IsDisabled(HoverFeature) || string.IsNullOrEmpty(result) || !thing) return;
        try
        {
            if (WeatherState.Reaches(kind, thing!.transform.position))
                result += "\n" + WeatherKinds.Colored(kind) + ObliteratorHints.Explain(kind);
        }
        catch (Exception exception) { PatchGuard.Fail(HoverFeature, exception); }
    }

    // ---- Sentry-Sight: ballistas ----

    // While a ballista under the rite looks for a target, the game's search is answered by the rite's (see SentryTargets).
    [HarmonyPatch(typeof(Turret), "UpdateTarget")]
    private static class SentryAimPatch
    {
        private const string Feature = "Sentry-Sight aim";

        private static void Prefix(Turret __instance)
        {
            SentryTargets.Aiming = null;
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (!__instance.m_targetEnemies || !SentryTargets.Reaches(__instance)) return;
                SentryTargets.Aiming = __instance;
                // The game looks only every 10 seconds with nobody within 40 m; with twice the sight that is too slow.
                if (__instance.m_updateTargetTimer > __instance.m_updateTargetIntervalNear)
                    __instance.m_updateTargetTimer = __instance.m_updateTargetIntervalNear;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Finalizer() => SentryTargets.Aiming = null;
    }

    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FindClosestCreature))]
    private static class SentrySearchPatch
    {
        private const string Feature = "Sentry-Sight search";

        private static bool Prefix(Transform me, Vector3 eyePoint, float hearRange, float viewRange, float viewAngle, bool alerted, bool mistVision,
            bool passiveAggresive, ref Character? __result)
        {
            Turret? aiming = SentryTargets.Aiming;
            if (!aiming || PatchGuard.IsDisabled(Feature) || me != aiming!.transform) return true;
            try
            {
                __result = SentryTargets.Pick(me, eyePoint, hearRange, viewRange * SentryTargets.SightMultiplier, viewAngle, alerted, mistVision,
                    passiveAggresive);
                return false;
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    // Ballistas under the rite turn toward where the bolt will meet the creature, and only shoot well aimed (see
    // SentryTargets.Turn).
    [HarmonyPatch(typeof(Turret), "UpdateTurretRotation")]
    private static class SentryTurnPatch
    {
        private const string Feature = "Sentry-Sight turning";

        private static bool Prefix(Turret __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                return !(__instance.m_targetEnemies && SentryTargets.Reaches(__instance) && SentryTargets.Turn(__instance));
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    // Ballistas under the rite hold fire with anything but foes in the way, and send the bolt straight on the aim with
    // no spread (see SentryTargets.BeforeShot).
    [HarmonyPatch(typeof(Turret), nameof(Turret.ShootProjectile))]
    private static class SentryShotPatch
    {
        private const string Feature = "Sentry-Sight shot";

        private static bool Prefix(Turret __instance, out SentryTargets.Shot? __state)
        {
            __state = null;
            if (PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                if (!__instance.m_targetEnemies || !SentryTargets.Reaches(__instance)) return true;
                return SentryTargets.BeforeShot(__instance, out __state);
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }

        // The eye goes back to where the ballista really points, and the bolts get their spread back.
        private static void Finalizer(Turret __instance, SentryTargets.Shot? __state) => SentryTargets.AfterShot(__instance, __state);
    }

    [HarmonyPatch(typeof(Turret), nameof(Turret.GetHoverText))]
    private static class SentryHover
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Turret __instance, ref string __result)
        {
            if (__instance.m_targetEnemies) Hover(WeatherKind.SentrySight, __instance, ref __result);
        }
    }

    // ---- Taproot-Surge: sap extractors and ancient roots (eitr refineries: see StationHoverPatches) ----

    [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.GetHoverText))]
    private static class SapHover
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SapCollector __instance, ref string __result) => Hover(WeatherKind.TaprootSurge, __instance, ref __result);
    }

    [HarmonyPatch(typeof(ResourceRoot), nameof(ResourceRoot.GetHoverText))]
    private static class RootHover
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ResourceRoot __instance, ref string __result) => Hover(WeatherKind.TaprootSurge, __instance, ref __result);
    }

    // ---- Pitch-Seal: items on the ground ----

    [HarmonyPatch(typeof(ItemDrop), "TimedDestruction")]
    private static class AnchorPatch
    {
        private const string Feature = "Pitch-Seal items";

        private static bool Prefix(ItemDrop __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return true;
            try { return !DropAnchor.Holds(__instance); }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText))]
    private static class AnchorHover
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ItemDrop __instance, ref string __result)
        {
            if (!__instance.m_autoDestroy || string.IsNullOrEmpty(__result) || !DropAnchor.Reaches(__instance)) return;
            Hover(WeatherKind.MantleAnchor, __instance, ref __result);
            if (PatchGuard.IsDisabled(HoverFeature)) return;
            try { __result += DropAnchor.DespawnLine(__instance); }
            catch (Exception exception) { PatchGuard.Fail(HoverFeature, exception); }
        }
    }

    // ---- Primal-Hearth: wisp fountains and eternal pyres ----

    // The shortest wait between two draws is Draw times shorter, so the extra tries (see LureSurge) can bring one.
    [HarmonyPatch(typeof(WispSpawner), "TrySpawn")]
    private static class LureDrawPatch
    {
        private const string Feature = "Primal-Hearth draw";

        private static void Prefix(WispSpawner __instance, out float __state)
        {
            __state = -1f;
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (!LureSurge.Reaches(__instance)) return;
                __state = __instance.m_spawnInterval;
                __instance.m_spawnInterval /= LureSurge.Draw;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Finalizer(WispSpawner __instance, float __state)
        {
            if (__state >= 0f && __instance) __instance.m_spawnInterval = __state;
        }
    }

    // Full only with more around it, on every game that asks (owners spawning, wisps choosing where to go, hover text).
    [HarmonyPatch(typeof(WispSpawner), "GetStatus")]
    private static class LureCapacityPatch
    {
        private const string Feature = "Primal-Hearth capacity";

        private static void Prefix(WispSpawner __instance, out int __state)
        {
            __state = -1;
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (!LureSurge.Reaches(__instance)) return;
                __state = __instance.m_maxSpawned;
                __instance.m_maxSpawned = Mathf.Max(1, Mathf.RoundToInt(__instance.m_maxSpawned * LureSurge.Capacity));
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Finalizer(WispSpawner __instance, int __state)
        {
            if (__state >= 0 && __instance) __instance.m_maxSpawned = __state;
        }
    }

    // A lured wisp or ember choosing where to go: lures the rite reaches take only their own kind (see LureSurge).
    [HarmonyPatch(typeof(LuredWisp), "UpdateTarget")]
    private static class LureSeekPatch
    {
        private const string Feature = "Primal-Hearth lure kinds";

        private static void Prefix(LuredWisp __instance, out string? __state)
        {
            __state = LureSurge.Seeking;
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (WeatherState.IsActive(WeatherKind.PrimalHearth)) LureSurge.Seeking = LureSurge.KindOf(__instance);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Finalizer(string? __state) => LureSurge.Seeking = __state;
    }

    [HarmonyPatch(typeof(WispSpawner), nameof(WispSpawner.GetBestSpawner))]
    private static class LureChoicePatch
    {
        private const string Feature = "Primal-Hearth lure kinds";

        private static bool Prefix(Vector3 p, float maxRange, ref WispSpawner? __result)
        {
            if (LureSurge.Seeking is not { } kind || PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                __result = LureSurge.BestFor(kind, p, maxRange);
                return false;
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    // A lure the rite reaches counts only its own kind toward being full.
    [HarmonyPatch(typeof(WispSpawner), "GetStatus")]
    private static class LureCountPatch
    {
        private const string Feature = "Primal-Hearth lure kinds";

        private static void Prefix(WispSpawner __instance, out (bool Set, string? Before) __state)
        {
            __state = (false, null);
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (!LureSurge.Reaches(__instance)) return;
                __state = (true, LureSurge.Counting);
                LureSurge.Counting = LureSurge.Draws(__instance);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Finalizer((bool Set, string? Before) __state)
        {
            if (__state.Set) LureSurge.Counting = __state.Before;
        }
    }

    [HarmonyPatch(typeof(LuredWisp), nameof(LuredWisp.GetWispsInArea))]
    private static class LureAroundPatch
    {
        private const string Feature = "Primal-Hearth lure kinds";

        private static bool Prefix(Vector3 p, float r, ref int __result)
        {
            if (LureSurge.Counting is not { } kind || PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                __result = LureSurge.Around(kind, p, r);
                return false;
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(WispSpawner), nameof(WispSpawner.GetHoverText))]
    private static class LureHover
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(WispSpawner __instance, ref string __result) => Hover(WeatherKind.PrimalHearth, __instance, ref __result);
    }

    // ---- Hearth-Kindle: resting by a fire ----

    // Resting counts toward Rested faster.
    [HarmonyPatch(typeof(SE_Cozy), nameof(SE_Cozy.UpdateStatusEffect))]
    private static class RestingPatch
    {
        private const string Feature = "Hearth-Kindle resting";

        private static void Prefix(SE_Cozy __instance, ref float dt)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (RestKindle.Reaches(__instance.m_character)) dt *= RestKindle.Resting;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Rested got while resting there lasts longer: worked out as the game does, then made longer.
    [HarmonyPatch(typeof(SE_Rested), "UpdateTTL")]
    private static class RestedPatch
    {
        private const string Feature = "Hearth-Kindle rested";

        private static void Postfix(SE_Rested __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (__instance.m_character is not Player player || !RestKindle.Reaches(player)
                    || !player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectResting)) return;
                float usual = __instance.m_baseTTL + (player.GetComfortLevel() - 1) * __instance.m_TTLPerComfortLevel;
                float longer = usual * RestKindle.Rested;
                if (longer <= __instance.m_ttl - __instance.m_time) return;
                __instance.m_ttl = longer;
                __instance.m_time = 0f;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Becoming Rested by a fire under the rite says so in place of the game's "Rested (Comfort 3)", with a puff of Rested symbols.
    // The game shows its message inside Setup, so the message patch below swaps it while this is set.
    [HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.Setup))]
    private static class RestedStartPatch
    {
        private const string Feature = "Hearth-Kindle rested message";
        internal static bool Kindled;

        private static void Prefix(Character character)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                Kindled = character is Player player && RestKindle.Reaches(player)
                    && player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectResting);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Postfix(Character character)
        {
            if (!Kindled) return;
            Kindled = false;
            try { DomainVisuals.RestedPuff(character); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            Kindled = false;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Message))]
    private static class RestedMessagePatch
    {
        private const string RestedStart = "$se_rested_start";

        private static void Prefix(Player __instance, ref string msg)
        {
            if (!RestedStartPatch.Kindled || msg == null || !msg.StartsWith(RestedStart, StringComparison.Ordinal)) return;
            msg = string.Format(Messages.HearthKindleRested, "$se_rested_comfort:" + __instance.GetComfortLevel());
        }
    }

    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.GetHoverText))]
    private static class FireHover
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Fireplace __instance, ref string __result)
        {
            if (PatchGuard.IsDisabled(HoverFeature) || !__instance.m_nview || !__instance.m_nview.IsValid()) return;
            if (RiteSpeedup.Takes(WeatherKind.HearthKindle, __instance.m_nview.GetZDO().GetPrefab()))
                Hover(WeatherKind.HearthKindle, __instance, ref __result);
        }
    }
}
