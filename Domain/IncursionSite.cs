using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// When and where the Crimson, Ragnarök and Hoard-Bane Rites can't be performed:
// - while a raid goes on anywhere, or a boss fight (see IncursionWatch);
// - where their bubble would reach the world's spawn, a trader's camp or a boss altar (also where its boss would rise);
// - a dungeon's entrance (the dungeon itself, far above, isn't in the fight; Hildir's crypt and cave too);
// - hostile creatures still inside (as it starts, everything inside dies, so it would clear a camp or a ruin for free):
//   a place with creature spawners is fine once cleared, as its spawners stay still until it ends;
// - a claimed bed (someone's spawn point: they'd come back to life in the fight);
// - a shield generator's bubble, or the Sovereign Ward Rite's;
// - a ward standing inside (on or off), or the area of one that is on reaching in;
// - a boss standing there (it would be killed without its drops).
// Checked by the player pulling the lever, with what their game has loaded, and again by the server, with the world's
// locations and objects.
internal static class IncursionSite
{
    // The game's own places, by location name: those the server knows of, also far from any player.
    private static readonly HashSet<string> SpawnPlaces = new(StringComparer.OrdinalIgnoreCase) { "StartTemple" };
    private static readonly HashSet<string> TraderPlaces = new(StringComparer.OrdinalIgnoreCase)
        { "Vendor_BlackForest", "Hildir_camp", "BogWitch_Camp" };
    private static readonly HashSet<string> AltarPlaces = new(StringComparer.OrdinalIgnoreCase)
        { "Eikthyrnir", "GDKing", "Bonemass", "Dragonqueen", "GoblinKing", "Mistlands_DvergrBossEntrance1", "FaderLocation" };
    private static readonly HashSet<string> DungeonPlaces = new(StringComparer.OrdinalIgnoreCase) { "Hildir_crypt", "Hildir_cave" };

    // A trader with no camp around it still has room to trade.
    private const float TraderRoom = 10f;
    // A dungeon's entrance is this near where its rooms are (far above).
    private const float DungeonEntrance = 16f;
    // Objects are looked for this far beyond the bubble: no shield generator reaches farther.
    private const float LookBeyond = 64f;

    private enum Thing
    {
        None = 0,
        Shield = 1,
        Boss = 2,
        Trader = 3,
        Bed = 4,
        Creature = 5,
        Dungeon = 6,
        Ward = 7
    }

    // What each prefab is, by its hash.
    private static readonly Dictionary<int, (Thing Thing, float Reach)> Prefabs = [];

    // Why an incursion rite can't be performed here now, or null.
    internal static string? Refused(WeatherKind kind, Vector3 center)
    {
        string name = WeatherKinds.Name(kind);
        if (RaidGoing()) return string.Format(Messages.IncursionRefusedRaid, name);
        if (BossFightGoing()) return string.Format(Messages.IncursionRefusedBossFight, name);
        float radius = PluginConfig.Radius(kind);
        int hostiles = 0;
        string? what = Ward(center, radius) ?? Places(center, radius) ?? Loaded(center, radius) ?? Objects(center, radius, out hostiles);
        if (what != null) return string.Format(Messages.IncursionSiteRefused, name, what);
        return hostiles > 0 ? string.Format(Messages.IncursionRefusedHostile, name) : null;
    }

    // A raid anywhere in the world (every game knows of it).
    private static bool RaidGoing()
    {
        RandEventSystem? raids = RandEventSystem.instance;
        return raids && raids!.m_randomEvent != null;
    }

    // A boss fight anywhere, as the server says, or a boss this game has (not one a rite brought).
    private static bool BossFightGoing()
    {
        if (IncursionWatch.BossFight) return true;
        foreach (Character character in Character.GetAllCharacters())
            if (character && !character.IsDead() && character.IsBoss() && !IncursionArena.IsRiteCreature(character)) return true;
        return false;
    }

    // Whether this prefab is a boss.
    internal static bool IsBoss(int hash) => ZNetScene.instance && What(hash).Item1 == Thing.Boss;

    private static string? Ward(Vector3 center, float radius)
    {
        if (!WeatherState.IsActive(WeatherKind.SovereignWard)) return null;
        float reach = radius + PluginConfig.Radius(WeatherKind.SovereignWard);
        if (Utils.DistanceXZ(WeatherState.Position(WeatherKind.SovereignWard), center) > reach) return null;
        return string.Format(Messages.IncursionSiteWard, WeatherKinds.Name(WeatherKind.SovereignWard));
    }

    // The world's locations, as the server knows them (players' games don't).
    private static string? Places(Vector3 center, float radius)
    {
        ZoneSystem? zones = ZoneSystem.instance;
        if (!zones) return null;
        foreach (ZoneSystem.LocationInstance place in zones!.m_locationInstances.Values)
        {
            ZoneSystem.ZoneLocation? location = place.m_location;
            if (location == null) continue;
            string? what = PlaceKind(location.Hash);
            if (what == null) continue;
            float size = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
            if (Utils.DistanceXZ(place.m_position, center) < radius + size) return what;
        }
        return null;
    }

    private static string? PlaceKind(string name) => PlaceKind(name.GetStableHashCode());

    // By the hash of the location's name, as the server keeps it.
    private static string? PlaceKind(int hash)
    {
        if (Hashes(SpawnPlaces).Contains(hash)) return Messages.IncursionSiteSpawn;
        if (Hashes(TraderPlaces).Contains(hash)) return Messages.IncursionSiteTrader;
        if (Hashes(AltarPlaces).Contains(hash)) return Messages.IncursionSiteAltar;
        if (Hashes(DungeonPlaces).Contains(hash)) return Messages.IncursionSiteDungeon;
        return null;
    }

