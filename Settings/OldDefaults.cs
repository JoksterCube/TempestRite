using System;
using System.IO;
using System.Linq;

namespace JoksterCube.TempestRite.Settings;

// Moves settings still on an old default to the new one, once. A setting lists the defaults it had before, with the
// version that changed each. When the config file was last saved by an older version, a value equal to that old
// default becomes the new default; any other value was chosen by the player and stays.
internal static class OldDefaults
{
    // The mod version that last saved the config file, from the header BepInEx writes on its first line
    // ("## Settings file was created by plugin TempestRite v1.1.3"). Null for a new or unreadable file.
    internal static System.Version? SavedBy(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            return ParseHeader(File.ReadLines(path).FirstOrDefault());
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static System.Version? ParseHeader(string? line)
    {
        if (line == null || !line.StartsWith("##")) return null;
        int at = line.LastIndexOf(" v", StringComparison.Ordinal);
        if (at < 0) return null;
        return System.Version.TryParse(line.Substring(at + 2).Trim(), out System.Version version) ? version : null;
    }

    // Whether a value still on oldDefault should move to the default from changedIn.
    internal static bool Applies(System.Version? savedBy, string changedIn) =>
        savedBy != null && System.Version.TryParse(changedIn, out System.Version changed) && savedBy < changed;

    // Lists and environment tables match whatever their spacing or case.
    internal static bool Same<T>(T value, T oldDefault)
    {
        if (value is string text && oldDefault is string old)
            return string.Equals(Compact(text), Compact(old), StringComparison.OrdinalIgnoreCase);
        return Equals(value, oldDefault);
    }

    private static string Compact(string text) => new(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
}
