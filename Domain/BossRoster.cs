using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The bosses of the Ragnarök Rite's Bosses setting, weakest first, as the Ragnarök and Hoard-Bane Rites use them: which
// ones the players inside have slain (asked of each player's game, see RagnarokArena) and how strong each is.
internal static class BossRoster
{
    internal readonly struct Boss(string prefab, string key, string name, float health, float taken, float damage)
    {
        internal string Prefab { get; } = prefab;
        // The key it sets as it dies ("defeated_eikthyr"), and its name ("$enemy_eikthyr").
        internal string Key { get; } = key;
        internal string Name { get; } = name;
        // Its health, the share of damage it takes and its average damage per attack.
        internal float Health { get; } = health;
        internal float Taken { get; } = taken;
        internal float Damage { get; } = damage;
    }

    // The bosses of the Bosses setting the game has, with the key each gives as it dies. rite: for warnings.
    internal static List<Boss> Read(string rite)
    {
        List<Boss> roster = [];
        foreach (string name in KeyItemLists.Parse(PluginConfig.RagnarokBosses.Value))
        {
            GameObject? prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(name) : null;
            Character? boss = prefab ? prefab!.GetComponent<Character>() : null;
            if (!boss)
            {
                Plugin.ModLogger.LogWarning($"{rite}: boss \"{name}\" is not a creature the game knows. Check the Ragnarök Rite's Bosses setting.");
                continue;
            }
            if (string.IsNullOrEmpty(boss!.m_defeatSetGlobalKey))
            {
                Plugin.ModLogger.LogWarning($"{rite}: boss \"{name}\" sets no key when it dies, so nobody can be known to have slain it. " +
                    "Check the Ragnarök Rite's Bosses setting.");
                continue;
            }
            // Listed twice: once is enough.
            if (roster.Exists(r => r.Key == boss.m_defeatSetGlobalKey)) continue;
            roster.Add(new Boss(name, boss.m_defeatSetGlobalKey, boss.m_name, boss.m_health, Taken(boss.m_damageModifiers),
                RagnarokPower.Damage(name, ReadDamage(prefab!))));
        }
        return roster;
    }

    // What each player inside is asked: the bosses' keys and names.
    internal static ZPackage AskPackage(List<Boss> roster)
    {
        ZPackage keys = new();
        keys.Write(roster.Count);
        foreach (Boss boss in roster)
        {
            keys.Write(boss.Key);
            keys.Write(boss.Name);
        }
        return keys;
    }

    // The bosses (their places) every player who answered has slain, and those slain by some but not all. With player
    // based raids off (and World Advancement Progression's private raids too), the world's keys count for everyone.
    internal static void Slain(List<Boss> roster, ICollection<HashSet<string>> answers, List<int> everyone, List<int> some)
    {
        everyone.Clear();
        some.Clear();
        bool byPlayer = ZoneSystem.instance &&
            (ZoneSystem.instance.GetGlobalKey(GlobalKeys.PlayerEvents) || AdvancementProgression.PrivateRaids());
        for (int place = 0; place < roster.Count; place++)
        {
            string key = roster[place].Key;
            bool world = !byPlayer && ZoneSystem.instance && ZoneSystem.instance.GetGlobalKey(key);
            bool all = answers.Count > 0;
            bool someone = world;
            foreach (HashSet<string> keys in answers)
            {
                if (keys.Contains(key)) someone = true;
                else if (!world) all = false;
            }
            if (all) everyone.Add(place);
            else if (someone) some.Add(place);
        }
    }

    // The Ragnarök Rite's early bosses: the first ones of the Bosses setting (Eikthyr, the Elder, Bonemass). A player
    // who has slain the last of them or a stronger one counts them all as slain, so those who missed the early game
    // aren't left out; until someone inside has got that far, the rite calls no boss.
    internal const int EarlyBosses = 3;

    // Adds the early bosses to what a player has slain when they got past them. keys: the bosses' keys, weakest first.
    internal static void FillEarly(IList<string> keys, HashSet<string> slain)
    {
        if (!PastEarly(keys, slain)) return;
        for (int place = 0; place < Math.Min(EarlyBosses, keys.Count); place++) slain.Add(keys[place]);
    }

    // Whether the last early boss (with fewer bosses in the setting, the strongest) or a stronger one is among these.
    internal static bool PastEarly(IList<string> keys, ICollection<string> slain)
    {
        for (int place = Math.Min(EarlyBosses, keys.Count) - 1; place >= 0 && place < keys.Count; place++)
            if (slain.Contains(keys[place])) return true;
        return false;
    }

    // The bosses' keys the world has, when they count for everyone (see Slain); otherwise none.
    internal static HashSet<string> WorldSlain(List<Boss> roster)
    {
        HashSet<string> world = [];
        if (!ZoneSystem.instance || ZoneSystem.instance.GetGlobalKey(GlobalKeys.PlayerEvents) || AdvancementProgression.PrivateRaids())
            return world;
        foreach (Boss boss in roster)
            if (ZoneSystem.instance.GetGlobalKey(boss.Key)) world.Add(boss.Key);
        return world;
    }

    // The share of damage it takes, on average over the damage types players deal.
    private static float Taken(HitData.DamageModifiers modifiers)
    {
        HitData.DamageModifier[] types = [modifiers.m_blunt, modifiers.m_slash, modifiers.m_pierce, modifiers.m_fire,
            modifiers.m_frost, modifiers.m_lightning, modifiers.m_poison];
        float total = 0f;
        foreach (HitData.DamageModifier type in types) total += RagnarokPower.Share((int)type);
        return total / types.Length;
    }

    internal static float TakenBy(Character character) => Taken(character.m_damageModifiers);

    // A creature's average damage per attack, from the weapons it attacks with.
    internal static float ReadDamage(GameObject prefab)
    {
        Humanoid humanoid = prefab.GetComponent<Humanoid>();
        if (!humanoid) return 0f;
        List<GameObject> weapons = [];
        if (humanoid.m_defaultItems != null) weapons.AddRange(humanoid.m_defaultItems);
        if (humanoid.m_randomWeapon != null) weapons.AddRange(humanoid.m_randomWeapon);
        float total = 0f;
        int attacks = 0;
        foreach (GameObject weapon in weapons)
        {
            ItemDrop? item = weapon ? weapon.GetComponent<ItemDrop>() : null;
            float damage = item ? item!.m_itemData.m_shared.m_damages.GetTotalDamage() : 0f;
            if (damage <= 0f) continue;
            total += damage;
            attacks++;
        }
        return attacks == 0 ? 0f : total / attacks;
    }
}
