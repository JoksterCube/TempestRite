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
            float floor = Mathf.Clamp01(PluginConfig.FoodFloor.Value);
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
            if (!_makesTombstone) return;
            _tombstoneAt = __instance.GetCenterPoint();
            // Before the game takes the gear off.
            try { GraveGear.Remember(__instance, _tombstoneAt); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Postfix()
        {
            if (!_makesTombstone) return;
            _makesTombstone = false;
            try { GraveBeacon.Place(_tombstoneAt); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Take All on the Grave-Bind tombstone: everything is in the inventory now.
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.OnTakeAllSuccess))]
    private static class GraveTakenAll
    {
        private static void Postfix(TombStone __instance)
        {
            if (GraveGear.IsTheGrave(__instance)) GraveGear.Reequip();
        }
    }

    // The Grave-Bind tombstone emptied by hand: the game removes it once it is empty and closed, in the game holding it
    // (the looter's, as opening it hands it over).
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.UpdateDespawn))]
    private static class GraveEmptied
    {
        private static void Prefix(TombStone __instance)
        {
            if (!__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()) return;
            if (__instance.m_container.IsInUse() || __instance.m_container.GetInventory().NrOfItems() > 0) return;
            if (GraveGear.IsTheGrave(__instance)) GraveGear.Reequip();
        }
    }
}

// The Vein-Shatter Rite works on every mineable that breaks in pieces: rocks, ore veins, mud piles, ice, the giant
// swords and skulls of the Mistlands (the game's MineRock5 and MineRock), but those on the Excluded list. It is worked
// out by the game that owns the deposit (it applies every hit, so its health is the true one): each hit of a blessed
// player also deals part of it to the pieces right next to the one hit. After every hit (pieces broken by it or falling
// off unsupported included), once the deposit is down to the Shatter At Percent of its health, every piece left breaks,
// with drops and effects as usual. The spread costs the pickaxe nothing more; a shatter wears it as for one hit per
// other piece it breaks (told to the striking player's game).
internal static class VeinShatterPatch
{
    private const string Feature = "Vein-Shatter Rite";
    // Pieces whose bounds come this close count as next to each other.
    private const float NeighborGap = 0.6f;

    // A deposit made of pieces, whichever of the game's two kinds it is.
    private abstract class Deposit
    {
        internal abstract ZNetView View { get; }
        internal abstract int Count { get; }
        internal abstract int MinToolTier { get; }
        internal abstract DropTable? Drops { get; }
        // What each piece has when whole.
        internal abstract float Full { get; }
        internal abstract void Load();
        internal abstract float Health(int index);
        internal abstract Collider? Collider(int index);
        // Hits a piece as the game does on the owner's side.
        internal abstract void Hit(HitData hit, int index);
    }

    private sealed class Fractured(MineRock5 rock) : Deposit
    {
        internal override ZNetView View => rock.m_nview;
        internal override int Count => rock.m_hitAreas?.Count ?? 0;
        internal override int MinToolTier => rock.m_minToolTier;
        internal override DropTable? Drops => rock.m_dropItems;
        internal override float Full => rock.m_health + Game.m_worldLevel * rock.m_health * Game.instance.m_worldLevelMineHPMultiplier;
        internal override void Load() => rock.LoadHealth();
        internal override float Health(int index) => rock.m_hitAreas[index].m_health;
        internal override Collider? Collider(int index) => rock.m_hitAreas[index].m_collider;
        internal override void Hit(HitData hit, int index) => rock.RPC_Damage(0L, hit, index);
    }

    private sealed class Parted(MineRock rock) : Deposit
    {
        internal override ZNetView View => rock.m_nview;
        internal override int Count => rock.m_hitAreas?.Length ?? 0;
        internal override int MinToolTier => rock.m_minToolTier;
        internal override DropTable? Drops => rock.m_dropItems;
        internal override float Full => rock.GetHealth();
        internal override void Load() { }
        internal override float Health(int index) => View.GetZDO().GetFloat("Health" + index, rock.GetHealth());
        internal override Collider? Collider(int index) => rock.m_hitAreas[index];
        internal override void Hit(HitData hit, int index) => rock.RPC_Hit(0L, hit, index);
    }

