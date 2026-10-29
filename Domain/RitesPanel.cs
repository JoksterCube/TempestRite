using System.Collections.Generic;
using JoksterCube.ChestKit.Ui;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// Short instructions, shown in the obliterator's panel itself in place of its items (opened from this mod's flap): tabs
// for the groups of rites (Atmospheric, Celestial, Biome Mastery, Domain, Blessings, Reckoning) where "Take all" is, each
// a gold emblem (white while selected) with the group's name as its tooltip, a close
// button at the top right, a line on how rites work, then one row per rite of the tab the player knows: the rite's icon,
// its name in its color and what it does, and at the right the items to burn. Only the items the player has held show:
// a rite with several key items shows the others once they are found, those the player holds first; past 4, the
// last slot is a "+N" button that shows the rest under the row. Rites none of whose items the player has held
// yet are only counted. Clicking an item puts as many as the rite needs from the player's inventory into the obliterator
// (that item first, the rite's others for the rest) and the page stays open; it doesn't react when the player doesn't
// have them. A rite clashing with one already prepared isn't put in: a warning names that rite, whose row flashes.
// Items the player can't put in are dimmed (red tinted when clashing). The rites pulling the lever performs say so in green. Placed from the panel's own size every frame, so it fits
// when other mods make the panel bigger.
internal static class RitesPanel
{
    private const float TabGap = 8f;
    private const float ContentGap = 10f;
    private const float HintHeight = 44f;
    private const float RowHeight = 58f;
    private const float RowGap = 6f;
    private const float IconSize = 52f;
    private const float TextGap = 12f;
    private const float ItemGap = 6f;
    private const float FooterHeight = 44f;
    private const float TabIconInset = 4f;
    private const int MaxShownItems = 4;
    private static readonly Color HintColor = new(0.85f, 0.82f, 0.75f, 1f);
    private static readonly Color TabIconColor = new(0.84f, 0.6f, 0.26f, 1f);
    private static readonly Color TabIconSelected = new(1f, 0.97f, 0.9f, 1f);
    // Item icons: as they are when they can be put in (or are in), dimmed when the player lacks them, red tinted when
    // their rite clashes with a prepared one.
    private static readonly Color ItemUsable = Color.white;
    private static readonly Color ItemLacking = new(0.45f, 0.45f, 0.45f, 0.7f);
    private static readonly Color ItemClashing = new(0.85f, 0.4f, 0.35f, 0.8f);
    private const float FlashSeconds = 2.5f;
    private static readonly Color FlashColor = new(1f, 0.45f, 0.35f, 1f);

    private static RectTransform? _overlay;
    private static RectTransform? _content;
    private static RectTransform? _list;
    private static UiParts.TextButton? _close;
    private static readonly List<UiParts.TextButton> Tabs = [];
    // Each tab's emblem, by the same index.
    private static readonly List<Image> TabIcons = [];
    // The tabs of groups with at least one rite turned on, in order.
    private static readonly List<UiParts.TextButton> ShownTabs = [];
    // The how-it-works line and the list under it, which takes its room when the line is hidden.
    private static GameObject? _hint;
    private static RectTransform? _scrollArea;
    private static readonly PanelOverlay.HiddenVanilla Hidden = new(Settings.Constants.Plugin.ModName);
    private static readonly List<GameObject> Rows = [];
    // A rite's row on the page: its item buttons (with each one's prefab name), and its text with and without the
    // "ready" note.
    private sealed class RiteRow(WeatherKind kind, List<(string item, UiParts.IconButton button)> buttons, TMP_Text text,
        string plain, string ready, string prepared, Image icon)
    {
        internal WeatherKind Kind { get; } = kind;
        internal string Prepared { get; } = prepared;
        internal Image Icon { get; } = icon;
        internal List<(string item, UiParts.IconButton button)> Buttons { get; } = buttons;
        internal TMP_Text Text { get; } = text;
        internal string Plain { get; } = plain;
        internal string Ready { get; } = ready;
    }

    private static readonly List<RiteRow> RiteRows = [];
    // Rites whose items that don't fit in the row are shown under it; kept between openings.
    private static readonly HashSet<WeatherKind> Expanded = [];

    // The prepared rite that kept a clicked one out, flashing until _flashUntil.
    private static WeatherKind _flash = WeatherKind.None;
    private static float _flashUntil;

    private static Container? _container;
    private static bool _open;
    // Kept between openings.
    private static int _tab;

    internal static bool IsOpen => _open;

    internal static void Toggle(InventoryGui gui, Container container)
    {
        if (_open) Close();
        else Open(gui, container);
    }

