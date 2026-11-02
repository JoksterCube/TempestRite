using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;
using Object = UnityEngine.Object;

namespace JoksterCube.TempestRite.Domain;

// The Hoard-Bane Rite in each player's game (the server runs it, see HoardBaneRush; the bubble, holding players and
// creatures, portals and the map are IncursionArena's):
// - spawns the fight when the server asks this game to (see HoardBaneLoot for who and where): the elites, with stars
//   beyond what the game spawns, dormant in their bubbles with their guards; the miniboss shielded and hunting with its
//   followers;
// - in the game that owns an elite or guard: its pack wakes when a player steps into its bubble or something hits one
//   of it (see HoardBanePatches: until then they don't move and take no damage);
// - in the game that owns a miniboss: its shield comes off when the server says every elite has fallen;
// - tells the server when an elite or miniboss it owns falls (guards don't count);
// - spawns one more follower when the server asks, where it says;
// - draws each sleeping elite's bubble and each shielded miniboss's shield (flashing as the local player hits it), and
//   gives the elites and minibosses a menacing red glow, their own colors and more size, collider and all (their
//   corpses too);
// - shows how it goes: lightning strikes the miniboss as each elite falls, and the obliterator as it ends in victory.
internal static class HoardBaneArena
{
    internal const int EliteRole = 1;
    internal const int BossRole = 2;
    internal const int FollowerRole = 3;
    internal const int GuardRole = 4;

    // Saved on each creature the Hoard-Bane Rite brings: what it is (the roles above).
    internal static readonly int Role = "TempestRite_HoardRole".GetStableHashCode();
    // An elite or guard still waiting in its bubble: where its bubble is, how big, and which pack it is in.
    internal static readonly int Dormant = "TempestRite_HoardDormant".GetStableHashCode();
    private static readonly int Post = "TempestRite_HoardPost".GetStableHashCode();
    private static readonly int PostRadius = "TempestRite_HoardPostRadius".GetStableHashCode();
    private static readonly int Pack = "TempestRite_HoardPack".GetStableHashCode();
    // A miniboss under its shield.
    internal static readonly int Shield = "TempestRite_HoardShield".GetStableHashCode();
    // An elite's own name, how its colors shift (hue, saturation, value; on minibosses too), and what they are multiplied by.
    internal static readonly int EliteName = "TempestRite_HoardName".GetStableHashCode();
    private static readonly int EliteTint = "TempestRite_HoardTint".GetStableHashCode();
    private static readonly int EliteColor = "TempestRite_HoardColor".GetStableHashCode();
    // How much bigger an elite is than its level makes it.
    private static readonly int EliteSize = "TempestRite_HoardSize".GetStableHashCode();
    // The same on an elite's or miniboss's corpse, with the size it ends up (its whole scale, the same in every game).
    private static readonly int CorpseSize = "TempestRite_HoardCorpseSize".GetStableHashCode();

    private const float StrikeSeenFrom = 50f;
    private const float WakeInterval = 0.25f;
    private const float DecorateInterval = 0.5f;
    // Followers rise around the miniboss, this far at most.
    private const float FollowerSpread = 5f;
    // The brothers rise this far apart.
    private const float BrotherGap = 3f;
    private const float NoHit = -1000f;
    // The elites' bubbles' color (the game's shield gradient, by fuel): nearly empty.
    private const float BubbleFuel = 0.1f;
    private const string GlowName = "TempestRite_EliteGlow";
    // How much bigger minibosses are than their level makes them (elites: see HoardBaneLoot.Elite.Size).
    private const float BossScale = 1.15f;
    // Minibosses are a star stronger than the game's own (the game draws it, and its health and damage go with it).
    private const int BossLevel = 2;
    // A shielded miniboss's pace: a third of what it was (never more than 4.5 m/s, nor than it runs), a slow walk.
    private const float ShieldedPace = 4.5f / 3f;
    private const float ShieldedShare = 1f / 3f;
    private const int ArcherSeedTries = 500;
    // How far an awake creature looks for a player to go for when it has none.
    private const float PreyRange = 80f;

