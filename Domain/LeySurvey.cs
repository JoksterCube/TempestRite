using System;
using System.Collections.Generic;
using System.Text;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The Ley-Survey Rite: shows, around its obliterator, whether a Crimson, Ragnarök or Hoard-Bane Rite could be fought
// there, and what stands in the way:
// - a red ring where the arena's edge would be (the widest of the three);
// - a red sheet at sea level over the whole arena (the same line as the water check, see IncursionArena.DryGroundAt): wherever it can be seen, the ground is under the sea, and a
//   floor built over it makes it dry;
// - a red beam over everything that keeps the arena away (claimed beds, dungeon entrances, the world's spawn, traders,
//   boss altars, bosses, wards, shield generators, the Sovereign Ward Rite);
// - hostile creatures inside outlined by PingMark, when it is installed (see PingMarkLink);
// - only for a rite without room enough for its creatures to rise (see ArenaRoom), which can't be performed there: the
//   spots that have room in orange (the Crimson Rite's raid creatures; the Ragnarök Rite's bosses, one per slice of
//   their ring, a slice without any red; the Hoard-Bane Rite's lairs and miniboss post, turned to where they have the
//   most room, a post without room red);
// - its status lists what is in the way.
// It shows to each player within the ring or 10 m beyond it, in their own game, and fades 5 seconds after they go farther (back when they
// step in again). It ends when its time runs out, its obliterator is destroyed (see DomainAnchors) or any other
// Reckoning rite is performed (see WeatherState.Start).
// A refused Crimson, Ragnarök or Hoard-Bane Rite gives the player who pulled the lever a glimpse of the same, for 30
// seconds (the Arena Bounds status): it ends 5 seconds after they go more than 10 m beyond the ring, when its obliterator is
// destroyed, when a Reckoning rite is performed, or when the status is cleared.
internal static class LeySurvey
{
    private const string Feature = "Ley-Survey";
    private const float LeaveGrace = 5f;
    // It keeps showing this far beyond the ring; only farther out does the fading start.
    private const float KeepNear = 10f;
    private const float ScanInterval = 2f;
    // The water check looks at the ground every few meters across the arena: not on every scan.
    private const int WaterEvery = 3;
    private const float OutlineSeconds = ScanInterval + 1.5f;
    // Right at sea level: ground the sheet covers is wet, ground above it dry, as creatures rise and the water check go.
    private const float SheetLift = 0f;
    private const int SheetSegments = 72;
    private const float BeamHeight = 40f;
    private const float BeamWidth = 0.35f;
    private const int MaxBeams = 24;
    // Dungeon rooms stand far above their entrance: a beam starts from the ground below anything this high above it.
    private const float HighAbove = 100f;
    private static readonly Color Red = new(1f, 0.22f, 0.18f);
    private static readonly Color Orange = new(1f, 0.5f, 0.1f);
    // The Ragnarök bosses' ring is shown in slices around the obliterator: one circle each.
    private const int BossSlices = 12;
    private const int SpotPoints = 20;
    private const float SpotLift = 0.3f;
    private const float SpotWidth = 0.08f;
    private static readonly int GlimpseHash = StatusCommands.ArenaPreviewStatus.GetStableHashCode();

    // The glimpse given by a refused arena rite.
    private static bool _glimpse;
    private static Vector3 _glimpseAt;
    private static ZDOID _glimpseObliterator = ZDOID.None;
    private static float _glimpseOutsideSince = -1f;
    private static StatusEffect? _effect;

    // What shows now.
    private static bool _showing;
    private static Vector3 _center;
    private static float _radius;
    private static float _outsideSince = -1f;
    private static float _nextScan;
    private static int _scans;
    private static bool _scanned;
    private static readonly IncursionSite.Findings Findings = new();
    private static readonly ArenaRoom.Survey Room = new();

