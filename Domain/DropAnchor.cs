using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The Pitch-Seal Rite: items lying on the ground within its area never despawn. The game that owns an item checks
// now and then whether it has lain there for an hour; under the rite that check is skipped and the item's spawn time
// moved up to now (at most once a minute, so its ZDO isn't sent over and over), so its hour starts over as the rite
// ends. Items are handed to players with the mod (see WeatherOwnership). Items held fast now and then give off little
// blobs of pitch in place of the yellow sparks dropped items give off (see Swap).
internal static class DropAnchor
{
    internal static readonly SpedWork Work = new AnchorWork();
    // An item's spawn time is moved up once it is this old.
    private const double RefreshAfter = 60.0;

    internal static bool Reaches(Component item) => item && WeatherState.Reaches(WeatherKind.MantleAnchor, item.transform.position);

    // Whether the game's despawn check should be skipped for this item now (on the item's owner).
    internal static bool Holds(ItemDrop item)
    {
        if (!Reaches(item) || !item.m_nview || !item.m_nview.IsValid()) return false;
        if (item.m_nview.IsOwner())
        {
            ZDO zdo = item.m_nview.GetZDO();
            DateTime now = ZNet.instance.GetTime();
            DateTime spawned = new(zdo.GetLong(ZDOVars.s_spawnTime, now.Ticks));
            if ((now - spawned).TotalSeconds > RefreshAfter) zdo.Set(ZDOVars.s_spawnTime, now.Ticks);
        }
        return true;
    }

    // Items held fast near the player give off little blobs of pitch in place of the yellow sparks every dropped item gives off
    // (the game's particle system drawn with its "item_particle" material, on the item itself or on its fx_ItemSparkles;
    // about one spark a second, two at most) is drawn as the rite's blobs of pitch instead, and made heavy: smaller,
    // one at a time every few seconds, rising slowly (about a meter, above the item), tilted a little but not spinning. Back to sparks once the rite ends
    // or no longer reaches them. Nothing else on the item changes. RiteSpeedup tells which every second: Keep, then Sweep.
    private const string Feature = "Pitch-Seal sparks";
    private const string SparkMaterial = "item_particle";
    private static readonly ParticleSystem.MinMaxCurve SealSize = new(0.18f, 0.26f);
    private static readonly ParticleSystem.MinMaxCurve SealLifetime = new(3f, 4f);
    private static readonly ParticleSystem.MinMaxCurve SealTilt = new(-0.3f, 0.3f);
    // Blobs a second from each item, and how fast they rise (meters a second; sparks rise 1).
    private const float SealRate = 0.3f;
    private const float SealRise = 0.3f;

    private sealed class Swapped
    {
        internal ParticleSystem Sparks = null!;
        internal ParticleSystemRenderer Renderer = null!;
        internal Material Material = null!;
        internal ParticleSystem.MinMaxGradient Color;
        internal ParticleSystem.MinMaxCurve Size;
        internal ParticleSystem.MinMaxCurve Lifetime;
        internal ParticleSystem.MinMaxCurve Rotation;
        internal int MaxParticles;
        internal ParticleSystem.MinMaxCurve Rate;
        internal bool Spins;
        internal ParticleSystem.MinMaxCurve Rise;
    }

    private static readonly Dictionary<ZNetView, List<Swapped>> Showing = [];
    private static readonly HashSet<ZNetView> Kept = [];
    private static readonly List<ZNetView> Gone = [];

    internal static void Keep(ZNetView view) => Kept.Add(view);

    internal static void ClearSession()
    {
        foreach (List<Swapped> swapped in Showing.Values)
            try { Restore(swapped); }
            catch (Exception) { /* leaving the world: nothing to keep right */ }
        Showing.Clear();
        Kept.Clear();
    }

    internal static void Sweep()
    {
        if (PatchGuard.IsDisabled(Feature))
        {
            Kept.Clear();
            return;
        }
        try
        {
            if (!Work.ShowMotes) Kept.Clear();
            Gone.Clear();
            foreach (ZNetView view in Showing.Keys)
                if (!view || !Kept.Contains(view)) Gone.Add(view!);
            foreach (ZNetView view in Gone)
            {
                Restore(Showing[view]);
                Showing.Remove(view);
            }
            foreach (ZNetView view in Kept)
                if (view && !Showing.ContainsKey(view)) Showing[view] = Swap(view);
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            ClearSession();
        }
        finally { Kept.Clear(); }
    }

