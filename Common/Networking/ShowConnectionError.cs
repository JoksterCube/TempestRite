using HarmonyLib;

namespace JoksterCube.TempestRite.Common.Networking;

[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.ShowConnectError))]
internal sealed class ShowConnectionError
{
    private static void Postfix(FejdStartup __instance)
    {
        if (__instance.m_connectionFailedPanel.activeSelf && Plugin.ConnectionError.Length > 0)
        {
            __instance.m_connectionFailedError.fontSizeMax = 25;
            __instance.m_connectionFailedError.fontSizeMin = 15;
            __instance.m_connectionFailedError.text += $"\n{Plugin.ConnectionError}";
        }
    }
}