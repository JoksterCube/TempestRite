using JoksterCube.TempestRite.Settings;
using static MessageHud;

namespace JoksterCube.TempestRite.Common;

internal static class PlayerMessages
{
    internal static void Center(string message)
    {
        if (Player.m_localPlayer && PluginConfig.ShowMessages.IsOn())
            Player.m_localPlayer.Message(MessageType.Center, message);
    }

    internal static void TopLeft(string message)
    {
        if (Player.m_localPlayer && PluginConfig.ShowMessages.IsOn())
            Player.m_localPlayer.Message(MessageType.TopLeft, message);
    }

    // Refusals are always shown, whatever Show Messages is set to.
    internal static void Warning(string message)
    {
        if (Player.m_localPlayer) Player.m_localPlayer.Message(MessageType.Center, message);
    }
}
