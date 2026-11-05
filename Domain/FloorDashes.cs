using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// A slowly turning ring of dashes like the game's ward marker (CircleProjector), but lying on the ground and on every
// built floor above it, so the edge shows on each story of a building and not under it.
// The surfaces along the whole ring are looked up once, every few centimeters, and kept up to date a slice each frame.
// Each dash then shows exactly the parts of it that lie on a surface: cut at a floor's edge instead of hanging over it,
// split where it runs from a floor onto the ground, and sliding smoothly past an edge as the ring turns instead of
// jumping from one surface to another.
internal sealed class FloorDashes : MonoBehaviour
{
    // A surface this close under another is hidden under it (the ground under a floor): it gets none.
    private const float MinGap = 1.5f;
    // Built surfaces steeper than this (about 40°) are roofs and wall tops, not floors. The ground counts at any slope.
    private const float MinUp = 0.75f;
    private const int MaxFloors = 8;
    private const float From = 500f;
    // How far apart along the ring the surfaces are looked up, in meters.
    private const float Spacing = 0.15f;
    // How many of those spots are looked up again each frame (built floors come and go).
    private const int RefreshPerFrame = 250;
    // Neighbouring spots on a floor this close in height are the same floor; farther apart, a step or an edge.
    private const float SameFloor = 0.3f;
    // A piece of a dash shorter than this (meters) isn't shown.
    private const float MinPiece = 0.1f;
    // A dash's length when the prefab's can't be measured.
    private const float FallbackLength = 1.5f;
    private static readonly RaycastHit[] Hits = new RaycastHit[32];
    private static int _mask, _terrain = -1;

    internal GameObject Prefab = null!;
    // A color for the dashes (the Ley-Survey's red), set on each dash's own renderers so they keep the game's material
    // and draw over grass and steps as the ward's do; none keeps the ward's color.
    internal Color? Tint;
    private MaterialPropertyBlock? _block;
    internal float Radius = 5f;
    internal int Segments = 20;
    internal float Speed = 0.1f;
    private readonly List<GameObject> _dashes = [];
    private int _used;
    // The dash's length along its direction, and its own scale.
    private float _length = -1f;
    private Vector3 _scale = Vector3.one;

    private struct Surface
    {
        internal float Height;
        internal Vector3 Normal;
        internal bool Terrain;
    }

    // The surfaces at each spot around the ring, top down, and how many there are.
    private Surface[][] _spots = [];
    private int[] _counts = [];
    private int _next;
    private float _builtRadius = -1f;
    private Vector3 _builtCenter;

    // A stretch of one surface under a dash.
    private struct Run
    {
        internal bool Terrain;
        internal int First, Last;
        internal float FirstHeight, LastHeight;
        internal Vector3 Normal;
    }

    private static readonly List<Run> Runs = [];

    private void Update()
    {
        if (!Prefab || Segments <= 0 || Radius <= 0f) return;
        if (_terrain < 0)
        {
            _terrain = LayerMask.NameToLayer("terrain");
            _mask = LayerMask.GetMask("terrain", "piece");
        }
        Vector3 center = transform.position;
        if (_spots.Length == 0 || !Mathf.Approximately(_builtRadius, Radius) || Utils.DistanceXZ(_builtCenter, center) > 0.1f) Build(center);
        else Refresh(center);

        float length = Length();
        float full = length > 0f ? length : FallbackLength;
        _used = 0;
        float step = Mathf.PI * 2f / Segments, turn = Time.time * Speed;
        for (int i = 0; i < Segments; i++)
        {
            float middle = (i * step + turn) * Radius;
            Pieces(center, middle - full * 0.5f, middle + full * 0.5f, full, length > 0f);
        }
        for (int i = _used; i < _dashes.Count; i++)
            if (_dashes[i] && _dashes[i].activeSelf) _dashes[i].SetActive(false);
    }

    // ---- The surfaces around the ring ----

    private void Build(Vector3 center)
    {
        int count = Mathf.Max(16, Mathf.CeilToInt(Mathf.PI * 2f * Radius / Spacing));
        _spots = new Surface[count][];
        _counts = new int[count];
        for (int k = 0; k < count; k++)
        {
            _spots[k] = new Surface[MaxFloors];
            Look(center, k);
        }
        _builtRadius = Radius;
        _builtCenter = center;
        _next = 0;
    }

    private void Refresh(Vector3 center)
    {
        int count = _spots.Length;
        for (int n = 0; n < RefreshPerFrame && n < count; n++)
        {
            Look(center, _next);
            _next = (_next + 1) % count;
        }
    }

    // The ground and the built floors at spot k, top down.
    private void Look(Vector3 center, int k)
    {
        Vector3 at = Point(center, k * Spacing);
        int count = Physics.RaycastNonAlloc(at + Vector3.up * From, Vector3.down, Hits, From * 2f, _mask);
        // Highest first.
        for (int i = 1; i < count; i++)
        {
            RaycastHit hit = Hits[i];
            int j = i - 1;
            for (; j >= 0 && Hits[j].point.y < hit.point.y; j--) Hits[j + 1] = Hits[j];
            Hits[j + 1] = hit;
        }
        Surface[] surfaces = _spots[k];
        int found = 0;
        float last = float.PositiveInfinity;
        for (int i = 0; i < count && found < MaxFloors; i++)
        {
            RaycastHit hit = Hits[i];
            // Ships and carts are no floor.
            if (!hit.collider || hit.collider.attachedRigidbody) continue;
            bool terrain = hit.collider.gameObject.layer == _terrain;
            if (!terrain && hit.normal.y < MinUp) continue;
            if (last - hit.point.y < MinGap) continue;
            last = hit.point.y;
            surfaces[found++] = new Surface { Height = hit.point.y, Normal = hit.normal, Terrain = terrain };
        }
        _counts[k] = found;
    }

