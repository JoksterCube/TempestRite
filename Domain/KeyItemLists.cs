using System;
using System.Collections.Generic;

namespace JoksterCube.TempestRite.Domain;

// A rite's Key Item setting is a list of prefab names: any mix of them counts toward its Key Item Amount. An item
// belongs to one rite only; when several list it, the first rite given keeps it.
internal static class KeyItemLists
{
    private static readonly char[] Separators = [',', ';'];

    // "SurtlingCore, Ruby" -> [SurtlingCore, Ruby]: trimmed, empty and repeated names left out, in order.
    internal static List<string> Parse(string? text)
    {
        List<string> items = [];
        if (string.IsNullOrEmpty(text)) return items;
        foreach (string part in text!.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            string name = part.Trim();
            if (name.Length > 0 && !items.Contains(name)) items.Add(name);
        }
        return items;
    }

    // One item listed by two rites: the rite that keeps it, and the one it is taken from.
    internal readonly struct Conflict(string item, WeatherKind kept, WeatherKind dropped)
    {
        internal string Item { get; } = item;
        internal WeatherKind Kept { get; } = kept;
        internal WeatherKind Dropped { get; } = dropped;
    }

    // Each rite's items, with those an earlier rite already lists left out. owners: item -> its rite.
    internal static Dictionary<WeatherKind, List<string>> Resolve(IEnumerable<(WeatherKind kind, string text)> rites,
        Dictionary<string, WeatherKind> owners, List<Conflict> conflicts)
    {
        Dictionary<WeatherKind, List<string>> lists = [];
        owners.Clear();
        conflicts.Clear();
        foreach ((WeatherKind kind, string text) in rites)
        {
            List<string> own = [];
            foreach (string item in Parse(text))
            {
                if (owners.TryGetValue(item, out WeatherKind owner))
                {
                    if (owner != kind) conflicts.Add(new Conflict(item, owner, kind));
                    continue;
                }
                owners[item] = kind;
                own.Add(item);
            }
            lists[kind] = own;
        }
        return lists;
    }
}
