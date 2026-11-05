using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// While a Domain rite goes on, its area and its obliterator show it, in this game only (players with the mod; nothing
// is sent):
// - the area edge of the obliterator the player is in (or for a moment the one they just left, see DomainBorder) is
//   marked the way the game marks a ward's: a ring of the ward's own glowing dashes on
//   the ground and on every built floor over it (see FloorDashes), slowly turning, around the widest rite going there (a thin line instead, if the ward's marker can't be
//   found);
// - the obliterator glows and gives off rising motes in the colors of its rites, each rite in its own shape so several
//   at once can be told apart: Verdant leaves, Vat-Boil bubbles, Pitch-Bellows embers (and now and then a shower of the
//   forge's hammer sparks), Kindred hearts, Harrow-Spur little ghosts, Amber-Mend amber drops, Sovereign Ward stars, Hearth-Haul feathers, Aether-Clear wisps circling it, Elder-Sprout little sprouts, Packward wolf paw prints,
//   Carrion flies buzzing about, Sentry-Sight ballista bolts, Taproot-Surge glowing sap drops,
//   Pitch-Seal blobs of pitch with a coin stuck in them, Primal-Hearth violet flames, Hearth-Kindle crescent moons, round motes for the rest. The more rites at once, the fewer each gives.
// Rites also show short puffs of the same shapes elsewhere (a plant, station or animal a rite speeds up; the Plume-Ward
// Rite's tufts of down and the Morgen-Pouch Rite's wisps are blessings that only puff), and players
// carrying too much under the Hearth-Haul Rite give off its feathers (theirs is seen from the game's own synced
// "encumbered" walk, so other players see it too).
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
    // Dashes on the edge: about as far apart as on a ward's, within these counts.
    private const int MinDashes = 24;
    private const int MaxDashes = 160;
    // The light sits this high on the obliterator; the motes rise from around this height.
    private const float GlowHeight = 1.3f;
    private const float MoteHeight = 2.2f;
    // Motes a second from one rite; with more than two at once, each gives its share of two rites' worth.
    private const float MoteRate = 3f;
    // Pitch-Bellows: the forge's hammer sparks, this many times as big, every this many seconds.
    private const float SparkScale = 2.5f;
    private const float SparkEveryMin = 2.5f;
    private const float SparkEveryMax = 6f;

    private enum Shape { Dot, Leaf, Bubble, Ember, Star, Heart, Amber, Paw, Fly, Ghost, Feather, Wisp, Sprout, Down, Bolt, Drop, Tar, Flame, Rested, Moon }

    private static readonly Dictionary<WeatherKind, Color> Colors = new()
    {
        [WeatherKind.Packward] = new Color(0.4f, 0.65f, 1f),
        [WeatherKind.Carrion] = new Color(0.6f, 0.9f, 0.2f),
        [WeatherKind.Verdant] = new Color(0.3f, 1f, 0.45f),
        [WeatherKind.VatBoil] = new Color(0.95f, 0.85f, 0.25f),
        [WeatherKind.PitchBellows] = new Color(1f, 0.38f, 0.12f),
        [WeatherKind.Kindred] = new Color(1f, 0.45f, 0.62f),
        [WeatherKind.HarrowSpur] = new Color(0.55f, 0.95f, 0.85f),
        [WeatherKind.AmberMend] = new Color(1f, 0.58f, 0.15f),
        [WeatherKind.SovereignWard] = new Color(1f, 0.88f, 0.5f),
        [WeatherKind.HearthHaul] = new Color(0.9f, 0.68f, 0.45f),
        [WeatherKind.AetherClear] = new Color(0.6f, 0.92f, 1f),
        [WeatherKind.ElderSprout] = new Color(0.78f, 0.95f, 0.55f),
        [WeatherKind.PlumeWard] = new Color(0.92f, 0.95f, 1f),
        [WeatherKind.SentrySight] = new Color(0.66f, 0.76f, 0.9f),
        [WeatherKind.TaprootSurge] = new Color(0.5f, 0.95f, 0.6f),
        [WeatherKind.MantleAnchor] = new Color(0.55f, 0.36f, 0.17f),
        [WeatherKind.PrimalHearth] = new Color(0.82f, 0.45f, 1f),
        [WeatherKind.HearthKindle] = new Color(1f, 0.86f, 0.6f),
        // Blessings that puff too (not shown on the obliterator).
        [WeatherKind.MorgenPouch] = new Color(0.78f, 0.62f, 1f),
    };

    // Rites whose obliterator motes differ from their puffs elsewhere: Hearth-Kindle's crescent moons (its puffs on fires
    // and on a player becoming Rested show the game's Rested symbol).
    private static readonly Dictionary<WeatherKind, Shape> ObliteratorShapes = new()
    {
        [WeatherKind.HearthKindle] = Shape.Moon,
    };

    private static readonly Dictionary<WeatherKind, Shape> Shapes = new()
    {
        [WeatherKind.Packward] = Shape.Paw,
        [WeatherKind.Carrion] = Shape.Fly,
        [WeatherKind.Verdant] = Shape.Leaf,
        [WeatherKind.VatBoil] = Shape.Bubble,
        [WeatherKind.PitchBellows] = Shape.Ember,
        [WeatherKind.Kindred] = Shape.Heart,
        [WeatherKind.HarrowSpur] = Shape.Ghost,
        [WeatherKind.AmberMend] = Shape.Amber,
        [WeatherKind.SovereignWard] = Shape.Star,
        [WeatherKind.HearthHaul] = Shape.Feather,
        [WeatherKind.AetherClear] = Shape.Wisp,
        [WeatherKind.ElderSprout] = Shape.Sprout,
        [WeatherKind.PlumeWard] = Shape.Down,
        [WeatherKind.SentrySight] = Shape.Bolt,
        [WeatherKind.TaprootSurge] = Shape.Drop,
        [WeatherKind.MantleAnchor] = Shape.Tar,
        [WeatherKind.PrimalHearth] = Shape.Flame,
        [WeatherKind.HearthKindle] = Shape.Rested,
        [WeatherKind.MorgenPouch] = Shape.Wisp,
    };

    private static readonly WeatherKind[] Kinds =
        [
            WeatherKind.Packward, WeatherKind.Carrion, WeatherKind.Verdant, WeatherKind.VatBoil, WeatherKind.PitchBellows, WeatherKind.Kindred,
            WeatherKind.HarrowSpur, WeatherKind.AmberMend, WeatherKind.SovereignWard, WeatherKind.HearthHaul,
            WeatherKind.AetherClear, WeatherKind.ElderSprout, WeatherKind.SentrySight, WeatherKind.TaprootSurge,
            WeatherKind.MantleAnchor, WeatherKind.PrimalHearth, WeatherKind.HearthKindle
        ];

    private sealed class Shown
    {
        internal GameObject Root = null!;
        internal Light Light = null!;
        internal ParticleSystem? Motes;
        internal Vector3 At;
        internal float NextSparks;
    }

    private static readonly Dictionary<WeatherKind, Shown> Showing = [];
    private static readonly bool[] Wanted = new bool[Kinds.Length];
    private static readonly Vector3[] Points = new Vector3[EdgePoints];
    private static readonly Dictionary<Shape, Material> MoteMaterials = [];
    private static Material? _material;
    // One edge per obliterator with Domain rites going: the ward's dashes, or the line if those can't be found.
    private sealed class EdgeShown
    {
        internal GameObject Root = null!;
        internal FloorDashes? Dashes;
        internal LineRenderer? Line;
        internal Vector3 At;
        internal float Radius;
        internal float Next;
    }

    // Rites this close together are on the same obliterator.
    private const float SameSpot = 1f;
    private static readonly List<EdgeShown> Edges = [];
    private static readonly List<Vector3> EdgeAt = [];
    private static readonly List<float> EdgeRadius = [];
    private static CircleProjector? _wardMarker;
    private static bool _wardLooked;
    private static GameObject? _sparks;
    private static bool _sparksLooked;
    // The area the player was last in, so its edge can stay a moment after they leave.
    private static (Vector3 At, float Radius)? _lastEdge;

    internal static void ClearSession()
    {
        foreach (Shown shown in Showing.Values)
            if (shown.Root) UnityEngine.Object.Destroy(shown.Root);
        Showing.Clear();
        while (Edges.Count > 0) HideEdge(Edges.Count - 1);
        DomainBorder.ClearSession();
        _lastEdge = null;
        BurstsEnd.Clear();
        foreach (ParticleSystem? feathers in Haulers.Values)
            if (feathers) UnityEngine.Object.Destroy(feathers!.gameObject);
        Haulers.Clear();
        if (_restEmbers) UnityEngine.Object.Destroy(_restEmbers!.gameObject);
        _restEmbers = null;
        _sparks = null;
        _sparksLooked = false;
        _wardMarker = null;
        _wardLooked = false;
    }

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Run();
            UpdateHaulers();
            UpdateRester();
        }
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
        int count = 0;
        EdgeAt.Clear();
        EdgeRadius.Clear();
        for (int i = 0; i < Kinds.Length; i++)
        {
            WeatherKind kind = Kinds[i];
            Vector3 at = WeatherState.Position(kind);
            float radius = PluginConfig.Radius(kind);
            Wanted[i] = on && WeatherState.IsActive(kind)
                && Utils.DistanceXZ(player!.transform.position, at) <= radius + ShowBeyond;
            if (!Wanted[i]) continue;
            count++;
            // The widest rite on each obliterator sets its edge.
            int spot = EdgeIndex(at);
            if (spot < 0)
            {
                EdgeAt.Add(at);
                EdgeRadius.Add(radius);
            }
            else if (radius > EdgeRadius[spot]) EdgeRadius[spot] = radius;
        }
        KeepOwnEdges(player);
        while (Edges.Count > EdgeAt.Count) HideEdge(Edges.Count - 1);
        for (int e = 0; e < EdgeAt.Count; e++) Edge(e, EdgeAt[e], EdgeRadius[e]);
        float rate = MoteRate * Mathf.Min(1f, 2f / Mathf.Max(1, count));
        for (int i = 0; i < Kinds.Length; i++)
        {
            WeatherKind kind = Kinds[i];
            if (!Wanted[i])
            {
                Hide(kind);
                continue;
            }
            Shown shown = Get(kind, i);
            Animate(shown, i, rate);
            if (kind == WeatherKind.PitchBellows) Sparks(shown);
        }
    }

    // The edge already noted for an obliterator at this spot, or -1. A plain loop: a lambda here would allocate every frame.
    private static int EdgeIndex(Vector3 at)
    {
        for (int e = 0; e < EdgeAt.Count; e++)
            if (Utils.DistanceXZ(EdgeAt[e], at) <= SameSpot) return e;
        return -1;
    }

    // Only the edges of the areas the player stands in, or for a moment the one they just left, unless they switched
    // it off (see DomainBorder); Always Show Domain Border keeps every edge nearby. A player inside a Crimson, Ragnarök
    // or Hoard-Bane Rite's Blood Grounds sees none: no Domain rite reaches in there.
    private static void KeepOwnEdges(Player? player)
    {
        bool arena = player && IncursionArena.InGrounds(player!.transform.position);
        bool inside = false;
        if (player && !arena)
            for (int e = EdgeAt.Count - 1; e >= 0; e--)
            {
                if (Utils.DistanceXZ(player!.transform.position, EdgeAt[e]) > EdgeRadius[e]) continue;
                inside = true;
                _lastEdge = (EdgeAt[e], EdgeRadius[e]);
            }
        bool show = DomainBorder.Update(player, inside) && !arena;
        if (!show) EdgeAt.Clear();
        else if (!DomainBorder.AlwaysShow)
        {
            Vector3 me = player!.transform.position;
            for (int e = EdgeAt.Count - 1; e >= 0; e--)
            {
                bool own = inside
                    ? Utils.DistanceXZ(me, EdgeAt[e]) <= EdgeRadius[e]
                    : _lastEdge is { } last && Utils.DistanceXZ(last.At, EdgeAt[e]) <= SameSpot;
                if (own) continue;
                EdgeAt.RemoveAt(e);
                EdgeRadius.RemoveAt(e);
            }
        }
        if (EdgeAt.Count == 0) EdgeRadius.Clear();
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
        if (Showing.TryGetValue(kind, out Shown shown) && shown.Root)
        {
            if (shown.At != at)
            {
                shown.At = at;
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
            At = at,
            NextSparks = Time.time + 1f
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
        Shape shape = ObliteratorShapes.TryGetValue(kind, out Shape own) ? own : Shapes.TryGetValue(kind, out Shape puff) ? puff : Shape.Dot;
        shown.Motes = Motes(root.transform, side + Vector3.up * MoteHeight, color, shape);

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

    private static void Animate(Shown shown, int index, float rate)
    {
        float t = Time.time;
        if (shown.Light) shown.Light.intensity = 1.1f + 0.4f * Mathf.Sin(t * 0.8f + index * 1.7f);
        if (shown.Motes)
        {
            ParticleSystem.EmissionModule emission = shown.Motes!.emission;
            if (!Mathf.Approximately(emission.rateOverTimeMultiplier, rate)) emission.rateOverTimeMultiplier = rate;
        }
    }

    private static void Paint(LineRenderer line, Color color, float alpha)
    {
        Color c = new(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        line.startColor = c;
        line.endColor = c;
    }

    // A circle on the ground (or the water) around center; onBuilt: on what is built there too.
    private static void Ring(LineRenderer line, Vector3 center, float radius, int count, bool onBuilt = false)
    {
        float water = ZoneSystem.instance.m_waterLevel;
        for (int k = 0; k < count; k++)
        {
            float angle = k * Mathf.PI * 2f / count;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            bool found = onBuilt ? IncursionArena.GroundAt(point, out float height) : ZoneSystem.instance.GetGroundHeight(point, out height);
            point.y = (found ? Mathf.Max(height, water) : center.y) + Lift;
            Points[k] = point;
        }
        line.positionCount = count;
        line.SetPositions(Points);
    }

    // ---- The edge: the ward's dashes ----

    // The index-th obliterator's edge, around at.
    private static void Edge(int index, Vector3 at, float radius)
    {
        EdgeShown edge;
        if (index >= Edges.Count || !Edges[index].Root)
        {
            GameObject root = new("TempestRite_DomainEdge");
            root.transform.position = at;
            edge = new EdgeShown { Root = root, At = at, Radius = radius, Next = 0f };
            edge.Dashes = Dashes(root.transform, radius);
            edge.Line = edge.Dashes ? null : Line(root.transform, "Line", EdgeWidth);
            if (index < Edges.Count) Edges[index] = edge;
            else Edges.Add(edge);
        }
        else
        {
            edge = Edges[index];
            if (edge.At != at || edge.Radius != radius)
            {
                edge.At = at;
                edge.Radius = radius;
                edge.Next = 0f;
                edge.Root.transform.position = at;
                if (edge.Dashes) Size(edge.Dashes!, radius);
            }
        }
        if (!edge.Line) return;
        float t = Time.time;
        if (t >= edge.Next)
        {
            edge.Next = t + EdgeRefresh;
            Ring(edge.Line!, at, radius, EdgePoints);
            edge.Line!.enabled = true;
        }
        float breath = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / EdgeBreath);
        Paint(edge.Line!, EdgeColor, 0.3f + 0.2f * breath);
    }

    // The line's color, near the ward's warm glow.
    private static readonly Color EdgeColor = new(1f, 0.65f, 0.25f);

    private static void HideEdge(int index)
    {
        if (Edges[index].Root) UnityEngine.Object.Destroy(Edges[index].Root);
        Edges.RemoveAt(index);
    }

    // A ring of the ward's area-marker dashes, on the ground and every floor built over it, slowly turning; null when the
    // game's ward marker can't be found.
    private static FloorDashes? Dashes(Transform parent, float radius)
    {
        CircleProjector? ward = WardMarker();
        if (!ward) return null;
        GameObject go = new("Dashes");
        go.transform.SetParent(parent, false);
        FloorDashes dashes = go.AddComponent<FloorDashes>();
        dashes.Prefab = ward!.m_prefab;
        Size(dashes, radius);
        return dashes;
    }

    // As many dashes as keep the ward's spacing, moving along the edge as fast as the ward's.
    private static void Size(FloorDashes dashes, float radius)
    {
        CircleProjector? ward = WardMarker();
        float wardRadius = ward && ward!.m_radius > 0f ? ward.m_radius : 10f;
        int wardCount = ward && ward!.m_nrOfSegments > 0 ? ward.m_nrOfSegments : 40;
        float wardSpeed = ward ? ward!.m_speed : 0.1f;
        dashes.Radius = radius;
        dashes.Segments = Mathf.Clamp(Mathf.RoundToInt(wardCount * radius / wardRadius), MinDashes, MaxDashes);
        dashes.Speed = wardSpeed * wardRadius / Mathf.Max(1f, radius);
    }

    // ---- An edge in another color (see LeySurvey) ----

    internal sealed class ColoredEdge
    {
        internal GameObject Root = null!;
        internal LineRenderer? Line;
        internal Vector3 At;
        internal float Radius;
        internal Color Color;
        internal float Next;
    }

    // Color properties a dash's material may have: whichever it has is tinted.
    internal static readonly string[] TintProperties = ["_Color", "_TintColor", "_EmissionColor"];

    // The same ring as a Domain rite's edge, in this color: the ward's dashes tinted, or the line if they can't be.
    internal static ColoredEdge ColorEdge(Vector3 at, float radius, Color color)
    {
        GameObject root = new("TempestRite_ColoredEdge");
        root.transform.position = at;
        ColoredEdge edge = new() { Root = root, At = at, Radius = radius, Color = color };
        FloorDashes? dashes = CanTint() ? Dashes(root.transform, radius) : null;
        if (dashes) dashes!.Tint = color;
        else edge.Line = Line(root.transform, "Line", EdgeWidth);
        return edge;
    }

    // Every frame while it shows: the line (if it is one) follows the ground and breathes as the Domain edge does.
    internal static void Keep(ColoredEdge edge)
    {
        if (!edge.Line || !ZoneSystem.instance) return;
        float t = Time.time;
        if (t >= edge.Next)
        {
            edge.Next = t + EdgeRefresh;
            Ring(edge.Line!, edge.At, edge.Radius, EdgePoints, onBuilt: true);
            edge.Line!.enabled = true;
        }
        float breath = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / EdgeBreath);
        Paint(edge.Line!, edge.Color, 0.45f + 0.25f * breath);
    }

    internal static void Remove(ColoredEdge? edge)
    {
        if (edge != null && edge.Root) UnityEngine.Object.Destroy(edge.Root);
    }

    // Whether the ward's dash has a color to tint. Each dash is tinted in place (see FloorDashes.Tint), keeping the
    // game's own material: a copied material drew under grass and steps instead of over them as the ward's does.
    private static bool CanTint()
    {
        CircleProjector? ward = WardMarker();
        if (!ward) return false;
        foreach (Renderer renderer in ward!.m_prefab.GetComponentsInChildren<Renderer>(true))
            foreach (Material material in renderer.sharedMaterials)
                if (material)
                    foreach (string property in TintProperties)
                        if (material.HasProperty(property)) return true;
        return false;
    }

    // The ward's area marker (the workbench's if that's not found).
    private static CircleProjector? WardMarker()
    {
        if (_wardLooked) return _wardMarker;
        if (!ZNetScene.instance) return null;
        _wardLooked = true;
        GameObject ward = ZNetScene.instance.GetPrefab("guard_stone");
        if (ward && ward.GetComponent<PrivateArea>() is { } area && area && area.m_areaMarker && area.m_areaMarker.m_prefab)
            return _wardMarker = area.m_areaMarker;
        GameObject bench = ZNetScene.instance.GetPrefab("piece_workbench");
        if (bench && bench.GetComponent<CraftingStation>() is { } station && station && station.m_areaMarker
            && station.m_areaMarker.GetComponentInChildren<CircleProjector>(true) is { } marker && marker && marker.m_prefab)
            _wardMarker = marker;
        return _wardMarker;
    }

    // ---- Pitch-Bellows: the forge's sparks on the obliterator ----

    // Now and then a big shower of the sparks the forge gives off as you craft at it.
    private static void Sparks(Shown shown)
    {
        if (Time.time < shown.NextSparks) return;
        shown.NextSparks = Time.time + UnityEngine.Random.Range(SparkEveryMin, SparkEveryMax);
        GameObject? prefab = SparksPrefab();
        if (!prefab) return;
        Vector3 offset = new(UnityEngine.Random.Range(-0.3f, 0.3f), GlowHeight + UnityEngine.Random.Range(0f, 0.5f), UnityEngine.Random.Range(-0.3f, 0.3f));
        GameObject go = UnityEngine.Object.Instantiate(prefab, shown.At + offset, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
        go.transform.localScale = prefab!.transform.localScale * SparkScale;
        foreach (ParticleSystem particles in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
        UnityEngine.Object.Destroy(go, 6f);
    }

    // The forge's sparks and the star motes, for the kinship rites' effects too (see KinshipMotes).
    internal static GameObject? ForgeSparks() => SparksPrefab();

    internal static Material? StarMaterial() => MoteMaterial(Shape.Star);

    // The Plume-Ward Rite's ward breaking into feathers (see PlumeFall).
    internal static Material? FeatherMaterial() => MoteMaterial(Shape.Feather);

    // The Tether-Lock Rite's sparks off a held harpoon (see TetherGlow).
    internal static Material? EmberMaterial() => MoteMaterial(Shape.Ember);

    // The Pitch-Seal Rite's blob of pitch, for the sparks of the items it holds (see DropAnchor).
    internal static Material? TarMaterial() => MoteMaterial(Shape.Tar);

    // The forge's crafting effect that only shows (particles; no sound, nothing networked); the game's hit sparks
    // without one.
    private static GameObject? SparksPrefab()
    {
        if (_sparksLooked) return _sparks;
        if (!ZNetScene.instance) return null;
        _sparksLooked = true;
        GameObject forge = ZNetScene.instance.GetPrefab("forge");
        if (forge && forge.GetComponent<CraftingStation>() is { } station && station)
            foreach (EffectList.EffectData effect in station.m_craftItemEffects.m_effectPrefabs)
                if (effect.m_enabled && OnlyShows(effect.m_prefab))
                {
                    _sparks = effect.m_prefab;
                    return _sparks;
                }
        GameObject fallback = ZNetScene.instance.GetPrefab("vfx_HitSparks");
        _sparks = OnlyShows(fallback) ? fallback : null;
        return _sparks;
    }

    private static bool OnlyShows(GameObject? prefab) =>
        prefab && prefab!.GetComponentInChildren<ParticleSystem>(true) && !prefab.GetComponentInChildren<AudioSource>(true)
        && !prefab.GetComponentInChildren<ZNetView>(true);

    // ---- Motes on the obliterator ----

    // Motes drifting up around the obliterator in the rite's color and shape, fading in and out.
    private static ParticleSystem? Motes(Transform parent, Vector3 at, Color color, Shape shape)
    {
        GameObject go = new("Motes");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        ParticleSystem? particles = MoteSystem(go, color, 1.2f, true, shape);
        if (!particles) return null;
        ParticleSystem.EmissionModule emission = particles!.emission;
        emission.rateOverTime = MoteRate;
        particles.Play();
        return particles;
    }

    // ---- Hearth-Haul: feathers on the players carrying too much under it ----
    private const float HaulerRange = 60f;
    private const float HaulerRefresh = 0.25f;
    private const float HaulerHeight = 1.1f;
    private const float HaulerRate = 4f;
    private const float HaulerSize = 0.6f;
    private static readonly Dictionary<Player, ParticleSystem?> Haulers = [];
    private static readonly List<Player> HaulersFound = [];
    private static readonly List<Player> HaulersGone = [];
    private static float _nextHaulers;

    // A few times a second: each player near this one who carries too much and walks without tiring because of the rite
    // gets feathers following them; once they don't, theirs stop and are taken away when the last has faded.
    private static void UpdateHaulers()
    {
        if (Time.time < _nextHaulers) return;
        _nextHaulers = Time.time + HaulerRefresh;
        Player? local = Player.m_localPlayer;
        bool on = local && PluginConfig.ShowDomainEffects.IsOn() && PluginConfig.ShowHearthHaulFeathers.IsOn()
            && PluginConfig.HearthHaulFreeStamina.IsOn() && WeatherState.IsActive(WeatherKind.HearthHaul);
        HaulersFound.Clear();
        if (on)
            foreach (Player player in Player.GetAllPlayers())
                if (player && Utils.DistanceXZ(player.transform.position, local!.transform.position) <= HaulerRange && Hauling(player))
                    HaulersFound.Add(player);
        foreach (Player player in HaulersFound)
            if (!Haulers.ContainsKey(player)) Haulers[player] = HaulerFeathers(player);

        HaulersGone.Clear();
        foreach (KeyValuePair<Player, ParticleSystem?> pair in Haulers)
        {
            ParticleSystem? feathers = pair.Value;
            if (!pair.Key || !feathers)
            {
                HaulersGone.Add(pair.Key!);
                continue;
            }
            bool show = HaulersFound.Contains(pair.Key);
            ParticleSystem.EmissionModule emission = feathers!.emission;
            emission.enabled = show;
            if (show || feathers.particleCount > 0) continue;
            UnityEngine.Object.Destroy(feathers.gameObject);
            HaulersGone.Add(pair.Key);
        }
        foreach (Player player in HaulersGone) Haulers.Remove(player);
    }

    // Carrying too much where the rite eases it. This game knows its own player's load; for another player it has the
    // game's "encumbered" walk, which their game keeps in their ZDO for everyone (see ZSyncAnimation).
    private static bool Hauling(Player player)
    {
        if (player.IsDead()) return false;
        bool encumbered = player == Player.m_localPlayer
            ? player.IsEncumbered()
            : player.m_nview && player.m_nview.GetZDO() is { } zdo && zdo.GetInt(ZSyncAnimation.c_ZDOSalt + Character.s_encumbered) == 1;
        return encumbered && HaulEase.Reaches(player);
    }

    // Feathers drifting up around the player's middle, following them; left behind in the air as they walk on.
    private static ParticleSystem? HaulerFeathers(Player player) =>
        PlayerMotes(player, "TempestRite_HaulFeathers", WeatherKind.HearthHaul, Shape.Feather, HaulerHeight, 0.45f, HaulerRate);

    // The rite's color in a shape drifting up from around the player at a height, following them.
    private static ParticleSystem? PlayerMotes(Player player, string name, WeatherKind kind, Shape shape, float height, float radius, float rate)
    {
        GameObject go = new(name);
        go.transform.SetParent(player.transform, false);
        go.transform.localPosition = Vector3.up * height;
        ParticleSystem? particles = MoteSystem(go, Colors[kind], radius, true, shape);
        if (!particles)
        {
            UnityEngine.Object.Destroy(go);
            return null;
        }
        ParticleSystem.MainModule main = particles!.main;
        ParticleSystem.MinMaxCurve size = main.startSize;
        main.startSize = new ParticleSystem.MinMaxCurve(size.constantMin * HaulerSize, size.constantMax * HaulerSize);
        main.maxParticles = 40;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;
        particles.Play();
        return particles;
    }

    // ---- Hearth-Kindle: the fire's warmth flowing into this game's player resting by it under the rite ----
    // Each ember takes this long from the fire to the player, rising this high over the straight line on the way.
    private const float EmberFlight = 1.6f;
    private const float EmberArc = 0.35f;
    // Embers given off per refresh, on average (about 3 a second).
    private const float EmberChance = 0.75f;
    // Above the fire's base, where the flames are.
    private const float EmberFrom = 0.4f;
    // How fast embers fall back: so that over an average flight they rise EmberArc halfway (arc = fall * time^2 / 8).
    private const float EmberFall = 8f * EmberArc / (EmberFlight * EmberFlight);
    private static readonly Color EmberColor = new(1f, 0.62f, 0.25f);
    private static ParticleSystem? _restEmbers;
    private static float _nextRester;

    // A few times a second: while this game's player rests by a fire the rite reaches, embers lift off that fire and
    // arc into the player's chest, fading as they reach it. Once they stop resting no more come, and the embers are
    // taken away when the last has faded. Only this game knows its player rests, so other players don't see them.
    private static void UpdateRester()
    {
        if (Time.time < _nextRester) return;
        _nextRester = Time.time + HaulerRefresh;
        Player? player = Player.m_localPlayer;
        bool show = player && !player!.IsDead() && PluginConfig.ShowDomainEffects.IsOn() && PluginConfig.ShowHearthKindleMotes.IsOn()
            && WeatherState.IsActive(WeatherKind.HearthKindle) && player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectResting)
            && RestKindle.Reaches(player);
        Vector3? fire = show ? RestingFire(player!) : null;
        if (fire != null && UnityEngine.Random.value < EmberChance)
        {
            if (!_restEmbers) _restEmbers = RestEmbers();
            if (_restEmbers) EmitEmber(_restEmbers!, fire.Value, player!.GetCenterPoint());
        }
        if (!_restEmbers || fire != null || _restEmbers!.particleCount > 0) return;
        UnityEngine.Object.Destroy(_restEmbers.gameObject);
        _restEmbers = null;
    }

    // The fire the player rests by: the warmth the game finds them in (as it does for resting), at its flames.
    private static Vector3? RestingFire(Player player)
    {
        EffectArea? area = EffectArea.IsPointInsideArea(player.transform.position, EffectArea.Type.Heat, 1f);
        if (!area) return null;
        Fireplace? fireplace = area!.GetComponentInParent<Fireplace>();
        Transform at = fireplace ? fireplace!.transform : area.transform;
        return at.position + Vector3.up * EmberFrom;
    }

    // Embers streaking along their path, flickering; each one is sent off by EmitEmber.
    private static ParticleSystem? RestEmbers()
    {
        GameObject go = new("TempestRite_RestEmbers");
        ParticleSystem? particles = MoteSystem(go, EmberColor, 0.05f, true, Shape.Ember);
        if (!particles)
        {
            UnityEngine.Object.Destroy(go);
            return null;
        }
        ParticleSystem.MainModule main = particles!.main;
        main.gravityModifier = EmberFall / -Physics.gravity.y;
        main.maxParticles = 30;
        // Steady along the path, so each lands where it was aimed.
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = false;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        particles.Play();
        return particles;
    }

    // One ember off the fire, thrown up so that falling back it reaches the chest just as it fades.
    private static void EmitEmber(ParticleSystem particles, Vector3 from, Vector3 to)
    {
        from += new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), UnityEngine.Random.Range(0f, 0.2f), UnityEngine.Random.Range(-0.15f, 0.15f));
        to += new Vector3(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.15f), UnityEngine.Random.Range(-0.1f, 0.1f));
        float time = EmberFlight * UnityEngine.Random.Range(0.85f, 1.15f);
        ParticleSystem.EmitParams ember = new()
        {
            position = from,
            velocity = (to - from) / time + Vector3.up * (EmberFall * time / 2f),
            startLifetime = time,
            startSize = UnityEngine.Random.Range(0.06f, 0.1f),
            startColor = Color.Lerp(EmberColor, new Color(1f, 0.85f, 0.5f), UnityEngine.Random.value)
        };
        particles.Emit(ember, 1);
    }

    // Becoming Rested by a fire under the Hearth-Kindle Rite: a puff of Rested symbols around the player.
    internal static void RestedPuff(Character character)
    {
        if (!character || !PluginConfig.ShowDomainEffects.IsOn() || !PluginConfig.ShowHearthKindleMotes.IsOn()) return;
        Burst(WeatherKind.HearthKindle, character.GetCenterPoint(), Colors[WeatherKind.HearthKindle], 1.3f, true);
    }

    internal const float BurstSeconds = 4.5f;
    private const int BurstMotes = 6;
    // At most this many puffs of one rite at once; more are skipped until one is gone.
    private const int MaxBursts = 6;
    private static readonly Dictionary<WeatherKind, Queue<float>> BurstsEnd = [];

    // Puffs are this big next to the obliterator's motes, before their scale.
    private const float BurstSize = 0.75f;

    // A short puff of the rite's motes (leaves, bubbles, embers...) rising around a point, gone after a few seconds (a
    // Verdant plant growing). Scale shrinks the motes, how far they spread and how many there are (0.5: crops). False when
    // too many of the rite's are showing, unless always (a puff that marks something happening, like a stump sprouting).
    internal static bool Burst(WeatherKind kind, Vector3 at, Color color, float scale = 1f, bool always = false)
    {
        if (PatchGuard.IsDisabled(Feature)) return false;
        if (!BurstsEnd.TryGetValue(kind, out Queue<float> ends)) BurstsEnd[kind] = ends = new Queue<float>();
        while (ends.Count > 0 && ends.Peek() <= Time.time) ends.Dequeue();
        if (ends.Count >= MaxBursts && !always) return false;
        ends.Enqueue(Time.time + BurstSeconds);

        GameObject go = new("TempestRite_MoteBurst");
        go.transform.position = at;
        ParticleSystem? particles = MoteSystem(go, color, 0.6f * scale, false, Shapes.TryGetValue(kind, out Shape shape) ? shape : Shape.Dot);
        if (particles)
        {
            ParticleSystem.MainModule main = particles!.main;
            ParticleSystem.MinMaxCurve speed = main.startSpeed, size = main.startSize, lifetime = main.startLifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.constantMin * scale, speed.constantMax * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(size.constantMin * BurstSize * scale, size.constantMax * BurstSize * scale);
            main.startLifetime = new ParticleSystem.MinMaxCurve(Mathf.Min(1.5f, lifetime.constantMin), Mathf.Min(3f, lifetime.constantMax));
            particles.Play();
            particles.Emit(Mathf.Max(3, Mathf.RoundToInt(BurstMotes * scale)));
        }
        UnityEngine.Object.Destroy(go, BurstSeconds);
        return true;
    }

    // Motes in a color drifting up from within a sphere, fading in and out; not emitting yet.
    private static ParticleSystem? MoteSystem(GameObject go, Color color, float radius, bool loop, Shape shape)
    {
        Material? material = MoteMaterial(shape);
        if (!material) return null;
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = loop;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
        main.gravityModifier = -0.015f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 150;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule area = particles.shape;
        area.enabled = true;
        area.shapeType = ParticleSystemShapeType.Sphere;
        area.radius = radius;

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
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
        Behave(particles, renderer, shape, color);
        return particles;
    }

    // How each shape moves: leaves tumble, bubbles swell and pop, embers streak and flicker, stars turn slowly, hearts
    // float up gently, swaying and beating.
    private static void Behave(ParticleSystem particles, ParticleSystemRenderer renderer, Shape shape, Color color)
    {
        ParticleSystem.MainModule main = particles.main;
        ParticleSystem.NoiseModule noise = particles.noise;
        ParticleSystem.RotationOverLifetimeModule spin = particles.rotationOverLifetime;
        switch (shape)
        {
            case Shape.Leaf:
                main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, new Color(0.75f, 0.9f, 0.2f), 0.6f));
                noise.strength = 0.2f;
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
                break;
            case Shape.Bubble:
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.28f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
                noise.strength = 0.08f;
                ParticleSystem.SizeOverLifetimeModule swell = particles.sizeOverLifetime;
                swell.enabled = true;
                swell.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.25f));
                Gradient pop = new();
                pop.SetKeys(
                    [new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f)],
                    [new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.9f), new GradientAlphaKey(0f, 1f)]);
                ParticleSystem.ColorOverLifetimeModule overLife = particles.colorOverLifetime;
                overLife.color = new ParticleSystem.MinMaxGradient(pop);
                break;
            case Shape.Ember:
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, new Color(1f, 0.8f, 0.35f));
                noise.strength = 0.3f;
                noise.frequency = 0.5f;
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 1.5f;
                renderer.velocityScale = 0.3f;
                break;
            case Shape.Heart:
            {
                main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.3f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.07f, 0.18f);
                main.startRotation = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, new Color(1f, 0.2f, 0.3f), 0.5f));
                noise.strength = 0.1f;
                noise.frequency = 0.3f;
                // Two quick beats early on, then settling.
                AnimationCurve beat = new(new Keyframe(0f, 0.7f), new Keyframe(0.12f, 1.1f), new Keyframe(0.2f, 0.9f), new Keyframe(0.28f, 1.1f),
                    new Keyframe(0.4f, 1f), new Keyframe(1f, 0.85f));
                ParticleSystem.SizeOverLifetimeModule pulse = particles.sizeOverLifetime;
                pulse.enabled = true;
                pulse.size = new ParticleSystem.MinMaxCurve(1f, beat);
                break;
            }
            case Shape.Star:
                main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                break;
            case Shape.Amber:
            {
                // Thick resin drops: slow and heavy, rocking a little, glinting now and then as they turn to the light.
                main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
                main.startRotation = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, new Color(1f, 0.85f, 0.4f), 0.6f));
                main.gravityModifier = -0.008f;
                noise.strength = 0.06f;
                noise.frequency = 0.25f;
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
                AnimationCurve glint = new(new Keyframe(0f, 0.8f), new Keyframe(0.3f, 1f), new Keyframe(0.38f, 1.25f), new Keyframe(0.46f, 1f),
                    new Keyframe(0.75f, 1.05f), new Keyframe(1f, 0.9f));
                ParticleSystem.SizeOverLifetimeModule shine = particles.sizeOverLifetime;
                shine.enabled = true;
                shine.size = new ParticleSystem.MinMaxCurve(1f, glint);
                break;
            }
            case Shape.Paw:
                // Paw prints drifting up, each turned a little, toes leading.
                main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.3f);
                main.startRotation = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, new Color(0.85f, 0.95f, 1f), 0.5f));
                noise.strength = 0.08f;
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
                break;
            case Shape.Ghost:
            {
                // Little ghosts rising slowly, swaying side to side as they go, rocking a little, fading out high up.
                main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.36f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 4.5f);
                main.startRotation = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.6f));
                main.gravityModifier = -0.02f;
                noise.strength = 0.18f;
                noise.frequency = 0.35f;
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                AnimationCurve grow = new(new Keyframe(0f, 0.6f), new Keyframe(0.25f, 1f), new Keyframe(1f, 1.1f));
                ParticleSystem.SizeOverLifetimeModule rise = particles.sizeOverLifetime;
                rise.enabled = true;
                rise.size = new ParticleSystem.MinMaxCurve(1f, grow);
                break;
            }
            case Shape.Feather:
            {
                // Feathers floating up as if weightless, rocking from side to side as they go.
                main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.34f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 4.5f);
                main.startRotation = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.55f));
                main.gravityModifier = -0.015f;
                noise.strength = 0.22f;
                noise.frequency = 0.3f;
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(-0.9f, 0.9f);
                break;
            }
            case Shape.Wisp:
            {
                // Little wisps circling the obliterator as they slowly rise, flickering like a Wisplight's.
                main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.3f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5.5f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.7f));
                main.gravityModifier = -0.01f;
                noise.strength = 0.15f;
                noise.frequency = 0.5f;
                ParticleSystem.VelocityOverLifetimeModule circle = particles.velocityOverLifetime;
                circle.enabled = true;
                // All three orbit curves must share one mode (two constants here), or Unity logs an error.
                circle.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
                circle.orbitalY = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
                circle.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
                circle.radial = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
                AnimationCurve flicker = new(new Keyframe(0f, 0.5f), new Keyframe(0.15f, 1f), new Keyframe(0.3f, 0.85f), new Keyframe(0.45f, 1.1f),
                    new Keyframe(0.6f, 0.9f), new Keyframe(0.8f, 1.05f), new Keyframe(1f, 0.7f));
                ParticleSystem.SizeOverLifetimeModule pulse = particles.sizeOverLifetime;
                pulse.enabled = true;
                pulse.size = new ParticleSystem.MinMaxCurve(1f, flicker);
                break;
            }
            case Shape.Sprout:
            {
                // Little sprouts springing up out of nothing, then rising gently, swaying a little.
                main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.3f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
                main.startRotation = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, new Color(0.45f, 0.85f, 0.3f), 0.5f));
                noise.strength = 0.1f;
                noise.frequency = 0.3f;
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                AnimationCurve spring = new(new Keyframe(0f, 0.2f), new Keyframe(0.15f, 1.15f), new Keyframe(0.25f, 0.95f), new Keyframe(1f, 1f));
                ParticleSystem.SizeOverLifetimeModule grow = particles.sizeOverLifetime;
                grow.enabled = true;
                grow.size = new ParticleSystem.MinMaxCurve(1f, spring);
                break;
            }
            case Shape.Down:
            {
                // Tufts of down drifting out and slowly settling, turning as they go.
                main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
                main.gravityModifier = 0.006f;
                noise.strength = 0.25f;
                noise.frequency = 0.3f;
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
                break;
            }
            case Shape.Bolt:
                // Ballista bolts rising point first, steady, without turning.
                main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.3f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
                main.gravityModifier = -0.02f;
                noise.strength = 0.04f;
                break;
            case Shape.Drop:
            {
                // Glowing sap drops welling up, swelling a little, then dripping back down.
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.6f));
                main.gravityModifier = 0.03f;
                noise.strength = 0.05f;
                AnimationCurve well = new(new Keyframe(0f, 0.5f), new Keyframe(0.3f, 1.1f), new Keyframe(1f, 0.9f));
                ParticleSystem.SizeOverLifetimeModule grow = particles.sizeOverLifetime;
                grow.enabled = true;
                grow.size = new ParticleSystem.MinMaxCurve(1f, well);
                break;
            }
            case Shape.Tar:
                // Little blobs of pitch rising slowly, rocking a little as they go; painted in their own colors.
                main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.26f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
                main.startRotation = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.9f, 0.9f, 0.9f), Color.white);
                main.gravityModifier = -0.005f;
                noise.strength = 0.05f;
                noise.frequency = 0.25f;
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                break;
            case Shape.Flame:
            {
                // Little flames leaping up, flickering and shrinking away.
                main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
                main.startRotation = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, new Color(1f, 0.8f, 1f), 0.5f));
                noise.strength = 0.18f;
                noise.frequency = 0.8f;
                AnimationCurve flicker = new(new Keyframe(0f, 0.6f), new Keyframe(0.15f, 1.1f), new Keyframe(0.3f, 0.9f), new Keyframe(0.45f, 1.05f),
                    new Keyframe(1f, 0.3f));
                ParticleSystem.SizeOverLifetimeModule leap = particles.sizeOverLifetime;
                leap.enabled = true;
                leap.size = new ParticleSystem.MinMaxCurve(1f, flicker);
                break;
            }
            case Shape.Rested:
            {
                // The game's Rested symbol in its own colors, rising gently, rocking a little, swelling in as it appears.
                main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.32f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 4.2f);
                main.startRotation = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.92f, 0.85f));
                noise.strength = 0.08f;
                noise.frequency = 0.3f;
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                AnimationCurve appear = new(new Keyframe(0f, 0.5f), new Keyframe(0.15f, 1.1f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.95f));
                ParticleSystem.SizeOverLifetimeModule grow = particles.sizeOverLifetime;
                grow.enabled = true;
                grow.size = new ParticleSystem.MinMaxCurve(1f, appear);
                break;
            }
            case Shape.Moon:
                // Crescent moons rising gently, swaying a little: rest.
                main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.28f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 4.2f);
                main.startRotation = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
                noise.strength = 0.1f;
                noise.frequency = 0.3f;
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                break;
            case Shape.Fly:
            {
                // Flies darting every which way and circling about, never rising off.
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.1f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
                main.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(color, Color.white, 0.2f), Color.Lerp(color, new Color(0.3f, 0.45f, 0.2f), 0.5f));
                main.gravityModifier = 0f;
                noise.strength = 0.9f;
                noise.frequency = 2.2f;
                noise.scrollSpeed = 1.5f;
                ParticleSystem.VelocityOverLifetimeModule circle = particles.velocityOverLifetime;
                circle.enabled = true;
                circle.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
                circle.orbitalY = new ParticleSystem.MinMaxCurve(-2f, 2f);
                circle.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
                circle.radial = new ParticleSystem.MinMaxCurve(-0.25f, 0.05f);
                break;
            }
        }
    }

    // A white picture of the shape, tinted by each mote's color; for Rested, the game's own Rested icon (round motes
    // until the game has it).
    private static Material? MoteMaterial(Shape shape)
    {
        if (MoteMaterials.TryGetValue(shape, out Material material) && material) return material;
        if (shape == Shape.Rested)
        {
            Material? icon = KinshipMotes.Material("RestedMote", KinshipMotes.RestedIcon());
            if (icon) MoteMaterials[shape] = icon!;
            return icon ? icon : MoteMaterial(Shape.Dot);
        }
        Shader? shader = Shader.Find("Sprites/Default");
        if (!shader) return null;
        const int size = 32;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            name = $"TempestRite_Mote{shape}",
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                pixels[y * size + x] = Pixel(shape, dx, dy);
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        material = new Material(shader)
        {
            name = $"TempestRite_Motes{shape}",
            mainTexture = texture,
            renderQueue = 3100,
            hideFlags = HideFlags.HideAndDontSave
        };
        MoteMaterials[shape] = material;
        return material;
    }

    // A feather's vane narrowed to 45% along a slanting notch between its barbs, at "at" along the quill.
    private static float Notch(float u, float v, float at) => Mathf.Clamp01(Mathf.Abs(u - Mathf.Abs(v) * 0.6f - at) / 0.12f) * 0.55f + 0.45f;

    // How far a point is outside an oval (negative: inside), in about the oval's own size.
    private static float Oval(float x, float y, float cx, float cy, float rx, float ry)
    {
        float ox = (x - cx) / rx, oy = (y - cy) / ry;
        return (Mathf.Sqrt(ox * ox + oy * oy) - 1f) * Mathf.Min(rx, ry);
    }

    // How far a point is outside an oval turned by angle (radians, counterclockwise), in about the oval's own size.
    private static float Leaflet(float x, float y, float cx, float cy, float rx, float ry, float angle)
    {
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
        float ox = x - cx, oy = y - cy;
        return Oval(ox * c + oy * s, -ox * s + oy * c, 0f, 0f, rx, ry);
    }

    // How far a point is from a line from (ax, ay) to (bx, by), less r (negative: inside the stroke).
    private static float Stroke(float x, float y, float ax, float ay, float bx, float by, float r)
    {
        float dx = bx - ax, dy = by - ay;
        float t = Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy));
        float px = ax + t * dx - x, py = ay + t * dy - y;
        return Mathf.Sqrt(px * px + py * py) - r;
    }

    // The shape at a point from -1 to 1 across.
    private static Color Pixel(Shape shape, float dx, float dy)
    {
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        float glow = Mathf.Clamp01(1f - r);
        switch (shape)
        {
            case Shape.Leaf:
            {
                // A pointed oval, tip up, with a darker midrib.
                float halfWidth = 0.42f * (1f - dy * dy);
                float alpha = Mathf.Clamp01((halfWidth - Mathf.Abs(dx)) * 12f) * Mathf.Clamp01((0.95f - Mathf.Abs(dy)) * 12f);
                float shade = Mathf.Abs(dx) < 0.05f ? 0.6f : 1f - 0.25f * Mathf.Abs(dx) / Mathf.Max(0.05f, halfWidth);
                return new Color(shade, shade, shade, alpha);
            }
            case Shape.Bubble:
            {
                // A thin bright ring, a faint fill and a gleam at the top left.
                float ring = Mathf.Exp(-Mathf.Pow((r - 0.78f) / 0.09f, 2f));
                float fill = r < 0.8f ? 0.12f : 0f;
                float gx = dx + 0.35f, gy = dy - 0.35f;
                float gleam = Mathf.Clamp01(1f - Mathf.Sqrt(gx * gx + gy * gy) / 0.18f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(ring, fill) + gleam));
            }
            case Shape.Heart:
            {
                // A heart, point down, lighter toward its top left: two round lobes over a point (its distance, in the
                // heart's own size, the point at 0 and the lobes' tops at about 1.1).
                float hx = Mathf.Abs(dx) * 0.65f, hy = (dy + 0.88f) * 0.65f;
                float d;
                if (hx + hy > 1f) d = Mathf.Sqrt((hx - 0.25f) * (hx - 0.25f) + (hy - 0.75f) * (hy - 0.75f)) - Mathf.Sqrt(2f) / 4f;
                else
                {
                    float m = 0.5f * Mathf.Max(hx + hy, 0f);
                    d = Mathf.Sqrt(Mathf.Min(hx * hx + (hy - 1f) * (hy - 1f), (hx - m) * (hx - m) + (hy - m) * (hy - m))) * Mathf.Sign(hx - hy);
                }
                float shade = 1f - 0.25f * Mathf.Clamp01((dx - dy + 0.6f) * 0.6f);
                return new Color(shade, shade, shade, Mathf.Clamp01(0.5f - d * 24f));
            }
            case Shape.Star:
            {
                // A soft core with four thin rays.
                float rays = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(dy) * 12f) * Mathf.Clamp01(1f - Mathf.Abs(dx)),
                    Mathf.Clamp01(1f - Mathf.Abs(dx) * 12f) * Mathf.Clamp01(1f - Mathf.Abs(dy)));
                float core = Mathf.Pow(Mathf.Clamp01(1f - r * 2.2f), 2f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(rays, core)));
            }
            case Shape.Amber:
            {
                // A resin drop, tip up: round below, narrowing to a point, darker at its rim, with a bright gleam on
                // its upper left and a warm glow inside.
                // Half its width: a circle (center 0.3 down, radius 0.6) below, a straight taper to the tip at 0.95 above.
                const float centerY = -0.3f, round = 0.6f, tip = 0.95f;
                float width = dy <= centerY
                    ? Mathf.Sqrt(Mathf.Max(0f, round * round - (dy - centerY) * (dy - centerY)))
                    : round * Mathf.Clamp01((tip - dy) / (tip - centerY));
                float edge = width - Mathf.Abs(dx);
                float alpha = Mathf.Clamp01(edge * 14f);
                float rim = Mathf.Clamp01(edge / Mathf.Max(0.05f, width));
                float shade = 0.6f + 0.4f * Mathf.Sqrt(rim);
                float gx = dx + 0.15f, gy = dy + 0.05f;
                float gleam = Mathf.Clamp01(1f - Mathf.Sqrt(gx * gx * 4f + gy * gy) / 0.22f);
                shade = Mathf.Clamp01(shade + gleam);
                return new Color(shade, shade, shade, alpha);
            }
            case Shape.Paw:
            {
                // A wolf's paw print, toes up: a wide pad below, four oval toes above it, the middle two higher.
                float d = Oval(dx, dy, 0f, -0.32f, 0.36f, 0.28f);
                d = Mathf.Min(d, Oval(dx, dy, -0.47f, 0.12f, 0.13f, 0.17f));
                d = Mathf.Min(d, Oval(dx, dy, 0.47f, 0.12f, 0.13f, 0.17f));
                d = Mathf.Min(d, Oval(dx, dy, -0.17f, 0.45f, 0.14f, 0.19f));
                d = Mathf.Min(d, Oval(dx, dy, 0.17f, 0.45f, 0.14f, 0.19f));
                float shade = 1f - 0.2f * Mathf.Clamp01((dx - dy + 0.8f) * 0.5f);
                return new Color(shade, shade, shade, Mathf.Clamp01(0.5f - d * 20f));
            }
            case Shape.Fly:
            {
                // A fly from above: a dark body and head between two pale, see-through wings.
                float body = Mathf.Min(Oval(dx, dy, 0f, -0.12f, 0.2f, 0.4f), Oval(dx, dy, 0f, 0.36f, 0.16f, 0.14f));
                float wings = Mathf.Min(Oval(dx, dy, -0.36f, 0.02f, 0.34f, 0.2f), Oval(dx, dy, 0.36f, 0.02f, 0.34f, 0.2f));
                float bodyAlpha = Mathf.Clamp01(0.5f - body * 20f);
                float wingAlpha = Mathf.Clamp01(0.5f - wings * 20f) * 0.5f;
                if (bodyAlpha <= 0f) return new Color(1f, 1f, 1f, wingAlpha);
                float shade = Mathf.Lerp(1f, 0.25f, bodyAlpha);
                return new Color(shade, shade, shade, Mathf.Max(bodyAlpha, wingAlpha));
            }
            case Shape.Ghost:
            {
                // A little ghost, as on the Harrow-Spur icon: a round head, raised arms, a body swaying down into a tail
                // that fades out, and two eyes and a mouth seen through.
                float t = Mathf.Clamp01((0.25f - dy) / 1.15f);
                float middle = Mathf.Sin(t * Mathf.PI * 1.4f) * 0.14f * t;
                float body = Mathf.Max(Mathf.Abs(dx - middle) - Mathf.Lerp(0.34f, 0.04f, t), dy - 0.25f);
                body = Mathf.Max(body, -0.92f - dy);
                float d = Mathf.Min(Oval(dx, dy, 0f, 0.42f, 0.31f, 0.31f), body);
                d = Mathf.Min(d, Mathf.Min(Stroke(dx, dy, -0.22f, 0.12f, -0.6f, 0.62f, 0.09f), Stroke(dx, dy, 0.22f, 0.12f, 0.6f, 0.62f, 0.09f)));
                float alpha = Mathf.Clamp01(0.5f - d * 20f) * Mathf.Lerp(1f, 0.25f, t * t);
                float holes = Mathf.Min(Mathf.Min(Oval(dx, dy, -0.12f, 0.47f, 0.07f, 0.1f), Oval(dx, dy, 0.12f, 0.47f, 0.07f, 0.1f)),
                    Oval(dx, dy, 0f, 0.26f, 0.06f, 0.09f));
                alpha *= 1f - 0.85f * Mathf.Clamp01(0.5f - holes * 30f);
                float shade = 1f - 0.2f * Mathf.Clamp01(t);
                return new Color(shade, shade, shade, alpha);
            }
            case Shape.Feather:
            {
                // A feather slanting up to the right: a quill bending a little toward its tip, bare at its base, with
                // down there; one side of the vane wider than the other, split by a few notches between its barbs.
                const float angle = 0.7f;
                float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
                float u = dx * c + dy * s, v = -dx * s + dy * c;
                v -= 0.1f * Mathf.Pow(Mathf.Clamp01((u + 0.2f) / 1.1f), 2f);
                float t = Mathf.Clamp01((u + 0.45f) / 1.35f);
                float outline = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.6f) * (1f - 0.35f * t);
                float notch = v > 0f ? Notch(u, v, 0.25f) : Mathf.Min(Notch(u, v, -0.05f), Notch(u, v, 0.5f));
                float width = (v > 0f ? 0.4f : 0.3f) * outline * notch;
                float vane = Mathf.Max(Mathf.Abs(v) - width, Mathf.Max(-(u + 0.45f), u - 0.9f));
                float quill = Mathf.Max(Mathf.Abs(v) - 0.05f, Mathf.Abs(u + 0.02f) - 0.94f);
                float down = Mathf.Sqrt((u + 0.5f) * (u + 0.5f) / 1.69f + v * v) - 0.26f;
                float alpha = Mathf.Max(Mathf.Max(Mathf.Clamp01(0.5f - vane * 22f), Mathf.Clamp01(0.5f - quill * 30f)),
                    Mathf.Clamp01(0.5f - down * 12f) * 0.45f);
                float barbs = 0.82f + 0.18f * Mathf.Sin((u - Mathf.Abs(v) * 1.1f) * 28f);
                float shade = quill < 0f ? 1f : barbs * (1f - 0.3f * Mathf.Clamp01(Mathf.Abs(v) / Mathf.Max(0.05f, width)));
                return new Color(shade, shade, shade, alpha);
            }
            case Shape.Sprout:
            {
                // A seedling: a stem rising from below, curving a little, opening into two leaves, the left one bigger.
                float stem = Stroke(dx, dy, 0f, -0.9f, 0.04f, 0.12f, 0.07f);
                float left = Leaflet(dx, dy, -0.3f, 0.32f, 0.36f, 0.17f, -0.6f);
                float right = Leaflet(dx, dy, 0.3f, 0.4f, 0.3f, 0.15f, 0.6f);
                float leaves = Mathf.Min(left, right);
                float d = Mathf.Min(stem, leaves);
                float shade = leaves < stem ? 1f - 0.2f * Mathf.Clamp01((0.6f - dy) * 0.8f) : 0.72f;
                return new Color(shade, shade, shade, Mathf.Clamp01(0.5f - d * 20f));
            }
            case Shape.Down:
            {
                // A tuft of down: a soft heart with fine wisps fanning out all around it.
                float angle = Mathf.Atan2(dy, dx);
                float wisps = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 4.5f)), 6f) * Mathf.Clamp01(1f - r) * 0.8f;
                float heart = Mathf.Clamp01((0.35f - r) * 5f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(heart, wisps + glow * glow * 0.35f)));
            }
            case Shape.Bolt:
            {
                // A ballista bolt pointing up: a shaft, an iron head, two fins at its tail.
                float shaft = Stroke(dx, dy, 0f, -0.85f, 0f, 0.45f, 0.07f);
                float head = Mathf.Max(Mathf.Abs(dx) - (0.92f - dy) * 0.5f, Mathf.Max(0.4f - dy, dy - 0.92f));
                float fins = Mathf.Min(Stroke(dx, dy, 0f, -0.55f, -0.28f, -0.9f, 0.06f), Stroke(dx, dy, 0f, -0.55f, 0.28f, -0.9f, 0.06f));
                float d = Mathf.Min(Mathf.Min(shaft, head), fins);
                float shade = head < Mathf.Min(shaft, fins) ? 0.75f : 1f;
                return new Color(shade, shade, shade, Mathf.Clamp01(0.5f - d * 20f));
            }
            case Shape.Drop:
            {
                // A glowing drop, tip up: bright inside, softly haloed.
                float width = dy <= -0.25f ? Mathf.Sqrt(Mathf.Max(0f, 0.3f - (dy + 0.25f) * (dy + 0.25f))) : 0.55f * Mathf.Clamp01((0.85f - dy) / 1.1f);
                float edge = width - Mathf.Abs(dx);
                float body = Mathf.Clamp01(edge * 12f);
                float gx = dx + 0.15f, gy = dy + 0.1f;
                float gleam = Mathf.Clamp01(1f - Mathf.Sqrt(gx * gx * 4f + gy * gy) / 0.25f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(body * (0.75f + 0.25f * gleam), glow * glow * 0.4f)));
            }
            case Shape.Tar:
            {
                // A blob of pitch, drips hanging off it, a gold coin stuck in its top. Painted in its own colors (the
                // particles are left untinted), lit from the upper left.
                float blob = Oval(dx, dy, 0f, -0.05f, 0.62f, 0.4f) - 0.04f * Mathf.Sin(dx * 9f);
                float drips = Mathf.Min(Mathf.Min(Stroke(dx, dy, -0.3f, -0.2f, -0.3f, -0.62f, 0.09f), Oval(dx, dy, -0.3f, -0.68f, 0.13f, 0.14f)),
                    Mathf.Min(Stroke(dx, dy, 0.28f, -0.25f, 0.28f, -0.48f, 0.08f), Oval(dx, dy, 0.28f, -0.53f, 0.11f, 0.12f)));
                float tar = Mathf.Min(blob, drips);
                float coin = Oval(dx, dy, 0.12f, 0.38f, 0.3f, 0.3f);
                float tarAlpha = Mathf.Clamp01(0.5f - tar * 20f);
                float coinAlpha = Mathf.Clamp01(0.5f - coin * 20f);
                float light = 1f - 0.45f * Mathf.Clamp01((dx - dy + 0.8f) * 0.6f);
                Color gold = new Color(1f, 0.78f, 0.3f) * Mathf.Lerp(0.6f, 1.1f, Mathf.Clamp01(-coin / 0.12f)) * light;
                gold.a = coinAlpha;
                float sheen = Mathf.Clamp01(1f - Mathf.Abs(Oval(dx, dy, -0.08f, -0.02f, 0.44f, 0.26f)) / 0.06f) * Mathf.Clamp01((dy + 0.02f) * 6f) * Mathf.Clamp01(-dx * 4f);
                float rim = Mathf.Clamp01(1f + tar / 0.08f);
                Color pitch = Color.Lerp(new Color(0.13f, 0.09f, 0.06f), new Color(0.5f, 0.32f, 0.14f), rim * 0.7f) * light;
                pitch = Color.Lerp(pitch, new Color(0.85f, 0.8f, 0.7f), sheen * 0.7f);
                pitch.a = tarAlpha;
                // The pitch over the coin.
                float a = pitch.a + gold.a * (1f - pitch.a);
                if (a <= 0f) return Color.clear;
                Color mixed = (pitch * pitch.a + gold * gold.a * (1f - pitch.a)) / a;
                mixed.a = a;
                return mixed;
            }
            case Shape.Flame:
            {
                // A flame, tip up: round at its base, licking up to a point, a bright heart low in it.
                float width = dy <= -0.35f ? Mathf.Sqrt(Mathf.Max(0f, 0.25f - (dy + 0.35f) * (dy + 0.35f)))
                    : 0.5f * Mathf.Pow(Mathf.Clamp01((0.95f - dy) / 1.3f), 0.8f);
                float bend = 0.12f * Mathf.Sin((dy + 0.35f) * 3f) * Mathf.Clamp01(dy + 0.35f);
                float edge = width - Mathf.Abs(dx - bend);
                float alpha = Mathf.Clamp01(edge * 12f);
                float heart = Mathf.Clamp01(-Oval(dx, dy, 0f, -0.4f, 0.22f, 0.3f) * 8f);
                float shade = Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(heart + Mathf.Clamp01(edge / 0.3f) * 0.4f));
                return new Color(shade, shade, shade, alpha);
            }
            case Shape.Moon:
            {
                // A crescent moon, horns to the right, with a faint glow.
                float outer = Oval(dx, dy, 0f, 0f, 0.62f, 0.62f);
                float inner = Oval(dx, dy, 0.32f, 0.12f, 0.52f, 0.52f);
                float d = Mathf.Max(outer, -inner);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(Mathf.Clamp01(0.5f - d * 20f), glow * glow * 0.25f)));
            }
            case Shape.Wisp:
            {
                // A wisp: a white-hot core in a soft halo.
                float core = Mathf.Clamp01((0.32f - r) * 8f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(core, glow * glow * 0.75f)));
            }
            default:
                return new Color(1f, 1f, 1f, glow * glow);
        }
    }
}
