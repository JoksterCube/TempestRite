using System.Collections.Generic;

namespace JoksterCube.TempestRite.Domain;

// With several rite items in the obliterator, the lowest one decides, in whatever row it is; when several share that
// row, the leftmost of them. That is the slot materials go to first when moved in with Ctrl + click (bottom row, from
// the left), so the first rite item put in that way decides.
internal static class KeyItemPicker
{
    internal static bool IsBefore(int x, int y, int otherX, int otherY) => y > otherY || (y == otherY && x < otherX);

    // Index of the deciding candidate, or -1 when there is none.
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
}
