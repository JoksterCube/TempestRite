using System;
using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Patches;

// The Glacial Satiety Rite: the game fades each food from all it gives down to about half, then it runs out. While the
// blessing lasts, no food counts for less than the Food Floor Percent of its full value.
[HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
internal static class GlacialSatietyPatch
{
    private const string Feature = "Glacial Satiety Rite";

    private static void Postfix(Player __instance, ref float hp, ref float stamina, ref float eitr)
    {
        if (PatchGuard.IsDisabled(Feature) || __instance != Player.m_localPlayer || !Blessings.IsActive(WeatherKind.GlacialSatiety)) return;
        try
        {
            float floor = Mathf.Clamp01(PluginConfig.FoodFloorPercent.Value / 100f);
            foreach (Player.Food food in __instance.m_foods)
            {
                ItemDrop.ItemData.SharedData shared = food.m_item.m_shared;
                hp += Mathf.Max(0f, shared.m_food * floor - food.m_health);
                stamina += Mathf.Max(0f, shared.m_foodStamina * floor - food.m_stamina);
                eitr += Mathf.Max(0f, shared.m_foodEitr * floor - food.m_eitr);
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// Death ends every blessing. Under the Grave-Bind Rite the skills aren't lowered, and the tombstone gets a beam.
internal static class DeathPatches
{
    private const string Feature = "Blessings at death";
    // Set while the local player's death is worked out, when the Grave-Bind Rite was on.
    private static bool _graveBound;
    private static bool _makesTombstone;
    private static Vector3 _tombstoneAt;

    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    private static class Dying
    {
        // Before the game marks the player dead (blessings only count for the living).
        private static void Prefix(Player __instance)
        {
            _graveBound = false;
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                _graveBound = __instance == Player.m_localPlayer && __instance.m_nview.IsOwner() && Blessings.IsActive(WeatherKind.GraveBind);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Postfix(Player __instance)
        {
            _graveBound = false;
            if (PatchGuard.IsDisabled(Feature)) return;
            try { Blessings.Died(__instance); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
    private static class KeepSkills
    {
        private static bool Prefix(Skills __instance)
        {
            if (!_graveBound || __instance.m_player != Player.m_localPlayer) return true;
            PlayerMessages.TopLeft(Messages.GraveBindKept);
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
    private static class Tombstone
    {
        // The same check the game makes: a tombstone only when something is left to put in it.
        private static void Prefix(Player __instance)
        {
            _makesTombstone = _graveBound && __instance.m_inventory.NrOfItems() != 0 && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathKeepInventory);
            if (_makesTombstone) _tombstoneAt = __instance.GetCenterPoint();
        }

        private static void Postfix()
        {
            if (!_makesTombstone) return;
            _makesTombstone = false;
            try { GraveBeacon.Place(_tombstoneAt); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}

// The Vein-Shatter Rite, worked out by the striking player's game: each hit of theirs on an ore vein also deals part of
// it to the pieces right next to the one hit, and once the vein is worn down to the Shatter At Percent of its health,
// every piece left gets a hit that breaks it. Those hits go to the vein's owner like any other, so drops and effects
// are as usual. The pickaxe wears for each of them as for a hit of that size.
[HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Damage))]
internal static class VeinShatterPatch
{
    private const string Feature = "Vein-Shatter Rite";
    // Pieces whose bounds come this close count as next to each other.
    private const float NeighborGap = 0.6f;
    // A vein shattered is left alone for a moment, while its owner breaks the pieces.
    private const float ShatterPause = 3f;

    private static readonly Dictionary<ZDOID, float> Shattered = [];
    private static readonly Dictionary<int, float> Planned = [];

    internal static void ClearSession() => Shattered.Clear();

    // The hit as it was, before the game sends it on.
    private static void Prefix(HitData hit, out HitData? __state)
    {
        __state = null;
        if (PatchGuard.IsDisabled(Feature) || hit == null || hit.m_hitCollider == null || hit.m_radius > 0f) return;
        try
        {
            if (hit.GetAttacker() == Player.m_localPlayer && Blessings.IsActive(WeatherKind.VeinShatter)) __state = hit.Clone();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Postfix(MineRock5 __instance, HitData hit, HitData? __state)
    {
        if (__state == null || PatchGuard.IsDisabled(Feature)) return;
        try { Spread(__instance, hit.m_hitCollider, __state); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Spread(MineRock5 rock, Collider hitCollider, HitData hit)
    {
        ZNetView view = rock.m_nview;
        if (!view || !view.IsValid() || rock.m_hitAreas == null || !IsVein(rock) || !hit.CheckToolTier(rock.m_minToolTier)) return;
        int index = rock.GetAreaIndex(hitCollider);
        float damage = hit.GetTotalDamage();
        if (index < 0 || damage <= 0f) return;
        ZDOID id = view.GetZDO().m_uid;
        if (Shattered.TryGetValue(id, out float at) && Time.time - at < ShatterPause) return;

        rock.LoadHealth();
        List<MineRock5.HitArea> areas = rock.m_hitAreas;
        MineRock5.HitArea struck = areas[index];
        if (struck.m_health <= 0f || !struck.m_collider) return;

        // What each piece is about to take: the hit itself, then the spread to its neighbors.
        Planned.Clear();
        Planned[index] = damage;
        float spread = Mathf.Clamp01(PluginConfig.VeinSpreadPercent.Value / 100f);
        if (spread > 0f)
        {
            Bounds near = struck.m_collider.bounds;
            near.Expand(NeighborGap * 2f);
            for (int i = 0; i < areas.Count; i++)
            {
                MineRock5.HitArea area = areas[i];
                if (i == index || area.m_health <= 0f || !area.m_collider || !near.Intersects(area.m_collider.bounds)) continue;
                Planned[i] = damage * spread;
            }
        }

        // Health left in the whole vein once these land (counted with the hit's own damage; the vein may resist some).
        float full = rock.m_health * areas.Count, left = 0f;
        for (int i = 0; i < areas.Count; i++)
        {
            float health = Mathf.Max(0f, areas[i].m_health);
            left += Planned.TryGetValue(i, out float planned) ? Mathf.Max(0f, health - planned) : health;
        }
        float shatterAt = Mathf.Clamp01(PluginConfig.VeinShatterAtPercent.Value / 100f);
        bool shatter = shatterAt > 0f && left <= full * shatterAt;
        if (shatter)
        {
            Shattered[id] = Time.time;
            // Every piece left takes more than it has, so it breaks even if the vein resists some.
            for (int i = 0; i < areas.Count; i++)
                if (areas[i].m_health > 0f) Planned[i] = Mathf.Max(Planned.TryGetValue(i, out float planned) ? planned : 0f, areas[i].m_health * 3f + damage);
        }

        float extraHits = 0f;
        foreach (KeyValuePair<int, float> plan in Planned)
        {
            if (plan.Key == index && !shatter) continue;
            float extra = plan.Key == index ? plan.Value - damage : plan.Value;
            if (extra <= 0f) continue;
            HitData copy = hit.Clone();
            copy.m_damage.Modify(extra / damage);
            copy.m_hitCollider = null;
            copy.m_point = areas[plan.Key].m_collider ? areas[plan.Key].m_collider.bounds.center : hit.m_point;
            view.InvokeRPC("RPC_Damage", copy, plan.Key);
            // The pickaxe wears for what the piece actually had, not the overkill that makes sure it breaks.
            extraHits += Mathf.Min(extra, Mathf.Max(0f, areas[plan.Key].m_health)) / damage;
        }
        Wear(extraHits);
    }

    private static void Wear(float extraHits)
    {
        Player? player = Player.m_localPlayer;
        ItemDrop.ItemData? tool = player ? player!.GetCurrentWeapon() : null;
        if (tool == null || !tool.m_shared.m_useDurability || extraHits <= 0f) return;
        tool.m_durability = Mathf.Max(0f, tool.m_durability - tool.m_shared.m_useDurabilityDrain * Game.m_durabilityRate * extraHits);
    }

    // A vein: it drops one of the listed items.
    private static bool IsVein(MineRock5 rock)
    {
        if (rock.m_dropItems == null) return false;
        HashSet<int> veins = PluginConfig.VeinPrefabs();
        foreach (DropTable.DropData drop in rock.m_dropItems.m_drops)
            if (drop.m_item && veins.Contains(drop.m_item.name.GetStableHashCode())) return true;
        return false;
    }
}

// The Fortune's Boon Rite.
// - A creature killed by a blessed player drops the most of each item it can, and trophies more often (up to always).
//   Worked out by the game that owns the creature, which reads the blessing off the killer.
// - A tree felled (or a log broken) by a blessed player always drops the listed items it can drop: fine wood, seeds.
internal static class FortunePatches
{
    private const string Feature = "Fortune's Boon Rite";

    private readonly struct Saved(CharacterDrop.Drop drop, int amountMin, float chance)
    {
        internal CharacterDrop.Drop Drop { get; } = drop;
        internal int AmountMin { get; } = amountMin;
        internal float Chance { get; } = chance;
    }

    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    private static class CreatureDrops
    {
        private static void Prefix(CharacterDrop __instance, out List<Saved>? __state)
        {
            __state = null;
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                Character? killer = __instance.m_character ? __instance.m_character.m_lastHit?.GetAttacker() : null;
                if (!Blessings.Has(killer, WeatherKind.FortunesBoon)) return;
                float multiplier = Mathf.Max(1f, PluginConfig.TrophyMultiplier.Value);
                __state = [];
                foreach (CharacterDrop.Drop drop in __instance.m_drops)
                {
                    if (drop.m_prefab == null) continue;
                    __state.Add(new Saved(drop, drop.m_amountMin, drop.m_chance));
                    drop.m_amountMin = Mathf.Max(drop.m_amountMin, drop.m_amountMax);
                    if (IsTrophy(drop.m_prefab)) drop.m_chance = Mathf.Min(1f, drop.m_chance * multiplier);
                }
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        // Put back as they were, also if something went wrong.
        private static Exception? Finalizer(Exception? __exception, List<Saved>? __state)
        {
            if (__state != null)
                foreach (Saved saved in __state)
                {
                    saved.Drop.m_amountMin = saved.AmountMin;
                    saved.Drop.m_chance = saved.Chance;
                }
            return __exception;
        }
    }

    private static bool IsTrophy(GameObject prefab) =>
        prefab.GetComponent<ItemDrop>() is { } item && item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy;

    // Set while a tree or log felled by a blessed player drops its items.
    private static int _felling;

    private static void Enter(HitData? hit)
    {
        if (!PatchGuard.IsDisabled(Feature) && hit != null && Blessings.Has(hit.GetAttacker(), WeatherKind.FortunesBoon)) _felling++;
    }

    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Destroy), typeof(HitData), typeof(bool))]
    private static class Logs
    {
        private static void Prefix(HitData hitData, out bool __state)
        {
            int before = _felling;
            Enter(hitData);
            __state = _felling > before;
        }

        private static Exception? Finalizer(Exception? __exception, bool __state)
        {
            if (__state) _felling--;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.RPC_Damage))]
    private static class Trees
    {
        private static void Prefix(HitData hit, out bool __state)
        {
            int before = _felling;
            Enter(hit);
            __state = _felling > before;
        }

        private static Exception? Finalizer(Exception? __exception, bool __state)
        {
            if (__state) _felling--;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(DropTable), nameof(DropTable.GetDropList), new Type[0])]
    private static class TreeDrops
    {
        private static void Postfix(DropTable __instance, List<GameObject> __result)
        {
            if (_felling <= 0 || __result == null) return;
            try
            {
                HashSet<int> wanted = PluginConfig.TreePrefabs();
                foreach (DropTable.DropData drop in __instance.m_drops)
                {
                    if (!drop.m_item || !wanted.Contains(drop.m_item.name.GetStableHashCode()) || __result.Contains(drop.m_item)) continue;
                    for (int i = Mathf.Max(1, drop.m_stackMin); i > 0; i--) __result.Add(drop.m_item);
                }
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}

// The Abyssal Surge Rite, in the game running a zone's spawning: serpents get more chances to spawn, fish come in
// bigger schools and roll more stars. Put back as they were right after.
internal static class AbyssalSurgePatches
{
    private const string Feature = "Abyssal Surge Rite";
    private static readonly int Serpent = "Serpent".GetStableHashCode();

    private readonly struct Saved(SpawnSystem.SpawnData data)
    {
        internal SpawnSystem.SpawnData Data { get; } = data;
        internal float Chance { get; } = data.m_spawnChance;
        internal int GroupMin { get; } = data.m_groupSizeMin;
        internal int GroupMax { get; } = data.m_groupSizeMax;
        internal int MaxSpawned { get; } = data.m_maxSpawned;
    }

    private static bool IsFish(SpawnSystem.SpawnData data) => data.m_prefab && data.m_prefab.GetComponent<Fish>();

    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawnList))]
    private static class SpawnLists
    {
        private static void Prefix(List<SpawnSystem.SpawnData> spawners, out List<Saved>? __state)
        {
            __state = null;
            if (PatchGuard.IsDisabled(Feature) || !WeatherState.IsActive(WeatherKind.AbyssalSurge)) return;
            try
            {
                float serpents = Mathf.Max(1f, PluginConfig.SerpentMultiplier.Value);
                float fish = Mathf.Max(1f, PluginConfig.FishMultiplier.Value);
                foreach (SpawnSystem.SpawnData data in spawners)
                {
                    if (!data.m_prefab) continue;
                    if (data.m_prefab.name.GetStableHashCode() == Serpent)
                    {
                        (__state ??= []).Add(new Saved(data));
                        data.m_spawnChance = Mathf.Min(100f, data.m_spawnChance * serpents);
                    }
                    else if (IsFish(data))
                    {
                        (__state ??= []).Add(new Saved(data));
                        data.m_groupSizeMin = Mathf.Max(1, Mathf.RoundToInt(data.m_groupSizeMin * fish));
                        data.m_groupSizeMax = Mathf.Max(data.m_groupSizeMin, Mathf.RoundToInt(data.m_groupSizeMax * fish));
                        if (data.m_maxSpawned > 0) data.m_maxSpawned = Mathf.RoundToInt(data.m_maxSpawned * fish);
                    }
                }
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static Exception? Finalizer(Exception? __exception, List<Saved>? __state)
        {
            if (__state != null)
                foreach (Saved saved in __state)
                {
                    saved.Data.m_spawnChance = saved.Chance;
                    saved.Data.m_groupSizeMin = saved.GroupMin;
                    saved.Data.m_groupSizeMax = saved.GroupMax;
                    saved.Data.m_maxSpawned = saved.MaxSpawned;
                }
            return __exception;
        }
    }

    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.GetLevelUpChance), typeof(Vector3), typeof(SpawnSystem.SpawnData))]
    private static class BigFish
    {
        private static void Postfix(SpawnSystem.SpawnData creature, ref float __result)
        {
            if (PatchGuard.IsDisabled(Feature) || !WeatherState.IsActive(WeatherKind.AbyssalSurge) || creature == null || !IsFish(creature)) return;
            __result = Mathf.Min(100f, __result * Mathf.Max(1f, PluginConfig.BigFishMultiplier.Value));
        }
    }
}
