using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Hoard-Bane Rite's fights and treasure, worked out without the game (see HoardBaneRush):
// - which of Hildir's minibosses comes for which chest, with its followers and the three elites guarding its shield;
// - where the elites' lairs and the miniboss stand on the edge of the arena;
// - the idols in the treasure (the battle and protection idols the Forge of Potential takes), by the strongest boss
//   everyone inside has slain: players who have all slain Moder get black metal ones, the Plains' tier, however far some
//   of them went.
internal static class HoardBaneLoot
{
    // Creatures guarding an elite in its lair: count of them, each with these stars. archers: only ones that carry a
    // bow (skeletons pick their weapon at random).
    internal readonly struct Guard(string prefab, int stars, int count, bool archers = false)
    {
        internal string Prefab { get; } = prefab;
        internal int Stars { get; } = stars;
        internal int Level => Stars + 1;
        internal int Count { get; } = count;
        internal bool Archers { get; } = archers;
    }

    // tint: how its colors shift from its level's own (hue, saturation, value; see HoardBaneArena.Decorate). color: what
    // its colors are multiplied by (red, green, blue), for creatures whose own colors a shift turns odd. size: how much
    // bigger than its level makes it, for creatures that would look small beside their pack.
    // holds: part of the name of an item it is sure to carry (creatures that pick their weapons at random: a troll's log).
    internal readonly struct Elite(string prefab, int stars, string name, Guard[] guards, Vector3? tint = null, Vector3? color = null,
        float size = DefaultSize, string? holds = null)
    {
        internal string Prefab { get; } = prefab;
        // As the game shows them: 0 for a plain creature. Its level is one more.
        internal int Stars { get; } = stars;
        internal int Level => Stars + 1;
        // Its own name, shown over it and in the messages.
        internal string Name { get; } = name;
        // Asleep with it in its lair, and woken with it. Only the elite has to fall.
        internal Guard[] Guards { get; } = guards;
        internal Vector3 Tint { get; } = tint ?? DefaultTint;
        internal Vector3 Color { get; } = color ?? Vector3.one;
        internal float Size { get; } = size;
        internal string? Holds { get; } = holds;
    }

    // How much bigger elites are than their level makes them.
    internal const float DefaultSize = 1.35f;

    // Darker and more saturated than its level's own colors.
    internal static readonly Vector3 DefaultTint = new(0f, 0.15f, -0.12f);

    internal sealed class Encounter(string chest, string[] bosses, string follower, Elite[] elites, bool lastRides = false)
    {
        // The chest that calls it.
        internal string Chest { get; } = chest;
        // The miniboss, or both brothers: each shielded until every elite falls.
        internal string[] Bosses { get; } = bosses;
        // The creature following the miniboss: FollowersAtStart of them rise with it, more as they fall (see HoardBaneRush).
        internal string Follower { get; } = follower;
        internal Elite[] Elites { get; } = elites;

        // The last miniboss rides the first, as in the game's own fight (Zil on Thungr): it doesn't rise with the
        // others but leaps down where the first falls.
        private bool LastRides { get; } = lastRides && bosses.Length > 1;

        internal bool Rides(int place) => LastRides && place == Bosses.Length - 1;

        // The miniboss riding the one at this place (its place), or -1.
        internal int RiderOf(int place) => LastRides && place == 0 ? Bosses.Length - 1 : -1;

        // The minibosses rising at the start ("Zil & Thungr" rides as one).
        internal string[] Leaders { get; } = lastRides && bosses.Length > 1 ? new List<string>(bosses).GetRange(0, bosses.Length - 1).ToArray() : bosses;
    }

    // The game's Hildir minibosses without their chest (the "_nochest" ones), so they drop none. Each elite has stars
    // (most beyond what the game spawns) and a pack of guards that wakes with it. In order of difficulty: the brass, silver
    // and bronze chests.
    internal static readonly Encounter[] Encounters =
    [
        // Brenna, from the Smouldering Tomb: skeletons.
        new("chest_hildir1", ["Skeleton_Hildir_nochest"], "Skeleton",
            [
                new("Troll", 3, "Brond, the Iron-Hide", [new("Greydwarf_Elite", 2, 1), new("Greydwarf_Shaman", 2, 1)], holds: "log"),
                new("Abomination", 1, "Marrow-Root", [new("Writhan", 2, 1), new("BlobElite", 0, 1)]),
                new("Wraith", 1, "Vörða, the Grave-Wail", [new("Ghost", 0, 4)])
            ]),
        // Geirrhafa, from the Howling Cavern: ulvs.
        new("chest_hildir2", ["Fenring_Cultist_Hildir_nochest"], "Ulv",
            [
                new("StoneGolem", 1, "Jötun-Crag", [new("Hatchling", 0, 3)]),
                new("Fenring", 2, "Sköll’s Kin, the Moon-Ripper", [new("Wolf", 2, 4)]),
                new("Fenring_Cultist", 1, "Moon-Chanter Hjördis", [new("Ulv", 1, 4)])
            ]),
        // Zil and Thungr, from the Sealed Tower: fulings. Zil rides Thungr (the game's "Zil & Thungr", casting Zil's
        // spells) and leaps down when Thungr falls.
        new("chest_hildir3", ["GoblinBruteBros_nochest", "GoblinShaman_Hildir_nochest"], "Goblin",
            [
                new("GoblinBrute", 3, "Gorgar Earth-Splitter", [new("Goblin", 2, 4)]),
                new("Unbjorn", 1, "Krag, the Black-Spear", [new("BlobTar", 0, 2)]),
                // A lox's olive-brown fur turns green under a hue and saturation shift: its colors multiplied toward a deep
                // demon red instead, a little darker.
                new("Lox", 1, "Grymm-Tusk", [new("Deathsquito", 0, 3)], new Vector3(0f, 0f, -0.1f), color: new Vector3(1f, 0.28f, 0.22f))
            ], lastRides: true)
    ];

