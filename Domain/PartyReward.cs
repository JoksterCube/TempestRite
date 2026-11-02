using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Crimson, Ragnarök and Hoard-Bane Rites' rewards grow with the players who fought: alone, the usual reward; each
// player more adds this share of it (0.5: two players get 1.5 times as much, three twice, four 2.5 times).
// The players counted are those alive inside the bubble as the reward is won: the fallen and those who left don't count.
internal static class PartyReward
{
    internal static float Multiplier(int players, float perExtraPlayer) =>
        1f + Math.Max(0f, perExtraPlayer) * Math.Max(0, players - 1);

    // amount times the multiplier, rounded down or up at random so that on average it is exact (1 x 1.5: 1 or 2, half
    // the time each). Roll from 0 to 1.
    internal static int Scale(int amount, float multiplier, float roll)
    {
        if (amount <= 0) return 0;
        double scaled = amount * (double)Math.Max(0f, multiplier);
        return (int)Math.Min(int.MaxValue, Math.Floor(scaled + Mathf.Clamp(roll, 0f, 0.9999f)));
    }

    internal static void ScaleAll(List<(string item, int amount)> rewards, float multiplier, Func<float> random)
    {
        for (int i = rewards.Count - 1; i >= 0; i--)
        {
            int amount = Scale(rewards[i].amount, multiplier, random());
            if (amount > 0) rewards[i] = (rewards[i].item, amount);
            else rewards.RemoveAt(i);
        }
    }
}
