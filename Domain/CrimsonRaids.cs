using System;
using System.Collections.Generic;
using System.Reflection;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// Raids live on the server (and in single player or when hosting); players' games only follow them (see CrimsonArena
// for the players' side: the kill count, the reward; IncursionArena for the Blood Grounds, portals and the map).
// - The Crimson Rite: while it goes on, the server keeps a raid going at the obliterator: when one ends, the next starts
//   after a short pause, picked among the raids the players within the radius are ready for (player based raids and
//   World Advancement Progression's private raids too), with no wait and no base needed, each coming as often as the
//   others, and raids below the players brought up to them (see RaidCatchUp); lightning strikes the obliterator as each raid after the first comes, and the last raid's creatures still
//   inside die. No other raid starts anywhere
//   meanwhile (see CrimsonPatch), and one going on elsewhere is replaced. Creatures slain inside are counted. It breaks off when the obliterator is destroyed or everyone inside has fallen
//   or left; when its time runs out, the obliterator receives a reward for each kill.
// - The Sanctuary Rite: ends the raid going on at once, and with it the Crimson Rite (no reward).
// Only one Crimson or Ragnarök Rite goes on at a time (see IncursionArena.RiteRefused).
internal static class CrimsonRaids
{
    internal enum Ending
    {
        Survived = 0,
        Obliterator = 1,
        Fallen = 2,
        Ended = 3
    }

    private const string Feature = "Crimson Rite raids";
    private const string ObliteratorPrefab = "incinerator";
    // Close enough to count as a raid at the obliterator, or as the obliterator itself.
    internal const float SamePlace = 2f;
    private const float CheckInterval = 1f;
    // How long nobody may be alive inside before the rite breaks off: player positions arrive a little late.
    private const float FallenGrace = 5f;
    // Going on from before the server started again: the players inside get this long to come back.
    private const float ResumeGrace = 60f;
    private const float RewardRetry = 5f;
    private const int RewardTries = 24;

    private static bool _running;
    private static Vector3 _center;
    private static ZDOID _obliterator = ZDOID.None;
    private static int _kills;
    private static readonly HashSet<ZDOID> Counted = [];
    private static float _nextStart;
    private static float _nextCheck;
    private static float _emptySince = -1f;
    private static bool _someoneInside;
    private static bool _performed;
    private static bool _resumed;
    private static bool _warned;
    private static string? _lastRaid;
    // How often each raid came during this rite.
    private static readonly Dictionary<string, int> Given = [];
    // The catch-up of the raid going on, told to everyone again now and then (for those who come later).
    private static int _steps;
    private static float _nextTell;
    private const float TellInterval = 10f;

    // A reward the obliterator's owner hasn't been reached for yet.
    private static ZDOID _rewardAt = ZDOID.None;
    private static int _rewardAmount;
    private static int _rewardTries;
    private static float _nextReward;

    // While the mod starts a raid itself (the only raid allowed to start during the Crimson Rite).
    internal static bool Starting { get; private set; }

    internal static bool Active => WeatherState.IsActive(WeatherKind.Crimson);

    internal static void Register(ZRoutedRpc rpc) => rpc.Register<ZDOID, Vector3>(Rpcs.CrimsonKill, RPC_Kill);

    internal static void ClearSession()
    {
        _running = false;
        _center = Vector3.zero;
        _obliterator = ZDOID.None;
        _kills = 0;
        Counted.Clear();
        _nextStart = 0f;
        _nextCheck = 0f;
        _emptySince = -1f;
        _someoneInside = false;
        _performed = false;
        _resumed = false;
        _warned = false;
        _lastRaid = null;
        Given.Clear();
        _steps = 0;
        _nextTell = 0f;
        _rewardAt = ZDOID.None;
        _rewardAmount = 0;
        _rewardTries = 0;
        Starting = false;
    }

    // On the server, right after a rite was performed.
    internal static void Performed(WeatherKind kind)
    {
        if (!IsServer() || !RandEventSystem.instance) return;
        switch (kind)
        {
            case WeatherKind.Sanctuary:
                if (RandEventSystem.instance.m_randomEvent != null)
                {
                    Plugin.ModLogger.LogInfo($"Sanctuary Rite: ended the raid {RandEventSystem.instance.m_randomEvent.m_name}.");
                    RandEventSystem.instance.ResetRandomEvent();
                }
                if (_running) Stop(Ending.Ended);
                break;
            case WeatherKind.Crimson:
                // The first raid comes right away.
                _nextStart = 0f;
                _performed = true;
                break;
        }
    }

