using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// Blessings: rites given only to the players standing near the obliterator when it burns (within Blessings Range, the
// range Forsaken powers are shared in). Each player keeps their own: they aren't sent to others or kept by the server,
// the Equilibrium Rite doesn't end them, and death does.
// - They are saved with the character, so logging out and back in keeps them.
// - Which ones a player has is also written on their character for other games to read (a creature's drops are
//   worked out by the game that owns it, which may not be the killer's).
// - The obliterator's lightning strikes each player who receives one: only a picture, no harm done.
internal static class Blessings
{
    private const string Feature = "Blessings";
    private const string SaveKey = "TempestRite_Blessings";
    private const float SaveInterval = 5f;
    private static readonly int BlessedKey = "TempestRite_Blessed".GetStableHashCode();
    private static readonly int FullKey = "TempestRite_Full".GetStableHashCode();

    private sealed class Held
    {
        internal float EndTime;
        internal float Duration;
        internal string CalledBy = string.Empty;
    }

    private static readonly Dictionary<WeatherKind, Held> Kept = [];
    private static readonly List<WeatherKind> Ended = [];
    private static readonly List<Player> Nearby = [];
    private static Player? _owner;
    private static float _nextSave;
    private static bool _changed;

    internal static void ClearSession()
    {
        Kept.Clear();
        _owner = null;
        _nextSave = 0f;
        _changed = false;
    }

    internal static bool IsActive(WeatherKind kind) =>
        Kept.TryGetValue(kind, out Held held) && Time.time < held.EndTime && Player.m_localPlayer && !Player.m_localPlayer.IsDead();

    internal static float Remaining(WeatherKind kind) => IsActive(kind) ? Kept[kind].EndTime - Time.time : 0f;

    internal static float Duration(WeatherKind kind) => IsActive(kind) ? Kept[kind].Duration : 0f;

    internal static string CalledBy(WeatherKind kind) => IsActive(kind) ? Kept[kind].CalledBy : string.Empty;

    // Whether a character carries the blessing: the local player as kept here, other players as written on them.
    internal static bool Has(Character? character, WeatherKind kind)
    {
        if (!character || character is not Player player) return false;
        if (player == Player.m_localPlayer) return IsActive(kind);
        ZDO? zdo = player.m_nview ? player.m_nview.GetZDO() : null;
        return zdo != null && (zdo.GetInt(BlessedKey) & Bit(kind)) != 0;
    }

    private static int Bit(WeatherKind kind) => 1 << ((int)kind % 31);

    // Whether a player's inventory has no empty slot left, as written on them (players without the mod: never).
    internal static bool IsFull(Player player)
    {
        if (player == Player.m_localPlayer) return !player.GetInventory().HaveEmptySlot();
        ZDO? zdo = player.m_nview ? player.m_nview.GetZDO() : null;
        return zdo != null && zdo.GetBool(FullKey);
    }

    // Blessings were performed at an obliterator by one burn: the lightning strikes the players standing there once,
    // and the local player receives them all if they are one of them.
    internal static void Performed(IReadOnlyList<WeatherKind> kinds, Vector3 position, bool byLocalPlayer, string calledBy)
    {
        if (kinds.Count == 0) return;
        float range = PluginConfig.BlessingRange.Value;
        Nearby.Clear();
        Player.GetPlayersInRange(position, range, Nearby);
        if (PluginConfig.ShowBlessingLightning.IsOn())
            foreach (Player player in Nearby)
                if (player && !player.IsDead()) BlessingLightning.Strike(player.transform.position);

        Player? me = Player.m_localPlayer;
        bool received = me && !me!.IsDead() && Nearby.Contains(me);
        string who = string.IsNullOrEmpty(calledBy) ? Messages.SomeoneName : calledBy;
        foreach (WeatherKind kind in kinds)
        {
            if (!WeatherKinds.Personal(kind)) continue;
            string what = WeatherKinds.Colored(kind);
            if (received)
            {
                Grant(kind, calledBy);
                PlayerMessages.Center(byLocalPlayer ? string.Format(Messages.BlessedByYou, what) : string.Format(Messages.BlessedBy, who, what));
            }
            else if (byLocalPlayer)
                PlayerMessages.Center(string.Format(Messages.BlessingMissed, what, Mathf.RoundToInt(range)));
        }
    }

    private static void Grant(WeatherKind kind, string calledBy)
    {
        WeatherConfig? config = PluginConfig.For(kind);
        if (config == null || config.Seconds <= 0f) return;
        Kept[kind] = new Held { EndTime = Time.time + config.Seconds, Duration = config.Seconds, CalledBy = calledBy };
        _changed = true;
    }

    // Every frame: picks up the character's saved blessings, ends those run out, and writes them down.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Player? player = Player.m_localPlayer;
            if (player != _owner)
            {
                _owner = player;
                Kept.Clear();
                if (player) Load(player!);
            }
            if (!player || player!.IsDead()) return;

