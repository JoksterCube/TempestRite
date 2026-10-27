using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// Pulling the lever only asks the obliterator's owner to burn; it answers a few seconds later, and may refuse (in use,
// empty). The weather is chosen when the lever is pulled, from what is in the obliterator then, and called only once
// the owner says it burnt.
internal static class ObliteratorPull
{
    // The owner answers after 5 to 7 seconds of lever animation.
    private const float AnswerTimeout = 30f;

    private static Incinerator? _incinerator;
    private static WeatherKind _kind;
    private static float _pulledAt;

    internal static void ClearSession()
    {
        _incinerator = null;
        _kind = WeatherKind.None;
    }

    internal static void Pulled(Incinerator incinerator)
    {
        ClearSession();
        WeatherKind kind = ObliteratorKeys.Pick(incinerator.m_container.GetInventory(), out _, out _);
        if (kind == WeatherKind.None) return;
        // The items burn either way, as without the mod.
        if (!WeatherSync.LocalPlayerMayChange())
        {
            PlayerMessages.Warning(Messages.RefusedAdminsOnly);
            return;
        }
        _incinerator = incinerator;
        _kind = kind;
        _pulledAt = Time.time;
    }

    // response: Incinerator.Response (0 fail / in use, 1 burnt, 2 burnt into something, 3 empty).
    internal static void Answered(Incinerator incinerator, int response)
    {
        if (_incinerator != incinerator || _kind == WeatherKind.None) return;
        WeatherKind kind = _kind;
        bool inTime = Time.time - _pulledAt <= AnswerTimeout;
        ClearSession();
        if (!inTime) return;
        if (response is (int)Incinerator.Response.Success or (int)Incinerator.Response.Conversion)
            WeatherSync.RequestChange(kind, incinerator.transform.position);
    }
}
