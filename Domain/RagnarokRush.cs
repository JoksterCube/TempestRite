using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The Ragnarök Rite on the server (and in single player or when hosting); players' games spawn the bosses, tell when
// they fall and show it all (see RagnarokArena, and IncursionArena for the bubble):
// 1. Asking: the players within the radius are asked which bosses they have slain (the key a boss gives as it dies, or
//    a kill of it in their character's stats: the game gives the key only to the player whose game had the boss).
//    The bosses every one of them has slain can come (with player based raids off, the world's keys count for all).
//    With none, it ends at once and everyone there is told why.
// 2. Bosses are picked, different ones while there are enough, weakest first, and rise one after another: one player
//    inside spawns each within the bubble, and the next rises a little after it falls.
// 3. When the last falls, it ends and the obliterator receives treasure (see RagnarokLoot), one roll for each boss.
// It breaks off with no reward when its time runs out, the obliterator is destroyed, everyone inside has fallen or
// left, or the Sanctuary or Equilibrium Rite ends it (only one Crimson or Ragnarök Rite goes on at a time, and only the
// Sanctuary Rite can be performed within its bubble, see IncursionArena.RiteRefused). However it ends, its bosses are
// taken away.
// Not kept through a server restart: a Ragnarök Rite still going on then simply ends.
internal static class RagnarokRush
{
    internal enum Ending
    {
        Survived = 0,
        Obliterator = 1,
        Fallen = 2,
        Ended = 3,
        TimeUp = 4,
        NoBosses = 5,
        NoOne = 6,
        NoSpawn = 7
    }

    private enum Phase
    {
        Asking = 0,
        Rising = 1,
        Fighting = 2,
        Resting = 3
    }

    private const string Feature = "Ragnarök Rite bosses";
    private const string ObliteratorPrefab = "incinerator";
    private const float CheckInterval = 1f;
    // How long nobody may be alive inside before it breaks off: player positions arrive a little late.
    private const float FallenGrace = 5f;
    // How long players have to answer which bosses they have slain.
    private const float AskWait = 4f;
    // How long a player has to say the boss is up before another is asked.
    private const float SpawnWait = 8f;
    private const int SpawnTries = 5;
    // How long a boss may be gone (without falling) before it is called again: its death may arrive a little late.
    private const float LostGrace = 5f;
    // The first boss rises a little after the bosses are named.
    private const float FirstRiseDelay = 4f;
    private const float RewardRetry = 5f;
    private const int RewardTries = 24;

    private static bool _running;
    // Performed in this session: one still going on when the server started again isn't picked up.
    private static bool _performed;
    private static Vector3 _center;
    private static ZDOID _obliterator = ZDOID.None;
    private static Phase _phase;
    private static float _phaseUntil;
    private static float _nextCheck;
    private static float _emptySince = -1f;

    // Asking: the players asked (by their game), and what each answered.
    private static readonly HashSet<long> Asked = [];
    private static readonly Dictionary<long, HashSet<string>> Answers = [];
    // Each boss in the Bosses setting with its key and name ("$enemy_eikthyr"), by its place there.
    private static readonly List<(string prefab, string key, string name)> Roster = [];

    // The bosses picked (their places in the Bosses setting), and which one is up.
    private static readonly List<int> Picked = [];
    private static int _current;
    private static ZDOID _boss = ZDOID.None;
    // Where it rose.
    private static Vector3 _bossAt;
    private static int _ticket;
    private static int _spawnTries;
    private static float _lostSince = -1f;

    // Treasure the obliterator's owner hasn't been reached for yet.
    private static ZDOID _rewardAt = ZDOID.None;
    private static readonly List<(string item, int amount)> Reward = [];
    private static int _rewardTries;
    private static float _nextReward;

