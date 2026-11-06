using System.Collections.Generic;
using System.Linq;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// Which items call which weather, and which rites an obliterator's contents perform. A rite can have several key items
// (see KeyItemLists): any mix of them counts toward its Key Item Amount.
internal static class ObliteratorKeys
{
    // Key item prefab name -> the item's shared name ("$item_surtlingcore"), so items are matched even without a
    // drop prefab. Looked up again whenever the key item setting changes.
    private static readonly Dictionary<string, string> SharedNames = [];

    // Each turned on rite's key items, and each key item's rite, read again whenever a Key Item or Enabled setting
    // changes (also when the server sends its own, and on joining a server without the mod).
    private static Dictionary<WeatherKind, List<string>> _lists = [];
    private static readonly Dictionary<string, WeatherKind> Owners = [];
    private static readonly List<KeyItemLists.Conflict> Conflicts = [];
    private static readonly Dictionary<WeatherKind, (string text, bool enabled)> ReadSettings = [];
    private static readonly List<(WeatherKind kind, string text)> Ordered = [];
    private static readonly List<string> NoItems = [];

    internal static void ClearSession()
    {
        SharedNames.Clear();
        ReadSettings.Clear();
        _lists = [];
        Owners.Clear();
    }

    internal static bool IsObliteratorHatch(Container container, out Incinerator? incinerator)
    {
        incinerator = container ? container.GetComponentInParent<Incinerator>() : null;
        return incinerator && incinerator!.m_container == container;
    }

    // Reads the lists again when a setting changed. Asked every frame, so it only compares the settings' strings.
    private static void Refresh()
    {
        bool changed = false;
        foreach (WeatherKind kind in WeatherKinds.All)
        {
            WeatherConfig? config = PluginConfig.For(kind);
            if (config == null) continue;
            string text = config.KeyItem.Value;
            bool enabled = PluginConfig.IsEnabled(kind);
            if (ReadSettings.TryGetValue(kind, out (string text, bool enabled) before) && before.enabled == enabled &&
                ReferenceEquals(before.text, text)) continue;
            ReadSettings[kind] = (text, enabled);
            changed = true;
        }
        if (!changed) return;

        // In the order of the rites page, so of two rites listing an item the one listed first there keeps it.
        // Rites that are turned off claim nothing.
        Ordered.Clear();
        foreach (WeatherKind kind in WeatherKinds.All)
        {
            WeatherConfig? config = PluginConfig.For(kind);
            if (config != null && PluginConfig.IsEnabled(kind)) Ordered.Add((kind, config.KeyItem.Value));
        }
        _lists = KeyItemLists.Resolve(Ordered, Owners, Conflicts);
        foreach (KeyItemLists.Conflict conflict in Conflicts)
            Plugin.ModLogger.LogWarning($"Key item \"{conflict.Item}\" is listed by both {WeatherKinds.Name(conflict.Kept)} and " +
                $"{WeatherKinds.Name(conflict.Dropped)}. An item performs one rite only, so only {WeatherKinds.Name(conflict.Kept)} " +
                "keeps it. Check the Key Item settings.");
    }

    // The prefab names of a turned on rite's key items, in the order of its setting.
    internal static IReadOnlyList<string> Items(WeatherKind kind)
    {
        Refresh();
        return _lists.TryGetValue(kind, out List<string> items) ? items : NoItems;
    }

    // The rite's key items the local player has found, in order. A new list every call.
    internal static List<string> KnownItems(WeatherKind kind)
    {
        List<string> known = [];
        foreach (string item in Items(kind))
            if (IsItemKnown(item)) known.Add(item);
        return known;
    }

    // The weather an item calls when burnt, if it is a key item of a weather that is turned on.
    internal static WeatherKind KindOf(ItemDrop.ItemData item)
    {
        Refresh();
        if (item.m_dropPrefab) return Owners.TryGetValue(item.m_dropPrefab.name, out WeatherKind kind) ? kind : WeatherKind.None;
        foreach (KeyValuePair<string, WeatherKind> owner in Owners)
            if (Matches(item, owner.Key)) return owner.Value;
        return WeatherKind.None;
    }

    // Whether the item is this key item (by prefab name).
    internal static bool IsItem(ItemDrop.ItemData item, string prefabName) => Matches(item, prefabName);

    // One rite this inventory performs when burnt, and the item that leads it (its lowest, leftmost one).
    internal readonly struct Picked(WeatherKind kind, ItemDrop.ItemData item)
    {
        internal WeatherKind Kind { get; } = kind;
        internal ItemDrop.ItemData Item { get; } = item;
    }