    private static readonly Dictionary<HashSet<string>, HashSet<int>> HashesOf = [];

    private static HashSet<int> Hashes(HashSet<string> names)
    {
        if (HashesOf.TryGetValue(names, out HashSet<int> hashes)) return hashes;
        hashes = [];
        foreach (string name in names) hashes.Add(name.GetStableHashCode());
        return HashesOf[names] = hashes;
    }

    // What the game has loaded around here: locations (also modded ones with a trader, a boss altar or a dungeon), traders
    // and altars standing alone.
    private static string? Loaded(Vector3 center, float radius)
    {
        foreach (Location location in Location.s_allLocations)
        {
            if (!location || !location.IsInside(center, radius)) continue;
            string? what = PlaceKind(Utils.GetPrefabName(location.gameObject));
            if (what != null) return what;
            if (location.GetComponentInChildren<Trader>(true)) return Messages.IncursionSiteTrader;
            foreach (OfferingBowl bowl in location.GetComponentsInChildren<OfferingBowl>(true))
                if (bowl.m_bossPrefab) return Messages.IncursionSiteAltar;
            if (location.m_hasInterior || location.m_generator || location.GetComponentInChildren<DungeonGenerator>(true) ||
                location.GetComponentInChildren<Teleport>(true)) return Messages.IncursionSiteDungeon;
        }
        foreach (Trader trader in UnityEngine.Object.FindObjectsByType<Trader>(FindObjectsSortMode.None))
            if (trader && Utils.DistanceXZ(trader.transform.position, center) < radius + TraderRoom) return Messages.IncursionSiteTrader;
        foreach (OfferingBowl bowl in UnityEngine.Object.FindObjectsByType<OfferingBowl>(FindObjectsSortMode.None))
            if (bowl && bowl.m_bossPrefab && AltarReaches(bowl, center, radius)) return Messages.IncursionSiteAltar;
        return null;
    }

    // Whether the boss of this altar could rise within the bubble.
    internal static bool AltarReaches(OfferingBowl bowl, Vector3 center, float radius) =>
        Utils.DistanceXZ(bowl.transform.position, center) < radius + Mathf.Max(0f, bowl.m_spawnBossMaxDistance) + IncursionArena.SamePlace;

    // Shield generators, wards, bosses, traders, claimed beds and dungeons, from the world's objects around (in a player's game,
    // those of the zones it has); and how many hostile creatures are inside.
    private static string? Objects(Vector3 center, float radius, out int hostiles)
    {
        hostiles = 0;
        if (ZDOMan.instance == null || !ZNetScene.instance) return null;
        List<ZDO> found = [];
        IncursionArena.FindAround(center, radius + LookBeyond, found);
        foreach (ZDO zdo in found)
        {
            (Thing thing, float reach) = What(zdo.GetPrefab());
            if (thing == Thing.None) continue;
            float distance = Utils.DistanceXZ(zdo.GetPosition(), center);
            // A dungeon's rooms are far above its entrance: where they are tells where it is.
            if (thing == Thing.Dungeon)
            {
                if (distance < radius + DungeonEntrance) return Messages.IncursionSiteDungeon;
                continue;
            }
            if (Character.InInterior(zdo.GetPosition())) continue;
            switch (thing)
            {
                case Thing.Bed when distance < radius && zdo.GetLong(ZDOVars.s_owner) != 0L:
                    return Messages.IncursionSiteBed;
                case Thing.Creature when distance < radius && !zdo.GetBool(ZDOVars.s_tamed) && !zdo.GetBool(IncursionArena.BossMark) &&
                                         zdo.GetInt(HoardBaneArena.Role) == 0:
                    hostiles++;
                    break;
                case Thing.Shield when distance < radius + reach:
                    return Messages.IncursionSiteShield;
                case Thing.Ward when distance < radius || (distance < radius + reach && zdo.GetBool(ZDOVars.s_enabled)):
                    return Messages.IncursionSitePlayerWard;
                // The rites' own bosses are long gone (see IncursionArena.ClearInside).
                case Thing.Boss when distance < radius && !zdo.GetBool(IncursionArena.BossMark) && zdo.GetInt(HoardBaneArena.Role) == 0:
                    return Messages.IncursionSiteBoss;
                case Thing.Trader when distance < radius + TraderRoom:
                    return Messages.IncursionSiteTrader;
            }
        }
        return null;
    }

    private static (Thing, float) What(int hash)
    {
        if (Prefabs.TryGetValue(hash, out (Thing Thing, float Reach) known)) return known;
        GameObject? prefab = ZNetScene.instance.GetPrefab(hash);
        (Thing, float) what = (Thing.None, 0f);
        if (prefab)
        {
            ShieldGenerator shield = prefab!.GetComponent<ShieldGenerator>();
            Character character = prefab.GetComponent<Character>();
            if (shield) what = (Thing.Shield, shield.m_maxShieldRadius);
            else if (character && character.IsBoss()) what = (Thing.Boss, 0f);
            else if (character && IncursionArena.HostilePrefab(character)) what = (Thing.Creature, 0f);
            else if (prefab.GetComponent<Trader>()) what = (Thing.Trader, 0f);
            else if (prefab.GetComponent<Bed>()) what = (Thing.Bed, 0f);
            else if (prefab.GetComponent<PrivateArea>() is { } ward) what = (Thing.Ward, ward.m_radius);
            else if (prefab.GetComponent<DungeonGenerator>()) what = (Thing.Dungeon, 0f);
        }
        Prefabs[hash] = what;
        return what;
    }
}
