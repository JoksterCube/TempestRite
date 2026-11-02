using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Draught-Flush Rite changes the time of meads the blessed player drinks (only the local player's own: each game
// runs its own player's status effects).
// - A recovery mead restores health, stamina or eitr over a few seconds, then waits out the rest of its time before
//   another of its kind can be drunk: that time is shortened by the Recovery Wait Multiplier, never below the restoring.
// - Every other mead (lingering, resistance, barley wine, tasty mead...) lasts Mead Duration Multiplier times as long.
//   Anything it restores over its whole time keeps its pace: the amount grows with the time.
// The time is set on the mead's own copy of its effect, before the game works out its restoring, so the prefab and other
// players are left alone. A mead already going when the blessing starts or ends keeps its time.
internal static class DraughtTimes
{
    // While the local player drinks a mead under the blessing: the name hash of the mead's effect.
    internal static int Drinking;

    // Whether drinking this item is changed: a mead (no food) drunk by the blessed local player.
    internal static bool Applies(Player player, ItemDrop.ItemData? item) =>
        item != null && player == Player.m_localPlayer && item.m_shared.m_consumeStatusEffect && item.m_shared.m_food <= 0f &&
        Blessings.IsActive(WeatherKind.DraughtFlush);

    internal static void Adjust(StatusEffect effect)
    {
        if (effect.m_ttl <= 0f) return;
        if (effect is SE_Stats stats && Recovery(stats))
        {
            float restoring = Mathf.Max(Restoring(stats.m_healthOverTime, stats.m_healthOverTimeDuration),
                Mathf.Max(Restoring(stats.m_staminaOverTime, stats.m_staminaOverTimeDuration),
                    Restoring(stats.m_eitrOverTime, stats.m_eitrOverTimeDuration)));
            effect.m_ttl = Mathf.Max(effect.m_ttl * RecoveryMultiplier(PluginConfig.DraughtRecoveryMultiplier.Value), restoring);
            return;
        }

        float multiplier = BuffMultiplier(PluginConfig.DraughtBuffMultiplier.Value);
        if (effect is SE_Stats spread)
        {
            // Restoring with no time of its own is spread over the whole time: more of it, at the same pace.
            if (spread.m_healthOverTime > 0f && spread.m_healthOverTimeDuration <= 0f) spread.m_healthOverTime *= multiplier;
            if (spread.m_staminaOverTime > 0f && spread.m_staminaOverTimeDuration <= 0f) spread.m_staminaOverTime *= multiplier;
            if (spread.m_eitrOverTime > 0f && spread.m_eitrOverTimeDuration <= 0f) spread.m_eitrOverTime *= multiplier;
        }
        effect.m_ttl *= multiplier;
    }

    internal static float RecoveryMultiplier(float value) => Mathf.Clamp(value, 0.1f, 1f);

    internal static float BuffMultiplier(float value) => Mathf.Clamp(value, 1f, 5f);

    // Restores health, stamina or eitr over less than its whole time: the rest is the wait before the next.
    private static bool Recovery(SE_Stats stats) =>
        Short(stats.m_healthOverTime, stats.m_healthOverTimeDuration, stats.m_ttl) ||
        Short(stats.m_staminaOverTime, stats.m_staminaOverTimeDuration, stats.m_ttl) ||
        Short(stats.m_eitrOverTime, stats.m_eitrOverTimeDuration, stats.m_ttl);

    private static bool Short(float amount, float duration, float ttl) => amount > 0f && duration > 0f && duration < ttl;

    private static float Restoring(float amount, float duration) => amount > 0f ? Mathf.Max(0f, duration) : 0f;
}
