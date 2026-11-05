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
// - TempestRite_HoardBane: a game environment (the mist by default) dimmed and tinted violet, the gloom of a cursed
//   hoard (the Hoard-Bane Rite).
// - TempestRite_SwampMist: the Swamp's rain under Misty's thick fog and drifting mist (the Shrouding Rite in the Swamp).
internal static class CustomEnvironments
{
    private const string AuroraBase = "Clear";
    private const string SwampMistRain = "SwampRain";
    private const string SwampMistFog = "Misty";

    private static EnvMan? _madeFor;
    private static string _bloodmoonBase = string.Empty;
    private static string _ragnarokBase = string.Empty;
    private static string _hoardBaneBase = string.Empty;

    // Makes them once per environment manager (one per world), and the blood moon again if its base changes.
    internal static void Ensure(EnvMan envMan)
    {
        string bloodmoonBase = PluginConfig.BloodmoonBase.Value;
        string ragnarokBase = PluginConfig.RagnarokSkyBase.Value;
        string hoardBaneBase = PluginConfig.HoardBaneSkyBase.Value;
        if (_madeFor == envMan && bloodmoonBase == _bloodmoonBase && ragnarokBase == _ragnarokBase && hoardBaneBase == _hoardBaneBase) return;
        _madeFor = envMan;
        _bloodmoonBase = bloodmoonBase;
        _ragnarokBase = ragnarokBase;
        _hoardBaneBase = hoardBaneBase;
        Replace(envMan, MakeAurora(envMan));
        Replace(envMan, MakeBloodmoon(envMan, bloodmoonBase));
        Replace(envMan, MakeRagnarok(envMan, ragnarokBase));
        Replace(envMan, MakeHoardBane(envMan, hoardBaneBase));
        Replace(envMan, MakeSwampMist(envMan));
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

    private static EnvSetup? MakeHoardBane(EnvMan envMan, string baseName)
    {
        EnvSetup? source = envMan.GetEnv(baseName);
        if (source == null || IsOurs(source))
        {
            Plugin.ModLogger.LogWarning($"Hoard-Bane Rite: cursed sky base environment \"{baseName}\" is not one the game knows.");
            return null;
        }
        EnvSetup sky = source.Clone();
        sky.m_name = CustomEnvironmentNames.HoardBane;
        sky.m_ambColorNight = Curse(sky.m_ambColorNight);
        sky.m_ambColorDay = Curse(sky.m_ambColorDay);
        sky.m_fogColorNight = Curse(sky.m_fogColorNight);
        sky.m_fogColorMorning = Curse(sky.m_fogColorMorning);
        sky.m_fogColorDay = Curse(sky.m_fogColorDay);
        sky.m_fogColorEvening = Curse(sky.m_fogColorEvening);
        sky.m_fogColorSunNight = Curse(sky.m_fogColorSunNight);
        sky.m_fogColorSunMorning = Curse(sky.m_fogColorSunMorning);
        sky.m_fogColorSunDay = Curse(sky.m_fogColorSunDay);
        sky.m_fogColorSunEvening = Curse(sky.m_fogColorSunEvening);
        // The sun a sickly gold through the gloom.
        sky.m_sunColorNight = Gilt(sky.m_sunColorNight);
        sky.m_sunColorMorning = Gilt(sky.m_sunColorMorning);
        sky.m_sunColorDay = Gilt(sky.m_sunColorDay);
        sky.m_sunColorEvening = Gilt(sky.m_sunColorEvening);
        sky.m_lightIntensityDay *= 0.75f;
        sky.m_fogDensityNight *= 1.3f;
        sky.m_fogDensityMorning *= 1.3f;
        sky.m_fogDensityDay *= 1.3f;
        sky.m_fogDensityEvening *= 1.3f;
        return sky;
    }

    private static EnvSetup? MakeSwampMist(EnvMan envMan)
    {
        EnvSetup? rain = envMan.GetEnv(SwampMistRain);
        EnvSetup? fog = envMan.GetEnv(SwampMistFog);
        if (rain == null || fog == null)
        {
            Plugin.ModLogger.LogWarning($"Shrouding Rite: \"{SwampMistRain}\" or \"{SwampMistFog}\" is not an environment the game knows; no swamp mist.");
            return null;
        }
        EnvSetup mist = rain.Clone();
        mist.m_name = CustomEnvironmentNames.SwampMist;
        // The thicker fog of the two at each time of day.
        mist.m_fogDensityNight = Mathf.Max(rain.m_fogDensityNight, fog.m_fogDensityNight);
        mist.m_fogDensityMorning = Mathf.Max(rain.m_fogDensityMorning, fog.m_fogDensityMorning);
        mist.m_fogDensityDay = Mathf.Max(rain.m_fogDensityDay, fog.m_fogDensityDay);
        mist.m_fogDensityEvening = Mathf.Max(rain.m_fogDensityEvening, fog.m_fogDensityEvening);
        // The rain's particles and the mist's together: the game only switches them on and off.
        var particles = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject[]? list in new[] { rain.m_psystems, fog.m_psystems })
            if (list != null)
                foreach (GameObject particle in list)
                    if (particle && !particles.Contains(particle)) particles.Add(particle);
        mist.m_psystems = particles.ToArray();
        return mist;
    }

    // Keeps how bright a color is, turned deep violet, and a little darker.
    private static Color Curse(Color color)
    {
        float light = (color.r + color.g + color.b) / 3f;
        return new Color(Mathf.Clamp01(light * 0.85f + 0.06f), Mathf.Clamp01(light * 0.4f + 0.02f), Mathf.Clamp01(light * 1.25f + 0.1f), color.a);
    }

    // Keeps how bright a color is, turned pale gold.
    private static Color Gilt(Color color)
    {
        float light = (color.r + color.g + color.b) / 3f;
        return new Color(Mathf.Clamp01(light * 1.2f), Mathf.Clamp01(light * 0.95f), Mathf.Clamp01(light * 0.45f), color.a);
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
