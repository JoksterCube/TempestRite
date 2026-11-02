using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Patches;

// The Hearth-Haul Rite (see HaulEase).
internal static class HearthHaulPatches
{
    // A player it reaches can carry more.
    [HarmonyPatch(typeof(Player), nameof(Player.GetMaxCarryWeight))]
    private static class CarryWeightPatch
    {
        private const string Feature = "Hearth-Haul carry weight";

        private static void Postfix(Player __instance, ref float __result)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (HaulEase.Reaches(__instance)) __result += HaulEase.Bonus * Game.m_carryWeightRate;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // The game's stamina update works out "carrying too much" once and uses it to stop stamina coming back and to use
    // some up while walking; it also slows the player, shows the Encumbered status and stops running, jumping and
    // dodging. Only the first two are changed, for a player it reaches: stamina comes back as usual and walking uses
    // none. The rest stays as the game has it.
    [HarmonyPatch(typeof(Player), "UpdateStats", typeof(float))]
    private static class StaminaPatch
    {
        private static readonly FieldInfo DrainField = AccessTools.Field(typeof(Player), nameof(Player.m_encumberedStaminaDrain));
        private static readonly MethodInfo IsEncumbered = AccessTools.Method(typeof(Character), nameof(Character.IsEncumbered));
        private static readonly MethodInfo Strains = AccessTools.Method(typeof(HaulEase), nameof(HaulEase.Strains));
        private static readonly MethodInfo Drain = AccessTools.Method(typeof(HaulEase), nameof(HaulEase.Drain));

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = [.. instructions];
            // "carrying too much" is kept in a local: where it is read and or-ed into the reasons stamina doesn't come
            // back, it is passed through Strains.
            int stored = -1;
            for (int i = 0; i + 1 < code.Count; i++)
                if (code[i].Calls(IsEncumbered) && code[i + 1].IsStloc())
                {
                    stored = i + 1;
                    break;
                }
            int regen = -1;
            if (stored >= 0)
                for (int i = stored + 1; i + 1 < code.Count; i++)
                    if (SameLocal(code[i], code[stored]) && code[i + 1].opcode == OpCodes.Or)
                    {
                        regen = i + 1;
                        break;
                    }
            int drain = code.FindIndex(c => c.LoadsField(DrainField));
            if (regen < 0 || drain < 0)
            {
                Plugin.ModLogger.LogWarning("Hearth-Haul Rite: the game's stamina update has changed; carrying too much under it costs stamina as usual.");
                return code;
            }
            // Later index first, so the earlier one stays where it was found.
            code.InsertRange(drain + 1, [new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Call, Drain)]);
            code.InsertRange(regen, [new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Call, Strains)]);
            return code;
        }

        private static bool SameLocal(CodeInstruction load, CodeInstruction store)
        {
            if (!load.IsLdloc()) return false;
            int? a = Index(load), b = Index(store);
            return a != null && a == b;
        }

        private static int? Index(CodeInstruction c)
        {
            if (c.opcode == OpCodes.Ldloc_0 || c.opcode == OpCodes.Stloc_0) return 0;
            if (c.opcode == OpCodes.Ldloc_1 || c.opcode == OpCodes.Stloc_1) return 1;
            if (c.opcode == OpCodes.Ldloc_2 || c.opcode == OpCodes.Stloc_2) return 2;
            if (c.opcode == OpCodes.Ldloc_3 || c.opcode == OpCodes.Stloc_3) return 3;
            return c.operand switch
            {
                LocalBuilder local => local.LocalIndex,
                int index => index,
                byte index => index,
                _ => null
            };
        }
    }
}
