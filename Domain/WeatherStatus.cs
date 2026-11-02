using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    // Tooltips only change when a setting or who performed a rite does: written again once a second, not every frame.
    private const float TooltipInterval = 1f;
    private static float _nextTooltips;

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
        bool tooltips = Time.time >= _nextTooltips;
        if (tooltips) _nextTooltips = Time.time + TooltipInterval;
        foreach (WeatherKind kind in WeatherKinds.All)
        {
            // The rites that happen at once (Equilibrium, Grounding...) have no status of their own.
            if (!WeatherKinds.Lasts(kind)) continue;
            StatusEffect template = Effect(kind);
            int hash = template.NameHash();
            StatusEffect? current = seMan.GetStatusEffect(hash);
            if (!show || !WeatherState.IsActive(kind) || !WeatherState.ShowsHere(kind))
            {
                if (current) StatusCommands.RemoveQuietly(seMan, hash);
                continue;
            }

            bool added = false;
            if (!current)
            {
                current = seMan.AddStatusEffect(template);
                added = true;
            }
            if (!current) continue;
            // Counts down with the called effect, also when it was called by someone else or before joining.
            // An untimed rite shows no time (a time to live of 0 never runs out).
            float duration = WeatherKinds.Untimed(kind) ? 0f : WeatherState.Duration(kind);
            current!.m_ttl = duration;
            current.m_time = WeatherKinds.Untimed(kind) ? 0f : duration - WeatherState.Remaining(kind);
            if (added || tooltips) current.m_tooltip = Tooltip(kind);
        }
    }

    // Like the game's own effects: what it does with its numbers in orange, then how long it lasts.
    private static string Tooltip(WeatherKind kind)
    {
        string text = Description(kind);
        if (WeatherKinds.Untimed(kind)) return text;
        float duration = WeatherState.Duration(kind);
        return duration > 1f ? $"{text}\n\n$se_ttl: {Value(Mathf.RoundToInt(duration).ToString())}" : text;
    }

    // A number with its unit after it ("{1}%", "{2}x", "{1} m") in orange, as the game shows the values of its effects.
    private static readonly Regex Numbers = new(@"\{\d+\}(?:%|x\b| m\b)", RegexOptions.Compiled);

    private static string Format(string template, params object[] args) =>
        string.Format(Numbers.Replace(template, match => Value(match.Value)), args);

    private static string Value(string text) => $"<color=orange>{text}</color>";

    private static string Description(WeatherKind kind)
    {
        string calledBy = WeatherState.CalledBy(kind);
        string who = string.IsNullOrEmpty(calledBy) ? Messages.SomeoneName : calledBy;
        switch (kind)
        {
            case WeatherKind.PolarDay: return Format(Messages.StatusTooltipPolarDay, who);
            case WeatherKind.PolarNight: return Format(Messages.StatusTooltipPolarNight, who);
            case WeatherKind.GaleBreath: return Format(Messages.StatusTooltipGaleBreath, who);
            case WeatherKind.NoMist: return Format(Messages.StatusTooltipNoMist, who);
            case WeatherKind.Sootfall: return Format(Messages.StatusTooltipSootfall, who);
            case WeatherKind.FairWind: return Format(Messages.StatusTooltipFairWind, who);
            case WeatherKind.HushFelling: return Format(Messages.StatusTooltipHushFelling, who, Times(PluginConfig.HushNoticeMultiplier.Value));
            case WeatherKind.DraughtFlush:
                return Format(Messages.StatusTooltipDraughtFlush, who, Times(PluginConfig.DraughtRecoveryMultiplier.Value),
                    Times(PluginConfig.DraughtBuffMultiplier.Value));
            case WeatherKind.TundraShroud: return Format(Messages.StatusTooltipTundraShroud, who, Times(PluginConfig.ShroudDryingMultiplier.Value));
            case WeatherKind.CryptDelver: return Format(Messages.StatusTooltipCryptDelver, who,
                Percent(PluginConfig.LootMultiplier.Value - 1f), Percent(PluginConfig.OneExtraChance.Value), Percent(PluginConfig.TwoExtraChance.Value));
            case WeatherKind.Packward: return Format(Messages.StatusTooltipPackward, who, Meters(kind));
            case WeatherKind.Carrion: return Format(Messages.StatusTooltipCarrion, who, Meters(kind));
            case WeatherKind.Verdant: return Format(Messages.StatusTooltipVerdant, who, Meters(kind), Times(PluginConfig.VerdantGrowthMultiplier.Value));
            case WeatherKind.Crimson: return Format(Messages.StatusTooltipCrimson, who, Meters(kind));
            case WeatherKind.Ragnarok: return Format(Messages.StatusTooltipRagnarok, who, Meters(kind));
            case WeatherKind.HoardBane: return Format(Messages.StatusTooltipHoardBane, who, Meters(kind));
            case WeatherKind.VatBoil: return Format(Messages.StatusTooltipVatBoil, who, Meters(kind), Times(PluginConfig.VatBoilSpeedMultiplier.Value));
            case WeatherKind.PitchBellows:
                return Format(Messages.StatusTooltipPitchBellows, who, Meters(kind), Times(PluginConfig.PitchBellowsSpeedMultiplier.Value));
            case WeatherKind.Kindred:
                return Format(Messages.StatusTooltipKindred, who, Meters(kind), Times(PluginConfig.KindredGrowthMultiplier.Value),
                    Times(PluginConfig.KindredCrowdMultiplier.Value));
            case WeatherKind.HarrowSpur:
                return Format(Messages.StatusTooltipHarrowSpur, who, Meters(kind), Times(PluginConfig.HarrowSpurSpeedMultiplier.Value),
                    Times(PluginConfig.HarrowSpurCapMultiplier.Value));
            case WeatherKind.AmberMend: return Format(Messages.StatusTooltipAmberMend, who, Meters(kind));
            case WeatherKind.SovereignWard:
                return Format(PluginConfig.SovereignSanctuary.IsOn() ? Messages.StatusTooltipSovereignSanctuary : Messages.StatusTooltipSovereignWard, who, Meters(kind));
            case WeatherKind.HearthHaul:
                return Format(PluginConfig.HearthHaulFreeStamina.IsOn() ? Messages.StatusTooltipHearthHaul : Messages.StatusTooltipHearthHaulCarry, who,
                    Meters(kind), Mathf.RoundToInt(HaulEase.Bonus));
            case WeatherKind.AetherClear: return Format(Messages.StatusTooltipAetherClear, who, Meters(kind));
            case WeatherKind.ElderSprout: return Format(Messages.StatusTooltipElderSprout, who, Meters(kind));
            case WeatherKind.SentrySight: return Format(Messages.StatusTooltipSentrySight, who, Meters(kind));
            case WeatherKind.TaprootSurge:
                return Format(Messages.StatusTooltipTaprootSurge, who, Meters(kind), Times(PluginConfig.TaprootSpeedMultiplier.Value),
                    Times(PluginConfig.TaprootRootMultiplier.Value));
            case WeatherKind.MantleAnchor: return Format(Messages.StatusTooltipMantleAnchor, who, Meters(kind));
            case WeatherKind.PrimalHearth:
                return Format(Messages.StatusTooltipPrimalHearth, who, Meters(kind), Times(PluginConfig.PrimalHearthSpawnMultiplier.Value),
                    Times(PluginConfig.PrimalHearthCapacityMultiplier.Value));
            case WeatherKind.HearthKindle:
                return Format(Messages.StatusTooltipHearthKindle, who, Meters(kind), Times(PluginConfig.KindleRestingMultiplier.Value),
                    Times(PluginConfig.KindleRestedMultiplier.Value));
            case WeatherKind.ScaldDrift:
                return Format(Messages.StatusTooltipScaldDrift, who, Times(PluginConfig.ScaldDamageMultiplier.Value),
                    Times(PluginConfig.ScaldTickSlowdown.Value), Times(PluginConfig.ScaldLavaDamageMultiplier.Value),
                    Times(PluginConfig.ScaldLavaTickSlowdown.Value));
            case WeatherKind.TetherLock:
                return Format(Messages.StatusTooltipTetherLock, who, Times(PluginConfig.TetherStrengthMultiplier.Value),
                    Times(PluginConfig.TetherStaminaMultiplier.Value), Times(PluginConfig.TetherReelMultiplier.Value));
            case WeatherKind.StriderGirth:
                return Format(Messages.StatusTooltipStriderGirth, who, Times(PluginConfig.StriderRegenMultiplier.Value));
            case WeatherKind.SecondKindle:
                return Format(Messages.StatusTooltipSecondKindle, who, Percent(PluginConfig.KindleHealthShare.Value),
                    Mathf.RoundToInt(PluginConfig.KindleKnockbackRadius.Value));
            case WeatherKind.PlumeWard: return Format(Messages.StatusTooltipPlumeWard, who, Times(PluginConfig.PlumeWardFallDamage.Value));
            case WeatherKind.MorgenPouch:
                return Format(Messages.StatusTooltipMorgenPouch, who, PluginConfig.MorgenPouchCount.Value,
                    PetPouch.KeyText, Mathf.RoundToInt(PluginConfig.MorgenPouchRange.Value),
                    Mathf.RoundToInt(PluginConfig.MorgenPouchReleaseCooldown.Value), Mathf.RoundToInt(PluginConfig.MorgenPouchCaptureCooldown.Value)) + PetPouch.Carrying();
            case WeatherKind.TendrilDraw:
                return Format(Messages.StatusTooltipTendrilDraw, who, Percent(PluginConfig.TendrilAmmoChance.Value),
                    Mathf.RoundToInt(PluginConfig.TendrilReturnSpeed.Value));
            case WeatherKind.Luminary: return Format(Messages.StatusTooltipLuminary, who, Mathf.RoundToInt(PluginConfig.LuminaryRange.Value));
            case WeatherKind.AbyssalDraw: return Format(Messages.StatusTooltipAbyssalDraw, who, Mathf.RoundToInt(PluginConfig.DrawRange.Value));
            case WeatherKind.AbyssalSurge:
                return Format(Messages.StatusTooltipAbyssalSurge, who, Times(PluginConfig.SerpentMultiplier.Value), Times(PluginConfig.FishMultiplier.Value));
            case WeatherKind.GlacialSatiety: return Format(Messages.StatusTooltipGlacialSatiety, who, Percent(PluginConfig.FoodFloor.Value));
            case WeatherKind.VeinShatter:
                return Format(Messages.StatusTooltipVeinShatter, who, Percent(PluginConfig.VeinSpread.Value), Percent(PluginConfig.VeinShatterAt.Value));
            case WeatherKind.CarapaceTemper: return Format(Messages.StatusTooltipCarapaceTemper, who, Percent(GearWear.Multiplier - 1f));
            case WeatherKind.GraveBind: return Format(Messages.StatusTooltipGraveBind, who);
            case WeatherKind.SkyCall: return Format(Messages.StatusTooltipSkyCall, who);
            case WeatherKind.Skal: return Format(Messages.StatusTooltipSkal, who);
            case WeatherKind.AnvilSpark: return Format(Messages.StatusTooltipAnvilSpark, who);
            case WeatherKind.Eldhrimnir: return Format(Messages.StatusTooltipEldhrimnir, who);
            case WeatherKind.RallyHorn: return Format(Messages.StatusTooltipRallyHorn, who);
            case WeatherKind.FortunesBoon:
                int penalty = PluginConfig.ForgeBreakPenalty.Value;
                string forge = penalty > 0 ? string.Format(Messages.FortuneForgeLoses, Value(penalty.ToString())) : Messages.FortuneForgeKeeps;
                return Format(Messages.StatusTooltipFortunesBoon, who, Times(PluginConfig.LuckMultiplier.Value), forge);
        }
        string plain = WeatherKinds.Plain(kind);
        string what = plain.Length > 0 ? char.ToUpperInvariant(plain[0]) + plain.Substring(1) : plain;
        string leftOut = PluginConfig.AffectedBiomes.Value.LeftOut();
        return string.IsNullOrEmpty(leftOut)
            ? Format(Messages.StatusTooltipNoBiomes, what, who)
            : Format(Messages.StatusTooltip, what, who, leftOut);
    }

    private static int Meters(WeatherKind kind) => Mathf.RoundToInt(PluginConfig.Radius(kind));

    // A multiplier as a percent for the tooltips: 0.75 -> 75.
    private static int Percent(float value) => Mathf.RoundToInt(value * 100f);

    // "3", "2.5"
    private static string Times(float value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    // The status, without its icon yet: also listed with the game's statuses for addstatus (see StatusCommands).
    internal static StatusEffect Template(WeatherKind kind)
    {
        if (!Effects.TryGetValue(kind, out StatusEffect effect) || !effect)
        {
            effect = ScriptableObject.CreateInstance<StatusEffect>();
            effect.name = StatusCommands.Name(kind);
            effect.m_name = WeatherKinds.Name(kind);
            effect.m_tooltip = string.Empty;
            effect.m_ttl = 0f;
            // A kinship rite's status is a wait: crossed out in red, like the game's mead cooldowns.
            effect.m_cooldownIcon = WeatherKinds.ForEveryone(kind);
            effect.hideFlags = HideFlags.HideAndDontSave;
            Effects[kind] = effect;
        }
        return effect;
    }

    private static StatusEffect Effect(WeatherKind kind)
    {
        StatusEffect effect = Template(kind);
        // The HUD only lists effects with an icon.
        if (!effect.m_icon) effect.m_icon = WeatherIcons.For(kind);
        return effect;
    }
}
