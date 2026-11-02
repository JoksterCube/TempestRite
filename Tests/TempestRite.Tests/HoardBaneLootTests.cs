using System.Collections.Generic;
using JoksterCube.TempestRite.Domain;
using UnityEngine;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

public class HoardBaneLootTests
{
    private static readonly string[] Bosses = ["Eikthyr", "gd_king", "Bonemass", "Dragon", "GoblinKing", "SeekerQueen", "Fader"];

    [Fact]
    public void Each_chest_calls_its_miniboss()
    {
        string[] keys = ["chest_hildir1", "chest_hildir2", "chest_hildir3"];
        Assert.Equal(0, HoardBaneLoot.EncounterFor("chest_hildir1", keys, _ => 2));
        Assert.Equal(1, HoardBaneLoot.EncounterFor("chest_hildir2", keys, _ => 2));
        Assert.Equal(2, HoardBaneLoot.EncounterFor("chest_hildir3", keys, _ => 0));
        Assert.Equal("Skeleton_Hildir_nochest", HoardBaneLoot.Encounters[0].Bosses[0]);
        Assert.Equal(2, HoardBaneLoot.Encounters[2].Bosses.Length);
        // Another key item: its place in the list; unknown: random.
        Assert.Equal(0, HoardBaneLoot.EncounterFor("MyChest", ["chest_hildir1", "chest_hildir2", "chest_hildir3", "MyChest"], _ => 2));
        Assert.Equal(2, HoardBaneLoot.EncounterFor(null, keys, _ => 2));
    }

    [Fact]
    public void Each_fight_has_three_elites_with_stars_and_one_kind_of_follower()
    {
        foreach (HoardBaneLoot.Encounter fight in HoardBaneLoot.Encounters) Assert.Equal(3, fight.Elites.Length);
        Assert.Equal(["Skeleton", "Ulv", "Goblin"], System.Array.ConvertAll(HoardBaneLoot.Encounters, f => f.Follower));
        // Four at the start; one more every 30 seconds while fewer than two stand.
        Assert.Equal(4, HoardBaneLoot.FollowersAtStart);
        Assert.Equal(30f, HoardBaneLoot.ReinforceInterval);
        Assert.Equal(2, HoardBaneLoot.ReinforceBelow);
        HoardBaneLoot.Elite troll = HoardBaneLoot.Encounters[0].Elites[0];
        Assert.Equal("Troll", troll.Prefab);
        Assert.Equal(3, troll.Stars);
        Assert.Equal(4, troll.Level);
        Assert.Equal("Brond, the Iron-Hide", troll.Name);
        Assert.Equal("log", troll.Holds);
        Assert.Null(HoardBaneLoot.Encounters[0].Elites[1].Holds);
        // Every elite has its own name.
        HashSet<string> names = [];
        foreach (HoardBaneLoot.Encounter fight in HoardBaneLoot.Encounters)
            foreach (HoardBaneLoot.Elite elite in fight.Elites) Assert.True(names.Add(elite.Name));
        Assert.Equal(9, names.Count);
    }

