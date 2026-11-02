using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// The Fair-Wind Rite, a blessing: while a player aboard the local player's ship carries it, the wind never blows
// against the ship. A wind coming from within Arc of the bow turns to come from Arc off the bow instead, on the side it
// was on; the game's sail stops pulling only from about 41 degrees in, so the sail still catches it. Any other wind, and
// its strength, stay the weather's. Each game works out its own wind, so this runs where the ship is sailed.
// The game eases into a new wind over 5 seconds and takes no other until then, so steering the wind through it lags and
// lurches. Instead, the weather's wind is left as it is, and the wind the game just worked out is turned right after,
// every update. It eases toward where it should be, mostly there within about 1.5 seconds, so it gives way as the bow
// turns into it instead of sticking to it, and eases back when no longer needed. Swinging from one side of the bow to the
// other, it goes round behind the ship, and while easing it never comes closer than Floor to the bow, so the sail keeps
// pulling.
[HarmonyPatch(typeof(EnvMan), nameof(EnvMan.UpdateWindTransition))]
internal static class FairWindPatch
{
    private const string Feature = "Fair-Wind Rite";
    private const float Arc = 45f;
    // Seconds for the wind to ease about two thirds of the way to where it should be.
    private const float Ease = 0.5f;
    // Closest to the bow the eased wind comes: just outside where the game's sail stops pulling.
    private const float Floor = 42f;
    // Wind from nearly straight ahead keeps the side it was turned to last, so it doesn't flip across the bow.
    private const float KeepSide = 15f;
    private static float side = 1f;
    // The wind the rite shows the game, while it differs from the weather's; null when they are the same.
    private static Vector3? shown;

    internal static void ClearSession() => shown = null;

    private static void Postfix(EnvMan __instance, float dt)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Vector3 weather = new(__instance.m_wind.x, 0f, __instance.m_wind.z);
            if (weather.sqrMagnitude < 0.0001f) return;
            weather.Normalize();
            Vector3 wanted = Wanted(__instance, weather, out Vector3 forward);
            if (shown == null)
            {
                if (wanted == weather) return;
                shown = weather;
            }
            float share = 1f - Mathf.Exp(-dt / Ease);
            Vector3 now = forward == Vector3.zero
                ? Vector3.RotateTowards(shown.Value, wanted, Vector3.Angle(shown.Value, wanted) * share * Mathf.Deg2Rad, 0f)
                : AroundBow(shown.Value, wanted, forward, share);
            // Back with the weather's wind: the game has it again.
            if (wanted == weather && Vector3.Angle(now, weather) < 0.5f)
            {
                shown = null;
                return;
            }
            shown = now;
            __instance.m_wind = new Vector4(now.x, 0f, now.z, __instance.m_wind.w);
            // Sails, cloth and flags follow it too.
            PointCloth(__instance, now);
            Shader.SetGlobalVector(EnvMan.s_globalWindForce, __instance.GetWindForce());
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // The cloth's wind zone (its type is in an assembly the mod doesn't reference), found once.
    // Called every update while the wind is turned, so it is bound once as a delegate rather than invoked by reflection.
    private static readonly System.Reflection.FieldInfo? ClothZone = AccessTools.Field(typeof(EnvMan), "m_clothWindZone");
    private static object? clothZone;
    private static Action<Vector3>? setClothWind;

    private static void PointCloth(EnvMan env, Vector3 dir)
    {
        object? zone = ClothZone?.GetValue(env);
        if (zone == null) return;
        if (!ReferenceEquals(zone, clothZone))
        {
            clothZone = zone;
            System.Reflection.MethodInfo? method = AccessTools.Method(zone.GetType(), "SetWindDirection", [typeof(Vector3)]);
            setClothWind = method == null ? null : (Action<Vector3>)Delegate.CreateDelegate(typeof(Action<Vector3>), zone, method, false);
        }
        setClothWind?.Invoke(dir);
    }

