using System.Collections.Generic;
using JoksterCube.ChestKit.Ui;
using JoksterCube.TempestRite.Settings;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// This mod's flap under the container panel (ChestKit's ChestFlap), shown while the obliterator is open. It holds three
// buttons, top to bottom: the rites going on here (see ActivePanel), the rites page (see RitesPanel) and the switch.
// The switch turns the obliterator's rites on or off (see ObliteratorSwitch): a red cross means off, the same as in
// FuelChests. Only the switch shows below the panel; pointing at the flap (or having one of its pages open) slides it
// further down to show the others. The hourglass only shows while rites performed at this obliterator go on (the
// Domain rites, Crimson, Ragnarök), its tooltip listing them with their time left; the rites page only while the
// rites are on. With neither, the flap stays put.
internal static class RitesFlap
{
    private const int ActiveButton = 0;
    private const int PageButton = 1;
    private const int SwitchButton = 2;

    private static ChestFlap? _flap;
    private static Container? _container;
    private static readonly List<WeatherKind> Active = [];

    internal static void Update(InventoryGui gui)
    {
        Container? obliterator = OpenObliterator(gui);
        Active.Clear();
        if (obliterator) ObliteratorHints.ActiveAt(obliterator!.transform.position, Active);
        bool active = Active.Count > 0;

        // No flap when every rite is turned off and none goes on here: there is nothing to do.
        Container? open = AnyRite() || active ? obliterator : null;
        if (open)
        {
            _container = open;
            RiteDiscovery.FoundObliterator();
        }

        // The rites page only goes with rites that are on: turning them off closes it.
        bool on = open && ObliteratorSwitch.IsOn(open!);
        RitesPanel.Update(gui, on ? open : null);
        ActivePanel.Update(gui, open);

        _flap ??= CreateFlap();
        // While sliding away, the flap keeps the buttons it had.
        if (open)
        {
            _flap.SetButtonShown(ActiveButton, active);
            _flap.SetButtonShown(PageButton, on);
        }
        _flap.Update(gui, open, canGrow: on || active, keepGrown: (on && RitesPanel.IsOpen) || ActivePanel.IsOpen);
        if (!_flap.Exists || _flap.Buttons.Count < 3) return;

        // While sliding away, the buttons keep showing the obliterator they were last about.
        if (!_container) return;
        UiParts.IconButton hourglass = _flap.Buttons[ActiveButton];
        UiParts.IconButton page = _flap.Buttons[PageButton];
        UiParts.IconButton toggle = _flap.Buttons[SwitchButton];
        if (!hourglass.Icon.sprite) hourglass.Icon.sprite = WeatherIcons.Hourglass();
        if (!page.Icon.sprite) page.Icon.sprite = WeatherIcons.RitesBook();
        if (!toggle.Icon.sprite) toggle.Icon.sprite = WeatherIcons.Flap();
        toggle.SetCrossed(!ObliteratorSwitch.IsOn(_container!));
        if (active) hourglass.SetTooltip(Messages.ActiveTooltip, ObliteratorHints.ActiveLines(_container!.transform.position));
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
        ChestFlap flap = new(Constants.Plugin.ModName, FlapPriority.TempestRite);
        flap.AddButton("TempestRiteActive", OnActive);
        flap.AddButton("TempestRiteRites", OnPage);
        flap.AddButton("TempestRiteSwitch", OnSwitch);
        return flap;
    }

    // The obliterator shown in the inventory screen. Not only for its owner: with MultiUserChest other players have it
    // open too, and their switch is sent to the owner (see ObliteratorSwitch).
    private static Container? OpenObliterator(InventoryGui gui)
    {
        if (!InventoryGui.IsVisible() || !gui.m_container || !gui.m_container.gameObject.activeInHierarchy) return null;
        Container? container = gui.m_currentContainer;
        return container && ObliteratorKeys.IsObliteratorHatch(container!, out _) ? container : null;
    }

    private static void OnActive()
    {
        if (_container && _flap is { IsGrown: true } && InventoryGui.instance) ActivePanel.Toggle(InventoryGui.instance, _container!);
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
        ActivePanel.ClearSession();
        _flap?.Clear();
        _container = null;
    }
}
