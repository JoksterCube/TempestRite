using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// Pulling the lever only asks the obliterator's owner to burn; it answers a few seconds later, and may refuse (in use,
// empty). The rites are chosen when the lever is pulled, from what is in the obliterator then, and performed only once
// the owner says it burnt.
internal static class ObliteratorPull
{
    // The owner answers after 5 to 7 seconds of lever animation.
    private const float AnswerTimeout = 30f;

    private static Incinerator? _incinerator;
    private static readonly List<WeatherKind> Kinds = [];
    private static float _pulledAt;

    internal static void ClearSession()
    {
        _incinerator = null;
        Kinds.Clear();
    }

    // Before the lever moves: why the rites in the obliterator can't be performed now (a Crimson or Ragnarök Rite going
    // on), or null. Refused, nothing burns, so the key items aren't lost.
    internal static string? Refused(Incinerator incinerator)
    {
        if (!ObliteratorSwitch.IsOn(incinerator)) return null;
        Vector3 at = incinerator.transform.position;
        foreach (ObliteratorKeys.Picked p in ObliteratorKeys.PickAll(incinerator.m_container.GetInventory()))
            if (PluginConfig.IsEnabled(p.Kind) && IncursionArena.RiteRefused(p.Kind, at) is { } why) return why;
        return null;
    }

    internal static void Pulled(Incinerator incinerator)
    {
        ClearSession();
        // Rites off: it burns as without the mod.
        if (!ObliteratorSwitch.IsOn(incinerator)) return;
        IReadOnlyList<ObliteratorKeys.Picked> picked = ObliteratorKeys.PickAll(incinerator.m_container.GetInventory());
        if (picked.Count == 0) return;
        // The items burn either way, as without the mod.
        if (!WeatherSync.LocalPlayerMayChange())
        {
            PlayerMessages.Warning(Messages.RefusedAdminsOnly);
            return;
        }
        _incinerator = incinerator;
        foreach (ObliteratorKeys.Picked p in picked) Kinds.Add(p.Kind);
        _pulledAt = Time.time;
    }

    // response: Incinerator.Response (0 fail / in use, 1 burnt, 2 burnt into something, 3 empty).
    internal static void Answered(Incinerator incinerator, int response)
    {
        if (_incinerator != incinerator || Kinds.Count == 0) return;
        List<WeatherKind> kinds = [.. Kinds];
        bool inTime = Time.time - _pulledAt <= AnswerTimeout;
        ClearSession();
        if (!inTime) return;
        if (response is (int)Incinerator.Response.Success or (int)Incinerator.Response.Conversion)
            WeatherSync.RequestChange(kinds, incinerator.transform.position);
    }
}
