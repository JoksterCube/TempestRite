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
// button at the top right, a line on how rites work, then one row per rite of the tab the player knows: the rite's icon
// (click: its Compendium entry), its name in its color over what it does, told with a little lore (up to two lines, cut
// short when too long, the full text on hover), and at the right the items to burn, each with the number a click puts in
// under it, like a recipe. Only the items the player has held show: a rite with several key items shows the others once
// they are found, those the player can put in first; past 3, there are 2 slots and a "+N" button that moves them all
// under the row (it turns "-"). Those 2 slots move on to another item the player holds once theirs is in or used up,
// while the rite still needs more; once it has all it needs they keep what was put in. Rites none of whose items the
// player has held yet are only counted. Clicking an item puts as many as its number shows from the player's inventory
// into the obliterator (that item first, the rite's others for the rest; for a rite taking different items, just that
// one) and the page stays open; it doesn't react when the player doesn't have them. A rite clashing with one already
// prepared isn't put in: a warning names that rite, whose row flashes.
// Items the player can't put in are dimmed (red tinted when clashing). The rites pulling the lever performs say so in green. Placed from the panel's own size every frame, so it fits
// when other mods make the panel bigger.
internal static class RitesPanel
{
    private const float TabGap = 8f;
    private const float ContentGap = 10f;
    private const float HintHeight = 44f;
    private const float RowHeight = 60f;
    private const float RowGap = 6f;
    private const float IconSize = 52f;
    private const float TextGap = 12f;
    private const float ItemGap = 6f;
    private const float FooterHeight = 44f;
    private const float TabIconInset = 4f;
    private const int MaxShownItems = 3;
    private const float NameSize = 20f;
    private const float NameHeight = 25f;
    private const float DetailSize = 14f;
    private const float CountSize = 16f;
    private static readonly Color HintColor = new(0.85f, 0.82f, 0.75f, 1f);
    // The number under an item: white, or red while the player lacks it, like a recipe's.
    private static readonly Color CountColor = Color.white;
    private static readonly Color CountLacking = new(1f, 0.42f, 0.38f, 1f);
    // "Any 3 different", in the gold of the tabs.
    private const string DifferentColor = "#d6993f";
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
    // One item slot of a row: its button, the number under it, and the item it shows (which the row's first slots
    // change while the rite still needs more).
    private sealed class Slot(string item)
    {
        internal string Item { get; set; } = item;
        internal UiParts.IconButton Button { get; set; } = null!;
        internal TMP_Text Count { get; set; } = null!;
    }

    // A rite's row on the page: its item slots, all its known items (in the setting's order), its name line with and
    // without the "ready" note, and the line under it with what the rite does.
    private sealed class RiteRow(WeatherKind kind, List<Slot> slots, List<string> items, TMP_Text text,
        string plain, string ready, string prepared, Image icon, TMP_Text? detail, UITooltip? detailTooltip)
    {
        internal WeatherKind Kind { get; } = kind;
        internal string Prepared { get; } = prepared;
        internal Image Icon { get; } = icon;
        internal List<Slot> Slots { get; } = slots;
        internal List<string> Items { get; } = items;
        internal TMP_Text Text { get; } = text;
        internal string Plain { get; } = plain;
        internal string Ready { get; } = ready;

        // The line's tooltip tells it in full, only while it is cut short.
        internal void Fit()
        {
            if (!detail || !detailTooltip) return;
            bool cut = detail!.isTextTruncated;
            if (detailTooltip!.enabled != cut) detailTooltip.enabled = cut;
        }
    }

