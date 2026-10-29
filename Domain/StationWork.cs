using System;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The stations the Vat-Boil and Pitch-Bellows Rites speed up (see RiteSpeedup).
// - Vat-Boil, fermenters: the time they started is moved back, as the game works out how long they fermented from it.
// - Vat-Boil, cooking stations (the cooking station, the iron one and the oven): each slot's cooked time moves on, but
//   never past done; and while the rite reaches them, nothing cooked there burns (see VatBoilPatches).
// - Pitch-Bellows, smelters, blast furnaces, charcoal kilns and eitr refineries: the seconds of work they owe are
//   added to, and they work them off at once as usual, using up ore and fuel (and only with a roof, smoke clear...).
internal static class StationWork
{
    internal static readonly SpedWork Fermenting = new FermenterWork();
    internal static readonly SpedWork Cooking = new CookingWork();
    internal static readonly SpedWork Smelting = new SmelterWork();

    // A fermenting fermenter under the Vat-Boil Rite: about how many seconds until it's done. Negative: not fermenting.
    internal static double FermentedIn(Fermenter fermenter) =>
        fermenter.m_nview && fermenter.m_nview.IsValid() && fermenter.GetStatus() == Fermenter.Status.Fermenting
            ? RiteSpeedup.ReadyIn(WeatherKind.VatBoil, PluginConfig.VatBoilSpeedMultiplier.Value,
                fermenter.m_fermentationDuration - fermenter.GetFermentationTime())
            : -1.0;

    // Higher up than the middle: over a fermenter's lid, a furnace's top.
    private static Vector3 Above(ZNetView view, Vector3 position)
    {
        if (view.GetComponentInChildren<Renderer>() is { } renderer && renderer)
        {
            Bounds bounds = renderer.bounds;
            return bounds.center + Vector3.up * (bounds.extents.y * 0.6f);
        }
        return position + Vector3.up * 1.2f;
    }

    private sealed class FermenterWork() : SpedWork(WeatherKind.VatBoil)
    {
        internal override float Speed => PluginConfig.VatBoilSpeedMultiplier.Value;
        internal override bool ShowMotes => PluginConfig.ShowVatBoilMotes.IsOn();
        internal override float? Takes(GameObject prefab) => prefab.GetComponent<Fermenter>() ? 0.8f : null;

        internal override bool Busy(ZDO zdo, ZNetView view) =>
            view.GetComponent<Fermenter>() is { } fermenter && fermenter && fermenter.GetStatus() == Fermenter.Status.Fermenting
            && !fermenter.m_exposed && fermenter.m_hasRoof;

        internal override void Hurry(ZDO zdo, ZNetView view, double seconds)
        {
            long started = zdo.GetLong(ZDOVars.s_startTime, 0L);
            if (started > 0L) zdo.Set(ZDOVars.s_startTime, Math.Max(1L, started - TimeSpan.FromSeconds(seconds).Ticks));
        }

        internal override Vector3 MotesAt(ZNetView view, Vector3 position) => Above(view, position);
    }

    private sealed class CookingWork() : SpedWork(WeatherKind.VatBoil)
    {
        internal override float Speed => PluginConfig.VatBoilSpeedMultiplier.Value;
        internal override bool ShowMotes => PluginConfig.ShowVatBoilMotes.IsOn();
        internal override float? Takes(GameObject prefab) => prefab.GetComponent<CookingStation>() ? 0.6f : null;

        internal override bool Busy(ZDO zdo, ZNetView view)
        {
            if (view.GetComponent<CookingStation>() is not { } station || !station) return false;
            bool lit = (station.m_requireFire && station.IsFireLit()) || (station.m_useFuel && station.GetFuel() > 0f);
            if (!lit) return false;
            for (int i = 0; i < station.m_slots.Length; i++)
                if (Raw(station, i, out _, out _, out _)) return true;
            return false;
        }

        internal override void Hurry(ZDO zdo, ZNetView view, double seconds)
        {
            if (view.GetComponent<CookingStation>() is not { } station || !station) return;
            for (int i = 0; i < station.m_slots.Length; i++)
            {
                if (!Raw(station, i, out string item, out float cooked, out float takes)) continue;
                station.GetSlot(i, out _, out _, out CookingStation.Status status, out bool cheated);
                // Up to done at most: the game finishes it on its next tick.
                station.SetSlot(i, item, Mathf.Min(cooked + (float)seconds, takes), status, cheated);
            }
        }

        // A slot with something not cooked yet.
        private static bool Raw(CookingStation station, int slot, out string item, out float cooked, out float takes)
        {
            station.GetSlot(slot, out item, out cooked, out CookingStation.Status status, out _);
            takes = 0f;
            if (string.IsNullOrEmpty(item) || status != CookingStation.Status.NotDone) return false;
            CookingStation.ItemConversion? conversion = station.GetItemConversion(item);
            if (conversion == null || !conversion.m_from || item != conversion.m_from.name) return false;
            takes = conversion.m_cookTime;
            return cooked < takes;
        }

        internal override Vector3 MotesAt(ZNetView view, Vector3 position) => Above(view, position);
    }

    private sealed class SmelterWork() : SpedWork(WeatherKind.PitchBellows)
    {
        // The game counts at most this many seconds owed.
        private const float MaxOwed = 3600f;

        internal override float Speed => PluginConfig.PitchBellowsSpeedMultiplier.Value;
        internal override bool ShowMotes => PluginConfig.ShowPitchBellowsMotes.IsOn();

        // Those burning fuel (smelter, blast furnace, eitr refinery) and the charcoal kiln; not the windmill or the
        // spinning wheel.
        internal override float? Takes(GameObject prefab)
        {
            if (prefab.GetComponent<Smelter>() is not { } smelter || !smelter || smelter.m_windmill) return null;
            if (smelter.m_fuelItem) return 1f;
            foreach (Smelter.ItemConversion conversion in smelter.m_conversion)
                if (conversion.m_to && conversion.m_to.name == "Coal") return 1f;
            return null;
        }

        internal override bool Busy(ZDO zdo, ZNetView view) =>
            view.GetComponent<Smelter>() is { } smelter && smelter && smelter.GetQueueSize() > 0
            && (smelter.m_maxFuel == 0 || smelter.GetFuel() > 0f) && (!smelter.m_requiresRoof || smelter.m_haveRoof) && !smelter.m_blockedSmoke;

        internal override void Hurry(ZDO zdo, ZNetView view, double seconds) =>
            zdo.Set(ZDOVars.s_accTime, Mathf.Min(zdo.GetFloat(ZDOVars.s_accTime) + (float)seconds, MaxOwed));

        internal override Vector3 MotesAt(ZNetView view, Vector3 position) => Above(view, position);
    }
}
