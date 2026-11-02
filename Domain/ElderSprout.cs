using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;
using Random = UnityEngine.Random;

namespace JoksterCube.TempestRite.Domain;

// The Elder-Sprout Rite, a Domain rite: while it goes on, the tree stumps within the Domain radius of the obliterator
// are taken away one after another, about one each second, and a sapling of the same tree is planted where each stood,
// turned its own way so a cleared forest doesn't grow back in rows of the same tree.
// - Done on the server, on the world's objects themselves: it has every stump, also those no player's game has loaded.
//   The area is looked through again every few seconds, so trees felled while it goes on sprout too. Stumps are picked
//   in a random order, favoring ones far from the last, so it jumps about the area like the Amber-Mend Rite.
// - Each stump taken away bursts for every player with the mod near it (a routed RPC from the server): the stump's own
//   breaking effect and sound, the sapling's planting sound and a puff of sprouts. Players without the mod see the stump
//   go and the sapling appear.
// - Stumps still waiting their turn give off a few little sprouts now and then, in each game for itself.
// - Which sapling a stump grows back from comes from the game's own prefabs: a sapling grows into trees, and each tree
//   leaves its own stump when felled. Stumps of trees no sapling grows into (swamp trees, the old meadow stumps) stay.
internal static class ElderSprout
{
    private const string Feature = "Elder-Sprout Rite";
    private const float SproutInterval = 1f;
    private const float ScanInterval = 10f;
    // Random picks compared each sprout; the one farthest from the last sprouted stump wins.
    private const int PickTries = 3;
    // Puffs are shown to players this close to the stump.
    private const float PuffRange = 150f;
    // Waiting stumps give off sprouts this close to the player, one every so often.
    private const float MotesRange = 60f;
    private const float MotesEvery = 3f;
    private const float MotesScale = 0.5f;

    // Stump prefab hash -> the sapling prefab that grows into its tree. Built from the scene's prefabs once per session.
    private static readonly Dictionary<int, GameObject> Saplings = [];
    private static ZNetScene? _builtFor;

    // ---- Server state ----
    private static readonly List<ZDO> Found = [];
    private static readonly List<ZDOID> Waiting = [];
    private static readonly HashSet<ZDOID> WaitingSet = [];
    private static float _nextScan;
    private static float _nextSprout;
    private static Vector3? _scannedAt;
    private static Vector3? _lastSprouted;

    // ---- Player state ----
    private static float _nextMotes;
    private static readonly List<Vector3> Near = [];

    internal static void Register(ZRoutedRpc rpc) => rpc.Register<Vector3, int>(Rpcs.ElderSproutPuff, RPC_Puff);

    internal static void ClearSession()
    {
        ResetServer();
        _nextMotes = 0f;
        Near.Clear();
        Saplings.Clear();
        _builtFor = null;
    }

