using System;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Fortune's Boon Rite raises the game's own lucky craft chance (crafting, the cauldron, the cooking station) for as
// long as the blessing lasts, not only during a craft, so whatever reads it sees the blessed chance too: the odds a
// crafting mod shows in the recipe panel, for one. Put back as soon as the blessing ends.
internal static class FortuneLuck
{
    private const string Feature = "Fortune's Boon luck";

    private static InventoryGui? _gui;
    // The game's chance without the blessing, and the one written over it (-1: not raised).
    private static float _base;
    private static float _raised = -1f;

    // A chance as raised by the Luck Multiplier, up to always.
    internal static float Raised(float chance) => Mathf.Min(1f, chance * Mathf.Max(1f, PluginConfig.LuckMultiplier.Value));

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            InventoryGui? gui = InventoryGui.instance;
            if (gui != _gui)
            {
                // A new panel (another world or character) starts from its own value.
                _gui = gui;
                _raised = -1f;
            }
            if (!gui) return;

            if (Blessings.IsActive(WeatherKind.FortunesBoon))
            {
                // Something else changed the chance meanwhile: that is the new unblessed one.
                if (_raised < 0f || !Mathf.Approximately(gui!.m_craftBonusChance, _raised)) _base = gui!.m_craftBonusChance;
                _raised = Raised(_base);
                gui.m_craftBonusChance = _raised;
            }
            else Restore();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    internal static void ClearSession()
    {
        Restore();
        _gui = null;
    }

    private static void Restore()
    {
        if (_raised < 0f) return;
        if (_gui && Mathf.Approximately(_gui!.m_craftBonusChance, _raised)) _gui.m_craftBonusChance = _base;
        _raised = -1f;
    }
}