    private static GameObject? _root;
    private static DomainVisuals.ColoredEdge? _edge;
    private static GameObject? _sheet;
    private static readonly List<LineRenderer> Beams = [];
    private static readonly List<LineRenderer> Spots = [];
    private static Mesh? _disc;
    private static Material? _material;

    internal static void ClearSession()
    {
        Hide();
        EndGlimpse();
    }

    // In the game of the player who pulled the lever: an arena rite was refused there.
    internal static void Glimpse(WeatherKind kind, Vector3 center)
    {
        if (PatchGuard.IsDisabled(Feature) || !WeatherKinds.IsIncursion(kind)) return;
        try
        {
            float seconds = PluginConfig.LeySurveyRefusedSeconds.Value;
            Player? me = Player.m_localPlayer;
            if (seconds <= 0f || !me || me!.IsDead()) return;
            _glimpse = true;
            _glimpseAt = center;
            _glimpseObliterator = DomainAnchors.FindObliterator(center);
            _glimpseOutsideSince = -1f;
            _nextScan = 0f;
            AddStatus(me, seconds);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Its status was cleared (clearstatus, or another mod): the glimpse goes with it.
    internal static void Dismiss() => _glimpse = false;

    // A rite was performed in this game: any other Reckoning rite ends the glimpse (the rite itself ends in WeatherState).
    internal static void Performed(WeatherKind kind)
    {
        if (WeatherKinds.EndsSurvey(kind)) EndGlimpse();
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
        Player? me = Player.m_localPlayer;
        if (!me || me!.IsDead() || !ZoneSystem.instance)
        {
            Hide();
            return;
        }
        Vector3 at = me.transform.position;
        float radius = PluginConfig.ArenaRadius;
        KeepGlimpse(me, at, radius);

        // The rite wins over a glimpse when the player stands in both.
        bool rite = WeatherState.IsActive(WeatherKind.LeySurvey);
        Vector3 riteAt = WeatherState.Position(WeatherKind.LeySurvey);
        bool found = false;
        Vector3 center = Vector3.zero;
        if (rite && Utils.DistanceXZ(at, riteAt) <= radius + KeepNear)
        {
            found = true;
            center = riteAt;
        }
        else if (_glimpse && Utils.DistanceXZ(at, _glimpseAt) <= radius + KeepNear)
        {
            found = true;
            center = _glimpseAt;
        }

        if (found) _outsideSince = -1f;
        else if (_showing && StillThere(rite, riteAt))
        {
            // Just went more than 10 m beyond the ring: it lingers a moment.
            if (_outsideSince < 0f) _outsideSince = Time.time;
            if (Time.time - _outsideSince <= LeaveGrace)
            {
                Show(_center, _radius);
                return;
            }
        }
        if (!found)
        {
            Hide();
            return;
        }
        Show(center, radius);
    }

    // Whether what was shown still goes on: the rite there, or the glimpse there.
    private static bool StillThere(bool rite, Vector3 riteAt) =>
        (rite && Same(riteAt, _center)) || (_glimpse && Same(_glimpseAt, _center));

    private static bool Same(Vector3 a, Vector3 b) => Utils.DistanceXZ(a, b) < 0.5f;

    // The glimpse ends with its status, an arena begun, its obliterator gone, or 5 seconds outside its ring.
    private static void KeepGlimpse(Player me, Vector3 at, float radius)
    {
        if (!_glimpse) return;
        StatusEffect? status = me.GetSEMan().GetStatusEffect(GlimpseHash);
        bool gone = !status || IncursionArena.Kind != WeatherKind.None ||
                    (!_glimpseObliterator.IsNone() && ZDOMan.instance != null && ZDOMan.instance.GetZDO(_glimpseObliterator) == null);
        if (!gone)
        {
            if (Utils.DistanceXZ(at, _glimpseAt) <= radius + KeepNear) _glimpseOutsideSince = -1f;
            else
            {
                if (_glimpseOutsideSince < 0f) _glimpseOutsideSince = Time.time;
                gone = Time.time - _glimpseOutsideSince > LeaveGrace;
            }
        }
        if (gone) EndGlimpse();
        else if (_scanned && Same(_center, _glimpseAt)) status!.m_tooltip = Messages.StatusTooltipArenaPreview + Report();
    }

    private static void EndGlimpse()
    {
        _glimpse = false;
        _glimpseObliterator = ZDOID.None;
        Player? me = Player.m_localPlayer;
        if (me && me!.GetSEMan().GetStatusEffect(GlimpseHash)) StatusCommands.RemoveQuietly(me.GetSEMan(), GlimpseHash);
    }

    // Given again at its full time on each refusal.
    private static void AddStatus(Player me, float seconds)
    {
        if (!_effect)
        {
            _effect = ScriptableObject.CreateInstance<StatusEffect>();
            _effect.name = StatusCommands.ArenaPreviewStatus;
            _effect.hideFlags = HideFlags.HideAndDontSave;
        }
        _effect!.m_name = Messages.ArenaPreviewName;
        // The HUD only lists effects with an icon.
        _effect.m_icon = WeatherIcons.BloodGrounds();
        SEMan seMan = me.GetSEMan();
        StatusEffect? current = seMan.GetStatusEffect(GlimpseHash);
        if (!current) current = seMan.AddStatusEffect(_effect, true);
        if (!current) return;
        current!.m_icon = _effect.m_icon;
        current.m_ttl = seconds;
        current.m_time = 0f;
        current.m_tooltip = Messages.StatusTooltipArenaPreview;
    }

    // ---- What is in the way, for the statuses' tooltips ----

    // The lines added under a status: everything in the way, or that nothing is.
    internal static string Report()
    {
        if (!_scanned) return string.Empty;
        if (Findings.Clear && Room.Clear) return "\n\n" + Messages.LeySurveyClear;
        List<string> parts = [];
        if (Findings.Raid) parts.Add(Messages.IncursionSiteRaid);
        if (Findings.BossFight) parts.Add(Messages.LeySurveyBossFight);
        Dictionary<string, int> counts = [];
        foreach (IncursionSite.Blocker blocker in Findings.Blockers)
        {
            counts.TryGetValue(blocker.What, out int count);
            if (count == 0) parts.Add(blocker.What);
            counts[blocker.What] = count + 1;
        }
        for (int i = 0; i < parts.Count; i++)
            if (counts.TryGetValue(parts[i], out int n) && n > 1) parts[i] += $" ({n})";
        if (Findings.Hostiles.Count > 0) parts.Add(string.Format(Messages.LeySurveyHostiles, Findings.Hostiles.Count));
        if (Findings.Wet) parts.Add(Messages.LeySurveyWater);
        StringBuilder text = new("\n\n");
        if (parts.Count > 0) text.Append(string.Format(Messages.LeySurveyBlocked, string.Join(", ", parts)));
        if (!Room.Clear)
        {
            List<string> cramped = [];
            if (!Room.Crimson) cramped.Add(WeatherKinds.Name(WeatherKind.Crimson));
            if (!Room.Ragnarok) cramped.Add(WeatherKinds.Name(WeatherKind.Ragnarok));
            if (!Room.HoardBane) cramped.Add(WeatherKinds.Name(WeatherKind.HoardBane));
            if (parts.Count > 0) text.Append('\n');
            text.Append(string.Format(Messages.LeySurveyCramped, string.Join(", ", cramped)));
        }
        return text.ToString();
    }

    // ---- In the world ----

    private static void Show(Vector3 center, float radius)
    {
        if (!_showing || !Same(center, _center) || !Mathf.Approximately(radius, _radius))
        {
            Hide();
            _showing = true;
            _center = center;
            _radius = radius;
            _scans = 0;
            _scanned = false;
            _nextScan = 0f;
            Build();
        }
        if (_edge != null) DomainVisuals.Keep(_edge);
        if (Time.time < _nextScan) return;
        _nextScan = Time.time + ScanInterval;
        bool full = _scans % WaterEvery == 0;
        IncursionSite.Survey(center, radius, Findings, checkWater: full);
        // Room takes a few hundred looks at the ground: with the water.
        if (full)
        {
            ArenaRoom.Look(center, Room);
            MarkRoom();
        }
        _scans++;
        _scanned = true;
        Mark();
        PingMarkLink.Highlight(Findings.Hostiles, OutlineSeconds);
    }

    private static void Hide()
    {
        _showing = false;
        _scanned = false;
        _outsideSince = -1f;
        DomainVisuals.Remove(_edge);
        _edge = null;
        if (_root) UnityEngine.Object.Destroy(_root);
        _root = null;
        _sheet = null;
        Beams.Clear();
        Spots.Clear();
    }

    private static void Build()
    {
        _root = new GameObject("TempestRite_LeySurvey");
        _root.transform.position = _center;
        _edge = DomainVisuals.ColorEdge(_center, _radius, Red);
        Material? material = Material();
        if (!material) return;
        _sheet = new GameObject("Sheet");
        _sheet.transform.SetParent(_root.transform, false);
        _sheet.transform.position = new Vector3(_center.x, ZoneSystem.instance.m_waterLevel + SheetLift, _center.z);
        _sheet.transform.localScale = new Vector3(_radius, 1f, _radius);
        _sheet.AddComponent<MeshFilter>().sharedMesh = Disc();
        MeshRenderer renderer = _sheet.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // A beam over each thing in the way, as many as were found (up to a few dozen).
    private static void Mark()
    {
        if (!_root) return;
        int count = Mathf.Min(Findings.Blockers.Count, MaxBeams);
        while (Beams.Count < count) Beams.Add(Beam());
        for (int i = 0; i < Beams.Count; i++)
        {
            LineRenderer beam = Beams[i];
            if (!beam) continue;
            beam.enabled = i < count;
            if (i >= count) continue;
            Vector3 at = Findings.Blockers[i].At;
            float ground = ZoneSystem.instance.GetSolidHeight(at, out float solid) ? solid : at.y;
            float bottom = at.y > ground + HighAbove ? ground : at.y;
            beam.SetPosition(0, new Vector3(at.x, bottom, at.z));
            beam.SetPosition(1, new Vector3(at.x, bottom + BeamHeight, at.z));
        }
    }

    // Circles only for the rites without room enough: where their creatures could rise (orange) and where they can't (red).
    // A rite with room shows none.
    private static void MarkRoom()
    {
        if (!_root) return;
        List<(Vector3 At, float Radius, Color Color)> wanted = [];
        if (!Room.Crimson)
            foreach (Vector3 spot in Room.CrimsonAt)
                wanted.Add((spot, ArenaRoom.Raider.Radius, Orange));
        if (!Room.Ragnarok) BossSpots(wanted);
        if (!Room.HoardBane)
            foreach ((Vector3 at, bool room, bool boss) in Room.HoardBanePosts)
                wanted.Add((at, boss ? ArenaRoom.Boss.Radius : ArenaRoom.Elite.Radius, room ? Orange : Red));
        while (Spots.Count < wanted.Count) Spots.Add(Spot());
        for (int i = 0; i < Spots.Count; i++)
        {
            LineRenderer line = Spots[i];
            if (!line) continue;
            line.enabled = i < wanted.Count;
            if (i >= wanted.Count) continue;
            (Vector3 at, float r, Color color) = wanted[i];
            for (int k = 0; k < SpotPoints; k++)
            {
                float angle = k * Mathf.PI * 2f / SpotPoints;
                line.SetPosition(k, at + new Vector3(Mathf.Cos(angle) * r, SpotLift, Mathf.Sin(angle) * r));
            }
            Color c = new(color.r, color.g, color.b, 0.85f);
            line.startColor = c;
            line.endColor = c;
        }
    }

    // One circle in each slice of the ring where Ragnarök bosses rise, spread all around: on the spot with room nearest
    // the middle of the slice (orange), or red in the middle of a slice with no room at all.
    private static void BossSpots(List<(Vector3 At, float Radius, Color Color)> wanted)
    {
        if (!PluginConfig.IsEnabled(WeatherKind.Ragnarok)) return;
        (float clear, float reach) = RagnarokArena.SpawnRing();
        float middle = (clear + reach) * 0.5f;
        for (int slice = 0; slice < BossSlices; slice++)
        {
            float angle = (slice + 0.5f) * Mathf.PI * 2f / BossSlices;
            Vector3 aim = _center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * middle;
            bool found = false;
            Vector3 best = aim;
            float nearest = float.MaxValue;
            foreach (Vector3 spot in Room.RagnarokSpots)
            {
                if (Slice(spot) != slice) continue;
                float distance = Utils.DistanceXZ(spot, aim);
                if (distance >= nearest) continue;
                nearest = distance;
                best = spot;
                found = true;
            }
            if (!found) best.y = IncursionArena.GroundAt(aim, out float ground) ? ground : _center.y;
            wanted.Add((best, ArenaRoom.Boss.Radius, found ? Orange : Red));
        }
    }

    private static int Slice(Vector3 spot)
    {
        float angle = Mathf.Atan2(spot.z - _center.z, spot.x - _center.x);
        return Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(angle, Mathf.PI * 2f) / (Mathf.PI * 2f) * BossSlices), 0, BossSlices - 1);
    }

    private static LineRenderer Spot()
    {
        GameObject go = new("Spot");
        go.transform.SetParent(_root!.transform, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = SpotPoints;
        line.widthMultiplier = SpotWidth;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        Material? material = Material();
        if (material) line.sharedMaterial = material;
        return line;
    }

    private static LineRenderer Beam()
    {
        GameObject go = new("Beam");
        go.transform.SetParent(_root!.transform, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.widthMultiplier = BeamWidth;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.startColor = new Color(Red.r, Red.g, Red.b, 0.85f);
        line.endColor = new Color(Red.r, Red.g, Red.b, 0f);
        Material? material = Material();
        if (material) line.sharedMaterial = material;
        return line;
    }

    // A flat disc of radius 1, see-through red, scaled to the arena.
    private static Mesh Disc()
    {
        if (_disc) return _disc!;
        Vector3[] vertices = new Vector3[SheetSegments + 1];
        Color[] colors = new Color[SheetSegments + 1];
        int[] triangles = new int[SheetSegments * 3];
        Color center = new(Red.r, Red.g, Red.b, 0.32f), edge = new(Red.r, Red.g, Red.b, 0.5f);
        vertices[0] = Vector3.zero;
        colors[0] = center;
        for (int i = 0; i < SheetSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / SheetSegments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            colors[i + 1] = edge;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i == SheetSegments - 1 ? 1 : i + 2;
            triangles[i * 3 + 2] = i + 1;
        }
        _disc = new Mesh { name = "TempestRite_LeySurveySheet", vertices = vertices, colors = colors, triangles = triangles, hideFlags = HideFlags.HideAndDontSave };
        _disc.RecalculateNormals();
        _disc.RecalculateBounds();
        return _disc;
    }

    private static Material? Material()
    {
        if (_material) return _material;
        Shader? shader = Shader.Find("Sprites/Default");
        if (!shader) return null;
        // Seen from above and below (Sprites/Default draws both sides), hidden by the ground over it.
        _material = new Material(shader) { name = "TempestRite_LeySurvey", renderQueue = 3100, hideFlags = HideFlags.HideAndDontSave };
        return _material;
    }
}
