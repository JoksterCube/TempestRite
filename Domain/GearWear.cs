using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Carapace Temper Rite: the game wears gear down in many places (swings, hits taken, blocks, building, tools). Rather
// than catching each one, every frame the local player's equipped gear is compared with the frame before, and part of
// what it lost is given back. Repairs and newly equipped gear are only noted. Item tooltips show what durability is
// worth under it.
internal static class GearWear
{
    private const string Feature = "Carapace Temper Rite";
    // Durability of each equipped item last frame and this frame; swapped every frame.
    private static Dictionary<ItemDrop.ItemData, float> Last = [];
    private static Dictionary<ItemDrop.ItemData, float> Now = [];

    internal static void ClearSession() => Last.Clear();

    // How many times longer gear lasts under the rite.
    internal static float Multiplier => Mathf.Max(1f, PluginConfig.DurabilityMultiplier.Value);

    // The tempered durability line for an item's tooltip, put under the game's durability line; the item's durability
    // is worth Multiplier times as much under the rite.
    internal static string AddTooltipLine(ItemDrop.ItemData item, int qualityLevel, string tooltip)
    {
        if (!item.m_shared.m_useDurability || !Blessings.IsActive(WeatherKind.CarapaceTemper)) return tooltip;
        const string durabilityLine = "\n$item_durability:";
        int start = tooltip.IndexOf(durabilityLine, StringComparison.Ordinal);
        if (start < 0) return tooltip;
        int end = tooltip.IndexOf('\n', start + 1);
        if (end < 0) end = tooltip.Length;
        float multiplier = Multiplier;
        string line = "\n" + string.Format(Constants.Messages.TemperedDurability,
            Mathf.RoundToInt((multiplier - 1f) * 100f),
            (item.m_durability * multiplier).ToString("0"), (item.GetMaxDurability(qualityLevel) * multiplier).ToString("0"));
        return tooltip.Insert(end, line);
    }

    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        Player? player = Player.m_localPlayer;
        if (!player || player!.IsDead() || !Blessings.IsActive(WeatherKind.CarapaceTemper))
        {
            Last.Clear();
            return;
        }
        try
        {
            // The gear wears 1/Multiplier of the usual, so it lasts Multiplier times as long: the rest is given back.
            float giveBack = 1f - 1f / Multiplier;
            Now.Clear();
            // The inventory's own list: GetEquippedItems would make a new list every frame.
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                if (!item.m_equipped || !item.m_shared.m_useDurability) continue;
                if (Last.TryGetValue(item, out float before) && item.m_durability < before)
                    item.m_durability = Mathf.Min(item.GetMaxDurability(), item.m_durability + (before - item.m_durability) * giveBack);
                Now[item] = item.m_durability;
            }
            (Last, Now) = (Now, Last);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
