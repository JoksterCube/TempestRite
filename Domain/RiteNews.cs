using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// What one burn tells the local player: a single message, however many rites it performed. Each rite whose news
// reaches the player adds a line (see WeatherState.Announce, Blessings, KinshipRites). One line shows as it is; several
// become one "rites were performed" message. A kinship rite's line wins over the rest (it says what it did to the
// player), then a Crimson, Ragnarök or Hoard-Bane Rite's.
internal static class RiteNews
{
    internal enum Weight
    {
        Plain = 0,
        Incursion = 1,
        Kinship = 2
    }

    private readonly struct Line(string text, Weight weight, bool always)
    {
        internal string Text { get; } = text;
        internal Weight Weight { get; } = weight;
        // Shown whatever Show Messages is set to.
        internal bool Always { get; } = always;
    }

    private static readonly List<Line> Lines = [];

    internal static void Begin() => Lines.Clear();

    internal static void Add(string text, Weight weight = Weight.Plain, bool always = false)
    {
        if (text.Length > 0) Lines.Add(new Line(text, weight, always));
    }

    // "Your Tempest Rite: Thor rides out..." or "Testeroo's Tempest Rite: Thor rides out...".
    internal static string Told(WeatherKind kind, bool byLocalPlayer, string calledBy, string line) => byLocalPlayer
        ? string.Format(Messages.RiteByYou, WeatherKinds.Colored(kind), line)
        : string.Format(Messages.RiteBy, Who(calledBy), WeatherKinds.Colored(kind), line);

    private static string Who(string calledBy) => string.IsNullOrEmpty(calledBy) ? Messages.SomeoneName : calledBy;

    // Shows what the burn gathered as one message.
    internal static void Tell(bool byLocalPlayer, string calledBy)
    {
        if (Lines.Count == 0) return;
        Line chosen = Lines[0];
        foreach (Line line in Lines)
            if (line.Weight > chosen.Weight) chosen = line;
        bool together = chosen.Weight == Weight.Plain && Lines.Count > 1;
        string text = !together ? chosen.Text
            : byLocalPlayer ? Messages.RitesByYou
            : string.Format(Messages.RitesBy, Who(calledBy));
        Lines.Clear();
        if (chosen.Always) PlayerMessages.Warning(text);
        else PlayerMessages.Center(text);
    }
}
