using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// While a Domain rite goes on, its area and its obliterator show it, in this game only (players with the mod; nothing
// is sent):
// - a ring in the rite's color lies on the ground along the area's edge, slowly breathing;
// - the obliterator glows and gives off rising motes in the colors of its rites.
// Rites can also show a short puff of the same motes elsewhere (a plant or station a rite speeds up).
internal static class DomainVisuals
{
    private const string Feature = "Domain effects";
    // Shown while the player is within this far beyond the area's edge.
    private const float ShowBeyond = 100f;
    private const float Lift = 0.3f;
    private const int EdgePoints = 96;
    private const float EdgeRefresh = 3f;
    private const float EdgeWidth = 0.14f;
    private const float EdgeBreath = 10f;
    // The light sits this high on the obliterator; the motes rise from around this height.
    private const float GlowHeight = 1.3f;
    private const float MoteHeight = 2.2f;

    private static readonly Dictionary<WeatherKind, Color> Colors = new()
    {
        [WeatherKind.Packward] = new Color(0.4f, 0.65f, 1f),
        [WeatherKind.Carrion] = new Color(0.6f, 0.9f, 0.2f),
        [WeatherKind.Verdant] = new Color(0.3f, 1f, 0.45f),
        [WeatherKind.VatBoil] = new Color(0.95f, 0.85f, 0.25f),
        [WeatherKind.PitchBellows] = new Color(1f, 0.38f, 0.12f),
        [WeatherKind.AmberMend] = new Color(1f, 0.58f, 0.15f),
        [WeatherKind.SovereignWard] = new Color(1f, 0.88f, 0.5f),
    };

    private static readonly WeatherKind[] Kinds =
        [
            WeatherKind.Packward, WeatherKind.Carrion, WeatherKind.Verdant, WeatherKind.VatBoil, WeatherKind.PitchBellows, WeatherKind.AmberMend,
            WeatherKind.SovereignWard
        ];

    private sealed class Shown
    {
        internal GameObject Root = null!;
        internal LineRenderer Edge = null!;
        internal Light Light = null!;
        internal Vector3 At;
        internal float Radius;
        internal float NextEdge;
    }

    private static readonly Dictionary<WeatherKind, Shown> Showing = [];
    private static readonly Vector3[] Points = new Vector3[EdgePoints];
    private static Material? _material;
    private static Material? _moteMaterial;