    // Eases the wind toward wanted, measured from the bow: never across it (round behind the ship instead), and never
    // closer to it than Floor.
    private static Vector3 AroundBow(Vector3 shown, Vector3 wanted, Vector3 forward, float share)
    {
        float from = Vector3.SignedAngle(forward, -shown, Vector3.up);
        float to = Vector3.SignedAngle(forward, -wanted, Vector3.up);
        float turn = Mathf.DeltaAngle(from, to);
        float end = from + turn;
        if (from != 0f && Mathf.Sign(from) != Mathf.Sign(end) && Mathf.Abs(end) <= 180f) turn -= 360f * Mathf.Sign(turn);
        float angle = Mathf.DeltaAngle(0f, from + turn * share);
        if (Mathf.Abs(angle) < Floor) angle = (angle == 0f ? side : Mathf.Sign(angle)) * Floor;
        return -(Quaternion.AngleAxis(angle, Vector3.up) * forward);
    }

    // The wind the ship should feel: the weather's, unless it comes from within Arc of the bow. forward: the ship's bow
    // when the rite steers the wind, else zero.
    private static Vector3 Wanted(EnvMan env, Vector3 weather, out Vector3 forward)
    {
        forward = Vector3.zero;
        if (env.m_debugWind || !Player.m_localPlayer || Player.m_localPlayer.InInterior()) return weather;
        // At the edge of the world the game blows outward; that stays.
        if (Utils.LengthXZ(Player.m_localPlayer.transform.position) > 10500f - env.m_edgeOfWorldWidth) return weather;
        Ship ship = Ship.GetLocalShip();
        // Moder's power already blows from behind.
        if (!ship || ship.IsWindControllActive() || !Blessed(ship)) return weather;

        Vector3 bow = ship.transform.forward;
        bow.y = 0f;
        if (bow.sqrMagnitude < 0.0001f) return weather;
        forward = bow.normalized;
        float angle = Vector3.SignedAngle(forward, -weather, Vector3.up);
        if (Mathf.Abs(angle) >= Arc) return weather;
        if (Mathf.Abs(angle) > KeepSide) side = Mathf.Sign(angle);
        return -(Quaternion.AngleAxis(side * Arc, Vector3.up) * forward);
    }

    private static bool Blessed(Ship ship)
    {
        foreach (Player player in ship.m_players)
            if (Blessings.Has(player, WeatherKind.FairWind)) return true;
        return false;
    }
}

// The Gale-Breath Rite: the wind blows at full strength. Each game works out its own wind from the weather (and turns it
// behind a ship with Moder's power, or off the bow with the Fair-Wind Rite); only the strength it aims for is raised,
// so the direction still changes as usual and the game eases into the new strength the way it eases into any other.
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
//   target is inside waits at the edge instead of following, for a few seconds, then gives up and runs. Wandering never picks a point inside. None goes after the
//   structures in or near the area, from the moment the rite starts. One hurt not long ago (or whose pack was) fights
//   as usual while it senses a foe; once it hasn't for a few seconds, it runs out like the rest.
//   A creature that first appeared inside a location (ruins, dvergr towers, fuling villages...) or from a spawner
//   (a spawn point, nest or bone pile) lives there: at home the rite leaves it alone, even inside the area, but it
//   doesn't chase a foe out into the area; strayed into the area, it shuns it like the rest but goes back home.
// - Carrion: a creature within reach that would wander about walks to the obliterator instead and mills about there.
//   Its senses are dulled while it has no target and isn't alerted, so it notices a player only from closer; once it
//   does, it reacts as usual with its full senses, and walks on once it calms down.
internal static class CreaturePatches
{
    private const string Feature = "Packward and Carrion Rites";
    // Packward: a creature in a fight that hasn't sensed a foe (or been hurt) for this long runs.
    private const float LostSight = 5f;
    // Packward: a creature waits at the edge for its target inside this long, then gives up on it and runs.
    private const float EdgeWait = 5f;
    // When a creature held back since, and when it last did (an older hold is forgotten).
    private static readonly System.Collections.Generic.Dictionary<int, (float Since, float Seen)> Holding = [];
    // A drawn creature that has no path to the obliterator wanders as usual for this long before it tries again.
    private const float NoPathWait = 10f;
    private static readonly System.Collections.Generic.Dictionary<int, float> NoPathUntil = [];
    // Packward: a location creature counts as home within this much beyond its location's edge.
    private const float HomeMargin = 8f;
    // Packward: how long "no home here" is trusted before looking again (the location may still be loading).
    private const float HomeRecheck = 5f;
    // Each creature's home location (center and radius with the margin), or none until Recheck.
    private static readonly System.Collections.Generic.Dictionary<int, (bool Has, Vector3 Center, float Radius, float Recheck)> Homes = [];

