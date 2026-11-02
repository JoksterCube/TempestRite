using System.Collections.Generic;
using System.Linq;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The blessings that change how the blessed player moves and fares (see their patches):
// - Scald-Drift: the Ashlands' boiling sea hurts far less, and lava less (still enough to flee). The player's own game
//   hurts its player, so it works it out.
// - Tether-Lock: the player's harpoon line holds longer, costs less stamina and reels in faster. The harpooned
//   creature's game pulls it (usually the harpooner's own), reading the blessing off the harpooner's character.
// - Strider-Girth: the mount the player rides gets its stamina back faster, and the attack keys make it attack. The
//   rider's game owns the mount while riding, so it works it out.
internal static class BlessingPerks
{
    internal static bool Scalded(Character? character) =>
        character && character == Player.m_localPlayer && Blessings.IsActive(WeatherKind.ScaldDrift);

    internal static float ScaldDamage => Mathf.Clamp01(PluginConfig.ScaldDamageMultiplier.Value);
    internal static float ScaldTickSlowdown => Mathf.Max(1f, PluginConfig.ScaldTickSlowdown.Value);
    internal static float LavaDamage => Mathf.Clamp01(PluginConfig.ScaldLavaDamageMultiplier.Value);
    internal static float LavaTickSlowdown => Mathf.Max(1f, PluginConfig.ScaldLavaTickSlowdown.Value);

    // Whether this harpooner's line is blessed: read off their character, so it works for whoever pulls.
    internal static bool Tethered(Character? attacker) => attacker && Blessings.Has(attacker, WeatherKind.TetherLock);

    internal static float TetherStrength => Mathf.Max(1f, PluginConfig.TetherStrengthMultiplier.Value);
    internal static float TetherStamina => Mathf.Clamp01(PluginConfig.TetherStaminaMultiplier.Value);
    internal static float TetherReel => Mathf.Max(1f, PluginConfig.TetherReelMultiplier.Value);

    // Whether the local player rides this mount under the Strider-Girth Rite.
    internal static bool Striding(Sadle saddle) => saddle.IsLocalUser() && Blessings.IsActive(WeatherKind.StriderGirth);

    internal static float StriderRegen => Mathf.Max(1f, PluginConfig.StriderRegenMultiplier.Value);

    // Strider-Girth: the attack keys make the mount use its own attacks (attack = its first, secondary attack = its
    // second, e.g. a lox's bite and stomp; swapped for the mounts listed in the settings) instead of getting the rider
    // off. Each costs the mount stamina. The rider's game owns the mount while riding, so it starts the attack and the
    // game syncs it like any creature attack.
    internal static void MountAttack(Sadle saddle, bool secondary)
    {
        if (!saddle.m_nview || !saddle.m_nview.IsValid() || !saddle.m_nview.IsOwner()) return;
        if (saddle.m_character is not Humanoid mount || !mount || mount.IsDead() || mount.InAttack() || mount.IsSwimming()) return;
        float cost = Mathf.Max(0f, (secondary ? PluginConfig.StriderSecondaryStamina : PluginConfig.StriderAttackStamina).Value);
        if (cost > 0f && saddle.GetStamina() < cost) return;
        List<ItemDrop.ItemData> attacks = MountAttacks(mount);
        if (attacks.Count == 0) return;
        bool second = secondary != PluginConfig.StriderSwappedPrefabs().Contains(saddle.m_nview.GetZDO().GetPrefab());
        ItemDrop.ItemData attack = second && attacks.Count > 1 ? attacks[1] : attacks[0];

        // It attacks the way it faces: to hit somewhere else, the rider turns it first.
        if (!mount.IsItemEquiped(attack)) mount.EquipItem(attack);
        if (mount.StartAttack(null, false)) saddle.UseStamina(cost);
    }

    // The mount's attacks against enemies, the plain ones (bite, swipe) before the area ones (stomp, slam), then the
    // more frequent first, keeping the creature's own order on ties.
    private static List<ItemDrop.ItemData> MountAttacks(Humanoid mount) => mount.GetInventory().GetAllItems()
        .Where(item => item.IsWeapon() && item.HavePrimaryAttack()
            && item.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy)
        .OrderBy(item => item.m_shared.m_attack.m_attackType == Attack.AttackType.Area ? 1 : 0)
        .ThenBy(item => item.m_shared.m_aiAttackInterval)
        .ToList();
}
