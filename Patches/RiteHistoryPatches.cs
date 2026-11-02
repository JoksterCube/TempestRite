using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Patches;

// A piece just built (a smelter, a cooking station, a sapling...) is marked as sped up to now, so it isn't owed rite
// time from before it stood there (see RiteSpeedup). The game names a piece's builder only as it is placed.
[HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
internal static class RiteHistoryPatches
{
    private const string Feature = "Rite catch-up for new pieces";

    private static void Postfix(Piece __instance)
    {
        if (PatchGuard.IsDisabled(Feature) || !__instance.m_nview || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()) return;
        try
        {
            RiteSpeedup.MarkNow(__instance.m_nview.GetZDO());
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
