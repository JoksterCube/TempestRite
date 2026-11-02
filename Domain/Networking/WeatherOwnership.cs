using System.Collections.Generic;
using JoksterCube.TempestRite.Common.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain.Networking;

// Some of what rites do is worked out only by the game that owns an object, usually the nearest player's. While a
// rite goes on, the objects it changes are handed to players with the mod near them, so everyone around, players
// without the mod too, gets what the rite does there:
// - creatures spawning in a zone or at a spawn point (serpents in a thunderstorm, night creatures under the Midnight
//   Rite): during weather and daylight rites;
// - spreading fire going out in rain, fires that rain puts out: during weather rites;
// - plants and bushes near a Verdant Rite's obliterator (they grow faster);
// - creatures near a Packward or Carrion Rite's obliterator (they keep out, or come), tamed animals near a Kindred
//   Rite's (they breed and grow faster) and its eggs (they hatch faster);
// - fermenters and cooking stations near a Vat-Boil Rite's obliterator, smelters and kilns near a Pitch-Bellows
//   Rite's (they work faster);
// - creature spawners (nests, body piles...) near a Harrow-Spur Rite's obliterator (they spawn faster).
// - ballistas near a Sentry-Sight Rite's obliterator (they pick their targets), sap extractors, eitr refineries and
//   ancient roots near a Taproot-Surge Rite's (they work faster), items on the ground near a Pitch-Seal Rite's (they
//   don't despawn), wisp fountains and eternal pyres near a Primal-Hearth Rite's (they draw more);
// - wisp fountains and eternal pyres, and the wisps and embers on their way to them: also during daylight rites (the
//   Midnight Rite's night is only night on games with the mod, so only those draw wisps then and keep them).
// Plants, stations and tamed animals still owed time from a rite that went on while nobody was near (see RiteSpeedup)
// are handed out the same way, also after the rite ended, so they make it up.
// This only matters when the server lets players without the mod join (Require Mod On Clients off). The server already
// hands out objects to players every 2 seconds; right after, it moves these objects from players without the mod to
// a player with it who is near. Objects already owned by a player with the mod are left alone.
internal static class WeatherOwnership
{
    // Spawning works zone by zone (64 m), around players in the zone.
    private const float ZoneRange = 96f;
    private const float FireRange = 48f;
    // Creatures are taken a little before they reach a located rite's radius.
    private const float CreatureMargin = 20f;
    // Lured wisps and embers are taken this far from a Primal-Hearth Rite's radius (they are sent from up to 25 m away).
    private const float LuredMargin = 30f;

    private enum Kind
    {
        None,
        Spawner,
        Fire,
        Growing,
        Creature,
        // Sped up by a rite (see RiteSpeedup.KindOf).
        Station,
        // A creature spawner with the game's SpawnArea (see SpawnerSpur).
        Nest,
        // A wisp fountain or eternal pyre (see LureSurge).
        Lure,
        // A wisp or ember on its way to one.
        Lured
    }

    private static Dictionary<int, Kind>? _prefabs;

    // Server: players with the mod, worked out once per frame.
    private static readonly HashSet<long> ServerModded = [];
    private static int _serverModdedFrame = -1;

    internal static void ClearSession()
    {
        ServerModded.Clear();
        _serverModdedFrame = -1;
    }

    // Whether any rite going on hands objects out.
    private static bool AnyRite(bool weather) =>
        weather
        || WeatherState.Active(EffectSlot.Daylight) != WeatherKind.None
        || WeatherState.IsActive(WeatherKind.Verdant)
        || WeatherState.IsActive(WeatherKind.Packward)
        || WeatherState.IsActive(WeatherKind.Carrion)
        || WeatherState.IsActive(WeatherKind.VatBoil)
        || WeatherState.IsActive(WeatherKind.PitchBellows)
        || WeatherState.IsActive(WeatherKind.Kindred)
        || WeatherState.IsActive(WeatherKind.HarrowSpur)
        || WeatherState.IsActive(WeatherKind.SentrySight)
        || WeatherState.IsActive(WeatherKind.TaprootSurge)
        || WeatherState.IsActive(WeatherKind.MantleAnchor)
        || WeatherState.IsActive(WeatherKind.PrimalHearth)
        || RiteHistory.Any;

