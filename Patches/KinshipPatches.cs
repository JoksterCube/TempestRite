using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// The kinship rites: nothing hurts a player while they get ready for and toast to the Skål Rite, roar to the Sky-Call Rite or flex to the
// Anvil-Spark Rite. Worked out in the player's own game, where every hit on them is applied.
[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
internal static class KinshipShieldPatch
{
    private const string Feature = "Kinship rites shield";

    private static bool Prefix(Character __instance)
    {
        if (PatchGuard.IsDisabled(Feature)) return true;
        try
        {
            if (__instance is not Player || !__instance.m_nview || !__instance.m_nview.IsOwner()) return true;
            return !KinshipRites.Shielded(__instance);
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return true;
        }
    }
}

// The kinship rites: while a player stops what they're doing for a rite's emote, and while they do it, their controls
// are ignored (no moving, attacking, blocking, jumping...), so nothing cuts the emote short.
[HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
internal static class KinshipHoldPatch
{
    private const string Feature = "Kinship rites hold";

    private static void Prefix(Player __instance, ref Vector3 movedir, ref bool attack, ref bool attackHold,
        ref bool secondaryAttack, ref bool secondaryAttackHold, ref bool block, ref bool blockHold, ref bool jump,
        ref bool crouch, ref bool run, ref bool autoRun, ref bool dodge)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            if (!KinshipRites.Holds(__instance)) return;
            movedir = Vector3.zero;
            attack = attackHold = secondaryAttack = secondaryAttackHold = false;
            block = blockHold = jump = crouch = run = autoRun = dodge = false;
            KinshipRites.Settle(__instance);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