    internal static bool Active => WeatherState.IsActive(WeatherKind.Ragnarok);

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<ZPackage>(Rpcs.RagnarokAnswer, RPC_Answer);
        rpc.Register<ZDOID, int, Vector3>(Rpcs.RagnarokSpawned, RPC_Spawned);
        rpc.Register<ZDOID>(Rpcs.RagnarokDown, RPC_Down);
    }

    internal static void ClearSession()
    {
        _running = false;
        _performed = false;
        _center = Vector3.zero;
        _obliterator = ZDOID.None;
        _phase = Phase.Asking;
        _phaseUntil = 0f;
        _nextCheck = 0f;
        _emptySince = -1f;
        Asked.Clear();
        Answers.Clear();
        Roster.Clear();
        Picked.Clear();
        _current = 0;
        _boss = ZDOID.None;
        _ticket = 0;
        _spawnTries = 0;
        _lostSince = -1f;
        _rewardAt = ZDOID.None;
        Reward.Clear();
        _rewardTries = 0;
    }

    // On the server, right after a rite was performed.
    internal static void Performed(WeatherKind kind)
    {
        if (!IsServer()) return;
        switch (kind)
        {
            case WeatherKind.Ragnarok:
                _performed = true;
                break;
            case WeatherKind.Sanctuary:
                if (_running) Stop(Ending.Ended);
                break;
            case WeatherKind.Reset:
                if (_running && !Active) Stop(Ending.Ended);
                break;
        }
    }

    // Every frame on the server.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || !IsServer() || ZDOMan.instance == null) return;
        try
        {
            GiveRewardLater();
            if (!Active)
            {
                // Its time ran out: the other ways it ends stop it themselves.
                if (_running) Stop(Ending.TimeUp);
                return;
            }
            if (!_running)
            {
                if (_performed) Begin();
                else Abandon();
                return;
            }
            if (Time.time >= _nextCheck)
            {
                _nextCheck = Time.time + CheckInterval;
                if (CheckBroken()) return;
            }
            Step();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Still going on from before the server started again: what it called is gone with the restart, so it ends.
    private static void Abandon()
    {
        _center = WeatherState.Position(WeatherKind.Ragnarok);
        Plugin.ModLogger.LogInfo("Ragnarök Rite: still going on from before the server started; it ends.");
        _running = true;
        Stop(Ending.Ended);
    }

    private static void Begin()
    {
        _running = true;
        _performed = false;
        _center = WeatherState.Position(WeatherKind.Ragnarok);
        _obliterator = FindObliterator(_center);
        _emptySince = -1f;
        _nextCheck = Time.time + CheckInterval;
        Picked.Clear();
        _current = 0;
        _boss = ZDOID.None;
        _lostSince = -1f;
        EndRaidHere();
        Ask();
    }

    // A raid going on that reaches the bubble ends: no raid comes while the bosses rise (see CrimsonPatch).
    private static void EndRaidHere()
    {
        RandEventSystem? raids = RandEventSystem.instance;
        RandomEvent? raid = raids ? raids!.m_randomEvent : null;
        if (raid == null || Utils.DistanceXZ(raid.m_pos, _center) > raid.m_eventRange + Radius) return;
        Plugin.ModLogger.LogInfo($"Ragnarök Rite: ended the raid {raid.m_name}.");
        raids!.ResetRandomEvent();
    }

    // ---- 1. Asking ----

    private static void Ask()
    {
        _phase = Phase.Asking;
        _phaseUntil = Time.time + AskWait;
        Asked.Clear();
        Answers.Clear();
        ReadRoster();
        ZPackage keys = new();
        keys.Write(Roster.Count);
        foreach ((string _, string key, string name) in Roster)
        {
            keys.Write(key);
            keys.Write(name);
        }
        // All of them first: the host's own player answers at once, before the others are asked.
        Asked.UnionWith(PlayersInside());
        if (Asked.Count == 0)
        {
            Stop(Ending.NoOne);
            return;
        }
        foreach (long player in new List<long>(Asked))
        {
            if (!_running || _phase != Phase.Asking) break;
            ZRoutedRpc.instance.InvokeRoutedRPC(player, Rpcs.RagnarokAsk, keys, _center);
        }
    }

    // The bosses of the Bosses setting the game has, with the key each gives as it dies.
    private static void ReadRoster()
    {
        Roster.Clear();
        foreach (string name in KeyItemLists.Parse(PluginConfig.RagnarokBosses.Value))
        {
            GameObject? prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(name) : null;
            Character? boss = prefab ? prefab!.GetComponent<Character>() : null;
            if (!boss)
            {
                Plugin.ModLogger.LogWarning($"Ragnarök Rite: boss \"{name}\" is not a creature the game knows. Check the Bosses setting.");
                continue;
            }
            if (string.IsNullOrEmpty(boss!.m_defeatSetGlobalKey))
            {
                Plugin.ModLogger.LogWarning($"Ragnarök Rite: boss \"{name}\" sets no key when it dies, so nobody can be known to have slain it. Check the Bosses setting.");
                continue;
            }
            Roster.Add((name, boss.m_defeatSetGlobalKey, boss.m_name));
        }
    }

    // From a player inside: the keys of those asked that they have.
    private static void RPC_Answer(long sender, ZPackage package)
    {
        if (!IsServer() || !_running || _phase != Phase.Asking || !Asked.Contains(sender)) return;
        HashSet<string> keys = [];
        int count = package.ReadInt();
        for (int i = 0; i < count; i++) keys.Add(package.ReadString());
        Answers[sender] = keys;
        if (Answers.Count >= Asked.Count) Pick();
    }

    // The bosses every player who answered has slain; players without the mod (who can't answer) are left out.
    private static void Pick()
    {
        bool byPlayer = ZoneSystem.instance && ZoneSystem.instance.GetGlobalKey(GlobalKeys.PlayerEvents);
        List<int> eligible = [];
        for (int place = 0; place < Roster.Count; place++)
        {
            string key = Roster[place].key;
            bool world = !byPlayer && ZoneSystem.instance && ZoneSystem.instance.GetGlobalKey(key);
            bool everyone = Answers.Count > 0;
            foreach (HashSet<string> keys in Answers.Values)
                if (!world && !keys.Contains(key)) everyone = false;
            if (everyone) eligible.Add(place);
        }
        if (Answers.Count < Asked.Count)
            Plugin.ModLogger.LogInfo($"Ragnarök Rite: {Asked.Count - Answers.Count} players inside didn't answer (no mod?); left out of the choice.");
        if (eligible.Count == 0)
        {
            Stop(Ending.NoBosses);
            return;
        }
        Picked.Clear();
        Picked.AddRange(RagnarokLoot.PickBosses(eligible, PluginConfig.RagnarokBossCount.Value, n => UnityEngine.Random.Range(0, n)));
        // Only how many: which bosses come is kept for when each rises.
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.RagnarokBegun, Picked.Count, _center);
        Plugin.ModLogger.LogInfo($"Ragnarök Rite: the bosses rise: {string.Join(", ", Picked.ConvertAll(p => Roster[p].prefab))}.");
        _current = 0;
        _spawnTries = 0;
        Rest(FirstRiseDelay);
    }

    // ---- 2. Rising and fighting ----

    private static void Step()
    {
        switch (_phase)
        {
            case Phase.Asking:
                if (Time.time >= _phaseUntil) Pick();
                break;
            case Phase.Resting:
                if (Time.time >= _phaseUntil) Rise();
                break;
            case Phase.Rising:
                if (Time.time < _phaseUntil) break;
                // Nobody said it is up: another player is asked.
                if (++_spawnTries >= SpawnTries) Stop(Ending.NoSpawn);
                else Rise();
                break;
            case Phase.Fighting:
                CheckLost();
                break;
        }
    }

    private static void Rest(float seconds)
    {
        _phase = Phase.Resting;
        _phaseUntil = Time.time + seconds;
    }

    // One player inside spawns the boss: a different one on each try.
    private static void Rise()
    {
        List<long> players = PlayersInside();
        if (players.Count == 0) return;
        long spawner = players[_spawnTries % players.Count];
        _phase = Phase.Rising;
        _phaseUntil = Time.time + SpawnWait;
        _ticket++;
        _boss = ZDOID.None;
        _lostSince = -1f;
        ZRoutedRpc.instance.InvokeRoutedRPC(spawner, Rpcs.RagnarokSpawn, Roster[Picked[_current]].prefab, _center, _ticket);
    }

    // From the player who spawned it.
    private static void RPC_Spawned(long sender, ZDOID boss, int ticket, Vector3 at)
    {
        if (!IsServer()) return;
        if (!_running || ticket != _ticket || _phase != Phase.Rising)
        {
            // Too late: another was called already, or it ended.
            TakeAway(boss);
            return;
        }
        _boss = boss;
        _bossAt = at;
        _phase = Phase.Fighting;
        _spawnTries = 0;
        Tell(fell: false);
    }

    // From the game that owned it, as it died.
    private static void RPC_Down(long sender, ZDOID boss)
    {
        if (!IsServer() || !_running || boss.IsNone() || boss != _boss) return;
        _boss = ZDOID.None;
        Tell(fell: true);
        _current++;
        if (_current >= Picked.Count)
        {
            Stop(Ending.Survived);
            return;
        }
        Rest(Mathf.Max(0f, PluginConfig.RagnarokPause.Value));
    }

    // The boss is gone without falling (taken away by something else): it is called again.
    private static void CheckLost()
    {
        if (_boss.IsNone() || ZDOMan.instance.GetZDO(_boss) != null)
        {
            _lostSince = -1f;
            return;
        }
        if (_lostSince < 0f) _lostSince = Time.time;
        if (Time.time - _lostSince < LostGrace) return;
        Plugin.ModLogger.LogWarning($"Ragnarök Rite: {Roster[Picked[_current]].prefab} is gone without falling; it rises again.");
        Rise();
    }

    private static void Tell(bool fell)
    {
        ZPackage package = new();
        package.Write(Roster[Picked[_current]].prefab);
        package.Write(_current + 1);
        package.Write(Picked.Count);
        package.Write(fell);
        package.Write(_center);
        package.Write(_bossAt);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.RagnarokBoss, package);
    }

    // ---- Ending ----

    // Whether it broke off: the obliterator is gone, or everyone inside has fallen or left.
    private static bool CheckBroken()
    {
        if (!_obliterator.IsNone() && ZDOMan.instance.GetZDO(_obliterator) == null)
        {
            Stop(Ending.Obliterator);
            return true;
        }
        if (PlayersInside().Count > 0)
        {
            _emptySince = -1f;
            return false;
        }
        if (_emptySince < 0f) _emptySince = Time.time;
        if (Time.time - _emptySince < FallenGrace) return false;
        Stop(Ending.Fallen);
        return true;
    }

    private static void Stop(Ending ending)
    {
        _running = false;
        if (WeatherState.IsActive(WeatherKind.Ragnarok))
        {
            WeatherState.End(WeatherKind.Ragnarok);
            WeatherSync.Broadcast();
        }
        Reward.Clear();
        if (ending == Ending.Survived) RollReward();
        Plugin.ModLogger.LogInfo($"Ragnarök Rite ended ({ending}) with {_current} of {Picked.Count} bosses slain" +
            (Reward.Count > 0 ? $", treasure {string.Join(", ", Reward.ConvertAll(r => $"{r.amount} {r.item}"))}." : "."));
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.RagnarokEnded, (int)ending, Reward.Count > 0, _center);
        // Its bosses are taken away wherever they are, and what no player's game has; the rest is cleared in the games
        // that have it.
        IncursionArena.SweepOnServer(_center, Radius, Messages.RagnarokName);
        _boss = ZDOID.None;
        Picked.Clear();
        _current = 0;
        if (Reward.Count > 0)
        {
            if (_obliterator.IsNone() || ZDOMan.instance.GetZDO(_obliterator) == null) _obliterator = FindObliterator(_center);
            _rewardAt = _obliterator;
            _rewardTries = 0;
            _nextReward = 0f;
            GiveRewardLater();
        }
    }

    // One roll of the treasure table for each boss slain, by its strength.
    private static void RollReward()
    {
        List<string> problems = [];
        List<RagnarokLoot.Treasure> table = RagnarokLoot.Parse(PluginConfig.RagnarokTreasure.Value, problems);
        foreach (string problem in problems)
            Plugin.ModLogger.LogWarning($"Ragnarök Rite: couldn't read \"{problem}\" in the Treasure setting: use Item: Min-Max.");
        List<float> strengths = Picked.ConvertAll(place => RagnarokLoot.Strength(place, Roster.Count));
        Reward.AddRange(RagnarokLoot.Roll(table, strengths, () => UnityEngine.Random.value));
    }

    // The game that owns the obliterator puts the treasure inside (see RagnarokArena.Reward). Tried again for a while
    // when nobody owns it right now.
    private static void GiveRewardLater()
    {
        if (Reward.Count == 0 || _running || Time.time < _nextReward) return;
        _nextReward = Time.time + RewardRetry;
        ZDO? zdo = _rewardAt.IsNone() ? null : ZDOMan.instance.GetZDO(_rewardAt);
        if (zdo == null)
        {
            Plugin.ModLogger.LogWarning("Ragnarök Rite: no obliterator to put the treasure in.");
            Reward.Clear();
            return;
        }
        long owner = zdo.GetOwner();
        if (owner == 0L)
        {
            if (++_rewardTries < RewardTries) return;
            Plugin.ModLogger.LogWarning("Ragnarök Rite: nobody has the obliterator loaded; the treasure is lost.");
            Reward.Clear();
            return;
        }
        ZPackage package = new();
        package.Write(Reward.Count);
        foreach ((string item, int amount) in Reward)
        {
            package.Write(item);
            package.Write(amount);
        }
        ZRoutedRpc.instance.InvokeRoutedRPC(owner, zdo.m_uid, Rpcs.RagnarokReward, package);
        Reward.Clear();
    }

    // ---- Helpers ----

    private static float Radius => PluginConfig.Radius(WeatherKind.Ragnarok);

    // The games of the players alive within the bubble (a player's character belongs to their game).
    private static List<long> PlayersInside()
    {
        List<long> players = [];
        float radius = Radius;
        foreach (ZDO zdo in ZNet.instance.GetAllCharacterZDOS())
        {
            if (zdo.GetBool(ZDOVars.s_dead) || Utils.DistanceXZ(zdo.GetPosition(), _center) > radius) continue;
            long owner = zdo.GetOwner();
            if (owner != 0L && !players.Contains(owner)) players.Add(owner);
        }
        return players;
    }

    private static void TakeAway(ZDOID boss)
    {
        ZDO? zdo = boss.IsNone() ? null : ZDOMan.instance.GetZDO(boss);
        if (zdo == null) return;
        zdo.SetOwner(ZDOMan.GetSessionID());
        ZDOMan.instance.DestroyZDO(zdo);
    }

    private static ZDOID FindObliterator(Vector3 at)
    {
        List<ZDO> found = [];
        ZDOMan.instance.FindObjects(ZoneSystem.GetZone(at), found, []);
        int prefab = ObliteratorPrefab.GetStableHashCode();
        foreach (ZDO zdo in found)
            if (zdo.GetPrefab() == prefab && Utils.DistanceXZ(zdo.GetPosition(), at) < IncursionArena.SamePlace) return zdo.m_uid;
        return ZDOID.None;
    }

    private static bool IsServer() => ZNet.instance && ZNet.instance.IsServer();
}
