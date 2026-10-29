using System.Collections.Generic;

namespace JoksterCube.TempestRite.Domain;

// With several rite items in the obliterator, every rite with enough of its item in is performed. They go in order:
// the lowest item first, in whatever row it is; when several share that row, the leftmost of them. That is the slot
// materials go to first when moved in with Ctrl + click (bottom row, from the left), so the first rite item put in that
// way goes first. A rite that clashes with one before it (the same slot, or ended by it, see WeatherKinds.Clashes) is
// left out: its item just burns.
internal static class KeyItemPicker
{
    internal static bool IsBefore(int x, int y, int otherX, int otherY) => y > otherY || (y == otherY && x < otherX);

    // Index of the first candidate, or -1 when there is none.
    internal static int PickIndex(IReadOnlyList<(WeatherKind kind, int x, int y)> candidates)
    {
        int best = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            (WeatherKind kind, int x, int y) = candidates[i];
            if (kind == WeatherKind.None) continue;
            if (best < 0 || IsBefore(x, y, candidates[best].x, candidates[best].y)) best = i;
        }
        return best;
    }

    internal static WeatherKind Pick(IReadOnlyList<(WeatherKind kind, int x, int y)> candidates)
    {
        int index = PickIndex(candidates);
        return index < 0 ? WeatherKind.None : candidates[index].kind;
    }

    // Indexes of the candidates performed, in order: one per rite (its first item), none clashing with one before it.
    internal static List<int> PickAllIndexes(IReadOnlyList<(WeatherKind kind, int x, int y)> candidates)
    {
        List<int> order = [];
        for (int i = 0; i < candidates.Count; i++)
        {
            (WeatherKind kind, int x, int y) = candidates[i];
            if (kind == WeatherKind.None) continue;
            int at = order.Count;
            while (at > 0 && IsBefore(x, y, candidates[order[at - 1]].x, candidates[order[at - 1]].y)) at--;
            order.Insert(at, i);
        }

        List<int> picked = [];
        foreach (int i in order)
        {
            bool clashes = false;
            foreach (int p in picked)
                if (WeatherKinds.Clashes(candidates[p].kind, candidates[i].kind))
                {
                    clashes = true;
                    break;
                }
            if (!clashes) picked.Add(i);
        }
        return picked;
    }

    // The rites performed, in order.
    internal static List<WeatherKind> PickAll(IReadOnlyList<(WeatherKind kind, int x, int y)> candidates)
    {
        List<WeatherKind> kinds = [];
        foreach (int i in PickAllIndexes(candidates)) kinds.Add(candidates[i].kind);
        return kinds;
    }
}
