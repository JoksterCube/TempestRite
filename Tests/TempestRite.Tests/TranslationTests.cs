using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

// Translations/English.json and the keys Settings/Constants.cs asks for must match, so no text shows as its key and
// translators aren't handed unused ones.
public class TranslationTests
{
    private static string ProjectDir([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));

    private static Dictionary<string, string> English() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(ProjectDir(), "Translations", "English.json")))!;

    private static HashSet<string> UsedKeys() =>
        Regex.Matches(File.ReadAllText(Path.Combine(ProjectDir(), "Settings", "Constants.cs")), "Text\\.Get\\(\"([a-z0-9_]+)\"\\)")
            .Select(match => match.Groups[1].Value).ToHashSet();

    [Fact]
    public void Every_used_key_has_an_english_text() =>
        Assert.Empty(UsedKeys().Except(English().Keys));

    [Fact]
    public void Every_english_text_is_used() =>
        Assert.Empty(English().Keys.Except(UsedKeys()));

    [Fact]
    public void English_texts_format()
    {
        object[] args = Enumerable.Repeat<object>("x", 10).ToArray();
        foreach (KeyValuePair<string, string> text in English())
            _ = string.Format(text.Value, args);
    }

    // Colors around a whole text belong in code (Constants.Colored), so translators only see words.
    [Fact]
    public void English_texts_are_not_wrapped_in_markup() =>
        Assert.Empty(English().Where(text => Regex.IsMatch(text.Value, "^\\s*<[^>]+>.*</[^>]+>\\s*$", RegexOptions.Singleline))
            .Select(text => text.Key));

    [Fact]
    public void Texts_outside_the_game_are_english() =>
        Assert.Equal("Tempest Rite", Settings.Constants.Messages.ThunderstormName);
}
