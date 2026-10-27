using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// The Tailwind Rite: the game asks the ship whether someone aboard controls the wind (Moder's power); while the rite
// goes on and the local player is aboard, the answer is yes, so the wind blows from behind. Its strength stays the
// weather's.
[HarmonyPatch(typeof(Ship), nameof(Ship.IsWindControllActive))]
internal static class TailwindPatch
{
    private const string Feature = "Tailwind Rite";

    private static void Postfix(Ship __instance, ref bool __result)
    {
        if (__result || PatchGuard.IsDisabled(Feature) || !WeatherState.IsActive(WeatherKind.Tailwind)) return;
        try
        {
            Player? player = Player.m_localPlayer;
            if (player && __instance.m_players.Contains(player!)) __result = true;
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// The Crypt Delver Rite: the local player can't get the Wet status (any already on is removed by WeatherStatus).
[HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), typeof(int), typeof(bool), typeof(int), typeof(float), typeof(short))]
internal static class CryptDelverPatch
{
    private const string Feature = "Crypt Delver Rite";

    private static bool Prefix(SEMan __instance, int nameHash, ref StatusEffect? __result)
    {
        if (nameHash != SEMan.s_statusEffectWet || PatchGuard.IsDisabled(Feature)) return true;
        try
        {
            if (!WeatherState.IsActive(WeatherKind.CryptDelver) || __instance.m_character != Player.m_localPlayer) return true;
            __result = null;
            return false;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return true;
        }
    }
}

// The Packward and Carrion Rites steer creatures, in the game that runs their AI (their owner).
// - Packward: a hostile creature (not a boss, not tamed) that comes within the radius of the obliterator is marked to
//   flee, and runs from the obliterator for as long as the rite goes on, wherever it goes.
// - Carrion: wild animals of the listed kinds within the radius walk to the obliterator, unless they are fighting,
//   alerted or already there.
internal static class CreaturePatches
{
    private const string Feature = "Packward and Carrion Rites";
    // Saved on the creature: until when (world time, ticks) it flees.
    private static readonly int FleeUntil = "TempestRite_FleeUntil".GetStableHashCode();
    // Close enough to the obliterator to stop walking to it.
    private const float CarrionArrived = 10f;

    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    private static class Monsters
    {
        private static bool Prefix(MonsterAI __instance, float dt, ref bool __result)
        {
            if (PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                if (Flee(__instance, dt) || Lure(__instance, dt, __instance.m_targetCreature != null))
                {
                    __result = true;
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(AnimalAI), nameof(AnimalAI.UpdateAI))]
    private static class Animals
    {
        private static bool Prefix(AnimalAI __instance, float dt, ref bool __result)
        {
            if (PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                if (Lure(__instance, dt, false))
                {
                    __result = true;
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    private static bool Flee(MonsterAI ai, float dt)
    {
        if (!WeatherState.IsActive(WeatherKind.Packward)) return false;
        // Only the game that owns a creature runs its AI.
        if (!ai.m_nview || !ai.m_nview.IsValid() || !ai.m_nview.IsOwner()) return false;
        Character character = ai.m_character;
        if (!character || character.IsBoss() || character.IsTamed() || !IsHostile(character)) return false;
        ZDO zdo = ai.m_nview.GetZDO();

        long now = ZNet.instance.GetTime().Ticks;
        long until = zdo.GetLong(FleeUntil, 0L);
        if (until <= now)
        {
            if (!WeatherState.Reaches(WeatherKind.Packward, character.transform.position)) return false;
            // Marked when it comes near: it flees for as long as the rite goes on.
            until = now + TimeSpan.FromSeconds(WeatherState.Remaining(WeatherKind.Packward)).Ticks;
            zdo.Set(FleeUntil, until);
            Plugin.ModLogger.LogDebug($"Packward Rite: {character.m_name} flees.");
        }
        ai.m_targetCreature = null;
        ai.m_targetStatic = null;
        ai.Flee(dt, WeatherState.Position(WeatherKind.Packward));
        return true;
    }

    private static bool Lure(BaseAI ai, float dt, bool hasTarget)
    {
        if (hasTarget || !WeatherState.IsActive(WeatherKind.Carrion) || ai.IsAlerted()) return false;
        if (!ai.m_nview || !ai.m_nview.IsValid() || !ai.m_nview.IsOwner()) return false;
        Character character = ai.m_character;
        if (!character || character.IsTamed()) return false;
        if (!PluginConfig.CarrionPrefabs().Contains(ai.m_nview.GetZDO().GetPrefab())) return false;
        Vector3 altar = WeatherState.Position(WeatherKind.Carrion);
        Vector3 position = character.transform.position;
        if (!WeatherState.Reaches(WeatherKind.Carrion, position) || Utils.DistanceXZ(position, altar) <= CarrionArrived) return false;
        ai.MoveTo(dt, altar, CarrionArrived, false);
        return true;
    }

    // Would attack players: not animals or other players' own (tamed are left out above).
    private static bool IsHostile(Character character) =>
        Player.m_localPlayer && BaseAI.IsEnemy(character, Player.m_localPlayer);
}

// The Crimson Rite: no raid but the mod's own starts while it goes on (ending one is always allowed).
[HarmonyPatch(typeof(RandEventSystem), nameof(RandEventSystem.SetRandomEvent))]
internal static class CrimsonPatch
{
    private static bool Prefix(RandomEvent? ev) => ev == null || CrimsonRaids.Starting || !CrimsonRaids.Active;
}
