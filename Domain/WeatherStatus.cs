using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The status shown for each rite going on and each blessing the local player carries, with the time left; for rites
// that work around an obliterator, only near it. It lives only on this game: added to your own status list, never sent
// to other players. It is put back every frame it is missing, so a rite's status stays through death (blessings end
// with it). A kinship rite's is the wait until it can give the player something again.
internal static class WeatherStatus
{
    private const string Feature = "Weather status";
    private static readonly Dictionary<WeatherKind, StatusEffect> Effects = [];

    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        Player? player = Player.m_localPlayer;
        if (!player || player!.IsDead()) return;
        try { Apply(player.GetSEMan()); }
        catch (System.Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Apply(SEMan seMan)
    {
        bool show = PluginConfig.ShowStatus.IsOn();
        foreach (WeatherKind kind in WeatherKinds.All)
        {
            // The Equilibrium Rite ends the others and has no status of its own.
            if (!WeatherKinds.Lasts(kind)) continue;
            StatusEffect template = Effect(kind);
            int hash = template.NameHash();
            StatusEffect? current = seMan.GetStatusEffect(hash);
            if (!show || !WeatherState.IsActive(kind) || !WeatherState.ShowsHere(kind))
            {
                if (current) seMan.RemoveStatusEffect(hash, true);
                continue;
            }

            if (!current) current = seMan.AddStatusEffect(template);
            if (!current) continue;
            // Counts down with the called effect, also when it was called by someone else or before joining.
            float duration = WeatherState.Duration(kind);
            current!.m_ttl = duration;
            current.m_time = duration - WeatherState.Remaining(kind);
            current.m_tooltip = Tooltip(kind);
        }
    }

    private static string Tooltip(WeatherKind kind)
    {
        string calledBy = WeatherState.CalledBy(kind);
        string who = string.IsNullOrEmpty(calledBy) ? Messages.SomeoneName : calledBy;
        switch (kind)
        {
            case WeatherKind.PolarDay: return string.Format(Messages.StatusTooltipPolarDay, who);
            case WeatherKind.PolarNight: return string.Format(Messages.StatusTooltipPolarNight, who);
            case WeatherKind.GaleBreath: return string.Format(Messages.StatusTooltipGaleBreath, who);
            case WeatherKind.NoMist: return string.Format(Messages.StatusTooltipNoMist, who);
            case WeatherKind.Sootfall: return string.Format(Messages.StatusTooltipSootfall, who);
            case WeatherKind.Tailwind: return string.Format(Messages.StatusTooltipTailwind, who);
            case WeatherKind.CryptDelver: return string.Format(Messages.StatusTooltipCryptDelver, who,
                Percent(PluginConfig.LootMultiplier.Value - 1f), Percent(PluginConfig.OneExtraChance.Value), Percent(PluginConfig.TwoExtraChance.Value));
            case WeatherKind.Packward: return string.Format(Messages.StatusTooltipPackward, who, Meters(kind));
            case WeatherKind.Carrion: return string.Format(Messages.StatusTooltipCarrion, who, Meters(kind));
            case WeatherKind.Verdant: return string.Format(Messages.StatusTooltipVerdant, who, Meters(kind), Times(PluginConfig.VerdantGrowthMultiplier.Value));
            case WeatherKind.Crimson: return string.Format(Messages.StatusTooltipCrimson, who, Meters(kind));
            case WeatherKind.Ragnarok: return string.Format(Messages.StatusTooltipRagnarok, who, Meters(kind));
            case WeatherKind.VatBoil: return string.Format(Messages.StatusTooltipVatBoil, who, Meters(kind), Times(PluginConfig.VatBoilSpeedMultiplier.Value));
            case WeatherKind.PitchBellows:
                return string.Format(Messages.StatusTooltipPitchBellows, who, Meters(kind), Times(PluginConfig.PitchBellowsSpeedMultiplier.Value));
            case WeatherKind.AmberMend: return string.Format(Messages.StatusTooltipAmberMend, who, Meters(kind));
            case WeatherKind.SovereignWard:
                return string.Format(PluginConfig.SovereignSanctuary.IsOn() ? Messages.StatusTooltipSovereignSanctuary : Messages.StatusTooltipSovereignWard, who, Meters(kind));
            case WeatherKind.Luminary: return string.Format(Messages.StatusTooltipLuminary, who, Mathf.RoundToInt(PluginConfig.LuminaryRange.Value));
            case WeatherKind.AbyssalDraw: return string.Format(Messages.StatusTooltipAbyssalDraw, who, Mathf.RoundToInt(PluginConfig.DrawRange.Value));
            case WeatherKind.AbyssalSurge:
                return string.Format(Messages.StatusTooltipAbyssalSurge, who, Times(PluginConfig.SerpentMultiplier.Value), Times(PluginConfig.FishMultiplier.Value));
            case WeatherKind.GlacialSatiety: return string.Format(Messages.StatusTooltipGlacialSatiety, who, Percent(PluginConfig.FoodFloor.Value));
            case WeatherKind.VeinShatter:
                return string.Format(Messages.StatusTooltipVeinShatter, who, Percent(PluginConfig.VeinSpread.Value), Percent(PluginConfig.VeinShatterAt.Value));
            case WeatherKind.CarapaceTemper: return string.Format(Messages.StatusTooltipCarapaceTemper, who, Percent(1f - PluginConfig.WearMultiplier.Value));
            case WeatherKind.GraveBind: return string.Format(Messages.StatusTooltipGraveBind, who);
            case WeatherKind.SkyCall: return string.Format(Messages.StatusTooltipSkyCall, who);
            case WeatherKind.Skal: return string.Format(Messages.StatusTooltipSkal, who);
            case WeatherKind.AnvilSpark: return string.Format(Messages.StatusTooltipAnvilSpark, who);
            case WeatherKind.FortunesBoon:
                int penalty = PluginConfig.ForgeBreakPenalty.Value;
                string forge = penalty > 0 ? string.Format(Messages.FortuneForgeLoses, penalty) : Messages.FortuneForgeKeeps;
                return string.Format(Messages.StatusTooltipFortunesBoon, who, Times(PluginConfig.LuckMultiplier.Value), forge);
        }
        string plain = WeatherKinds.Plain(kind);
        string what = plain.Length > 0 ? char.ToUpperInvariant(plain[0]) + plain.Substring(1) : plain;
        string leftOut = PluginConfig.AffectedBiomes.Value.LeftOut();
        return string.IsNullOrEmpty(leftOut)
            ? string.Format(Messages.StatusTooltipNoBiomes, what, who)
            : string.Format(Messages.StatusTooltip, what, who, leftOut);
    }

    private static int Meters(WeatherKind kind) => Mathf.RoundToInt(PluginConfig.Radius(kind));

    // A multiplier as a percent for the tooltips: 0.75 -> 75.
    private static int Percent(float value) => Mathf.RoundToInt(value * 100f);

    // "3", "2.5"
    private static string Times(float value) => value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

    private static StatusEffect Effect(WeatherKind kind)
    {
        if (!Effects.TryGetValue(kind, out StatusEffect effect) || !effect)
        {
            effect = ScriptableObject.CreateInstance<StatusEffect>();
            effect.name = $"SE_TempestRite_{kind}";
            effect.m_name = WeatherKinds.Name(kind);
            effect.m_tooltip = string.Empty;
            effect.m_ttl = 0f;
            effect.hideFlags = HideFlags.HideAndDontSave;
            Effects[kind] = effect;
        }
        // The HUD only lists effects with an icon.
        if (!effect.m_icon) effect.m_icon = WeatherIcons.For(kind);
        return effect;
    }
}
