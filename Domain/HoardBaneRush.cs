using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The Hoard-Bane Rite on the server (and in single player or when hosting); players' games spawn the fight, tell when
// its creatures fall and show it all (see HoardBaneArena, and IncursionArena for the bubble):
// 1. The chest burnt picks the fight (see HoardBaneLoot): the player who burnt it says which, right before asking for
//    the rite. The players inside are asked which bosses they have slain, as for the Ragnarök Rite: the strongest boss
//    everyone has slain sets how strong the miniboss is and the treasure.
// 2. One player inside spawns it all: three dormant elites in bubbles evenly around the edge, and the miniboss (or both
//    brothers) on the edge between them with its followers, shielded.
//    Every 30 seconds after, while fewer than two followers stand, one more comes in where it is furthest from the
//    players (see HoardBaneLoot.FarthestFrom), spawned by the player nearest that place.
// 3. Each elite falling strikes the miniboss with lightning; when the last falls, its shield breaks (told again and
//    again, so a game taking the miniboss over later knows). When every miniboss falls, it ends and the obliterator
//    receives treasure. There is no time limit.
// A miniboss or elite that falls out of the bubble and can't be put back (see ArenaRescue), or is gone without falling:
// an elite counts as slain; a miniboss too once its shield is down, else it rises again, whole and shielded.
// It breaks off with no reward when the obliterator is destroyed, everyone inside has fallen or left, or the Sanctuary
// Rite ends it; the chest burnt isn't lost then: it goes back into the obliterator (or, with none left, to
// the player who burnt it). However it ends, what it brought is taken away. Not kept through a server restart.
internal static class HoardBaneRush
{
    internal enum Ending
    {
        Survived = 0,
        Obliterator = 1,
        Fallen = 2,
        Ended = 3,
        TimeUp = 4,
        NoOne = 5,
        NoSpawn = 6
    }

    internal enum News
    {
        EliteFell = 0,
        ShieldBroke = 1,
        BossFell = 2,
        BossReturns = 3
    }

    private enum Phase
    {
        Asking = 0,
        Rising = 1,
        Fighting = 2
    }

    private const string Feature = "Hoard-Bane Rite";
    private const string ObliteratorPrefab = "incinerator";
    private const float CheckInterval = 1f;
    private const float FallenGrace = 5f;
    private const float AskWait = 4f;
    private const float SpawnWait = 8f;
    private const int SpawnTries = 5;
    // How long one of its creatures may be gone (without falling) before it counts as lost: its death may come late.
    private const float LostGrace = 5f;
    private const float UnshieldInterval = 3f;
    private const float RewardRetry = 5f;
    private const int RewardTries = 24;

    private static bool _running;
    private static bool _performed;
    private static Vector3 _center;
    private static ZDOID _obliterator = ZDOID.None;
    private static Phase _phase;
    private static float _phaseUntil;
    private static float _nextCheck;
    private static float _emptySince = -1f;

    // The chest the last player to ask for it burnt, where, and who burnt it.
    private static string? _chest;
    private static Vector3 _chestAt;
    private static long _chestBy;
    // The chest burnt for the fight going on, given back if it fails (null: none known).
    private static string? _burnt;
    private static long _burntBy;

    private static readonly HashSet<long> Asked = [];
    private static readonly Dictionary<long, HashSet<string>> Answers = [];
    private static List<BossRoster.Boss> Roster = [];

    private static int _encounter;
    // The strongest boss everyone inside has slain (its place in the roster), or -1.
    private static int _strongest = -1;
    // Where the first elite's bubble is, in degrees around the obliterator.
    private static float _angle;
    // Each miniboss's health and damage multipliers, by its place in the fight.
    private static readonly List<(float health, float damage)> Power = [];

    // The elites standing (with their prefab), and the minibosses standing (with their place in the fight).
    private static readonly Dictionary<ZDOID, string> Elites = [];
    private static readonly Dictionary<ZDOID, int> Bosses = [];
    private static readonly Dictionary<ZDOID, float> GoneSince = [];
    // The followers standing, when to check them next, and the last call for one more.
    private static readonly List<ZDOID> Followers = [];
    private static float _nextReinforce;
    private static int _reinforceTicket;
    private static int _elitesTotal;
    private static int _elitesSlain;
    private static bool _shieldBroken;
    private static float _nextUnshield;

