using System;
using System.Collections.Generic;

namespace JoksterCube.TempestRite.Common;

// Keeps a broken feature (usually after a game update) from throwing every frame inside vanilla code:
// the first error is logged and that feature is switched off for the rest of the game session.
internal static class PatchGuard
{
    private static readonly HashSet<string> Disabled = [];

    internal static bool IsDisabled(string feature) => Disabled.Contains(feature);

    internal static void Fail(string feature, Exception exception)
    {
        if (Disabled.Add(feature))
            Plugin.ModLogger.LogError($"{feature} disabled after an unexpected error. Please report it with this log.\n{exception}");
    }
}
