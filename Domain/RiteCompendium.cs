using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The "Tempest Rite" page of the Compendium: how rites work, then each group with what its rites share, and each rite
// the player knows with its item, its time and what it does in detail. Numbers come from the settings in use (the
// server's when it syncs them). Rites the player hasn't found are only counted, as on the rites page; turned off ones
// aren't mentioned.
// Each rite's icon sits left of its name and summary: the text is indented there, the name is a link marking the
// spot, and the icons are images laid over those spots every frame, as children of the text so they scroll with it.
internal static class RiteCompendium
{
    private const string GroupHeader = "<size=120%><b><color={0}>{1}</color></b></size>\n";
    // {0}: link index, {1}: color, {2}: name, {3}: the summary line.
    private const string RiteHeader = "<indent=" + IndentEm + "em><link=\"" + LinkPrefix + "{0}\"><b><color={1}>{2}</color></b></link>\n{3}</indent>";
    private const string LinkPrefix = "tempestrite:";
    private const string IndentEm = "3.2";
    // The icon's size, in heights of the name's line, and its gap to the text in the same.
    private const float IconLines = 2.1f;
    private const float IconGap = 0.35f;
    private const string Note = "<color=" + Messages.Faint + ">{0}</color>\n";

    // The rite of each link, by its index.
    private static readonly List<WeatherKind> Targets = [];
    private static readonly List<Image> Icons = [];

    internal static string Topic => CompendiumTexts.Topic;

    internal static string BuildText()
    {
        Targets.Clear();
        StringBuilder text = new(8192);
        text.Append(CompendiumTexts.Intro).Append("\n\n").Append(CompendiumTexts.Rules).Append("\n\n");
        if (PluginConfig.WhoCanChange.Value == WeatherPermission.AdminsOnly) text.Append(CompendiumTexts.AdminsOnly).Append("\n\n");
        text.AppendFormat(Note, CompendiumTexts.Learning).Append('\n');

        List<WeatherKind> known = [.. ObliteratorKeys.Known()];
        int hidden = ObliteratorKeys.Enabled().Count() - known.Count;
        if (known.Count == 0) text.Append(CompendiumTexts.NoneKnown).Append("\n\n");

        foreach (RiteGroup group in WeatherKinds.Groups)
        {
            List<WeatherKind> rites = known.Where(kind => WeatherKinds.Group(kind) == group).ToList();
            if (rites.Count == 0) continue;
            text.AppendFormat(GroupHeader, WeatherKinds.GroupColorHex(group), WeatherKinds.GroupName(group));
            text.AppendFormat(Note, GroupText(group)).Append('\n');
            foreach (WeatherKind kind in rites)
            {
                text.AppendFormat(RiteHeader, Targets.Count, WeatherKinds.ColorHex(kind), WeatherKinds.Name(kind),
                    string.Format(Note, Summary(kind)));
                Targets.Add(kind);
                text.Append(Detail(kind)).Append("\n\n");
            }
        }

        if (hidden > 0)
            text.AppendFormat(Note, hidden == 1 ? CompendiumTexts.OneMoreToFind : string.Format(CompendiumTexts.MoreToFind, hidden));
        return text.ToString();
    }