    private static void ResetServer()
    {
        Found.Clear();
        Waiting.Clear();
        WaitingSet.Clear();
        _nextScan = 0f;
        _nextSprout = 0f;
        _scannedAt = null;
        _lastSprouted = null;
    }

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || !ZNet.instance || !ZNetScene.instance || ZDOMan.instance == null) return;
        try
        {
            if (ZNet.instance.IsServer()) SproutOnServer();
            if (!ZNet.instance.IsDedicated()) ShowWaiting();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // ---- Server ----

    private static void SproutOnServer()
    {
        if (!WeatherState.IsActive(WeatherKind.ElderSprout))
        {
            if (_scannedAt != null) ResetServer();
            return;
        }
        Vector3 center = WeatherState.Position(WeatherKind.ElderSprout);
        // Performed again elsewhere: look through the new area at once.
        if (_scannedAt is not { } at || Utils.DistanceXZ(at, center) > 1f) _nextScan = 0f;
        if (Time.time >= _nextScan) Scan(center);
        if (Time.time < _nextSprout) return;
        _nextSprout = Time.time + SproutInterval;
        if (Pick() is { } stump) Sprout(stump);
    }

    // Keeps the stumps in reach that aren't waiting yet.
    private static void Scan(Vector3 center)
    {
        _nextScan = Time.time + ScanInterval;
        bool first = _scannedAt == null;
        _scannedAt = center;
        BuildSaplings();
        if (Saplings.Count == 0) return;
        Found.Clear();
        IncursionArena.FindAround(center, PluginConfig.Radius(WeatherKind.ElderSprout), Found);
        foreach (ZDO zdo in Found)
            if (Sproutable(zdo) && WaitingSet.Add(zdo.m_uid)) Waiting.Add(zdo.m_uid);
        Found.Clear();
        if (first) Plugin.ModLogger.LogInfo($"{Feature}: {Waiting.Count} stumps to sprout around ({center.x:0}, {center.z:0}).");
    }

    // A few random waiting stumps, the one farthest from the last sprouted; null when none is left. Stumps gone or out
    // of reach are dropped.
    private static ZDO? Pick()
    {
        ZDO? best = null;
        int bestIndex = -1;
        float bestDistance = -1f;
        for (int tries = 0; tries < PickTries && Waiting.Count > 0;)
        {
            int index = Random.Range(0, Waiting.Count);
            ZDO? zdo = ZDOMan.instance.GetZDO(Waiting[index]);
            if (zdo == null || !Sproutable(zdo))
            {
                Drop(index);
                if (bestIndex == Waiting.Count) bestIndex = index;
                continue;
            }
            tries++;
            float distance = _lastSprouted is { } last ? Utils.DistanceXZ(zdo.GetPosition(), last) : 0f;
            if (distance <= bestDistance) continue;
            best = zdo;
            bestIndex = index;
            bestDistance = distance;
        }
        if (bestIndex >= 0) Drop(bestIndex);
        return best;
    }

    private static bool Sproutable(ZDO zdo) =>
        zdo.IsValid() && Saplings.ContainsKey(zdo.GetPrefab()) && WeatherState.Reaches(WeatherKind.ElderSprout, zdo.GetPosition());

    // Removes by swapping in the last one.
    private static void Drop(int index)
    {
        ZDOID id = Waiting[index];
        int last = Waiting.Count - 1;
        Waiting[index] = Waiting[last];
        Waiting.RemoveAt(last);
        WaitingSet.Remove(id);
    }

    private static void Sprout(ZDO stump)
    {
        GameObject sapling = Saplings[stump.GetPrefab()];
        Vector3 at = stump.GetPosition();
        int stumpHash = stump.GetPrefab();
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.ElderSproutPuff, at, stumpHash);
        stump.SetOwner(ZDOMan.GetSessionID());
        ZDOMan.instance.DestroyZDO(stump);
        Plant(sapling, at);
        _lastSprouted = at;
    }

    // A new sapling where the stump stood, as the game makes one when it is planted (see ZNetView.Awake), turned a
    // random way round. Let go of, so the game of a player near takes it over and grows it.
    private static void Plant(GameObject sapling, Vector3 at)
    {
        ZNetView view = sapling.GetComponent<ZNetView>();
        int hash = sapling.name.GetStableHashCode();
        ZDO zdo = ZDOMan.instance.CreateNewZDO(at, hash);
        zdo.Persistent = view.m_persistent;
        zdo.Type = view.m_type;
        zdo.Distant = view.m_distant;
        zdo.SetPrefab(hash);
        zdo.SetRotation(Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        // Planted now: its growing time starts here, whichever game takes it over.
        zdo.Set(ZDOVars.s_plantTime, ZNet.instance.GetTime().Ticks);
        zdo.SetOwner(0L);
    }

    private static void BuildSaplings()
    {
        if (_builtFor == ZNetScene.instance) return;
        _builtFor = ZNetScene.instance;
        Saplings.Clear();
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            if (!prefab || prefab.GetComponent<Plant>() is not { } plant || !prefab.GetComponent<ZNetView>()) continue;
            foreach (GameObject grown in plant.m_grownPrefabs)
            {
                if (!grown || grown.GetComponent<TreeBase>() is not { } tree || !tree.m_stubPrefab) continue;
                // The first sapling found keeps a stump shared by several.
                int stub = tree.m_stubPrefab.name.GetStableHashCode();
                if (!Saplings.ContainsKey(stub)) Saplings[stub] = prefab;
            }
        }
    }

    // ---- Players ----

    // A stump near is taken away: it breaks with its own effect and sound, the sapling is planted with its own, and
    // sprouts puff up.
    private static void RPC_Puff(long sender, Vector3 at, int stumpHash)
    {
        if (!ZNet.instance || ZNet.instance.IsDedicated() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
        Player? player = Player.m_localPlayer;
        if (!player || Utils.DistanceXZ(player!.transform.position, at) > PuffRange || !ZNetScene.instance) return;
        BuildSaplings();
        GameObject stump = ZNetScene.instance.GetPrefab(stumpHash);
        if (stump && stump.GetComponent<Destructible>() is { } destructible) destructible.m_destroyedEffect.Create(at, Quaternion.identity);
        if (Saplings.TryGetValue(stumpHash, out GameObject sapling) && sapling.GetComponent<Piece>() is { } piece)
            piece.m_placeEffect.Create(at, Quaternion.identity);
        if (PluginConfig.ShowDomainEffects.IsOn())
            DomainVisuals.Burst(WeatherKind.ElderSprout, at + Vector3.up * 0.6f, DomainVisuals.RiteColor(WeatherKind.ElderSprout), always: true);
    }

    // Now and then one of the stumps waiting near the player gives off a few little sprouts.
    private static void ShowWaiting()
    {
        if (Time.time < _nextMotes || !PluginConfig.ShowDomainEffects.IsOn() || !WeatherState.IsActive(WeatherKind.ElderSprout)) return;
        Player? player = Player.m_localPlayer;
        if (!player) return;
        _nextMotes = Time.time + MotesEvery;
        BuildSaplings();
        Vector3 here = player!.transform.position;
        Near.Clear();
        foreach (KeyValuePair<ZDO, ZNetView> instance in ZNetScene.instance.m_instances)
        {
            ZDO zdo = instance.Key;
            if (!Saplings.ContainsKey(zdo.GetPrefab())) continue;
            Vector3 position = zdo.GetPosition();
            if (Utils.DistanceXZ(position, here) <= MotesRange && WeatherState.Reaches(WeatherKind.ElderSprout, position)) Near.Add(position);
        }
        if (Near.Count == 0) return;
        Vector3 at = Near[Random.Range(0, Near.Count)];
        Near.Clear();
        DomainVisuals.Burst(WeatherKind.ElderSprout, at + Vector3.up * 0.4f, DomainVisuals.RiteColor(WeatherKind.ElderSprout), MotesScale);
    }
}