    internal static void ClearSession()
    {
        foreach (Shown shown in Showing.Values)
            if (shown.Root) UnityEngine.Object.Destroy(shown.Root);
        Showing.Clear();
        BurstsEnd.Clear();
    }

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { Run(); }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            ClearSession();
        }
    }

    private static void Run()
    {
        Player? player = Player.m_localPlayer;
        bool on = player && ZoneSystem.instance && PluginConfig.ShowDomainEffects.IsOn();
        for (int i = 0; i < Kinds.Length; i++)
        {
            WeatherKind kind = Kinds[i];
            bool wanted = on && WeatherState.IsActive(kind)
                && Utils.DistanceXZ(player!.transform.position, WeatherState.Position(kind)) <= PluginConfig.Radius(kind) + ShowBeyond;
            if (!wanted)
            {
                Hide(kind);
                continue;
            }
            Shown shown = Get(kind, i);
            Animate(shown, Colors[kind], i);
        }
    }

    // The rite's color (white for one without).
    internal static Color RiteColor(WeatherKind kind) => Colors.TryGetValue(kind, out Color color) ? color : Color.white;

    private static void Hide(WeatherKind kind)
    {
        if (!Showing.TryGetValue(kind, out Shown shown)) return;
        if (shown.Root) UnityEngine.Object.Destroy(shown.Root);
        Showing.Remove(kind);
    }

    private static Shown Get(WeatherKind kind, int index)
    {
        Vector3 at = WeatherState.Position(kind);
        float radius = PluginConfig.Radius(kind);
        if (Showing.TryGetValue(kind, out Shown shown) && shown.Root)
        {
            if (shown.At != at || shown.Radius != radius)
            {
                shown.At = at;
                shown.Radius = radius;
                shown.NextEdge = 0f;
                shown.Root.transform.position = at;
            }
            return shown;
        }

        Color color = Colors[kind];
        GameObject root = new($"TempestRite_Domain_{kind}");
        root.transform.position = at;
        shown = new Shown
        {
            Root = root,
            Edge = Line(root.transform, "Edge", EdgeWidth),
            At = at,
            Radius = radius
        };

        // Each rite's glow a little to its own side, so several on one obliterator show each color.
        float angle = index * Mathf.PI * 2f / Kinds.Length;
        Vector3 side = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.35f;
        GameObject glow = new("Glow");
        glow.transform.SetParent(root.transform, false);
        glow.transform.localPosition = side + Vector3.up * GlowHeight;
        shown.Light = glow.AddComponent<Light>();
        shown.Light.type = LightType.Point;
        shown.Light.color = color;
        shown.Light.range = 6f;
        shown.Light.intensity = 1.2f;
        shown.Light.shadows = LightShadows.None;
        Motes(root.transform, side + Vector3.up * MoteHeight, color);

        Showing[kind] = shown;
        return shown;
    }

    private static LineRenderer Line(Transform parent, string name, float width)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = width;
        line.numCornerVertices = 0;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.positionCount = 0;
        line.enabled = false;
        Material? material = Material();
        if (material) line.sharedMaterial = material;
        return line;
    }

    private static Material? Material()
    {
        if (_material) return _material;
        Shader? shader = Shader.Find("Sprites/Default");
        if (!shader) return null;
        _material = new Material(shader) { name = "TempestRite_DomainRing", renderQueue = 3100, hideFlags = HideFlags.HideAndDontSave };
        return _material;
    }

    private static void Animate(Shown shown, Color color, int index)
    {
        float t = Time.time;
        if (t >= shown.NextEdge)
        {
            shown.NextEdge = t + EdgeRefresh;
            Ring(shown.Edge, shown.At, shown.Radius, EdgePoints);
            shown.Edge.enabled = true;
        }
        float breath = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / EdgeBreath + index);
        Paint(shown.Edge, color, 0.3f + 0.2f * breath);
        if (shown.Light) shown.Light.intensity = 1.1f + 0.4f * Mathf.Sin(t * 0.8f + index * 1.7f);
    }

    private static void Paint(LineRenderer line, Color color, float alpha)
    {
        Color c = new(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        line.startColor = c;
        line.endColor = c;
    }

    // A circle on the ground (or the water) around center.
    private static void Ring(LineRenderer line, Vector3 center, float radius, int count)
    {
        float water = ZoneSystem.instance.m_waterLevel;
        for (int k = 0; k < count; k++)
        {
            float angle = k * Mathf.PI * 2f / count;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            point.y = (ZoneSystem.instance.GetGroundHeight(point, out float height) ? Mathf.Max(height, water) : center.y) + Lift;
            Points[k] = point;
        }
        line.positionCount = count;
        line.SetPositions(Points);
    }

    // ---- Motes on the obliterator ----

    // Soft round motes drifting up around the obliterator in the rite's color, fading in and out.
    private static void Motes(Transform parent, Vector3 at, Color color)
    {
        GameObject go = new("Motes");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        ParticleSystem? particles = MoteSystem(go, color, 1.2f, true);
        if (!particles) return;
        ParticleSystem.EmissionModule emission = particles!.emission;
        emission.rateOverTime = 16f;
        particles.Play();
    }

    private const int BurstMotes = 10;
    private const float BurstSeconds = 4.5f;
    // At most this many puffs at once; more are skipped until one is gone.
    private const int MaxBursts = 6;
    private static readonly Queue<float> BurstsEnd = new();

    // A short puff of the same motes rising around a point, gone after a few seconds (a Verdant plant growing). Scale
    // shrinks the motes, how far they spread and how many there are (0.5: crops). False when too many are showing.
    internal static bool Burst(Vector3 at, Color color, float scale = 1f)
    {
        if (PatchGuard.IsDisabled(Feature)) return false;
        while (BurstsEnd.Count > 0 && BurstsEnd.Peek() <= Time.time) BurstsEnd.Dequeue();
        if (BurstsEnd.Count >= MaxBursts) return false;
        BurstsEnd.Enqueue(Time.time + BurstSeconds);

        GameObject go = new("TempestRite_MoteBurst");
        go.transform.position = at;
        ParticleSystem? particles = MoteSystem(go, color, 0.6f * scale, false);
        if (particles)
        {
            ParticleSystem.MainModule main = particles!.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f * scale, 0.5f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f * scale, 0.22f * scale);
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            particles.Play();
            particles.Emit(Mathf.Max(3, Mathf.RoundToInt(BurstMotes * scale)));
        }
        UnityEngine.Object.Destroy(go, BurstSeconds);
        return true;
    }

    // Soft motes in a color drifting up from within a sphere, fading in and out; not emitting yet.
    private static ParticleSystem? MoteSystem(GameObject go, Color color, float radius, bool loop)
    {
        Material? material = MoteMaterial();
        if (!material) return null;
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = loop;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
        main.gravityModifier = -0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 150;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.4f;

        Gradient fade = new();
        fade.SetKeys(
            [new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f)],
            [new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f)]);
        ParticleSystem.ColorOverLifetimeModule overLife = particles.colorOverLifetime;
        overLife.enabled = true;
        overLife.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    // A soft white dot, tinted by each mote's color.
    private static Material? MoteMaterial()
    {
        if (_moteMaterial) return _moteMaterial;
        Shader? shader = Shader.Find("Sprites/Default");
        if (!shader) return null;
        const int size = 32;
        Texture2D dot = new(size, size, TextureFormat.RGBA32, false)
        {
            name = "TempestRite_Mote",
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float glow = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color(1f, 1f, 1f, glow * glow);
            }
        dot.SetPixels(pixels);
        dot.Apply(false, true);
        _moteMaterial = new Material(shader)
        {
            name = "TempestRite_Motes",
            mainTexture = dot,
            renderQueue = 3100,
            hideFlags = HideFlags.HideAndDontSave
        };
        return _moteMaterial;
    }
}
