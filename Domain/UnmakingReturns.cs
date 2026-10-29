using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// What the Unmaking Rite gives back for an item: part of what it cost to make and upgrade it. Only craftable weapons,
// tools, shields and armor are unmade. Used by the burn itself and by the open obliterator's frames and tooltips.
internal static class UnmakingReturns
{
    // Station materials count up to this level; upgrade altar materials (battle idols) only past it, if the setting is on.
    private const int MaxLevel = 4;

    private static readonly HashSet<ItemDrop.ItemData.ItemType> Gear =
    [
        ItemDrop.ItemData.ItemType.OneHandedWeapon, ItemDrop.ItemData.ItemType.TwoHandedWeapon, ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft,
        ItemDrop.ItemData.ItemType.Bow, ItemDrop.ItemData.ItemType.Shield, ItemDrop.ItemData.ItemType.Helmet, ItemDrop.ItemData.ItemType.Chest,
        ItemDrop.ItemData.ItemType.Legs, ItemDrop.ItemData.ItemType.Hands, ItemDrop.ItemData.ItemType.Shoulder, ItemDrop.ItemData.ItemType.Utility,
        ItemDrop.ItemData.ItemType.Tool, ItemDrop.ItemData.ItemType.Torch, ItemDrop.ItemData.ItemType.Trinket,
        ItemDrop.ItemData.ItemType.Attach_Atgeir
    ];

    internal readonly struct Return(ItemDrop item, int amount)
    {
        internal ItemDrop Item { get; } = item;
        internal int Amount { get; } = amount;
    }

    // Recipes by item name: the open obliterator asks every frame. Cleared when the recipes change.
    private static ObjectDB? _db;
    private static int _recipeCount = -1;
    private static readonly Dictionary<string, Recipe?> Recipes = [];

    // Whether the rite unmakes the item (it is taken out of the burn even if nothing comes back for it).
    internal static bool Unmakes(ItemDrop.ItemData item) => RecipeOf(item) != null;

    // What burning the item gives back, each material once, in recipe order. False when it isn't unmade.
    internal static bool Returns(ItemDrop.ItemData item, List<Return> into)
    {
        into.Clear();
        Recipe? recipe = RecipeOf(item);
        if (recipe == null) return false;
        float share = Mathf.Clamp01(PluginConfig.UnmakingReturn.Value);
        int level = Mathf.Max(item.m_quality, 1);
        bool altar = PluginConfig.UnmakingUpgradeMaterials.IsOn();
        foreach (Piece.Requirement requirement in recipe.m_resources)
        {
            if (!requirement.m_resItem) continue;
            // The game takes altar materials only at the upgrade altar (levels past 4), and station materials only elsewhere.
            if (requirement.m_upgraderResource && !altar) continue;
            int from = requirement.m_upgraderResource ? MaxLevel + 1 : 1;
            int to = requirement.m_upgraderResource ? level : Mathf.Min(level, MaxLevel);
            int cost = 0;
            for (int l = from; l <= to; l++) cost += requirement.GetAmount(l);
            if (cost <= 0) continue;
            // Rounded down, but every material that went in gives back at least 1.
            int amount = Mathf.Max(1, Mathf.FloorToInt(cost * share * item.m_stack));
            int at = into.FindIndex(r => r.Item == requirement.m_resItem);
            if (at >= 0) into[at] = new Return(requirement.m_resItem, into[at].Amount + amount);
            else into.Add(new Return(requirement.m_resItem, amount));
        }
        return true;
    }

    private static Recipe? RecipeOf(ItemDrop.ItemData item)
    {
        if (!Gear.Contains(item.m_shared.m_itemType) || item.m_shared.m_maxStackSize != 1) return null;
        ObjectDB db = ObjectDB.instance;
        if (!db) return null;
        if (db != _db || db.m_recipes.Count != _recipeCount)
        {
            _db = db;
            _recipeCount = db.m_recipes.Count;
            Recipes.Clear();
        }
        if (Recipes.TryGetValue(item.m_shared.m_name, out Recipe? known)) return known;
        Recipe? recipe = db.GetRecipe(item);
        if (!recipe || recipe!.m_resources == null || recipe.m_resources.Length == 0) recipe = null;
        Recipes[item.m_shared.m_name] = recipe;
        return recipe;
    }
}
