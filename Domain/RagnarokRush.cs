using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The Ragnarök Rite on the server (and in single player or when hosting); players' games spawn the bosses, tell when
// they fall and show it all (see RagnarokArena, and IncursionArena for the Blood Grounds):
// 1. Asking: the players within the radius are asked which bosses they have slain (the key a boss gives as it dies, or
//    a kill of it in their character's stats: the game gives the key only to the player whose game had the boss).
//    The bosses every one of them has slain can come (with player based raids off, the world's keys count for all).
//    A player who has slain Bonemass (the last early boss) or a stronger one counts Eikthyr and the Elder as slain too;
//    if nobody inside has got that far, it ends at once and its key items go back into the obliterator (see
//    BossRoster.EarlyBosses). With no boss slain by everyone, it ends at once too. Either way everyone there is told why.
// 2. Bosses are picked, different ones while there are enough (those slain by only some of them
//    fill in before any comes again), in a random order. Each below the strongest boss everyone has slain is raised to it, with a
//    star for each place below it (see RagnarokPower): one player inside spawns it within the Blood Grounds, and the next rises a little after it falls. Their
//    drops but trophies are theirs to keep.
// 3. When the last falls, it ends and each boss slain gives a chest, struck down around the obliterator by the game
//    that owns it (see TreasureChests): the resources of the boss's own lands, more for each star it had (see
//    RagnarokLoot). No valuables. There is no time limit.
// A boss that keeps falling out of the Blood Grounds (through the ground) is put back, and if that doesn't hold, slain where the
// players can reach its drops (see ArenaRescue): it counts as slain.
// It breaks off with no reward when the obliterator is destroyed, everyone inside has fallen or
// left, or the Sanctuary Rite ends it (only one Crimson or Ragnarök Rite goes on at a time, and only the
// Sanctuary Rite can be performed within its Blood Grounds, see IncursionArena.RiteRefused). However it ends, the bosses still
// standing are taken away.
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
        NoSpawn = 7,
        // Nobody inside has slain the last early boss (Bonemass) or a stronger one: its key items are given back.
        TooEarly = 8
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
    // Each boss in the Bosses setting, by its place there.
    private static List<BossRoster.Boss> Roster = [];

    // The bosses picked (their places in the Bosses setting), and which one is up. The strongest
    // boss everyone has slain (its place) sets how strong they all are and the treasure.
    private static readonly List<int> Picked = [];
    private static int _strongest;
    private static int _current;
    // The boss up now (a set: one taken away is called again while its late death may still arrive).
    private static readonly HashSet<ZDOID> Bosses = [];
    // Where they rose.
    private static Vector3 _bossAt;
    private static int _ticket;
    // How many the last call to rise asked for.
    private static int _rising;
    private static bool _told;
    private static int _spawnTries;
    private static float _lostSince = -1f;

    // Treasure the obliterator's owner hasn't been reached for yet: each boss's chest.
    private static ZDOID _rewardAt = ZDOID.None;
    private static readonly List<(string boss, List<(string item, int amount)> loot)> Reward = [];
    private static int _rewardTries;
    private static float _nextReward;

    internal static bool Active => WeatherState.IsActive(WeatherKind.Ragnarok);

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<ZPackage>(Rpcs.RagnarokAnswer, RPC_Answer);
        rpc.Register<ZPackage>(Rpcs.RagnarokSpawned, RPC_Spawned);
        rpc.Register<ZDOID>(Rpcs.RagnarokDown, RPC_Down);
        rpc.Register<ZDOID>(Rpcs.RagnarokLost, RPC_Lost);
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
        _strongest = 0;
        _current = 0;
        Bosses.Clear();
        _rising = 0;
        _told = false;
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
        Bosses.Clear();
        _lostSince = -1f;
        EndRaidHere();
        Ask();
    }

    // A raid going on that reaches the Blood Grounds ends: no raid comes while the bosses rise (see CrimsonPatch).
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
        Roster = BossRoster.Read(Messages.RagnarokName);
        ZPackage keys = BossRoster.AskPackage(Roster);
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

    // From a player inside: the keys of those asked that they have.
    // The Hoard-Bane Rite asks the same (see HoardBaneRush.Answered).
    private static void RPC_Answer(long sender, ZPackage package)
    {
        if (!IsServer()) return;
        HashSet<string> keys = [];
        int count = package.ReadInt();
        for (int i = 0; i < count; i++) keys.Add(package.ReadString());
        if (HoardBaneRush.Answered(sender, keys)) return;
        if (!_running || _phase != Phase.Asking || !Asked.Contains(sender)) return;
        Answers[sender] = keys;
        if (Answers.Count >= Asked.Count) Pick();
    }

    // The bosses every player who answered has slain; players without the mod (who can't answer) are left out.
    private static void Pick()
    {
        List<int> eligible = [];
        // Slain by some but not all: they fill in before any boss comes again.
        List<int> spare = [];
        if (Answers.Count < Asked.Count)
            Plugin.ModLogger.LogInfo($"Ragnarök Rite: {Asked.Count - Answers.Count} players inside didn't answer (no mod?); left out of the choice.");
        if (Roster.Count > 0 && !FillEarly())
        {
            bool givenBack = IncursionPending.GiveBack(WeatherKind.Ragnarok, _center);
            Plugin.ModLogger.LogInfo($"Ragnarök Rite: nobody inside has slain {Roster[Math.Min(BossRoster.EarlyBosses, Roster.Count) - 1].Prefab} " +
                $"or a stronger boss{(givenBack ? "" : " (no key items to give back)")}.");
            Stop(Ending.TooEarly);
            return;
        }
        BossRoster.Slain(Roster, Answers.Values, eligible, spare);
        if (eligible.Count == 0)
        {
            Stop(Ending.NoBosses);
            return;
        }
        _strongest = eligible[eligible.Count - 1];
        Picked.Clear();
        Picked.AddRange(RagnarokLoot.PickBosses(eligible, spare, PluginConfig.RagnarokBossCount.Value, n => UnityEngine.Random.Range(0, n)));
        // Only how many: which bosses come is kept for when each rises.
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.RagnarokBegun, Picked.Count, _center);
        Plugin.ModLogger.LogInfo($"Ragnarök Rite: as strong as {Roster[_strongest].Prefab}, the bosses rise: " +
            $"{string.Join(", ", Picked.ConvertAll(p => Roster[p].Prefab))}.");
        _current = 0;
        _spawnTries = 0;
        Rest(FirstRiseDelay);
    }

    // Each player who got past the early bosses counts them all as slain (the world's keys too, when they count for
    // everyone). Whether anyone inside got that far.
    private static bool FillEarly()
    {
        List<string> keys = Roster.ConvertAll(boss => boss.Key);
        HashSet<string> world = BossRoster.WorldSlain(Roster);
        bool past = BossRoster.PastEarly(keys, world);
        foreach (HashSet<string> slain in Answers.Values)
        {
            slain.UnionWith(world);
            BossRoster.FillEarly(keys, slain);
            past |= BossRoster.PastEarly(keys, slain);
        }
        return past;
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
                if (Time.time >= _phaseUntil) BeginNext();
                break;
            case Phase.Rising:
                if (Time.time < _phaseUntil) break;
                // Nobody said they are up: another player is asked.
                if (++_spawnTries >= SpawnTries) Stop(Ending.NoSpawn);
                else Rise(_rising);
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

    private static void BeginNext()
    {
        Bosses.Clear();
        _told = false;
        Rise(1);
    }

    // One player inside spawns the bosses: a different one on each try.
    private static void Rise(int count)
    {
        List<long> players = PlayersInside();
        if (players.Count == 0) return;
        long spawner = players[_spawnTries % players.Count];
        int place = Picked[_current];
        BossRoster.Boss boss = Roster[place];
        int stars = RagnarokPower.Stars(place, _strongest);
        BossRoster.Boss raised = Roster[RagnarokPower.RaisedTo(place, _strongest)];
        float healthBy = RagnarokPower.HealthMultiplier(boss.Health, boss.Taken, raised.Health, raised.Taken) *
            PluginConfig.RagnarokHealthMultiplier.Value;
        float damageBy = RagnarokPower.DamageMultiplier(boss.Damage, raised.Damage) * PluginConfig.RagnarokDamageMultiplier.Value;
        _phase = Phase.Rising;
        _phaseUntil = Time.time + SpawnWait;
        _ticket++;
        _rising = count;
        _lostSince = -1f;
        ZPackage package = new();
        package.Write(Roster[place].Prefab);
        package.Write(_center);
        package.Write(_ticket);
        package.Write(count);
        package.Write(healthBy);
        package.Write(damageBy);
        // A star for each place below the strongest: after Bonemass, Eikthyr has two (see RagnarokPower).
        package.Write(stars);
        ZRoutedRpc.instance.InvokeRoutedRPC(spawner, Rpcs.RagnarokSpawn, package);
    }

    // From the player who spawned them.
    private static void RPC_Spawned(long sender, ZPackage package)
    {
        if (!IsServer()) return;
        int ticket = package.ReadInt();
        Vector3 at = package.ReadVector3();
        int count = package.ReadInt();
        List<ZDOID> risen = [];
        for (int i = 0; i < count; i++) risen.Add(package.ReadZDOID());
        if (!_running || ticket != _ticket || _phase != Phase.Rising)
        {
            // Too late: others were called already, or it ended.
            foreach (ZDOID boss in risen) TakeAway(boss);
            return;
        }
        Bosses.UnionWith(risen);
        _bossAt = at;
        _phase = Phase.Fighting;
        _spawnTries = 0;
        if (_told) return;
        _told = true;
        Tell(fell: false);
    }

    // From the game that owned it, as it died. The next rises after it.
    private static void RPC_Down(long sender, ZDOID boss)
    {
        if (!IsServer() || !_running || boss.IsNone() || !Bosses.Remove(boss)) return;
        if (Bosses.Count > 0 || _phase != Phase.Fighting) return;
        Tell(fell: true);
        _current++;
        if (_current >= Picked.Count)
        {
            Stop(Ending.Survived);
            return;
        }
        Rest(Mathf.Max(0f, PluginConfig.RagnarokPause.Value));
    }

    // From the game that owns it: it kept falling out of the Blood Grounds (through the ground) and couldn't even be slain there
    // (see ArenaRescue). Not the players' fault: it counts as slain, and the next rises.
    private static void RPC_Lost(long sender, ZDOID boss)
    {
        if (!IsServer() || !_running || boss.IsNone() || !Bosses.Contains(boss)) return;
        Plugin.ModLogger.LogWarning($"Ragnarök Rite: {Roster[Picked[_current]].Prefab} was lost out of the Blood Grounds; it counts as slain.");
        RPC_Down(sender, boss);
        TakeAway(boss);
    }

    // Bosses gone without falling (taken away by something else) are called again.
    private static void CheckLost()
    {
        List<ZDOID> lost = [];
        foreach (ZDOID boss in Bosses)
            if (ZDOMan.instance.GetZDO(boss) == null) lost.Add(boss);
        if (lost.Count == 0)
        {
            _lostSince = -1f;
            return;
        }
        if (_lostSince < 0f) _lostSince = Time.time;
        if (Time.time - _lostSince < LostGrace) return;
        Plugin.ModLogger.LogWarning($"Ragnarök Rite: {lost.Count} {Roster[Picked[_current]].Prefab} gone without falling; they rise again.");
        foreach (ZDOID boss in lost) Bosses.Remove(boss);
        Rise(lost.Count);
    }

    private static void Tell(bool fell)
    {
        ZPackage package = new();
        package.Write(Roster[Picked[_current]].Prefab);
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
            (Reward.Count > 0 ? $", treasure chests: {string.Join(" | ", Reward.ConvertAll(chest => $"{chest.boss}: " + string.Join(", ", chest.loot.ConvertAll(r => $"{r.amount} {r.item}"))))}." : "."));
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.RagnarokEnded, (int)ending, Reward.Count > 0, _center);
        // Its bosses are taken away wherever they are, and what no player's game has; the rest is cleared in the games
        // that have it.
        IncursionArena.SweepOnServer(_center, Radius, Messages.RagnarokName);
        Bosses.Clear();
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

    // For each boss slain, a chest: its own loot (a few common, uncommon, rare and legendary items, more for each star it
    // had), more with more players.
    private static void RollReward()
    {
        List<string> problems = [];
        float party = PartyMultiplier();
        foreach (int place in Picked)
        {
            BossRoster.Boss boss = Roster[place];
            List<RagnarokLoot.Tier> tiers = [];
            foreach (string tier in PluginConfig.RagnarokBossLootOf(boss.Prefab))
            {
                problems.Clear();
                tiers.Add(RagnarokLoot.ParseTier(tier, problems));
                foreach (string problem in problems)
                    Plugin.ModLogger.LogWarning($"Ragnarök Rite: couldn't read \"{problem}\" in {boss.Prefab}'s Loot settings: use Item: Min-Max.");
            }
            float byStars = RagnarokLoot.StarMultiplier(RagnarokPower.Stars(place, _strongest), PluginConfig.RagnarokLootPerStar.Value);
            // Its loot grows with its stars and the players at once (half items half as much).
            List<(string item, int amount)> chest = RagnarokLoot.BossLoot(tiers, byStars * party, () => UnityEngine.Random.value);
            if (chest.Count > 0) Reward.Add((boss.Prefab, chest));
        }
    }

    // More players, more treasure: the multiplier for the players alive inside as it is won.
    private static float PartyMultiplier()
    {
        int players = Math.Max(1, PlayersInside().Count);
        float multiplier = PartyReward.Multiplier(players, PluginConfig.RagnarokRewardPerPlayer.Value, PluginConfig.RagnarokMaxPlayers.Value);
        Plugin.ModLogger.LogInfo($"Ragnarök Rite: {players} players inside, treasure x{multiplier:0.##}.");
        return multiplier;
    }

    // The game that owns the obliterator strikes the chests down around it (see TreasureChests). Tried again for a while
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
        TreasureChests.Send(zdo, "Ragnarök Rite", [.. Reward]);
        Reward.Clear();
    }

    // ---- Helpers ----

    private static float Radius => PluginConfig.Radius(WeatherKind.Ragnarok);

    // The games of the players alive within the Blood Grounds (a player's character belongs to their game).
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
