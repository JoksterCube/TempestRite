using JoksterCube.TempestRite.Settings;

namespace JoksterCube.TempestRite.Domain;

// The Tundra Shroud Rite keeps the blessed local player out of the weather's chill: while the game works out their
// Wet, Cold and Freezing (Player.UpdateEnvStatusEffects), the weather is taken as neither wet nor cold, so rain gives
// no Wet and nightfall (or any merely cold weather) no Cold. Freezing is left as the weather has it, and water still
// wets: swimming and wading add Wet elsewhere (Character.UpdateWater). That Wet wears off faster, as by a fire.
internal static class TundraShroud
{
    internal static bool Active => Blessings.IsActive(WeatherKind.TundraShroud);

    // The weather's own wet and cold, put back after the game has looked at them.
    private static bool _wasWet, _wasCold, _hidden;

    internal static void Hide()
    {
        _wasWet = EnvMan.s_isWet;
        _wasCold = EnvMan.s_isCold;
        _hidden = true;
        EnvMan.s_isWet = false;
        EnvMan.s_isCold = false;
    }

    internal static void Restore()
    {
        if (!_hidden) return;
        _hidden = false;
        EnvMan.s_isWet = _wasWet;
        EnvMan.s_isCold = _wasCold;
    }

    // How much more of Wet's time wears off this frame: as by a fire (where the game already does it, nothing more).
    internal static float ExtraDrying(Character character, float dt)
    {
        if (character != Player.m_localPlayer || !Active) return 0f;
        SEMan seman = character.GetSEMan();
        if (seman.HaveStatusEffect(SEMan.s_statusEffectCampFire) || seman.HaveStatusEffect(SEMan.s_statusEffectBurning)) return 0f;
        return dt * (PluginConfig.ShroudDryingMultiplier.Value - 1f);
    }
}
