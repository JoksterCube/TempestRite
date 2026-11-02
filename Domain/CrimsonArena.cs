using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The Crimson Rite in each player's game (the server runs its raids, see CrimsonRaids; the bubble, holding players and
// creatures, portals and the map are IncursionArena's):
// - a creature that dies inside, in the game that owns it, is told to the server, which counts it; everyone inside sees
//   the count after each kill, and how it ended (not what the reward is: it waits in the obliterator);
// - lightning strikes the obliterator as each raid after the first comes, and the last raid's creatures still inside die;
// - when it ends, the creatures still inside are cleared out, dropping nothing;
// - the game that owns the obliterator puts the reward in it.
// Raid creatures only spawn inside (see CrimsonSpawnPatches).
internal static class CrimsonArena
{
    // How far beyond the bubble the obliterator's lightning is seen.
    private const float StrikeSeenFrom = 50f;

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<int, Vector3>(Rpcs.CrimsonKills, RPC_Kills);
        rpc.Register<int, int, int, Vector3>(Rpcs.CrimsonEnded, RPC_Ended);
        rpc.Register<Vector3>(Rpcs.CrimsonWave, RPC_Wave);
        rpc.Register<string, int, Vector3>(Rpcs.CrimsonCatchUp, RPC_CatchUp);
    }

    internal static void ClearSession()
    {
        _catchUpRaid = null;
        _catchUpSteps = 0;
    }

    // ---- Catch-up ----

    // The raid going on and how many steps the server caught it up (see RaidCatchUp, CrimsonSpawnPatches).
    private static string? _catchUpRaid;
    private static int _catchUpSteps;

    private static void RPC_CatchUp(long sender, string raid, int steps, Vector3 center)
    {
        if (!IncursionArena.FromServer(sender)) return;
        _catchUpRaid = raid;
        _catchUpSteps = Mathf.Max(0, steps);
    }

    // How many steps this raid is caught up (0: not told, or another raid).
    internal static int CatchUpSteps(string raid) => raid == _catchUpRaid ? _catchUpSteps : 0;

    private static bool Active => WeatherState.IsActive(WeatherKind.Crimson);
    private static float Radius => PluginConfig.Radius(WeatherKind.Crimson);

    // ---- Kills ----

    // In the game that owns a creature, as it dies: hostile creatures (not players, tamed ones or animals) inside count.
    internal static void CreatureDied(Character character)
    {
        if (!Active || !IncursionArena.Hostile(character) || IncursionArena.WasCleared(character)) return;
        ZNetView nview = character.m_nview;
        if (!nview || !nview.IsValid() || !nview.IsOwner()) return;
        Vector3 position = character.transform.position;
        if (!WeatherState.Reaches(WeatherKind.Crimson, position)) return;
        ZRoutedRpc.instance?.InvokeRoutedRPC(Rpcs.CrimsonKill, nview.GetZDO().m_uid, position);
    }

    private static void RPC_Kills(long sender, int kills, Vector3 center)
    {
        if (!IncursionArena.FromServer(sender) || !IncursionArena.Here(center, WeatherKind.Crimson)) return;
        // Always shown, whatever Show Messages is set to: it is the rite's score.
        Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, string.Format(Messages.CrimsonKillCount, kills));
    }

    // The next raid comes: the last one's creatures still inside die, dropping nothing and not counted.
    private static void RPC_Wave(long sender, Vector3 center)
    {
        if (!IncursionArena.FromServer(sender)) return;
        IncursionArena.ClearInside(center, Radius, Messages.CrimsonName);
        if (Near(center)) BlessingLightning.Strike(center);
    }

    private static bool Near(Vector3 center) =>
        Player.m_localPlayer && Utils.DistanceXZ(Player.m_localPlayer.transform.position, center) <= Radius + StrikeSeenFrom;

    private static void RPC_Ended(long sender, int ending, int kills, int reward, Vector3 center)
    {
        if (!IncursionArena.FromServer(sender)) return;
        ClearSession();
        // However it ended, nothing it brought is left behind, and nothing cleared out drops anything.
        IncursionArena.ClearInside(center, Radius, Messages.CrimsonName);
        // The obliterator is struck once more as the reward goes in.
        if ((CrimsonRaids.Ending)ending == CrimsonRaids.Ending.Survived && reward > 0 && Near(center)) BlessingLightning.Strike(center);
        if (!IncursionArena.WasInside(center) && !IncursionArena.Here(center, WeatherKind.Crimson))
        {
            IncursionArena.TellFar(WeatherKind.Crimson, (CrimsonRaids.Ending)ending == CrimsonRaids.Ending.Survived);
            return;
        }
        string message = (CrimsonRaids.Ending)ending switch
        {
            CrimsonRaids.Ending.Survived when reward > 0 => Messages.CrimsonSurvived,
            CrimsonRaids.Ending.Survived => Messages.CrimsonSurvivedNoReward,
            CrimsonRaids.Ending.Obliterator => Messages.CrimsonFailedObliterator,
            CrimsonRaids.Ending.Fallen => Messages.CrimsonFailedFallen,
            _ => Messages.CrimsonFailedEnded
        };
        PlayerMessages.Warning(message);
    }

    // ---- Reward ----

    // In the game that owns the obliterator, when the server says the rite was survived.
    internal static void Reward(Incinerator incinerator, long sender, int amount)
    {
        if (amount <= 0 || !incinerator || !incinerator.m_nview || !incinerator.m_nview.IsOwner()) return;
        if (ZRoutedRpc.instance == null || (sender != ZRoutedRpc.instance.GetServerPeerID() && sender != ZRoutedRpc.instance.m_id)) return;
        GameObject? prefab = RewardPrefab();
        if (!prefab || !prefab!.GetComponent<ItemDrop>())
        {
            Plugin.ModLogger.LogWarning($"Crimson Rite: reward item \"{PluginConfig.CrimsonRewardItem.Value}\" is not an item the game knows.");
            return;
        }
        PutIn(incinerator, prefab, amount);
        Plugin.ModLogger.LogInfo($"Crimson Rite: put {amount} {prefab.name} in the obliterator.");
    }

    // Puts items in the obliterator; what doesn't fit falls out on top.
    internal static void PutIn(Incinerator incinerator, GameObject prefab, int amount)
    {
        ItemDrop drop = prefab.GetComponent<ItemDrop>();
        Inventory inventory = incinerator.m_container.GetInventory();
        int stack = Mathf.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
        int left = amount;
        while (left > 0)
        {
            int count = Mathf.Min(left, stack);
            if (!inventory.CanAddItem(prefab, count) || !inventory.AddItem(prefab, count)) break;
            left -= count;
        }
        Vector3 top = incinerator.transform.position + Vector3.up * 1.5f;
        while (left > 0)
        {
            int count = Mathf.Min(left, stack);
            ItemDrop.DropItem(drop.m_itemData, count, top, Quaternion.identity);
            left -= count;
        }
    }

    private static GameObject? RewardPrefab()
    {
        if (!ObjectDB.instance) return null;
        return ObjectDB.instance.GetItemPrefab(PluginConfig.CrimsonRewardItem.Value.Trim());
    }
}
