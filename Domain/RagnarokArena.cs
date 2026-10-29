using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The Ragnarök Rite in each player's game (the server runs it, see RagnarokRush; the bubble, holding players and
// creatures, portals and the map are IncursionArena's):
// - says which of the bosses asked about the local player has slain;
// - spawns a boss when the server asks this game to, within the bubble, struck by lightning, hunting the players;
// - tells the server when a boss it owns falls;
// - shows the bosses rising and falling (lightning strikes the obliterator as each falls), and how it ended, never
//   which bosses are to come nor what treasure was won; clears out what it brought, dropping nothing;
// - the game that owns the obliterator puts the treasure in it.
internal static class RagnarokArena
{
    // How far beyond the bubble its lightning is seen.
    private const float StrikeSeenFrom = 50f;
    private const int SpawnTries = 40;
    // Bosses rise no nearer the obliterator than this (or a third of the radius, if smaller).
    private const float SpawnClearOfCenter = 15f;
    // Bosses rise within this share of the radius, clear of the edge.
    private const float SpawnShare = 0.6f;

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<ZPackage, Vector3>(Rpcs.RagnarokAsk, RPC_Ask);
        rpc.Register<string, Vector3, int>(Rpcs.RagnarokSpawn, RPC_Spawn);
        rpc.Register<int, Vector3>(Rpcs.RagnarokBegun, RPC_Begun);
        rpc.Register<ZPackage>(Rpcs.RagnarokBoss, RPC_Boss);
        rpc.Register<int, bool, Vector3>(Rpcs.RagnarokEnded, RPC_Ended);
    }

    private static float Radius => PluginConfig.Radius(WeatherKind.Ragnarok);

    // ---- Asked by the server ----

    // Which of these bosses (by the key each gives as it dies) the local player has slain: they have its key, or a kill
    // of it in their character's stats. The game gives the key only to the player whose game had the boss when it died;
    // everyone who hit it gets the kill.
    private static void RPC_Ask(long sender, ZPackage asked, Vector3 center)
    {
        if (!IncursionArena.FromServer(sender)) return;
        Player player = Player.m_localPlayer;
        Dictionary<string, float>? kills = Game.instance ? Game.instance.GetPlayerProfile()?.m_playerStats[0]?.m_enemyStats[0] : null;
        List<string> have = [];
        int count = asked.ReadInt();
        for (int i = 0; i < count; i++)
        {
            string key = asked.ReadString();
            string name = asked.ReadString();
            if (player.HaveUniqueKey(key) || (kills != null && kills.TryGetValue(name, out float killed) && killed > 0f)) have.Add(key);
        }
        ZPackage answer = new();
        answer.Write(have.Count);
        foreach (string key in have) answer.Write(key);
        ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.RagnarokAnswer, answer);
    }

    // Spawns the boss within the bubble, away from the obliterator, on dry ground.
    private static void RPC_Spawn(long sender, string prefabName, Vector3 center, int ticket)
    {
        if (!IncursionArena.FromServer(sender) || !ZNetScene.instance) return;
        GameObject? prefab = ZNetScene.instance.GetPrefab(prefabName);
        if (!prefab || !prefab!.GetComponent<Character>())
        {
            Plugin.ModLogger.LogWarning($"Ragnarök Rite: can't spawn \"{prefabName}\": not a creature this game knows.");
            return;
        }
        Vector3 at = SpawnPoint(center);
        Quaternion facing = Quaternion.LookRotation(new Vector3(center.x - at.x, 0f, center.z - at.z).normalized + Vector3.forward * 0.001f);
        GameObject boss = Object.Instantiate(prefab, at, facing);
        ZNetView nview = boss.GetComponent<ZNetView>();
        if (!nview || !nview.IsValid())
        {
            Plugin.ModLogger.LogWarning($"Ragnarök Rite: \"{prefabName}\" spawned without a network object.");
            Object.Destroy(boss);
            return;
        }
        nview.GetZDO().Set(IncursionArena.BossMark, true);
        BaseAI ai = boss.GetComponent<BaseAI>();
        if (ai)
        {
            ai.SetPatrolPoint();
            ai.Alert();
            ai.SetHuntPlayer(true);
        }
        Plugin.ModLogger.LogInfo($"Ragnarök Rite: {prefabName} rises at ({at.x:0}, {at.z:0}).");
        ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.RagnarokSpawned, nview.GetZDO().m_uid, ticket, at);
    }

    private static Vector3 SpawnPoint(Vector3 center)
    {
        float radius = Radius;
        float reach = Mathf.Max(5f, radius * SpawnShare);
        float clear = Mathf.Min(SpawnClearOfCenter, radius / 3f);
        ZoneSystem zones = ZoneSystem.instance;
        for (int i = 0; i < SpawnTries; i++)
        {
            Vector2 offset = Random.insideUnitCircle * reach;
            if (offset.magnitude < clear) continue;
            Vector3 point = center + new Vector3(offset.x, 0f, offset.y);
            if (!zones || !zones.GetSolidHeight(point, out float ground) || ground < zones.m_waterLevel) continue;
            point.y = ground + 1f;
            return point;
        }
        // Nowhere better: beside the obliterator.
        Vector3 beside = center + Vector3.forward * clear;
        beside.y = (zones && zones.GetSolidHeight(beside, out float height) ? Mathf.Max(height, center.y) : center.y) + 1f;
        return beside;
    }

    // ---- Bosses falling ----

    // In the game that owns a creature, as it dies: one of the Ragnarök Rite's bosses is told to the server.
    internal static void CreatureDied(Character character)
    {
        if (!character || !IncursionArena.IsRagnarokBoss(character)) return;
        ZNetView nview = character.m_nview;
        if (!nview.IsOwner()) return;
        ZRoutedRpc.instance?.InvokeRoutedRPC(Rpcs.RagnarokDown, nview.GetZDO().m_uid);
    }

    // ---- What everyone sees ----

    // How many bosses will rise, not which: each is a surprise.
    private static void RPC_Begun(long sender, int count, Vector3 center)
    {
        if (!IncursionArena.FromServer(sender) || !IncursionArena.Here(center, WeatherKind.Ragnarok)) return;
        PlayerMessages.Warning(string.Format(Messages.RagnarokBegins, count));
    }

    private static void RPC_Boss(long sender, ZPackage package)
    {
        if (!IncursionArena.FromServer(sender)) return;
        string prefab = package.ReadString();
        int which = package.ReadInt();
        int of = package.ReadInt();
        bool fell = package.ReadBool();
        Vector3 center = package.ReadVector3();
        Vector3 at = package.ReadVector3();
        if (!fell && Near(at)) BlessingLightning.Strike(at);
        // Each boss but the last falling strikes the obliterator: the next is coming (the last is struck as it ends).
        if (fell && which < of && Near(center)) BlessingLightning.Strike(center);
        if (!IncursionArena.Here(center, WeatherKind.Ragnarok)) return;
        PlayerMessages.Warning(string.Format(fell ? Messages.RagnarokFalls : Messages.RagnarokRises, BossName(prefab), which, of));
    }

    // What treasure was won isn't told: it waits in the obliterator.
    private static void RPC_Ended(long sender, int ending, bool treasure, Vector3 center)
    {
        if (!IncursionArena.FromServer(sender)) return;
        // However it ended, nothing it brought is left behind, and nothing cleared out drops anything.
        IncursionArena.ClearInside(center, Radius, Messages.RagnarokName);
        RagnarokRush.Ending how = (RagnarokRush.Ending)ending;
        // The obliterator is struck as the last boss falls and the treasure goes in.
        if (how == RagnarokRush.Ending.Survived && Near(center)) BlessingLightning.Strike(center);
        if (!IncursionArena.WasInside(center) && !IncursionArena.Here(center, WeatherKind.Ragnarok)) return;
        string message = how switch
        {
            RagnarokRush.Ending.Survived when treasure => Messages.RagnarokSurvived,
            RagnarokRush.Ending.Survived => Messages.RagnarokSurvivedNoReward,
            RagnarokRush.Ending.Obliterator => Messages.RagnarokFailedObliterator,
            RagnarokRush.Ending.Fallen => Messages.RagnarokFailedFallen,
            RagnarokRush.Ending.TimeUp => Messages.RagnarokFailedTime,
            RagnarokRush.Ending.NoBosses => Messages.RagnarokNoBosses,
            RagnarokRush.Ending.NoOne => Messages.RagnarokNoOne,
            RagnarokRush.Ending.NoSpawn => Messages.RagnarokFailedSpawn,
            _ => Messages.RagnarokFailedEnded
        };
        PlayerMessages.Warning(message);
    }

    private static bool Near(Vector3 point) =>
        Player.m_localPlayer && Utils.DistanceXZ(Player.m_localPlayer.transform.position, point) <= Radius + StrikeSeenFrom;

    // A boss's name in the player's language: "Eikthyr".
    private static string BossName(string prefabName)
    {
        GameObject? prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(prefabName) : null;
        Character? boss = prefab ? prefab!.GetComponent<Character>() : null;
        string name = boss ? boss!.m_name : prefabName;
        return Localization.instance != null ? Localization.instance.Localize(name) : name;
    }

    // ---- Treasure ----

    // In the game that owns the obliterator, when the server says the rite was survived.
    internal static void Reward(Incinerator incinerator, long sender, ZPackage treasure)
    {
        if (!incinerator || !incinerator.m_nview || !incinerator.m_nview.IsOwner() || !ObjectDB.instance) return;
        if (ZRoutedRpc.instance == null || (sender != ZRoutedRpc.instance.GetServerPeerID() && sender != ZRoutedRpc.instance.m_id)) return;
        int count = treasure.ReadInt();
        for (int i = 0; i < count; i++)
        {
            string item = treasure.ReadString();
            int amount = treasure.ReadInt();
            GameObject? prefab = ObjectDB.instance.GetItemPrefab(item);
            if (amount <= 0) continue;
            if (!prefab || !prefab!.GetComponent<ItemDrop>())
            {
                Plugin.ModLogger.LogWarning($"Ragnarök Rite: treasure \"{item}\" is not an item the game knows. Check the Treasure setting.");
                continue;
            }
            CrimsonArena.PutIn(incinerator, prefab, amount);
            Plugin.ModLogger.LogInfo($"Ragnarök Rite: put {amount} {prefab.name} in the obliterator.");
        }
    }
}