    private static readonly List<RiteRow> RiteRows = [];
    // Rites whose items are shown under their row; all fold back each time the page opens.
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
        foreach (RiteRow row in RiteRows) row.Fit();
    }

    // Each row: "ready" while its rite is one of those pulling the lever performs; its buttons only react while the player
    // has what the obliterator still needs: for most rites any mix of its items, for one taking different items that
    // item, not yet in.
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
            bool distinct = ObliteratorKeys.Distinct(row.Kind);
            if (missing > 0) Arrange(row, inventory, obliterator, distinct);
            WeatherKind clash = missing > 0 ? ObliteratorKeys.ClashingWith(prepared, row.Kind) : WeatherKind.None;
            // Most rites: what is missing, in any mix of its items.
            int have = distinct ? 0 : RiteItems.Have(inventory, row.Kind);

            bool flash = flashing && row.Kind == _flash;
            string text = flash ? row.Prepared : on && ObliteratorKeys.Contains(prepared, row.Kind) ? row.Ready : row.Plain;
            if (row.Text.text != text) row.Text.text = text;
            // The flashing rite's icon pulses red.
            Color iconColor = flash ? Color.Lerp(Color.white, FlashColor, Mathf.PingPong(Time.unscaledTime * 4f, 1f)) : Color.white;
            if (row.Icon.color != iconColor) row.Icon.color = iconColor;

            foreach (Slot slot in row.Slots)
            {
                UiParts.IconButton button = slot.Button;
                // What a click puts in: one of a different item, else what is missing (the full amount once it is in).
                int need = distinct ? 1 : missing > 0 ? missing : ObliteratorKeys.Amount(row.Kind);
                bool inside = distinct && RiteItems.Contains(obliterator, slot.Item);
                // Only items the player holds react; for a mix, the total of all its items must also be enough.
                bool holds = Holds(inventory, slot.Item);
                int held = !holds ? 0 : distinct ? 1 : have;
                bool lacking = missing > 0 && !inside && held < need;
                bool canPut = missing > 0 && !inside && !lacking;
                bool clashing = canPut && clash != WeatherKind.None;

                Color itemColor = lacking ? ItemLacking : clashing ? ItemClashing : ItemUsable;
                if (button.Icon.color != itemColor) button.Icon.color = itemColor;
                string count = need.ToString();
                if (slot.Count.text != count) slot.Count.text = count;
                Color countColor = lacking ? CountLacking : CountColor;
                if (slot.Count.color != countColor) slot.Count.color = countColor;
                // No hover or press look when there is nothing to put in; the item still shows as it is. A clashing
                // one still reacts, to say why it isn't put in.
                Selectable.Transition transition = canPut ? Selectable.Transition.ColorTint : Selectable.Transition.None;
                if (button.Button.transition != transition) button.Button.transition = transition;

                string name = ObliteratorKeys.ItemName(slot.Item);
                if (lacking) button.SetTooltip(Messages.PanelItemMissing, $"{name}\n{string.Format(Messages.PanelItemNeeds, need, held)}");
                else if (clashing) button.SetTooltip(Messages.PanelItemClash, $"{name}\n{string.Format(Messages.PanelItemClashDetail, WeatherKinds.Colored(clash))}");
                else if (canPut) button.SetTooltip("", $"{name}\n{string.Format(Messages.PanelItemPut, need)}");
                else if (distinct && !inside) button.SetTooltip("", $"{name}\n{Messages.PanelItemEnough}");
                else button.SetTooltip("", $"{name}\n{Messages.PanelItemIn}");
            }
        }
    }

    private static readonly List<string> Arranged = [];

    // While the rite still needs more: the items the player can put in fill the slots from the left, in the setting's
    // order (cheapest first), so clicking the leftmost slot again and again puts them in from the cheapest up. Slots
    // left over keep the items they showed, in the order they had: one just put in moves right, behind those still
    // to put in.
    private static void Arrange(RiteRow row, Inventory inventory, Inventory obliterator, bool distinct)
    {
        int count = row.Slots.Count;
        Arranged.Clear();
        foreach (string item in row.Items)
        {
            if (Arranged.Count == count) break;
            if (Usable(item, inventory, obliterator, distinct)) Arranged.Add(item);
        }
        foreach (Slot slot in row.Slots)
        {
            if (Arranged.Count == count) break;
            if (!Arranged.Contains(slot.Item)) Arranged.Add(slot.Item);
        }
        for (int i = 0; i < count; i++)
        {
            Slot slot = row.Slots[i];
            if (slot.Item == Arranged[i]) continue;
            slot.Item = Arranged[i];
            slot.Button.Icon.sprite = ObliteratorKeys.ItemIcon(slot.Item);
        }
    }

    private static bool Usable(string item, Inventory inventory, Inventory obliterator, bool distinct) =>
        Holds(inventory, item) && !(distinct && RiteItems.Contains(obliterator, item));

    // Puts the items in (the clicked one first) and keeps the page open, so more rites can be prepared. Not when a rite
    // already prepared clashes with it: then a warning names that rite, whose row flashes.
    private static void PutIn(WeatherKind kind, string item)
    {
        if (!_container || !Player.m_localPlayer) return;
        Inventory obliterator = _container!.GetInventory();
        Inventory inventory = Player.m_localPlayer.GetInventory();
        int missing = RiteItems.Missing(obliterator, kind);
        bool distinct = ObliteratorKeys.Distinct(kind);
        if (missing <= 0) return;
        if (distinct ? !Usable(item, inventory, obliterator, true) : !Holds(inventory, item) || RiteItems.Have(inventory, kind) < missing) return;
        WeatherKind clash = ObliteratorKeys.ClashingWith(ObliteratorKeys.PickAll(obliterator), kind);
        if (clash != WeatherKind.None)
        {
            PlayerMessages.Warning(string.Format(Messages.PanelClashWarning, WeatherKinds.Colored(kind), WeatherKinds.Colored(clash)));
            _flash = clash;
            _flashUntil = Time.unscaledTime + FlashSeconds;
            return;
        }
        if (distinct) RiteItems.PutOne(inventory, obliterator, kind, item);
        else RiteItems.PutIn(inventory, obliterator, kind, item);
    }

    private static void Open(InventoryGui gui, Container container)
    {
        if (!gui.m_container) return;
        if (!_overlay) Create(gui);
        if (!_overlay) return;
        _container = container;
        _open = true;
        Expanded.Clear();
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
        // Clicking the icon opens the rite's entry in the Compendium.
        Button open = riteIcon.AddComponent<Button>();
        open.transition = Selectable.Transition.None;
        open.targetGraphic = image;
        open.onClick.AddListener(() => RiteCompendium.Open(gui, kind));
        UITooltip? openTooltip = UiParts.AddTooltip(gui, riteIcon, CardTooltip(gui));
        if (openTooltip)
        {
            openTooltip!.m_topic = WeatherKinds.Name(kind);
            openTooltip.m_text = Messages.PanelOpenCompendium;
        }

        // In the order of ShownOrder (and kept so, see Arrange). Up to MaxShownItems fit in the row; with more, 2 slots
        // and a "+N" button that moves them all under the row, leaving only a "-" in it, so the row's
        // text has more room.
        List<string> known = ObliteratorKeys.KnownItems(kind);
        List<string> items = ShownOrder(kind, known);
        bool overflow = items.Count > MaxShownItems;
        bool expanded = overflow && Expanded.Contains(kind);
        int inRow = expanded ? 0 : overflow ? MaxShownItems - 1 : items.Count;
        int slotCount = expanded ? 1 : overflow ? MaxShownItems : items.Count;
        List<Slot> slots = [];
        for (int i = 0; i < inRow; i++)
        {
            Slot slot = ItemSlot(gui, row.transform, kind, items[i]);
            Place(slot.Button.Rect, slotCount - 1 - i);
            slots.Add(slot);
        }
        if (overflow) MoreButton(gui, row.transform, kind, expanded, items.Count - inRow);
        float itemsWidth = slotCount > 0 ? slotCount * IconSize + (slotCount - 1) * ItemGap + TextGap : 0f;

        // Under the name: "Any 3 different" for a rite taking different items, and with the hints on, what it does.
        string detail = ObliteratorHints.Explaining ? $"<color={Messages.Faint}>{WeatherKinds.Flavour(kind)}</color>" : "";
        int amount = ObliteratorKeys.Amount(kind);
        if (ObliteratorKeys.Distinct(kind) && amount > 1)
            detail = $"<color={DifferentColor}>{string.Format(Messages.PanelAnyDifferent, amount)}</color>" + (detail.Length > 0 ? "  " + detail : "");

        TextMeshProUGUI text = UiParts.CreateText(gui, row.transform, "Text", NameSize, Color.white,
            detail.Length > 0 ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.Left);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        string plain = WeatherKinds.Colored(kind);
        string ready = $"{plain}  <size=75%>{Messages.PanelReady}</size>";
        string prepared = $"{plain}  <size=75%>{Messages.PanelPrepared}</size>";
        text.text = plain;
        RectTransform textRect = text.rectTransform;
        // With nothing under it, the name sits in the middle of the row.
        textRect.anchorMin = new Vector2(0f, detail.Length > 0 ? 1f : 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(IconSize + TextGap, detail.Length > 0 ? -NameHeight : 0f);
        textRect.offsetMax = new Vector2(-itemsWidth, 0f);

        TMP_Text? detailText = null;
        UITooltip? detailTooltip = null;
        if (detail.Length > 0) detailText = DetailText(gui, row.transform, kind, detail, itemsWidth, out detailTooltip);

        RiteRows.Add(new RiteRow(kind, slots, known, text, plain, ready, prepared, image, detailText, detailTooltip));

        if (expanded) MoreRow(gui, kind, items, inRow, slots);
    }

    // Under the rite's name: up to two lines, cut short with "..." when longer (its tooltip then tells it in full).
    private static TMP_Text DetailText(InventoryGui gui, Transform row, WeatherKind kind, string content, float itemsWidth, out UITooltip? tooltip)
    {
        TextMeshProUGUI text = UiParts.CreateText(gui, row, "Detail", DetailSize, Color.white, TextAlignmentOptions.TopLeft);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.lineSpacing = -12f;
        // Hovered for its tooltip.
        text.raycastTarget = true;
        text.text = content;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(IconSize + TextGap, 0f);
        rect.offsetMax = new Vector2(-itemsWidth, -NameHeight);

        tooltip = UiParts.AddTooltip(gui, text.gameObject, CardTooltip(gui));
        if (tooltip)
        {
            tooltip!.m_topic = WeatherKinds.Name(kind);
            tooltip.m_text = content;
            tooltip.enabled = false;
        }
        return text;
    }

    // The inventory item card's tooltip, on a dark background like the item cards (the craft button's has none, so
    // over the text it hovers it is hard to read). Null: the craft button's.
    private static GameObject? CardTooltip(InventoryGui gui) => UiParts.ItemTooltipPrefab(gui, out _);

    // Each in the setting's order: while the rite still needs more, the items the player can put in first, then the
    // others; once it has all it needs, those already in first, so the slots keep showing what was put in.
    private static List<string> ShownOrder(WeatherKind kind, List<string> items)
    {
        Inventory? inventory = Player.m_localPlayer ? Player.m_localPlayer.GetInventory() : null;
        Inventory? obliterator = _container ? _container!.GetInventory() : null;
        if (inventory == null || obliterator == null) return items;
        bool distinct = ObliteratorKeys.Distinct(kind);
        bool satisfied = RiteItems.Missing(obliterator, kind) <= 0;
        List<string> first = [], rest = [];
        foreach (string item in items)
        {
            bool front = satisfied ? RiteItems.Contains(obliterator, item) : Usable(item, inventory, obliterator, distinct);
            (front ? first : rest).Add(item);
        }
        first.AddRange(rest);
        return first;
    }

    private static bool Holds(Inventory inventory, string item)
    {
        foreach (ItemDrop.ItemData there in inventory.GetAllItems())
            if (ObliteratorKeys.IsItem(there, item)) return true;
        return false;
    }

    // An item's button with the number a click puts in at its bottom, like a recipe's; it puts in the item the slot
    // shows at the time.
    private static Slot ItemSlot(InventoryGui gui, Transform parent, WeatherKind kind, string item)
    {
        Slot slot = new(item);
        slot.Button = UiParts.CreateIconButton(gui, parent, $"Item_{item}", IconSize, () => PutIn(kind, slot.Item));
        slot.Button.Icon.sprite = ObliteratorKeys.ItemIcon(item);
        slot.Button.SetTooltip("", ObliteratorKeys.ItemName(item));

        TextMeshProUGUI count = UiParts.CreateText(gui, slot.Button.Button.transform, "Count", CountSize, CountColor, TextAlignmentOptions.Bottom);
        count.textWrappingMode = TextWrappingModes.NoWrap;
        count.fontStyle = FontStyles.Bold;
        count.outlineWidth = 0.25f;
        count.outlineColor = new Color32(0, 0, 0, 255);
        UiParts.Stretch(count.rectTransform, 2f);
        slot.Count = count;
        return slot;
    }

    // At the row's right end, fromRight slots in from it.
    private static void Place(RectTransform rect, int fromRight)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-fromRight * (IconSize + ItemGap), 0f);
    }

    // The rightmost slot: "+N" moves all the items under the row, "-" puts them back.
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

    // The rite's items from the given one on, under its row, wrapping onto more lines as needed.
    private static void MoreRow(InventoryGui gui, WeatherKind kind, List<string> items, int from,
        List<Slot> slots)
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
            slots.Add(ItemSlot(gui, more.transform, kind, items[i]));
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