    // Every frame on the server.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || !IsServer() || !RandEventSystem.instance) return;
        try
        {
            GiveRewardLater();
            bool active = Active;
            if (!active)
            {
                // Its time ran out: the other ways it ends stop it themselves.
                if (_running) Stop(Ending.Survived);
                return;
            }
            if (!_running) Begin();

            if (Time.time >= _nextCheck)
            {
                _nextCheck = Time.time + CheckInterval;
                if (CheckBroken()) return;
            }
            Keep(RandEventSystem.instance);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Performed, or still going on after the server started again.
    private static void Begin()
    {
        _running = true;
        _kills = 0;
        Counted.Clear();
        _emptySince = -1f;
        _someoneInside = false;
        _resumed = !_performed;
        _performed = false;
        _warned = false;
        _lastRaid = null;
        Given.Clear();
        _steps = 0;
        _nextStart = 0f;
        _nextCheck = Time.time + CheckInterval;
        _center = WeatherState.Position(WeatherKind.Crimson);
        _obliterator = FindObliterator(_center);
        if (_obliterator.IsNone())
            Plugin.ModLogger.LogWarning($"Crimson Rite: found no obliterator at ({_center.x:0}, {_center.z:0}); it won't break off if one is destroyed.");
    }

    // Whether it broke off: the obliterator is gone, or nobody is alive inside (fallen, left, or never stayed: the one who
    // pulled the lever may have stepped out at once).
    private static bool CheckBroken()
    {
        if (!_obliterator.IsNone() && ZDOMan.instance.GetZDO(_obliterator) == null)
        {
            Stop(Ending.Obliterator);
            return true;
        }
        if (PlayersAliveInside() > 0)
        {
            _someoneInside = true;
            _emptySince = -1f;
            return false;
        }
        // Going on from before the server started again, and nobody is back yet: nothing to break.
        bool waitingForReturn = _resumed && !_someoneInside;
        if (waitingForReturn && ZNet.instance.GetAllCharacterZDOS().Count == 0)
        {
            _emptySince = -1f;
            return false;
        }
        if (_emptySince < 0f) _emptySince = Time.time;
        if (Time.time - _emptySince < (waitingForReturn ? ResumeGrace : FallenGrace)) return false;
        Stop(Ending.Fallen);
        return true;
    }

    private static int PlayersAliveInside()
    {
        float radius = PluginConfig.Radius(WeatherKind.Crimson);
        int inside = 0;
        foreach (ZDO zdo in ZNet.instance.GetAllCharacterZDOS())
            if (!zdo.GetBool(ZDOVars.s_dead) && Utils.DistanceXZ(zdo.GetPosition(), _center) <= radius) inside++;
        return inside;
    }

    // Ends it: stops its raid, and tells everyone how it went. Survived: its time ran out, the obliterator gets the reward.
    private static void Stop(Ending ending)
    {
        _running = false;
        RandEventSystem raids = RandEventSystem.instance;
        if (raids && raids.m_randomEvent != null && Utils.DistanceXZ(raids.m_randomEvent.m_pos, _center) < SamePlace)
        {
            Plugin.ModLogger.LogInfo($"Crimson Rite: ended the raid {raids.m_randomEvent.m_name}.");
            raids.ResetRandomEvent();
        }
        if (ending != Ending.Survived && WeatherState.IsActive(WeatherKind.Crimson))
        {
            WeatherState.End(WeatherKind.Crimson);
            WeatherSync.Broadcast();
        }

        // The reward grows with the players alive inside as it is given (see PartyReward).
        int players = Mathf.Max(1, PlayersAliveInside());
        float multiplier = PartyReward.Multiplier(players, PluginConfig.CrimsonRewardPerPlayer.Value, PluginConfig.CrimsonMaxPlayers.Value);
        int reward = ending == Ending.Survived
            ? PartyReward.Scale(_kills * Mathf.Max(0, PluginConfig.CrimsonRewardPerKill.Value), multiplier, UnityEngine.Random.value)
            : 0;
        Plugin.ModLogger.LogInfo($"Crimson Rite ended ({ending}) with {_kills} kills, {players} players (x{multiplier:0.##})" +
            (reward > 0 ? $", reward {reward} {PluginConfig.CrimsonRewardItem.Value}." : "."));
        if (reward > 0)
        {
            if (_obliterator.IsNone() || ZDOMan.instance.GetZDO(_obliterator) == null) _obliterator = FindObliterator(_center);
            _rewardAt = _obliterator;
            _rewardAmount = reward;
            _rewardTries = 0;
            _nextReward = 0f;
            GiveRewardLater();
        }
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.CrimsonEnded, (int)ending, _kills, reward, _center);
        // What no player's game has (everyone left) is taken away here; the rest is cleared in the games that have it.
        IncursionArena.SweepOnServer(_center, PluginConfig.Radius(WeatherKind.Crimson), Messages.CrimsonName);
        _kills = 0;
        Counted.Clear();
    }

