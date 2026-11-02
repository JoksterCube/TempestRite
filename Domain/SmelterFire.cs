using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Pitch-Bellows Rite: the fire of the smelters, kilns and furnaces it reaches near the player takes on a tint
// (Pitch-Bellows Fire Tint), in this game only. The Taproot-Surge Rite does the same to eitr refineries (Taproot-Surge
// Fire Tint), and their machinery moves as many times as fast as they work. Their flames, sparks and glow lean toward the tint's hue, as far as its
// alpha says (0: not at all, 1: fully that hue), keeping how bright and see-through each part is; gray smoke stays gray.
// They burn (and move) as usual again once the rite ends or no longer reaches them.
//
// RiteSpeedup tells which ones every second: Keep for each, then Sweep.
internal static class SmelterFire
{
    private const string Feature = "Rite station fire";
    // Colors with less saturation than this (smoke, ash) keep theirs.
    private const float Gray = 0.12f;
    // Tints this faint change nothing.
    private const float NoTint = 0.01f;

    private sealed class Tinted
    {
        internal Color Tint;
        internal bool Recolor;
        internal float Speed = 1f;
        internal Animator[] Animators = [];
        internal float[] AnimatorSpeeds = [];
        internal ParticleSystem[] Systems = [];
        internal ParticleSystem.MinMaxGradient[] Start = [];
        internal ParticleSystem.MinMaxGradient[] OverLife = [];
        internal Light[] Lights = [];
        internal Color[] LightColors = [];
    }

    private static readonly Dictionary<ZNetView, Tinted> Showing = [];
    private static readonly Dictionary<ZNetView, (Color Tint, bool Recolor, float Speed)> Kept = [];
    private static readonly List<ZNetView> Gone = [];

    internal static void ClearSession()
    {
        foreach (KeyValuePair<ZNetView, Tinted> entry in Showing)
            try { Restore(entry.Value); }
            catch (Exception) { /* leaving the world: nothing to keep right */ }
        Showing.Clear();
        Kept.Clear();
    }

    // This one's fire leans toward the tint and its animations play this many times as fast. Recolor: its flames and
    // lights take the tint itself (the eitr refinery's flames are magenta in their texture, so a hue lean shows nothing);
    // smoke stays as it is.
    internal static void Keep(ZNetView view, Color tint, bool recolor, float speed) => Kept[view] = (tint, recolor, speed);

    internal static void Sweep()
    {
        if (PatchGuard.IsDisabled(Feature))
        {
            Kept.Clear();
            return;
        }
        try
        {
            Gone.Clear();
            foreach (KeyValuePair<ZNetView, Tinted> entry in Showing)
                if (!entry.Key || !Kept.TryGetValue(entry.Key, out (Color Tint, bool Recolor, float Speed) look) || entry.Value.Tint != look.Tint
                    || entry.Value.Recolor != look.Recolor || !Mathf.Approximately(entry.Value.Speed, look.Speed)) Gone.Add(entry.Key!);
            foreach (ZNetView view in Gone)
            {
                Restore(Showing[view]);
                Showing.Remove(view);
            }
            foreach (KeyValuePair<ZNetView, (Color Tint, bool Recolor, float Speed)> entry in Kept)
                if (entry.Key && !Showing.ContainsKey(entry.Key) && (entry.Value.Tint.a > NoTint || entry.Value.Speed > 1f))
                    Showing[entry.Key] = Tint(entry.Key, entry.Value.Tint, entry.Value.Recolor, entry.Value.Speed);
            // The game (switching worn and new looks, waking it up) can set an animator's speed back: set it again.
            foreach (Tinted tinted in Showing.Values)
                for (int i = 0; i < tinted.Animators.Length; i++)
                    if (tinted.Animators[i] && !Mathf.Approximately(tinted.Animators[i].speed, tinted.AnimatorSpeeds[i] * tinted.Speed))
                        tinted.Animators[i].speed = tinted.AnimatorSpeeds[i] * tinted.Speed;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            ClearSession();
        }
        finally { Kept.Clear(); }
    }