    internal static void ClearSession()
    {
        Homes.Clear();
        Nests.Clear();
        GoneNests.Clear();
        NoPathUntil.Clear();
        Provokes.Clear();
        Holding.Clear();
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
                if (__instance.m_targetStatic && Spares(__instance, __instance.m_targetStatic)) __instance.m_targetStatic = null;
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

    // Under the Packward Rite, structures in or near the area are never picked as targets.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FindClosestStaticPriorityTarget))]
    private static class PriorityStructure
    {
        private static void Postfix(BaseAI __instance, ref StaticTarget? __result) => Refuse(__instance, ref __result);
    }

    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FindRandomStaticTarget))]
    private static class RandomStructure
    {
        private static void Postfix(BaseAI __instance, ref StaticTarget? __result) => Refuse(__instance, ref __result);
    }

    private static void Refuse(BaseAI ai, ref StaticTarget? target)
    {
        if (!target || PatchGuard.IsDisabled(Feature)) return;
        try
        {
            if (Spares(ai, target!)) target = null;
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Packward: whether a creature leaves a structure alone: the structure, or the creature, is in or near the area.
    // Not within a Crimson or Ragnarök Rite's bubble, or while a raid reaches the area.
    private static bool Spares(BaseAI ai, StaticTarget target)
    {
        if (!WeatherState.IsActive(WeatherKind.Packward) || !Owned(ai)) return false;
        Character character = ai.m_character;
        if (!character || !CreatureRites.Steered(character, WeatherKind.Packward)) return false;
        Vector3 position = character.transform.position;
        if (WeatherState.Suspended(WeatherKind.Packward, position)) return false;
        // At home: only structures out in the area are spared, not those in its home.
        if (HomeOf(ai, out Vector3 home, out float homeRadius) && Utils.DistanceXZ(position, home) <= homeRadius)
            return WeatherState.Reaches(WeatherKind.Packward, target.GetCenter(), CreatureRites.Buffer)
                && Utils.DistanceXZ(target.GetCenter(), home) > homeRadius;
        return WeatherState.Reaches(WeatherKind.Packward, position, CreatureRites.Buffer)
            || WeatherState.Reaches(WeatherKind.Packward, target.GetCenter(), CreatureRites.Buffer);
    }

    // Packward: whether a creature in a fight still has a foe: one it sensed (or was hurt) within LostSight. Animals
    // run from their foe on their own: theirs counts as long as they have one.
    private static bool SensesFoe(BaseAI ai)
    {
        if (ai is not MonsterAI monster) return ai.HaveTarget();
        if (!monster.m_targetCreature) return false;
        return monster.m_timeSinceSensedTargetCreature < LostSight || ai.m_timeSinceHurt < LostSight;
    }

    private static void FleeIfNoTarget(BaseAI ai, Senses state)
    {
        if (!state.FleeAfter || PatchGuard.IsDisabled(Feature)) return;
        try
        {
            if (!ai || SensesFoe(ai) || !WeatherState.IsActive(WeatherKind.Packward)) return;
            GoOut(ai, state.Dt, WeatherState.Position(WeatherKind.Packward), PluginConfig.Radius(WeatherKind.Packward) + CreatureRites.Buffer, HasHome(ai));
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

    // Under the Packward Rite, no wandering into the area. Creatures with a home wander about it as usual, even where
    // it is inside the area, but not out of it into the area.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsValidRandomMovePoint))]
    private static class WanderPoint
    {
        private static void Postfix(BaseAI __instance, Vector3 point, ref bool __result)
        {
            if (!__result || PatchGuard.IsDisabled(Feature) || !WeatherState.IsActive(WeatherKind.Packward)) return;
            try
            {
                if (WeatherState.Reaches(WeatherKind.Packward, point, CreatureRites.Buffer / 2f)
                    && CreatureRites.Steered(__instance.m_character, WeatherKind.Packward)
                    && !AtHome(__instance, point))
                    __result = false;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    private static bool Owned(BaseAI ai) => ai.m_nview && ai.m_nview.IsValid() && ai.m_nview.IsOwner();

    // Packward: the creature's home, if it has one: the location its spawn point is in, or else the spawner it came
    // from (a single spawn point, or a nest or bone pile it appeared around). Looked up again now and then, so a
    // destroyed spawner stops being home.
    private static bool HomeOf(BaseAI ai, out Vector3 center, out float radius)
    {
        int id = ai.GetInstanceID();
        float now = Time.time;
        if (!Homes.TryGetValue(id, out var home) || now >= home.Recheck)
        {
            home = FindHome(ai, out Vector3 at, out float size)
                ? (true, at, size, now + HomeKept)
                : (false, Vector3.zero, 0f, now + HomeRecheck);
            if (Homes.Count > 1000) Homes.Clear();
            Homes[id] = home;
        }
        center = home.Center;
        radius = home.Radius;
        return home.Has;
    }

    // Packward: how long a home found is trusted before looking again.
    private const float HomeKept = 30f;
    // A single spawn point puts its creature on its own spot (dropped to the floor): within this of it.
    private const float SpawnerSpot = 1.5f;
    // Loaded nests and bone piles (the game keeps no list of them).
    private static readonly System.Collections.Generic.HashSet<SpawnArea> Nests = [];
    private static readonly System.Collections.Generic.List<SpawnArea> GoneNests = [];

    [HarmonyPatch(typeof(SpawnArea), "Awake")]
    private static class NestLoaded
    {
        private static void Postfix(SpawnArea __instance)
        {
            if (__instance) Nests.Add(__instance);
        }
    }

    private static bool FindHome(BaseAI ai, out Vector3 center, out float radius)
    {
        Vector3 spawn = ai.m_spawnPoint;
        Location location = Location.GetLocation(spawn);
        if (location)
        {
            center = location.transform.position;
            radius = location.GetMaxRadius() + HomeMargin;
            return true;
        }
        // Around a spawner: as far as it wanders about its spawn point, and the margin.
        radius = Mathf.Max(ai.m_randomMoveRange, 2f) + HomeMargin;
        foreach (CreatureSpawner spawner in CreatureSpawner.m_creatureSpawners)
        {
            if (!spawner || Utils.DistanceXZ(spawner.transform.position, spawn) > SpawnerSpot) continue;
            center = spawner.transform.position;
            return true;
        }
        foreach (SpawnArea nest in Nests)
        {
            if (!nest)
            {
                GoneNests.Add(nest!);
                continue;
            }
            if (Utils.DistanceXZ(nest.transform.position, spawn) > nest.m_spawnRadius + 0.5f) continue;
            center = nest.transform.position;
            radius += nest.m_spawnRadius;
            return true;
        }
        foreach (SpawnArea gone in GoneNests) Nests.Remove(gone);
        GoneNests.Clear();
        center = Vector3.zero;
        return false;
    }

    private static bool HasHome(BaseAI ai) => HomeOf(ai, out _, out _);

    // Packward: a creature going home is back: at its spawn point, or well inside its home (past the margin).
    private static bool Arrived(BaseAI ai, Vector3 position, Vector3 home, float homeRadius) =>
        Utils.DistanceXZ(position, ai.m_spawnPoint) <= 3f || Utils.DistanceXZ(position, home) <= homeRadius - HomeMargin;

    // Packward: a location creature at its home, which the rite leaves alone.
    private static bool AtHome(BaseAI ai, Vector3 position) =>
        HomeOf(ai, out Vector3 center, out float radius) && Utils.DistanceXZ(position, center) <= radius;

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
        if (HomeOf(ai, out Vector3 home, out float homeRadius)) return ShunFromHome(ai, dt, target, targetStatic, home, homeRadius);
        CreatureRites.Mood was = CreatureRites.MoodOf(zdo);
        float distance = Utils.DistanceXZ(position, center);
        CreatureRites.Mood mood = CreatureRites.Mood.None;
        // Hurt not long ago, or one of its pack nearby: it fights whoever it senses; with no one in sight for a few
        // seconds, it runs.
        bool hurt = ai.m_timeSinceHurt < PluginConfig.PackwardFightSeconds.Value;
        bool provoked = !hurt && distance <= radius + CreatureRites.Buffer && Provoked(ai);
        bool roused = hurt || provoked;
        if (!(roused && SensesFoe(ai)))
        {
            if (distance <= radius || (was == CreatureRites.Mood.Fleeing && distance <= radius + CreatureRites.Buffer))
                mood = CreatureRites.Mood.Fleeing;
            else if (distance <= radius + CreatureRites.Buffer && TargetInside(target, targetStatic, center, radius, out _))
                mood = CreatureRites.Mood.HoldingBack;
        }
        if (mood == CreatureRites.Mood.HoldingBack && HeldTooLong(ai))
        {
            mood = CreatureRites.Mood.Fleeing;
            GiveUp(ai);
        }
        if (mood != was)
        {
            CreatureRites.SetMood(zdo, mood);
            if (mood != CreatureRites.Mood.None) Plugin.ModLogger.LogDebug($"Packward Rite: {character.m_name} {(mood == CreatureRites.Mood.Fleeing ? "leaves the area" : "holds back")}.");
        }
        if (mood == CreatureRites.Mood.None) return Steer.None;
        // Roused: its own AI looks around first, so it can notice a foe.
        if (roused && mood == CreatureRites.Mood.Fleeing) return Steer.FleeAfter;

        if (!BaseUpdate.Run(ai, dt)) return Steer.None;
        if (mood == CreatureRites.Mood.Fleeing) GoOut(ai, dt, center, radius + CreatureRites.Buffer, false);
        else
        {
            ai.StopMoving();
            if (TargetInside(target, targetStatic, center, radius, out Vector3 at)) ai.LookAt(at);
        }
        return Steer.Took;
    }

    // Packward, for a creature with a home (a location or spawner).
    // - At home it is left alone, but doesn't go after a foe out in the area: it holds back at home, watching, and
    //   gives up after EdgeWait. Whoever comes into its home it fights as usual.
    // - Out in the area it acts like any other creature there, but goes home instead of out: it fights only when
    //   roused (hurt, or its pack was) while it senses a foe, and goes all the way back to where it spawned.
    // So it doesn't flip between chasing and going home at its home's edge.
    private static Steer ShunFromHome(BaseAI ai, float dt, Character? target, StaticTarget? targetStatic, Vector3 home, float homeRadius)
    {
        ZDO zdo = ai.m_nview.GetZDO();
        Vector3 center = WeatherState.Position(WeatherKind.Packward);
        float reach = PluginConfig.Radius(WeatherKind.Packward) + CreatureRites.Buffer;
        Vector3 position = ai.m_character.transform.position;
        float distance = Utils.DistanceXZ(position, center);
        bool atHome = Utils.DistanceXZ(position, home) <= homeRadius;
        CreatureRites.Mood was = CreatureRites.MoodOf(zdo);
        bool returning = was == CreatureRites.Mood.Fleeing;
        bool hurt = ai.m_timeSinceHurt < PluginConfig.PackwardFightSeconds.Value;
        bool roused = hurt || (distance <= reach && Provoked(ai));

        CreatureRites.Mood mood = CreatureRites.Mood.None;
        if (!(roused && SensesFoe(ai)))
        {
            if (atHome)
            {
                if (returning && !Arrived(ai, position, home, homeRadius)) mood = CreatureRites.Mood.Fleeing;
                else
                {
                    if (returning) Forget(ai);
                    if (TargetOut(target, targetStatic, center, reach, home, homeRadius, out _)) mood = CreatureRites.Mood.HoldingBack;
                }
            }
            // Once going home, it keeps on a little farther out than it would start to.
            else if (distance <= reach + (returning ? CreatureRites.Buffer : 0f)) mood = CreatureRites.Mood.Fleeing;
        }
        if (mood == CreatureRites.Mood.HoldingBack && HeldTooLong(ai))
        {
            mood = CreatureRites.Mood.None;
            GiveUp(ai);
        }
        if (mood != was)
        {
            CreatureRites.SetMood(zdo, mood);
            if (mood != CreatureRites.Mood.None) Plugin.ModLogger.LogDebug($"Packward Rite: {ai.m_character.m_name} {(mood == CreatureRites.Mood.Fleeing ? "goes back home" : "holds back at home")}.");
        }
        if (mood == CreatureRites.Mood.None) return Steer.None;
        if (roused && mood == CreatureRites.Mood.Fleeing) return Steer.FleeAfter;

        if (!BaseUpdate.Run(ai, dt)) return Steer.None;
        if (mood == CreatureRites.Mood.Fleeing) GoOut(ai, dt, center, reach, true);
        else
        {
            ai.StopMoving();
            if (TargetOut(target, targetStatic, center, reach, home, homeRadius, out Vector3 at)) ai.LookAt(at);
        }
        return Steer.Took;
    }

    // Packward: whether a home creature's target is out in the area (in reach, and not in its home).
    private static bool TargetOut(Character? target, StaticTarget? targetStatic, Vector3 center, float reach, Vector3 home, float homeRadius, out Vector3 at)
    {
        at = target ? target!.transform.position : targetStatic ? targetStatic!.GetCenter() : Vector3.zero;
        return (target || targetStatic) && Utils.DistanceXZ(at, center) <= reach && Utils.DistanceXZ(at, home) > homeRadius;
    }

    // Packward: whether a creature holding back at the edge has waited EdgeWait already.
    private static bool HeldTooLong(BaseAI ai)
    {
        int id = ai.GetInstanceID();
        float now = Time.time;
        if (!Holding.TryGetValue(id, out var held) || now - held.Seen > 1f) held = (now, now);
        if (now - held.Since >= EdgeWait)
        {
            Holding.Remove(id);
            return true;
        }
        Holding[id] = (held.Since, now);
        if (Holding.Count > 500) Holding.Clear();
        return false;
    }

    // Back home, it forgets the foe it lost and calms down, but looks around again at once: whoever it sees or hears,
    // or who comes into its home, it goes after as usual.
    private static void Forget(BaseAI ai)
    {
        if (ai is MonsterAI monster)
        {
            monster.m_targetCreature = null;
            monster.m_targetStatic = null;
            monster.m_updateTargetTimer = 0f;
        }
        ai.SetAlerted(false);
    }

    // It forgets its target and calms down, and looks for a new one only after a while, so it gets out first.
    private static void GiveUp(BaseAI ai)
    {
        if (ai is MonsterAI monster)
        {
            monster.m_targetCreature = null;
            monster.m_targetStatic = null;
            monster.m_updateTargetTimer = EdgeWait * 2f;
        }
        ai.SetAlerted(false);
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
    // fleeing. One with a home runs back to where it spawned instead, while there is a path there.
    private static void GoOut(BaseAI ai, float dt, Vector3 center, float exitRadius, bool home)
    {
        Vector3 position = ai.transform.position;
        float now = Time.time;
        if (now - ai.m_fleeTargetUpdateTime > ai.m_fleeInterval)
        {
            ai.m_fleeTargetUpdateTime = now;
            ai.m_lastFlee = now;
            if (home && ai.HavePath(ai.m_spawnPoint))
            {
                ai.m_fleeTarget = ai.m_spawnPoint;
                ai.MoveTo(dt, ai.m_fleeTarget, 1f, true);
                return;
            }
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
            // Its home is inside: it wanders where it is instead (points inside are refused, see WanderPoint). Location
            // creatures wander about their home as usual.
            if (!WeatherState.Reaches(WeatherKind.Packward, ai.m_spawnPoint, CreatureRites.Buffer)) return false;
            if (!CreatureRites.Steered(character, WeatherKind.Packward) || HasHome(ai)) return false;
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

    // Whether any icon was made since the rites last went quiet: until then, huds aren't searched for icons every frame.
    private static bool _anyIcons;

    private static void Postfix(EnemyHud __instance)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            bool on = On();
            if (!on && !_anyIcons) return;
            // One more pass with the rites off hides the icons still showing; after that, nothing to do.
            _anyIcons = on;
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
