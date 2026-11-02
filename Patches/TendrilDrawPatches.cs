using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Patches;

// The Tendril-Draw Rite (see TendrilReturn).
internal static class TendrilDrawPatches
{
    private const string AmmoFeature = "Tendril-Draw arrows";
    private const string SpearFeature = "Tendril-Draw spears";

    // Set while the game takes ammo for a shot that keeps it, or drops a thrown weapon that comes back.
    private static bool _keepAmmo;
    private static bool _returning;

    // Arrows and bolts: the shot is rolled as the game takes its ammo.
    [HarmonyPatch(typeof(Attack), "UseAmmo")]
    private static class UseAmmoPatch
    {
        private static void Prefix(Attack __instance)
        {
            _keepAmmo = false;
            if (PatchGuard.IsDisabled(AmmoFeature)) return;
            try { _keepAmmo = TendrilReturn.SavesAmmo(__instance.m_character, __instance.m_weapon); }
            catch (Exception exception) { PatchGuard.Fail(AmmoFeature, exception); }
        }

        private static void Finalizer() => _keepAmmo = false;
    }

    // ...and the arrow or bolt isn't taken from the inventory. The shot still flies with it.
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), typeof(ItemDrop.ItemData), typeof(int))]
    private static class RemoveAmmoPatch
    {
        private static bool Prefix(ItemDrop.ItemData item, ref bool __result)
        {
            if (!_keepAmmo || item == null || item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Ammo) return true;
            _keepAmmo = false;
            __result = true;
            return false;
        }
    }

    // Thrown spears: the game drops them where they strike...
    [HarmonyPatch(typeof(Projectile), "SpawnOnHit")]
    private static class SpawnOnHitPatch
    {
        private static void Prefix(Projectile __instance)
        {
            _returning = false;
            if (PatchGuard.IsDisabled(SpearFeature)) return;
            try { _returning = TendrilReturn.Returns(__instance.m_owner, __instance.m_spawnItem); }
            catch (Exception exception) { PatchGuard.Fail(SpearFeature, exception); }
        }

        private static void Finalizer() => _returning = false;
    }

    // ...and the dropped spear starts back to its thrower.
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.DropItem))]
    private static class DropItemPatch
    {
        private static void Postfix(ItemDrop __result)
        {
            if (!_returning) return;
            _returning = false;
            try { TendrilReturn.Dropped(__result); }
            catch (Exception exception) { PatchGuard.Fail(SpearFeature, exception); }
        }
    }

    // Another player trying to pick up (or auto-pick up) a dropped item asks its owner to hand it over; the thrower
    // owns the returning spear, so the ask lands here and is refused while it flies back. Works on players without the
    // mod too, since the refusal happens in the thrower's game.
    [HarmonyPatch(typeof(ItemDrop), "RPC_RequestOwn")]
    private static class RequestOwnPatch
    {
        private static bool Prefix(ItemDrop __instance)
        {
            if (PatchGuard.IsDisabled(SpearFeature)) return true;
            try { return !TendrilReturn.IsReturning(__instance); }
            catch (Exception exception)
            {
                PatchGuard.Fail(SpearFeature, exception);
                return true;
            }
        }
    }
}
