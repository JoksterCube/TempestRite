using JoksterCube.TempestRite.Domain;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

public class RagnarokPowerTests
{
    [Fact]
    public void Health_matches_the_strongest_resistances_counted()
    {
        // Eikthyr taking all damage, the strongest 25000 taking half: Eikthyr needs 50000 to last as long.
        Assert.Equal(100f, RagnarokPower.HealthMultiplier(500f, 1f, 25000f, 0.5f), 3);
        // A boss taking half needs half as much.
        Assert.Equal(50f, RagnarokPower.HealthMultiplier(500f, 0.5f, 25000f, 0.5f), 3);
        // Never weaker than itself.
        Assert.Equal(1f, RagnarokPower.HealthMultiplier(25000f, 1f, 500f, 1f));
        Assert.Equal(1f, RagnarokPower.HealthMultiplier(0f, 1f, 500f, 1f));
    }

    [Fact]
    public void Damage_matches_the_strongest()
    {
        float eikthyr = RagnarokPower.Damage("Eikthyr", 0f);
        float fader = RagnarokPower.Damage("Fader", 0f);
        Assert.Equal(17.5f, eikthyr, 3);
        Assert.True(RagnarokPower.DamageMultiplier(eikthyr, fader) > 8f);
        Assert.Equal(1f, RagnarokPower.DamageMultiplier(fader, eikthyr));
        // Modded bosses: what was read from their attacks.
        Assert.Equal(42f, RagnarokPower.Damage("MyBoss", 42f));
        Assert.Equal(1f, RagnarokPower.DamageMultiplier(0f, fader));
    }

    // Places in the default Bosses setting: Eikthyr 0, Elder 1, Bonemass 2, Moder 3, Yagluth 4, Queen 5, Fader 6.
    [Theory]
    [InlineData(0, 0, 0, 0)] // Eikthyr after Eikthyr: as it is.
    [InlineData(0, 1, 1, 1)] // after the Elder: 1 star, as strong as the Elder.
    [InlineData(0, 2, 2, 2)] // after Bonemass: 2 stars, as strong as Bonemass.
    [InlineData(1, 2, 1, 2)] // the Elder after Bonemass: 1 star, as strong as Bonemass.
    [InlineData(2, 2, 0, 2)] // Bonemass after Bonemass: as it is.
    [InlineData(1, 3, 2, 3)] // the Elder after Moder: 2 stars, as strong as Moder.
    [InlineData(0, 6, 6, 6)] // after the Fader: 6 stars, as strong as the Fader.
    [InlineData(5, 6, 1, 6)] // the Queen after the Fader: 1 star, as strong as the Fader.
    [InlineData(4, 2, 0, 4)] // above the strongest: as it is.
    public void Raised_to_the_strongest_with_a_star_per_place(int place, int strongest, int stars, int raisedTo)
    {
        Assert.Equal(stars, RagnarokPower.Stars(place, strongest));
        Assert.Equal(raisedTo, RagnarokPower.RaisedTo(place, strongest));
    }
}
