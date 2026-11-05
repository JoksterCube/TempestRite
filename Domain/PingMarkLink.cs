using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;

namespace JoksterCube.TempestRite.Domain;

// When PingMark is installed, creatures can be outlined the way its pings and Perception outline them (for this player
// only, not counting toward its highlight limit). Called by reflection, so nothing changes without it.
internal static class PingMarkLink
{
    internal const string PingMarkGuid = "JoksterCube.PingMark";
    private const string HighlightType = "JoksterCube.PingMark.Domain.MobHighlight";

    private static MethodInfo? _highlight;
    private static bool _looked;

    internal static void Highlight(List<ZDOID> creatures, float seconds)
    {
        if (creatures.Count == 0 || !Find()) return;
        try
        {
            UserInfo me = UserInfo.GetLocalUser();
            long sender = ZNet.GetUID();
            foreach (ZDOID id in creatures)
                _highlight!.Invoke(null, [sender, id, me, false, true, (float?)seconds]);
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogWarning($"Could not outline creatures with PingMark: {exception.Message}");
            _highlight = null;
        }
    }

    private static bool Find()
    {
        if (_looked) return _highlight != null;
        _looked = true;
        try
        {
            if (!Chainloader.PluginInfos.TryGetValue(PingMarkGuid, out PluginInfo info) || !info.Instance) return false;
            Type? type = info.Instance.GetType().Assembly.GetType(HighlightType);
            _highlight = type?.GetMethod("Highlight", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null,
                [typeof(long), typeof(ZDOID), typeof(UserInfo), typeof(bool), typeof(bool), typeof(float?)], null);
            if (_highlight == null) Plugin.ModLogger.LogWarning("PingMark found, but not the call TempestRite uses: hostile creatures aren't outlined.");
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogWarning($"Could not find PingMark's outlines: {exception.Message}");
            _highlight = null;
        }
        return _highlight != null;
    }
}