    [Fact]
    public void Each_elite_has_its_pack()
    {
        foreach (HoardBaneLoot.Encounter fight in HoardBaneLoot.Encounters)
            foreach (HoardBaneLoot.Elite elite in fight.Elites) Assert.NotEmpty(elite.Guards);
        // Brenna's: a 3-star troll with a 2-star brute and a 2-star shaman, a 1-star abomination with a 2-star writhan
        // and an oozer, a 1-star wraith with four ghosts.
        HoardBaneLoot.Elite[] brenna = HoardBaneLoot.Encounters[0].Elites;
        Assert.Equal([3, 1, 1], System.Array.ConvertAll(brenna, e => e.Stars));
        Assert.Equal(("Greydwarf_Elite", 3, 1), (brenna[0].Guards[0].Prefab, brenna[0].Guards[0].Level, brenna[0].Guards[0].Count));
        Assert.Equal(("Greydwarf_Shaman", 3, 1), (brenna[0].Guards[1].Prefab, brenna[0].Guards[1].Level, brenna[0].Guards[1].Count));
        Assert.Equal("Abomination", brenna[1].Prefab);
        Assert.Equal(("Writhan", 3, 1), (brenna[1].Guards[0].Prefab, brenna[1].Guards[0].Level, brenna[1].Guards[0].Count));
        Assert.Equal(("BlobElite", 1), (brenna[1].Guards[1].Prefab, brenna[1].Guards[1].Count));
        Assert.Equal("Wraith", brenna[2].Prefab);
        Assert.Equal(("Ghost", 4), (brenna[2].Guards[0].Prefab, brenna[2].Guards[0].Count));
        // Geirrhafa's: a 1-star golem with three drakes, a 2-star fenring with four 2-star wolves, a 1-star cultist with
        // four 1-star ulvs.
        HoardBaneLoot.Elite[] geirrhafa = HoardBaneLoot.Encounters[1].Elites;
        Assert.Equal([1, 2, 1], System.Array.ConvertAll(geirrhafa, e => e.Stars));
        Assert.Equal(("Hatchling", 1, 3), (geirrhafa[0].Guards[0].Prefab, geirrhafa[0].Guards[0].Level, geirrhafa[0].Guards[0].Count));
        Assert.Equal("Fenring_Cultist", geirrhafa[2].Prefab);
        Assert.Equal(("Ulv", 2, 4), (geirrhafa[2].Guards[0].Prefab, geirrhafa[2].Guards[0].Level, geirrhafa[2].Guards[0].Count));
        // Every elite the same size now.
        foreach (HoardBaneLoot.Encounter fight in HoardBaneLoot.Encounters)
            foreach (HoardBaneLoot.Elite elite in fight.Elites) Assert.Equal(HoardBaneLoot.DefaultSize, elite.Size);
        // Zil and Thungr's: a 3-star berserker, a 1-star vile with two growths and a 1-star lox with three deathsquitos.
        HoardBaneLoot.Elite[] zil = HoardBaneLoot.Encounters[2].Elites;
        Assert.Equal([3, 1, 1], System.Array.ConvertAll(zil, e => e.Stars));
        Assert.Equal(("BlobTar", 2), (zil[1].Guards[0].Prefab, zil[1].Guards[0].Count));
        Assert.Equal(("Deathsquito", 3), (zil[2].Guards[0].Prefab, zil[2].Guards[0].Count));
        // Only the lox has its colors multiplied: toward red.
        Vector3 lox = zil[2].Color;
        Assert.True(lox.x > lox.y && lox.x > lox.z);
        Assert.Equal(Vector3.one, zil[0].Color);
    }

    [Fact]
    public void A_follower_comes_in_furthest_from_the_players()
    {
        // One player on the east edge: the follower comes in on the west edge.
        Vector2 far = HoardBaneLoot.FarthestFrom([new Vector2(40f, 0f)], 44f);
        Assert.Equal(new Vector2(-44f, 0f).x, far.x, 2);
        Assert.Equal(0f, far.y, 2);
        // Never beyond reach.
        Assert.True(far.magnitude <= 44f + 0.01f);
        // Two players east and west: north or south.
        Vector2 between = HoardBaneLoot.FarthestFrom([new Vector2(40f, 0f), new Vector2(-40f, 0f)], 44f);
        Assert.Equal(0f, between.x, 2);
        Assert.Equal(44f, Mathf.Abs(between.y), 2);
        // A player in the middle: on the edge.
        Assert.Equal(44f, HoardBaneLoot.FarthestFrom([Vector2.zero], 44f).magnitude, 2);
    }

    [Fact]
    public void A_pack_stands_evenly_around_its_elite()
    {
        Vector2[] posts = HoardBaneLoot.PackPosts(10f, 0f, 4);
        Assert.Equal(4, posts.Length);
        foreach (Vector2 post in posts) Assert.Equal(10f * HoardBaneLoot.PackSpread, post.magnitude, 3);
        // Never on top of each other: the nearest two are a side of the square apart.
        for (int i = 0; i < posts.Length; i++)
            for (int j = i + 1; j < posts.Length; j++)
                Assert.True(Vector2.Distance(posts[i], posts[j]) > 9f);
        // A lone guard stands off from its elite, not on it.
        Assert.Equal(6.5f, HoardBaneLoot.PackPosts(10f, 90f, 1)[0].magnitude, 3);
        Assert.Empty(HoardBaneLoot.PackPosts(10f, 0f, 0));
    }