    // Called every frame while the Compendium is open: places each rite's icon where the text has its name, and hides
    // the icons on other pages.
    internal static void UpdateIcons(TextsDialog dialog)
    {
        TMP_Text? area = dialog.m_textArea;
        TMP_Text? topic = dialog.m_textAreaTopic;
        if (!area || !topic) return;
        Icons.RemoveAll(icon => !icon);
        int placed = 0;
        if (topic!.text == Topic && area!.textInfo is { } info)
        {
            for (int link = 0; link < info.linkCount; link++)
            {
                TMP_LinkInfo linkInfo = info.linkInfo[link];
                string id = linkInfo.GetLinkID();
                if (id == null || !id.StartsWith(LinkPrefix, StringComparison.Ordinal)
                    || !int.TryParse(id.Substring(LinkPrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
                    || index < 0 || index >= Targets.Count
                    || !TryGetBounds(info, linkInfo, out Vector2 min, out Vector2 max)) continue;
                Sprite? sprite = WeatherIcons.For(Targets[index]);
                if (!sprite) continue;
                while (Icons.Count <= placed) Icons.Add(CreateIcon(area));
                Place(Icons[placed++], sprite!, min, max);
            }
        }
        for (int index = placed; index < Icons.Count; index++)
            if (Icons[index].gameObject.activeSelf) Icons[index].gameObject.SetActive(false);
    }

    // Square, left of the name, its top level with the name's.
    private static void Place(Image icon, Sprite sprite, Vector2 min, Vector2 max)
    {
        float line = max.y - min.y;
        float size = line * IconLines;
        RectTransform rect = icon.rectTransform;
        Vector2 sizeDelta = new(size, size);
        Vector3 center = new(min.x - line * IconGap - size / 2f, max.y - size / 2f, 0f);
        if (icon.sprite != sprite) icon.sprite = sprite;
        if (rect.sizeDelta != sizeDelta) rect.sizeDelta = sizeDelta;
        if (rect.localPosition != center) rect.localPosition = center;
        if (!icon.gameObject.activeSelf) icon.gameObject.SetActive(true);
    }

    private static Image CreateIcon(TMP_Text area)
    {
        GameObject gameObject = new("TempestRite_CompendiumIcon", typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)gameObject.transform;
        rect.SetParent(area.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        Image image = gameObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        gameObject.SetActive(false);
        return image;
    }

    // Where the link's characters are, in the text's own space.
    private static bool TryGetBounds(TMP_TextInfo info, TMP_LinkInfo link, out Vector2 min, out Vector2 max)
    {
        min = new Vector2(float.MaxValue, float.MaxValue);
        max = new Vector2(float.MinValue, float.MinValue);
        int end = Math.Min(link.linkTextfirstCharacterIndex + link.linkTextLength, info.characterCount);
        for (int index = Math.Max(0, link.linkTextfirstCharacterIndex); index < end; index++)
        {
            TMP_CharacterInfo character = info.characterInfo[index];
            if (!character.isVisible) continue;
            min = Vector2.Min(min, new Vector2(character.origin, character.descender));
            max = Vector2.Max(max, new Vector2(character.xAdvance, character.ascender));
        }
        return max.x > min.x && max.y > min.y;
    }

    private static string GroupText(RiteGroup group) => group switch
    {
        RiteGroup.Atmospheric => CompendiumTexts.GroupAtmospheric,
        RiteGroup.Celestial => CompendiumTexts.GroupCelestial,
        RiteGroup.BiomeMastery => CompendiumTexts.GroupBiomeMastery,
        RiteGroup.Domain => string.Format(CompendiumTexts.GroupDomain, Num(PluginConfig.DomainRadius.Value)),
        RiteGroup.Blessings => string.Format(CompendiumTexts.GroupBlessings, Num(PluginConfig.BlessingRange.Value)),
        RiteGroup.Kinship => CompendiumTexts.GroupKinship,
        RiteGroup.Reckoning => CompendiumTexts.GroupReckoning,
        _ => string.Empty
    };

    // "Burn 2x Ooze  ·  lasts 30 min"
    private static string Summary(WeatherKind kind)
    {
        string burn = string.Format(CompendiumTexts.Burn,
            $"<color={Messages.ItemColor}>{ObliteratorKeys.ItemLabel(kind)}</color>");
        string time = Duration(Seconds(kind));
        string when = kind switch
        {
            WeatherKind.Skal => string.Format(CompendiumTexts.SkalTimes, time, Duration(PluginConfig.SkalCooldown.Value * 60f)),
            WeatherKind.SkyCall or WeatherKind.AnvilSpark => string.Format(CompendiumTexts.KinshipWait, time),
            WeatherKind.Ragnarok => string.Format(CompendiumTexts.RagnarokTime, time),
            _ when WeatherKinds.Lasts(kind) => string.Format(CompendiumTexts.Lasts, time),
            _ => CompendiumTexts.AtOnce
        };
        return burn + CompendiumTexts.Separator + when;
    }

    private static string Detail(WeatherKind kind) => kind switch
    {
        WeatherKind.Clear => CompendiumTexts.Clear,
        WeatherKind.Thunderstorm => CompendiumTexts.Thunderstorm,
        WeatherKind.Rain => CompendiumTexts.Rain,
        WeatherKind.Mist => CompendiumTexts.Mist,
        WeatherKind.Aurora => CompendiumTexts.Aurora,
        WeatherKind.PolarDay => CompendiumTexts.PolarDay,
        WeatherKind.PolarNight => CompendiumTexts.PolarNight,
        WeatherKind.GaleBreath => CompendiumTexts.GaleBreath,
        WeatherKind.NoMist => CompendiumTexts.NoMist,
        WeatherKind.Sootfall => CompendiumTexts.Sootfall,
        WeatherKind.AbyssalSurge => string.Format(CompendiumTexts.AbyssalSurge, Num(PluginConfig.SerpentMultiplier.Value),
            Num(PluginConfig.FishMultiplier.Value), Num(PluginConfig.BigFishMultiplier.Value)),
        WeatherKind.Packward => string.Format(CompendiumTexts.Packward, Duration(PluginConfig.PackwardFightSeconds.Value)),
        WeatherKind.Carrion => string.Format(CompendiumTexts.Carrion, Percent(PluginConfig.CarrionSenses.Value)),
        WeatherKind.Verdant => string.Format(CompendiumTexts.Verdant, Num(PluginConfig.VerdantGrowthMultiplier.Value)),
        WeatherKind.VatBoil => string.Format(CompendiumTexts.VatBoil, Num(PluginConfig.VatBoilSpeedMultiplier.Value)),
        WeatherKind.PitchBellows => string.Format(CompendiumTexts.PitchBellows, Num(PluginConfig.PitchBellowsSpeedMultiplier.Value)),
        WeatherKind.AmberMend => CompendiumTexts.AmberMend,
        WeatherKind.SovereignWard => CompendiumTexts.SovereignWard +
            (PluginConfig.SovereignSanctuary.IsOn() ? CompendiumTexts.SovereignSanctuary : string.Empty),
        WeatherKind.Grounding => CompendiumTexts.Grounding,
        WeatherKind.CryptDelver => string.Format(CompendiumTexts.CryptDelver, Num(PluginConfig.LootMultiplier.Value),
            Percent(PluginConfig.OneExtraChance.Value), Percent(PluginConfig.TwoExtraChance.Value)),
        WeatherKind.GlacialSatiety => string.Format(CompendiumTexts.GlacialSatiety, Percent(PluginConfig.FoodFloor.Value)),
        WeatherKind.VeinShatter => PluginConfig.VeinShatterAt.Value > 0f
            ? string.Format(CompendiumTexts.VeinShatter, Percent(PluginConfig.VeinSpread.Value), Percent(PluginConfig.VeinShatterAt.Value))
            : string.Format(CompendiumTexts.VeinShatterNoBreak, Percent(PluginConfig.VeinSpread.Value)),
        WeatherKind.CarapaceTemper => string.Format(CompendiumTexts.CarapaceTemper, Percent(1f - PluginConfig.WearMultiplier.Value)),
        WeatherKind.GraveBind => CompendiumTexts.GraveBind + (PluginConfig.GraveBeacon.IsOn() ? CompendiumTexts.GraveBindBeam : string.Empty) +
            (PluginConfig.GraveReequip.IsOn() ? CompendiumTexts.GraveBindReequip : string.Empty),
        WeatherKind.FortunesBoon => string.Format(CompendiumTexts.FortunesBoon, Num(PluginConfig.LuckMultiplier.Value),
            PluginConfig.ForgeBreakPenalty.Value > 0
                ? string.Format(Messages.FortuneForgeLoses, PluginConfig.ForgeBreakPenalty.Value)
                : Messages.FortuneForgeKeeps),
        WeatherKind.Luminary => string.Format(CompendiumTexts.Luminary, Num(PluginConfig.LuminaryRange.Value)) +
            (PluginConfig.LuminaryShadows.IsOn() ? CompendiumTexts.LuminaryShadows : string.Empty),
        WeatherKind.AbyssalDraw => string.Format(CompendiumTexts.AbyssalDraw, Num(PluginConfig.DrawRange.Value)),
        WeatherKind.Tailwind => CompendiumTexts.Tailwind,
        WeatherKind.Skal => CompendiumTexts.Skal,
        WeatherKind.SkyCall => CompendiumTexts.SkyCall,
        WeatherKind.AnvilSpark => CompendiumTexts.AnvilSpark,
        WeatherKind.Crimson => string.Format(CompendiumTexts.Crimson, Duration(PluginConfig.CrimsonPause.Value),
            Num(PluginConfig.Radius(kind))) + CrimsonReward() + CompendiumTexts.CrimsonEnd,
        WeatherKind.Ragnarok => string.Format(CompendiumTexts.Ragnarok, PluginConfig.RagnarokBossCount.Value,
            Duration(PluginConfig.RagnarokPause.Value), Num(PluginConfig.Radius(kind))),
        WeatherKind.Sanctuary => CompendiumTexts.Sanctuary,
        WeatherKind.Reset => CompendiumTexts.Reset,
        WeatherKind.Unmaking => string.Format(CompendiumTexts.Unmaking, Percent(PluginConfig.UnmakingReturn.Value)) +
            (PluginConfig.UnmakingUpgradeMaterials.IsOn() ? CompendiumTexts.UnmakingUpgradeMaterials : string.Empty),
        _ => WeatherKinds.Plain(kind)
    };

    // "2x Coins" for each kill, or nothing when the reward is turned off.
    private static string CrimsonReward()
    {
        int amount = PluginConfig.CrimsonRewardPerKill.Value;
        string item = PluginConfig.CrimsonRewardItem.Value.Trim();
        if (amount <= 0 || item.Length == 0) return CompendiumTexts.CrimsonNoReward;
        string reward = $"<color={Messages.ItemColor}>{string.Format(Messages.ItemAmount, amount, ObliteratorKeys.ItemName(item))}</color>";
        return string.Format(CompendiumTexts.CrimsonReward, reward);
    }

    private static float Seconds(WeatherKind kind) => PluginConfig.For(kind)?.Seconds ?? 0f;

    private static string Duration(float seconds) => TimeText.Duration(Math.Max(0f, seconds));

    // 2.2 -> "2.2", 100 -> "100".
    private static string Num(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    // 0.75 -> "75".
    private static string Percent(float share) => Math.Round(share * 100f).ToString(CultureInfo.InvariantCulture);
}
