using System;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Kindred Rite: tamed animals near the obliterator breed faster (see RiteSpeedup and KindredPatches).
// - Young ones grow up faster: the game grows them once enough time has passed since they were born (saved on them, in
//   world time), so that time is moved back.
// - Pregnant ones (hens: about to lay) give birth sooner: the same, with the time they fell pregnant.
// - Falling pregnant: the game checks every 10 seconds whether a tamed animal gains a love point; under the rite it
//   checks Breeding Speed Multiplier times as often.
// - The game stops a tamed animal breeding once too many of its kind (and its young) are close by; under the rite that
//   limit is Crowd Limit Multiplier times as high.
// - Warm eggs hatch sooner: the game hatches them once enough time has passed since they got warm (saved on them, in
//   world seconds), so that time is moved back.
// Now and then a few of the young, pregnant ones and warm eggs it reaches give off pink hearts.
internal static class KindredBreeding
{
    // Young ones get smaller puffs of hearts than grown ones.
    private const float YoungHearts = 0.55f;
    private const float GrownHearts = 0.8f;
    private const float EggHearts = 0.4f;

    internal static readonly SpedWork Work = new BreedingWork();

    private static float Speed => Math.Max(1f, PluginConfig.KindredGrowthMultiplier.Value);

    // Whether the rite reaches a tamed animal (for its hover text and breeding).
    internal static bool Reaches(Component animal) =>
        WeatherState.IsActive(WeatherKind.Kindred) && WeatherState.Reaches(WeatherKind.Kindred, animal.transform.position)
        && animal.GetComponent<ZNetView>() is { } view && view && view.IsValid() && view.GetZDO().GetBool(ZDOVars.s_tamed);

    // The crowd limit under the rite, from the animal's own.
    internal static int Crowd(int usual) => Mathf.Max(usual, Mathf.RoundToInt(usual * Math.Max(1f, PluginConfig.KindredCrowdMultiplier.Value)));

    // How many more times than usual to check for a love point this time: 1 at 2x, 0 or 1 at 1.5x.
    internal static int ExtraChecks()
    {
        float extra = Speed - 1f;
        int whole = Mathf.FloorToInt(extra);
        return whole + (UnityEngine.Random.value < extra - whole ? 1 : 0);
    }

    // About how many seconds until a tamed young animal is grown, at the rite's speed while it lasts. Negative: not known.
    internal static double GrownIn(Growup growup) =>
        growup.GetComponent<ZNetView>() is { } view && view && view.IsValid()
            ? ReadyIn(view.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L), growup.m_growTime)
            : -1.0;

    // About how many seconds until a pregnant animal gives birth. Negative: not pregnant.
    internal static double DueIn(Procreation procreation) =>
        procreation.GetComponent<ZNetView>() is { } view && view && view.IsValid()
            ? ReadyIn(view.GetZDO().GetLong(ZDOVars.s_pregnant, 0L), procreation.m_pregnancyDuration)
            : -1.0;

    // About how many seconds until a warm egg hatches. Negative: not warm.
    internal static double HatchesIn(EggGrow egg)
    {
        if (egg.GetComponent<ZNetView>() is not { } view || !view || !view.IsValid() || !ZNet.instance) return -1.0;
        float warm = view.GetZDO().GetFloat(ZDOVars.s_growStart);
        if (warm <= 0f) return -1.0;
        return Left(egg.m_growTime - (ZNet.instance.GetTimeSeconds() - warm));
    }

    private static double ReadyIn(long since, double takes)
    {
        if (since < 1L || !ZNet.instance) return -1.0;
        return Left(takes - (ZNet.instance.GetTime() - new DateTime(since)).TotalSeconds);
    }

    private static double Left(double left) => RiteSpeedup.ReadyIn(WeatherKind.Kindred, PluginConfig.KindredGrowthMultiplier.Value, left);

    private sealed class BreedingWork() : SpedWork(WeatherKind.Kindred)
    {
        internal override float Speed => PluginConfig.KindredGrowthMultiplier.Value;
        internal override bool ShowMotes => PluginConfig.ShowKindredHearts.IsOn();
        internal override float MotesEvery => 3f;

        // Eggs that hatch, and tamable animals that grow up or breed (not bosses).
        internal override float? Takes(GameObject prefab)
        {
            if (prefab.GetComponent<EggGrow>()) return EggHearts;
            if (prefab.GetComponent<Character>() is not { } character || !character || character.IsBoss() || character.IsPlayer()) return null;
            return prefab.GetComponent<Growup>() ? YoungHearts : prefab.GetComponent<Procreation>() ? GrownHearts : null;
        }

        // A warm egg, a tamed young one growing up, or a tamed pregnant one. Only these give off hearts.
        internal override bool Busy(ZDO zdo, ZNetView view) =>
            view.GetComponent<EggGrow>()
                ? zdo.GetFloat(ZDOVars.s_growStart) > 0f
                : zdo.GetBool(ZDOVars.s_tamed)
                  && (view.GetComponent<Growup>() ? zdo.GetLong(ZDOVars.s_spawnTime, 0L) > 1L : zdo.GetLong(ZDOVars.s_pregnant, 0L) > 1L);

        // When it was born, fell pregnant or got warm (an egg), whichever was last.
        internal override double? Since(ZDO zdo)
        {
            double? since = null;
            foreach (double? time in new[]
                     {
                         RiteSpeedup.FromTicks(zdo.GetLong(ZDOVars.s_spawnTime, 0L)), RiteSpeedup.FromTicks(zdo.GetLong(ZDOVars.s_pregnant, 0L)),
                         zdo.GetFloat(ZDOVars.s_growStart) is var warm && warm > 0f ? warm : null
                     })
                if (time is { } t && (since == null || t > since)) since = t;
            return since;
        }

        internal override void Hurry(ZDO zdo, ZNetView view, double seconds)
        {
            if (view.GetComponent<EggGrow>())
            {
                // 0 means cold, so never reach it.
                float warm = zdo.GetFloat(ZDOVars.s_growStart);
                if (warm > 0f) zdo.Set(ZDOVars.s_growStart, Mathf.Max(1f, (float)(warm - seconds)));
                return;
            }
            int key = view.GetComponent<Growup>() ? ZDOVars.s_spawnTime : ZDOVars.s_pregnant;
            long since = zdo.GetLong(key, 0L);
            // 0 means not pregnant, so never reach it.
            if (since > 1L) zdo.Set(key, Math.Max(1L, since - TimeSpan.FromSeconds(seconds).Ticks));
        }

        // Above the animal's head.
        internal override Vector3 MotesAt(ZNetView view, Vector3 position) =>
            view.GetComponent<Character>() is { } character && character && character.m_collider
                ? character.GetTopPoint() + Vector3.up * 0.2f
                : base.MotesAt(view, position);
    }
}