    // Every frame with the open obliterator, or null when none is open; closes when that changes, or when another
    // mod's page was opened over this one.
    internal static void Update(InventoryGui gui, Container? container)
    {
        if (!_open) return;
        if (!container || container != _container || !_overlay || PanelOverlay.IsCovered(_overlay!))
        {
            Close();
            return;
        }
        Layout(gui);
        RefreshItems();
    }

    // Each row: "ready" while its rite is one of those pulling the lever performs; its buttons only react while the player
    // has what the obliterator still needs, counting any mix of the rite's items.
    private static void RefreshItems()
    {
        if (!_container || !Player.m_localPlayer) return;
        Inventory obliterator = _container!.GetInventory();
        Inventory inventory = Player.m_localPlayer.GetInventory();
        // What is in, whether the rites are on or not: what clashes with it, and so what may still be put in.
        List<ObliteratorKeys.Picked> prepared = [.. ObliteratorKeys.PickAll(obliterator)];
        bool on = ObliteratorSwitch.IsOn(_container!);
        bool flashing = _flash != WeatherKind.None && Time.unscaledTime < _flashUntil;
        if (!flashing) _flash = WeatherKind.None;
        foreach (RiteRow row in RiteRows)
        {
            int missing = RiteItems.Missing(obliterator, row.Kind);
            int have = RiteItems.Have(inventory, row.Kind);
            WeatherKind clash = missing > 0 ? ObliteratorKeys.ClashingWith(prepared, row.Kind) : WeatherKind.None;
            bool clashing = clash != WeatherKind.None;
            bool lacking = missing > 0 && have < missing;
            bool canPut = missing > 0 && !lacking;

            bool flash = flashing && row.Kind == _flash;
            string text = flash ? row.Prepared : on && ObliteratorKeys.Contains(prepared, row.Kind) ? row.Ready : row.Plain;
            if (row.Text.text != text) row.Text.text = text;
            // The flashing rite's icon pulses red.
            Color iconColor = flash ? Color.Lerp(Color.white, FlashColor, Mathf.PingPong(Time.unscaledTime * 4f, 1f)) : Color.white;
            if (row.Icon.color != iconColor) row.Icon.color = iconColor;

            Color itemColor = lacking ? ItemLacking : clashing ? ItemClashing : ItemUsable;
            foreach ((string item, UiParts.IconButton button) in row.Buttons)
            {
                if (button.Icon.color != itemColor) button.Icon.color = itemColor;
                // No hover or press look when there is nothing to put in; the item still shows as it is. A clashing
                // one still reacts, to say why it isn't put in.
                Selectable.Transition transition = canPut ? Selectable.Transition.ColorTint : Selectable.Transition.None;
                if (button.Button.transition != transition) button.Button.transition = transition;

                string name = ObliteratorKeys.ItemName(item);
                if (lacking) button.SetTooltip(Messages.PanelItemMissing, $"{name}\n{string.Format(Messages.PanelItemNeeds, missing, have)}");
                else if (clashing) button.SetTooltip(Messages.PanelItemClash, $"{name}\n{string.Format(Messages.PanelItemClashDetail, WeatherKinds.Colored(clash))}");
                else if (canPut) button.SetTooltip("", $"{name}\n{string.Format(Messages.PanelItemPut, missing)}");
                else button.SetTooltip("", $"{name}\n{Messages.PanelItemIn}");
            }
        }
    }

    // Puts the items in (the clicked one first) and keeps the page open, so more rites can be prepared. Not when a rite
    // already prepared clashes with it: then a warning names that rite, whose row flashes.
    private static void PutIn(WeatherKind kind, string item)
    {
        if (!_container || !Player.m_localPlayer) return;
        Inventory obliterator = _container!.GetInventory();
        Inventory inventory = Player.m_localPlayer.GetInventory();
        int missing = RiteItems.Missing(obliterator, kind);
        if (missing <= 0 || RiteItems.Have(inventory, kind) < missing) return;
        WeatherKind clash = ObliteratorKeys.ClashingWith(ObliteratorKeys.PickAll(obliterator), kind);
        if (clash != WeatherKind.None)
        {
            PlayerMessages.Warning(string.Format(Messages.PanelClashWarning, WeatherKinds.Colored(kind), WeatherKinds.Colored(clash)));
            _flash = clash;
            _flashUntil = Time.unscaledTime + FlashSeconds;
            return;
        }
        RiteItems.PutIn(inventory, obliterator, kind, item);
    }

