using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// How many stars the game itself ever gives a creature, so the rites never make starred creatures the game doesn't
// (a starred ghost): in the world, from raids, from spawners and from spawn areas (nests, bone piles).
internal static class VanillaStars
{
    private static readonly Dictionary<string, int> MostLevels = [];

    // The highest level (1: no star) the game ever spawns this creature at. 1 when the game doesn't spawn it at all.
    internal static int MostLevel(string prefab)
    {
        if (string.IsNullOrEmpty(prefab)) return 1;
        if (MostLevels.TryGetValue(prefab, out int known)) return known;
        int most = 1;
        SpawnSystem? world = ZoneSystem.instance && ZoneSystem.instance.m_zoneCtrlPrefab
            ? ZoneSystem.instance.m_zoneCtrlPrefab.GetComponent<SpawnSystem>() : null;
        if (world)
            foreach (SpawnSystemList list in world!.m_spawnLists)
                if (list)
                    foreach (SpawnSystem.SpawnData data in list.m_spawners)
                        if (data.m_prefab && data.m_prefab.name == prefab) most = Math.Max(most, data.m_maxLevel);
        // The game's own raids (each raid going on is a copy: changing one leaves these as the game made them).
        if (RandEventSystem.instance)
            foreach (RandomEvent raid in RandEventSystem.instance.m_events)
                if (raid != null)
                    foreach (SpawnSystem.SpawnData data in raid.m_spawn)
                        if (data.m_prefab && data.m_prefab.name == prefab) most = Math.Max(most, data.m_maxLevel);
        if (ZNetScene.instance)
            foreach (GameObject candidate in ZNetScene.instance.m_prefabs)
            {
                if (!candidate) continue;
                foreach (CreatureSpawner spawner in candidate.GetComponentsInChildren<CreatureSpawner>(true))
                    if (spawner.m_creaturePrefab && spawner.m_creaturePrefab.name == prefab) most = Math.Max(most, spawner.m_maxLevel);
                foreach (SpawnArea area in candidate.GetComponentsInChildren<SpawnArea>(true))
                    foreach (SpawnArea.SpawnData data in area.m_prefabs)
                        if (data.m_prefab && data.m_prefab.name == prefab) most = Math.Max(most, data.m_maxLevel);
            }
        MostLevels[prefab] = most;
        Plugin.ModLogger.LogInfo($"{prefab}: the game gives it up to {most - 1} stars.");
        return most;
    }
}
