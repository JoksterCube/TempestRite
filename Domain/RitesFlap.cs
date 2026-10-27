using JoksterCube.ChestKit.Ui;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// This mod's flap under the container panel (ChestKit's ChestFlap), shown while the obliterator is open. Its one
// button opens the rites page (see RitesPanel) in the obliterator's panel, and closes it again.
internal static class RitesFlap
{
    private static ChestFlap? _flap;
    private static Container? _container;

    internal static void Update(InventoryGui gui)
    {
        // No flap when every rite is turned off: its page would be empty.
        Container? open = PluginConfig.ShowRitesFlap.IsOn() && AnyRite() ? OpenObliterator(gui) : null;
        if (open) _container = open;

        RitesPanel.Update(gui, open);

        _flap ??= CreateFlap();
        _flap.Update(gui, open, canGrow: false);
        if (!_flap.Exists || _flap.Buttons.Count == 0) return;

        UiParts.IconButton button = _flap.Buttons[0];
        if (!button.Icon.sprite) button.Icon.sprite = WeatherIcons.For(WeatherKind.Thunderstorm);
        // Just the name: the craft button's tooltip shows only its text.
        button.SetTooltip("", Messages.FlapTooltip);
    }

    private static bool AnyRite()
    {
        foreach (WeatherKind _ in ObliteratorKeys.Enabled()) return true;
        return false;
    }

    private static ChestFlap CreateFlap()
    {
        ChestFlap flap = new(Constants.Plugin.ModName, Constants.Plugin.FlapPriority);
        flap.AddButton("TempestRiteRites", OnRites);
        return flap;
    }

    // The obliterator shown in the inventory screen. The game only shows containers this game owns.
    private static Container? OpenObliterator(InventoryGui gui)
    {
        if (!InventoryGui.IsVisible() || !gui.m_container || !gui.m_container.gameObject.activeInHierarchy) return null;
        Container? container = gui.m_currentContainer;
        return container && container!.IsOwner() && ObliteratorKeys.IsObliteratorHatch(container, out _) ? container : null;
    }

    private static void OnRites()
    {
        if (_container && _flap is { IsShown: true } && InventoryGui.instance) RitesPanel.Toggle(InventoryGui.instance, _container!);
    }

    // The inventory screen is rebuilt for every session, so the flap is made again next time.
    internal static void ClearSession()
    {
        RitesPanel.ClearSession();
        _flap?.Clear();
        _container = null;
    }
}
