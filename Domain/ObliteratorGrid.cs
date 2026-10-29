using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace JoksterCube.TempestRite.Domain;

// In the open obliterator, the items of each rite the burn performs get a frame in its rite's color (a recolored copy
// of the frame the game puts on items queued to equip): blue for weather, gold for daylight, violet for clearing a
// biome's haze, pale for the Equilibrium Rite. Other rite items (without enough of them in yet, or kept out by a rite
// before them) get a dim gray one. Tooltips say what each performs, how many more are needed, or that they just burn.
// When the burn performs the Unmaking Rite, the gear it unmakes is framed in its color too, and its tooltip shows what
// comes back (see UnmakingTooltip).
internal static class ObliteratorGrid
{
    private const string MarkName = "TempestRite_WeatherMark";
    private static readonly Color OtherColor = new(0.6f, 0.6f, 0.6f, 0.38f);
    private const float KeyAlpha = 0.85f;

    // Worked out once per frame: the grid and the tooltip both ask.
    private static int _frame = -1;
    private static readonly List<ObliteratorKeys.Picked> Picked = [];
    private static bool _allowed;

    private static bool IsObliteratorGrid(InventoryGrid grid, out Inventory? inventory)
    {
        inventory = null;
        InventoryGui gui = InventoryGui.instance;
        if (!gui || grid != gui.m_containerGrid || !gui.m_currentContainer) return false;
        if (!ObliteratorKeys.IsObliteratorHatch(gui.m_currentContainer, out _) || !ObliteratorSwitch.IsOn(gui.m_currentContainer)) return false;
        inventory = gui.m_currentContainer.GetInventory();
        return inventory != null;
    }

    private static List<ObliteratorKeys.Picked> Current(Inventory inventory)
    {
        if (_frame != Time.frameCount)
        {
            _frame = Time.frameCount;
            _allowed = WeatherSync.LocalPlayerMayChange();
            Picked.Clear();
            if (_allowed) Picked.AddRange(ObliteratorKeys.PickAll(inventory));
        }
        return Picked;
    }

    // After the grid has drawn its items.
    internal static void UpdateMarks(InventoryGrid grid)
    {
        InventoryGui gui = InventoryGui.instance;
        if (!gui || grid != gui.m_containerGrid) return;
        Inventory? inventory = null;
        List<ObliteratorKeys.Picked>? picked = null;
        bool showing = PluginConfig.ShowItemHighlight.IsOn() && IsObliteratorGrid(grid, out inventory);
        if (showing) picked = Current(inventory!);
        showing &= _allowed;
        bool unmaking = showing && Unmaking(picked!);

        foreach (InventoryElement element in grid.m_elements)
        {
            // Items of the rites performed are framed in their rite's color; other rite items get a dim gray frame.
            Color? color = null;
            if (showing)
            {
                ItemDrop.ItemData? item = inventory!.GetItemAt(element.Position.x, element.Position.y);
                WeatherKind own = item != null ? ObliteratorKeys.KindOf(item) : WeatherKind.None;
                // Only rites whose items the player has found are shown.
                if (own != WeatherKind.None && ObliteratorKeys.IsKnown(own))
                {
                    Color rite = WeatherKinds.Color(own);
                    color = ObliteratorKeys.Contains(picked!, own) ? new Color(rite.r, rite.g, rite.b, KeyAlpha) : OtherColor;
                }
                else if (unmaking && item != null && UnmakingReturns.Unmakes(item))
                {
                    Color rite = WeatherKinds.Color(WeatherKind.Unmaking);
                    color = new Color(rite.r, rite.g, rite.b, KeyAlpha);
                }
            }
            bool marked = color != null;
            Image? mark = Mark(element, marked);
            if (!mark) continue;
            if (mark!.enabled != marked) mark.enabled = marked;
            if (marked) mark.color = color!.Value;
        }
    }

    // Extra line for an item's tooltip in the open obliterator, or empty.
    internal static string TooltipLine(InventoryGrid grid, ItemDrop.ItemData item)
    {
        if (!PluginConfig.ShowItemHighlight.IsOn() || !IsObliteratorGrid(grid, out Inventory? inventory)) return string.Empty;
        List<ObliteratorKeys.Picked> picked = Current(inventory!);
        return _allowed ? ObliteratorHints.Tooltip(inventory!, item, picked) : string.Empty;
    }

    // The item in the open obliterator whose tooltip is the one showing, when the burn unmakes it; otherwise null.
    internal static ItemDrop.ItemData? UnmadeItem(UITooltip? tooltip)
    {
        InventoryGui gui = InventoryGui.instance;
        if (!tooltip || !gui || !PluginConfig.ShowItemHighlight.IsOn()) return null;
        InventoryGrid grid = gui.m_containerGrid;
        if (!grid || !IsObliteratorGrid(grid, out Inventory? inventory)) return null;
        List<ObliteratorKeys.Picked> picked = Current(inventory!);
        if (!_allowed || !Unmaking(picked)) return null;
        foreach (InventoryElement element in grid.m_elements)
        {
            if (element.m_tooltip != tooltip) continue;
            ItemDrop.ItemData? item = inventory!.GetItemAt(element.Position.x, element.Position.y);
            return item != null && ObliteratorKeys.KindOf(item) == WeatherKind.None && UnmakingReturns.Unmakes(item) ? item : null;
        }
        return null;
    }

    private static bool Unmaking(List<ObliteratorKeys.Picked> picked) =>
        ObliteratorKeys.Contains(picked, WeatherKind.Unmaking) && ObliteratorKeys.IsKnown(WeatherKind.Unmaking);

    // The element's mark, made from its queued frame the first time it is needed.
    private static Image? Mark(InventoryElement element, bool create)
    {
        Image queued = element.m_queued;
        // Only a frame of its own can be copied, never the whole slot.
        if (!queued || queued.gameObject == element.gameObject) return null;
        Transform parent = queued.transform.parent;
        Transform existing = parent.Find(MarkName);
        if (existing) return existing.GetComponent<Image>();
        if (!create) return null;

        GameObject copy = Object.Instantiate(queued.gameObject, parent, false);
        copy.name = MarkName;
        copy.transform.SetSiblingIndex(queued.transform.GetSiblingIndex() + 1);
        foreach (Transform child in copy.transform) Object.Destroy(child.gameObject);
        Image mark = copy.GetComponent<Image>();
        mark.raycastTarget = false;
        copy.SetActive(true);
        return mark;
    }
}