    // The hit as it came, before the game applies the deposit's resistances, and the pieces next to the one hit.
    private sealed class Strike(HitData hit, Player attacker, float before, List<int> neighbors)
    {
        internal HitData Hit { get; } = hit;
        internal Player Attacker { get; } = attacker;
        internal float Before { get; } = before;
        internal List<int> Neighbors { get; } = neighbors;
    }

    // Hits this patch deals itself are not spread or checked again.
    private static bool _working;

    internal static void Register(ZRoutedRpc rpc) => rpc.Register<int>(Rpcs.VeinWear, RPC_VeinWear);

    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.RPC_Damage))]
    private static class FracturedHit
    {
        private static void Prefix(MineRock5 __instance, HitData hit, int hitAreaIndex, out object? __state) =>
            __state = Before(new Fractured(__instance), hit, hitAreaIndex);

        private static void Postfix(MineRock5 __instance, int hitAreaIndex, object? __state) =>
            After(new Fractured(__instance), hitAreaIndex, __state);
    }

    [HarmonyPatch(typeof(MineRock), nameof(MineRock.RPC_Hit))]
    private static class PartedHit
    {
        private static void Prefix(MineRock __instance, HitData hit, int hitAreaIndex, out object? __state) =>
            __state = Before(new Parted(__instance), hit, hitAreaIndex);

        private static void Postfix(MineRock __instance, int hitAreaIndex, object? __state) =>
            After(new Parted(__instance), hitAreaIndex, __state);
    }

    private static Strike? Before(Deposit deposit, HitData hit, int index)
    {
        if (_working || PatchGuard.IsDisabled(Feature) || hit == null) return null;
        try { return Read(deposit, hit, index); }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return null;
        }
    }

    private static void After(Deposit deposit, int index, object? state)
    {
        if (state is not Strike strike || PatchGuard.IsDisabled(Feature)) return;
        _working = true;
        try { Work(deposit, index, strike); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        finally { _working = false; }
    }

    private static Strike? Read(Deposit deposit, HitData hit, int index)
    {
        ZNetView view = deposit.View;
        if (!view || !view.IsValid() || !view.IsOwner() || index < 0 || index >= deposit.Count) return null;
        if (hit.GetAttacker() is not Player attacker || !Blessings.Has(attacker, WeatherKind.VeinShatter)) return null;
        if (!hit.CheckToolTier(deposit.MinToolTier) || IsExcluded(deposit)) return null;

        deposit.Load();
        float before = deposit.Health(index);
        if (before <= 0f) return null;

        // Found now, while the struck piece is still there to measure.
        List<int> neighbors = [];
        Collider? struck = deposit.Collider(index);
        if (struck && hit.m_radius <= 0f)
        {
            Bounds near = struck!.bounds;
            near.Expand(NeighborGap * 2f);
            for (int i = 0; i < deposit.Count; i++)
            {
                if (i == index || deposit.Health(i) <= 0f) continue;
                Collider? other = deposit.Collider(i);
                if (other && near.Intersects(other!.bounds)) neighbors.Add(i);
            }
        }
        return new Strike(hit.Clone(), attacker, before, neighbors);
    }

    private static void Work(Deposit deposit, int index, Strike strike)
    {
        ZNetView view = deposit.View;
        if (!view || !view.IsValid()) return;
        // The hit did nothing (too hard, or resisted): nothing more either.
        if (deposit.Health(index) >= strike.Before) return;

        float spread = Mathf.Clamp01(PluginConfig.VeinSpread.Value);
        if (spread > 0f)
            foreach (int i in strike.Neighbors)
            {
                if (!view.IsValid()) return;
                if (deposit.Health(i) <= 0f) continue;
                HitData copy = strike.Hit.Clone();
                copy.m_damage.Modify(spread);
                Deal(deposit, copy, i);
            }

        if (!view.IsValid() || !ShouldShatter(deposit)) return;
        int broken = 0;
        for (int i = 0; i < deposit.Count && view.IsValid(); i++)
        {
            float health = deposit.Health(i);
            if (health <= 0f) continue;
            // Plain damage, which no resistance touches, a little more than the piece has.
            HitData copy = strike.Hit.Clone();
            copy.m_damage = new HitData.DamageTypes { m_damage = health + 1f };
            Deal(deposit, copy, i);
            // The struck piece already wore the pickaxe with the hit itself.
            if (i != index) broken++;
        }
        Wear(strike.Attacker, broken);
    }

    private static void Deal(Deposit deposit, HitData hit, int index)
    {
        Collider? piece = deposit.Collider(index);
        hit.m_hitCollider = null;
        hit.m_radius = 0f;
        if (piece) hit.m_point = piece!.bounds.center;
        deposit.Hit(hit, index);
    }

    // Whether what is left of the deposit is down to the Shatter At Health Multiplier of its full health.
    private static bool ShouldShatter(Deposit deposit)
    {
        float shatterAt = Mathf.Clamp01(PluginConfig.VeinShatterAt.Value);
        if (shatterAt <= 0f) return false;
        float full = deposit.Full * deposit.Count, left = 0f;
        for (int i = 0; i < deposit.Count; i++) left += Mathf.Max(0f, deposit.Health(i));
        return left > 0f && left <= full * shatterAt;
    }

    // On the Excluded list: by its own prefab name, or by an item it drops.
    private static bool IsExcluded(Deposit deposit)
    {
        HashSet<int> excluded = PluginConfig.VeinExcludedPrefabs();
        if (excluded.Count == 0) return false;
        if (excluded.Contains(deposit.View.GetZDO().GetPrefab())) return true;
        if (deposit.Drops?.m_drops == null) return false;
        foreach (DropTable.DropData drop in deposit.Drops.m_drops)
            if (drop.m_item && excluded.Contains(drop.m_item.name.GetStableHashCode())) return true;
        return false;
    }

    // The shatter wears the striker's pickaxe once more for each piece it broke, on their own game.
    private static void Wear(Player attacker, int pieces)
    {
        if (pieces <= 0) return;
        if (attacker == Player.m_localPlayer)
        {
            WearLocal(pieces);
            return;
        }
        ZDO? zdo = attacker.m_nview ? attacker.m_nview.GetZDO() : null;
        if (zdo != null && ZRoutedRpc.instance != null) ZRoutedRpc.instance.InvokeRoutedRPC(zdo.GetOwner(), Rpcs.VeinWear, pieces);
    }

    private static void RPC_VeinWear(long sender, int pieces)
    {
        if (PatchGuard.IsDisabled(Feature) || !Blessings.IsActive(WeatherKind.VeinShatter)) return;
        try { WearLocal(Mathf.Clamp(pieces, 0, 100)); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void WearLocal(int pieces)
    {
        Player? player = Player.m_localPlayer;
        ItemDrop.ItemData? tool = player ? player!.GetCurrentWeapon() : null;
        if (tool == null || !tool.m_shared.m_useDurability || pieces <= 0) return;
        tool.m_durability = Mathf.Max(0f, tool.m_durability - tool.m_shared.m_useDurabilityDrain * Game.m_durabilityRate * pieces);
    }
}

// The Fortune's Boon Rite, for the local player (the game of the player acting makes every lucky roll).
// - The skills' lucky rolls for extra items come more often: crafting (the cauldron too), the cooking station and
//   picking crops and other pickables. The game's chance is raised just for that call and put back right after.
// - The Forge of Potential (a crafting station marked as upgrader) rolls one number per upgrade: under the material's
//   upgrade chance it succeeds, within its break chance of the top the item is destroyed, in between it loses a level.
//   A roll that would destroy the item takes the losing path instead, with the level set to the Forge Break Penalty
//   below the old one, and the blessing ends.
internal static class FortunePatches
{
    private const string Feature = "Fortune's Boon Rite";

    private static bool Blessed(Character? who) =>
        !PatchGuard.IsDisabled(Feature) && who && who == Player.m_localPlayer && Blessings.IsActive(WeatherKind.FortunesBoon);

    private static float Raised(float chance) => Mathf.Min(1f, chance * Mathf.Max(1f, PluginConfig.LuckMultiplier.Value));

    // An upgrade at the forge, worked out under the blessing.
    private sealed class Upgrade(ItemDrop.ItemData.SharedData material, string prefab, string name, int level)
    {
        internal ItemDrop.ItemData.SharedData Material { get; } = material;
        internal string Prefab { get; } = prefab;
        internal string Name { get; } = name;
        internal int Level { get; } = level;
        // The material's own break chance, set aside while the roll that would have destroyed the item is worked out.
        internal float BreakChance { get; set; } = -1f;
        internal int SavedLevel { get; set; } = -1;
    }

    private static Upgrade? _upgrade;

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    private static class Crafting
    {
        private static void Prefix(InventoryGui __instance, Player player, out float __state)
        {
            __state = -1f;
            _upgrade = null;
            if (!Blessed(player)) return;
            try
            {
                __state = __instance.m_craftBonusChance;
                __instance.m_craftBonusChance = Raised(__state);
                _upgrade = AtForge(__instance, player);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static Exception? Finalizer(Exception? __exception, InventoryGui __instance, float __state)
        {
            if (__state >= 0f) __instance.m_craftBonusChance = __state;
            Upgrade? upgrade = _upgrade;
            _upgrade = null;
            if (upgrade != null)
            {
                if (upgrade.BreakChance >= 0f) upgrade.Material.m_breakChance = upgrade.BreakChance;
                if (upgrade.SavedLevel >= 0) Saved(upgrade);
            }
            return __exception;
        }
    }

    private static Upgrade? AtForge(InventoryGui gui, Player player)
    {
        CraftingStation? station = player.GetCurrentCraftingStation();
        ItemDrop.ItemData? item = gui.m_craftUpgradeItem;
        Recipe? recipe = gui.m_craftRecipe;
        if (!station || !station!.m_upgrader || item == null || !recipe || !recipe!.m_item) return null;
        foreach (Piece.Requirement requirement in recipe.m_resources)
            if (requirement.m_upgraderResource && requirement.m_resItem)
                return new Upgrade(requirement.m_resItem.m_itemData.m_shared, recipe.m_item.gameObject.name, item.m_shared.m_name, item.m_quality);
        return null;
    }

    private static void Saved(Upgrade upgrade)
    {
        try
        {
            string name = Localization.instance != null ? Localization.instance.Localize(upgrade.Name) : upgrade.Name;
            // Always shown: it replaces the game's own message about the failed upgrade.
            if (Player.m_localPlayer)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(Messages.FortuneSavedItem, name, upgrade.SavedLevel));
            Blessings.End(WeatherKind.FortunesBoon);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // The forge's roll for the upgrade: one that would destroy the item turns its material's break chance off, so the
    // game takes the path of a failed upgrade.
    [HarmonyPatch(typeof(Player), nameof(Player.GetItemUpgradeChance))]
    private static class ForgeRoll
    {
        private static void Postfix(float __result)
        {
            Upgrade? upgrade = _upgrade;
            if (upgrade == null || upgrade.BreakChance >= 0f) return;
            try
            {
                ItemDrop.ItemData.SharedData material = upgrade.Material;
                bool breaks = material.m_upgradeChance < __result && material.m_breakChance >= 1f - __result;
                if (!breaks) return;
                upgrade.BreakChance = material.m_breakChance;
                material.m_breakChance = -1f;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // The failed upgrade hands the item back: at the Forge Break Penalty below its old level instead of one below.
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem),
        typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool))]
    private static class ForgeReturn
    {
        private static void Prefix(string name, ref int quality)
        {
            Upgrade? upgrade = _upgrade;
            if (upgrade == null || upgrade.BreakChance < 0f || upgrade.SavedLevel >= 0 || name != upgrade.Prefab) return;
            int penalty = Mathf.Max(0, PluginConfig.ForgeBreakPenalty.Value);
            quality = Mathf.Max(upgrade.Level - penalty, Mathf.Min(upgrade.Level, 1));
            upgrade.SavedLevel = quality;
        }
    }

    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnInteract))]
    private static class Cooking
    {
        private static void Prefix(Humanoid user, out float __state)
        {
            __state = -1f;
            if (!Blessed(user) || !InventoryGui.instance) return;
            __state = InventoryGui.instance.m_craftBonusChance;
            InventoryGui.instance.m_craftBonusChance = Raised(__state);
        }

        private static Exception? Finalizer(Exception? __exception, float __state)
        {
            if (__state >= 0f && InventoryGui.instance) InventoryGui.instance.m_craftBonusChance = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
    private static class Picking
    {
        private static void Prefix(Pickable __instance, Humanoid character, out float __state)
        {
            __state = -1f;
            if (!Blessed(character)) return;
            __state = __instance.m_maxLevelBonusChance;
            __instance.m_maxLevelBonusChance = Raised(__state);
        }

        private static Exception? Finalizer(Exception? __exception, Pickable __instance, float __state)
        {
            if (__state >= 0f && __instance) __instance.m_maxLevelBonusChance = __state;
            return __exception;
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

// The Crypt Delver Rite. The game fills a chest once, when it first loads (not when it is opened), so the bonus is
// added when a blessed player first opens a dungeon chest: one inside a dungeon, filled by the game, not built by a
// player. The chest is the opener's then (the game hands it to whoever opens it). It gets the Bonus Loot Percent more
// items of what it rolled (rounded up), maybe one or two more on top, all rolled again from the chest's own loot list,
// so they can be anything it can hold. Each chest gives this once, to the first blessed player who opens it. What a
// chest rolled is written on it when it is filled; chests filled before (or by a game without the mod) count what is
// in them when opened.
internal static class CryptDelverPatches
{
    private const string Feature = "Crypt Delver Rite";
    private static readonly int Delved = "TempestRite_Delved".GetStableHashCode();
    private static readonly int Rolled = "TempestRite_Rolled".GetStableHashCode();
    // Where the game's own check puts the inside of dungeons (they are built high above the world).
    private const float InteriorHeight = 3000f;

    // Set while the game fills a chest.
    private static Container? _filling;

    [HarmonyPatch(typeof(Container), nameof(Container.AddDefaultItems))]
    private static class Filling
    {
        private static void Prefix(Container __instance) => _filling = __instance;

        private static Exception? Finalizer(Exception? __exception)
        {
            _filling = null;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(DropTable), nameof(DropTable.GetDropListItems))]
    private static class Rolling
    {
        private static void Postfix(DropTable __instance, List<ItemDrop.ItemData> __result)
        {
            Container? chest = _filling;
            if (chest == null || __instance != chest.m_defaultItems || PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (IsDungeonChest(chest)) chest.m_nview.GetZDO().Set(Rolled, __result.Count);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenResponse))]
    private static class Opening
    {
        // Before the game shows the chest, so the extra items are in it when it opens.
        private static void Prefix(Container __instance, bool granted)
        {
            if (!granted || PatchGuard.IsDisabled(Feature)) return;
            try { Delve(__instance); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    private static bool IsDungeonChest(Container chest) =>
        chest.m_nview && chest.m_nview.GetZDO() != null && chest.m_defaultItems is { m_drops.Count: > 0 } &&
        chest.transform.position.y > InteriorHeight && (!chest.m_piece || !chest.m_piece.IsPlacedByPlayer());

    private static void Delve(Container chest)
    {
        if (!Player.m_localPlayer || !Blessings.IsActive(WeatherKind.CryptDelver) || !IsDungeonChest(chest) || !chest.m_nview.IsOwner()) return;
        ZDO zdo = chest.m_nview.GetZDO();
        if (zdo.GetBool(Delved)) return;
        zdo.Set(Delved, true);

        Inventory inventory = chest.GetInventory();
        int rolled = zdo.GetInt(Rolled, -1);
        if (rolled < 0) rolled = inventory.NrOfItems();
        int extra = CryptLoot.Extra(rolled, PluginConfig.LootMultiplier.Value, PluginConfig.OneExtraChance.Value,
            PluginConfig.TwoExtraChance.Value, UnityEngine.Random.value);
        if (extra <= 0) return;

        // The chest's own loot list, rolled for just the extra items, any of them (also more of one already in).
        DropTable table = chest.m_defaultItems.Clone();
        table.m_dropMin = table.m_dropMax = extra;
        table.m_dropChance = 1f;
        table.m_oneOfEach = false;
        bool cheated = zdo.IsCheated() && !PlayerProfile.s_bypassCheatChecks;
        foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(table.GetDropListItems()))
        {
            if (cheated) item.m_cheated = true;
            // A full chest drops the rest at its feet.
            if (!inventory.AddItem(item))
                ItemDrop.DropItem(item, item.m_stack, chest.transform.position + Vector3.up, Quaternion.identity);
        }
        PlayerMessages.TopLeft(string.Format(Messages.CryptDelverFound, extra));
    }
}
