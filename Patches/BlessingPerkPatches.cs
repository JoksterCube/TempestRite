using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// The Scald-Drift, Tether-Lock and Strider-Girth Rites (see BlessingPerks).
internal static class BlessingPerkPatches
{
    // Scald-Drift: the boiling sea's and the lava's burns are smaller.
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    private static class ScaldDamagePatch
    {
        private const string Feature = "Scald-Drift damage";

        private static void Prefix(Character __instance, HitData hit)
        {
            if (PatchGuard.IsDisabled(Feature) || hit == null
                || hit.m_hitType is not (HitData.HitType.AshlandsOcean or HitData.HitType.AshlandsLava)) return;
            try
            {
                if (!BlessingPerks.Scalded(__instance)) return;
                // Heat hits carry plain damage, which the game's ApplyModifier leaves out, so it is scaled here too.
                float multiplier = hit.m_hitType == HitData.HitType.AshlandsLava ? BlessingPerks.LavaDamage : BlessingPerks.ScaldDamage;
                hit.m_damage.m_damage *= multiplier;
                hit.ApplyModifier(multiplier);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Scald-Drift: the boiling sea and the lava burn less often. They share one burn timer, which runs slower by
    // whichever would burn now: lava when its heat is past the burning point, else the sea.
    [HarmonyPatch(typeof(Character), "UpdateHeatDamage")]
    private static class ScaldTickPatch
    {
        private const string Feature = "Scald-Drift burn interval";

        private static void Prefix(Character __instance, ref float dt)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (BlessingPerks.Scalded(__instance))
                    dt /= __instance.m_lavaHeatLevel >= __instance.m_heatLevelFirstDamageThreshold
                        ? BlessingPerks.LavaTickSlowdown : BlessingPerks.ScaldTickSlowdown;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Tether-Lock: while the game pulls a harpooned creature, the line holds longer, costs less and pulls faster.
    [HarmonyPatch(typeof(SE_Harpooned), nameof(SE_Harpooned.UpdateStatusEffect))]
    private static class TetherPatch
    {
        private const string Feature = "Tether-Lock line";

        private static void Prefix(SE_Harpooned __instance, out Vector4? __state)
        {
            __state = null;
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (!BlessingPerks.Tethered(__instance.m_attacker)) return;
                __state = new Vector4(__instance.m_breakDistance, __instance.m_staminaDrain, __instance.m_pullSpeed, __instance.m_pullForce);
                __instance.m_breakDistance *= BlessingPerks.TetherStrength;
                __instance.m_staminaDrain *= BlessingPerks.TetherStamina;
                __instance.m_pullSpeed *= BlessingPerks.TetherReel;
                __instance.m_pullForce *= BlessingPerks.TetherReel;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Finalizer(SE_Harpooned __instance, Vector4? __state)
        {
            if (__state is not { } was || !__instance) return;
            __instance.m_breakDistance = was.x;
            __instance.m_staminaDrain = was.y;
            __instance.m_pullSpeed = was.z;
            __instance.m_pullForce = was.w;
        }
    }

    // Tether-Lock: a blessed player's held harpoon glows, on every game with the mod.
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.CustomUpdate))]
    private static class TetherGlowPatch
    {
        private static void Postfix(VisEquipment __instance) => TetherGlow.Follow(__instance);
    }

    // Tether-Lock: a blessed player's harpoon line glows a little too.
    [HarmonyPatch(typeof(LineConnect), "LateUpdate")]
    private static class TetherLinePatch
    {
        private static void Postfix(LineConnect __instance) => TetherGlow.FollowLine(__instance);
    }

    // Strider-Girth: riding, the attack keys make the mount attack. The game would get the rider off on an attack
    // press, so the presses are taken here first.
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    private static class StriderAttackPatch
    {
        private const string Feature = "Strider-Girth mount attacks";

        private static void Prefix(Player __instance, ref bool attack, ref bool attackHold, ref bool secondaryAttack,
            ref bool secondaryAttackHold)
        {
            if (PatchGuard.IsDisabled(Feature) || !(attack | attackHold | secondaryAttack | secondaryAttackHold)) return;
            try
            {
                if (__instance != Player.m_localPlayer || __instance.GetDoodadController() is not Sadle saddle || !saddle
                    || !BlessingPerks.Striding(saddle)) return;
                if (attack || secondaryAttack) BlessingPerks.MountAttack(saddle, secondary: !attack);
                attack = attackHold = secondaryAttack = secondaryAttackHold = false;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Strider-Girth: the mount gets its stamina back faster. After the game's own stamina update, when it gave some
    // back, more is added on the same terms.
    [HarmonyPatch(typeof(Sadle), "UpdateStamina")]
    private static class StriderRegenPatch
    {
        private const string Feature = "Strider-Girth stamina regen";

        private static void Postfix(Sadle __instance, float dt)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                Sadle saddle = __instance;
                Character character = saddle.m_character;
                if (!character || saddle.m_staminaRegenTimer > 0f || character.InAttack() || character.IsSwimming()
                    || !saddle.m_nview || !saddle.m_nview.IsOwner() || !BlessingPerks.Striding(saddle)) return;
                float stamina = saddle.GetStamina(), max = saddle.GetMaxStamina();
                if (stamina >= max) return;
                float regen = saddle.m_tambable && saddle.m_tambable.IsHungry() ? saddle.m_staminaRegenHungry : saddle.m_staminaRegen;
                float rate = regen + (1f - stamina / max) * regen;
                saddle.SetStamina(Mathf.Min(max, stamina + rate * dt * (BlessingPerks.StriderRegen - 1f)));
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}
