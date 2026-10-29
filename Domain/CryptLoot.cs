using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// How many more items the Crypt Delver Rite adds to a dungeon chest.
internal static class CryptLoot
{
    // rolled: the items the chest rolled. multiplier: how many times that it gets, rounded up (1.5: 1 becomes 2, 3 becomes 5).
    // oneChance, twoChance: the chances (0 to 1) of one or two more on top. roll: a random number from 0 to 1.
    internal static int Extra(int rolled, float multiplier, float oneChance, float twoChance, float roll)
    {
        // Taken off before rounding up, so 2 x 0.5 stays 1 and isn't pushed to 2 by float error.
        int extra = Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0, rolled) * Mathf.Max(0f, multiplier - 1f) - 0.0001f));
        float two = Mathf.Clamp01(twoChance);
        float one = Mathf.Clamp01(oneChance);
        if (roll < two) extra += 2;
        else if (roll < two + one) extra += 1;
        return extra;
    }
}
