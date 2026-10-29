using JoksterCube.TempestRite.Domain;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

public class CryptLootTests
{
    [Theory]
    // 1.5 times, rounded up; a roll above both chances adds nothing on top.
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(0, 0)]
    public void One_and_a_half_times_rounded_up(int rolled, int extra) =>
        Assert.Equal(extra, CryptLoot.Extra(rolled, 1.5f, 0.2f, 0.05f, 0.9f));

    [Theory]
    [InlineData(0.00f, 3)] // within the 5%: two more
    [InlineData(0.04f, 3)]
    [InlineData(0.05f, 2)] // the next 20%: one more
    [InlineData(0.24f, 2)]
    [InlineData(0.25f, 1)] // the rest: none
    [InlineData(0.99f, 1)]
    public void Chances_of_one_or_two_more(float roll, int extra) =>
        Assert.Equal(extra, CryptLoot.Extra(1, 1.5f, 0.2f, 0.05f, roll));

    [Fact]
    public void Multiplier_of_two_doubles() =>
        Assert.Equal(3, CryptLoot.Extra(3, 2f, 0f, 0f, 0.9f));

    [Fact]
    public void Multiplier_of_one_adds_only_the_chances() =>
        Assert.Equal(0, CryptLoot.Extra(5, 1f, 0f, 0f, 0f));
}
