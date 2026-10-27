using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants.Plugin;

namespace JoksterCube.TempestRite.Domain;

// On the server (and in single player or when hosting), called effects are saved with the world, so they go on with
// the time they had left after a restart. One small file per world, in BepInEx/config/JoksterCube.TempestRite/.
internal static class WeatherStore
{
    private const float SaveInterval = 30f;

    private static string? _world;
    private static float _nextSave;
    private static string _lastSaved = string.Empty;

    internal static void ClearSession()
    {
        _world = null;
        _nextSave = 0f;
        _lastSaved = string.Empty;
    }

    // Every frame: loads once the world is known, then saves now and then while something is going on.
    internal static void Update()
    {
        if (!ZNet.instance || !ZNet.instance.IsServer()) return;
        if (_world == null)
        {
            string world = ZNet.instance.GetWorldName();
            if (string.IsNullOrEmpty(world)) return;
            _world = world;
            Load();
            _nextSave = Time.time + SaveInterval;
            return;
        }
        if (Time.time < _nextSave) return;
        _nextSave = Time.time + SaveInterval;
        Save();
    }

    // Right away after something was called, and when the world closes.
    internal static void Save()
    {
        if (_world == null || !ZNet.instance || !ZNet.instance.IsServer()) return;
        string text = Format(WeatherState.Entries());
        if (text == _lastSaved) return;
        try
        {
            string path = PathFor(_world);
            if (text.Length == 0)
            {
                if (File.Exists(path)) File.Delete(path);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, text);
            }
            _lastSaved = text;
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogWarning($"Couldn't save rites for {_world}: {exception.Message}");
        }
    }

    private static void Load()
    {
        try
        {
            string path = PathFor(_world!);
            if (!File.Exists(path)) return;
            string text = File.ReadAllText(path);
            List<WeatherState.Entry> entries = Parse(text);
            foreach (WeatherState.Entry entry in entries) WeatherState.Set(entry);
            _lastSaved = text;
            if (entries.Count > 0)
                Plugin.ModLogger.LogInfo($"Rites go on: {string.Join(", ", entries.Select(e => $"{e.Kind} ({TimeText.Duration(e.Remaining)} left)"))}.");
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogWarning($"Couldn't read rites for {_world}: {exception.Message}");
        }
    }

    private static string PathFor(string world)
    {
        foreach (char bad in Path.GetInvalidFileNameChars()) world = world.Replace(bad, '_');
        return Path.Combine(Paths.ConfigPath, ModGUID, world + ".txt");
    }

    // One rite per line: kind;seconds left;total seconds;x;y;z;called by (where it was performed, for located rites).
    // Lines saved before rites had a place (kind;seconds left;total seconds;called by) are read too.
    internal static string Format(List<WeatherState.Entry> entries) =>
        string.Join("\n", entries.Select(e => string.Join(";",
            ((int)e.Kind).ToString(CultureInfo.InvariantCulture),
            e.Remaining.ToString("0.#", CultureInfo.InvariantCulture),
            e.Duration.ToString("0.#", CultureInfo.InvariantCulture),
            e.Position.x.ToString("0.##", CultureInfo.InvariantCulture),
            e.Position.y.ToString("0.##", CultureInfo.InvariantCulture),
            e.Position.z.ToString("0.##", CultureInfo.InvariantCulture),
            e.CalledBy.Replace("\n", " "))));

    internal static List<WeatherState.Entry> Parse(string text)
    {
        List<WeatherState.Entry> entries = [];
        foreach (string line in text.Split('\n'))
        {
            string[] parts = line.Trim().Split([';'], 7);
            if (parts.Length < 3) continue;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int kind)) continue;
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float remaining)) continue;
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float duration)) continue;
            if (!WeatherKinds.HasSlot((WeatherKind)kind) || remaining <= 0f) continue;

            Vector3 position = Vector3.zero;
            string calledBy;
            if (parts.Length == 7 && Float(parts[3], out float x) && Float(parts[4], out float y) && Float(parts[5], out float z))
            {
                position = new Vector3(x, y, z);
                calledBy = parts[6];
            }
            else
                // An older line: everything after the times is the name.
                calledBy = string.Join(";", parts, 3, parts.Length - 3);
            entries.Add(new WeatherState.Entry((WeatherKind)kind, remaining, duration, calledBy, position));
        }
        return entries;
    }

    private static bool Float(string text, out float value) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
