using HarmonyLib;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Patches;

// The incursion rite's obliterator can't be destroyed and no creature goes after it (see ObliteratorBastion).
internal static class ObliteratorBastionPatches
{
    // A hit, as the game takes it, but with nothing taken off: it shows a 0.
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.RPC_Damage))]
    private static class Hits
    {
        private static bool Prefix(WearNTear __instance, HitData hit)
        {
            ZNetView nview = __instance.m_nview;
            if (!nview || !nview.IsValid() || !nview.IsOwner() || !ObliteratorBastion.Guards(__instance)) return true;
            hit.ApplyResistance(__instance.m_damages, out HitData.DamageModifier modifier);
            DamageText.instance.ShowText(modifier, hit.m_point, 0f);
            return false;
        }
    }

    // Everything else that would take health off: losing its support (the structure below destroyed), burning, impacts.
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.ApplyDamage))]
    private static class Health
    {
        private static bool Prefix(WearNTear __instance, ref bool __result)
        {
            if (!ObliteratorBastion.Guards(__instance)) return true;
            __result = false;
            return false;
        }
    }

    // Torn down any other way (removed, or destroyed outright).
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy), typeof(HitData), typeof(bool))]
    private static class Destroyed
    {
        private static bool Prefix(WearNTear __instance) => !ObliteratorBastion.Guards(__instance);
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.IsPriorityTarget))]
    private static class PriorityTarget
    {
        private static void Postfix(Piece __instance, ref bool __result)
        {
            if (__result && ObliteratorBastion.Guards(__instance)) __result = false;
        }
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.IsRandomTarget))]
    private static class RandomTarget
    {
        private static void Postfix(Piece __instance, ref bool __result)
        {
            if (__result && ObliteratorBastion.Guards(__instance)) __result = false;
        }
    }

    // A creature already after it (from before the rite began) lets it go.
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    private static class LetGo
    {
        private static void Prefix(MonsterAI __instance)
        {
            if (__instance.m_targetStatic && ObliteratorBastion.Guards(__instance.m_targetStatic)) __instance.m_targetStatic = null;
        }
    }
}