    private static float _nextWake;
    private static float _nextDecorate;
    private static readonly DomeSet Domes = new("TempestRite_HoardBaneDomes");
    // Elites seen asleep, to tell when they wake.
    private static readonly HashSet<Character> Sleeping = [];

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<ZPackage>(Rpcs.HoardBaneSpawn, RPC_Spawn);
        rpc.Register<ZPackage>(Rpcs.HoardBaneReinforce, RPC_Reinforce);
        rpc.Register<ZPackage>(Rpcs.HoardBaneBegun, RPC_Begun);
        rpc.Register<ZPackage>(Rpcs.HoardBaneNews, RPC_News);
        rpc.Register<Vector3>(Rpcs.HoardBaneUnshield, RPC_Unshield);
        rpc.Register<int, bool, Vector3>(Rpcs.HoardBaneEnded, RPC_Ended);
        rpc.Register<string>(Rpcs.HoardBaneChestBack, RPC_ChestBack);
    }

    internal static void ClearSession()
    {
        Domes.Clear();
        Sleeping.Clear();
        ShieldLook.Clear();
        _nextWake = 0f;
        _nextDecorate = 0f;
    }

    private static float Radius => PluginConfig.Radius(WeatherKind.HoardBane);

    // An elite's bubble: never more than a third of the arena, so three fit on its edge.
    private static float BubbleRadius => Mathf.Clamp(PluginConfig.HoardBaneEliteRadius.Value, 1f, Radius / 3f);

    internal static int RoleOf(Character? character)
    {
        if (!character) return 0;
        ZNetView nview = character!.m_nview;
        return nview && nview.IsValid() ? nview.GetZDO().GetInt(Role) : 0;
    }

    internal static bool IsDormant(Character character) =>
        RoleOf(character) is EliteRole or GuardRole && character.m_nview.GetZDO().GetBool(Dormant);

    internal static bool IsShielded(Character character) =>
        RoleOf(character) == BossRole && character.m_nview.GetZDO().GetBool(Shield);

    // ---- Spawning, asked by the server ----

    private static void RPC_Spawn(long sender, ZPackage package)
    {
        if (!IncursionArena.FromServer(sender) || !ZNetScene.instance) return;
        int ticket = package.ReadInt();
        int encounter = package.ReadInt();
        Vector3 center = package.ReadVector3();
        float angle = package.ReadSingle();
        bool all = package.ReadBool();
        int count = package.ReadInt();
        List<(int place, float health, float damage)> bosses = [];
        for (int i = 0; i < count; i++) bosses.Add((package.ReadInt(), package.ReadSingle(), package.ReadSingle()));
        if (encounter < 0 || encounter >= HoardBaneLoot.Encounters.Length) return;
        HoardBaneLoot.Encounter fight = HoardBaneLoot.Encounters[encounter];
        float radius = Radius;

        List<(ZDOID id, string prefab)> elites = [];
        List<ZDOID> guarding = [];
        List<ZDOID> followers = [];
        if (all)
        {
            Vector2[] posts = HoardBaneLoot.BubblePosts(radius, BubbleRadius, angle, fight.Elites.Length);
            for (int i = 0; i < posts.Length; i++)
            {
                HoardBaneLoot.Elite elite = fight.Elites[i];
                Vector3 post = center + new Vector3(posts[i].x, 0f, posts[i].y);
                int pack = UnityEngine.Random.Range(1, int.MaxValue);
                Character? waiting = SpawnElite(elite, center, post, pack);
                if (waiting) elites.Add((waiting!.m_nview.GetZDO().m_uid, elite.Name));
                // The pack evenly around its elite. Guards are taken away with the followers if they come too late.
                List<HoardBaneLoot.Guard> guards = [];
                foreach (HoardBaneLoot.Guard guard in elite.Guards)
                    for (int g = 0; g < guard.Count; g++) guards.Add(guard);
                Vector2[] spots = HoardBaneLoot.PackPosts(BubbleRadius, UnityEngine.Random.Range(0f, 360f), guards.Count);
                for (int g = 0; g < guards.Count; g++)
                {
                    Vector3 at = post + new Vector3(spots[g].x, 0f, spots[g].y);
                    Character? guard = SpawnGuard(guards[g], center, at, post, pack);
                    if (guard) guarding.Add(guard!.m_nview.GetZDO().m_uid);
                }
            }
        }

        Vector2 bossPost = HoardBaneLoot.BossPost(radius, angle, fight.Elites.Length);
        Vector3 bossAt = center + new Vector3(bossPost.x, 0f, bossPost.y);
        Vector3 along = Vector3.Cross(Vector3.up, (bossAt - center).normalized);
        List<(ZDOID id, int place)> risen = [];
        foreach ((int place, float health, float damage) in bosses)
        {
            if (place < 0 || place >= fight.Bosses.Length) continue;
            Vector3 at = bossAt + along * ((place - (fight.Bosses.Length - 1) * 0.5f) * BrotherGap);
            Character? boss = SpawnBoss(fight.Bosses[place], center, at, health, damage);
            if (boss) risen.Add((boss!.m_nview.GetZDO().m_uid, place));
        }

        if (all)
            for (int i = 0; i < HoardBaneLoot.FollowersAtStart; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * FollowerSpread;
                Character? follower = SpawnFollower(fight.Follower, center, bossAt + new Vector3(offset.x, 0f, offset.y));
                if (follower) followers.Add(follower!.m_nview.GetZDO().m_uid);
            }

        Plugin.ModLogger.LogInfo($"Hoard-Bane Rite: {risen.Count} minibosses, {elites.Count} elites, {guarding.Count} guards and {followers.Count} followers rise.");
        ZPackage spawned = new();
        spawned.Write(ticket);
        spawned.Write(elites.Count);
        foreach ((ZDOID id, string prefab) in elites)
        {
            spawned.Write(id);
            spawned.Write(prefab);
        }
        spawned.Write(risen.Count);
        foreach ((ZDOID id, int place) in risen)
        {
            spawned.Write(id);
            spawned.Write(place);
        }
        spawned.Write(guarding.Count);
        foreach (ZDOID id in guarding) spawned.Write(id);
        spawned.Write(followers.Count);
        foreach (ZDOID id in followers) spawned.Write(id);
        ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.HoardBaneSpawned, spawned);
    }

    // One more follower, where the server says (the place furthest from the players), hunting at once.
    private static void RPC_Reinforce(long sender, ZPackage package)
    {
        if (!IncursionArena.FromServer(sender) || !ZNetScene.instance) return;
        int ticket = package.ReadInt();
        int encounter = package.ReadInt();
        Vector3 center = package.ReadVector3();
        Vector3 at = package.ReadVector3();
        if (encounter < 0 || encounter >= HoardBaneLoot.Encounters.Length) return;
        Character? follower = SpawnFollower(HoardBaneLoot.Encounters[encounter].Follower, center, at);
        if (!follower) return;
        ZPackage spawned = new();
        spawned.Write(ticket);
        spawned.Write(follower!.m_nview.GetZDO().m_uid);
        ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.HoardBaneReinforced, spawned);
    }

    // Asleep in its bubble, at the level its stars make it: the game's own health and damage for that level.
    private static Character? SpawnElite(HoardBaneLoot.Elite elite, Vector3 center, Vector3 post, int pack)
    {
        Character? character = Spawn(elite.Prefab, center, OnGround(post));
        if (!character) return null;
        Sleep(character!, EliteRole, post, pack);
        ZDO zdo = character!.m_nview.GetZDO();
        zdo.Set(EliteName, elite.Name);
        zdo.Set(EliteTint, elite.Tint);
        zdo.Set(EliteSize, elite.Size);
        if (elite.Color != Vector3.one) zdo.Set(EliteColor, elite.Color);
        if (elite.Holds is { } holds) Arm(character, item => NameHas(item, holds), holds);
        character.SetLevel(elite.Level);
        character.SetHealth(character.GetMaxHealth());
        return character;
    }

    // Asleep at its spot around its elite (post: the bubble's center), woken with it.
    private static Character? SpawnGuard(HoardBaneLoot.Guard guard, Vector3 center, Vector3 at, Vector3 post, int pack)
    {
        Character? character = Spawn(guard.Prefab, center, OnGround(at));
        if (!character) return null;
        Sleep(character!, GuardRole, post, pack);
        if (guard.Level > 1)
        {
            character!.SetLevel(guard.Level);
            character.SetHealth(character.GetMaxHealth());
        }
        if (guard.Archers) Arm(character!, IsBow, "bow");
        return character;
    }

    private static void Sleep(Character character, int role, Vector3 post, int pack)
    {
        ZDO zdo = character.m_nview.GetZDO();
        zdo.Set(Role, role);
        zdo.Set(Dormant, true);
        zdo.Set(Post, post);
        zdo.Set(PostRadius, BubbleRadius);
        zdo.Set(Pack, pack);
        BaseAI ai = character.GetBaseAI();
        if (ai) ai.SetPatrolPoint();
    }

    // A humanoid picks its weapons at random as it starts, from the seed saved on it (in every game alike): a seed that
    // gives it an item wanted (what: for the log), set before it starts. Nothing to do when it always carries one.
    private static void Arm(Character character, Func<GameObject?, bool> wanted, string what)
    {
        if (character is not Humanoid humanoid) return;
        foreach (GameObject item in humanoid.m_defaultItems)
            if (wanted(item)) return;
        for (int seed = 1; seed <= ArcherSeedTries; seed++)
        {
            if (!Picks(humanoid, seed, wanted)) continue;
            humanoid.m_seed = seed;
            humanoid.m_nview.GetZDO().Set(ZDOVars.s_seed, seed);
            return;
        }
        Plugin.ModLogger.LogWarning($"Hoard-Bane Rite: {character.name} can't be made to carry a {what}.");
    }

    // The same picks as the game's (Humanoid.GiveDefaultItems), in the same order.
    private static bool Picks(Humanoid humanoid, int seed, Func<GameObject?, bool> wanted)
    {
        UnityEngine.Random.State state = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(seed);
            if (humanoid.m_randomShield.Length != 0 && wanted(humanoid.m_randomShield[UnityEngine.Random.Range(0, humanoid.m_randomShield.Length)]))
                return true;
            if (humanoid.m_randomWeapon.Length != 0 && wanted(humanoid.m_randomWeapon[UnityEngine.Random.Range(0, humanoid.m_randomWeapon.Length)]))
                return true;
            if (humanoid.m_randomArmor.Length != 0 && wanted(humanoid.m_randomArmor[UnityEngine.Random.Range(0, humanoid.m_randomArmor.Length)]))
                return true;
            if (humanoid.m_randomSets.Length != 0)
                foreach (GameObject item in humanoid.m_randomSets[UnityEngine.Random.Range(0, humanoid.m_randomSets.Length)].m_items)
                    if (wanted(item)) return true;
            return false;
        }
        finally { UnityEngine.Random.state = state; }
    }

    private static bool NameHas(GameObject? prefab, string part) =>
        prefab && prefab!.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool IsBow(GameObject? prefab)
    {
        ItemDrop? item = prefab ? prefab!.GetComponent<ItemDrop>() : null;
        if (!item) return false;
        ItemDrop.ItemData.SharedData shared = item!.m_itemData.m_shared;
        return shared.m_itemType == ItemDrop.ItemData.ItemType.Bow || shared.m_skillType == Skills.SkillType.Bows;
    }

    // Shielded, a star stronger than the game's own and as strong as the server says on top (see RagnarokPatches for
    // the multipliers), hunting the players.
    private static Character? SpawnBoss(string prefab, Vector3 center, Vector3 at, float healthBy, float damageBy)
    {
        Character? character = Spawn(prefab, center, OnGround(at));
        if (!character) return null;
        ZDO zdo = character!.m_nview.GetZDO();
        zdo.Set(Role, BossRole);
        zdo.Set(Shield, true);
        zdo.Set(IncursionArena.BossHealth, healthBy);
        zdo.Set(IncursionArena.BossDamage, damageBy);
        zdo.Set(EliteTint, HoardBaneLoot.DefaultTint);
        character.SetLevel(BossLevel);
        // Its health was set up as it woke, before it was marked.
        character.SetMaxHealth(character.GetMaxHealthBase() * character.GetLevel());
        character.SetHealth(character.GetMaxHealth());
        Hunt(character, null);
        return character;
    }

    // Now and then a star or two, as the game spawns them: its own chance for each, never more than the game ever gives
    // that creature.
    private static Character? SpawnFollower(string prefab, Vector3 center, Vector3 at)
    {
        Character? character = Spawn(prefab, center, OnGround(at));
        if (!character) return null;
        character!.m_nview.GetZDO().Set(Role, FollowerRole);
        int most = MostLevel(prefab);
        float chance = SpawnSystem.GetLevelUpChance(at);
        int level = 1;
        while (level < most && UnityEngine.Random.Range(0f, 100f) <= chance) level++;
        if (level > 1)
        {
            character.SetLevel(level);
            character.SetHealth(character.GetMaxHealth());
        }
        Hunt(character, null);
        return character;
    }

    private static readonly Dictionary<string, int> MostLevels = [];

    // The highest level the game ever spawns this creature at: in the world, from spawners and from spawn areas (nests,
    // bone piles). 1 when the game doesn't spawn it at all.
    private static int MostLevel(string prefab)
    {
        if (MostLevels.TryGetValue(prefab, out int known)) return known;
        int most = 1;
        SpawnSystem? world = ZoneSystem.instance && ZoneSystem.instance.m_zoneCtrlPrefab
            ? ZoneSystem.instance.m_zoneCtrlPrefab.GetComponent<SpawnSystem>() : null;
        if (world)
            foreach (SpawnSystemList list in world!.m_spawnLists)
                if (list)
                    foreach (SpawnSystem.SpawnData data in list.m_spawners)
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
        Plugin.ModLogger.LogInfo($"Hoard-Bane Rite: followers {prefab} rise with up to {most - 1} stars.");
        return most;
    }

    private static Character? Spawn(string prefabName, Vector3 center, Vector3 at)
    {
        GameObject? prefab = ZNetScene.instance.GetPrefab(prefabName);
        if (!prefab || !prefab!.GetComponent<Character>())
        {
            Plugin.ModLogger.LogWarning($"Hoard-Bane Rite: can't spawn \"{prefabName}\": not a creature this game knows.");
            return null;
        }
        // Each its own way, as the game's spawns.
        Quaternion facing = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        GameObject spawned = Object.Instantiate(prefab, at, facing);
        ZNetView nview = spawned.GetComponent<ZNetView>();
        if (!nview || !nview.IsValid())
        {
            Plugin.ModLogger.LogWarning($"Hoard-Bane Rite: \"{prefabName}\" spawned without a network object.");
            Object.Destroy(spawned);
            return null;
        }
        return spawned.GetComponent<Character>();
    }

    // A little above the solid ground there (or the point itself, when the ground isn't known).
    internal static Vector3 OnGround(Vector3 point)
    {
        ZoneSystem zones = ZoneSystem.instance;
        if (zones && zones.GetSolidHeight(point, out float ground)) point.y = ground + 0.5f;
        return point;
    }

    private static void Hunt(Character character, Character? target)
    {
        BaseAI ai = character.GetBaseAI();
        if (!ai) return;
        ai.SetPatrolPoint();
        ai.Alert();
        ai.SetHuntPlayer(true);
        if (ai.IsAggravatable()) ai.SetAggravated(true, BaseAI.AggravatedReason.Damage);
        if (target && ai is MonsterAI monster) monster.m_targetCreature = target;
    }

    // ---- Elites waking ----

    // In the game that owns it: something hit it (see HoardBanePatches), or a player stepped into its bubble.
    internal static void Wake(Character elite, Character? by)
    {
        ZNetView nview = elite.m_nview;
        if (!nview || !nview.IsValid() || !nview.IsOwner() || !nview.GetZDO().GetBool(Dormant)) return;
        nview.GetZDO().Set(Dormant, false);
        Hunt(elite, by);
        // The rest of its pack this game owns wakes with it at once (the other games' see it on its saved state).
        int pack = nview.GetZDO().GetInt(Pack);
        if (pack == 0) return;
        foreach (Character other in Character.GetAllCharacters())
            if (other && other != elite && !other.IsDead() && other.m_nview && other.m_nview.IsOwner() && IsDormant(other) &&
                other.m_nview.GetZDO().GetInt(Pack) == pack)
                Wake(other, by);
    }

    private static readonly HashSet<int> AwakePacks = [];

    // The elites and guards this game owns wake when a living player is within their bubble, or when any of their pack
    // woke (in whichever game owns it: they see it on its saved state).
    private static void WakeNear()
    {
        List<Player> players = Player.GetAllPlayers();
        List<Character> characters = Character.GetAllCharacters();
        AwakePacks.Clear();
        foreach (Character character in characters)
        {
            if (!character || RoleOf(character) is not (EliteRole or GuardRole)) continue;
            ZDO zdo = character.m_nview.GetZDO();
            int pack = zdo.GetInt(Pack);
            if (pack != 0 && !zdo.GetBool(Dormant)) AwakePacks.Add(pack);
        }
        foreach (Character character in characters)
        {
            if (!character || character.IsDead() || !character.m_nview || !character.m_nview.IsOwner() || !IsDormant(character)) continue;
            ZDO zdo = character.m_nview.GetZDO();
            int pack = zdo.GetInt(Pack);
            if (pack != 0 && AwakePacks.Contains(pack))
            {
                Wake(character, null);
                continue;
            }
            Vector3 post = zdo.GetVec3(Post, character.transform.position);
            float reach = zdo.GetFloat(PostRadius, BubbleRadius);
            foreach (Player player in players)
            {
                if (!player || player.IsDead() || Utils.DistanceXZ(player.transform.position, post) > reach) continue;
                Wake(character, player);
                if (pack != 0) AwakePacks.Add(pack);
                break;
            }
        }
    }

    // ---- Shield ----

    private static void RPC_Unshield(long sender, Vector3 center)
    {
        if (!IncursionArena.FromServer(sender)) return;
        float reach = Radius + IncursionArena.SamePlace;
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!character || !character.m_nview || !character.m_nview.IsOwner() || !IsShielded(character)) continue;
            if (Utils.DistanceXZ(character.transform.position, center) > reach * 2f) continue;
            character.m_nview.GetZDO().Set(Shield, false);
        }
    }

    // The local player hit a shielded miniboss: its shield shows the hit, as the fuling shaman's does.
    internal static void Hit(Character boss, Vector3 at) => ShieldLook.Hit(boss, at);

    // An elite's own name ("Brond, the Iron-Hide"), or null for anything else.
    internal static string? NameOf(Character character)
    {
        if (RoleOf(character) != EliteRole) return null;
        string name = character.m_nview.GetZDO().GetString(EliteName);
        return name.Length > 0 ? name : null;
    }

    // ---- Every frame ----

    internal static void Update()
    {
        if (IncursionArena.Kind != WeatherKind.HoardBane)
        {
            Domes.Clear();
            Sleeping.Clear();
            ShieldLook.Clear();
            return;
        }
        bool wake = Time.time >= _nextWake;
        if (wake) _nextWake = Time.time + WakeInterval;
        bool decorate = Time.time >= _nextDecorate;
        if (decorate) _nextDecorate = Time.time + DecorateInterval;
        if (wake) WakeNear();

        Domes.Begin();
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!character || character.IsDead()) continue;
            int role = RoleOf(character);
            if (role == EliteRole)
            {
                ZDO zdo = character.m_nview.GetZDO();
                bool dormant = zdo.GetBool(Dormant);
                if (dormant)
                {
                    Domes.Add(zdo.GetVec3(Post, character.transform.position), zdo.GetFloat(PostRadius, BubbleRadius), BubbleFuel, NoHit);
                    Sleeping.Add(character);
                }
                else if (Sleeping.Remove(character) && IncursionArena.Here(WeatherState.Position(WeatherKind.HoardBane), WeatherKind.HoardBane))
                    PlayerMessages.Warning(string.Format(Messages.HoardBaneEliteWakes, Named(character)));
                if (decorate) Decorate(character);
            }
            else if (role == BossRole)
            {
                bool shielded = character.m_nview.GetZDO().GetBool(Shield);
                ShieldLook.Show(character, shielded);
                if (character.m_nview.IsOwner()) Pace(character, shielded);
                if (decorate) Decorate(character);
            }
            if (decorate && role != 0 && character.m_nview.IsOwner())
            {
                Fearless(character);
                if (!IsDormant(character)) Fierce(character);
            }
        }
        Domes.End();
        ShieldLook.Prune();
    }

    // A shielded miniboss keeps an even, slow pace: the players have time for the elites.
    private static void Pace(Character boss, bool shielded)
    {
        // By the prefab's hash: GetPrefabName makes a new string, and this runs every frame.
        GameObject? prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(boss.m_nview.GetZDO().GetPrefab()) : null;
        Character? usual = prefab ? prefab!.GetComponent<Character>() : null;
        if (!usual) return;
        float pace = Mathf.Min(usual!.m_runSpeed * ShieldedShare, ShieldedPace);
        boss.m_walkSpeed = shielded ? pace : usual.m_walkSpeed;
        boss.m_runSpeed = shielded ? pace : usual.m_runSpeed;
        boss.m_speed = shielded ? pace : usual.m_speed;
    }

    // What the rite brings never flees: not when hurt, nor from fire. Set in the game that owns it, which runs its AI.
    private static void Fearless(Character character)
    {
        if (character.GetBaseAI() is not MonsterAI ai) return;
        ai.m_fleeIfHurtWhenTargetCantBeReached = false;
        ai.m_fleeIfNotAlerted = false;
        ai.m_fleeIfLowHealth = 0f;
        ai.m_afraidOfFire = false;
        ai.m_avoidFire = false;
    }

    // Once awake it keeps coming for the players, even creatures that would only defend themselves (the lox): hunting,
    // alert, and with the nearest player as its target whenever it has none.
    private static void Fierce(Character character)
    {
        if (character.GetBaseAI() is not MonsterAI ai) return;
        ai.m_passiveAggresive = false;
        if (!ai.HuntPlayer()) ai.SetHuntPlayer(true);
        if (!ai.IsAlerted()) ai.Alert();
        if (ai.m_targetCreature && !ai.m_targetCreature.IsDead()) return;
        Player? prey = null;
        float nearest = PreyRange;
        foreach (Player player in Player.GetAllPlayers())
        {
            // Only those in the fight: inside the bubble.
            if (!player || player.IsDead() || player.InGhostMode() || player.InDebugFlyMode() || !IncursionArena.InBubble(player.transform.position)) continue;
            float distance = Vector3.Distance(player.transform.position, character.transform.position);
            if (distance >= nearest) continue;
            nearest = distance;
            prey = player;
        }
        if (prey) ai.m_targetCreature = prey;
    }

    // A dim red glow on the elites and minibosses, their own colors, and their size.
    private static void Decorate(Character creature)
    {
        Resize(creature);
        if (creature.transform.Find(GlowName)) return;
        GameObject glow = new(GlowName);
        glow.transform.SetParent(creature.transform, false);
        glow.transform.localPosition = Vector3.up * Mathf.Max(1f, creature.GetHeight() * 0.6f);
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.12f, 0.08f);
        light.range = Mathf.Clamp(creature.GetHeight() * 2.5f, 4f, 10f);
        light.intensity = 1.6f;
        light.shadows = LightShadows.None;
        (Vector3 shift, Vector3 color) = LookOf(creature);
        if (creature.m_visual) Tint(creature.m_visual, shift, color);
    }

    // How much bigger it is than its level makes it.
    internal static float LookScale(Character? character) => RoleOf(character) switch
    {
        EliteRole => character!.m_nview.GetZDO().GetFloat(EliteSize, HoardBaneLoot.DefaultSize),
        BossRole => BossScale,
        _ => 1f
    };

    // Bigger as a whole, as the game makes creatures bigger (world modifiers): the collider grows with the body, so hits,
    // the shield and the health bar fit what is seen. Set outright, never multiplied, so it is the same in every game and
    // however often it is done (some creatures share their scale between games).
    internal static void Resize(Character character)
    {
        float bigger = LookScale(character);
        if (bigger == 1f) return;
        Vector3 wanted = NaturalScale(character) * bigger;
        if (character.transform.localScale != wanted) character.transform.localScale = wanted;
    }

    // Its scale without the rite: the prefab's, with the world modifiers as the game sets them as it wakes. A creature
    // whose level look is on itself: that level's size (no more than the highest the game draws), as the game sets it.
    private static Vector3 NaturalScale(Character character)
    {
        LevelEffects? effects = character.GetComponentInChildren<LevelEffects>();
        int level = character.GetLevel();
        if (effects && effects!.transform == character.transform && level > 1 && effects.m_levelSetups.Count > 0)
        {
            float scale = effects.m_levelSetups[Mathf.Min(level, effects.m_levelSetups.Count + 1) - 2].m_scale;
            return new Vector3(scale, scale, scale);
        }
        GameObject? prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(Utils.GetPrefabName(character.gameObject)) : null;
        Vector3 natural = prefab ? prefab!.transform.localScale : Vector3.one;
        if (Game.m_enemySpeedSize != 1f && !character.InInterior()) natural *= Game.m_enemySpeedSize;
        if (Game.m_worldLevel > 0 && Game.instance) natural *= 1f + Game.m_worldLevel * Game.instance.m_worldLevelEnemyMoveSpeedMultiplier;
        return natural;
    }

    private static (Vector3 shift, Vector3 color) LookOf(Character character)
    {
        ZDO zdo = character.m_nview.GetZDO();
        return (zdo.GetVec3(EliteTint, HoardBaneLoot.DefaultTint), zdo.GetVec3(EliteColor, Vector3.one));
    }

    private static readonly int HueId = Shader.PropertyToID("_Hue");
    private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
    private static readonly int ValueId = Shader.PropertyToID("_Value");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    // Shifts the colors of every part of it the game can tint (as it tints creatures by level), from what they are now;
    // its own color times color. Also the Ragnarök Rite's bosses (see RagnarokLook).
    internal static void Tint(Character elite, Vector3 shift)
    {
        if (elite.m_visual) Tint(elite.m_visual, shift, Vector3.one);
    }

    private static void Tint(GameObject visual, Vector3 shift, Vector3 color)
    {
        MaterialPropertyBlock block = new();
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is not (SkinnedMeshRenderer or MeshRenderer)) continue;
            Material material = renderer.sharedMaterial;
            if (!material || !material.HasProperty(ValueId)) continue;
            renderer.GetPropertyBlock(block);
            if (material.HasProperty(HueId)) block.SetFloat(HueId, material.GetFloat(HueId) + shift.x);
            if (color != Vector3.one && material.HasProperty(ColorId))
            {
                Color own = material.GetColor(ColorId);
                block.SetColor(ColorId, new Color(own.r * color.x, own.g * color.y, own.b * color.z, own.a));
            }
            if (material.HasProperty(SaturationId)) block.SetFloat(SaturationId, material.GetFloat(SaturationId) + shift.y);
            block.SetFloat(ValueId, material.GetFloat(ValueId) + shift.z);
            renderer.SetPropertyBlock(block);
        }
    }

    // ---- Corpses ----

    // The creature dying now in this game (see HoardBanePatches.CorpsePatch): its corpse is made as it dies.
    internal static Character? Dying;

    // As the game makes an elite's or miniboss's corpse, in the game that owns it: the level it draws (no more than the
    // game draws, as for the creature), and that level's colors (the game finds none beyond the levels it draws).
    internal static void CorpseLevel(ref int level, ref float hue, ref float saturation, ref float value)
    {
        if (LookScale(Dying) == 1f) return;
        LevelEffects? effects = Dying!.GetComponentInChildren<LevelEffects>();
        int drawn = effects ? effects!.m_levelSetups.Count : 0;
        if (drawn == 0 || level - 1 <= drawn) return;
        level = drawn + 1;
        LevelEffects.LevelSetup setup = effects!.m_levelSetups[drawn - 1];
        hue = setup.m_hue;
        saturation = setup.m_saturation;
        value = setup.m_value;
    }

    // Right after: its size and colors saved on the corpse, for every game to dress it alike (DressCorpse). A corpse the
    // game made as big as the creature (it takes the creature's scale) is already as big; any other is made bigger.
    internal static void CorpseMade(Ragdoll ragdoll)
    {
        float scale = LookScale(Dying);
        if (scale == 1f || !ragdoll.m_nview || !ragdoll.m_nview.IsValid()) return;
        (Vector3 shift, Vector3 color) = LookOf(Dying!);
        Vector3 size = TakesScale(Dying!, ragdoll) ? ragdoll.transform.localScale : ragdoll.transform.localScale * scale;
        ZDO zdo = ragdoll.m_nview.GetZDO();
        zdo.Set(CorpseSize, size);
        zdo.Set(EliteTint, shift);
        if (color != Vector3.one) zdo.Set(EliteColor, color);
        DressCorpse(ragdoll);
    }

    // Whether the game made this corpse with the creature's own scale (see EffectList.Create).
    private static bool TakesScale(Character creature, Ragdoll ragdoll)
    {
        string name = Utils.GetPrefabName(ragdoll.gameObject);
        foreach (EffectList.EffectData data in creature.m_deathEffects.m_effectPrefabs)
            if (data.m_enabled && data.m_prefab && data.m_prefab.name == name) return data.m_inheritParentScale;
        return false;
    }

    // In every game: as big as the creature was, and its colors. In the game that made it, after the game set it up; in
    // the others, as it appears (already set up from what is saved on it).
    internal static void DressCorpse(Ragdoll ragdoll)
    {
        if (!ragdoll.m_nview || !ragdoll.m_nview.IsValid()) return;
        ZDO zdo = ragdoll.m_nview.GetZDO();
        Vector3 size = zdo.GetVec3(CorpseSize, Vector3.zero);
        if (size == Vector3.zero) return;
        ragdoll.transform.localScale = size;
        Tint(ragdoll.gameObject, zdo.GetVec3(EliteTint, HoardBaneLoot.DefaultTint), zdo.GetVec3(EliteColor, Vector3.one));
    }

    // ---- Falling ----

    // In the game that owns a creature, as it dies.
    internal static void CreatureDied(Character character)
    {
        int role = RoleOf(character);
        if (role is not (EliteRole or BossRole) || !character.m_nview.IsOwner()) return;
        ZRoutedRpc.instance?.InvokeRoutedRPC(Rpcs.HoardBaneDown, character.m_nview.GetZDO().m_uid);
    }

    // ---- What everyone sees ----

    private static void RPC_Begun(long sender, ZPackage package)
    {
        if (!IncursionArena.FromServer(sender)) return;
        List<string> bosses = ReadNames(package);
        int elites = package.ReadInt();
        Vector3 center = package.ReadVector3();
        if (!IncursionArena.Here(center, WeatherKind.HoardBane)) return;
        PlayerMessages.Warning(string.Format(Messages.HoardBaneBegins, Together(bosses)));
    }

    private static void RPC_News(long sender, ZPackage package)
    {
        if (!IncursionArena.FromServer(sender)) return;
        HoardBaneRush.News news = (HoardBaneRush.News)package.ReadInt();
        string prefab = package.ReadString();
        int which = package.ReadInt();
        int of = package.ReadInt();
        Vector3 center = package.ReadVector3();
        int strikes = package.ReadInt();
        for (int i = 0; i < strikes; i++)
        {
            Vector3 at = package.ReadVector3();
            if (Near(at)) BlessingLightning.Strike(at);
        }
        List<string> bosses = ReadNames(package);
        if (!IncursionArena.Here(center, WeatherKind.HoardBane)) return;
        string? message = news switch
        {
            HoardBaneRush.News.EliteFell => string.Format(Messages.HoardBaneEliteFalls, Name(prefab)),
            HoardBaneRush.News.ShieldBroke => string.Format(Messages.HoardBaneShieldBreaks, Together(bosses)),
            HoardBaneRush.News.BossFell => string.Format(Messages.HoardBaneBossFalls, Name(prefab)),
            HoardBaneRush.News.BossReturns => string.Format(Messages.HoardBaneBossReturns, Name(prefab)),
            _ => null
        };
        if (message != null) PlayerMessages.Warning(message);
    }

    // What treasure was won isn't told: it waits in the obliterator. Failed, treasure: the chest burnt waits there instead.
    private static void RPC_Ended(long sender, int ending, bool treasure, Vector3 center)
    {
        if (!IncursionArena.FromServer(sender)) return;
        IncursionArena.ClearInside(center, Radius, Messages.HoardBaneName);
        HoardBaneRush.Ending how = (HoardBaneRush.Ending)ending;
        if (how == HoardBaneRush.Ending.Survived && Near(center)) BlessingLightning.Strike(center);
        if (!IncursionArena.WasInside(center) && !IncursionArena.Here(center, WeatherKind.HoardBane))
        {
            IncursionArena.TellFar(WeatherKind.HoardBane, how == HoardBaneRush.Ending.Survived);
            return;
        }
        string failed = how switch
        {
            HoardBaneRush.Ending.Survived when treasure => Messages.HoardBaneSurvived,
            HoardBaneRush.Ending.Survived => Messages.HoardBaneSurvivedNoReward,
            HoardBaneRush.Ending.Obliterator => Messages.HoardBaneFailedObliterator,
            HoardBaneRush.Ending.Fallen => Messages.HoardBaneFailedFallen,
            HoardBaneRush.Ending.TimeUp => Messages.HoardBaneFailedTime,
            HoardBaneRush.Ending.NoOne => Messages.HoardBaneNoOne,
            HoardBaneRush.Ending.NoSpawn => Messages.HoardBaneFailedSpawn,
            _ => Messages.HoardBaneFailedEnded
        };
        PlayerMessages.Warning(how != HoardBaneRush.Ending.Survived && treasure ? failed + "\n" + Messages.HoardBaneChestReturned : failed);
    }

    // The rite failed with no obliterator left to hold the chest this player burnt: back in their inventory, or at their
    // feet when it's full.
    private static void RPC_ChestBack(long sender, string chest)
    {
        if (!IncursionArena.FromServer(sender) || !Player.m_localPlayer || !ObjectDB.instance) return;
        GameObject? prefab = ObjectDB.instance.GetItemPrefab(chest);
        ItemDrop? drop = prefab ? prefab!.GetComponent<ItemDrop>() : null;
        if (!drop) return;
        Player player = Player.m_localPlayer;
        Inventory inventory = player.GetInventory();
        if (!inventory.CanAddItem(prefab, 1) || !inventory.AddItem(prefab, 1))
            ItemDrop.DropItem(drop!.m_itemData, 1, player.transform.position + Vector3.up, Quaternion.identity);
        PlayerMessages.Warning(Messages.HoardBaneChestBack);
        Plugin.ModLogger.LogInfo($"Hoard-Bane Rite: {chest} is back with the player who burnt it.");
    }

    private static List<string> ReadNames(ZPackage package)
    {
        int count = package.ReadInt();
        List<string> names = [];
        for (int i = 0; i < count; i++) names.Add(package.ReadString());
        return names;
    }

    private static bool Near(Vector3 point) =>
        Player.m_localPlayer && Utils.DistanceXZ(Player.m_localPlayer.transform.position, point) <= Radius + StrikeSeenFrom;

    // "Zil & Thungr".
    private static string Together(List<string> prefabs) => string.Join(Messages.HoardBaneAnd, prefabs.ConvertAll(Name));

    // A creature's name in the player's language: "Brenna".
    private static string Name(string prefabName)
    {
        GameObject? prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(prefabName) : null;
        Character? character = prefab ? prefab!.GetComponent<Character>() : null;
        string name = character ? character!.m_name : prefabName;
        return Localization.instance != null ? Localization.instance.Localize(name) : name;
    }

    private static string Named(Character character) =>
        Localization.instance != null ? Localization.instance.Localize(character.GetHoverName()) : character.GetHoverName();
}

