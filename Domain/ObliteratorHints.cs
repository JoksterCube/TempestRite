using System.Collections.Generic;
using System.Text;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The lines added to the obliterator's lever and hatch hover text, and to rite items' tooltips in it.
internal static class ObliteratorHints
{
    private static readonly StringBuilder Text = new();
    private static readonly List<UnmakingReturns.Return> Returns = [];
    private static readonly List<WeatherKind> ActiveKinds = [];

    // Lever: the rites pulling it performs, one per line, then the rites going on at this obliterator.
    internal static string Lever(Incinerator incinerator) => LeverRites(incinerator) + Active(incinerator.transform.position);

    private static string LeverRites(Incinerator incinerator)
    {
        Text.Clear();
        bool on = ObliteratorSwitch.IsOn(incinerator);
        foreach (ObliteratorKeys.Picked picked in ObliteratorKeys.PickAll(incinerator.m_container.GetInventory()))
            if (ObliteratorKeys.IsKnown(picked.Kind))
                Text.Append('\n').AppendFormat(Messages.LeverBrings, WeatherKinds.Colored(picked.Kind), Explain(picked.Kind));
        if (Text.Length == 0) return string.Empty;
        if (!on) return "\n" + Messages.LeverSwitchedOff;
        return WeatherSync.LocalPlayerMayChange() ? Text.ToString() : "\n" + Messages.LeverNotAllowed;
    }

    // Hatch: every item that performs a rite (nothing while the obliterator's rites are off), then the rites going on
    // at this obliterator.
    internal static string Hatch(Container hatch) => HatchItems(hatch) + Active(hatch.transform.position);

    private static string HatchItems(Container hatch)
    {
        Text.Clear();
        if (!ObliteratorSwitch.IsOn(hatch)) return string.Empty;
        foreach (WeatherKind kind in ObliteratorKeys.Known())
        {
            if (Text.Length == 0) Text.Append('\n').Append(Messages.HatchTitle);
            Text.Append('\n').AppendFormat(Messages.HatchLine, ObliteratorKeys.ItemLabel(kind), WeatherKinds.Colored(kind), Explain(kind));
        }
        if (Text.Length > 0 && !WeatherSync.LocalPlayerMayChange()) Text.Append('\n').Append(Messages.LeverNotAllowed);
        return Text.ToString();
    }

    // The rites performed at the obliterator standing here that still go on (the Domain rites, Crimson, Ragnarök).
    internal static void ActiveAt(Vector3 at, List<WeatherKind> found)
    {
        found.Clear();
        foreach (WeatherKind kind in WeatherKinds.All)
            if (WeatherKinds.Located(kind) && WeatherState.IsActive(kind) && Utils.DistanceXZ(WeatherState.Position(kind), at) < CrimsonRaids.SamePlace)
                found.Add(kind);
    }

    // Hover text: a title, then one line per rite going on here with its time left; nothing when none is.
    internal static string Active(Vector3 at)
    {
        string lines = ActiveLines(at);
        return lines.Length == 0 ? string.Empty : "\n" + Messages.ActiveTitle + "\n" + lines;
    }

    // One line per rite going on here with its time left ("paused" while a raid holds its time still).
    internal static string ActiveLines(Vector3 at)
    {
        ActiveAt(at, ActiveKinds);
        StringBuilder text = new();
        foreach (WeatherKind kind in ActiveKinds)
        {
            string left = TimeText.Duration(WeatherState.Remaining(kind));
            string time = WeatherKinds.Untimed(kind) ? Messages.ActiveUntimed
                : string.Format(WeatherState.Suspended(kind, at) ? Messages.ActivePaused : Messages.ActiveLeft, left);
            if (text.Length > 0) text.Append('\n');
            text.AppendFormat(Messages.ActiveLine, WeatherKinds.Colored(kind), time);
        }
        return text.ToString();
    }

    // Item tooltip in the open obliterator: whether this item's rite is performed, needs more of it, or is kept out by a
    // rite before it (then the item just burns). Gear the Unmaking Rite unmakes says so, the materials shown under it.
    // picked: the rites the burn performs.
    internal static string Tooltip(Inventory inventory, ItemDrop.ItemData item, IReadOnlyList<ObliteratorKeys.Picked> picked)
    {
        WeatherKind own = ObliteratorKeys.KindOf(item);
        if (own == WeatherKind.None) return Unmade(item, picked);
        if (!ObliteratorKeys.IsKnown(own)) return string.Empty;
        if (ObliteratorKeys.Contains(picked, own)) return string.Format(Messages.TooltipDecides, WeatherKinds.Colored(own), Explain(own));
        int amount = ObliteratorKeys.Amount(own);
        int count = ObliteratorKeys.Count(inventory, own);
        if (count < amount)
            return Explaining
                ? string.Format(Messages.TooltipNeedsMore, amount, WeatherKinds.Colored(own), count)
                : string.Format(Messages.TooltipNeedsMoreShort, amount);
        WeatherKind first = ObliteratorKeys.ClashingWith(picked, own);
        return Explaining && first != WeatherKind.None ? string.Format(Messages.TooltipOverruled, WeatherKinds.Colored(first)) : Messages.TooltipOverruledShort;
    }

    private static string Unmade(ItemDrop.ItemData item, IReadOnlyList<ObliteratorKeys.Picked> picked)
    {
        if (!ObliteratorKeys.Contains(picked, WeatherKind.Unmaking) || !ObliteratorKeys.IsKnown(WeatherKind.Unmaking)) return string.Empty;
        if (!UnmakingReturns.Returns(item, Returns)) return string.Empty;
        return string.Format(Returns.Count > 0 ? Messages.TooltipUnmade : Messages.TooltipUnmadeNothing, WeatherKinds.Colored(WeatherKind.Unmaking));
    }

    internal static bool Explaining => PluginConfig.ShowExplanations.IsOn();

    // " (thunderstorm)" in gray after a rite's name, or nothing when Show Explanations is off.
    internal static string Explain(WeatherKind kind) => Explaining ? string.Format(Messages.Explanation, WeatherKinds.Plain(kind)) : string.Empty;
}
