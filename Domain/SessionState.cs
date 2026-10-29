using JoksterCube.TempestRite.Common.Networking;
using JoksterCube.TempestRite.Domain.Networking;

namespace JoksterCube.TempestRite.Domain;

// Single place that forgets everything tied to one world/server connection, so joining another server or world
// starts clean. Synced settings are put back to this computer's own values by ServerSync when leaving a server.
internal static class SessionState
{
    internal static void Clear()
    {
        WeatherState.Clear();
        WeatherSync.ClearSession();
        WeatherEnvironment.ClearSession();
        WeatherOwnership.ClearSession();
        WeatherStore.ClearSession();
        CrimsonRaids.ClearSession();
        IncursionArena.ClearSession();
        RagnarokRush.ClearSession();
        RiteSpeedup.ClearSession();
        RiteEffects.ClearSession();
        DomainVisuals.ClearSession();
        DomainAnchors.ClearSession();
        CreatureIcons.ClearSession();
        Patches.CreaturePatches.ClearSession();
        Blessings.ClearSession();
        KinshipRites.ClearSession();
        DomainWork.ClearSession();
        GearWear.ClearSession();
        GraveBeacon.ClearSession();
        GraveGear.ClearSession();
        BlessingLights.ClearSession();
        ObliteratorPull.ClearSession();
        ObliteratorKeys.ClearSession();
        RitesFlap.ClearSession();
        Patches.DaylightPatch.ClearSession();
        Patches.SootfallPatch.ClearSession();
        // The next server is checked again.
        RpcHandlers.ResetServerVersionCheck();
    }
}
