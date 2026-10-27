using BepInEx.Configuration;
using System;

namespace JoksterCube.TempestRite.Common;

internal static class ToggleExtensions
{
    internal static Toggle Not(this Toggle toggle) => toggle switch
    {
        Toggle.On => Toggle.Off,
        Toggle.Off => Toggle.On,
        _ => throw new NotImplementedException()
    };

    internal static bool IsOn(this Toggle toggle) => toggle == Toggle.On;

    internal static bool IsOn(this ConfigEntry<Toggle> toggleConfig) => toggleConfig.Value.IsOn();
}