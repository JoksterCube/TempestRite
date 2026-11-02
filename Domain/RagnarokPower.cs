using System;
using System.Collections.Generic;

namespace JoksterCube.TempestRite.Domain;

// How hard the Ragnarök Rite's bosses hit and how much they take: every boss below the strongest boss the players have
// all slain is raised to it, with a star for each place (in the Bosses setting) it stands below. After Bonemass,
// Eikthyr is 2★ and the Elder 1★, both as strong as Bonemass; after the Fader, Eikthyr is 6★ and as strong as the Fader.
// - Health: as hard to kill as the boss it is raised to, its resistances counted: a boss taking half of most damage
//   needs half the health to last as long.
// - Damage: as much per attack, on average, as the boss it is raised to.
// What a boss summons (the Elder's roots, the Queen's seekers...) gets the same multipliers (see RagnarokPatches).
// Never weaker than the boss itself.
internal static class RagnarokPower
{
    // The vanilla bosses' average damage per attack (each attack's damage types added up).
    private static readonly Dictionary<string, float> VanillaDamage = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Eikthyr"] = Average(20f, 15f),
        ["gd_king"] = Average(35f, 60f),
        ["Bonemass"] = Average(80f + 50f, 130f),
        ["Dragon"] = Average(30f + 200f, 110f, 200f),
        ["GoblinKing"] = Average(65f + 65f, 100f, 40f + 20f, 40f + 120f),
        ["SeekerQueen"] = Average(130f, 140f + 100f, 150f, 100f, 40f + 40f),
        ["Fader"] = Average(120f, 210f, 200f, 200f, 140f, 60f, 120f + 40f, 120f)
    };

    // A boss's stars: one for each place it stands below the strongest slain.
    internal static int Stars(int place, int strongest) => Math.Max(0, strongest - place);

    // The place of the boss it is made as strong as: the strongest slain, or itself when it stands above it.
    internal static int RaisedTo(int place, int strongest) => Math.Max(place, strongest);

    // The lowest share of damage counted as taken: a boss immune to almost everything isn't counted as unkillable.
    private const float LeastTaken = 0.1f;

    private static float Average(params float[] attacks)
    {
        float total = 0f;
        foreach (float attack in attacks) total += attack;
        return attacks.Length == 0 ? 0f : total / attacks.Length;
    }

    // A vanilla boss's average damage per attack, or the one read from its attacks.
    internal static float Damage(string prefab, float read) =>
        VanillaDamage.TryGetValue(prefab, out float damage) ? damage : read;

    // How much its health is multiplied by. taken: the share of damage it takes, on average over the usual types.
    internal static float HealthMultiplier(float health, float taken, float strongestHealth, float strongestTaken)
    {
        if (health <= 0f || strongestHealth <= 0f) return 1f;
        float lasts = strongestHealth / Math.Max(LeastTaken, strongestTaken);
        return Math.Max(1f, lasts * Math.Max(LeastTaken, taken) / health);
    }

    // How much its damage is multiplied by.
    internal static float DamageMultiplier(float damage, float strongestDamage) =>
        damage <= 0f || strongestDamage <= 0f ? 1f : Math.Max(1f, strongestDamage / damage);

    // The share of a damage type taken for each of the game's damage modifiers (HitData.DamageModifier, by number):
    // Normal, Resistant, Weak, Immune, Ignore, VeryResistant, VeryWeak, SlightlyResistant, SlightlyWeak.
    private static readonly float[] Shares = [1f, 0.5f, 1.5f, 0f, 0f, 0.25f, 2f, 0.75f, 1.25f];

    internal static float Share(int modifier) => modifier >= 0 && modifier < Shares.Length ? Shares[modifier] : 1f;
}
