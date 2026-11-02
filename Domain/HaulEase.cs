using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Hearth-Haul Rite: players within its area can carry Carry Weight Bonus more, and carrying too much only slows
// them as usual: walking doesn't use up stamina and stamina comes back as usual (see HearthHaulPatches). Each game
// works it out for its own player, who it reaches where they stand.
internal static class HaulEase
{
    internal static bool Reaches(Player player) =>
        WeatherState.IsActive(WeatherKind.HearthHaul) && player && WeatherState.Reaches(WeatherKind.HearthHaul, player.transform.position);

    // Added before the world's carry weight modifier, as the game adds a belt's.
    internal static float Bonus => Mathf.Max(0f, PluginConfig.HearthHaulCarryBonus.Value);

    internal const string StaminaFeature = "Hearth-Haul stamina";

    // Whether carrying too much costs this player no stamina: under the rite with Free Stamina on. Called from within
    // the game's own stamina update, so it never throws.
    private static bool Eased(Player player)
    {
        if (PatchGuard.IsDisabled(StaminaFeature)) return false;
        try { return PluginConfig.HearthHaulFreeStamina.IsOn() && Reaches(player); }
        catch (System.Exception exception)
        {
            PatchGuard.Fail(StaminaFeature, exception);
            return false;
        }
    }

    // The game's "carrying too much" as far as stamina coming back goes.
    internal static bool Strains(bool encumbered, Player player) => encumbered && !Eased(player);

    // The stamina a second walking with too much costs.
    internal static float Drain(float drain, Player player) => Eased(player) ? 0f : drain;
}