    // What the last call to rise asked for.
    private static int _ticket;
    private static int _spawnTries;
    private static bool _risingAll;
    private static readonly List<int> RisingBosses = [];

    private static ZDOID _rewardAt = ZDOID.None;
    private static readonly List<(string item, int amount)> Reward = [];
    private static int _rewardTries;
    private static float _nextReward;

    internal static bool Active => WeatherState.IsActive(WeatherKind.HoardBane);

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<string, Vector3>(Rpcs.HoardBaneChest, RPC_Chest);
        rpc.Register<ZPackage>(Rpcs.HoardBaneSpawned, RPC_Spawned);
        rpc.Register<ZPackage>(Rpcs.HoardBaneReinforced, RPC_Reinforced);
        rpc.Register<ZDOID>(Rpcs.HoardBaneDown, RPC_Down);
        rpc.Register<ZDOID>(Rpcs.HoardBaneLost, RPC_Lost);
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
        _chest = null;
        _burnt = null;
        Asked.Clear();
        Answers.Clear();
        Roster.Clear();
        Power.Clear();
        _strongest = -1;
        ClearFight();
        _rewardAt = ZDOID.None;
        Reward.Clear();
        _rewardTries = 0;
    }

    private static void ClearFight()
    {
        Elites.Clear();
        Bosses.Clear();
        GoneSince.Clear();
        Followers.Clear();
        _nextReinforce = 0f;
        _elitesTotal = 0;
        _elitesSlain = 0;
        _shieldBroken = false;
        _ticket = 0;
        _spawnTries = 0;
        _risingAll = false;
        RisingBosses.Clear();
    }

    // In the game of the player who burnt the chest, right before asking for the rite.
    internal static void SendChest(string chest, Vector3 at)
    {
        if (ZRoutedRpc.instance == null) return;
        if (IsServer()) SetChest(chest, at, ZRoutedRpc.instance.m_id);
        else ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), Rpcs.HoardBaneChest, chest, at);
    }

    // Only one of the rite's key items is taken: a failed fight gives the chest back to whoever sent it.
    private static void RPC_Chest(long sender, string chest, Vector3 at)
    {
        if (!IsServer()) return;
        foreach (string item in ObliteratorKeys.Items(WeatherKind.HoardBane))
            if (string.Equals(item, chest, StringComparison.OrdinalIgnoreCase))
            {
                SetChest(chest, at, sender);
                return;
            }
        Plugin.ModLogger.LogWarning($"Hoard-Bane Rite: ignored a chest that is not one of its key items ({chest}).");
    }

    private static void SetChest(string chest, Vector3 at, long by)
    {
        _chest = chest;
        _chestAt = at;
        _chestBy = by;
    }

    // On the server, right after a rite was performed.
    internal static void Performed(WeatherKind kind)
    {
        if (!IsServer()) return;
        switch (kind)
        {
            case WeatherKind.HoardBane:
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
                CheckGone();
                if (!_running) return;
            }
            Step();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Still going on from before the server started again: what it called is gone with the restart, so it ends.
    private static void Abandon()
    {
        _center = WeatherState.Position(WeatherKind.HoardBane);
        Plugin.ModLogger.LogInfo("Hoard-Bane Rite: still going on from before the server started; it ends.");
        _running = true;
        Stop(Ending.Ended);
    }

    private static void Begin()
    {
        _running = true;
        _performed = false;
        _center = WeatherState.Position(WeatherKind.HoardBane);
        _obliterator = FindObliterator(_center);
        _emptySince = -1f;
        _nextCheck = Time.time + CheckInterval;
        ClearFight();
        string? chest = _chest != null && Utils.DistanceXZ(_chestAt, _center) < IncursionArena.SamePlace ? _chest : null;
        _chest = null;
        _burnt = chest;
        _burntBy = _chestBy;
        _encounter = HoardBaneLoot.EncounterFor(chest, ObliteratorKeys.Items(WeatherKind.HoardBane), n => UnityEngine.Random.Range(0, n));
        _angle = UnityEngine.Random.Range(0f, 360f);
        EndRaidHere();
        Ask();
    }

    private static HoardBaneLoot.Encounter Fight => HoardBaneLoot.Encounters[_encounter];

    // A raid going on that reaches the bubble ends: no raid comes while it goes on (see CrimsonPatch).
    private static void EndRaidHere()
    {
        RandEventSystem? raids = RandEventSystem.instance;
        RandomEvent? raid = raids ? raids!.m_randomEvent : null;
        if (raid == null || Utils.DistanceXZ(raid.m_pos, _center) > raid.m_eventRange + Radius) return;
        Plugin.ModLogger.LogInfo($"Hoard-Bane Rite: ended the raid {raid.m_name}.");
        raids!.ResetRandomEvent();
    }

    // ---- 1. Asking ----

    private static void Ask()
    {
        _phase = Phase.Asking;
        _phaseUntil = Time.time + AskWait;
        Asked.Clear();
        Answers.Clear();
        Roster = BossRoster.Read(Messages.HoardBaneName);
        ZPackage keys = BossRoster.AskPackage(Roster);
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

    // A player's answer to the Ragnarök Rite's question (see RagnarokRush.RPC_Answer): whether it was for this rite.
    internal static bool Answered(long sender, HashSet<string> keys)
    {
        if (!_running || _phase != Phase.Asking || !Asked.Contains(sender)) return false;
        Answers[sender] = keys;
        if (Answers.Count >= Asked.Count) Pick();
        return true;
    }

    // How strong the minibosses are, then up they come.
    private static void Pick()
    {
        List<int> everyone = [], some = [];
        BossRoster.Slain(Roster, Answers.Values, everyone, some);
        _strongest = everyone.Count > 0 ? everyone[everyone.Count - 1] : -1;
        if (Answers.Count < Asked.Count)
            Plugin.ModLogger.LogInfo($"Hoard-Bane Rite: {Asked.Count - Answers.Count} players inside didn't answer (no mod?); left out.");
        Power.Clear();
        foreach (string prefab in Fight.Bosses) Power.Add(Strength(prefab));
        ZPackage begun = new();
        begun.Write(Fight.Bosses.Length);
        foreach (string boss in Fight.Bosses) begun.Write(boss);
        begun.Write(Fight.Elites.Length);
        begun.Write(_center);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.HoardBaneBegun, begun);
        Plugin.ModLogger.LogInfo($"Hoard-Bane Rite: {string.Join(" & ", Fight.Bosses)} comes for {Fight.Chest}, as strong as " +
            (_strongest < 0 ? "no boss slain" : Roster[_strongest].Prefab) + ": " +
            string.Join(", ", Power.ConvertAll(p => $"health x{p.health:0.##}, damage x{p.damage:0.##}")) + ".");
        _spawnTries = 0;
        Rise(all: true, []);
    }

    private static (float health, float damage) Strength(string prefab)
    {
        GameObject? found = ZNetScene.instance ? ZNetScene.instance.GetPrefab(prefab) : null;
        Character? character = found ? found!.GetComponent<Character>() : null;
        if (!character || _strongest < 0) return (HoardBaneLoot.LeastHealth, HoardBaneLoot.LeastDamage);
        BossRoster.Boss strongest = Roster[_strongest];
        float health = HoardBaneLoot.HealthMultiplier(character!.m_health, BossRoster.TakenBy(character), strongest.Health,
            strongest.Taken, PluginConfig.HoardBaneHealthShare.Value, Fight.Bosses.Length);
        float damage = HoardBaneLoot.DamageMultiplier(RagnarokPower.Damage(prefab, BossRoster.ReadDamage(found!)), strongest.Damage,
            PluginConfig.HoardBaneDamageShare.Value);
        return (health, damage);
    }

    // ---- 2. Rising ----

    private static void Step()
    {
        switch (_phase)
        {
            case Phase.Asking:
                if (Time.time >= _phaseUntil) Pick();
                break;
            case Phase.Rising:
                if (Time.time < _phaseUntil) break;
                if (++_spawnTries >= SpawnTries) Stop(Ending.NoSpawn);
                else Rise(_risingAll, [.. RisingBosses]);
                break;
            case Phase.Fighting:
                if (_shieldBroken && Time.time >= _nextUnshield) Unshield();
                if (_nextReinforce > 0f && Time.time >= _nextReinforce)
                {
                    _nextReinforce += HoardBaneLoot.ReinforceInterval;
                    if (_nextReinforce <= Time.time) _nextReinforce = Time.time + HoardBaneLoot.ReinforceInterval;
                    Reinforce();
                }
                break;
        }
    }

    // One player inside spawns it: everything (all), or only these minibosses again. A different player on each try.
    private static void Rise(bool all, List<int> bosses)
    {
        List<long> players = PlayersInside();
        if (players.Count == 0) return;
        long spawner = players[_spawnTries % players.Count];
        if (all)
        {
            bosses = [];
            for (int i = 0; i < Fight.Bosses.Length; i++) bosses.Add(i);
        }
        _phase = Phase.Rising;
        _phaseUntil = Time.time + SpawnWait;
        _ticket++;
        _risingAll = all;
        RisingBosses.Clear();
        RisingBosses.AddRange(bosses);
        ZPackage package = new();
        package.Write(_ticket);
        package.Write(_encounter);
        package.Write(_center);
        package.Write(_angle);
        package.Write(all);
        package.Write(bosses.Count);
        foreach (int boss in bosses)
        {
            package.Write(boss);
            package.Write(Power[boss].health);
            package.Write(Power[boss].damage);
        }
        ZRoutedRpc.instance.InvokeRoutedRPC(spawner, Rpcs.HoardBaneSpawn, package);
    }

    // From the player who spawned them.
    private static void RPC_Spawned(long sender, ZPackage package)
    {
        if (!IsServer()) return;
        int ticket = package.ReadInt();
        List<(ZDOID id, string prefab)> elites = [];
        int count = package.ReadInt();
        for (int i = 0; i < count; i++) elites.Add((package.ReadZDOID(), package.ReadString()));
        List<(ZDOID id, int place)> bosses = [];
        count = package.ReadInt();
        for (int i = 0; i < count; i++) bosses.Add((package.ReadZDOID(), package.ReadInt()));
        List<ZDOID> guards = [];
        count = package.ReadInt();
        for (int i = 0; i < count; i++) guards.Add(package.ReadZDOID());
        List<ZDOID> followers = [];
        count = package.ReadInt();
        for (int i = 0; i < count; i++) followers.Add(package.ReadZDOID());
        if (!_running || ticket != _ticket || _phase != Phase.Rising)
        {
            // Too late: others were called already, or it ended.
            foreach ((ZDOID id, string _) in elites) TakeAway(id);
            foreach ((ZDOID id, int _) in bosses) TakeAway(id);
            foreach (ZDOID id in guards) TakeAway(id);
            foreach (ZDOID id in followers) TakeAway(id);
            return;
        }
        if (bosses.Count == 0)
        {
            // Nothing to fight for: a miniboss the game doesn't have.
            foreach ((ZDOID id, string _) in elites) TakeAway(id);
            foreach (ZDOID id in guards) TakeAway(id);
            foreach (ZDOID id in followers) TakeAway(id);
            Stop(Ending.NoSpawn);
            return;
        }
        foreach ((ZDOID id, string prefab) in elites) Elites[id] = prefab;
        foreach ((ZDOID id, int place) in bosses) Bosses[id] = place;
        _phase = Phase.Fighting;
        _spawnTries = 0;
        if (!_risingAll) return;
        Followers.AddRange(followers);
        _nextReinforce = Time.time + HoardBaneLoot.ReinforceInterval;
        _elitesTotal = elites.Count;
        // No elite could rise (a modded game): no shield to break.
        if (_elitesTotal == 0) BreakShield();
    }

    // ---- 3. Fighting ----

    // From the game that owned it, as it died.
    private static void RPC_Down(long sender, ZDOID id)
    {
        if (!IsServer() || !_running || id.IsNone()) return;
        if (Elites.TryGetValue(id, out string elite))
        {
            Elites.Remove(id);
            EliteFell(elite);
        }
        else if (Bosses.TryGetValue(id, out int place)) BossFell(id, place);
    }

    // From the game that owns it: it fell out of the bubble and couldn't be put back (see ArenaRescue).
    private static void RPC_Lost(long sender, ZDOID id)
    {
        if (!IsServer() || !_running || id.IsNone()) return;
        Lost(id);
    }

    private static void Lost(ZDOID id)
    {
        GoneSince.Remove(id);
        if (Elites.TryGetValue(id, out string elite))
        {
            Plugin.ModLogger.LogWarning($"Hoard-Bane Rite: elite {elite} was lost; it counts as slain.");
            Elites.Remove(id);
            TakeAway(id);
            EliteFell(elite);
            return;
        }
        if (!Bosses.TryGetValue(id, out int place)) return;
        TakeAway(id);
        if (_shieldBroken)
        {
            Plugin.ModLogger.LogWarning($"Hoard-Bane Rite: {Fight.Bosses[place]} was lost with its shield down; it counts as slain.");
            BossFell(id, place);
            return;
        }
        Plugin.ModLogger.LogWarning($"Hoard-Bane Rite: {Fight.Bosses[place]} was lost with elites still standing; it rises again.");
        Bosses.Remove(id);
        Tell(News.BossReturns, Fight.Bosses[place], 0, 0);
        _spawnTries = 0;
        // Others rising already (one lost while it rose) come along.
        List<int> again = _phase == Phase.Rising ? [.. RisingBosses] : [];
        if (!again.Contains(place)) again.Add(place);
        Rise(_phase == Phase.Rising && _risingAll, again);
    }

    private static void EliteFell(string prefab)
    {
        _elitesSlain++;
        Tell(News.EliteFell, prefab, _elitesSlain, _elitesTotal);
        if (_elitesSlain >= _elitesTotal && !_shieldBroken) BreakShield();
    }

    private static void BreakShield()
    {
        _shieldBroken = true;
        Unshield();
        Tell(News.ShieldBroke, string.Empty, 0, 0);
    }

    // Every game takes the shield off the minibosses it owns (see HoardBaneArena).
    private static void Unshield()
    {
        _nextUnshield = Time.time + UnshieldInterval;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.HoardBaneUnshield, _center);
    }

    private static void BossFell(ZDOID id, int place)
    {
        Bosses.Remove(id);
        GoneSince.Remove(id);
        Tell(News.BossFell, Fight.Bosses[place], 0, 0);
        if (Bosses.Count == 0 && _phase != Phase.Rising) Stop(Ending.Survived);
    }

    // Elites and minibosses gone without falling (taken away by something else) count as lost.
    private static void CheckGone()
    {
        List<ZDOID> lost = [];
        foreach (ZDOID id in new List<ZDOID>([.. Elites.Keys, .. Bosses.Keys]))
        {
            if (ZDOMan.instance.GetZDO(id) != null)
            {
                GoneSince.Remove(id);
                continue;
            }
            if (!GoneSince.TryGetValue(id, out float since)) GoneSince[id] = since = Time.time;
            if (Time.time - since >= LostGrace) lost.Add(id);
        }
        foreach (ZDOID id in lost)
        {
            if (!_running) return;
            Lost(id);
        }
    }

    // News for everyone: lightning strikes the minibosses as an elite falls.
    private static void Tell(News news, string prefab, int which, int of)
    {
        ZPackage package = new();
        package.Write((int)news);
        package.Write(prefab);
        package.Write(which);
        package.Write(of);
        package.Write(_center);
        List<Vector3> strikes = [];
        if (news == News.EliteFell)
            foreach (ZDOID boss in Bosses.Keys)
                if (ZDOMan.instance.GetZDO(boss) is { } zdo) strikes.Add(zdo.GetPosition());
        package.Write(strikes.Count);
        foreach (Vector3 at in strikes) package.Write(at);
        package.Write(Fight.Bosses.Length);
        foreach (string boss in Fight.Bosses) package.Write(boss);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.HoardBaneNews, package);
    }

    // ---- Followers ----

    // On each check: while fewer than ReinforceBelow followers stand, one more, furthest from the players inside.
    private static void Reinforce()
    {
        Followers.RemoveAll(id => ZDOMan.instance.GetZDO(id) is not { } zdo || zdo.GetBool(ZDOVars.s_dead));
        if (Followers.Count >= HoardBaneLoot.ReinforceBelow) return;
        List<(long owner, Vector3 at)> players = PlayersInsideAt();
        if (players.Count == 0) return;
        List<Vector2> offsets = players.ConvertAll(p => new Vector2(p.at.x - _center.x, p.at.z - _center.z));
        Vector2 far = HoardBaneLoot.FarthestFrom(offsets, Mathf.Max(0f, Radius - HoardBaneLoot.FollowerClearOfEdge));
        Vector3 spot = _center + new Vector3(far.x, 0f, far.y);
        // The player nearest it spawns it: that place is surely loaded in their game.
        long spawner = players[0].owner;
        float nearest = float.MaxValue;
        foreach ((long owner, Vector3 at) in players)
        {
            float distance = Utils.DistanceXZ(at, spot);
            if (distance >= nearest) continue;
            nearest = distance;
            spawner = owner;
        }
        ZPackage package = new();
        package.Write(++_reinforceTicket);
        package.Write(_encounter);
        package.Write(_center);
        package.Write(spot);
        ZRoutedRpc.instance.InvokeRoutedRPC(spawner, Rpcs.HoardBaneReinforce, package);
    }

    // From the player who spawned it.
    private static void RPC_Reinforced(long sender, ZPackage package)
    {
        if (!IsServer()) return;
        int ticket = package.ReadInt();
        ZDOID id = package.ReadZDOID();
        // Too late: it ended, or another was asked for since.
        if (!_running || ticket != _reinforceTicket) TakeAway(id);
        else if (!Followers.Contains(id)) Followers.Add(id);
    }

    // ---- Ending ----

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
        if (WeatherState.IsActive(WeatherKind.HoardBane))
        {
            WeatherState.End(WeatherKind.HoardBane);
            WeatherSync.Broadcast();
        }
        Reward.Clear();
        if (ending == Ending.Survived) RollReward();
        if (_obliterator.IsNone() || ZDOMan.instance.GetZDO(_obliterator) == null) _obliterator = FindObliterator(_center);
        // Failed: the chest isn't lost. Back into the obliterator, or with none left, to whoever burnt it.
        string? chest = ending == Ending.Survived ? null : _burnt;
        _burnt = null;
        bool chestBack = chest != null && _obliterator.IsNone();
        if (chest != null && !chestBack) Reward.Add((chest, 1));
        Plugin.ModLogger.LogInfo($"Hoard-Bane Rite ended ({ending}) with {_elitesSlain} of {_elitesTotal} elites slain" +
            (Reward.Count > 0 ? $", {(chest != null ? "the chest goes back" : "treasure")}: {string.Join(", ", Reward.ConvertAll(r => $"{r.amount} {r.item}"))}." : "."));
        // treasure: on a failure, whether the chest waits in the obliterator.
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.HoardBaneEnded, (int)ending, Reward.Count > 0, _center);
        if (chestBack) GiveChestBack(chest!);
        IncursionArena.SweepOnServer(_center, Radius, Messages.HoardBaneName);
        ClearFight();
        if (Reward.Count > 0)
        {
            _rewardAt = _obliterator;
            _rewardTries = 0;
            _nextReward = 0f;
            GiveRewardLater();
        }
    }

    // The player who burnt the chest, when no obliterator is left to hold it (see HoardBaneArena.RPC_ChestBack).
    private static void GiveChestBack(string chest)
    {
        bool known = _burntBy != 0L && (_burntBy == ZRoutedRpc.instance.m_id || ZNet.instance.GetPeer(_burntBy) != null);
        if (!known)
        {
            Plugin.ModLogger.LogWarning($"Hoard-Bane Rite: no obliterator, and whoever burnt {chest} is gone; it is lost.");
            return;
        }
        ZRoutedRpc.instance.InvokeRoutedRPC(_burntBy, Rpcs.HoardBaneChestBack, chest);
    }

    // Coins and valuables (as rich as the strongest boss everyone inside has slain), and that boss's idols. Each chest has
    // its own coins and valuables: the harder its fight, the richer.
    private static void RollReward()
    {
        List<string> problems = [];
        List<(string item, int amount)> rolled = [];
        int chest = Mathf.Clamp(_encounter, 0, PluginConfig.HoardBaneCoins.Length - 1);
        List<RagnarokLoot.Treasure> coins = RagnarokLoot.Parse(PluginConfig.HoardBaneCoins[chest].Value, problems);
        if (coins.Count > 0) rolled.Add((coins[0].Item, HoardBaneLoot.Amount(coins[0], UnityEngine.Random.value)));
        List<RagnarokLoot.Treasure> table = RagnarokLoot.Parse(PluginConfig.HoardBaneTreasure[chest].Value, problems);
        float strength = _strongest < 0 ? 0f : RagnarokLoot.Strength(_strongest, Roster.Count);
        List<float> strengths = [];
        for (int i = 0; i < PluginConfig.HoardBaneTreasureRolls[chest].Value; i++) strengths.Add(strength);
        rolled.AddRange(RagnarokLoot.Roll(table, strengths, () => UnityEngine.Random.value));
        List<HoardBaneLoot.Idol> idols = HoardBaneLoot.ParseIdols(PluginConfig.HoardBaneIdols.Value, problems);
        foreach (RagnarokLoot.Treasure idol in HoardBaneLoot.PickIdols(idols, Roster.ConvertAll(r => r.Prefab), _strongest))
            rolled.Add((idol.Item, HoardBaneLoot.Amount(idol, UnityEngine.Random.value)));
        foreach (string problem in problems)
            Plugin.ModLogger.LogWarning($"Hoard-Bane Rite: couldn't read \"{problem}\" in its Coins, Treasure or Idols setting.");
        foreach ((string item, int amount) in rolled)
        {
            if (amount <= 0) continue;
            int at = Reward.FindIndex(r => r.item == item);
            if (at < 0) Reward.Add((item, amount));
            else Reward[at] = (item, Reward[at].amount + amount);
        }
        // More players, more treasure: each item's amount times the multiplier for the players alive inside as it is won.
        int players = Math.Max(1, PlayersInside().Count);
        float multiplier = PartyReward.Multiplier(players, PluginConfig.HoardBaneRewardPerPlayer.Value);
        PartyReward.ScaleAll(Reward, multiplier, () => UnityEngine.Random.value);
        Plugin.ModLogger.LogInfo($"Hoard-Bane Rite: {players} players inside, treasure x{multiplier:0.##}.");
    }

    // The game that owns the obliterator puts the treasure inside (see RagnarokArena.Reward).
    private static void GiveRewardLater()
    {
        if (Reward.Count == 0 || _running || Time.time < _nextReward) return;
        _nextReward = Time.time + RewardRetry;
        ZDO? zdo = _rewardAt.IsNone() ? null : ZDOMan.instance.GetZDO(_rewardAt);
        if (zdo == null)
        {
            Plugin.ModLogger.LogWarning("Hoard-Bane Rite: no obliterator to put the treasure in.");
            Reward.Clear();
            return;
        }
        long owner = zdo.GetOwner();
        if (owner == 0L)
        {
            if (++_rewardTries < RewardTries) return;
            Plugin.ModLogger.LogWarning("Hoard-Bane Rite: nobody has the obliterator loaded; the treasure is lost.");
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
        ZRoutedRpc.instance.InvokeRoutedRPC(owner, zdo.m_uid, Rpcs.HoardBaneReward, package);
        Reward.Clear();
    }

    // ---- Helpers ----

    private static float Radius => PluginConfig.Radius(WeatherKind.HoardBane);

    private static List<long> PlayersInside() => PlayersInsideAt().ConvertAll(p => p.owner);

    // Each player alive inside, with where they stand.
    private static List<(long owner, Vector3 at)> PlayersInsideAt()
    {
        List<(long owner, Vector3 at)> players = [];
        float radius = Radius;
        foreach (ZDO zdo in ZNet.instance.GetAllCharacterZDOS())
        {
            if (zdo.GetBool(ZDOVars.s_dead) || Utils.DistanceXZ(zdo.GetPosition(), _center) > radius) continue;
            long owner = zdo.GetOwner();
            if (owner != 0L && !players.Exists(p => p.owner == owner)) players.Add((owner, zdo.GetPosition()));
        }
        return players;
    }

    private static void TakeAway(ZDOID id)
    {
        ZDO? zdo = id.IsNone() ? null : ZDOMan.instance.GetZDO(id);
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
