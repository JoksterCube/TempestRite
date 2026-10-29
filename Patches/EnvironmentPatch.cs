using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// Called weather takes the place of the biome's own weather, but only when nothing else (raid, boss, intro, special
// area) already sets the weather. Dungeons set theirs another way, which wins over this too.
[HarmonyPatch(typeof(EnvMan), nameof(EnvMan.GetEnvironmentOverride))]
internal static class EnvironmentPatch
{
    private const string Feature = "Weather change";

    private static void Postfix(EnvMan __instance, ref string? __result)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            // The blood moon and the burning sky win over raids' and bosses' own skies, not over a sky set from the console.
            if (string.IsNullOrEmpty(__instance.m_debugEnv) && WeatherEnvironment.Incursion(__instance) is { } moon)
            {
                WeatherEnvironment.NotOverriding();
                __result = moon;
                return;
            }
            if (!string.IsNullOrEmpty(__result))
            {
                WeatherEnvironment.NotOverriding();
                return;
            }
            string? environment = WeatherEnvironment.Current(__instance);
            if (environment != null) __result = environment;
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// Called weather never makes a place warmer than its own weather would: in the mountains, clear skies are as freezing
// as the snow they replace. Colder called weather (a snowstorm) does make it colder, as it would on its own.
internal static class TemperaturePatch
{
    private const string Feature = "Weather temperature";

    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.CalculateFreezing))]
    private static class Freezing
    {
        private static void Postfix(EnvMan __instance, ref bool __result)
        {
            if (__result || !WeatherEnvironment.Overriding || PatchGuard.IsDisabled(Feature)) return;
            try
            {
                EnvSetup? natural = WeatherEnvironment.Natural(__instance);
                if (natural != null) __result = natural.m_isFreezing || (natural.m_isFreezingAtNight && !EnvMan.IsDay());
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.CalculateCold))]
    private static class Cold
    {
        private static void Postfix(EnvMan __instance, ref bool __result)
        {
            if (__result || !WeatherEnvironment.Overriding || PatchGuard.IsDisabled(Feature)) return;
            try
            {
                EnvSetup? natural = WeatherEnvironment.Natural(__instance);
                if (natural != null) __result = natural.m_isCold || (natural.m_isColdAtNight && !EnvMan.IsDay());
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}

// Polar day and polar night: the sun and the light are held at noon or midnight, and so is what the game counts as day
// or night: creatures spawning by day or night (more and stronger ones at night), night creatures leaving at dawn,
// night cold. Time itself goes on as usual: days are counted, beds go by the real clock, and clocks (from other mods
// too) show the real time.
//
// The game keeps one time of day, eased towards the clock, in a field (m_smoothDayFraction). Its environment update
// (FixedUpdate) eases it, draws the sun and light from it, and sets the day and night flags everything else asks
// (EnvMan.IsDay, IsNight...). So while the sun is held, the held, eased value is put in the field only for the
// environment update, and the real time of day is put back right after, for clocks and anything reading the time.
// When the rite ends the sun is eased back to the real time of day before the field is left to the game again.
internal static class DaylightPatch
{
    private const string Feature = "Polar day and night";
    private const float Noon = 0.5f;
    private const float Midnight = 0f;
    // Close enough to the real time of day to stop easing the sun back.
    private const float CaughtUp = 0.002f;

    // The time of day the sun and light are drawn at, eased like the game eases its own.
    private static float _shown;
    private static bool _showing;
    // While working out the real time of day, the held one is not put in.
    private static bool _real;

    internal static void ClearSession()
    {
        _showing = false;
        _real = false;
    }

    private static float? Held() => WeatherState.Active(EffectSlot.Daylight) switch
    {
        WeatherKind.PolarDay => Noon,
        WeatherKind.PolarNight => Midnight,
        _ => null
    };

    // The same sum the game makes from the world clock, without easing.
    private static float RealTimeOfDay(EnvMan envMan)
    {
        double dayLengthMs = envMan.m_dayLengthSec * 1000.0;
        float raw = Mathf.Clamp01((float)(ZNet.instance.GetTimeSeconds() * 1000.0 % dayLengthMs / 1000.0) / envMan.m_dayLengthSec);
        _real = true;
        try { return envMan.RescaleDayFraction(raw); }
        finally { _real = false; }
    }

    [HarmonyPatch(typeof(EnvMan), "FixedUpdate")]
    private static class DrawHeldSun
    {
        private static void Prefix(EnvMan __instance, out bool __state)
        {
            __state = false;
            if (PatchGuard.IsDisabled(Feature) || !ZNet.instance) return;
            try
            {
                bool held = Held() != null;
                if (!held && !_showing) return;
                if (!_showing)
                {
                    // Starts from where the sun is now, so it moves there smoothly.
                    _shown = __instance.m_smoothDayFraction;
                    _showing = true;
                }
                __instance.m_smoothDayFraction = _shown;
                __state = true;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Postfix(EnvMan __instance, bool __state)
        {
            if (!__state) return;
            try
            {
                _shown = __instance.m_smoothDayFraction;
                float real = RealTimeOfDay(__instance);
                __instance.m_smoothDayFraction = real;
                // Once the rite is over and the sun has caught up, the game has its field back.
                if (Held() == null && Mathf.Abs(Mathf.DeltaAngle(_shown * 360f, real * 360f)) / 360f < CaughtUp) _showing = false;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Inside the environment update: where the sun is eased to.
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.RescaleDayFraction))]
    private static class HoldTheSun
    {
        private static void Postfix(ref float __result)
        {
            if (_real || PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (Held() is float held) __result = held;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Beds go by the real clock: the held time of day would let players sleep at any time under polar night, and never
    // under polar day. The same rule as the game's: in the afternoon or at night, once the last sleep has worn off.
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.CalculateCanSleep))]
    private static class SleepByClock
    {
        private static void Postfix(EnvMan __instance, ref bool __result)
        {
            if (PatchGuard.IsDisabled(Feature) || Held() == null || !ZNet.instance) return;
            try
            {
                float real = RealTimeOfDay(__instance);
                bool afternoon = real >= 0.5f && real <= 0.75f;
                bool night = real <= 0.25f || real >= 0.75f;
                Player? player = Player.m_localPlayer;
                __result = (afternoon || night)
                    && (!player || ZNet.instance.GetTimeSeconds() > player!.m_wakeupTime + __instance.m_sleepCooldownSeconds);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}

// Clear Mistlands: no mist is made, what is there fades, and creatures see as if there were no mist.
internal static class MistPatch
{
    private const string Feature = "Clear Mistlands";

    private static bool Active => !PatchGuard.IsDisabled(Feature) && WeatherState.Active(EffectSlot.Mistlands) == WeatherKind.NoMist;

    [HarmonyPatch(typeof(ParticleMist), "Update")]
    private static class NoNewMist
    {
        private static bool Prefix() => !Active;
    }

    [HarmonyPatch(typeof(ParticleMist), nameof(ParticleMist.IsInMist))]
    private static class NotInMist
    {
        private static void Postfix(ref bool __result)
        {
            if (__result && Active) __result = false;
        }
    }

    [HarmonyPatch(typeof(ParticleMist), nameof(ParticleMist.IsMistBlocked))]
    private static class SightNotBlocked
    {
        private static void Postfix(ref bool __result)
        {
            if (__result && Active) __result = false;
        }
    }
}