    // The game that owns the obliterator strikes the reward down in a chest beside it (see TreasureChests). Tried again
    // for a while when nobody owns it right now.
    private static void GiveRewardLater()
    {
        if (_rewardAmount <= 0 || Time.time < _nextReward) return;
        _nextReward = Time.time + RewardRetry;
        ZDO? zdo = _rewardAt.IsNone() ? null : ZDOMan.instance.GetZDO(_rewardAt);
        if (zdo == null)
        {
            Plugin.ModLogger.LogWarning($"Crimson Rite: no obliterator to put the reward ({_rewardAmount}) in.");
            _rewardAmount = 0;
            return;
        }
        long owner = zdo.GetOwner();
        if (owner == 0L)
        {
            if (++_rewardTries < RewardTries) return;
            Plugin.ModLogger.LogWarning($"Crimson Rite: nobody has the obliterator loaded; the reward ({_rewardAmount}) is lost.");
            _rewardAmount = 0;
            return;
        }
        TreasureChests.Send(zdo, "Crimson Rite", [("", [(PluginConfig.CrimsonRewardItem.Value.Trim(), _rewardAmount)])]);
        _rewardAmount = 0;
    }

    private static ZDOID FindObliterator(Vector3 at)
    {
        List<ZDO> found = [];
        ZDOMan.instance.FindObjects(ZoneSystem.GetZone(at), found, []);
        int prefab = ObliteratorPrefab.GetStableHashCode();
        foreach (ZDO zdo in found)
            if (zdo.GetPrefab() == prefab && Utils.DistanceXZ(zdo.GetPosition(), at) < SamePlace) return zdo.m_uid;
        return ZDOID.None;
    }

