using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// When and where the rites that speed things up (Verdant, Vat-Boil, Pitch-Bellows, Kindred, Taproot-Surge) went on,
// in world time, so what they reach is still sped up for that time when nobody was near (see RiteSpeedup): crops left
// under the Verdant Rite have grown faster when their owner comes back, even after the rite ended.
//
// Every game follows the rites going on by itself; the server saves the spans with the world (see WeatherStore) and
// sends them to players with the rites (see WeatherSync), whose copy then takes its place.
internal static class RiteHistory
{
    // One rite's time: from start to end in world seconds, at most duration of it counted (sleeping skips world time).
    internal sealed class Span(WeatherKind kind, Vector3 position, float radius, double start, double end, float duration)
    {
        internal WeatherKind Kind { get; } = kind;
        internal Vector3 Position { get; } = position;
        internal float Radius { get; } = radius;
        internal double Start { get; } = start;
        internal double End { get; set; } = end;
        internal float Duration { get; set; } = duration;

        internal bool Reaches(Vector3 point)
        {
            float x = point.x - Position.x, z = point.z - Position.z;
            return x * x + z * z <= Radius * Radius;
        }
    }

    // Older spans of a rite are dropped past this many.
    private const int KeepPerKind = 10;
    // A span the rite going on carries on: same obliterator, and it ended no longer ago than this (a game's last look,
    // or the server's last word).
    private const double CarryOnSeconds = 90.0;

    private static readonly List<Span> Spans = [];

    internal static bool Any => Spans.Count > 0;

    internal static IReadOnlyList<Span> All => Spans;

    internal static void ClearSession() => Spans.Clear();

    // Whether any span of this rite reaches the point.
    internal static bool Reaches(WeatherKind kind, Vector3 point)
    {
        foreach (Span span in Spans)
            if (span.Kind == kind && span.Reaches(point)) return true;
        return false;
    }

    // The world seconds a rite worked at the point after `from`, up to now.
    internal static double Owed(WeatherKind kind, Vector3 point, double from, double now) => Owed(Spans, kind, point, from, now);

    internal static double Owed(IEnumerable<Span> spans, WeatherKind kind, Vector3 point, double from, double now)
    {
        double owed = 0.0;
        foreach (Span span in spans)
        {
            if (span.Kind != kind || !span.Reaches(point)) continue;
            double seconds = Math.Min(now, span.End) - Math.Max(from, span.Start);
            if (seconds > 0.0) owed += Math.Min(seconds, span.Duration > 0f ? span.Duration : seconds);
        }
        return owed;
    }

    // Whether an object last sped up at `from` still has rite time owed at the point (for handing it to players with
    // the mod, see WeatherOwnership).
    internal static bool Pending(WeatherKind kind, Vector3 point, double from)
    {
        foreach (Span span in Spans)
            if (span.Kind == kind && span.End > from + 1.0 && span.Reaches(point)) return true;
        return false;
    }

    // Every second: the rites going on carry their span on to now, or start one.
    internal static void Track(IEnumerable<WeatherKind> kinds, double now)
    {
        foreach (WeatherKind kind in kinds)
        {
            if (!WeatherState.IsActive(kind)) continue;
            Track(Spans, kind, WeatherState.Position(kind), PluginConfig.Radius(kind), WeatherState.Duration(kind), now);
        }
    }

    internal static void Track(List<Span> spans, WeatherKind kind, Vector3 position, float radius, float duration, double now)
    {
        Span? last = null;
        foreach (Span span in spans)
            if (span.Kind == kind && (last == null || span.Start > last.Start)) last = span;
        if (last != null && (last.Position - position).sqrMagnitude < 1f && now - last.End <= CarryOnSeconds && now >= last.Start)
        {
            last.End = Math.Max(last.End, now);
            last.Duration = Math.Max(last.Duration, duration);
            return;
        }
        spans.Add(new Span(kind, position, radius, now, now, duration));
        int count = spans.Count(s => s.Kind == kind);
        while (count-- > KeepPerKind)
        {
            Span oldest = spans.Where(s => s.Kind == kind).OrderBy(s => s.Start).First();
            spans.Remove(oldest);
        }
    }

    // What the server said: it has the last word.
    internal static void SetAll(List<Span> spans)
    {
        Spans.Clear();
        Spans.AddRange(spans);
    }

    // After the world loaded: a span saved after the world last was can't end later than now.
    internal static void Load(List<Span> spans, double now)
    {
        Spans.Clear();
        foreach (Span span in spans)
        {
            if (span.Start > now) continue;
            span.End = Math.Min(span.End, now);
            Spans.Add(span);
        }
    }

    // ---- Saved and sent ----

    private const string LinePrefix = "h";

    // One span per line: h;kind;start;end;duration;radius;x;y;z.
    internal static string Format(IEnumerable<Span> spans) =>
        string.Join("\n", spans.Select(s => string.Join(";",
            LinePrefix,
            ((int)s.Kind).ToString(CultureInfo.InvariantCulture),
            s.Start.ToString("0.#", CultureInfo.InvariantCulture),
            s.End.ToString("0.#", CultureInfo.InvariantCulture),
            s.Duration.ToString("0.#", CultureInfo.InvariantCulture),
            s.Radius.ToString("0.#", CultureInfo.InvariantCulture),
            s.Position.x.ToString("0.##", CultureInfo.InvariantCulture),
            s.Position.y.ToString("0.##", CultureInfo.InvariantCulture),
            s.Position.z.ToString("0.##", CultureInfo.InvariantCulture))));

    // Reads the span lines, skipping all others (the rites going on, see WeatherStore).
    internal static List<Span> Parse(string text)
    {
        List<Span> spans = [];
        foreach (string line in text.Split('\n'))
        {
            string[] parts = line.Trim().Split(';');
            if (parts.Length != 9 || parts[0] != LinePrefix) continue;
            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int kind)) continue;
            if (!Double(parts[2], out double start) || !Double(parts[3], out double end) || end < start) continue;
            if (!Double(parts[4], out double duration) || !Double(parts[5], out double radius)) continue;
            if (!Double(parts[6], out double x) || !Double(parts[7], out double y) || !Double(parts[8], out double z)) continue;
            if (!WeatherKinds.HasSlot((WeatherKind)kind)) continue;
            spans.Add(new Span((WeatherKind)kind, new Vector3((float)x, (float)y, (float)z), (float)radius, start, end, (float)duration));
        }
        return spans;
    }

    private static bool Double(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    internal static void Write(ZPackage package)
    {
        package.Write(Spans.Count);
        foreach (Span span in Spans)
        {
            package.Write((int)span.Kind);
            package.Write(span.Position);
            package.Write(span.Radius);
            package.Write(span.Start);
            package.Write(span.End);
            package.Write(span.Duration);
        }
    }

    internal static List<Span> Read(ZPackage package)
    {
        int count = package.ReadInt();
        List<Span> spans = new(count);
        for (int i = 0; i < count; i++)
        {
            WeatherKind kind = (WeatherKind)package.ReadInt();
            Vector3 position = package.ReadVector3();
            float radius = package.ReadSingle();
            double start = package.ReadDouble();
            double end = package.ReadDouble();
            float duration = package.ReadSingle();
            spans.Add(new Span(kind, position, radius, start, end, duration));
        }
        return spans;
    }
}
