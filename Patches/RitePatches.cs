using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// The Tailwind Rite, a blessing: the game asks the ship whether someone aboard controls the wind (Moder's power); while
// a player aboard carries the blessing, the answer is yes, so the wind blows from behind for the whole ship. Its
// strength stays the weather's.
[HarmonyPatch(typeof(Ship), nameof(Ship.IsWindControllActive))]
internal static class TailwindPatch
{
    private const string Feature = "Tailwind Rite";

    private static void Postfix(Ship __instance, ref bool __result)
    {
        if (__result || PatchGuard.IsDisabled(Feature)) return;
        try
        {
            foreach (Player player in __instance.m_players)
                if (Blessings.Has(player, WeatherKind.Tailwind))
                {
                    __result = true;
                    return;
                }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// The Gale-Breath Rite: the wind blows at full strength. Each game works out its own wind from the weather (and turns it
// behind a ship with Moder's power or the Tailwind Rite); only the strength it aims for is raised, so the direction
// still changes as usual and the game eases into the new strength the way it eases into any other.
[HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetTargetWind))]
internal static class GaleBreathPatch
{
    private const string Feature = "Gale-Breath Rite";

    private static void Prefix(ref float intensity)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            if (WeatherState.Active(EffectSlot.Wind) == WeatherKind.GaleBreath) intensity = 1f;
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}


// The Packward and Carrion Rites steer creatures (not bosses, not tamed, not on the rite's exclusion list), in the game
// that runs their AI (their owner). The game's own AI keeps running; the rites step in only where they must.
// - Packward: a creature inside the area finds its way out, and goes a little beyond before it turns around. One whose
//   target is inside waits at the edge instead of following. Wandering never picks a point inside. One hurt not long
//   ago fights as usual, inside or out.
// - Carrion: a creature within reach that would wander about walks to the obliterator instead and mills about there.
//   Its senses are dulled while it has no target and isn't alerted, so it notices a player only from closer; once it
//   does, it reacts as usual with its full senses, and walks on once it calms down.
internal static class CreaturePatches
{
    private const string Feature = "Packward and Carrion Rites";
    // A drawn creature that has no path to the obliterator wanders as usual for this long before it tries again.
    private const float NoPathWait = 10f;
    private static readonly System.Collections.Generic.Dictionary<int, float> NoPathUntil = [];

    internal static void ClearSession()
    {
        NoPathUntil.Clear();
        Provokes.Clear();
    }