    private static void Open(InventoryGui gui, Container container)
    {
        if (!gui.m_container) return;
        if (!_overlay) Create(gui);
        if (!_overlay) return;
        _container = container;
        _open = true;
        Hidden.Hide(gui);
        PanelOverlay.BringToFront(_overlay!);
        _overlay!.gameObject.SetActive(true);
        Layout(gui);
        ShowHint(ObliteratorHints.Explaining);
        ShowTab(gui, _tab);
    }

    private static void Close()
    {
        _open = false;
        _container = null;
        Hidden.Restore();
        if (_overlay) _overlay!.gameObject.SetActive(false);
    }

    private static void ShowTab(InventoryGui gui, int tab)
    {
        _tab = Mathf.Clamp(tab, 0, WeatherKinds.Groups.Length - 1);
        for (int i = 0; i < TabIcons.Count; i++)
        {
            Color color = i == _tab ? TabIconSelected : TabIconColor;
            if (TabIcons[i].color != color) TabIcons[i].color = color;
        }
        BuildRows(gui, WeatherKinds.Groups[_tab]);
    }

    // A group's tab only shows while at least one of its rites is turned on. Checked every frame, so a change in the
    // settings (also the server's) shows at once; if the open tab goes away, the first one left is shown.
    private static void UpdateShownTabs(InventoryGui gui)
    {
        ShownTabs.Clear();
        int firstShown = -1;
        for (int i = 0; i < Tabs.Count; i++)
        {
            bool shown = GroupHasRites(WeatherKinds.Groups[i]);
            GameObject tab = Tabs[i].Button.gameObject;
            if (tab.activeSelf != shown) tab.SetActive(shown);
            if (!shown) continue;
            ShownTabs.Add(Tabs[i]);
            if (firstShown < 0) firstShown = i;
        }
        if (firstShown >= 0 && !GroupHasRites(WeatherKinds.Groups[_tab])) ShowTab(gui, firstShown);
    }

    private static bool GroupHasRites(RiteGroup group)
    {
        foreach (WeatherKind kind in ObliteratorKeys.Enabled())
            if (WeatherKinds.Group(kind) == group) return true;
        return false;
    }

    // Tabs where the "Take all" button is, the close button at the top right with the same margin, the rest below.
    private static void Layout(InventoryGui gui)
    {
        Rect panel = _overlay!.rect;
        PanelOverlay.HeaderRow header = PanelOverlay.Header(gui, _overlay);
        float closeLeft = PanelOverlay.PlaceClose(_close!, header, panel);
        UpdateShownTabs(gui);
        PanelOverlay.PlaceTabs(ShownTabs, header, closeLeft, panel, TabGap);

        Vector2 offsetMin = new(header.Margin, header.Margin);
        Vector2 offsetMax = new(-header.Margin, -(panel.yMax - (header.Top - header.Height) + ContentGap));
        if (_content!.offsetMin != offsetMin) _content.offsetMin = offsetMin;
        if (_content.offsetMax != offsetMax) _content.offsetMax = offsetMax;
    }

