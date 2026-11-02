using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Taproot-Surge Rite (see RiteSpeedup):
// - sap extractors: the seconds of work they have stored are added to, and on their next tick they fill up from the
//   root as usual (only while the root has sap left);
// - eitr refineries: the seconds of work they owe are added to, as under the Pitch-Bellows Rite, and they work them
//   off using soft tissue and sap as usual (the Pitch-Bellows Rite doesn't speed them up);
// - ancient Yggdrasil roots: their sap comes back Root Refill Multiplier times as fast.
internal static class TaprootFlow
{
    internal static readonly SpedWork Extracting = new ExtractorWork();
    internal static readonly SpedWork Refining = new RefineryWork();
    internal static readonly SpedWork Refilling = new RootWork();

    internal static bool IsRefinery(GameObject prefab) => prefab.name == "eitrrefinery";

    private sealed class ExtractorWork() : SpedWork(WeatherKind.TaprootSurge)
    {
        internal override float Speed => PluginConfig.TaprootSpeedMultiplier.Value;
        internal override bool ShowMotes => PluginConfig.ShowTaprootSurgeMotes.IsOn();
        internal override float? Takes(GameObject prefab) => prefab.GetComponent<SapCollector>() ? 1f : null;

        // Every one the rite reaches puffs now and then, full or filling, a few seconds apart at random.
        internal override bool Glows(ZDO zdo, ZNetView view, bool busy) => true;
        internal override float MotesEvery => UnityEngine.Random.Range(3f, 8f);

        internal override bool Busy(ZDO zdo, ZNetView view) =>
            view.GetComponent<SapCollector>() is { } collector && collector && collector.GetLevel() < collector.m_maxLevel
            && collector.m_root && collector.m_root.CanDrain(1f);

        // The game turns what is stored into sap on its next tick, at most what the root has left.
        internal override void Hurry(ZDO zdo, ZNetView view, double seconds) =>
            zdo.Set(ZDOVars.s_product, zdo.GetFloat(ZDOVars.s_product) + (float)seconds);

        // From the middle of the extractor itself: its first renderer can be one of its effects, standing elsewhere.
        internal override Vector3 MotesAt(ZNetView view, Vector3 position)
        {
            Bounds? bounds = null;
            foreach (MeshRenderer renderer in view.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer || !renderer.enabled) continue;
                if (bounds is { } all) { all.Encapsulate(renderer.bounds); bounds = all; }
                else bounds = renderer.bounds;
            }
            return bounds?.center ?? position + Vector3.up * 0.5f;
        }
    }

    private sealed class RefineryWork() : SpedWork(WeatherKind.TaprootSurge)
    {
        internal override float Speed => PluginConfig.TaprootSpeedMultiplier.Value;
        internal override bool ShowMotes => PluginConfig.ShowTaprootSurgeMotes.IsOn();
        internal override float? Takes(GameObject prefab) => IsRefinery(prefab) && prefab.GetComponent<Smelter>() ? 1f : null;
        internal override bool Busy(ZDO zdo, ZNetView view) => StationWork.SmelterBusy(view);
        internal override void Hurry(ZDO zdo, ZNetView view, double seconds) => StationWork.SmelterHurry(zdo, seconds);
    }

    private sealed class RootWork() : SpedWork(WeatherKind.TaprootSurge)
    {
        internal override float Speed => PluginConfig.TaprootRootMultiplier.Value;
        internal override bool ShowMotes => PluginConfig.ShowTaprootSurgeMotes.IsOn();
        internal override float? Takes(GameObject prefab) => prefab.GetComponentInChildren<ResourceRoot>(true) ? 1.2f : null;

        internal override bool Busy(ZDO zdo, ZNetView view) =>
            view.GetComponentInChildren<ResourceRoot>() is { } root && root && root.GetLevel() < root.m_maxLevel;

        internal override void Hurry(ZDO zdo, ZNetView view, double seconds)
        {
            if (view.GetComponentInChildren<ResourceRoot>() is not { } root || !root) return;
            float level = zdo.GetFloat(ZDOVars.s_level, root.m_maxLevel);
            zdo.Set(ZDOVars.s_level, Mathf.Min(root.m_maxLevel, level + root.m_regenPerSec * (float)seconds));
        }

        // From where the sap glows, partway up the root.
        internal override Vector3 MotesAt(ZNetView view, Vector3 position) => position + Vector3.up * 2f;
    }
}