    // ---- The dashes ----

    // The pieces of the dash between these distances along the ring that lie on a surface.
    private void Pieces(Vector3 center, float from, float to, float full, bool scaled)
    {
        int count = _spots.Length;
        int k0 = Mathf.FloorToInt(from / Spacing), k1 = Mathf.CeilToInt(to / Spacing);
        Runs.Clear();
        for (int k = k0; k <= k1; k++)
        {
            int spot = ((k % count) + count) % count;
            Surface[] surfaces = _spots[spot];
            for (int s = 0; s < _counts[spot]; s++)
            {
                Surface surface = surfaces[s];
                int run = Continues(surface, k);
                if (run >= 0)
                {
                    Run r = Runs[run];
                    r.Last = k;
                    r.LastHeight = surface.Height;
                    Runs[run] = r;
                }
                else Runs.Add(new Run
                {
                    Terrain = surface.Terrain, First = k, Last = k, FirstHeight = surface.Height, LastHeight = surface.Height,
                    Normal = surface.Normal
                });
            }
        }
        foreach (Run r in Runs)
        {
            float start = Mathf.Max(from, (r.First - 0.5f) * Spacing), end = Mathf.Min(to, (r.Last + 0.5f) * Spacing);
            if (end - start < MinPiece) continue;
            float middle = (start + end) * 0.5f;
            float span = (r.Last - r.First) * Spacing;
            float t = span > 0f ? Mathf.Clamp01((middle - r.First * Spacing) / span) : 0f;
            Vector3 at = Point(center, middle);
            at.y = Mathf.Lerp(r.FirstHeight, r.LastHeight, t);
            float angle = middle / Radius;
            Vector3 along = new(Mathf.Cos(angle), 0f, -Mathf.Sin(angle));
            Place(at, along, r.Normal, scaled ? (end - start) / full : 1f, scaled);
        }
    }

    // The run this surface carries on from the spot before (the ground follows the ground; a floor the same floor), or -1.
    private static int Continues(Surface surface, int k)
    {
        int best = -1;
        float nearest = float.MaxValue;
        for (int i = 0; i < Runs.Count; i++)
        {
            Run r = Runs[i];
            if (r.Last != k - 1 || r.Terrain != surface.Terrain) continue;
            float gap = Mathf.Abs(surface.Height - r.LastHeight);
            if (!surface.Terrain && gap > SameFloor) continue;
            if (gap >= nearest) continue;
            nearest = gap;
            best = i;
        }
        return best;
    }

    // The point this far along the ring (at the center's height).
    private Vector3 Point(Vector3 center, float distance)
    {
        float angle = distance / Radius;
        return center + new Vector3(Mathf.Sin(angle) * Radius, 0f, Mathf.Cos(angle) * Radius);
    }

    // The dash's length along its direction, measured once from a copy lying unturned (0 when it can't be).
    private float Length()
    {
        if (_length >= 0f) return _length;
        _scale = Prefab.transform.localScale;
        GameObject probe = Instantiate(Prefab, Vector3.zero, Quaternion.identity);
        float length = 0f;
        foreach (Renderer renderer in probe.GetComponentsInChildren<Renderer>(true))
            length = Mathf.Max(length, renderer.bounds.size.z);
        Destroy(probe);
        return _length = length;
    }

    private GameObject NewDash(Vector3 at)
    {
        GameObject dash = Instantiate(Prefab, at, Quaternion.identity, transform);
        if (Tint is not { } color) return dash;
        _block ??= new MaterialPropertyBlock();
        foreach (Renderer renderer in dash.GetComponentsInChildren<Renderer>(true))
        {
            Material? material = renderer.sharedMaterial;
            if (!material) continue;
            renderer.GetPropertyBlock(_block);
            foreach (string property in DomainVisuals.TintProperties)
            {
                if (!material!.HasProperty(property)) continue;
                Color was = material.GetColor(property);
                _block.SetColor(property, property == "_EmissionColor"
                    ? color * Mathf.Max(1f, was.maxColorComponent)
                    : new Color(color.r, color.g, color.b, was.a));
            }
            renderer.SetPropertyBlock(_block);
            _block.Clear();
        }
        return dash;
    }

    private void Place(Vector3 at, Vector3 along, Vector3 normal, float share, bool scaled)
    {
        if (_used == _dashes.Count) _dashes.Add(NewDash(at));
        GameObject dash = _dashes[_used++];
        if (!dash) dash = _dashes[_used - 1] = NewDash(at);
        if (!dash.activeSelf) dash.SetActive(true);
        Vector3 forward = Vector3.ProjectOnPlane(along, normal);
        dash.transform.SetPositionAndRotation(at, Quaternion.LookRotation(forward.sqrMagnitude > 0.001f ? forward : along, Vector3.up));
        if (scaled) dash.transform.localScale = new Vector3(_scale.x, _scale.y, _scale.z * share);
    }
}
