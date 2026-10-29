using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// Whether an obliterator performs rites. Off (as built) it burns everything as without the mod; the switch on its flap
// turns it on. Saved on the obliterator itself, in its network data (ZDO), so everyone sees the same and it stays with
// the world.
internal static class ObliteratorSwitch
{
    private static readonly int Key = $"{Constants.Plugin.ModGUID}.RitesOn".GetStableHashCode();

    internal static bool IsOn(Incinerator incinerator) => incinerator && IsOn(incinerator.m_container);

    internal static bool IsOn(Container container)
    {
        ZDO? zdo = container && container.m_nview ? container.m_nview.GetZDO() : null;
        return zdo != null && zdo.GetBool(Key);
    }

    // The container panel only shows containers this game owns, so the switch is written here and reaches everyone
    // else with the obliterator's next update. Only players who may perform rites can turn it.
    internal static void Toggle(Container container)
    {
        if (!container || !container.m_nview || container.m_nview.GetZDO() == null) return;
        if (!WeatherSync.LocalPlayerMayChange())
        {
            PlayerMessages.Warning(Messages.RefusedAdminsOnly);
            return;
        }
        if (!container.m_nview.IsOwner())
        {
            PlayerMessages.Warning(Messages.SwitchNotOwner);
            return;
        }
        bool on = !IsOn(container);
        container.m_nview.GetZDO().Set(Key, on);
        PlayerMessages.Center(on ? Messages.SwitchedOn : Messages.SwitchedOff);
    }
}
