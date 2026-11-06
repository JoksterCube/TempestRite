using System.Linq;
using JoksterCube.TempestRite.Domain;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

// On a server without the mod only the rites each player's own game can do are offered.
public class ServerWithoutModTests
{
    [Theory]
    [InlineData((int)WeatherKind.Unmaking)]
    [InlineData((int)WeatherKind.GraveBind)]
    [InlineData((int)WeatherKind.MorgenPouch)]
    [InlineData((int)WeatherKind.Skal)]
    [InlineData((int)WeatherKind.Eldhrimnir)]
    public void Works(int kind) => Assert.True(WeatherKinds.WorksWithoutServerMod((WeatherKind)kind));

    [Theory]
    [InlineData((int)WeatherKind.SoulTether)]
    [InlineData((int)WeatherKind.RallyHorn)]
    [InlineData((int)WeatherKind.Thunderstorm)]
    [InlineData((int)WeatherKind.Verdant)]
    [InlineData((int)WeatherKind.HearthHaul)]
    [InlineData((int)WeatherKind.Crimson)]
    [InlineData((int)WeatherKind.LeySurvey)]
    [InlineData((int)WeatherKind.Sanctuary)]
    public void Needs_the_server(int kind) => Assert.False(WeatherKinds.WorksWithoutServerMod((WeatherKind)kind));

    [Fact]
    public void Only_blessings_kinship_and_unmaking() =>
        Assert.All(WeatherKinds.All.Where(WeatherKinds.WorksWithoutServerMod), kind =>
            Assert.True(kind == WeatherKind.Unmaking || WeatherKinds.Personal(kind) || WeatherKinds.ForEveryone(kind)));
}