    // The game's own BaseAI.UpdateAI (timers, regeneration, take-off and landing), for when a rite takes over the rest.
    [HarmonyPatch]
    private static class BaseUpdate
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.UpdateAI))]
        internal static bool Run(BaseAI instance, float dt) => throw new NotImplementedException("Filled in by Harmony.");
    }

    // What the rites did to a creature for one update: its usual senses while a Carrion Rite dulls them, or, under the
    // Packward Rite, that it runs out after its own AI looked for a target and found none.
    private struct Senses
    {
        internal bool Dulled;
        internal float View;
        internal float Hear;
        internal bool FleeAfter;
        internal float Dt;
    }

    private enum Steer
    {
        // Its own AI runs.
        None,
        // The rite moved it instead.
        Took,
        // Its own AI runs (so it can notice someone); with no target after, it runs out.
        FleeAfter
    }

    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    private static class Monsters
    {
        private static bool Prefix(MonsterAI __instance, float dt, ref bool __result, out Senses __state)
        {
            __state = default;
            if (PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                Steer steer = __instance.IsSleeping() ? Steer.None : Shun(__instance, dt, __instance.m_targetCreature, __instance.m_targetStatic);
                if (steer == Steer.Took)
                {
                    __result = true;
                    return false;
                }
                __state.FleeAfter = steer == Steer.FleeAfter;
                __state.Dt = dt;
                Dull(__instance, ref __state);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
            return true;
        }

        private static void Postfix(MonsterAI __instance, Senses __state) => FleeIfNoTarget(__instance, __state);

        private static Exception? Finalizer(MonsterAI __instance, Senses __state, Exception? __exception)
        {
            Restore(__instance, __state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(AnimalAI), nameof(AnimalAI.UpdateAI))]
    private static class Animals
    {
        private static bool Prefix(AnimalAI __instance, float dt, ref bool __result, out Senses __state)
        {
            __state = default;
            if (PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                Steer steer = Shun(__instance, dt, null, null);
                if (steer == Steer.Took)
                {
                    __result = true;
                    return false;
                }
                __state.FleeAfter = steer == Steer.FleeAfter;
                __state.Dt = dt;
                Dull(__instance, ref __state);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
            return true;
        }

        private static void Postfix(AnimalAI __instance, Senses __state) => FleeIfNoTarget(__instance, __state);

        private static Exception? Finalizer(AnimalAI __instance, Senses __state, Exception? __exception)
        {
            Restore(__instance, __state);
            return __exception;
        }
    }

    // Under the Packward Rite, a creature hurt within reach says so on itself, so its pack nearby fights along.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.OnDamaged))]
    private static class Hurt
    {
        private static void Postfix(BaseAI __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (!WeatherState.IsActive(WeatherKind.Packward) || !Owned(__instance) || !ZNet.instance) return;
                Character character = __instance.m_character;
                if (!WeatherState.Reaches(WeatherKind.Packward, character.transform.position, CreatureRites.Buffer)) return;
                if (!CreatureRites.Steered(character, WeatherKind.Packward)) return;
                __instance.m_nview.GetZDO().Set(CreatureRites.HurtKey, ZNet.instance.GetTime().Ticks);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    private static void FleeIfNoTarget(BaseAI ai, Senses state)
    {
        if (!state.FleeAfter || PatchGuard.IsDisabled(Feature)) return;
        try
        {
            if (!ai || ai.HaveTarget() || !WeatherState.IsActive(WeatherKind.Packward)) return;
            GoOut(ai, state.Dt, WeatherState.Position(WeatherKind.Packward), PluginConfig.Radius(WeatherKind.Packward) + CreatureRites.Buffer);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Wandering about: under the Carrion Rite, to the obliterator; under the Packward Rite, not around a spawn point
    // inside the area.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IdleMovement))]
    private static class Idle
    {
        private static bool Prefix(BaseAI __instance, float dt)
        {
            if (PatchGuard.IsDisabled(Feature)) return true;
            try { return !Wander(__instance, dt); }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    // Under the Packward Rite, no wandering into the area.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsValidRandomMovePoint))]
    private static class WanderPoint
    {
        private static void Postfix(BaseAI __instance, Vector3 point, ref bool __result)
        {
            if (!__result || PatchGuard.IsDisabled(Feature) || !WeatherState.IsActive(WeatherKind.Packward)) return;
            try
            {
                if (WeatherState.Reaches(WeatherKind.Packward, point, CreatureRites.Buffer / 2f)
                    && CreatureRites.Steered(__instance.m_character, WeatherKind.Packward))
                    __result = false;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    private static bool Owned(BaseAI ai) => ai.m_nview && ai.m_nview.IsValid() && ai.m_nview.IsOwner();

    // Packward: whether the rite moves the creature this update (instead of its own AI, or after it).
    private static Steer Shun(BaseAI ai, float dt, Character? target, StaticTarget? targetStatic)
    {
        if (!WeatherState.IsActive(WeatherKind.Packward) || !Owned(ai)) return Steer.None;
        Character character = ai.m_character;
        ZDO zdo = ai.m_nview.GetZDO();
        if (!CreatureRites.Steered(character, WeatherKind.Packward))
        {
            CreatureRites.SetMood(zdo, CreatureRites.Mood.None);
            return Steer.None;
        }

        Vector3 center = WeatherState.Position(WeatherKind.Packward);
        float radius = PluginConfig.Radius(WeatherKind.Packward);
        Vector3 position = character.transform.position;
        // Within a Crimson or Ragnarök Rite's bubble, or while a raid reaches the area, creatures don't shun it.
        if (WeatherState.Suspended(WeatherKind.Packward, position))
        {
            CreatureRites.SetMood(zdo, CreatureRites.Mood.None);
            return Steer.None;
        }
        float distance = Utils.DistanceXZ(position, center);
        CreatureRites.Mood was = CreatureRites.MoodOf(zdo);
        CreatureRites.Mood mood = CreatureRites.Mood.None;
        // Hurt not long ago: it fights. One of its pack hurt nearby: it fights whoever it notices, else it runs.
        bool hurt = ai.m_timeSinceHurt < PluginConfig.PackwardFightSeconds.Value;
        bool provoked = !hurt && distance <= radius + CreatureRites.Buffer && Provoked(ai);
        if (!hurt && !(provoked && ai.HaveTarget()))
        {
            if (distance <= radius || (was == CreatureRites.Mood.Fleeing && distance <= radius + CreatureRites.Buffer))
                mood = CreatureRites.Mood.Fleeing;
            else if (distance <= radius + CreatureRites.Buffer && TargetInside(target, targetStatic, center, radius, out _))
                mood = CreatureRites.Mood.HoldingBack;
        }
        if (mood != was)
        {
            CreatureRites.SetMood(zdo, mood);
            if (mood != CreatureRites.Mood.None) Plugin.ModLogger.LogDebug($"Packward Rite: {character.m_name} {(mood == CreatureRites.Mood.Fleeing ? "leaves the area" : "holds back")}.");
        }
        if (mood == CreatureRites.Mood.None) return Steer.None;
        // Provoked: its own AI looks around first.
        if (provoked) return mood == CreatureRites.Mood.Fleeing ? Steer.FleeAfter : Steer.None;

        if (!BaseUpdate.Run(ai, dt)) return Steer.None;
        if (mood == CreatureRites.Mood.Fleeing) GoOut(ai, dt, center, radius + CreatureRites.Buffer);
        else
        {
            ai.StopMoving();
            if (TargetInside(target, targetStatic, center, radius, out Vector3 at)) ai.LookAt(at);
        }
        return Steer.Took;
    }

    // How often a creature looks whether one of its pack nearby was hurt.
    private const float ProvokeCheck = 1f;
    private static readonly System.Collections.Generic.Dictionary<int, (float Next, bool Provoked)> Provokes = [];

    // Whether a creature of its faction within its sight was hurt under the Packward Rite lately. It is alerted when it
    // first hears of it, and still has to notice someone with its usual senses.
    private static bool Provoked(BaseAI ai)
    {
        int id = ai.GetInstanceID();
        float now = Time.time;
        if (Provokes.TryGetValue(id, out var known) && now < known.Next) return known.Provoked;

        bool provoked = false;
        Character self = ai.m_character;
        Vector3 position = self.transform.position;
        float range = Mathf.Max(ai.m_viewRange, 10f);
        foreach (BaseAI other in BaseAI.BaseAIInstances)
        {
            if (!other || other == ai || !other.m_nview || !other.m_nview.IsValid()) continue;
            Character character = other.m_character;
            if (!character || character.GetFaction() != self.GetFaction()) continue;
            if (Vector3.Distance(position, character.transform.position) > range) continue;
            if (!CreatureRites.HurtLately(other.m_nview.GetZDO())) continue;
            provoked = true;
            break;
        }
        if (provoked && !known.Provoked) ai.Alert();
        Provokes[id] = (now + ProvokeCheck, provoked);
        if (Provokes.Count > 500) Provokes.Clear();
        return provoked;
    }

    private static bool TargetInside(Character? target, StaticTarget? targetStatic, Vector3 center, float radius, out Vector3 at)
    {
        at = target ? target!.transform.position : targetStatic ? targetStatic!.GetCenter() : Vector3.zero;
        return (target || targetStatic) && Utils.DistanceXZ(at, center) <= radius;
    }

    // Runs outward, to beyond exitRadius: a new point every flee interval, the first with a path, like the game's own
    // fleeing.
    private static void GoOut(BaseAI ai, float dt, Vector3 center, float exitRadius)
    {
        Vector3 position = ai.transform.position;
        float now = Time.time;
        if (now - ai.m_fleeTargetUpdateTime > ai.m_fleeInterval)
        {
            ai.m_fleeTargetUpdateTime = now;
            ai.m_lastFlee = now;
            Vector3 away = position - center;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f) * Vector3.forward;
            away.Normalize();
            float length = Mathf.Clamp(exitRadius - Utils.DistanceXZ(position, center) + 4f, 8f, 30f);
            Vector3 target = position + away * length;
            for (int i = 0; i < 10; i++)
            {
                Vector3 candidate = position + Quaternion.Euler(0f, i == 0 ? 0f : UnityEngine.Random.Range(-70f, 70f), 0f) * away * length;
                if (!ai.HavePath(candidate)) continue;
                if (ai.m_avoidWater && !ai.m_character.IsSwimming() && ZoneSystem.instance.GetSolidHeight(candidate) < 30f) continue;
                if (ai.m_avoidLavaFlee && ZoneSystem.instance.IsLava(candidate)) continue;
                target = candidate;
                break;
            }
            ai.m_fleeTarget = target;
        }
        ai.MoveTo(dt, ai.m_fleeTarget, 1f, true);
    }

    // Carrion: senses dulled while drawn (within reach, no target, not alerted).
    private static void Dull(BaseAI ai, ref Senses senses)
    {
        if (!WeatherState.IsActive(WeatherKind.Carrion) || !Owned(ai) || ai.IsAlerted() || ai.HaveTarget()) return;
        Character character = ai.m_character;
        if (!WeatherState.Reaches(WeatherKind.Carrion, character.transform.position)) return;
        if (!CreatureRites.Steered(character, WeatherKind.Carrion)) return;
        float share = PluginConfig.CarrionSenses.Value;
        senses = new Senses { Dulled = true, View = ai.m_viewRange, Hear = ai.m_hearRange };
        ai.m_viewRange *= share;
        // Some creatures hear from anywhere (9999 m): kept that way.
        if (ai.m_hearRange < 1000f) ai.m_hearRange *= share;
    }

    private static void Restore(BaseAI ai, Senses senses)
    {
        if (!senses.Dulled || !ai) return;
        ai.m_viewRange = senses.View;
        ai.m_hearRange = senses.Hear;
    }

    // True when a rite chose where the creature wanders this update.
    private static bool Wander(BaseAI ai, float dt)
    {
        Character character = ai.m_character;
        if (!character || !Owned(ai)) return false;
        Vector3 position = character.transform.position;

        if (WeatherState.IsActive(WeatherKind.Packward))
        {
            // Its home is inside: it wanders where it is instead (points inside are refused, see WanderPoint).
            if (!WeatherState.Reaches(WeatherKind.Packward, ai.m_spawnPoint, CreatureRites.Buffer)) return false;
            if (!CreatureRites.Steered(character, WeatherKind.Packward)) return false;
            ai.RandomMovement(dt, position, true);
            return true;
        }

        if (!WeatherState.IsActive(WeatherKind.Carrion) || !WeatherState.Reaches(WeatherKind.Carrion, position)) return false;
        if (!CreatureRites.Steered(character, WeatherKind.Carrion)) return false;
        int id = ai.GetInstanceID();
        if (NoPathUntil.TryGetValue(id, out float until))
        {
            if (Time.time < until) return false;
            NoPathUntil.Remove(id);
        }
        Vector3 altar = WeatherState.Position(WeatherKind.Carrion);
        if (Utils.DistanceXZ(position, altar) <= CreatureRites.Arrived)
        {
            ai.RandomMovementArroundPoint(dt, altar, CreatureRites.Arrived * 0.6f, false);
            return true;
        }
        // Stopped while still far: no way there from here.
        if (ai.MoveTo(dt, altar, CreatureRites.Arrived * 0.8f, false) && Utils.DistanceXZ(position, altar) > CreatureRites.Arrived)
        {
            NoPathUntil[id] = Time.time + NoPathWait;
            if (NoPathUntil.Count > 500) NoPathUntil.Clear();
        }
        return true;
    }
}

// The Packward and Carrion Rites' icons over the creatures they steer (see CreatureIcons), where the game shows its
// own. The game shows a creature's hud only within 10 m and for a minute after it was aimed at; a creature the rites
// steer shows its hud (with the icon) within ShowRange, aimed at or not.
[HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.UpdateHuds))]
internal static class CreatureIconPatch
{
    private const string Feature = "Creature icons";
    private const float ShowRange = 30f;

    private static bool On() =>
        PluginConfig.ShowCreatureIcons.IsOn() && (WeatherState.IsActive(WeatherKind.Packward) || WeatherState.IsActive(WeatherKind.Carrion));

    [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.TestShow))]
    private static class Near
    {
        private static void Postfix(EnemyHud __instance, Character c, ref bool __result)
        {
            if (__result || PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (!c || c.IsPlayer() || c.IsBoss() || !On()) return;
                if ((c.transform.position - __instance.m_refPoint).sqrMagnitude > ShowRange * ShowRange) return;
                if (CreatureRites.ShownBy(c) != CreatureRites.Shown.None) __result = true;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // As if just aimed at, so the game shows the hud.
    private static void Prefix(EnemyHud __instance)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            if (!On()) return;
            foreach (EnemyHud.HudData hud in __instance.m_huds.Values)
                if (hud.m_character && hud.m_aware && CreatureRites.ShownBy(hud.m_character) != CreatureRites.Shown.None)
                    hud.m_hoverTimer = 0f;
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Postfix(EnemyHud __instance)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            bool on = On();
            foreach (EnemyHud.HudData hud in __instance.m_huds.Values)
            {
                if (!hud.m_gui || !hud.m_character || !hud.m_aware) continue;
                Transform parent = hud.m_aware.parent;
                Transform? drawn = parent.Find(CreatureIcons.DrawnName);
                Transform? scared = parent.Find(CreatureIcons.ScaredName);
                CreatureRites.Shown shown = on && hud.m_gui.activeSelf ? CreatureRites.ShownBy(hud.m_character) : CreatureRites.Shown.None;
                if (shown == CreatureRites.Shown.None && !drawn && !scared) continue;

                if (shown == CreatureRites.Shown.Drawn && !drawn)
                    drawn = CreatureIcons.Make(hud.m_aware, CreatureIcons.DrawnName, CreatureIcons.Drawn(), CreatureIcons.DrawnColor).transform;
                if (shown == CreatureRites.Shown.Scared && !scared)
                    scared = CreatureIcons.Make(hud.m_aware, CreatureIcons.ScaredName, CreatureIcons.Scared(), CreatureIcons.ScaredColor).transform;
                if (drawn) Show(drawn!, hud.m_aware, shown == CreatureRites.Shown.Drawn);
                if (scared) Show(scared!, hud.m_aware, shown == CreatureRites.Shown.Scared);
                // Scared wins over the game's own icons; a drawn creature has none (no target, not alerted).
                if (shown == CreatureRites.Shown.Scared)
                {
                    hud.m_aware.gameObject.SetActive(false);
                    if (hud.m_alerted) hud.m_alerted.gameObject.SetActive(false);
                }
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Where the game's "aware" icon is now (other mods move it, like nameplates that make room above the name), a
    // little smaller.
    private static void Show(Transform icon, RectTransform aware, bool visible)
    {
        if (icon.gameObject.activeSelf != visible) icon.gameObject.SetActive(visible);
        if (!visible) return;
        if (icon.localPosition != aware.localPosition) icon.localPosition = aware.localPosition;
        Vector3 scale = aware.localScale * CreatureIcons.Scale;
        if (icon.localScale != scale) icon.localScale = scale;
    }
}

// The Verdant Rite: a plant it speeds up says so when looked at.
[HarmonyPatch(typeof(Plant), nameof(Plant.GetHoverText))]
internal static class VerdantHoverPatch
{
    private const string Feature = "Verdant plant hint";

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(Plant __instance, ref string __result)
    {
        if (PatchGuard.IsDisabled(Feature) || string.IsNullOrEmpty(__result)) return;
        try
        {
            if (!VerdantGrowth.Speeds(__instance)) return;
            __result += "\n" + WeatherKinds.Colored(WeatherKind.Verdant) + ObliteratorHints.Explain(WeatherKind.Verdant);
            double ready = AzuHoverStatsPatches.Installed ? -1.0 : VerdantGrowth.ReadyIn(__instance);
            if (ready >= 0.0)
                __result += ready < 60.0 ? Constants.Messages.VerdantReadySoon : string.Format(Constants.Messages.VerdantReadyIn, Math.Ceiling(ready / 60.0));
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// A picked bush the Verdant Rite speeds up: the game shows nothing when looking at it; now its name and the rite.
[HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
internal static class VerdantBushHoverPatch
{
    private const string Feature = "Verdant bush hint";

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(Pickable __instance, ref string __result)
    {
        if (PatchGuard.IsDisabled(Feature) || !string.IsNullOrEmpty(__result) || !__instance.m_picked) return;
        try
        {
            double ready = VerdantGrowth.RegrowsIn(__instance);
            if (ready < 0.0) return;
            __result = Localization.instance.Localize(__instance.GetHoverName()) + "\n" + WeatherKinds.Colored(WeatherKind.Verdant) +
                       ObliteratorHints.Explain(WeatherKind.Verdant);
            if (!AzuHoverStatsPatches.Installed)
                __result += ready < 60.0 ? Constants.Messages.VerdantReadySoon : string.Format(Constants.Messages.VerdantReadyIn, Math.Ceiling(ready / 60.0));
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// AzuHoverStats (when installed) shows how long plants, picked bushes, fermenters and cooking food take, at the usual
// speed, and when food burns. Under the Verdant and Vat-Boil Rites, its times count the rite instead (at the rite's
// speed while it lasts, the usual speed after), and food under the Vat-Boil Rite won't burn.
internal static class AzuHoverStatsPatches
{
    private const string Feature = "AzuHoverStats rite times";
    private const string Utilities = "AzuHoverStats.Utilities";

    internal static bool Installed => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Azumatt.AzuHoverStats");

    [HarmonyPatch]
    private static class PlantTime
    {
        private static bool Prepare() => Installed && AccessTools.Method(Utilities + ":GetPlantString") != null;
        private static System.Reflection.MethodBase TargetMethod() => AccessTools.Method(Utilities + ":GetPlantString");

        private static void Postfix(Plant plantComp, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature) || string.IsNullOrEmpty(__result)) return;
            try
            {
                if (plantComp && VerdantGrowth.Speeds(plantComp)) Replace(VerdantGrowth.ReadyIn(plantComp), ref __result);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch]
    private static class BushTime
    {
        private static bool Prepare() => Installed && AccessTools.Method(Utilities + ":GetPickableString") != null;
        private static System.Reflection.MethodBase TargetMethod() => AccessTools.Method(Utilities + ":GetPickableString");

        private static void Postfix(Pickable pickableComp, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature) || string.IsNullOrEmpty(__result)) return;
            try
            {
                if (pickableComp) Replace(VerdantGrowth.RegrowsIn(pickableComp), ref __result);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch]
    private static class FermenterTime
    {
        private static bool Prepare() => Installed && AccessTools.Method(Utilities + ":GetFermenterString") != null;
        private static System.Reflection.MethodBase TargetMethod() => AccessTools.Method(Utilities + ":GetFermenterString");

        private static void Postfix(Fermenter fermenter, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature) || string.IsNullOrEmpty(__result)) return;
            try
            {
                if (fermenter && RiteSpeedup.Speeds(WeatherKind.VatBoil, fermenter))
                    Replace(StationWork.FermentedIn(fermenter), ref __result);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Under the Vat-Boil Rite each slot says when it is done at the rite's speed, and that it won't burn.
    [HarmonyPatch]
    private static class CookingTimes
    {
        private static bool Prepare() => Installed && AccessTools.Method(Utilities + ":GetCookingStationString") != null;
        private static System.Reflection.MethodBase TargetMethod() => AccessTools.Method(Utilities + ":GetCookingStationString");

        private static void Postfix(CookingStation cookingStationComp, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature) || string.IsNullOrEmpty(__result)) return;
            try
            {
                CookingStation station = cookingStationComp;
                if (!station || !RiteSpeedup.Speeds(WeatherKind.VatBoil, station)) return;
                System.Text.StringBuilder text = new();
                for (int i = 0; i < station.m_slots.Length; i++)
                {
                    station.GetSlot(i, out string item, out float cooked, out _, out _);
                    if (string.IsNullOrEmpty(item) || station.GetItemConversion(item) is not { } conversion || !conversion.m_to) continue;
                    text.Append(Localization.instance.Localize(conversion.m_to.GetHoverName()));
                    if (cooked < conversion.m_cookTime)
                        text.Append(" (" + Format(RiteSpeedup.ReadyIn(WeatherKind.VatBoil, PluginConfig.VatBoilSpeedMultiplier.Value,
                            conversion.m_cookTime - cooked)) + ")");
                    text.Append("  " + Constants.Messages.WontBurn + "\n");
                }
                __result = text.ToString();
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    private static void Replace(double seconds, ref string result)
    {
        if (seconds >= 0.0) result = "(" + Format(seconds) + ")";
    }

    // In AzuHoverStats' own format: "01h 02m 03s", "02m 03s" or "03s".
    private static string Format(double seconds)
    {
        TimeSpan left = TimeSpan.FromSeconds(Math.Ceiling(Math.Max(0.0, seconds)));
        return left.TotalHours >= 1.0 ? $"{(int)left.TotalHours:D2}h {left.Minutes:D2}m {left.Seconds:D2}s"
            : left.Minutes > 0 ? $"{left.Minutes:D2}m {left.Seconds:D2}s"
            : $"{left.Seconds:D2}s";
    }
}

// The Vat-Boil and Pitch-Bellows Rites: a station they speed up says so when looked at (the fermenter, the cooking
// station's food and fuel switches, the smelter's ore and fuel switches).
internal static class StationHoverPatches
{
    private const string Feature = "Rite station hint";

    private static void Add(WeatherKind kind, Component station, ref string result)
    {
        if (PatchGuard.IsDisabled(Feature) || string.IsNullOrEmpty(result)) return;
        try
        {
            if (station && RiteSpeedup.Speeds(kind, station))
                result += "\n" + WeatherKinds.Colored(kind) + ObliteratorHints.Explain(kind);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.GetHoverText))]
    private static class FermenterHover
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Fermenter __instance, ref string __result)
        {
            string before = __result;
            Add(WeatherKind.VatBoil, __instance, ref __result);
            if (__result == before || AzuHoverStatsPatches.Installed) return;
            try
            {
                double ready = StationWork.FermentedIn(__instance);
                if (ready >= 0.0)
                    __result += ready < 60.0 ? Constants.Messages.StationReadySoon : string.Format(Constants.Messages.StationReadyIn, Math.Ceiling(ready / 60.0));
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.HoverText))]
    private static class CookingHover
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(CookingStation __instance, ref string __result) => Add(WeatherKind.VatBoil, __instance, ref __result);
    }

    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnHoverFuelSwitch))]
    private static class CookingFuelHover
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(CookingStation __instance, ref string __result) => Add(WeatherKind.VatBoil, __instance, ref __result);
    }

    [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnHoverAddOre))]
    private static class SmelterOreHover
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Smelter __instance, ref string __result) => Add(Kind(__instance), __instance, ref __result);
    }

    [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnHoverAddFuel))]
    private static class SmelterFuelHover
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Smelter __instance, ref string __result) => Add(Kind(__instance), __instance, ref __result);
    }

    // Only the smelters the rite speeds up (not the windmill or spinning wheel).
    private static WeatherKind Kind(Smelter smelter) =>
        smelter && smelter.m_nview && smelter.m_nview.IsValid() ? RiteSpeedup.KindOf(smelter.m_nview.GetZDO().GetPrefab()) : WeatherKind.None;
}
