using System;
using JoksterCube.AbilityKit;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;
using Keys = JoksterCube.ChestKit.Common.KeyboardShortcutExtensions;

namespace JoksterCube.TempestRite.Domain;

// Whether the Domain rites' area edges (see DomainVisuals) show, for this player only.
// - Standing inside a Domain rite's area: a Domain Border slot beside the Guardian Power (see AbilityKit), a switch with
//   no cooldown, On or Off under it and a red cross over it when off, switched by the Domain Border Shortcut. On by
//   default; the choice is kept with the character, so the next area starts the way the player left it.
// - Just left: the edges (if on) and the slot stay for Linger Seconds, the time left under the slot.
// - Elsewhere: no edges and no slot.
// Always Show Domain Border shows every edge nearby, as before, and gives no slot.
internal static class DomainBorder
{
    private const string Feature = "Domain border";
    private const string SlotId = "TempestRite_DomainBorder";
    private const float LingerSeconds = 5f;
    // Kept with the character while the player has switched it off.
    private const string SaveKey = "TempestRite_DomainBorderOff";

    private static readonly AbilitySlot Slot = new(SlotId, SlotPriority.TempestRiteDomainBorder);

    private static bool _inside;
    private static float _leftAt = float.NegativeInfinity;

    internal static void ClearSession()
    {
        _inside = false;
        _leftAt = float.NegativeInfinity;
    }

    internal static bool AlwaysShow => PluginConfig.AlwaysShowDomainBorder.IsOn();

    private static bool HasShortcut => PluginConfig.DomainBorderShortcut.Value.MainKey != KeyCode.None;

    private static float LingerLeft => Mathf.Max(0f, _leftAt + LingerSeconds - Time.time);

    // While the slot shows, the key works.
    private static bool Offered => !AlwaysShow && HasShortcut && (_inside || LingerLeft > 0f);

    private static bool IsOn(Player player) => !player.m_customData.ContainsKey(SaveKey);

    // Every frame, from DomainVisuals: inside, whether the local player stands in any Domain rite's area now (false
    // while the effects are off). Returns whether the edges show.
    internal static bool Update(Player? player, bool inside)
    {
        if (inside) _inside = true;
        else if (_inside)
        {
            _inside = false;
            _leftAt = Time.time;
        }
        if (!player) return false;
        if (AlwaysShow) return true;
        if (Offered && !player!.IsDead() && RallyHorn.CanTakeInput(player)
            && Keys.IsDownWhileMoving(PluginConfig.DomainBorderShortcut.Value))
            Switch(player);
        return (_inside || LingerLeft > 0f) && IsOn(player!);
    }

    private static void Switch(Player player)
    {
        if (IsOn(player)) player.m_customData[SaveKey] = "1";
        else player.m_customData.Remove(SaveKey);
    }

    // The slot beside the Guardian Power, from the game's own update of that slot.
    internal static void UpdateHud(Hud hud)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Player? player = Player.m_localPlayer;
            bool show = player && !player!.IsDead() && PluginConfig.ShowDomainEffects.IsOn() && Offered;
            bool on = show && IsOn(player!);
            string text = !show ? string.Empty
                : _inside ? (on ? Messages.AbilityOn : Messages.AbilityOff)
                : AbilitySlot.TimeText(LingerLeft);
            Slot.Update(hud, show, WeatherIcons.Bounds(), Messages.DomainBorderName, text, ready: true,
                crossed: show && !on);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
