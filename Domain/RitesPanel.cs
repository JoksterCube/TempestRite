using System.Collections.Generic;
using JoksterCube.ChestKit.Ui;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// Short instructions, shown in the obliterator's panel itself in place of its items (opened from this mod's flap): tabs
// for the groups of rites (Atmospheric, Celestial, Biome Mastery, Domain, Incursions) where "Take all" is, a close
// button at the top right, a line on how rites work, then one row per rite of the tab the player knows: the item to
// burn, the rite's name in its color, and what it does. Rites whose items the player hasn't held yet are only counted.
// Clicking an item puts as many as the rite needs from the player's inventory into the obliterator and closes the page;
// it doesn't react when the player doesn't have them. The rite pulling the lever performs says so in green. Placed from the panel's own size
// every frame, so it fits when other mods make the panel bigger.
internal static class RitesPanel
{
    private const float TabGap = 8f;
    private const float ContentGap = 10f;
    private const float HintHeight = 44f;
    private const float RowHeight = 58f;
    private const float RowGap = 6f;
    private const float IconSize = 52f;
    private const float TextGap = 12f;
    private const float FooterHeight = 44f;
    private static readonly Color HintColor = new(0.85f, 0.82f, 0.75f, 1f);

    private static RectTransform? _overlay;
    private static RectTransform? _content;
    private static RectTransform? _list;
    private static UiParts.TextButton? _close;
    private static readonly List<UiParts.TextButton> Tabs = [];
    // The tabs of groups with at least one rite turned on, in order.
    private static readonly List<UiParts.TextButton> ShownTabs = [];
    // The how-it-works line and the list under it, which takes its room when the line is hidden.
    private static GameObject? _hint;
    private static RectTransform? _scrollArea;
    private static readonly PanelOverlay.HiddenVanilla Hidden = new(Settings.Constants.Plugin.ModName);
    private static readonly List<GameObject> Rows = [];
    // A rite's row on the page: its item button, and its text with and without the "ready" note.
    private sealed class ItemButton(WeatherKind kind, UiParts.IconButton button, TMP_Text text, string plain, string ready)
    {
        internal WeatherKind Kind { get; } = kind;
        internal UiParts.IconButton Button { get; } = button;
        internal TMP_Text Text { get; } = text;
        internal string Plain { get; } = plain;
        internal string Ready { get; } = ready;
    }

    private static readonly List<ItemButton> ItemButtons = [];

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

    // Each row: "ready" while its rite is the one pulling the lever performs; its button only reacts while the player has
    // what the obliterator still needs.
    private static void RefreshItems()
    {
        if (!_container || !Player.m_localPlayer) return;
        Inventory obliterator = _container!.GetInventory();
        Inventory inventory = Player.m_localPlayer.GetInventory();
        WeatherKind deciding = ObliteratorKeys.Pick(obliterator, out _, out _);
        foreach (ItemButton row in ItemButtons)
        {
            UiParts.IconButton button = row.Button;
            int missing = RiteItems.Missing(obliterator, row.Kind);
            int have = RiteItems.Have(inventory, row.Kind);
            bool canPut = missing > 0 && have >= missing;
            bool lacking = missing > 0 && have < missing;

            // No hover or press look when there is nothing to put in; the item still shows as it is.
            Selectable.Transition transition = canPut ? Selectable.Transition.ColorTint : Selectable.Transition.None;
            if (button.Button.transition != transition) button.Button.transition = transition;

            string text = row.Kind == deciding ? row.Ready : row.Plain;
            if (row.Text.text != text) row.Text.text = text;

            string name = ObliteratorKeys.ItemName(row.Kind);
            if (lacking) button.SetTooltip(Messages.PanelItemMissing, $"{name}\n{string.Format(Messages.PanelItemNeeds, missing, have)}");
            else if (canPut) button.SetTooltip("", $"{name}\n{string.Format(Messages.PanelItemPut, missing)}");
            else button.SetTooltip("", $"{name}\n{Messages.PanelItemIn}");
        }
    }

    // Puts the items in, then closes the page so the obliterator shows with them.
    private static void PutIn(WeatherKind kind)
    {
        if (!_container || !Player.m_localPlayer) return;
        if (RiteItems.PutIn(Player.m_localPlayer.GetInventory(), _container!.GetInventory(), kind)) Close();
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
        PanelOverlay.SelectTab(Tabs, _tab);
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
        for (int i = 0; i < WeatherKinds.Groups.Length; i++)
        {
            int index = i;
            UiParts.TextButton tab = UiParts.CreateTextButton(gui, _overlay, $"Tab{WeatherKinds.Groups[i]}",
                WeatherKinds.GroupName(WeatherKinds.Groups[i]), () => ShowTab(gui, index));
            // Five tabs share the row: long names get smaller rather than cut.
            if (tab.Label)
            {
                float size = tab.Label!.fontSize;
                tab.Label.enableAutoSizing = true;
                tab.Label.fontSizeMin = Mathf.Min(11f, size);
                tab.Label.fontSizeMax = size;
            }
            Tabs.Add(tab);
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
        ItemButtons.Clear();
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

    // The item's icon in a slot, and to its right the rite's name over the item's name and what the rite does.
    private static void Row(InventoryGui gui, WeatherKind kind)
    {
        GameObject row = new($"Rite_{kind}", typeof(RectTransform));
        row.transform.SetParent(_list!, false);
        Height(row, RowHeight);
        Rows.Add(row);

        string itemName = ObliteratorKeys.ItemLabel(kind);
        UiParts.IconButton slot = UiParts.CreateIconButton(gui, row.transform, "Item", IconSize, () => PutIn(kind));
        RectTransform slotRect = slot.Rect;
        slotRect.anchorMin = slotRect.anchorMax = slotRect.pivot = new Vector2(0f, 0.5f);
        slotRect.anchoredPosition = Vector2.zero;
        slot.Icon.sprite = ObliteratorKeys.ItemIcon(kind);
        slot.SetTooltip("", ObliteratorKeys.ItemName(kind));

        TextMeshProUGUI text = UiParts.CreateText(gui, row.transform, "Text", 20f, Color.white, TextAlignmentOptions.Left);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        string detail = ObliteratorHints.Explaining
            ? string.Format(Messages.PanelRowDetail, itemName, WeatherKinds.Plain(kind))
            : string.Format(Messages.PanelRowItem, itemName);
        string plain = $"{WeatherKinds.Colored(kind)}\n<size=75%>{detail}</size>";
        string ready = $"{WeatherKinds.Colored(kind)}  <size=75%>{Messages.PanelReady}</size>\n<size=75%>{detail}</size>";
        text.text = plain;
        ItemButtons.Add(new ItemButton(kind, slot, text, plain, ready));
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(IconSize + TextGap, 0f);
        textRect.offsetMax = Vector2.zero;
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
        ItemButtons.Clear();
        Tabs.Clear();
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