    // ---- Followers ----

    // How many followers rise with the miniboss; then every ReinforceInterval seconds, one more while fewer than
    // ReinforceBelow stand.
    internal const int FollowersAtStart = 4;
    internal const float ReinforceInterval = 30f;
    internal const int ReinforceBelow = 2;

    private const int FarRings = 6;
    private const int FarPerRing = 24;

    // Where a follower comes in: the place in the arena (an offset from the obliterator, no further than reach) furthest
    // from every player (offsets too). With no player: the first spot tried.
    internal static Vector2 FarthestFrom(IReadOnlyList<Vector2> players, float reach)
    {
        Vector2 best = Vector2.zero;
        float bestDistance = -1f;
        for (int ring = 1; ring <= FarRings; ring++)
        {
            float distance = reach * ring / FarRings;
            for (int i = 0; i < FarPerRing; i++)
            {
                Vector2 spot = Around(i * 360f / FarPerRing, distance);
                float nearest = float.MaxValue;
                foreach (Vector2 player in players) nearest = Mathf.Min(nearest, Vector2.Distance(spot, player));
                if (nearest <= bestDistance) continue;
                bestDistance = nearest;
                best = spot;
            }
        }
        return best;
    }

    // The fight for the chest burnt (its prefab name). Another key item (added to the Key Item setting) gets the fight at
    // its place in that list; an unknown one, a random fight. random(n): 0 to n - 1.
    internal static int EncounterFor(string? chest, IReadOnlyList<string> keyItems, Func<int, int> random)
    {
        if (!string.IsNullOrEmpty(chest))
        {
            for (int i = 0; i < Encounters.Length; i++)
                if (string.Equals(Encounters[i].Chest, chest, StringComparison.OrdinalIgnoreCase)) return i;
            for (int i = 0; i < keyItems.Count; i++)
                if (string.Equals(keyItems[i], chest, StringComparison.OrdinalIgnoreCase)) return i % Encounters.Length;
        }
        return Mathf.Clamp(random(Encounters.Length), 0, Encounters.Length - 1);
    }

    // ---- Where they stand ----

    // How far inside the arena's edge an elite's pack stays (the lair itself may reach a little past it).
    private const float LairClearOfEdge = 2f;
    // How far inside the edge the miniboss rises.
    private const float BossClearOfEdge = 6f;
    // How far inside the edge followers come in.
    internal const float FollowerClearOfEdge = BossClearOfEdge;

    // The centers of the elites' lairs, as offsets from the obliterator: evenly around, as near the edge as their packs fit.
    // angle: where the first one is, in degrees.
    internal static Vector2[] LairPosts(float radius, float lairRadius, float angle, int count)
    {
        float distance = LairDistance(radius, lairRadius);
        Vector2[] posts = new Vector2[Math.Max(0, count)];
        for (int i = 0; i < posts.Length; i++) posts[i] = Around(angle + i * 360f / posts.Length, distance);
        return posts;
    }

    internal static float LairDistance(float radius, float lairRadius) =>
        Mathf.Max(0f, radius - lairRadius * PackSpread - LairClearOfEdge);

    // Where the miniboss rises: on the edge, halfway between the first two lairs.
    internal static Vector2 BossPost(float radius, float angle, int lairs) =>
        Around(angle + 180f / Math.Max(1, lairs), Mathf.Max(0f, radius - BossClearOfEdge));

    // How far from its elite a pack stands, as a share of its lair.
    internal const float PackSpread = 0.65f;

    // Where an elite's guards stand, as offsets from it (the lair's center): evenly around it, far enough apart that big
    // creatures don't stand on each other. angle: where the first one is, in degrees.
    internal static Vector2[] PackPosts(float lairRadius, float angle, int count)
    {
        Vector2[] posts = new Vector2[Math.Max(0, count)];
        for (int i = 0; i < posts.Length; i++) posts[i] = Around(angle + i * 360f / posts.Length, lairRadius * PackSpread);
        return posts;
    }