// Shield domes drawn by the game's shield dome effect, each standing for nothing but itself (as IncursionArena's bubble):
// as many as were added since Begin, the rest taken away at End.
internal sealed class DomeSet(string name)
{
    private const float FindInterval = 1f;
    private readonly List<ShieldGenerator> _keys = [];
    private GameObject? _holder;
    private ShieldDomeImageEffect? _effect;
    private float _nextFind;
    private int _used;
    private int _shown;

    internal void Begin() => _used = 0;

    internal void Add(Vector3 at, float radius, float fuel, float lastHit)
    {
        ShieldDomeImageEffect? effect = Effect();
        if (!effect) return;
        if (_used >= _keys.Count) _keys.Add(NewKey());
        effect!.SetShieldData(_keys[_used++], at, radius, fuel, lastHit);
    }

    internal void End()
    {
        if (_effect)
            for (int i = _used; i < _shown && i < _keys.Count; i++) _effect!.RemoveShield(_keys[i]);
        _shown = _used;
    }

    internal void Clear()
    {
        if (_shown == 0) return;
        _used = 0;
        End();
    }

    private ShieldDomeImageEffect? Effect()
    {
        if (_effect) return _effect;
        // The camera's effect was replaced (a new scene): the domes start over.
        _shown = 0;
        if (Time.time < _nextFind) return null;
        _nextFind = Time.time + FindInterval;
        _effect = Object.FindFirstObjectByType<ShieldDomeImageEffect>();
        return _effect;
    }

