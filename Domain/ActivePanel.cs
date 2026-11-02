using System.Collections.Generic;
using JoksterCube.ChestKit.Ui;
using JoksterCube.TempestRite.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The rites going on at the open obliterator (the Domain rites, Crimson, Ragnarök), shown in its panel in place of its
// items, opened from the hourglass on this mod's flap: a title where "Take all" is, a close button at the top right,
// then one row per rite: its icon, its name in its color with the time left, and what it does and who performed it.
// The times count down while it is open; it closes by itself when the last of them ends.
internal static class ActivePanel
{
    private const float ContentGap = 10f;
    private const float RowHeight = 58f;
    private const float RowGap = 6f;
    private const float IconSize = 52f;
    private const float TextGap = 12f;
    private static readonly Color DetailColor = new(0.85f, 0.82f, 0.75f, 1f);

    private sealed class Row(WeatherKind kind, TMP_Text text)
    {
        internal WeatherKind Kind { get; } = kind;
        internal TMP_Text Text { get; } = text;
    }

    private static RectTransform? _overlay;
    private static RectTransform? _content;
    private static RectTransform? _list;
    private static TextMeshProUGUI? _title;
    private static UiParts.TextButton? _close;
    private static readonly PanelOverlay.HiddenVanilla Hidden = new(Settings.Constants.Plugin.ModName + "Active");
    private static readonly List<Row> Rows = [];
    private static readonly List<WeatherKind> Active = [];
    private static Container? _container;
    private static bool _open;

    internal static bool IsOpen => _open;

    internal static void Toggle(InventoryGui gui, Container container)
    {
        if (_open) Close();
        else Open(gui, container);
    }

    // Every frame with the open obliterator, or null when none is open; closes when that changes, when no rite goes on
    // there any more, or when another page was opened over this one.
    internal static void Update(InventoryGui gui, Container? container)
    {
        if (!_open) return;
        if (!container || container != _container || !_overlay || PanelOverlay.IsCovered(_overlay!))
        {
            Close();
            return;
        }
        ObliteratorHints.ActiveAt(container!.transform.position, Active);
        if (Active.Count == 0)
        {
            Close();
            return;
        }
        Layout(gui);
        if (!SameRites()) BuildRows(gui);
        Refresh(container.transform.position);
    }

    private static bool SameRites()
    {
        if (Rows.Count != Active.Count) return false;
        for (int i = 0; i < Rows.Count; i++)
            if (Rows[i].Kind != Active[i]) return false;
        return true;
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
        ObliteratorHints.ActiveAt(container.transform.position, Active);
        BuildRows(gui);
        Refresh(container.transform.position);
    }

    private static void Close()
    {
        _open = false;
        _container = null;
        Hidden.Restore();
        if (_overlay) _overlay!.gameObject.SetActive(false);
    }

    // The title where "Take all" is, the close button at the top right with the same margin, the list below.
    private static void Layout(InventoryGui gui)
    {
        Rect panel = _overlay!.rect;
        PanelOverlay.HeaderRow header = PanelOverlay.Header(gui, _overlay);
        float closeLeft = PanelOverlay.PlaceClose(_close!, header, panel);
        float width = Mathf.Max(40f, closeLeft - 8f - header.Left);
        PanelOverlay.Put(_title!.rectTransform, new Vector2(header.Left + width / 2f, header.CenterY), new Vector2(width, header.Height), panel);

        Vector2 offsetMin = new(header.Margin, header.Margin);
        Vector2 offsetMax = new(-header.Margin, -(panel.yMax - (header.Top - header.Height) + ContentGap));
        if (_content!.offsetMin != offsetMin) _content.offsetMin = offsetMin;
        if (_content.offsetMax != offsetMax) _content.offsetMax = offsetMax;
    }

    private static void Create(InventoryGui gui)
    {
        float sensitivity = PanelOverlay.ScrollSensitivity(gui);
        _overlay = PanelOverlay.Create(gui, Settings.Constants.Plugin.ModName + "Active");

        _title = UiParts.CreateText(gui, _overlay, "Title", 22f, Color.white, TextAlignmentOptions.Left);
        _title.text = Messages.ActiveTooltip;
        RectTransform titleRect = _title.rectTransform;
        titleRect.anchorMin = titleRect.anchorMax = titleRect.pivot = new Vector2(0.5f, 0.5f);

        _close = PanelOverlay.CreateCloseButton(gui, _overlay, Close, Messages.CloseTooltip);
        _content = UiParts.Child(_overlay, "Content", 0f);
        _list = PanelOverlay.ScrollContent(gui, _content, sensitivity);
        VerticalLayoutGroup layout = _list.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = RowGap;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    // One row per rite going on, in Active's order.
    private static void BuildRows(InventoryGui gui)
    {
        foreach (Row row in Rows)
            if (row.Text) Object.Destroy(row.Text.transform.parent.gameObject);
        Rows.Clear();
        if (!_list) return;
        foreach (WeatherKind kind in Active) Rows.Add(MakeRow(gui, kind));
    }

    private static Row MakeRow(InventoryGui gui, WeatherKind kind)
    {
        GameObject row = new($"Active_{kind}", typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(_list!, false);
        LayoutElement element = row.GetComponent<LayoutElement>();
        element.minHeight = RowHeight;
        element.preferredHeight = RowHeight;

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

        TextMeshProUGUI text = UiParts.CreateText(gui, row.transform, "Text", 20f, Color.white, TextAlignmentOptions.Left);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(IconSize + TextGap, 0f);
        textRect.offsetMax = Vector2.zero;
        return new Row(kind, text);
    }

    // "Verdant Rite  24 min left" over "speeds up crops' growth, performed by Ragnar", counting down.
    private static void Refresh(Vector3 at)
    {
        foreach (Row row in Rows)
        {
            string left = TimeText.Duration(WeatherState.Remaining(row.Kind));
            string time = WeatherKinds.Untimed(row.Kind) ? Messages.ActiveUntimed
                : string.Format(WeatherState.Suspended(row.Kind, at) ? Messages.ActivePaused : Messages.ActiveLeft, left);
            string calledBy = WeatherState.CalledBy(row.Kind);
            string who = string.IsNullOrEmpty(calledBy) ? Messages.SomeoneName : calledBy;
            string plain = WeatherKinds.Plain(row.Kind);
            string what = plain.Length > 0 ? char.ToUpperInvariant(plain[0]) + plain.Substring(1) : plain;
            string detail = string.Format(Messages.ActiveDetail, what, who);
            string text = $"{WeatherKinds.Colored(row.Kind)}  <size=75%>{time}</size>\n<size=75%><color=#{ColorUtility.ToHtmlStringRGB(DetailColor)}>{detail}</color></size>";
            if (row.Text.text != text) row.Text.text = text;
        }
    }

    // The inventory screen is rebuilt for every session, so the page is made again next time.
    internal static void ClearSession()
    {
        Close();
        Rows.Clear();
        Active.Clear();
        if (_overlay) Object.Destroy(_overlay!.gameObject);
        _overlay = null;
        _content = null;
        _list = null;
        _title = null;
        _close = null;
    }
}
