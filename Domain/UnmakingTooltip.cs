using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoksterCube.TempestRite.Domain;

// Under the tooltip of gear the Unmaking Rite unmakes in the open obliterator: what comes back, as the crafting panel
// shows a recipe's materials (each one's icon, name and amount, made from copies of that panel's slots).
// The game keeps a single tooltip object and destroys it when the pointer leaves, so the row is checked every frame.
internal static class UnmakingTooltip
{
    private const string RowName = "TempestRite_UnmakingRow";
    private const int Columns = 4;
    private const float Spacing = 4f;
    private const int Padding = 6;

    private static readonly FieldInfo? CurrentField = AccessTools.Field(typeof(UITooltip), "m_current");
    private static readonly FieldInfo? TooltipField = AccessTools.Field(typeof(UITooltip), "m_tooltip");

    private static readonly List<UnmakingReturns.Return> Returns = [];
    private static readonly List<UnmakingReturns.Return> Shown = [];
    private static GameObject? _row;

    internal static void Update(InventoryGui gui)
    {
        GameObject? tooltip = TooltipField?.GetValue(null) as GameObject;
        ItemDrop.ItemData? item = tooltip ? ObliteratorGrid.UnmadeItem(CurrentField?.GetValue(null) as UITooltip) : null;
        if (item == null || !UnmakingReturns.Returns(item, Returns) || Returns.Count == 0)
        {
            Remove();
            return;
        }
        Transform panel = tooltip!.transform.childCount > 0 ? tooltip.transform.GetChild(0) : tooltip.transform;
        if (_row && _row!.transform.parent == panel && Same()) return;
        Remove();
        _row = Build(gui, panel);
        Shown.Clear();
        Shown.AddRange(Returns);
    }

    private static bool Same()
    {
        if (Shown.Count != Returns.Count) return false;
        for (int i = 0; i < Shown.Count; i++)
            if (Shown[i].Item != Returns[i].Item || Shown[i].Amount != Returns[i].Amount) return false;
        return true;
    }

    private static void Remove()
    {
        if (_row) Object.Destroy(_row);
        _row = null;
        Shown.Clear();
    }

    private static GameObject? Build(InventoryGui gui, Transform panel)
    {
        if (gui.m_recipeRequirementList == null || gui.m_recipeRequirementList.Length == 0 || !gui.m_recipeRequirementList[0]) return null;
        GameObject template = gui.m_recipeRequirementList[0];
        Vector2 cell = ((RectTransform)template.transform).rect.size;
        if (cell.x <= 0f || cell.y <= 0f) cell = new Vector2(64f, 64f);

        GameObject row = new(RowName, typeof(RectTransform));
        RectTransform rect = (RectTransform)row.transform;
        rect.SetParent(panel, false);
        int columns = Mathf.Min(Returns.Count, Columns);
        int rows = (Returns.Count + Columns - 1) / Columns;
        Vector2 size = new(
            columns * cell.x + (columns - 1) * Spacing + 2 * Padding,
            rows * cell.y + (rows - 1) * Spacing + 2 * Padding);
        rect.sizeDelta = size;

        GridLayoutGroup grid = row.AddComponent<GridLayoutGroup>();
        grid.cellSize = cell;
        grid.spacing = new Vector2(Spacing, Spacing);
        grid.padding = new RectOffset(Padding, Padding, Padding, Padding);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment = TextAnchor.UpperLeft;
        // A layout group on the panel (which then grows around the row) sizes it from this.
        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = size.x;
        layout.minHeight = layout.preferredHeight = size.y;

        // Without one, the row hangs under the panel on a background like it.
        if (!panel.GetComponent<LayoutGroup>())
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            Image? background = panel.GetComponent<Image>();
            if (background)
            {
                Image copy = row.AddComponent<Image>();
                copy.sprite = background!.sprite;
                copy.type = background.type;
                copy.color = background.color;
                copy.raycastTarget = false;
            }
        }

        foreach (UnmakingReturns.Return part in Returns) Slot(template, rect, part);
        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)panel);
        return row;
    }

    private static void Slot(GameObject template, Transform row, UnmakingReturns.Return part)
    {
        GameObject slot = Object.Instantiate(template, row, false);
        slot.name = "TempestRite_UnmakingSlot";
        // A copy is only a picture: no tooltip of its own, nothing to hover or click.
        foreach (UITooltip tooltip in slot.GetComponentsInChildren<UITooltip>(true)) Object.Destroy(tooltip);
        foreach (Selectable selectable in slot.GetComponentsInChildren<Selectable>(true)) selectable.enabled = false;
        foreach (Graphic graphic in slot.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;

        ItemDrop.ItemData data = part.Item.m_itemData;
        Transform? icon = slot.transform.Find("res_icon");
        Transform? name = slot.transform.Find("res_name");
        Transform? amount = slot.transform.Find("res_amount");
        if (icon && icon!.GetComponent<Image>() is { } image)
        {
            icon.gameObject.SetActive(true);
            image.sprite = data.GetIcon();
            image.color = Color.white;
        }
        if (name && name!.GetComponent<TMP_Text>() is { } nameText)
        {
            name.gameObject.SetActive(true);
            nameText.text = Localization.instance.Localize(data.m_shared.m_name);
        }
        if (amount && amount!.GetComponent<TMP_Text>() is { } amountText)
        {
            amount.gameObject.SetActive(true);
            amountText.text = part.Amount.ToString();
            amountText.color = Color.white;
        }
        slot.SetActive(true);
    }
}
