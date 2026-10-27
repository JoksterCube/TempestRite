using System.Text;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The lines added to the obliterator's lever and hatch hover text, and to rite items' tooltips in it.
internal static class ObliteratorHints
{
    private static readonly StringBuilder Text = new();

    // Lever: the rite pulling it performs.
    internal static string Lever(Incinerator incinerator)
    {
        WeatherKind kind = ObliteratorKeys.Pick(incinerator.m_container.GetInventory(), out _, out _);
        if (kind == WeatherKind.None || !ObliteratorKeys.IsKnown(kind)) return string.Empty;
        if (!WeatherSync.LocalPlayerMayChange()) return "\n" + Messages.LeverNotAllowed;
        return "\n" + string.Format(Messages.LeverBrings, WeatherKinds.Colored(kind), Explain(kind));
    }

    // Hatch: every item that performs a rite.
    internal static string Hatch()
    {
        Text.Clear();
        foreach (WeatherKind kind in ObliteratorKeys.Known())
        {
            if (Text.Length == 0) Text.Append('\n').Append(Messages.HatchTitle);
            Text.Append('\n').AppendFormat(Messages.HatchLine, ObliteratorKeys.ItemLabel(kind), WeatherKinds.Colored(kind), Explain(kind));
        }
        if (Text.Length > 0 && !WeatherSync.LocalPlayerMayChange()) Text.Append('\n').Append(Messages.LeverNotAllowed);
        return Text.ToString();
    }

    // Item tooltip in the open obliterator: whether this item decides the rite, needs more of it, or just burns.
    // keyItem: the deciding item, or null when none has enough in.
    internal static string Tooltip(Inventory inventory, ItemDrop.ItemData item, ItemDrop.ItemData? keyItem, WeatherKind kind, bool several)
    {
        if (item == keyItem)
            return ObliteratorKeys.IsKnown(kind)
                ? string.Format(several && Explaining ? Messages.TooltipDecidesOfSeveral : Messages.TooltipDecides, WeatherKinds.Colored(kind), Explain(kind))
                : string.Empty;
        WeatherKind own = ObliteratorKeys.KindOf(item);
        if (own == WeatherKind.None || !ObliteratorKeys.IsKnown(own)) return string.Empty;
        int amount = ObliteratorKeys.Amount(own);
        int count = ObliteratorKeys.Count(inventory, own);
        if (count < amount)
            return Explaining
                ? string.Format(Messages.TooltipNeedsMore, amount, WeatherKinds.Colored(own), count)
                : string.Format(Messages.TooltipNeedsMoreShort, amount);
        return Explaining ? Messages.TooltipOverruled : Messages.TooltipOverruledShort;
    }

    internal static bool Explaining => PluginConfig.ShowExplanations.IsOn();

    // " (thunderstorm)" in gray after a rite's name, or nothing when Show Explanations is off.
    internal static string Explain(WeatherKind kind) => Explaining ? string.Format(Messages.Explanation, WeatherKinds.Plain(kind)) : string.Empty;
}
