using JoksterCube.TempestRite.Domain;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

// The Sentry-Sight Rite's choice of target: the most pressing creature, not just the closest.
public class SentryTargetTests
{
    private const float Range = 30f;
    private const float Bolt = 60f;

    private static float Score(float maxHealth, float health, float hit, float distance, SentryTargets.Mood mood = SentryTargets.Mood.Fighting,
        bool current = false) =>
        SentryTargets.Score(maxHealth, health, hit, distance, Range, mood, Bolt, current);

    [Fact]
    public void A_fighting_troll_beats_a_closer_wandering_greyling() =>
        Assert.True(Score(600, 600, 70, 25) > Score(40, 40, 6, 5, SentryTargets.Mood.Idle));

    [Fact]
    public void A_creature_hitting_a_building_comes_before_an_equal_one_fighting() =>
        Assert.True(Score(200, 200, 30, 20, SentryTargets.Mood.Raiding) > Score(200, 200, 30, 15));

    [Fact]
    public void A_deathsquito_beats_a_tougher_but_weaker_hitting_creature() =>
        Assert.True(Score(10, 10, 90, 20) > Score(300, 300, 20, 20));

    [Fact]
    public void Closer_wins_between_equals() =>
        Assert.True(Score(200, 200, 30, 5) > Score(200, 200, 30, 25));

    [Fact]
    public void A_nearly_dead_creature_one_bolt_finishes_comes_first_between_equals() =>
        Assert.True(Score(200, 50, 30, 15) > Score(200, 200, 30, 15));

    [Fact]
    public void The_current_target_is_kept_over_an_equal() =>
        Assert.True(Score(200, 200, 30, 15, current: true) > Score(200, 200, 30, 15));

    [Fact]
    public void A_bolt_flight_comes_down_as_far_as_gravity_says()
    {
        // 50 m/s flat, 10 m/s² gravity, no drag: after 30 m (0.6 s) it has dropped about 0.5 * 10 * 0.36 = 1.8 m.
        Assert.True(SentryTargets.Fly(UnityEngine.Vector3.zero, new UnityEngine.Vector3(50f, 0f, 0f), 10f, 0f, new UnityEngine.Vector3(30f, 0f, 0f), 0.02f,
            out float height, out float seconds));
        Assert.InRange(seconds, 0.58f, 0.62f);
        Assert.InRange(height, -1.95f, -1.65f);
    }

    [Fact]
    public void Drag_slows_the_bolt() =>
        Assert.True(SentryTargets.Fly(UnityEngine.Vector3.zero, new UnityEngine.Vector3(50f, 0f, 0f), 0f, 0.01f, new UnityEngine.Vector3(30f, 0f, 0f), 0.02f,
            out _, out float seconds) && seconds > 0.65f);

    [Fact]
    public void A_close_weak_foe_comes_before_a_far_somewhat_stronger_one() =>
        Assert.True(Score(40, 40, 6, 5) > Score(80, 80, 15, 28));

    [Fact]
    public void A_creature_standing_still_is_a_sure_hit() =>
        Assert.Equal(1f, SentryTargets.HitChance(0.5f, 0f, 40f, 60f));

    [Fact]
    public void Small_fast_far_creatures_are_harder_to_hit_than_big_slow_close_ones()
    {
        float boar = SentryTargets.HitChance(0.4f, 7f, 40f, 60f);
        Assert.True(boar < SentryTargets.HitChance(1.2f, 3f, 40f, 60f)); // a troll walking
        Assert.True(boar < SentryTargets.HitChance(0.4f, 7f, 10f, 60f)); // the same boar close by
        Assert.InRange(boar, 0.25f, 0.6f);
    }

    [Fact]
    public void Speed_is_measured_from_how_far_it_went()
    {
        UnityEngine.Vector3 speed = SentryTargets.Measured(new UnityEngine.Vector3(0f, 0f, 0f), new UnityEngine.Vector3(1f, 0.5f, 2f), 0.25f, walking: false);
        Assert.Equal((4f, 2f, 8f), (speed.x, speed.y, speed.z));
    }

    [Fact]
    public void Walking_creatures_only_count_speed_along_the_ground() =>
        Assert.Equal(0f, SentryTargets.Measured(new UnityEngine.Vector3(0f, 0f, 0f), new UnityEngine.Vector3(1f, 0.5f, 2f), 0.25f, walking: true).y);

    [Fact]
    public void The_current_target_is_dropped_for_a_much_stronger_one() =>
        Assert.True(Score(900, 900, 80, 15) > Score(40, 40, 6, 15, current: true));
}