            Ended.Clear();
            foreach (KeyValuePair<WeatherKind, Held> pair in Kept)
                if (Time.time >= pair.Value.EndTime) Ended.Add(pair.Key);
            foreach (WeatherKind kind in Ended)
            {
                Kept.Remove(kind);
                _changed = true;
                PlayerMessages.TopLeft(string.Format(Messages.BlessingEnded, WeatherKinds.Colored(kind)));
            }

            WriteOnCharacter(player);
            if (_changed || Time.time >= _nextSave) Save(player);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // A blessing used up before its time (the Fortune's Boon Rite saving an item at the forge): it ends now.
    internal static void End(WeatherKind kind)
    {
        if (!Kept.Remove(kind)) return;
        _changed = true;
        PlayerMessages.TopLeft(string.Format(Messages.BlessingEnded, WeatherKinds.Colored(kind)));
        if (Player.m_localPlayer) WriteOnCharacter(Player.m_localPlayer);
    }

    // The local player died: every blessing ends.
    internal static void Died(Player player)
    {
        if (player != Player.m_localPlayer || Kept.Count == 0) return;
        Kept.Clear();
        _changed = true;
        PlayerMessages.TopLeft(Messages.BlessingsLostToDeath);
        WriteOnCharacter(player);
        Save(player);
    }

    private static void WriteOnCharacter(Player player)
    {
        if (!player.m_nview || !player.m_nview.IsValid() || !player.m_nview.IsOwner()) return;
        int mask = 0;
        foreach (KeyValuePair<WeatherKind, Held> pair in Kept)
            if (Time.time < pair.Value.EndTime) mask |= Bit(pair.Key);
        ZDO zdo = player.m_nview.GetZDO();
        if (zdo.GetInt(BlessedKey) != mask) zdo.Set(BlessedKey, mask);
        bool full = !player.GetInventory().HaveEmptySlot();
        if (zdo.GetBool(FullKey) != full) zdo.Set(FullKey, full);
    }

    // Saved with the character as "kind,remaining,duration,calledBy" lines.
    private static void Save(Player player)
    {
        _changed = false;
        _nextSave = Time.time + SaveInterval;
        if (Kept.Count == 0)
        {
            player.m_customData.Remove(SaveKey);
            return;
        }
        StringBuilder text = new();
        foreach (KeyValuePair<WeatherKind, Held> pair in Kept)
        {
            float remaining = pair.Value.EndTime - Time.time;
            if (remaining <= 0f) continue;
            string calledBy = pair.Value.CalledBy.Replace(",", " ").Replace("\n", " ");
            text.Append((int)pair.Key).Append(',')
                .Append(remaining.ToString("0.#", CultureInfo.InvariantCulture)).Append(',')
                .Append(pair.Value.Duration.ToString("0.#", CultureInfo.InvariantCulture)).Append(',')
                .Append(calledBy).Append('\n');
        }
        player.m_customData[SaveKey] = text.ToString();
    }

    private static void Load(Player player)
    {
        if (!player.m_customData.TryGetValue(SaveKey, out string text)) return;
        foreach (string line in text.Split('\n'))
        {
            string[] parts = line.Split([','], 4);
            if (parts.Length < 4 || !int.TryParse(parts[0], out int number)) continue;
            WeatherKind kind = (WeatherKind)number;
            if (!WeatherKinds.Personal(kind)
                || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float remaining)
                || !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float duration)
                || remaining <= 0f) continue;
            Kept[kind] = new Held { EndTime = Time.time + remaining, Duration = Mathf.Max(duration, remaining), CalledBy = parts[3] };
        }
    }
}

// The obliterator's own lightning, made here as a picture only: what would hurt or be sent to others is taken off.
internal static class BlessingLightning
{
    private const float Seconds = 6f;
    private static GameObject? _prefab;

    internal static void Strike(Vector3 position)
    {
        GameObject? prefab = Prefab();
        if (!prefab) return;
        // Made inside an inactive holder, so nothing on it wakes up before it is stripped.
        GameObject holder = new("TempestRite_Strike");
        holder.SetActive(false);
        try
        {
            GameObject strike = UnityEngine.Object.Instantiate(prefab!, position, Quaternion.identity, holder.transform);
            Strip<Aoe>(strike);
            Strip<TimedDestruction>(strike);
            Strip<ZSyncTransform>(strike);
            Strip<Rigidbody>(strike);
            Strip<ZNetView>(strike);
            strike.transform.SetParent(null, true);
            UnityEngine.Object.Destroy(strike, Seconds);
        }
        finally { UnityEngine.Object.Destroy(holder); }
    }

    private static void Strip<T>(GameObject strike) where T : Component
    {
        foreach (T component in strike.GetComponentsInChildren<T>(true)) UnityEngine.Object.DestroyImmediate(component);
    }

    private static GameObject? Prefab()
    {
        if (_prefab) return _prefab;
        if (!ZNetScene.instance) return null;
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            if (prefab && prefab.GetComponent<Incinerator>() is { } incinerator && incinerator.m_lightingAOEs)
            {
                _prefab = incinerator.m_lightingAOEs;
                break;
            }
        return _prefab;
    }
}
