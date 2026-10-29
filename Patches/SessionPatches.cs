using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Domain.Networking;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

internal static class SessionPatches
{
    // ZNet makes a new routed RPC handler for every world or server session.
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    private static class RegisterRpcs
    {
        private const string Feature = "Weather sync";

        private static void Postfix()
        {
            if (PatchGuard.IsDisabled(Feature) || ZRoutedRpc.instance == null) return;
            try
            {
                WeatherSync.Register(ZRoutedRpc.instance);
                VeinShatterPatch.Register(ZRoutedRpc.instance);
                KinshipRites.Register(ZRoutedRpc.instance);
                DomainAnchors.Register(ZRoutedRpc.instance);
                CrimsonRaids.Register(ZRoutedRpc.instance);
                CrimsonArena.Register(ZRoutedRpc.instance);
                RagnarokRush.Register(ZRoutedRpc.instance);
                RagnarokArena.Register(ZRoutedRpc.instance);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // ZNet is destroyed whenever a world or server session ends (logout, disconnect, server shutdown).
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
    private static class ResetSession
    {
        private const string Feature = "Session reset";

        // The server saves what is going on before forgetting it.
        private static void Prefix()
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { WeatherStore.Save(); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Postfix()
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { SessionState.Clear(); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Server with the mod: after it hands out the objects near a player, weather-sensitive ones go to players with the
    // mod (see WeatherOwnership).
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ReleaseNearbyZDOS))]
    private static class WeatherOwners
    {
        private const string Feature = "Weather object owners";

        private static void Postfix(ZDOMan __instance, Vector3 refPosition, long uid)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { WeatherOwnership.AfterServerRelease(__instance, refPosition, uid); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}
