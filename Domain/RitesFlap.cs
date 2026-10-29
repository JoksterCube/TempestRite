using JoksterCube.ChestKit.Ui;
using JoksterCube.TempestRite.Settings;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// This mod's flap under the container panel (ChestKit's ChestFlap), shown while the obliterator is open. It holds two
// buttons, top to bottom: the rites page (see RitesPanel) and the switch. The switch turns the obliterator's rites on
// or off (see ObliteratorSwitch): a red cross means off, the same as in FuelChests. Only the switch shows below the
// panel; while the rites are on, pointing at the flap (or having the rites page open) slides it further down to show the
// page button too. While they are off it stays put.
internal static class RitesFlap
{
    private const int PageButton = 0;
    private const int SwitchButton = 1;

    private static ChestFlap? _flap;
    private static Container? _container;

    internal static void Update(InventoryGui gui)
    {
        // No flap when every rite is turned off: there is nothing to perform.
        Container? open = AnyRite() ? OpenObliterator(gui) : null;
        if (open)
        {
            _container = open;
            RiteDiscovery.FoundObliterator();
        }

        // The rites page only goes with rites that are on: turning them off closes it.
        bool on = open && ObliteratorSwitch.IsOn(open!);
        RitesPanel.Update(gui, on ? open : null);

        _flap ??= CreateFlap();
        _flap.Update(gui, open, canGrow: on, keepGrown: on && RitesPanel.IsOpen);
        if (!_flap.Exists || _flap.Buttons.Count < 2) return;

        // While sliding away, the buttons keep showing the obliterator they were last about.
        if (!_container) return;
        UiParts.IconButton page = _flap.Buttons[PageButton];
        UiParts.IconButton toggle = _flap.Buttons[SwitchButton];
        if (!page.Icon.sprite) page.Icon.sprite = WeatherIcons.RitesBook();
        if (!toggle.Icon.sprite) toggle.Icon.sprite = WeatherIcons.Flap();
        toggle.SetCrossed(!ObliteratorSwitch.IsOn(_container!));
        // Just the name: the craft button's tooltip shows only its text.
        page.SetTooltip("", Messages.PageTooltip);
        toggle.SetTooltip("", Messages.SwitchTooltip);
    }

    private static bool AnyRite()
    {
        foreach (WeatherKind _ in ObliteratorKeys.Enabled()) return true;
        return false;
    }

    private static ChestFlap CreateFlap()
    {
        ChestFlap flap = new(Constants.Plugin.ModName, Constants.Plugin.FlapPriority);
        flap.AddButton("TempestRiteRites", OnPage);
        flap.AddButton("TempestRiteSwitch", OnSwitch);
        return flap;
    }

    // The obliterator shown in the inventory screen. The game only shows containers this game owns.
    private static Container? OpenObliterator(InventoryGui gui)
    {
        if (!InventoryGui.IsVisible() || !gui.m_container || !gui.m_container.gameObject.activeInHierarchy) return null;
        Container? container = gui.m_currentContainer;
        return container && container!.IsOwner() && ObliteratorKeys.IsObliteratorHatch(container, out _) ? container : null;
    }

    private static void OnPage()
    {
        if (_container && _flap is { IsGrown: true } && InventoryGui.instance) RitesPanel.Toggle(InventoryGui.instance, _container!);
    }

    private static void OnSwitch()
    {
        if (_container && _flap is { IsShown: true }) ObliteratorSwitch.Toggle(_container!);
    }

    // The inventory screen is rebuilt for every session, so the flap is made again next time.
    internal static void ClearSession()
    {
        RitesPanel.ClearSession();
        _flap?.Clear();
        _container = null;
    }
}