    private static void Create(InventoryGui gui)
    {
        float sensitivity = PanelOverlay.ScrollSensitivity(gui);
        _overlay = PanelOverlay.Create(gui, Settings.Constants.Plugin.ModName);

        Tabs.Clear();
        TabIcons.Clear();
        for (int i = 0; i < WeatherKinds.Groups.Length; i++)
        {
            int index = i;
            RiteGroup group = WeatherKinds.Groups[i];
            UiParts.TextButton tab = UiParts.CreateTextButton(gui, _overlay, $"Tab{group}", "", () => ShowTab(gui, index));
            if (tab.Label) tab.Label!.gameObject.SetActive(false);

            Image icon = UiParts.Child(tab.Button.transform, "Icon", TabIconInset).gameObject.AddComponent<Image>();
            icon.sprite = WeatherIcons.Tab(group);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = TabIconColor;

            UITooltip? tooltip = UiParts.AddTooltip(gui, tab.Button.gameObject);
            if (tooltip)
            {
                tooltip!.m_topic = "";
                tooltip.m_text = WeatherKinds.GroupName(group);
            }
            Tabs.Add(tab);
            TabIcons.Add(icon);
        }
        _close = PanelOverlay.CreateCloseButton(gui, _overlay, Close, Messages.CloseTooltip);

        _content = UiParts.Child(_overlay, "Content", 0f);

        TextMeshProUGUI hint = UiParts.CreateText(gui, _content, "Hint", 16f, HintColor, TextAlignmentOptions.TopLeft);
        hint.text = Messages.PanelHint;
        hint.enableAutoSizing = true;
        hint.fontSizeMin = 11f;
        hint.fontSizeMax = 16f;
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.anchoredPosition = Vector2.zero;
        hintRect.sizeDelta = new Vector2(0f, HintHeight);
        _hint = hint.gameObject;

        RectTransform scrollArea = UiParts.Child(_content, "Scroll", 0f);
        scrollArea.offsetMax = new Vector2(0f, -HintHeight);
        _scrollArea = scrollArea;
        _list = PanelOverlay.ScrollContent(gui, scrollArea, sensitivity);
        VerticalLayoutGroup layout = _list.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = RowGap;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void ShowHint(bool show)
    {
        if (_hint && _hint!.activeSelf != show) _hint.SetActive(show);
        if (_scrollArea) _scrollArea!.offsetMax = new Vector2(0f, show ? -HintHeight : 0f);
    }

    // One row per rite of the group the player knows, then a line counting the group's rites still to be found. Made again whenever a tab is shown, so newly found items show.
    private static void BuildRows(InventoryGui gui, RiteGroup group)
    {
        foreach (GameObject row in Rows)
            if (row) Object.Destroy(row);
        Rows.Clear();
        RiteRows.Clear();
        if (!_list) return;

        if (!WeatherSync.LocalPlayerMayChange()) Footer(gui, Messages.LeverNotAllowed);

        int known = 0, unknown = 0;
        foreach (WeatherKind kind in ObliteratorKeys.Enabled())
        {
            if (WeatherKinds.Group(kind) != group) continue;
            if (!ObliteratorKeys.IsKnown(kind))
            {
                unknown++;
                continue;
            }
            known++;
            Row(gui, kind);
        }

        if (unknown == 1) Footer(gui, Messages.PanelOneMoreToFind);
        else if (unknown > 1) Footer(gui, string.Format(Messages.PanelMoreToFind, unknown));
    }

    // The rite's icon, to its right the rite's name over the items' names and what the rite does, and at the right end
    // a slot for each of its items the player has found.
    private static void Row(InventoryGui gui, WeatherKind kind)
    {
        GameObject row = new($"Rite_{kind}", typeof(RectTransform));
        row.transform.SetParent(_list!, false);
        Height(row, RowHeight);
        Rows.Add(row);

        GameObject riteIcon = new("RiteIcon", typeof(RectTransform));
        riteIcon.transform.SetParent(row.transform, false);
        RectTransform iconRect = (RectTransform)riteIcon.transform;
        iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(IconSize, IconSize);
        Image image = riteIcon.AddComponent<Image>();
        image.sprite = WeatherIcons.For(kind);
        image.preserveAspect = true;
        image.raycastTarget = false;

        // The items the player holds first, then the others, each in the setting's order. At most MaxShownItems fit
        // in the row; with more, the last slot is a "+N" button that shows the rest under the row's text.
        List<string> items = ShownOrder(ObliteratorKeys.KnownItems(kind));
        bool overflow = items.Count > MaxShownItems;
        int inRow = overflow ? MaxShownItems - 1 : items.Count;
        int slots = overflow ? MaxShownItems : items.Count;
        List<(string item, UiParts.IconButton button)> buttons = [];
        for (int i = 0; i < inRow; i++)
        {
            UiParts.IconButton slot = ItemButton(gui, row.transform, kind, items[i]);
            Place(slot.Rect, slots - 1 - i);
            buttons.Add((items[i], slot));
        }
        bool expanded = overflow && Expanded.Contains(kind);
        if (overflow) MoreButton(gui, row.transform, kind, expanded, items.Count - inRow);
        float itemsWidth = slots > 0 ? slots * IconSize + (slots - 1) * ItemGap + TextGap : 0f;

        string itemName = ObliteratorKeys.ItemLabel(kind);

        TextMeshProUGUI text = UiParts.CreateText(gui, row.transform, "Text", 20f, Color.white, TextAlignmentOptions.Left);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        string detail = ObliteratorHints.Explaining
            ? string.Format(Messages.PanelRowDetail, itemName, WeatherKinds.Plain(kind))
            : string.Format(Messages.PanelRowItem, itemName);
        string plain = $"{WeatherKinds.Colored(kind)}\n<size=75%>{detail}</size>";
        string ready = $"{WeatherKinds.Colored(kind)}  <size=75%>{Messages.PanelReady}</size>\n<size=75%>{detail}</size>";
        string prepared = $"{WeatherKinds.Colored(kind)}  <size=75%>{Messages.PanelPrepared}</size>\n<size=75%>{detail}</size>";
        text.text = plain;
        RiteRows.Add(new RiteRow(kind, buttons, text, plain, ready, prepared, image));
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(IconSize + TextGap, 0f);
        textRect.offsetMax = new Vector2(-itemsWidth, 0f);

        if (expanded) MoreRow(gui, kind, items, inRow, buttons);
    }

    // Held items first (keeping the setting's order among them), then the rest in the setting's order.
    private static List<string> ShownOrder(List<string> items)
    {
        Inventory? inventory = Player.m_localPlayer ? Player.m_localPlayer.GetInventory() : null;
        if (inventory == null) return items;
        List<string> held = [], rest = [];
        foreach (string item in items) (Holds(inventory, item) ? held : rest).Add(item);
        held.AddRange(rest);
        return held;
    }

    private static bool Holds(Inventory inventory, string item)
    {
        foreach (ItemDrop.ItemData there in inventory.GetAllItems())
            if (ObliteratorKeys.IsItem(there, item)) return true;
        return false;
    }

    private static UiParts.IconButton ItemButton(InventoryGui gui, Transform parent, WeatherKind kind, string item)
    {
        UiParts.IconButton slot = UiParts.CreateIconButton(gui, parent, $"Item_{item}", IconSize, () => PutIn(kind, item));
        slot.Icon.sprite = ObliteratorKeys.ItemIcon(item);
        slot.SetTooltip("", ObliteratorKeys.ItemName(item));
        return slot;
    }

    // At the row's right end, fromRight slots in from it.
    private static void Place(RectTransform rect, int fromRight)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-fromRight * (IconSize + ItemGap), 0f);
    }

    // The rightmost slot: "+N" shows the items that don't fit under the row, "-" hides them again.
    private static void MoreButton(InventoryGui gui, Transform parent, WeatherKind kind, bool expanded, int hidden)
    {
        UiParts.IconButton more = UiParts.CreateIconButton(gui, parent, "More", IconSize, () =>
        {
            if (!Expanded.Remove(kind)) Expanded.Add(kind);
            BuildRows(gui, WeatherKinds.Groups[_tab]);
        });
        more.Icon.gameObject.SetActive(false);
        more.SetTooltip("", expanded ? Messages.PanelFewerItems : string.Format(Messages.PanelMoreItems, hidden));
        Place(more.Rect, 0);

        TextMeshProUGUI label = UiParts.CreateText(gui, more.Button.transform, "Label", 22f, TabIconColor, TextAlignmentOptions.Center);
        label.text = expanded ? "-" : $"+{hidden}";
        label.raycastTarget = false;
        UiParts.Stretch(label.rectTransform, 0f);
    }

    // The items that don't fit in the rite's row, under its text, wrapping onto more lines as needed.
    private static void MoreRow(InventoryGui gui, WeatherKind kind, List<string> items, int from,
        List<(string item, UiParts.IconButton button)> buttons)
    {
        GameObject more = new($"Rite_{kind}_More", typeof(RectTransform));
        more.transform.SetParent(_list!, false);
        Rows.Add(more);
        GridLayoutGroup grid = more.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset((int)(IconSize + TextGap), 0, 0, 0);
        grid.cellSize = new Vector2(IconSize, IconSize);
        grid.spacing = new Vector2(ItemGap, ItemGap);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        for (int i = from; i < items.Count; i++)
            buttons.Add((items[i], ItemButton(gui, more.transform, kind, items[i])));
    }

    private static void Footer(InventoryGui gui, string message)
    {
        TextMeshProUGUI text = UiParts.CreateText(gui, _list!, "Note", 16f, HintColor, TextAlignmentOptions.Left);
        text.text = message;
        Height(text.gameObject, FooterHeight);
        Rows.Add(text.gameObject);
    }

    private static void Height(GameObject gameObject, float height)
    {
        if (!gameObject.TryGetComponent(out LayoutElement element)) element = gameObject.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
    }

    // The inventory screen is rebuilt for every session, so the page is made again next time.
    internal static void ClearSession()
    {
        Close();
        Rows.Clear();
        RiteRows.Clear();
        Expanded.Clear();
        _flash = WeatherKind.None;
        Tabs.Clear();
        TabIcons.Clear();
        ShownTabs.Clear();
        if (_overlay) Object.Destroy(_overlay!.gameObject);
        _overlay = null;
        _content = null;
        _list = null;
        _close = null;
        _hint = null;
        _scrollArea = null;
    }
}