    // From the game that owns a creature that died within the Blood Grounds (see CrimsonArena.CreatureDied).
    private static void RPC_Kill(long sender, ZDOID creature, Vector3 position)
    {
        if (!IsServer() || !_running || !Active) return;
        if (Utils.DistanceXZ(position, _center) > PluginConfig.Radius(WeatherKind.Crimson) + SamePlace) return;
        if (!creature.IsNone() && !Counted.Add(creature)) return;
        _kills++;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.CrimsonKills, _kills, _center);
    }

    private static void Keep(RandEventSystem raids)
    {
        RandomEvent? current = raids.m_randomEvent;
        bool oursGoingOn = current != null && Utils.DistanceXZ(current.m_pos, _center) < SamePlace;
        if (oursGoingOn)
        {
            _nextStart = Time.time + PluginConfig.CrimsonPause.Value;
            if (Time.time >= _nextTell) TellCatchUp(current!.m_name);
            return;
        }
        // One going on elsewhere is replaced at once; after ours ends, the next comes after the pause.
        if (current == null && Time.time < _nextStart) return;
        _nextStart = Time.time + PluginConfig.CrimsonPause.Value;

        RandomEvent? raid = Pick(raids, _center, out int raidTier, out int partyTier);
        if (raid == null)
        {
            if (!_warned) Plugin.ModLogger.LogWarning("Crimson Rite: no raid the players at the obliterator are ready for.");
            _warned = true;
            return;
        }
        _warned = false;
        // Each raid after the first is announced by lightning on the obliterator, and the last one's creatures still
        // inside die (in the games that have them, see CrimsonArena; here those nobody's game has), so raids don't fight
        // each other.
        if (_lastRaid != null)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.CrimsonWave, _center);
            IncursionArena.SweepOnServer(_center, PluginConfig.Radius(WeatherKind.Crimson), Messages.CrimsonName);
        }
        _lastRaid = raid.m_name;
        Given[raid.m_name] = (Given.TryGetValue(raid.m_name, out int given) ? given : 0) + 1;
        _steps = RaidCatchUp.Steps(raidTier, partyTier);
        // Told first, so the players' games know it before the raid's first creatures spawn there.
        TellCatchUp(raid.m_name);
        Starting = true;
        try { raids.SetRandomEvent(raid, _center); }
        finally { Starting = false; }
        Plugin.ModLogger.LogInfo($"Crimson Rite: started the raid {raid.m_name} (tier {raidTier}, players' tier {partyTier}" +
            (_steps > 0 ? $", caught up {_steps} steps)." : ")."));
    }

    // Everyone's game (the raid's creatures spawn there): how many steps the raid is caught up (see CrimsonSpawnPatches).
    private static void TellCatchUp(string raid)
    {
        _nextTell = Time.time + TellInterval;
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.CrimsonCatchUp, raid, _steps, _center);
    }

    // The bosses' keys and what each drops (a raid may need a boss's trophy known), weakest first.
    private static void ReadBosses(List<string> keys, List<ICollection<string>> items)
    {
        foreach (BossRoster.Boss boss in BossRoster.Read(Messages.CrimsonName))
        {
            keys.Add(boss.Key);
            HashSet<string> drops = new(StringComparer.OrdinalIgnoreCase);
            GameObject? prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(boss.Prefab) : null;
            CharacterDrop? drop = prefab ? prefab!.GetComponent<CharacterDrop>() : null;
            if (drop && drop!.m_drops != null)
                foreach (CharacterDrop.Drop item in drop.m_drops)
                    if (item.m_prefab) drops.Add(item.m_prefab.name);
            items.Add(drops);
        }
    }

    private static int RaidTier(RandomEvent raid, List<string> bossKeys, List<ICollection<string>> bossItems)
    {
        List<string> keys = [.. raid.m_requiredGlobalKeys, .. raid.m_altRequiredPlayerKeysAny, .. raid.m_altRequiredPlayerKeysAll];
        List<string> items = [];
        foreach (ItemDrop item in raid.m_altRequiredKnownItems)
            if (item) items.Add(item.gameObject.name);
        return RaidCatchUp.RaidTier(keys, items, bossKeys, bossItems);
    }

    // The raids the players within the radius are ready for, as the game decides it for each player (world or player
    // keys, World Advancement Progression's private keys), leaving out only where they are and their base; the one that
    // came least so far comes (see RaidCatchUp.Choose). raidTier, partyTier: see RaidCatchUp.
    private static RandomEvent? Pick(RandEventSystem raids, Vector3 center, out int raidTier, out int partyTier)
    {
        raidTier = -1;
        partyTier = -1;
        RandEventSystem.RefreshPlayerEventData();
        bool byPlayer = ZoneSystem.instance.GetGlobalKey(GlobalKeys.PlayerEvents) || AdvancementProgression.PrivateRaids();
        float radius = PluginConfig.Radius(WeatherKind.Crimson);

        List<RandEventSystem.PlayerEventData> inside = RandEventSystem.s_playerEventDatas.FindAll(p => Utils.DistanceXZ(p.position, center) <= radius);
        // Nobody's data inside (it comes a little late): the game's own check for everyone.
        bool everyone = inside.Count == 0;
        if (everyone) inside = RandEventSystem.s_playerEventDatas;

        List<string> bossKeys = [];
        List<ICollection<string>> bossItems = [];
        ReadBosses(bossKeys, bossItems);

        List<RandomEvent> ready = [];
        List<int> tiers = [];
        List<string> names = [];
        List<bool> fitting = [];
        // Each player's tier: the strongest raid they are ready for (with no player's data, the world's).
        List<int> playerTiers = [];
        for (int i = 0; i < Mathf.Max(1, inside.Count); i++) playerTiers.Add(-1);
        List<RandEventSystem.PlayerEventData> one = new(1);
        foreach (RandomEvent raid in raids.m_events)
        {
            if (!raid.m_enabled || !raid.m_random) continue;
            int tier = RaidTier(raid, bossKeys, bossItems);
            bool readyForIt = false;
            for (int i = 0; i < inside.Count; i++)
            {
                RandEventSystem.PlayerEventData player = inside[i];
                one.Clear();
                one.Add(player);
                if (!raids.HaveGlobalKeys(raid, one)) continue;
                if (byPlayer && (player.possibleEvents == null || !player.possibleEvents.Contains(raid.m_name))) continue;
                readyForIt = true;
                playerTiers[i] = Mathf.Max(playerTiers[i], tier);
            }
            // Without any player's data, the world's keys alone (as raids go without player based raids).
            if (inside.Count == 0 && !byPlayer && raids.HaveGlobalKeys(raid, inside))
            {
                readyForIt = true;
                playerTiers[0] = Mathf.Max(playerTiers[0], tier);
            }
            if (!readyForIt) continue;
            ready.Add(raid);
            tiers.Add(tier);
            names.Add(raid.m_name);
            fitting.Add(raids.InValidBiome(raid, center));
        }
        int chosen = RaidCatchUp.Choose(names, fitting, Given, _lastRaid, n => UnityEngine.Random.Range(0, n));
        if (chosen < 0) return null;
        raidTier = tiers[chosen];
        partyTier = RaidCatchUp.PartyTier(playerTiers);
        return ready[chosen];
    }

    private static bool IsServer() => ZNet.instance && ZNet.instance.IsServer();
}