    // The rites this inventory performs when burnt, in order (see KeyItemPicker): every rite with enough of its items in
    // (Key Item Amount, counted over all its stacks of any of its items), but those clashing with one before them. The
    // list is reused on the next call: copy it to keep it.
    internal static IReadOnlyList<Picked> PickAll(Inventory inventory)
    {
        PickedList.Clear();
        Candidates.Clear();
        PickedItems.Clear();
        CountAll(inventory);
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            WeatherKind kind = KindOf(item);
            if (kind == WeatherKind.None || Counted(kind) < Amount(kind)) continue;
            Candidates.Add((kind, item.m_gridPos.x, item.m_gridPos.y));
            PickedItems.Add(item);
        }
        foreach (int index in KeyItemPicker.PickAllIndexes(Candidates)) PickedList.Add(new Picked(Candidates[index].kind, PickedItems[index]));
        return PickedList;
    }

    private static readonly List<Picked> PickedList = [];

    internal static bool Contains(IReadOnlyList<Picked> picked, WeatherKind kind)
    {
        foreach (Picked p in picked)
            if (p.Kind == kind) return true;
        return false;
    }

    // The picked rite that keeps kind out, or None.
    internal static WeatherKind ClashingWith(IReadOnlyList<Picked> picked, WeatherKind kind)
    {
        foreach (Picked p in picked)
            if (p.Kind != kind && WeatherKinds.Clashes(p.Kind, kind)) return p.Kind;
        return WeatherKind.None;
    }

    // How many of each rite's items are in, from the last Pick or Count.
    private static readonly Dictionary<WeatherKind, int> Counts = [];

    // The different items counted so far, for rites that count each different one once (the Ragnarök Rite).
    private static readonly HashSet<(WeatherKind kind, string item)> Seen = [];

    private static void CountAll(Inventory inventory)
    {
        Counts.Clear();
        Seen.Clear();
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            WeatherKind kind = KindOf(item);
            if (kind == WeatherKind.None) continue;
            if (PluginConfig.For(kind)?.DistinctKeys != true) Counts[kind] = Counted(kind) + item.m_stack;
            else if (Seen.Add((kind, item.m_dropPrefab ? item.m_dropPrefab.name : item.m_shared.m_name))) Counts[kind] = Counted(kind) + 1;
        }
    }

    private static int Counted(WeatherKind kind) => Counts.TryGetValue(kind, out int count) ? count : 0;

    // How many of the rite's items (any of them) are in this inventory.
    internal static int Count(Inventory inventory, WeatherKind kind)
    {
        CountAll(inventory);
        return Counted(kind);
    }

    // Whether the rite counts each different item once, so it takes one of each (the Ragnarök Rite).
    internal static bool Distinct(WeatherKind kind) => PluginConfig.For(kind)?.DistinctKeys == true;

    // How many of its items a rite needs (Key Item Amount).
    internal static int Amount(WeatherKind kind) => System.Math.Max(1, PluginConfig.For(kind)?.KeyAmount.Value ?? 1);

    // "Ruby", "Ruby / Amber" when the player has found several of its items, or "2x Ooze" when it takes more than one.
    internal static string ItemLabel(WeatherKind kind)
    {
        int amount = Amount(kind);
        string name = ItemName(kind);
        if (amount > 1 && PluginConfig.For(kind)?.DistinctKeys == true) return string.Format(Settings.Constants.Messages.ItemsDifferent, amount, name);
        return amount > 1 ? string.Format(Settings.Constants.Messages.ItemAmount, amount, name) : name;
    }

    // Reused every call: hover text asks every frame.
    private static readonly List<(WeatherKind kind, int x, int y)> Candidates = [];
    private static readonly List<ItemDrop.ItemData> PickedItems = [];

    // The names of the rite's key items the player has found, in the player's language: "Surtling core / Ruby".
    internal static string ItemName(WeatherKind kind)
    {
        List<string> known = KnownItems(kind);
        if (known.Count == 0) known.AddRange(Items(kind));
        return string.Join(Settings.Constants.Messages.ItemSeparator, known.Select(p => ItemName(p)));
    }

    // A key item's name in the player's language: "Surtling core".
    internal static string ItemName(string prefabName)
    {
        string shared = SharedName(prefabName);
        return string.IsNullOrEmpty(shared) ? prefabName : Localize(shared);
    }

    // A key item's icon.
    internal static Sprite? ItemIcon(string prefabName)
    {
        if (!ObjectDB.instance) return null;
        GameObject? prefab = ObjectDB.instance.GetItemPrefab(prefabName);
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

    // Each rite that is turned on and one of whose items the local player has found: the only ones shown to them.
    internal static IEnumerable<WeatherKind> Known() => Enabled().Where(IsKnown);

    // Whether the local player has found enough of the rite's items to perform it (held them at least once), as the game
    // keeps it for recipes: one of them, or for a rite that takes different items, as many different ones as it takes
    // (fewer when its list is shorter). Until then the rite is never mentioned to them; burning the items still performs it.
    // The Ley-Survey Rite also waits for a Crimson, Ragnarök or Hoard-Bane Rite to be known (and on): until then there is no
    // fight to survey the ground for.
    internal static bool IsKnown(WeatherKind kind)
    {
        if (kind == WeatherKind.LeySurvey && !SurveyWanted()) return false;
        IReadOnlyList<string> items = Items(kind);
        int needed = Distinct(kind) ? System.Math.Min(Amount(kind), items.Count) : 1;
        int found = 0;
        foreach (string item in items)
            if (IsItemKnown(item) && ++found >= needed) return true;
        return false;
    }

    // The arena rites the Ley-Survey Rite surveys the ground for.
    internal static readonly WeatherKind[] Surveyed = [WeatherKind.Crimson, WeatherKind.Ragnarok, WeatherKind.HoardBane];

    private static bool SurveyWanted() => Surveyed.Any(kind => PluginConfig.IsEnabled(kind) && IsKnown(kind));

    // Whether the local player has found this item. A rite's other items stay hidden until they find those too.
    internal static bool IsItemKnown(string prefabName)
    {
        Player? player = Player.m_localPlayer;
        if (!player) return false;
        string shared = SharedName(prefabName);
        return !string.IsNullOrEmpty(shared) && player!.IsMaterialKnown(shared);
    }
}
