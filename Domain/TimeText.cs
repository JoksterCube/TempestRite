using System;

namespace JoksterCube.TempestRite.Domain;

internal static class TimeText
{
    // "45 s", "30 min", "1 h", "1 h 30 min". Rounded up, so time left never reads 0 while something goes on.
    internal static string Duration(float seconds)
    {
        if (seconds < 60f) return $"{Math.Max(1, (int)Math.Ceiling(seconds))} s";
        int minutes = (int)Math.Ceiling(seconds / 60f - 0.001f);
        if (minutes < 60) return $"{minutes} min";
        int hours = minutes / 60;
        int rest = minutes % 60;
        return rest == 0 ? $"{hours} h" : $"{hours} h {rest} min";
    }
}