// World Advancement Progression (VentureValheim), when installed: with its private raids, raids follow each player's
// own keys, which it puts into the players' raid data.
internal static class AdvancementProgression
{
    private const string ConfigType = "VentureValheim.Progression.ProgressionConfiguration";

    private static bool _looked;
    private static object? _config;
    private static MethodInfo? _privateRaids;
    private static MethodInfo? _privateKeys;

    internal static bool PrivateRaids()
    {
        try
        {
            if (!_looked)
            {
                _looked = true;
                Type? type = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    if ((type = assembly.GetType(ConfigType, throwOnError: false)) != null) break;
                _config = type?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                _privateRaids = _config?.GetType().GetMethod("GetUsePrivateRaids", Type.EmptyTypes);
                _privateKeys = _config?.GetType().GetMethod("GetUsePrivateKeys", Type.EmptyTypes);
            }
            if (_config == null || _privateRaids == null || _privateKeys == null) return false;
            return (bool)_privateRaids.Invoke(_config, null) && (bool)_privateKeys.Invoke(_config, null);
        }
        catch (Exception)
        {
            _config = null;
            return false;
        }
    }

    private const string KeyManagerType = "VentureValheim.Progression.KeyManager";
    // Where it saves the local player's own keys on their character (lower case, separated by commas).
    private const string SavedKeys = "VV_PrivateKeys";

    private static bool _lookedKeys;
    private static PropertyInfo? _instance;
    private static PropertyInfo? _keysList;

    // Whether the local player has this key of their own: the game gives a boss's key only to the player whose game had
    // the boss, but World Advancement Progression gives it to everyone near. Its live keys, else those saved.
    internal static bool HasPrivateKey(Player player, string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        string lower = key.ToLowerInvariant();
        try
        {
            if (!_lookedKeys)
            {
                _lookedKeys = true;
                Type? type = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    if ((type = assembly.GetType(KeyManagerType, throwOnError: false)) != null) break;
                _instance = type?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                _keysList = type?.GetProperty("PrivateKeysList", BindingFlags.Public | BindingFlags.Instance);
            }
            object? manager = _instance?.GetValue(null);
            if (manager != null && _keysList?.GetValue(manager) is HashSet<string> keys && keys.Contains(lower)) return true;
        }
        catch (Exception)
        {
            _instance = null;
        }
        if (!player || !player.m_customData.TryGetValue(SavedKeys, out string saved)) return false;
        foreach (string part in saved.Split(','))
            if (part.Trim().ToLowerInvariant() == lower) return true;
        return false;
    }
}
