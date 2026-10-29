using System.Collections.Generic;

namespace JoksterCube.TempestRite.Domain;

// Putting a rite's items into the obliterator from the player's inventory (the rites page's item buttons).
internal static class RiteItems
{
    private static readonly List<ItemDrop.ItemData> Matching = [];

    // How many more of the rite's items (any mix of them) the obliterator needs.
    internal static int Missing(Inventory obliterator, WeatherKind kind) =>
        System.Math.Max(0, ObliteratorKeys.Amount(kind) - ObliteratorKeys.Count(obliterator, kind));

    internal static int Have(Inventory inventory, WeatherKind kind) => ObliteratorKeys.Count(inventory, kind);

    // Moves what the obliterator is missing from the player's inventory into it: onto a stack of the same item there,
    // or into the first free slot from the bottom left, where items go first and decide. The clicked item goes first,
    // then the rite's other items make up the rest. Only when the player has all of it. Returns whether anything moved.
    internal static bool PutIn(Inventory from, Inventory to, WeatherKind kind, string preferred)
    {
        int needed = Missing(to, kind);
        if (needed <= 0 || Have(from, kind) < needed) return false;

        Matching.Clear();
        foreach (ItemDrop.ItemData item in from.GetAllItems())
            if (ObliteratorKeys.KindOf(item) == kind && ObliteratorKeys.IsItem(item, preferred)) Matching.Add(item);
        foreach (ItemDrop.ItemData item in from.GetAllItems())
            if (ObliteratorKeys.KindOf(item) == kind && !ObliteratorKeys.IsItem(item, preferred)) Matching.Add(item);

        bool moved = false;
        foreach (ItemDrop.ItemData item in Matching)
        {
            while (needed > 0 && item.m_stack > 0)
            {
                if (!Target(to, item, out int x, out int y, out int room)) return moved;
                int amount = System.Math.Min(System.Math.Min(needed, item.m_stack), room);
                int before = item.m_stack;
                to.MoveItemToThis(from, item, amount, x, y);
                int done = before - item.m_stack;
                if (done <= 0) return moved;
                needed -= done;
                moved = true;
            }
            if (needed <= 0) break;
        }
        return moved;
    }

    // Where to put it: a stack of the same item with room, or the first free slot from the bottom left.
    private static bool Target(Inventory inventory, ItemDrop.ItemData item, out int x, out int y, out int room)
    {
        foreach (ItemDrop.ItemData there in inventory.GetAllItems())
        {
            if (!there.IsSameType(item)) continue;
            room = there.m_shared.m_maxStackSize - there.m_stack;
            if (room <= 0) continue;
            x = there.m_gridPos.x;
            y = there.m_gridPos.y;
            return true;
        }
        for (y = inventory.GetHeight() - 1; y >= 0; y--)
            for (x = 0; x < inventory.GetWidth(); x++)
                if (inventory.GetItemAt(x, y) == null)
                {
                    room = item.m_shared.m_maxStackSize;
                    return true;
                }
        x = y = room = 0;
        return false;
    }
}
