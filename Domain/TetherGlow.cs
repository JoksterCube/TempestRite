using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Tether-Lock Rite's look: while a blessed player holds a harpoon, it glows abyssal blue like a high-level weapon - the
// harpoon itself lit, a few sparks lifting off it and a soft light on it - and its rope itself glows blue, with
// sparks drifting off it
// (fishing lines stay as they are). The game shows every player's held item and line from synced data, and the
// blessing is read off each player, so every game with the mod sees it.
internal static class TetherGlow
{
    private const string Feature = "Tether-Lock glow";
    private const string Name = "TempestRite_TetherGlow";
    private const float CheckEvery = 0.25f;
    private const float SparkRate = 14f;
    private const float LightIntensity = 0.9f;
    private const float LightRange = 2.2f;

    private static readonly Color Blue = new(0.25f, 0.6f, 1f);
    private static readonly Color Pale = new(0.7f, 0.9f, 1f);

    private sealed class Held
    {
        internal Player? Player;
        internal float NextCheck;
        internal GameObject? Item;
        internal GameObject? Glow;
        internal Light? Light;
        internal readonly List<(Renderer Renderer, Material[] Shared)> Renderers = [];
        internal readonly List<(Material Copy, Color Emission)> Copies = [];
        internal readonly List<Material> Made = [];
    }

    private const string Emission = "_EmissionColor";
    private const string Tint = "_Color";
    // How strongly the harpoon itself glows blue, breathing between these.
    private const float GlowLow = 0.12f;
    private const float GlowHigh = 0.25f;

    private static readonly Dictionary<VisEquipment, Held> Players = [];
    private static readonly Dictionary<int, bool> Harpoons = [];
    private static readonly List<VisEquipment> Gone = [];
    private static float _nextPrune;

    internal static void ClearSession()
    {
        foreach (Held held in Players.Values)
            try { Remove(held); }
            catch (Exception) { /* leaving the world: nothing to keep right */ }
        Players.Clear();
        Harpoons.Clear();
        foreach (Rope rope in Ropes.Values)
            try
            {
                Dim(rope);
                if (rope.Sparks) UnityEngine.Object.Destroy(rope.Sparks!.gameObject);
            }
            catch (Exception) { /* leaving the world: nothing to keep right */ }
        Ropes.Clear();
    }

    // Each frame, after the game updated what this character shows in its hands.
    internal static void Follow(VisEquipment visual)
    {
        if (!visual.m_isPlayer || PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Prune();
            if (!Players.TryGetValue(visual, out Held held))
            {
                held = new Held { Player = visual.GetComponent<Player>() };
                Players[visual] = held;
            }
            if (Time.time >= held.NextCheck)
            {
                held.NextCheck = Time.time + CheckEvery;
                GameObject? item = visual.m_rightItemInstance;
                bool show = item && IsHarpoon(visual.m_currentRightItemHash) && BlessingPerks.Tethered(held.Player);
                if (!show) Remove(held);
                else if (held.Item != item || !held.Glow) Attach(held, item!);
            }
            if (held.Glow) Pulse(held, 0.5f + 0.5f * Mathf.Sin(Time.time * 2.5f));
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            ClearSession();
        }
    }

    // Any weapon whose hit harpoons (the Abyssal Harpoon, and modded ones like it).
    private static bool IsHarpoon(int hash)
    {
        if (hash == 0) return false;
        if (Harpoons.TryGetValue(hash, out bool harpoon)) return harpoon;
        GameObject? prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(hash) : null;
        ItemDrop? drop = prefab ? prefab!.GetComponent<ItemDrop>() : null;
        harpoon = drop && drop!.m_itemData?.m_shared?.m_attackStatusEffect is SE_Harpooned;
        Harpoons[hash] = harpoon;
        return harpoon;
    }

    // Sparks from all along the harpoon's body (its biggest mesh), drifting off and left behind as it moves, and a soft
    // light in its middle.
    private static void Attach(Held held, GameObject item)
    {
        Remove(held);
        held.Item = item;
        MeshFilter? body = null;
        float biggest = 0f;
        foreach (MeshFilter filter in item.GetComponentsInChildren<MeshFilter>())
        {
            if (!filter.sharedMesh || !filter.GetComponent<MeshRenderer>()) continue;
            float size = filter.sharedMesh.bounds.size.sqrMagnitude;
            if (size <= biggest) continue;
            biggest = size;
            body = filter;
        }
        Transform at = body ? body!.transform : item.transform;
        Bounds bounds = body ? body!.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one * 0.3f);

        GameObject glow = new(Name);
        glow.transform.SetParent(at, false);
        glow.transform.localPosition = bounds.center;
        held.Glow = glow;
        Sparks(glow, bounds.size);

