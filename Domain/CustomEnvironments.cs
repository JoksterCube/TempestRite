using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// Skies the game doesn't have, made from its own environments and added to its list, so rites (and the env console
// command) can use them by name:
// - TempestRite_Aurora: a clear sky with the strongest auroras any of the game's environments has, at night, and calm,
//   quiet wind (the Aurora Rite).
// - TempestRite_Bloodmoon: a game environment (Eikthyr's by default) tinted blood red (the Crimson Rite).
internal static class CustomEnvironments
{
    private const string AuroraBase = "Clear";

    private static EnvMan? _madeFor;
    private static string _bloodmoonBase = string.Empty;

    // Makes them once per environment manager (one per world), and the blood moon again if its base changes.
    internal static void Ensure(EnvMan envMan)
    {
        string bloodmoonBase = PluginConfig.BloodmoonBase.Value;
        if (_madeFor == envMan && bloodmoonBase == _bloodmoonBase) return;
        _madeFor = envMan;
        _bloodmoonBase = bloodmoonBase;
        Replace(envMan, MakeAurora(envMan));
        Replace(envMan, MakeBloodmoon(envMan, bloodmoonBase));
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

    // Keeps how bright a color is, turned deep red.
    private static Color Blood(Color color)
    {
        float light = (color.r + color.g + color.b) / 3f;
        return new Color(Mathf.Clamp01(light * 1.35f + 0.08f), Mathf.Clamp01(light * 0.28f), Mathf.Clamp01(light * 0.24f), color.a);
    }

    private static bool IsOurs(EnvSetup env) => env.m_name.StartsWith("TempestRite_", System.StringComparison.Ordinal);
}
