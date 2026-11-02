using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Primal-Hearth Rite: wisp fountains and eternal pyres it reaches near the player glow brighter (Primal-Hearth
// Glow), in this game only: their lights shine stronger and further, their glowing parts (veins, embers) are brighter
// and their fires burn thicker. As usual again once the rite ends or no longer reaches them.
// RiteSpeedup tells which every second: Keep, then Sweep.
internal static class LureGlow
{
    private const string Feature = "Primal-Hearth glow";
    private const string Emission = "_EmissionColor";
    // Glowing parts dimmer than this aren't glowing at all.
    private const float Dark = 0.01f;

    private sealed class Bright
    {
        internal float Glow;
        internal Light[] Lights = [];
        internal float[] Intensities = [];
        internal float[] Ranges = [];
        // The game's flicker and light-distance scripts set the light from these every frame.
        internal LightFlicker?[] Flickers = [];
        internal LightLod?[] Lods = [];
        internal readonly List<(Renderer Renderer, Material[] Shared)> Renderers = [];
        internal readonly List<Material> Copies = [];
        internal ParticleSystem[] Systems = [];
        internal float[] Rates = [];
    }

    private static readonly Dictionary<ZNetView, Bright> Showing = [];
    private static readonly HashSet<ZNetView> Kept = [];
    private static readonly List<ZNetView> Gone = [];

    private static float Glow => Mathf.Max(1f, PluginConfig.PrimalHearthGlow.Value);

    internal static void Keep(ZNetView view) => Kept.Add(view);

    internal static void ClearSession()
    {
        foreach (Bright bright in Showing.Values)
            try { Restore(bright); }
            catch (Exception) { /* leaving the world: nothing to keep right */ }
        Showing.Clear();
        Kept.Clear();
    }

    internal static void Sweep()
    {
        if (PatchGuard.IsDisabled(Feature))
        {
            Kept.Clear();
            return;
        }
        try
        {
            float glow = Glow;
            if (glow <= 1.001f) Kept.Clear();
            Gone.Clear();
            foreach (KeyValuePair<ZNetView, Bright> entry in Showing)
                if (!entry.Key || !Kept.Contains(entry.Key) || !Mathf.Approximately(entry.Value.Glow, glow)) Gone.Add(entry.Key!);
            foreach (ZNetView view in Gone)
            {
                Restore(Showing[view]);
                Showing.Remove(view);
            }
            foreach (ZNetView view in Kept)
                if (view && !Showing.ContainsKey(view)) Showing[view] = Brighten(view, glow);
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            ClearSession();
        }
        finally { Kept.Clear(); }
    }

    private static Bright Brighten(ZNetView view, float glow)
    {
        Light[] lights = view.GetComponentsInChildren<Light>(true);
        ParticleSystem[] systems = view.GetComponentsInChildren<ParticleSystem>(true);
        Bright bright = new()
        {
            Glow = glow,
            Lights = lights,
            Intensities = new float[lights.Length],
            Ranges = new float[lights.Length],
            Flickers = new LightFlicker?[lights.Length],
            Lods = new LightLod?[lights.Length],
            Systems = systems,
            Rates = new float[systems.Length]
        };
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            LightFlicker? flicker = light.GetComponent<LightFlicker>();
            LightLod? lod = light.GetComponent<LightLod>();
            bright.Flickers[i] = flicker ? flicker : null;
            bright.Lods[i] = lod ? lod : null;
            bright.Intensities[i] = flicker ? flicker!.m_baseIntensity : light.intensity;
            bright.Ranges[i] = lod ? lod!.m_baseRange : light.range;
            if (flicker) flicker!.m_baseIntensity *= glow;
            else light.intensity *= glow;
            if (lod) lod!.m_baseRange *= Mathf.Sqrt(glow);
            light.range *= Mathf.Sqrt(glow);
        }
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem.EmissionModule emission = systems[i].emission;
            bright.Rates[i] = emission.rateOverTimeMultiplier;
            emission.rateOverTimeMultiplier *= glow;
        }
        // Each glowing material gets a brighter copy on this one only; the shared ones stay as they are.
        foreach (Renderer renderer in view.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer || renderer is ParticleSystemRenderer) continue;
            Material[] shared = renderer.sharedMaterials;
            Material[]? brighter = null;
            for (int i = 0; i < shared.Length; i++)
            {
                Material material = shared[i];
                if (!material || !material.HasProperty(Emission)) continue;
                Color color = material.GetColor(Emission);
                if (color.maxColorComponent < Dark) continue;
                Material copy = new(material);
                copy.SetColor(Emission, color * glow);
                bright.Copies.Add(copy);
                brighter ??= (Material[])shared.Clone();
                brighter[i] = copy;
            }
            if (brighter == null) continue;
            bright.Renderers.Add((renderer, shared));
            renderer.sharedMaterials = brighter;
        }
        return bright;
    }

    private static void Restore(Bright bright)
    {
        for (int i = 0; i < bright.Lights.Length; i++)
        {
            Light light = bright.Lights[i];
            if (!light) continue;
            if (bright.Flickers[i] is { } flicker && flicker) flicker.m_baseIntensity = bright.Intensities[i];
            else light.intensity = bright.Intensities[i];
            if (bright.Lods[i] is { } lod && lod)
            {
                lod.m_baseRange = bright.Ranges[i];
                light.range = Mathf.Min(light.range, bright.Ranges[i]);
            }
            else light.range = bright.Ranges[i];
        }
        for (int i = 0; i < bright.Systems.Length; i++)
        {
            if (!bright.Systems[i]) continue;
            ParticleSystem.EmissionModule emission = bright.Systems[i].emission;
            emission.rateOverTimeMultiplier = bright.Rates[i];
        }
        foreach ((Renderer renderer, Material[] shared) in bright.Renderers)
            if (renderer) renderer.sharedMaterials = shared;
        foreach (Material copy in bright.Copies)
            if (copy) UnityEngine.Object.Destroy(copy);
    }
}