    // The item's own sparks drawn as blobs of pitch; empty when it has none (or the blob can't be drawn).
    private static List<Swapped> Swap(ZNetView view)
    {
        List<Swapped> swapped = [];
        if (DomainVisuals.TarMaterial() is not { } seal || !seal) return swapped;
        // The blob is painted in its own colors: left untinted.
        ParticleSystem.MinMaxGradient sealColor = new(new Color(0.9f, 0.9f, 0.9f), UnityEngine.Color.white);
        foreach (ParticleSystemRenderer renderer in view.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            if (!renderer || !IsSparks(renderer.sharedMaterial) || renderer.GetComponent<ParticleSystem>() is not { } sparks || !sparks) continue;
            ParticleSystem.MainModule main = sparks.main;
            ParticleSystem.EmissionModule emission = sparks.emission;
            ParticleSystem.RotationOverLifetimeModule spin = sparks.rotationOverLifetime;
            ParticleSystem.VelocityOverLifetimeModule velocity = sparks.velocityOverLifetime;
            swapped.Add(new Swapped
            {
                Sparks = sparks, Renderer = renderer, Material = renderer.sharedMaterial, Color = main.startColor, Size = main.startSize,
                Lifetime = main.startLifetime, Rotation = main.startRotation, MaxParticles = main.maxParticles, Rate = emission.rateOverTime,
                Spins = spin.enabled, Rise = velocity.y
            });
            main.startColor = sealColor;
            main.startSize = SealSize;
            main.startLifetime = SealLifetime;
            main.startRotation = SealTilt;
            main.maxParticles = 1;
            emission.rateOverTime = SealRate;
            spin.enabled = false;
            // Only when the sparks' rise is a plain number (as in the game's): the three directions must be alike.
            if (velocity.enabled && velocity.y.mode == ParticleSystemCurveMode.Constant) velocity.y = SealRise;
            renderer.sharedMaterial = seal;
            sparks.Clear(false);
        }
        return swapped;
    }

    // The game's item sparkle material (instances get " (Instance)" added).
    private static bool IsSparks(Material? material) => material && material!.name.StartsWith(SparkMaterial, StringComparison.Ordinal);

    private static void Restore(List<Swapped> swapped)
    {
        foreach (Swapped one in swapped)
        {
            if (!one.Sparks) continue;
            ParticleSystem.MainModule main = one.Sparks.main;
            main.startColor = one.Color;
            main.startSize = one.Size;
            main.startLifetime = one.Lifetime;
            main.startRotation = one.Rotation;
            main.maxParticles = one.MaxParticles;
            ParticleSystem.EmissionModule emission = one.Sparks.emission;
            emission.rateOverTime = one.Rate;
            ParticleSystem.RotationOverLifetimeModule spin = one.Sparks.rotationOverLifetime;
            spin.enabled = one.Spins;
            ParticleSystem.VelocityOverLifetimeModule velocity = one.Sparks.velocityOverLifetime;
            if (velocity.enabled && velocity.y.mode == ParticleSystemCurveMode.Constant) velocity.y = one.Rise;
            if (one.Renderer) one.Renderer.sharedMaterial = one.Material;
            one.Sparks.Clear(false);
        }
    }

    // The item's hover line under the rite: when it despawns if nobody takes it (the rest of the rite, then what is left
    // of its usual hour, which starts over as the rite ends), or that it never does where it lies (a base, tar).
    internal static string DespawnLine(ItemDrop item)
    {
        if (item.IsInsideBase()) return Messages.AnchorStaysBase;
        if (item.InTar()) return Messages.AnchorStaysTar;
        float rite = Math.Max(0f, WeatherState.Remaining(WeatherKind.MantleAnchor));
        float hour = DespawnSeconds;
        if (item.m_nview && item.m_nview.IsValid() && ZNet.instance)
        {
            DateTime now = ZNet.instance.GetTime();
            long spawned = item.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, now.Ticks);
            hour = Mathf.Clamp(DespawnSeconds - (float)(now - new DateTime(spawned)).TotalSeconds, 0f, DespawnSeconds);
        }
        return string.Format(Messages.AnchorDespawnIn, TimeText.Duration(rite + hour));
    }

    // The game's despawn time for items left on the ground (ItemDrop.c_AutoDestroyTimeout).
    private const float DespawnSeconds = 3600f;

    private sealed class AnchorWork() : SpedWork(WeatherKind.MantleAnchor)
    {
        internal override float Speed => 1f;
        internal override bool ShowMotes => PluginConfig.ShowMantleAnchorMotes.IsOn();
        // Items that can despawn (not ones built into something).
        internal override float? Takes(GameObject prefab) => prefab.GetComponent<ItemDrop>() is { } item && item && item.m_autoDestroy ? 0.5f : null;
        internal override bool Busy(ZDO zdo, ZNetView view) => true;
        internal override void Hurry(ZDO zdo, ZNetView view, double seconds) { }
        // No puffs: the items' own sparks turn into blobs of pitch instead (see Swap).
        internal override bool Glows(ZDO zdo, ZNetView view, bool busy) => false;
    }
}
