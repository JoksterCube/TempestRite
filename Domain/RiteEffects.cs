using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// What the Domain rites do, made visible in this game (only for players with the mod; nothing is sent): the game's own
// effects, recolored and shown for a moment.
// - Verdant: now and then a green sparkle (the game's "picked" effect) on a plant or bush that grows faster.
// - Packward: a fleeing creature drips with fear (the game's "wet" drops).
// - Carrion: an animal walking to the obliterator trails a green smell (the game's poison cloud).
internal static class RiteEffects
{
    private const string Feature = "Rite effects";
    private const float Interval = 1.5f;
    // Effects only show within this far of the player.
    private const float ShowRange = 80f;
    private const int SparklesPerTick = 3;
    // The same creature shows its effect again only after this long.
    private const float CreatureRepeat = 3f;
    private const float CreatureEffectSeconds = 2.5f;
    private const float SparkleSeconds = 3f;

    private const string SparkleEffect = "vfx_pickable_pick";
    private const string FearEffect = "vfx_Wet";
    private const string SmellEffect = "vfx_Poison";

    private static readonly Color SparkleColor = new(0.45f, 1f, 0.4f, 1f);
    private static readonly Color FearColor = new(0.75f, 0.85f, 1f, 1f);
    private static readonly Color SmellColor = new(0.6f, 0.85f, 0.2f, 0.8f);
    private static readonly int FleeUntil = "TempestRite_FleeUntil".GetStableHashCode();

    private static float _next;
    private static readonly Dictionary<ZDOID, float> LastShown = [];
    private static readonly List<ZDO> Growing = [];
    private static readonly List<ZDOID> Forget = [];

    internal static void ClearSession()
    {
        _next = 0f;
        LastShown.Clear();
    }

    // Effects are mostly not in the game's network prefab list: they are found once among all loaded assets.
    private static readonly Dictionary<string, GameObject?> Prefabs = [];

