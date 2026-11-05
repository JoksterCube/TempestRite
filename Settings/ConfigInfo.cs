using System.Collections.Generic;
using BepInEx.Configuration;

namespace JoksterCube.TempestRite.Settings;

internal sealed class ConfigInfo<T>
{
    internal string Group { get; }
    internal string Name { get; }
    internal ConfigDescription Description { get; }
    internal T DefaultValue { get; }
    internal bool Synchronized { get; }
    // Hidden behind "Advanced settings" in Configuration Manager.
    internal bool IsAdvanced { get; set; }
    // Defaults this setting had before, with the version that changed each (see OldDefaults).
    internal List<(string ChangedIn, T Value)> OldDefaultValues { get; } = [];

    internal ConfigInfo<T> OldDefault(string changedIn, T value)
    {
        OldDefaultValues.Add((changedIn, value));
        return this;
    }

    internal ConfigInfo(string group, string name, string description, T defaultValue, bool synchronized = true) :
        this(group, name, new ConfigDescription(description), defaultValue, synchronized)
    { }

    internal ConfigInfo(string group, string name, ConfigDescription description, T defaultValue, bool synchronized = true)
    {
        Group = group;
        Name = name;
        Description = description;
        DefaultValue = defaultValue;
        Synchronized = synchronized;
    }
}