    private static Vector2 Around(float degrees, float distance)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians) * distance, Mathf.Sin(radians) * distance);
    }

    // ---- Treasure ----

    // The idol entry for no boss slain at all.
    internal const string NoBoss = "None";

    internal readonly struct Idol(string boss, RagnarokLoot.Treasure treasure)
    {
        internal string Boss { get; } = boss;
        internal RagnarokLoot.Treasure Treasure { get; } = treasure;
    }

    private static readonly char[] Separators = [',', ';'];

    // "Eikthyr: Upgrader1Weapon: 1-2, Dragon: Upgrader4Armor" -> [Eikthyr Upgrader1Weapon 1-2, Dragon Upgrader4Armor 1-1].
    // A boss can be listed more than once, for several idols. Entries that can't be read go to problems.
    internal static List<Idol> ParseIdols(string? text, List<string>? problems = null)
    {
        List<Idol> idols = [];
        if (string.IsNullOrEmpty(text)) return idols;
        foreach (string part in text!.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            string entry = part.Trim();
            if (entry.Length == 0) continue;
            int colon = entry.IndexOf(':');
            string boss = colon < 0 ? string.Empty : entry.Substring(0, colon).Trim();
            List<RagnarokLoot.Treasure> treasure = colon < 0 ? [] : RagnarokLoot.Parse(entry.Substring(colon + 1));
            if (boss.Length == 0 || treasure.Count != 1)
            {
                problems?.Add(entry);
                continue;
            }
            idols.Add(new Idol(boss, treasure[0]));
        }
        return idols;
    }

    // The idols for the strongest boss everyone inside has slain (its place among the bosses, weakest first; -1: none):
    // every entry listed for it. A boss not listed takes the entries of the nearest weaker one listed, and with none of
    // those, the None entries.
    internal static List<RagnarokLoot.Treasure> PickIdols(List<Idol> idols, IReadOnlyList<string> bosses, int strongest)
    {
        for (int place = Math.Min(strongest, bosses.Count - 1); place >= 0; place--)
        {
            List<RagnarokLoot.Treasure> found = For(idols, bosses[place]);
            if (found.Count > 0) return found;
        }
        return For(idols, NoBoss);
    }

    // The idols won, from every tier up to the strongest boss everyone inside has slain (its place, weakest first; -1:
    // none), in windows of two tiers from the top down: one idol from the top two, one from the two below, and so on to
    // the None tier. Each window picks one of its tiers, that tier one of its idols (battle or protection), and then how
    // many. Bonemass slain: one from Bonemass' or the Elder's, one from Eikthyr's or the None tier's.
    internal static List<(string item, int amount)> RollIdols(List<Idol> idols, IReadOnlyList<string> bosses, int strongest, Func<float> random)
    {
        List<(string item, int amount)> rolled = [];
        int top = Math.Max(-1, Math.Min(strongest, bosses.Count - 1));
        for (int high = top; high >= -1; high -= 2)
        {
            int tiers = high - 1 >= -1 ? 2 : 1;
            int place = high - Pick(tiers, random());
            List<RagnarokLoot.Treasure> tier = PickIdols(idols, bosses, place);
            if (tier.Count == 0) continue;
            RagnarokLoot.Treasure idol = tier[Pick(tier.Count, random())];
            int amount = Amount(idol, random());
            if (amount <= 0) continue;
            int at = rolled.FindIndex(r => r.item == idol.Item);
            if (at < 0) rolled.Add((idol.Item, amount));
            else rolled[at] = (idol.Item, rolled[at].amount + amount);
        }
        return rolled;
    }

    // One of count, evenly: roll from 0 to 1.
    private static int Pick(int count, float roll) => Math.Min(count - 1, Mathf.FloorToInt(Mathf.Clamp01(roll) * count));

    private static List<RagnarokLoot.Treasure> For(List<Idol> idols, string boss) =>
        idols.FindAll(idol => string.Equals(idol.Boss, boss, StringComparison.OrdinalIgnoreCase)).ConvertAll(idol => idol.Treasure);

    // How many: from Min to Max, roll from 0 to 1.
    internal static int Amount(RagnarokLoot.Treasure treasure, float roll) =>
        Math.Min(treasure.Max, treasure.Min + Mathf.FloorToInt(Mathf.Clamp01(roll) * (treasure.Max - treasure.Min + 1)));

    // ---- How strong the miniboss is ----

    // Never weaker than this: a miniboss that has to be fought for is stronger than usual.
    internal const float LeastHealth = 1.5f;
    internal const float LeastDamage = 1.2f;

    // Its health multiplier: as hard to kill as this share of the strongest boss slain (its resistances counted), split
    // between the brothers when two come. No boss slain: only the least.
    internal static float HealthMultiplier(float health, float taken, float bossHealth, float bossTaken, float share, int bosses)
    {
        if (bossHealth <= 0f || health <= 0f) return LeastHealth;
        float target = bossHealth * Math.Max(0f, share) / Math.Max(1, bosses);
        return Math.Max(LeastHealth, RagnarokPower.HealthMultiplier(health, taken, target, bossTaken));
    }

    // Its damage multiplier: as much per attack as this share of the strongest boss slain.
    internal static float DamageMultiplier(float damage, float bossDamage, float share)
    {
        if (bossDamage <= 0f || damage <= 0f) return LeastDamage;
        return Math.Max(LeastDamage, RagnarokPower.DamageMultiplier(damage, bossDamage * Math.Max(0f, share)));
    }
}