    private static GameObject? Prefab(string name)
    {
        if (Prefabs.TryGetValue(name, out GameObject? found) && (found == null || found)) return found;
        found = ZNetScene.instance ? ZNetScene.instance.GetPrefab(name) : null;
        if (!found)
            foreach (GameObject asset in Resources.FindObjectsOfTypeAll<GameObject>())
                if (asset.name == name && !asset.scene.IsValid() && !asset.transform.parent)
                {
                    found = asset;
                    break;
                }
        if (!found) Plugin.ModLogger.LogWarning($"Effect {name} not found; it won't show.");
        Prefabs[name] = found ? found : null;
        return Prefabs[name];
    }

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { Report(); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        if (Time.time < _next) return;
        _next = Time.time + Interval;
        Player? player = Player.m_localPlayer;
        if (!player || !ZNetScene.instance || !ZNet.instance) return;
        bool verdant = WeatherState.IsActive(WeatherKind.Verdant) && PluginConfig.ShowVerdantSparkles.IsOn();
        bool creatures = PluginConfig.ShowCreatureEffects.IsOn();
        bool packward = creatures && WeatherState.IsActive(WeatherKind.Packward);
        bool carrion = creatures && WeatherState.IsActive(WeatherKind.Carrion);
        if (!verdant && !packward && !carrion) return;

        try { Show(player!.transform.position, verdant, packward, carrion); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // While the Packward Rite goes on, a line in the log now and then: what it reaches and what flees, to tell why a
    // creature doesn't.
    private const float ReportInterval = 10f;
    private static float _nextReport;

    internal static void Report()
    {
        if (!WeatherState.IsActive(WeatherKind.Packward) || Time.time < _nextReport || !ZNetScene.instance || !ZNet.instance) return;
        _nextReport = Time.time + ReportInterval;
        int inReach = 0, owned = 0, fleeing = 0;
        long me = ZDOMan.GetSessionID();
        long now = ZNet.instance.GetTime().Ticks;
        foreach (KeyValuePair<ZDO, ZNetView> instance in ZNetScene.instance.m_instances)
        {
            ZNetView view = instance.Value;
            if (!view || !view.GetComponent<MonsterAI>()) continue;
            Character? character = view.GetComponent<Character>();
            if (!character || character!.IsBoss() || character.IsTamed()) continue;
            if (!WeatherState.Reaches(WeatherKind.Packward, character.transform.position)) continue;
            inReach++;
            if (instance.Key.GetOwner() == me) owned++;
            if (instance.Key.GetLong(FleeUntil, 0L) > now) fleeing++;
        }
        Vector3 at = WeatherState.Position(WeatherKind.Packward);
        Plugin.ModLogger.LogDebug($"Packward Rite at ({at.x:0}, {at.z:0}): {inReach} creatures within reach, {owned} run by this game, {fleeing} fleeing.");
    }

    private static void Show(Vector3 here, bool verdant, bool packward, bool carrion)
    {
        Growing.Clear();
        long now = ZNet.instance.GetTime().Ticks;
        HashSet<int> lured = PluginConfig.CarrionPrefabs();
        foreach (KeyValuePair<ZDO, ZNetView> instance in ZNetScene.instance.m_instances)
        {
            ZDO zdo = instance.Key;
            Vector3 position = zdo.GetPosition();
            if (Utils.DistanceXZ(position, here) > ShowRange) continue;
            int prefab = zdo.GetPrefab();

            if (verdant && VerdantGrowth.IsGrowing(prefab))
            {
                if (WeatherState.Reaches(WeatherKind.Verdant, position) && StillGrowing(zdo)) Growing.Add(zdo);
                continue;
            }

            ZNetView view = instance.Value;
            if (!view) continue;
            if (packward && zdo.GetLong(FleeUntil, 0L) > now) Creature(zdo, view, FearEffect, FearColor);
            else if (carrion && lured.Contains(prefab) && Lured(view, position)) Creature(zdo, view, SmellEffect, SmellColor);
        }

        for (int i = 0; i < SparklesPerTick && Growing.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, Growing.Count);
            Spawn(SparkleEffect, Growing[index].GetPosition(), null, SparkleColor, SparkleSeconds);
            Growing.RemoveAt(index);
        }

        // Creatures gone or long not seen are forgotten.
        Forget.Clear();
        foreach (KeyValuePair<ZDOID, float> shown in LastShown)
            if (Time.time - shown.Value > 60f) Forget.Add(shown.Key);
        foreach (ZDOID id in Forget) LastShown.Remove(id);
    }

    // Planted and not grown yet, or a picked bush waiting to regrow.
    private static bool StillGrowing(ZDO zdo) =>
        zdo.GetLong(ZDOVars.s_plantTime, 0L) > 0L || zdo.GetBool(ZDOVars.s_picked);

    // On its way to the obliterator: within reach, not already there, not fighting or scared.
    private static bool Lured(ZNetView view, Vector3 position)
    {
        if (!WeatherState.Reaches(WeatherKind.Carrion, position)) return false;
        if (Utils.DistanceXZ(position, WeatherState.Position(WeatherKind.Carrion)) <= 10f) return false;
        BaseAI? ai = view.GetComponent<BaseAI>();
        Character? character = view.GetComponent<Character>();
        return ai && !ai!.IsAlerted() && character && !character!.IsTamed();
    }

    // The game's sparkle, in a color, for a moment (Vat-Boil bubbles).
    internal static void Sparkle(Vector3 position, Color color, float seconds) => Spawn(SparkleEffect, position, null, color, seconds);

    private static void Creature(ZDO zdo, ZNetView view, string effect, Color color)
    {
        if (LastShown.TryGetValue(zdo.m_uid, out float last) && Time.time - last < CreatureRepeat) return;
        LastShown[zdo.m_uid] = Time.time;
        Character? character = view.GetComponent<Character>();
        Transform anchor = character && character!.m_head ? character.m_head : view.transform;
        Spawn(effect, anchor.position, anchor, color, CreatureEffectSeconds);
    }

    // A local copy of the game's effect, recolored, removed after a while. Effects that would be sent to others
    // (with a network view) are never made.
    private static void Spawn(string name, Vector3 position, Transform? parent, Color color, float seconds)
    {
        GameObject? prefab = Prefab(name);
        if (!prefab || prefab!.GetComponent<ZNetView>()) return;
        GameObject effect = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity, parent);
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            main.startColor = color;
        }
        UnityEngine.Object.Destroy(effect, seconds);
    }
}
