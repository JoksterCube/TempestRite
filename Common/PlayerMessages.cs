using System;
using System.Text;
using JoksterCube.TempestRite.Settings;
using static MessageHud;

namespace JoksterCube.TempestRite.Common;

internal static class PlayerMessages
{
    // A center message replaces the one before it, so messages said together (several rites from one burn) are
    // gathered and shown as one, a line each.
    private static readonly StringBuilder Gathered = new();
    private static int _gathering;

    // using (PlayerMessages.Together()) { ... }: center messages said inside show together at the end.
    internal static Gathering Together()
    {
        _gathering++;
        return new Gathering();
    }

    internal readonly struct Gathering : IDisposable
    {
        public void Dispose()
        {
            if (--_gathering > 0 || Gathered.Length == 0) return;
            string message = Gathered.ToString();
            Gathered.Clear();
            if (Player.m_localPlayer) Player.m_localPlayer.Message(MessageType.Center, message);
        }
    }

    internal static void Center(string message)
    {
        if (Player.m_localPlayer && PluginConfig.ShowMessages.IsOn()) Show(message);
    }

    internal static void TopLeft(string message)
    {
        if (Player.m_localPlayer && PluginConfig.ShowMessages.IsOn())
            Player.m_localPlayer.Message(MessageType.TopLeft, message);
    }

    // Refusals are always shown, whatever Show Messages is set to.
    internal static void Warning(string message)
    {
        if (Player.m_localPlayer) Show(message);
    }

    private static void Show(string message)
    {
        if (_gathering > 0)
        {
            if (Gathered.Length > 0) Gathered.Append('\n');
            Gathered.Append(message);
            return;
        }
        Player.m_localPlayer.Message(MessageType.Center, message);
    }
}
