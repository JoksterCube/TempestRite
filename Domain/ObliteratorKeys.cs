using System.Collections.Generic;
using System.Linq;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// Which items call which weather, and which one an obliterator's contents call.
internal static class ObliteratorKeys
{
    // Key item prefab name -> the item's shared name ("$item_surtlingcore"), so items are matched even without a
    // drop prefab. Looked up again whenever the key item setting changes.
    private static readonly Dictionary<string, string> SharedNames = [];

    internal static void ClearSession() => SharedNames.Clear();

    internal static bool IsObliteratorHatch(Container container, out Incinerator? incinerator)
    {
        incinerator = container ? container.GetComponentInParent<Incinerator>() : null;
        return incinerator && incinerator!.m_container == container;
    }

    // The weather an item calls when burnt, if it is a key item of a weather that is turned on.
    internal static WeatherKind KindOf(ItemDrop.ItemData item)
    {
        foreach (WeatherKind kind in WeatherKinds.All)
        {
            WeatherConfig? config = PluginConfig.For(kind);
            if (config != null && config.Enabled.IsOn() && Matches(item, config.KeyItem.Value)) return kind;
        }
        return WeatherKind.None;
    }

    // The weather this inventory calls when burnt: from the highest key item (leftmost in its row). Also says whether there are key items
    // of more than one weather.
    // Only rites with enough of their item in (Key Item Amount, counted over all its stacks) can decide.
    internal static WeatherKind Pick(Inventory inventory, out ItemDrop.ItemData? keyItem, out bool several)
    {
        keyItem = null;
        several = false;
        Candidates.Clear();
        Items.Clear();
        CountAll(inventory);
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            WeatherKind kind = KindOf(item);
            if (kind == WeatherKind.None || Counted(kind) < Amount(kind)) continue;
            if (Candidates.Count > 0 && Candidates[0].kind != kind) several = true;
            Candidates.Add((kind, item.m_gridPos.x, item.m_gridPos.y));
            Items.Add(item);
        }

        int index = KeyItemPicker.PickIndex(Candidates);
        if (index < 0) return WeatherKind.None;
        keyItem = Items[index];
        return Candidates[index].kind;
    }

    // How many of each rite's item are in, from the last Pick or Count.
    private static readonly Dictionary<WeatherKind, int> Counts = [];

    private static void CountAll(Inventory inventory)
    {
        Counts.Clear();
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            WeatherKind kind = KindOf(item);
            if (kind != WeatherKind.None) Counts[kind] = Counted(kind) + item.m_stack;
        }
    }

    private static int Counted(WeatherKind kind) => Counts.TryGetValue(kind, out int count) ? count : 0;

    // How many of the rite's item are in this inventory.
    internal static int Count(Inventory inventory, WeatherKind kind)
    {
        CountAll(inventory);
        return Counted(kind);
    }

    // How many of its item a rite needs (Key Item Amount).
    internal static int Amount(WeatherKind kind) => System.Math.Max(1, PluginConfig.For(kind)?.KeyAmount.Value ?? 1);

    // "Ruby", or "2x Ooze" when it takes more than one.
    internal static string ItemLabel(WeatherKind kind)
    {
        int amount = Amount(kind);
        string name = ItemName(kind);
        return amount > 1 ? string.Format(Settings.Constants.Messages.ItemAmount, amount, name) : name;
    }

    // Reused every call: hover text asks every frame.
    private static readonly List<(WeatherKind kind, int x, int y)> Candidates = [];
    private static readonly List<ItemDrop.ItemData> Items = [];

    // The key item's name in the player's language: "Surtling core".
    internal static string ItemName(WeatherKind kind)
    {
        WeatherConfig? config = PluginConfig.For(kind);
        if (config == null) return string.Empty;
        string shared = SharedName(config.KeyItem.Value);
        return string.IsNullOrEmpty(shared) ? config.KeyItem.Value : Localize(shared);
    }

    // The key item's icon.
    internal static Sprite? ItemIcon(WeatherKind kind)
    {
        WeatherConfig? config = PluginConfig.For(kind);
        if (config == null || !ObjectDB.instance) return null;
        GameObject? prefab = ObjectDB.instance.GetItemPrefab(config.KeyItem.Value);
        ItemDrop? drop = prefab ? prefab!.GetComponent<ItemDrop>() : null;
        return drop ? drop!.m_itemData.GetIcon() : null;
    }

    internal static string ItemName(ItemDrop.ItemData item) => Localize(item.m_shared.m_name);

    private static string Localize(string token) =>
        Localization.instance != null ? Localization.instance.Localize(token) : token;

    private static bool Matches(ItemDrop.ItemData item, string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return false;
        if (item.m_dropPrefab) return item.m_dropPrefab.name == prefabName;
        string shared = SharedName(prefabName);
        return !string.IsNullOrEmpty(shared) && item.m_shared.m_name == shared;
    }

    private static string SharedName(string prefabName)
    {
        if (SharedNames.TryGetValue(prefabName, out string shared)) return shared;
        if (!ObjectDB.instance) return string.Empty;
        GameObject? prefab = ObjectDB.instance.GetItemPrefab(prefabName);
        ItemDrop? drop = prefab ? prefab!.GetComponent<ItemDrop>() : null;
        shared = drop ? drop!.m_itemData.m_shared.m_name : string.Empty;
        if (string.IsNullOrEmpty(shared))
            Plugin.ModLogger.LogWarning($"Key item \"{prefabName}\" is not an item the game knows. Check the Key Item settings.");
        SharedNames[prefabName] = shared;
        return shared;
    }

    // Each rite that is turned on.
    internal static IEnumerable<WeatherKind> Enabled() => WeatherKinds.All.Where(PluginConfig.IsEnabled);

    // Each rite that is turned on and whose item the local player has found: the only ones shown to them.
    internal static IEnumerable<WeatherKind> Known() => Enabled().Where(IsKnown);

    // Whether the local player has found the rite's item (held it at least once), as the game keeps it for recipes.
    // Until then the rite is never mentioned to them; burning the item still performs it.
    internal static bool IsKnown(WeatherKind kind)
    {
        Player? player = Player.m_localPlayer;
        WeatherConfig? config = PluginConfig.For(kind);
        if (!player || config == null) return false;
        string shared = SharedName(config.KeyItem.Value);
        return !string.IsNullOrEmpty(shared) && player!.IsMaterialKnown(shared);
    }
}