    private ShieldGenerator NewKey()
    {
        if (!_holder)
        {
            _holder = new GameObject(name);
            _holder.SetActive(false);
            Object.DontDestroyOnLoad(_holder);
        }
        return _holder!.AddComponent<ShieldGenerator>();
    }
}

// The minibosses' shield, as every player sees it: the fuling shaman's own shield (its status effect's look; the Staff of
// Protection's if the game has no such), made in each game for itself, as the shield there only stands for the rite's.
// Hits on it show as they do on the shaman's.
internal static class ShieldLook
{
    private static readonly string[] Effects = ["GoblinShaman_shield", "Staff_shield"];
    private const float HitSeconds = 3f;

    private static readonly Dictionary<Character, GameObject[]> Shown = [];
    private static readonly List<Character> Gone = [];
    private static SE_Shield? _effect;
    private static bool _looked;

    internal static void Clear()
    {
        foreach (GameObject[] made in Shown.Values) Destroy(made);
        Shown.Clear();
    }

    // Every frame, for each miniboss in this game.
    internal static void Show(Character boss, bool shielded)
    {
        bool has = Shown.TryGetValue(boss, out GameObject[] made);
        if (shielded == has) return;
        if (!shielded)
        {
            Destroy(made);
            Shown.Remove(boss);
            return;
        }
        SE_Shield? effect = Effect();
        Shown[boss] = effect ? Make(effect!.m_startEffects, boss, boss.GetCenterPoint(), attachAll: true) : [];
    }