    // Whether the object at position should go to a player with the mod within reach, and how near they must be.
    // weatherHere: a weather rite shows at that position.
    private static bool Wanted(ZDO zdo, Vector3 position, bool weatherHere, out float reach)
    {
        reach = ZoneRange;
        switch (_prefabs!.TryGetValue(zdo.GetPrefab(), out Kind kind) ? kind : Kind.None)
        {
            case Kind.Spawner:
                return weatherHere || WeatherState.Active(EffectSlot.Daylight) != WeatherKind.None;
            case Kind.Fire:
                reach = FireRange;
                return weatherHere;
            case Kind.Growing:
                return WeatherState.Reaches(WeatherKind.Verdant, position) || RiteSpeedup.Owed(zdo, position);
            case Kind.Creature:
                return (WeatherState.Reaches(WeatherKind.Packward, position, CreatureMargin) && CreatureRites.Steered(zdo, WeatherKind.Packward))
                    || (WeatherState.Reaches(WeatherKind.Carrion, position, CreatureMargin) && CreatureRites.Steered(zdo, WeatherKind.Carrion))
                    || (WeatherState.Reaches(WeatherKind.Kindred, position) && zdo.GetBool(ZDOVars.s_tamed)
                        && RiteSpeedup.KindOf(zdo.GetPrefab()) == WeatherKind.Kindred)
                    || (zdo.GetBool(ZDOVars.s_tamed) && RiteSpeedup.Owed(zdo, position));
            case Kind.Station:
                return WeatherState.Reaches(RiteSpeedup.KindOf(zdo.GetPrefab()), position) || RiteSpeedup.Owed(zdo, position);
            case Kind.Nest:
                return SpawnerSpur.Reaches(zdo.GetPrefab(), position);
            case Kind.Lure:
                return WeatherState.Active(EffectSlot.Daylight) != WeatherKind.None || WeatherState.Reaches(WeatherKind.PrimalHearth, position);
            case Kind.Lured:
                return WeatherState.Active(EffectSlot.Daylight) != WeatherKind.None
                    || WeatherState.Reaches(WeatherKind.PrimalHearth, position, LuredMargin);
            default:
                return false;
        }
    }

    // After the server handed out the objects near one player.
    internal static void AfterServerRelease(ZDOMan zdoMan, Vector3 referencePosition, long uid)
    {
        bool weather = WeatherState.Active(EffectSlot.Weather) != WeatherKind.None;
        if (!AnyRite(weather) || !Prefabs()) return;
        if (!IsModdedOnServer(uid)) return;

        Vector2s zone = ZoneSystem.GetZone(referencePosition);
        WeatherBiomes affected = PluginConfig.AffectedBiomes.Value;
        foreach (ZDO zdo in zdoMan.m_tempNearObjects)
        {
            if (!_prefabs!.ContainsKey(zdo.GetPrefab())) continue;
            long owner = zdo.GetOwner();
            // No owner: the game gives it out itself. Owned by a player with the mod: already right.
            if (owner == 0 || owner == uid || IsModdedOnServer(owner)) continue;
            Vector3 position = zdo.GetPosition();
            if (!ZNetScene.InActiveArea(position, zone)) continue;
            bool weatherHere = weather && affected.Includes((int)WorldGenerator.instance.GetBiome(position));
            if (!Wanted(zdo, position, weatherHere, out float reach)) continue;
            if ((position - referencePosition).sqrMagnitude > reach * reach) continue;
            zdo.SetOwner(uid);
        }
    }

    private static bool IsModdedOnServer(long uid)
    {
        if (_serverModdedFrame != Time.frameCount)
        {
            _serverModdedFrame = Time.frameCount;
            ServerModded.Clear();
            // The host plays too; a dedicated server doesn't.
            if (!ZNet.instance.IsDedicated()) ServerModded.Add(ZDOMan.GetSessionID());
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
                if (RpcHandlers.ValidatedPeers.Contains(peer.m_rpc)) ServerModded.Add(peer.m_uid);
        }
        return ServerModded.Contains(uid);
    }

    // ---- Which objects ----

    // Found once from the game's prefabs: zone controllers and spawn points (spawning), anything weather can put out,
    // plants and bushes that regrow, creatures (not bosses), the stations rites speed up and creature spawners.
    private static bool Prefabs()
    {
        if (_prefabs != null) return true;
        if (!ZNetScene.instance || !ZoneSystem.instance) return false;
        _prefabs = [];
        if (ZoneSystem.instance.m_zoneCtrlPrefab) _prefabs[ZoneSystem.instance.m_zoneCtrlPrefab.name.GetStableHashCode()] = Kind.Spawner;
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            if (!prefab) continue;
            int hash = prefab.name.GetStableHashCode();
            Fireplace? fireplace = prefab.GetComponent<Fireplace>();
            Character? character = prefab.GetComponent<Character>();
            if (prefab.GetComponent<CreatureSpawner>()) _prefabs[hash] = Kind.Spawner;
            else if (prefab.GetComponentInChildren<SpawnArea>(true)) _prefabs[hash] = Kind.Nest;
            else if (prefab.GetComponentInChildren<WispSpawner>(true)) _prefabs[hash] = Kind.Lure;
            else if (prefab.GetComponent<LuredWisp>()) _prefabs[hash] = Kind.Lured;
            else if (prefab.GetComponent<Fire>() || (fireplace && fireplace!.m_canTurnOff)) _prefabs[hash] = Kind.Fire;
            else if (VerdantGrowth.IsGrowing(hash)) _prefabs[hash] = Kind.Growing;
            else if (prefab.GetComponent<BaseAI>() && character && !character!.IsBoss() && !character.IsPlayer()) _prefabs[hash] = Kind.Creature;
            else if (RiteSpeedup.KindOf(hash) != WeatherKind.None) _prefabs[hash] = Kind.Station;
        }
        Plugin.ModLogger.LogDebug($"Rite-sensitive prefabs: {_prefabs.Count}.");
        return true;
    }
}