    private static Tinted Tint(ZNetView view, Color tint, bool recolor, float speed)
    {
        ParticleSystem[] systems = Array.FindAll(view.GetComponentsInChildren<ParticleSystem>(true),
            particles => !recolor || particles.name.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) < 0);
        Light[] lights = view.GetComponentsInChildren<Light>(true);
        Animator[] animators = speed > 1f ? view.GetComponentsInChildren<Animator>(true) : [];
        Tinted tinted = new()
        {
            Tint = tint,
            Recolor = recolor,
            Speed = speed,
            Animators = animators,
            AnimatorSpeeds = new float[animators.Length],
            Systems = systems,
            Start = new ParticleSystem.MinMaxGradient[systems.Length],
            OverLife = new ParticleSystem.MinMaxGradient[systems.Length],
            Lights = lights,
            LightColors = new Color[lights.Length]
        };
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem.MainModule main = systems[i].main;
            tinted.Start[i] = main.startColor;
            main.startColor = Shift(main.startColor, tint, recolor);
            ParticleSystem.ColorOverLifetimeModule overLife = systems[i].colorOverLifetime;
            tinted.OverLife[i] = overLife.color;
            if (overLife.enabled) overLife.color = Shift(overLife.color, tint, recolor);
        }
        for (int i = 0; i < lights.Length; i++)
        {
            tinted.LightColors[i] = lights[i].color;
            lights[i].color = Shift(lights[i].color, tint, recolor);
        }
        for (int i = 0; i < animators.Length; i++)
        {
            tinted.AnimatorSpeeds[i] = animators[i].speed;
            animators[i].speed *= speed;
        }
        return tinted;
    }

    private static void Restore(Tinted tinted)
    {
        for (int i = 0; i < tinted.Systems.Length; i++)
        {
            ParticleSystem particles = tinted.Systems[i];
            if (!particles) continue;
            ParticleSystem.MainModule main = particles.main;
            main.startColor = tinted.Start[i];
            ParticleSystem.ColorOverLifetimeModule overLife = particles.colorOverLifetime;
            if (overLife.enabled) overLife.color = tinted.OverLife[i];
        }
        for (int i = 0; i < tinted.Lights.Length; i++)
            if (tinted.Lights[i]) tinted.Lights[i].color = tinted.LightColors[i];
        for (int i = 0; i < tinted.Animators.Length; i++)
            if (tinted.Animators[i]) tinted.Animators[i].speed = tinted.AnimatorSpeeds[i];
    }

    private static ParticleSystem.MinMaxGradient Shift(ParticleSystem.MinMaxGradient gradient, Color tint, bool recolor) =>
        gradient.mode switch
        {
            ParticleSystemGradientMode.Color => new ParticleSystem.MinMaxGradient(Shift(gradient.color, tint, recolor)),
            ParticleSystemGradientMode.TwoColors =>
                new ParticleSystem.MinMaxGradient(Shift(gradient.colorMin, tint, recolor), Shift(gradient.colorMax, tint, recolor)),
            ParticleSystemGradientMode.Gradient => new ParticleSystem.MinMaxGradient(Shift(gradient.gradient, tint, recolor)),
            ParticleSystemGradientMode.TwoGradients =>
                new ParticleSystem.MinMaxGradient(Shift(gradient.gradientMin, tint, recolor), Shift(gradient.gradientMax, tint, recolor)),
            ParticleSystemGradientMode.RandomColor =>
                new ParticleSystem.MinMaxGradient(Shift(gradient.gradient, tint, recolor)) { mode = ParticleSystemGradientMode.RandomColor },
            _ => gradient
        };

    private static Gradient Shift(Gradient gradient, Color tint, bool recolor)
    {
        if (gradient == null) return gradient!;
        GradientColorKey[] keys = gradient.colorKeys;
        for (int i = 0; i < keys.Length; i++) keys[i].color = Shift(keys[i].color, tint, recolor);
        Gradient shifted = new() { mode = gradient.mode };
        shifted.SetKeys(keys, gradient.alphaKeys);
        return shifted;
    }

    private static Color Shift(Color original, Color tint, bool recolor) => recolor ? Recolored(original, tint) : Shift(original, tint);

    // Turned toward the tint itself by its alpha, whatever color it was, keeping how see-through it is. The texture still
    // multiplies it: magenta flames times a deep blue come out blue.
    private static Color Recolored(Color original, Color tint)
    {
        if (tint.a <= NoTint) return original;
        Color shifted = Color.Lerp(original, new Color(tint.r, tint.g, tint.b, original.a), Mathf.Clamp01(tint.a));
        shifted.a = original.a;
        return shifted;
    }

    // Leaned toward the tint's hue by its alpha, keeping brightness and see-through; grays (smoke) as they are.
    internal static Color Shift(Color original, Color tint)
    {
        Color.RGBToHSV(original, out _, out float saturation, out float value);
        if (saturation < Gray || tint.a <= NoTint) return original;
        Color.RGBToHSV(tint, out float hue, out float tintSaturation, out _);
        Color full = Color.HSVToRGB(hue, Mathf.Max(saturation, tintSaturation), value, true);
        Color shifted = Color.Lerp(original, full, Mathf.Clamp01(tint.a));
        shifted.a = original.a;
        return shifted;
    }
}
