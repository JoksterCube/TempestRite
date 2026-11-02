using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// Fight music for the players inside a Crimson, Ragnarök or Hoard-Bane Rite's bubble, in their game only. The game plays
// a raid's or a boss's music while one is on (its event music, see MusicMan.HandleEventMusic); between raids, or between
// bosses, it would fall back to calm biome music. Inside the bubble this fills those gaps:
// - Ragnarök: the music of the boss fighting there (the nearest Ragnarök boss), or of the last one;
// - Crimson: the music of the raid going on, or of the last one;
// - otherwise (Hoard-Bane, or nothing yet): the music of a raid of the bubble's biome, else of any raid.
// The game's own boss and raid music still wins whenever it has some.
internal static class ArenaMusic
{
    private const float PickInterval = 2f;

    private static WeatherKind _kind;
    private static Vector3 _at;
    private static string? _music;
    private static float _nextPick;

    // The music to play now for the local player, or null (not in a bubble, or nothing fits).
    internal static string? Now()
    {
        Player? player = Player.m_localPlayer;
        WeatherKind kind = IncursionArena.Kind;
        if (!player || player!.IsDead() || kind == WeatherKind.None || !IncursionArena.InBubble(player.transform.position)) return null;
        Vector3 center = WeatherState.Position(kind);
        if (kind != _kind || Utils.DistanceXZ(center, _at) > IncursionArena.SamePlace)
        {
            _kind = kind;
            _at = center;
            _music = null;
            _nextPick = 0f;
        }
        if (Time.time >= _nextPick)
        {
            _nextPick = Time.time + PickInterval;
            string? fighting = kind switch
            {
                WeatherKind.Ragnarok => BossMusic(player.transform.position),
                WeatherKind.Crimson => RaidMusic(),
                _ => null
            };
            if (fighting != null) _music = fighting;
            _music ??= BiomeMusic(center);
        }
        return _music;
    }

    // The music of the nearest Ragnarök boss's fight.
    private static string? BossMusic(Vector3 from)
    {
        Character? nearest = null;
        float best = float.MaxValue;
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!character || character.IsDead() || string.IsNullOrEmpty(character.m_bossEvent) || !IncursionArena.IsRagnarokBoss(character)) continue;
            float distance = Vector3.Distance(from, character.transform.position);
            if (distance >= best) continue;
            best = distance;
            nearest = character;
        }
        return nearest ? EventMusic(nearest!.m_bossEvent) : null;
    }

    private static string? RaidMusic()
    {
        RandEventSystem? raids = RandEventSystem.instance;
        RandomEvent? raid = raids ? raids!.m_randomEvent : null;
        return raid != null && Exists(raid.m_forceMusic) ? raid.m_forceMusic : null;
    }

    // A raid's music for the biome at a point (the first raid listed there), else any raid's.
    private static string? BiomeMusic(Vector3 at)
    {
        RandEventSystem? raids = RandEventSystem.instance;
        if (!raids) return null;
        Heightmap.Biome biome = WorldGenerator.instance != null ? WorldGenerator.instance.GetBiome(at) : Heightmap.Biome.None;
        string? any = null;
        foreach (RandomEvent raid in raids!.m_events)
        {
            if (raid == null || !raid.m_random || !Exists(raid.m_forceMusic)) continue;
            if ((raid.m_biome & biome) != 0) return raid.m_forceMusic;
            any ??= raid.m_forceMusic;
        }
        return any;
    }

    private static string? EventMusic(string name)
    {
        RandEventSystem? raids = RandEventSystem.instance;
        if (!raids) return null;
        List<RandomEvent> events = raids!.m_events;
        foreach (RandomEvent ev in events)
            if (ev != null && ev.m_name == name) return Exists(ev.m_forceMusic) ? ev.m_forceMusic : null;
        return null;
    }

    // Only music the game has: an unknown name would stop the music instead.
    private static bool Exists(string? name) =>
        !string.IsNullOrEmpty(name) && MusicMan.instance && MusicMan.instance.FindMusic(name) != null;
}
