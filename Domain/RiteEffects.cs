using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// What the Domain rites do, made visible in this game (only for players with the mod; nothing is sent): the game's own
// effects, recolored and shown for a moment.
// (Motes on what the Verdant, Vat-Boil and Pitch-Bellows Rites speed up are shown by RiteSpeedup.)
// - Packward: a creature shunning the area drips with fear (the game's "wet" drops).
// - Carrion: a creature walking to the obliterator trails a green smell (the game's poison cloud).
internal static class RiteEffects
{
    private const string Feature = "Rite effects";
    private const float Interval = 1.5f;
    // Effects only show within this far of the player.
    private const float ShowRange = 80f;
    // The same creature shows its effect again only after this long.
    private const float CreatureRepeat = 3f;
    private const float CreatureEffectSeconds = 2.5f;

    private const string FearEffect = "vfx_Wet";
    private const string SmellEffect = "vfx_Poison";

    private static readonly Color FearColor = new(0.75f, 0.85f, 1f, 1f);
    private static readonly Color SmellColor = new(0.6f, 0.85f, 0.2f, 0.8f);

    private static float _next;
    private static readonly Dictionary<ZDOID, float> LastShown = [];
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
        bool creatures = PluginConfig.ShowCreatureEffects.IsOn();
        bool packward = creatures && WeatherState.IsActive(WeatherKind.Packward);
        bool carrion = creatures && WeatherState.IsActive(WeatherKind.Carrion);
        if (!packward && !carrion) return;

        try { Show(player!.transform.position); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // While the Packward or Carrion Rite goes on, a line in the log now and then: what it reaches and what it steers, to
    // tell why a creature isn't.
    private const float ReportInterval = 10f;
    private static float _nextReport;

    internal static void Report()
    {
        WeatherKind kind = WeatherState.Active(EffectSlot.Creatures);
        if (kind == WeatherKind.None || Time.time < _nextReport || !ZNetScene.instance || !ZNet.instance) return;
        _nextReport = Time.time + ReportInterval;
        int inReach = 0, owned = 0, steered = 0;
        long me = ZDOMan.GetSessionID();
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!character || character.IsPlayer() || !character.GetBaseAI() || !character.m_nview || !character.m_nview.IsValid()) continue;
            if (!WeatherState.Reaches(kind, character.transform.position, CreatureRites.Buffer)) continue;
            inReach++;
            if (character.m_nview.GetZDO().GetOwner() == me) owned++;
            if (CreatureRites.ShownBy(character) != CreatureRites.Shown.None) steered++;
        }
        Vector3 at = WeatherState.Position(kind);
        string doing = kind == WeatherKind.Packward ? "shunning the area" : "walking to the obliterator";
        Plugin.ModLogger.LogDebug($"{WeatherKinds.Name(kind)} at ({at.x:0}, {at.z:0}): {inReach} creatures within reach, {owned} run by this game, {steered} {doing}.");
    }

    private static void Show(Vector3 here)
    {
        foreach (KeyValuePair<ZDO, ZNetView> instance in ZNetScene.instance.m_instances)
        {
            ZDO zdo = instance.Key;
            Vector3 position = zdo.GetPosition();
            if (Utils.DistanceXZ(position, here) > ShowRange) continue;
            ZNetView view = instance.Value;
            if (!view) continue;
            Character? character = view.GetComponent<Character>();
            if (!character) continue;
            CreatureRites.Shown shown = CreatureRites.ShownBy(character!);
            if (shown == CreatureRites.Shown.Scared) Creature(zdo, view, FearEffect, FearColor);
            else if (shown == CreatureRites.Shown.Drawn) Creature(zdo, view, SmellEffect, SmellColor);
        }

        // Creatures gone or long not seen are forgotten.
        Forget.Clear();
        foreach (KeyValuePair<ZDOID, float> shown in LastShown)
            if (Time.time - shown.Value > 60f) Forget.Add(shown.Key);
        foreach (ZDOID id in Forget) LastShown.Remove(id);
    }

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
