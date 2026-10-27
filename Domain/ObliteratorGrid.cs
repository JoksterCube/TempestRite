using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace JoksterCube.TempestRite.Domain;

// In the open obliterator, the item that decides the rite gets a frame in its rite's color (a recolored copy
// of the frame the game puts on items queued to equip): blue for weather, gold for daylight, violet for clearing a
// biome's haze, pale for the Equilibrium Rite. Other rite items, and those without enough of them in yet, get a dim
// gray one. Tooltips say which decides and why, how many more are needed, or that they just burn.
internal static class ObliteratorGrid
{
    private const string MarkName = "TempestRite_WeatherMark";
    private static readonly Color OtherColor = new(0.6f, 0.6f, 0.6f, 0.38f);
    private const float KeyAlpha = 0.85f;

    // Worked out once per frame: the grid and the tooltip both ask.
    private static int _frame = -1;
    private static WeatherKind _kind;
    private static ItemDrop.ItemData? _keyItem;
    private static bool _several;
    private static bool _allowed;

    private static bool IsObliteratorGrid(InventoryGrid grid, out Inventory? inventory)
    {
        inventory = null;
        InventoryGui gui = InventoryGui.instance;
        if (!gui || grid != gui.m_containerGrid || !gui.m_currentContainer) return false;
        if (!ObliteratorKeys.IsObliteratorHatch(gui.m_currentContainer, out _)) return false;
        inventory = gui.m_currentContainer.GetInventory();
        return inventory != null;
    }

    private static WeatherKind Current(Inventory inventory, out ItemDrop.ItemData? keyItem, out bool several)
    {
        if (_frame != Time.frameCount)
        {
            _frame = Time.frameCount;
            _allowed = WeatherSync.LocalPlayerMayChange();
            _kind = _allowed ? ObliteratorKeys.Pick(inventory, out _keyItem, out _several) : WeatherKind.None;
            if (_kind == WeatherKind.None) _keyItem = null;
        }
        keyItem = _keyItem;
        several = _several;
        return _kind;
    }

    // After the grid has drawn its items.
    internal static void UpdateMarks(InventoryGrid grid)
    {
        InventoryGui gui = InventoryGui.instance;
        if (!gui || grid != gui.m_containerGrid) return;
        ItemDrop.ItemData? keyItem = null;
        Inventory? inventory = null;
        WeatherKind kind = WeatherKind.None;
        bool showing = PluginConfig.ShowItemHighlight.IsOn() && IsObliteratorGrid(grid, out inventory);
        if (showing) kind = Current(inventory!, out keyItem, out _);
        showing &= _allowed;
        Color keyColor = WeatherKinds.Color(kind);

        foreach (InventoryElement element in grid.m_elements)
        {
            // The deciding item is framed in its rite's color; other rite items get a dim gray frame.
            Color? color = null;
            if (showing)
            {
                ItemDrop.ItemData? item = inventory!.GetItemAt(element.Position.x, element.Position.y);
                // Only rites whose items the player has found are shown.
                if (item == keyItem) color = ObliteratorKeys.IsKnown(kind) ? new Color(keyColor.r, keyColor.g, keyColor.b, KeyAlpha) : null;
                else if (item != null && ObliteratorKeys.KindOf(item) is var own && own != WeatherKind.None && ObliteratorKeys.IsKnown(own)) color = OtherColor;
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
        WeatherKind kind = Current(inventory!, out ItemDrop.ItemData? keyItem, out bool several);
        return _allowed ? ObliteratorHints.Tooltip(inventory!, item, keyItem, kind, several) : string.Empty;
    }

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
