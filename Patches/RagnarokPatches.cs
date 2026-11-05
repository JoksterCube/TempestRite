using System;
using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// The Ragnarök Rite's bosses are as strong as the strongest boss the players have slain (see RagnarokPower): their
// health and damage are multiplied by what is saved on each. The game's own scaling stays on top: its damage per nearby
// player and world damage rate are applied to the hit after DamagePatch, and its per-player toughness (less damage taken
// by enemies with more players near) and world level health work on whatever their max health is.
internal static class RagnarokPatches
{
    private static float Multiplier(Character? character, int key)
    {
        if (!character || character!.IsPlayer()) return 1f;
        ZNetView nview = character.m_nview;
        if (!nview || !nview.IsValid()) return 1f;
        float multiplier = nview.GetZDO().GetFloat(key, 1f);
        return multiplier > 0f ? multiplier : 1f;
    }

    // Their health: also when another game takes them over and sets their health up again.
    [HarmonyPatch(typeof(Character), nameof(Character.GetMaxHealthBase))]
    internal static class HealthPatch
    {
        private const string Feature = "Ragnarök Rite boss health";

        private static void Postfix(Character __instance, ref float __result)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { __result *= Multiplier(__instance, IncursionArena.BossHealth); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Their hits, worked out in the game that owns what was hit, before armor and resistances.
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    internal static class DamagePatch
    {
        private const string Feature = "Ragnarök Rite boss damage";

        private static void Prefix(Character __instance, HitData hit)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (hit == null || !__instance.m_nview || !__instance.m_nview.IsOwner()) return;
                float multiplier = Multiplier(hit.GetAttacker(), IncursionArena.BossDamage);
                if (multiplier != 1f) hit.m_damage.Modify(multiplier);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // What a boss summons (the Elder's roots, the Queen's seekers, the Fader's minions...) is as strong as the boss:
    // called for each thing spawned, in the game that owns the boss, right after it is made.
    [HarmonyPatch(typeof(SpawnAbility), nameof(SpawnAbility.SetupAoe))]
    internal static class SummonPatch
    {
        private const string Feature = "Ragnarök Rite boss summons";

        private static void Prefix(SpawnAbility __instance, Character owner)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                Character summoner = __instance.m_owner;
                if (!owner || !summoner || !IncursionArena.IsRagnarokBoss(summoner)) return;
                ZNetView nview = owner.m_nview;
                if (!nview || !nview.IsValid() || !nview.IsOwner()) return;
                float health = Multiplier(summoner, IncursionArena.BossHealth);
                float damage = Multiplier(summoner, IncursionArena.BossDamage);
                if (health == 1f && damage == 1f) return;
                ZDO zdo = nview.GetZDO();
                zdo.Set(IncursionArena.BossHealth, health);
                zdo.Set(IncursionArena.BossDamage, damage);
                // Its health was set up as it woke, before it was marked.
                owner.SetMaxHealth(owner.GetMaxHealthBase() * owner.GetLevel());
                owner.SetHealth(owner.GetMaxHealth());
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // A boss raised to the strongest boss slain shows a star beside its name for each step (see RagnarokLook).
    [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.UpdateHuds))]
    internal static class StarsPatch
    {
        private const string Feature = "Ragnarök Rite boss stars";

        private static void Postfix(EnemyHud __instance)
        {
            if (PatchGuard.IsDisabled(Feature) || IncursionArena.Kind != WeatherKind.Ragnarok) return;
            try
            {
                foreach (KeyValuePair<Character, EnemyHud.HudData> pair in __instance.m_huds)
                {
                    EnemyHud.HudData hud = pair.Value;
                    if (!pair.Key || hud.m_gui == null || !hud.m_gui.activeSelf) continue;
                    int stars = RagnarokLook.StarsOf(pair.Key);
                    if (stars > 0) RagnarokLook.ShowStars(__instance, hud, stars);
                }
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // The game's per-player scaling counts the players within 100 m; within a Ragnarök or Hoard-Bane Rite's Blood Grounds only
    // the players inside it count, not those standing near it outside.
    [HarmonyPatch(typeof(Game), nameof(Game.GetPlayerDifficulty))]
    internal static class PlayerCountPatch
    {
        private const string Feature = "Ragnarök Rite player count";

        private static void Postfix(Game __instance, Vector3 pos, ref int __result)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                WeatherKind kind = IncursionArena.Kind;
                if (__instance.m_forcePlayers > 0 || kind is not (WeatherKind.Ragnarok or WeatherKind.HoardBane)) return;
                Vector3 center = WeatherState.Position(kind);
                float radius = PluginConfig.Radius(kind);
                if (Utils.DistanceXZ(pos, center) > radius) return;
                int inside = 0;
                foreach (Player player in Player.GetAllPlayers())
                    if (player && Utils.DistanceXZ(player.transform.position, center) <= radius) inside++;
                __result = Mathf.Clamp(inside, 1, Mathf.Max(1, __instance.m_difficultyScaleMaxPlayers));
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // The Reckoning rites' treasure chests, as each game wakes one: they vanish once emptied, give no materials and shine
    // until first opened (see TreasureChests.Mark).
    [HarmonyPatch(typeof(Container), "Awake")]
    internal static class ChestPatch
    {
        private const string Feature = "Treasure chests";

        private static void Postfix(Container __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                ZNetView nview = __instance.m_nview;
                if (TreasureChests.IsChest(nview)) TreasureChests.Mark(nview.gameObject);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // A treasure chest opened for the first time: its beam goes (see TreasureChests.Opened).
    [HarmonyPatch(typeof(Container), "RPC_OpenResponse")]
    internal static class ChestOpenedPatch
    {
        private const string Feature = "Treasure chest beams";

        private static void Postfix(Container __instance, bool granted)
        {
            if (!granted || PatchGuard.IsDisabled(Feature)) return;
            try { TreasureChests.Opened(__instance.m_nview); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // No trophies (from the Hoard-Bane Rite's minibosses either, which drop no chest: they are the game's chestless ones):
    // everything else they drop is the players'.
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    internal static class DropPatch
    {
        private const string Feature = "Ragnarök Rite boss drops";

        private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                Character character = __instance.m_character;
                if (__result == null || !character) return;
                if (!IncursionArena.IsRagnarokBoss(character) && HoardBaneArena.RoleOf(character) != HoardBaneArena.BossRole) return;
                __result.RemoveAll(drop => IsTrophy(drop.Key));
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static bool IsTrophy(GameObject? prefab)
        {
            ItemDrop? item = prefab ? prefab!.GetComponent<ItemDrop>() : null;
            return item && item!.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy;
        }
    }
}
