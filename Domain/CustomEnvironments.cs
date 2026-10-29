using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// Skies the game doesn't have, made from its own environments and added to its list, so rites (and the env console
// command) can use them by name:
// - TempestRite_Aurora: a clear sky with the strongest auroras any of the game's environments has, at night, and calm,
//   quiet wind (the Aurora Rite).
// - TempestRite_Bloodmoon: a game environment (Eikthyr's by default) tinted blood red (the Crimson Rite).
// - TempestRite_Ragnarok: a game environment (the thunderstorm by default) darkened and tinted ember and gold, a sky
//   on fire (the Ragnarök Rite).
internal static class CustomEnvironments
{
    private const string AuroraBase = "Clear";

    private static EnvMan? _madeFor;
    private static string _bloodmoonBase = string.Empty;
    private static string _ragnarokBase = string.Empty;

    // Makes them once per environment manager (one per world), and the blood moon again if its base changes.
    internal static void Ensure(EnvMan envMan)
    {
        string bloodmoonBase = PluginConfig.BloodmoonBase.Value;
        string ragnarokBase = PluginConfig.RagnarokSkyBase.Value;
        if (_madeFor == envMan && bloodmoonBase == _bloodmoonBase && ragnarokBase == _ragnarokBase) return;
        _madeFor = envMan;
        _bloodmoonBase = bloodmoonBase;
        _ragnarokBase = ragnarokBase;
        Replace(envMan, MakeAurora(envMan));
        Replace(envMan, MakeBloodmoon(envMan, bloodmoonBase));
        Replace(envMan, MakeRagnarok(envMan, ragnarokBase));
    }

    private static void Replace(EnvMan envMan, EnvSetup? env)
    {
        if (env == null) return;
        envMan.m_environments.RemoveAll(e => e != null && e.m_name == env.m_name);
        envMan.m_environments.Add(env);
    }

    private static EnvSetup? MakeAurora(EnvMan envMan)
    {
        EnvSetup? clear = envMan.GetEnv(AuroraBase);
        if (clear == null) return null;
        EnvSetup aurora = clear.Clone();
        aurora.m_name = CustomEnvironmentNames.Aurora;

        // The auroras of the game environment that has the strongest.
        EnvSetup? source = null;
        foreach (EnvSetup env in envMan.m_environments)
            if (env != null && !IsOurs(env) && env.m_auroraGradientTexture && (source == null || env.m_auroraIntensityNight > source.m_auroraIntensityNight))
                source = env;
        if (source != null && source.m_auroraIntensityNight > 0f)
        {
            aurora.m_auroraColors = source.m_auroraColors;
            aurora.m_auroraGradientTexture = source.m_auroraGradientTexture;
            aurora.m_auroraIntensityNight = source.m_auroraIntensityNight;
            aurora.m_auroraIntensityEvening = source.m_auroraIntensityNight * 0.5f;
            aurora.m_auroraIntensityMorning = source.m_auroraIntensityNight * 0.5f;
            aurora.m_auroraIntensityDay = 0f;
        }
        else Plugin.ModLogger.LogWarning("Aurora Rite: none of the game's environments has auroras to borrow; the sky will be clear and calm only.");

        // Calm, quiet wind.
        aurora.m_windMin = 0f;
        aurora.m_windMax = 0.1f;
        aurora.m_ambientVol *= 0.5f;
        return aurora;
    }

    private static EnvSetup? MakeBloodmoon(EnvMan envMan, string baseName)
    {
        EnvSetup? source = envMan.GetEnv(baseName);
        if (source == null || IsOurs(source))
        {
            Plugin.ModLogger.LogWarning($"Crimson Rite: blood moon base environment \"{baseName}\" is not one the game knows.");
            return null;
        }
        EnvSetup moon = source.Clone();
        moon.m_name = CustomEnvironmentNames.Bloodmoon;
        moon.m_ambColorNight = Blood(moon.m_ambColorNight);
        moon.m_ambColorDay = Blood(moon.m_ambColorDay);
        moon.m_fogColorNight = Blood(moon.m_fogColorNight);
        moon.m_fogColorMorning = Blood(moon.m_fogColorMorning);
        moon.m_fogColorDay = Blood(moon.m_fogColorDay);
        moon.m_fogColorEvening = Blood(moon.m_fogColorEvening);
        moon.m_fogColorSunNight = Blood(moon.m_fogColorSunNight);
        moon.m_fogColorSunMorning = Blood(moon.m_fogColorSunMorning);
        moon.m_fogColorSunDay = Blood(moon.m_fogColorSunDay);
        moon.m_fogColorSunEvening = Blood(moon.m_fogColorSunEvening);
        moon.m_sunColorNight = Blood(moon.m_sunColorNight);
        moon.m_sunColorMorning = Blood(moon.m_sunColorMorning);
        moon.m_sunColorDay = Blood(moon.m_sunColorDay);
        moon.m_sunColorEvening = Blood(moon.m_sunColorEvening);
        return moon;
    }

    private static EnvSetup? MakeRagnarok(EnvMan envMan, string baseName)
    {
        EnvSetup? source = envMan.GetEnv(baseName);
        if (source == null || IsOurs(source))
        {
            Plugin.ModLogger.LogWarning($"Ragnarök Rite: burning sky base environment \"{baseName}\" is not one the game knows.");
            return null;
        }
        EnvSetup sky = source.Clone();
        sky.m_name = CustomEnvironmentNames.Ragnarok;
        sky.m_ambColorNight = Ember(sky.m_ambColorNight);
        sky.m_ambColorDay = Ember(sky.m_ambColorDay);
        sky.m_fogColorNight = Ember(sky.m_fogColorNight);
        sky.m_fogColorMorning = Ember(sky.m_fogColorMorning);
        sky.m_fogColorDay = Ember(sky.m_fogColorDay);
        sky.m_fogColorEvening = Ember(sky.m_fogColorEvening);
        sky.m_fogColorSunNight = Ember(sky.m_fogColorSunNight);
        sky.m_fogColorSunMorning = Ember(sky.m_fogColorSunMorning);
        sky.m_fogColorSunDay = Ember(sky.m_fogColorSunDay);
        sky.m_fogColorSunEvening = Ember(sky.m_fogColorSunEvening);
        sky.m_sunColorNight = Ember(sky.m_sunColorNight);
        sky.m_sunColorMorning = Ember(sky.m_sunColorMorning);
        sky.m_sunColorDay = Ember(sky.m_sunColorDay);
        sky.m_sunColorEvening = Ember(sky.m_sunColorEvening);
        return sky;
    }

    // Keeps how bright a color is, turned to the orange and gold of a burning sky: brighter than the blood moon's red,
    // and never quite dark.
    private static Color Ember(Color color)
    {
        float light = (color.r + color.g + color.b) / 3f;
        return new Color(Mathf.Clamp01(light * 1.4f + 0.12f), Mathf.Clamp01(light * 0.8f + 0.05f), Mathf.Clamp01(light * 0.22f), color.a);
    }

    // Keeps how bright a color is, turned deep red.
    private static Color Blood(Color color)
    {
        float light = (color.r + color.g + color.b) / 3f;
        return new Color(Mathf.Clamp01(light * 1.35f + 0.08f), Mathf.Clamp01(light * 0.28f), Mathf.Clamp01(light * 0.24f), color.a);
    }

    private static bool IsOurs(EnvSetup env) => env.m_name.StartsWith("TempestRite_", System.StringComparison.Ordinal);
}