    // The minibosses gone, dead or no longer here.
    internal static void Prune()
    {
        Gone.Clear();
        foreach (Character boss in Shown.Keys)
            if (!boss || boss.IsDead()) Gone.Add(boss!);
        foreach (Character boss in Gone)
        {
            Destroy(Shown[boss]);
            Shown.Remove(boss);
        }
    }

    internal static void Hit(Character boss, Vector3 at)
    {
        SE_Shield? effect = Effect();
        if (!effect) return;
        foreach (GameObject made in Make(effect!.m_hitEffects, boss, at, attachAll: false))
            if (made) UnityEngine.Object.Destroy(made, HitSeconds);
    }

    private static SE_Shield? Effect()
    {
        if (_effect || _looked && !ObjectDB.instance) return _effect;
        if (!ObjectDB.instance) return null;
        _looked = true;
        foreach (string name in Effects)
            if (ObjectDB.instance.GetStatusEffect(name.GetStableHashCode()) is SE_Shield shield)
                return _effect = shield;
        return null;
    }

    // As the game makes a status effect's look on a creature, but only in this game: nothing on it is networked.
    private static GameObject[] Make(EffectList effects, Character on, Vector3 at, bool attachAll)
    {
        List<GameObject> made = [];
        float scale = on.GetRadius() * 2f;
        GameObject holder = new("TempestRite_Shield");
        holder.SetActive(false);
        try
        {
            foreach (EffectList.EffectData data in effects.m_effectPrefabs)
            {
                if (!data.m_enabled || !data.m_prefab) continue;
                GameObject made1 = UnityEngine.Object.Instantiate(data.m_prefab, at, on.transform.rotation, holder.transform);
                Strip<ZSyncTransform>(made1);
                Strip<ZNetView>(made1);
                Strip<TimedDestruction>(made1);
                if (data.m_scale)
                    made1.transform.localScale = data.m_inheritParentScale ? on.transform.localScale * scale : Vector3.one * scale;
                else if (data.m_inheritParentScale) made1.transform.localScale = on.transform.localScale;
                made1.transform.SetParent(attachAll || data.m_attach ? on.transform : null, true);
                made.Add(made1);
            }
        }
        finally { UnityEngine.Object.Destroy(holder); }
        return [.. made];
    }

    private static void Strip<T>(GameObject made) where T : Component
    {
        foreach (T component in made.GetComponentsInChildren<T>(true)) UnityEngine.Object.DestroyImmediate(component);
    }

    private static void Destroy(GameObject[] made)
    {
        foreach (GameObject one in made)
            if (one) UnityEngine.Object.Destroy(one);
    }
}
