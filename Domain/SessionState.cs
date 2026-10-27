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
        VerdantGrowth.ClearSession();
        RiteEffects.ClearSession();
        Blessings.ClearSession();
        DomainWork.ClearSession();
        GearWear.ClearSession();
        GraveBeacon.ClearSession();
        BlessingLights.ClearSession();
        Patches.VeinShatterPatch.ClearSession();
        ObliteratorPull.ClearSession();
        ObliteratorKeys.ClearSession();
        RitesFlap.ClearSession();
        Patches.DaylightPatch.ClearSession();
        Patches.SootfallPatch.ClearSession();
        // The next server is checked again.
        RpcHandlers.ResetServerVersionCheck();
    }
}