        held.Light = glow.AddComponent<Light>();
        held.Light.type = LightType.Point;
        held.Light.color = Blue;
        held.Light.range = LightRange;
        held.Light.intensity = LightIntensity;
        held.Light.shadows = LightShadows.None;
        Brighten(held, item);
    }

    // The harpoon itself lit blue: each of its materials gets a glowing copy on this one only (the shared ones stay as
    // they are). A material that can't glow is tinted blue instead.
    private static void Brighten(Held held, GameObject item)
    {
        foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer || renderer is ParticleSystemRenderer || renderer.transform.IsChildOf(held.Glow!.transform)) continue;
            Material[] shared = renderer.sharedMaterials;
            Material[] lit = (Material[])shared.Clone();
            bool changed = false;
            for (int i = 0; i < shared.Length; i++)
            {
                Material material = shared[i];
                if (!material) continue;
                Material copy = new(material);
                if (copy.HasProperty(Emission))
                {
                    copy.EnableKeyword("_EMISSION");
                    held.Copies.Add((copy, material.GetColor(Emission)));
                }
                else if (copy.HasProperty(Tint)) copy.SetColor(Tint, Color.Lerp(material.GetColor(Tint), Pale, 0.2f));
                else
                {
                    UnityEngine.Object.Destroy(copy);
                    continue;
                }
                held.Made.Add(copy);
                lit[i] = copy;
                changed = true;
            }
            if (!changed) continue;
            held.Renderers.Add((renderer, shared));
            renderer.sharedMaterials = lit;
        }
    }

    // The light and the harpoon's glow breathe together (0 to 1).
    private static void Pulse(Held held, float breath)
    {
        if (held.Light) held.Light!.intensity = LightIntensity * (0.75f + 0.25f * breath);
        Color glow = Blue * Mathf.Lerp(GlowLow, GlowHigh, breath);
        foreach ((Material copy, Color own) in held.Copies)
            if (copy) copy.SetColor(Emission, own + glow);
    }

    // ---- The line: the rope itself glowing blue, and sparks drifting off it ----
    // How strongly the rope glows blue, breathing between these.
    private const float RopeGlowLow = 0.25f;
    private const float RopeGlowHigh = 0.45f;
    // A rope whose material can't glow is tinted this far toward blue instead.
    private const float RopeTint = 0.35f;
    private const float RopeSparksPerMeter = 1.2f;
    private const float RopeSparksMax = 25f;

    private sealed class Rope
    {
        internal bool Fishing;
        internal bool Blessed;
        internal float NextCheck;
        internal float Owed;
        internal LineRenderer? Line;
        internal Material? Original;
        internal Material? Lit;
        internal bool Glows;
        internal Color OwnEmission;
        internal Color StartColor;
        internal Color EndColor;
        internal bool Tinted;
        internal ParticleSystem? Sparks;
    }

    private static readonly Dictionary<LineConnect, Rope> Ropes = [];
    private static readonly List<LineConnect> RopesGone = [];
    private static Vector3[] _points = new Vector3[32];

    // Each frame, after the game placed the line.
    internal static void FollowLine(LineConnect connect)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Prune();
            LineRenderer line = connect.m_lineRenderer;
            if (!line) return;
            if (!Ropes.TryGetValue(connect, out Rope rope))
            {
                rope = new Rope { Fishing = connect.GetComponentInParent<FishingFloat>(), Line = line };
                Ropes[connect] = rope;
            }
            if (rope.Fishing) return;
            if (Time.time >= rope.NextCheck)
            {
                rope.NextCheck = Time.time + CheckEvery;
                rope.Blessed = BlessingPerks.Tethered(Peer(connect));
            }
            if (!rope.Blessed || !line.enabled || line.positionCount < 2)
            {
                Dim(rope);
                rope.Owed = 0f;
                return;
            }
            if (!rope.Lit && !rope.Tinted) Light(rope, line);
            Shine(rope, line);
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            ClearSession();
        }
    }

    private static Player? Peer(LineConnect connect)
    {
        ZDO? zdo = connect.m_nview && connect.m_nview.IsValid() ? connect.m_nview.GetZDO() : null;
        if (zdo == null || !ZNetScene.instance) return null;
        GameObject? peer = ZNetScene.instance.FindInstance(zdo.GetZDOID(connect.m_linePeerID));
        return peer ? peer!.GetComponent<Player>() : null;
    }

    // The rope gets a glowing copy of its material, on this line only. One whose material can't glow, or be tinted, is
    // tinted blue through its line colors instead.
    private static void Light(Rope rope, LineRenderer line)
    {
        Material original = line.sharedMaterial;
        if (original)
        {
            Material copy = new(original) { name = original.name + " (Tether-Lock)" };
            if (copy.HasProperty(Emission))
            {
                copy.EnableKeyword("_EMISSION");
                rope.Glows = true;
                rope.OwnEmission = original.GetColor(Emission);
            }
            else if (copy.HasProperty(Tint)) copy.SetColor(Tint, Color.Lerp(original.GetColor(Tint), Pale, RopeTint));
            else
            {
                UnityEngine.Object.Destroy(copy);
                copy = null!;
            }
            if (copy)
            {
                rope.Original = original;
                rope.Lit = copy;
                line.sharedMaterial = copy;
            }
        }
        if (!rope.Lit)
        {
            rope.StartColor = line.startColor;
            rope.EndColor = line.endColor;
            line.startColor = Color.Lerp(rope.StartColor, Pale, RopeTint);
            line.endColor = Color.Lerp(rope.EndColor, Pale, RopeTint);
            rope.Tinted = true;
        }

        if (rope.Sparks) return;
        GameObject sparks = new("TempestRite_TetherRopeSparks");
        sparks.transform.SetParent(line.transform, false);
        rope.Sparks = Sparks(sparks, Vector3.zero, 0f);
        if (!rope.Sparks) return;
        ParticleSystem.ShapeModule shape = rope.Sparks!.shape;
        shape.enabled = false;
        ParticleSystem.MainModule main = rope.Sparks.main;
        main.maxParticles = 60;
    }

    // The rope as the game made it again.
    private static void Dim(Rope rope)
    {
        LineRenderer? line = rope.Line;
        if (rope.Lit)
        {
            if (line && line!.sharedMaterial == rope.Lit) line.sharedMaterial = rope.Original;
            UnityEngine.Object.Destroy(rope.Lit);
        }
        rope.Lit = null;
        rope.Original = null;
        rope.Glows = false;
        if (rope.Tinted && line)
        {
            line!.startColor = rope.StartColor;
            line.endColor = rope.EndColor;
        }
        rope.Tinted = false;
    }

    private static void Shine(Rope rope, LineRenderer line)
    {
        if (rope.Glows && rope.Lit)
            rope.Lit!.SetColor(Emission, rope.OwnEmission + Blue * Mathf.Lerp(RopeGlowLow, RopeGlowHigh, 0.5f + 0.5f * Mathf.Sin(Time.time * 2.5f)));

        if (!rope.Sparks) return;
        int count = line.positionCount;
        if (_points.Length < count) _points = new Vector3[Mathf.NextPowerOfTwo(count)];
        line.GetPositions(_points);
        if (!line.useWorldSpace)
            for (int i = 0; i < count; i++) _points[i] = line.transform.TransformPoint(_points[i]);
        float length = 0f;
        for (int i = 1; i < count; i++) length += Vector3.Distance(_points[i - 1], _points[i]);
        rope.Owed += Mathf.Min(RopeSparksMax, length * RopeSparksPerMeter) * Time.deltaTime;
        for (; rope.Owed >= 1f; rope.Owed -= 1f)
        {
            int i = UnityEngine.Random.Range(0, count - 1);
            rope.Sparks!.Emit(new ParticleSystem.EmitParams
            {
                position = Vector3.Lerp(_points[i], _points[i + 1], UnityEngine.Random.value),
                velocity = UnityEngine.Random.insideUnitSphere * 0.08f + Vector3.up * 0.05f,
                applyShapeToPosition = false
            }, 1);
        }
    }

    private static ParticleSystem? Sparks(GameObject glow, Vector3 size, float rate = SparkRate)
    {
        Material? material = DomainVisuals.EmberMaterial();
        if (!material) return null;
        ParticleSystem particles = glow.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startColor = new ParticleSystem.MinMaxGradient(Blue, Pale);
        main.gravityModifier = -0.02f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        // The box follows the harpoon's own size and scale; the sparks keep theirs.
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.maxParticles = 40;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = size;

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.6f;

        Gradient fade = new();
        fade.SetKeys(
            [new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f)],
            [new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.15f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f)]);
        ParticleSystem.ColorOverLifetimeModule overLife = particles.colorOverLifetime;
        overLife.enabled = true;
        overLife.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystemRenderer renderer = glow.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particles.Play();
        return particles;
    }

    private static void Remove(Held held)
    {
        if (held.Glow) UnityEngine.Object.Destroy(held.Glow);
        foreach ((Renderer renderer, Material[] shared) in held.Renderers)
            if (renderer) renderer.sharedMaterials = shared;
        held.Renderers.Clear();
        foreach (Material copy in held.Made)
            if (copy) UnityEngine.Object.Destroy(copy);
        held.Made.Clear();
        held.Copies.Clear();
        held.Glow = null;
        held.Light = null;
        held.Item = null;
    }

    // Players who left took their glow with them; forget them.
    private static void Prune()
    {
        if (Time.time < _nextPrune) return;
        _nextPrune = Time.time + 10f;
        Gone.Clear();
        foreach (VisEquipment visual in Players.Keys)
            if (!visual) Gone.Add(visual!);
        foreach (VisEquipment visual in Gone) Players.Remove(visual);
        RopesGone.Clear();
        foreach (LineConnect connect in Ropes.Keys)
            if (!connect) RopesGone.Add(connect!);
        foreach (LineConnect connect in RopesGone) Ropes.Remove(connect);
    }
}
