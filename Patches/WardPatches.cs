using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// The Sovereign Ward Rite, in the game that owns each thing:
// - player-built pieces within the radius lose no health to anything (taking them down with the hammer isn't damage);
// - projectiles not shot by players or their tamed creatures crumble as soon as they are within it;
// - with Sanctuary Mode on, players within it can't be hurt.
internal static class WardPatches
{
    private const string Feature = "Sovereign Ward Rite";

    private static bool Warded(Vector3 position) =>
        !PatchGuard.IsDisabled(Feature) && WeatherState.Reaches(WeatherKind.SovereignWard, position);

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.ApplyDamage))]
    private static class Pieces
    {
        private static bool Prefix(WearNTear __instance, ref bool __result)
        {
            try
            {
                if (!Warded(__instance.transform.position)) return true;
                Piece? piece = __instance.GetComponent<Piece>();
                if (!piece || !piece!.IsPlacedByPlayer()) return true;
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

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.FixedUpdate))]
    private static class Projectiles
    {
        private static bool Prefix(Projectile __instance)
        {
            try
            {
                if (__instance.m_didHit || !__instance.m_nview || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()) return true;
                Character? owner = __instance.m_owner;
                if (owner && (owner is Player || owner!.IsTamed())) return true;
                Vector3 position = __instance.transform.position;
                if (!Warded(position)) return true;
                __instance.m_didHit = true;
                __instance.m_hitEffects.Create(position, Quaternion.identity);
                ZNetScene.instance.Destroy(__instance.gameObject);
                return false;
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    private static class SafePlayers
    {
        private static bool Prefix(Character __instance)
        {
            try
            {
                if (__instance is not Player || !PluginConfig.SovereignSanctuary.IsOn()) return true;
                if (!__instance.m_nview || !__instance.m_nview.IsOwner()) return true;
                return !Warded(__instance.transform.position);
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }
}

// The Unmaking Rite, in the game that owns the obliterator: right before it burns, each craftable weapon, tool, shield
// and piece of armor in it is taken out, and once the rest has burnt, part of what it cost to make and upgrade it is put
// in (what doesn't fit lands next to the obliterator).
// The burn is a coroutine: it is wrapped, so the work happens in the same step as the game's burn.
[HarmonyPatch(typeof(Incinerator), nameof(Incinerator.Incinerate))]
internal static class UnmakingPatch
{
    private const string Feature = "Unmaking Rite";
    // Upgrades past this level (from other mods) give back nothing more.
    private const int MaxLevel = 4;

    private static readonly HashSet<ItemDrop.ItemData.ItemType> Gear =
    [
        ItemDrop.ItemData.ItemType.OneHandedWeapon, ItemDrop.ItemData.ItemType.TwoHandedWeapon, ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft,
        ItemDrop.ItemData.ItemType.Bow, ItemDrop.ItemData.ItemType.Shield, ItemDrop.ItemData.ItemType.Helmet, ItemDrop.ItemData.ItemType.Chest,
        ItemDrop.ItemData.ItemType.Legs, ItemDrop.ItemData.ItemType.Hands, ItemDrop.ItemData.ItemType.Shoulder, ItemDrop.ItemData.ItemType.Utility,
        ItemDrop.ItemData.ItemType.Tool, ItemDrop.ItemData.ItemType.Torch, ItemDrop.ItemData.ItemType.Trinket,
        ItemDrop.ItemData.ItemType.Attach_Atgeir
    ];

    private static void Postfix(Incinerator __instance, ref IEnumerator __result) => __result = Wrap(__result, __instance);

    private static IEnumerator Wrap(IEnumerator burn, Incinerator incinerator)
    {
        // Step 0 pulls the lever and waits; step 1 burns.
        for (int step = 0; ; step++)
        {
            Dictionary<GameObject, int>? back = step == 1 ? Take(incinerator) : null;
            bool more = burn.MoveNext();
            if (back != null) Give(incinerator, back);
            if (!more) yield break;
            yield return burn.Current;
        }
    }

    private static Dictionary<GameObject, int>? Take(Incinerator incinerator)
    {
        if (PatchGuard.IsDisabled(Feature)) return null;
        try
        {
            // The same checks the game makes before burning.
            if (!incinerator.m_nview.IsValid() || !incinerator.m_nview.IsOwner() || incinerator.m_container.IsInUse()) return null;
            Inventory inventory = incinerator.m_container.GetInventory();
            if (!PluginConfig.IsEnabled(WeatherKind.Unmaking) || ObliteratorKeys.Pick(inventory, out _, out _) != WeatherKind.Unmaking) return null;

            float share = Mathf.Clamp01(PluginConfig.UnmakingPercent.Value / 100f);
            Dictionary<GameObject, int> back = [];
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
            {
                if (!Gear.Contains(item.m_shared.m_itemType) || item.m_shared.m_maxStackSize != 1) continue;
                Recipe? recipe = ObjectDB.instance.GetRecipe(item);
                if (!recipe || recipe!.m_resources == null || recipe.m_resources.Length == 0) continue;
                int level = Mathf.Clamp(item.m_quality, 1, MaxLevel);
                foreach (Piece.Requirement requirement in recipe.m_resources)
                {
                    if (!requirement.m_resItem) continue;
                    int cost = 0;
                    for (int l = 1; l <= level; l++) cost += requirement.GetAmount(l);
                    int amount = Mathf.FloorToInt(cost * share * item.m_stack);
                    if (amount <= 0) continue;
                    GameObject prefab = requirement.m_resItem.gameObject;
                    back[prefab] = (back.TryGetValue(prefab, out int had) ? had : 0) + amount;
                }
                inventory.RemoveItem(item);
            }
            return back;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return null;
        }
    }

    private static void Give(Incinerator incinerator, Dictionary<GameObject, int> back)
    {
        try
        {
            Inventory inventory = incinerator.m_container.GetInventory();
            Vector3 outside = incinerator.transform.position + incinerator.transform.forward * 1.5f + Vector3.up;
            foreach (KeyValuePair<GameObject, int> pair in back)
            {
                int stack = Mathf.Max(1, pair.Key.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize);
                for (int left = pair.Value; left > 0;)
                {
                    int chunk = Mathf.Min(left, stack);
                    left -= chunk;
                    if (inventory.CanAddItem(pair.Key, chunk) && inventory.AddItem(pair.Key, chunk)) continue;
                    GameObject dropped = UnityEngine.Object.Instantiate(pair.Key, outside + UnityEngine.Random.insideUnitSphere * 0.4f, Quaternion.identity);
                    dropped.GetComponent<ItemDrop>().SetStack(chunk);
                }
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