    [Fact]
    public void Bubbles_sit_evenly_on_the_edge_and_the_miniboss_between_them()
    {
        Vector2[] posts = HoardBaneLoot.BubblePosts(50f, 10f, 0f, 3);
        Assert.Equal(3, posts.Length);
        foreach (Vector2 post in posts)
        {
            Assert.Equal(38f, post.magnitude, 3);
            // Wholly inside the arena.
            Assert.True(post.magnitude + 10f < 50f);
        }
        Assert.Equal(38f * Mathf.Sqrt(3f), Vector2.Distance(posts[0], posts[1]), 2);
        Vector2 boss = HoardBaneLoot.BossPost(50f, 0f, 3);
        Assert.Equal(44f, boss.magnitude, 3);
        Assert.Equal(Vector2.Distance(boss, posts[0]), Vector2.Distance(boss, posts[1]), 2);
    }

    [Fact]
    public void Idols_follow_the_strongest_boss_everyone_has_slain()
    {
        List<string> problems = [];
        List<HoardBaneLoot.Idol> idols = HoardBaneLoot.ParseIdols(
            "None: Upgrader0Weapon: 1-2, None: Upgrader0Armor: 1-2, Eikthyr: Upgrader1Weapon, Dragon: Upgrader4Weapon: 1-2, " +
            "Dragon: Upgrader4Armor: 1-2, Bad, Fader: Bell: many", problems);
        Assert.Equal(5, idols.Count);
        Assert.Equal(["Bad", "Fader: Bell: many"], problems);
        static List<string> Items(List<RagnarokLoot.Treasure> picked) => picked.ConvertAll(t => t.Item);
        Assert.Equal(["Upgrader0Weapon", "Upgrader0Armor"], Items(HoardBaneLoot.PickIdols(idols, Bosses, -1)));
        Assert.Equal(["Upgrader1Weapon"], Items(HoardBaneLoot.PickIdols(idols, Bosses, 0)));
        // The Elder and Bonemass aren't listed: Eikthyr's.
        Assert.Equal(["Upgrader1Weapon"], Items(HoardBaneLoot.PickIdols(idols, Bosses, 2)));
        // Everyone slew Moder (some went further): black metal, the Plains' idols.
        Assert.Equal(["Upgrader4Weapon", "Upgrader4Armor"], Items(HoardBaneLoot.PickIdols(idols, Bosses, 3)));
        Assert.Equal(["Upgrader4Weapon", "Upgrader4Armor"], Items(HoardBaneLoot.PickIdols(idols, Bosses, 6)));
        Assert.Empty(HoardBaneLoot.PickIdols(HoardBaneLoot.ParseIdols("Dragon: Upgrader4Weapon"), Bosses, 1));
    }

    [Fact]
    public void Amounts_stay_within_min_and_max()
    {
        RagnarokLoot.Treasure coins = new("Coins", 500, 1500);
        Assert.Equal(500, HoardBaneLoot.Amount(coins, 0f));
        Assert.Equal(1500, HoardBaneLoot.Amount(coins, 1f));
        Assert.InRange(HoardBaneLoot.Amount(coins, 0.5f), 990, 1010);
    }

    [Fact]
    public void Miniboss_is_stronger_than_usual_but_weaker_than_the_boss()
    {
        // No boss slain: only the least.
        Assert.Equal(HoardBaneLoot.LeastHealth, HoardBaneLoot.HealthMultiplier(1000f, 1f, 0f, 1f, 0.4f, 1));
        Assert.Equal(HoardBaneLoot.LeastDamage, HoardBaneLoot.DamageMultiplier(50f, 0f, 0.6f));
        // A weak boss: still at least the least.
        Assert.Equal(HoardBaneLoot.LeastHealth, HoardBaneLoot.HealthMultiplier(1000f, 1f, 500f, 1f, 0.4f, 1));
        // The Fader's 15000 health at 0.4 is 6000: 6 times a 1000 health miniboss, half each for the brothers.
        Assert.Equal(6f, HoardBaneLoot.HealthMultiplier(1000f, 1f, 15000f, 1f, 0.4f, 1), 3);
        Assert.Equal(3f, HoardBaneLoot.HealthMultiplier(1000f, 1f, 15000f, 1f, 0.4f, 2), 3);
        Assert.Equal(2f, HoardBaneLoot.DamageMultiplier(60f, 200f, 0.6f), 3);
    }
}
