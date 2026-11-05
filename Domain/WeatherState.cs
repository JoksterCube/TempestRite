using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The rites going on in this game: at most one per slot, each with its own time. Rites that work around an obliterator
// (the Domain rites, Crimson) also keep where it is. Blessings are kept apart, by each player (see Blessings).
// While a Crimson, Ragnarök or Hoard-Bane Rite goes on (they share the Incursion slot), no Domain rite works within its
// Blood Grounds: one performed at an obliterator inside it is cancelled as it starts, and one whose area only overlaps it keeps
// working everywhere outside the Blood Grounds, doing nothing inside until it ends.
//
// Time is counted with the game clock (Time.time), not world time: sleeping skips world time, and a rite performed
// for 30 minutes should last 30 minutes of play. Over the network only the time left is sent, so each game counts
// down on its own clock, and asks the server again now and then and after being paused (see WeatherSync).
internal static class WeatherState
{
    // One rite going on, as sent over the network and saved.
    internal readonly struct Entry(WeatherKind kind, float remaining, float duration, string calledBy, Vector3 position = default)
    {
        internal WeatherKind Kind { get; } = kind;
        internal float Remaining { get; } = remaining;
        internal float Duration { get; } = duration;
        internal string CalledBy { get; } = calledBy;
        // The obliterator it was performed at.
        internal Vector3 Position { get; } = position;
    }

    private sealed class Slot
    {
        internal WeatherKind Kind;
        internal float EndTime;
        internal float Duration;
        internal string CalledBy = string.Empty;
        internal Vector3 Position;
    }

    private static readonly Slot[] Slots = MakeSlots();

    private static Slot[] MakeSlots()
    {
        Slot[] slots = new Slot[WeatherKinds.Slots.Length];
        for (int i = 0; i < slots.Length; i++) slots[i] = new Slot();
        return slots;
    }

    private static Slot Of(EffectSlot slot) => Slots[(int)slot];

    internal static WeatherKind Active(EffectSlot slot)
    {
        Slot s = Of(slot);
        return s.Kind != WeatherKind.None && Time.time < s.EndTime ? s.Kind : WeatherKind.None;
    }

    // Blessings and what kinship rites leave are the local player's own (see Blessings, KinshipRites); the rest are
    // shared.
    internal static bool IsActive(WeatherKind kind) =>
        WeatherKinds.Personal(kind) ? Blessings.IsActive(kind)
        : WeatherKinds.ForEveryone(kind) ? KinshipRites.IsActive(kind)
        : WeatherKinds.HasSlot(kind) && Active(WeatherKinds.Slot(kind)) == kind;

    internal static float Remaining(WeatherKind kind) =>
        WeatherKinds.Personal(kind) ? Blessings.Remaining(kind)
        : WeatherKinds.ForEveryone(kind) ? KinshipRites.Remaining(kind)
        : IsActive(kind) ? Of(WeatherKinds.Slot(kind)).EndTime - Time.time : 0f;

    internal static float Duration(WeatherKind kind) =>
        WeatherKinds.Personal(kind) ? Blessings.Duration(kind)
        : WeatherKinds.ForEveryone(kind) ? KinshipRites.Duration(kind)
        : IsActive(kind) ? Of(WeatherKinds.Slot(kind)).Duration : 0f;

    internal static string CalledBy(WeatherKind kind) =>
        WeatherKinds.Personal(kind) ? Blessings.CalledBy(kind)
        : WeatherKinds.ForEveryone(kind) ? KinshipRites.CalledBy(kind)
        : IsActive(kind) ? Of(WeatherKinds.Slot(kind)).CalledBy : string.Empty;

    // Where a located rite was performed.
    internal static Vector3 Position(WeatherKind kind) =>
        IsActive(kind) && WeatherKinds.HasSlot(kind) ? Of(WeatherKinds.Slot(kind)).Position : Vector3.zero;

    // Whether a point is within a located rite's reach. Rites that aren't located reach everywhere. A Domain rite
    // doesn't reach into a Crimson, Ragnarök or Hoard-Bane Rite's Blood Grounds.
    internal static bool Reaches(WeatherKind kind, Vector3 point, float extra = 0f)
    {
        if (!IsActive(kind)) return false;
        if (!WeatherKinds.Located(kind)) return true;
        float radius = PluginConfig.Radius(kind) + extra;
        return Utils.DistanceXZ(point, Position(kind)) <= radius && !Suspended(kind, point);
    }

    // Whether a Domain rite does nothing at this point: it is within a Crimson, Ragnarök or Hoard-Bane Rite's Blood Grounds.
    internal static bool Suspended(WeatherKind kind, Vector3 point)
    {
        if (!IsDomain(kind)) return false;
        if (RaidPaused(kind)) return true;
        WeatherKind incursion = Active(EffectSlot.Incursion);
        return incursion != WeatherKind.None && Utils.DistanceXZ(point, Of(EffectSlot.Incursion).Position) <= PluginConfig.Radius(incursion);
    }

    // Whether a raid reaches into a Packward or Carrion Rite's area: the rite stops steering creatures and its time
    // stands still until the raid ends. Every game knows where the raid is, so each pauses on its own clock.
    internal static bool RaidPaused(WeatherKind kind)
    {
        if (kind is not (WeatherKind.Packward or WeatherKind.Carrion) || !IsActive(kind)) return false;
        return RaidOverlaps(Of(WeatherKinds.Slot(kind)));
    }

    private static bool RaidOverlaps(Slot s)
    {
        RandomEvent? raid = RandEventSystem.instance ? RandEventSystem.instance.GetCurrentRandomEvent() : null;
        return raid != null && Utils.DistanceXZ(raid.m_pos, s.Position) <= raid.m_eventRange + PluginConfig.Radius(s.Kind);
    }

    private static bool IsDomain(WeatherKind kind) => WeatherKinds.HasSlot(kind) && WeatherKinds.IsDomainSlot(WeatherKinds.Slot(kind));

    // Whether the local player is where a rite shows: everywhere, or near the obliterator for located rites.
    internal static bool ShowsHere(WeatherKind kind)
    {
        if (!WeatherKinds.Located(kind)) return true;
        Player? player = Player.m_localPlayer;
        return player && Utils.DistanceXZ(player!.transform.position, Position(kind)) <= PluginConfig.Radius(kind);
    }

    internal static Entry EntryFor(WeatherKind kind) => new(kind, Remaining(kind), Duration(kind), CalledBy(kind), Position(kind));

    // Everything going on now.
    internal static List<Entry> Entries()
    {
        List<Entry> entries = [];
        foreach (EffectSlot slot in WeatherKinds.Slots)
        {
            WeatherKind kind = Active(slot);
            if (kind != WeatherKind.None) entries.Add(EntryFor(kind));
        }
        return entries;
    }

    internal static void Set(Entry entry)
    {
        if (!WeatherKinds.HasSlot(entry.Kind) || entry.Remaining <= 0f) return;
        Slot s = Of(WeatherKinds.Slot(entry.Kind));
        s.Kind = entry.Kind;
        s.Duration = Mathf.Max(entry.Duration, entry.Remaining);
        s.CalledBy = entry.CalledBy;
        s.Position = entry.Position;
        s.EndTime = Time.time + entry.Remaining;
    }

    // Takes what the server says is going on: slots it doesn't mention are cleared.
    internal static void SetAll(List<Entry> entries)
    {
        // The Domain rites going on before: the server may have cancelled some for a Crimson, Ragnarök or Hoard-Bane Rite.
        List<(WeatherKind kind, Vector3 position)> domains = [];
        foreach (Slot s in Slots)
            if (s.Kind != WeatherKind.None && Time.time < s.EndTime && IsDomain(s.Kind)) domains.Add((s.Kind, s.Position));
        foreach (Slot s in Slots) Reset(s);
        foreach (Entry entry in entries) Set(entry);
        foreach ((WeatherKind kind, Vector3 position) in domains)
            if (!IsActive(kind) && InGrounds(position)) TellCancelled(kind, position);
    }

    // A rite the server has but this game doesn't, with less than this left, ran out here a moment before it did there:
    // not put back, so its end isn't told twice.
    private const float ResyncSlack = 5f;

    // On a player's game, the server's answer when asked again (see WeatherSync): its time left wins. This game's clock
    // stands still while it is paused (a player with debug mode can pause while the server goes on) and falls behind on
    // long stalls. A rite the server no longer has runs out here as if its time was up, with its ended message.
    internal static void Resync(List<Entry> entries)
    {
        foreach (Slot s in Slots)
            if (s.Kind != WeatherKind.None && !entries.Exists(e => e.Kind == s.Kind)) s.EndTime = Mathf.Min(s.EndTime, Time.time);
        foreach (Entry entry in entries)
        {
            if (!WeatherKinds.HasSlot(entry.Kind)) continue;
            Slot s = Of(WeatherKinds.Slot(entry.Kind));
            bool have = s.Kind == entry.Kind && Time.time < s.EndTime;
            if (have && entry.Remaining <= 0f) s.EndTime = Time.time;
            else if (have || entry.Remaining > ResyncSlack) Set(entry);
        }
    }

    // When a Crimson, Ragnarök or Hoard-Bane Rite starts: the Domain rites performed at an obliterator within its Blood Grounds
    // are cancelled. Ones whose obliterator is outside it go on, doing nothing only inside the Blood Grounds (see Suspended).
    private static void CancelDomains()
    {
        foreach (Slot s in Slots)
        {
            if (s.Kind == WeatherKind.None || !IsDomain(s.Kind) || !InGrounds(s.Position)) continue;
            WeatherKind kind = s.Kind;
            Vector3 position = s.Position;
            Reset(s);
            Plugin.ModLogger.LogInfo($"The {WeatherKinds.Name(kind)} is cancelled: its obliterator is within the {WeatherKinds.Name(Active(EffectSlot.Incursion))}'s Blood Grounds.");
            TellCancelled(kind, position);
        }
    }

    private static bool InGrounds(Vector3 point)
    {
        WeatherKind incursion = Active(EffectSlot.Incursion);
        return incursion != WeatherKind.None && Utils.DistanceXZ(point, Of(EffectSlot.Incursion).Position) <= PluginConfig.Radius(incursion);
    }

    // Told to players near the cancelled rite's obliterator.
    private static void TellCancelled(WeatherKind kind, Vector3 position)
    {
        Player? player = Player.m_localPlayer;
        if (!player || Utils.DistanceXZ(player!.transform.position, position) > PluginConfig.Radius(kind)) return;
        PlayerMessages.TopLeft(string.Format(Messages.DomainCancelled, WeatherKinds.Name(kind), WeatherKinds.Name(Active(EffectSlot.Incursion))));
    }

    internal static void Clear()
    {
        foreach (Slot s in Slots) Reset(s);
    }

    // Performs a rite in this game (for its player, or for a player through this server). The Equilibrium, Sun-Fall,
    // Nature's Rebound and Grounding Rites end the rites of their own group, the Sanctuary Rite the Crimson, Ragnarök and
    // Hoard-Bane Rites (and the server ends the raid and the bosses, see CrimsonRaids and RagnarokRush).
    internal static void Start(WeatherKind kind, string calledBy, Vector3 position)
    {
        // Any other Reckoning rite ends the Ley-Survey Rite, and the glimpse a refused arena gave.
        if (WeatherKinds.EndsSurvey(kind)) End(WeatherKind.LeySurvey);
        LeySurvey.Performed(kind);
        if (kind is WeatherKind.Reset or WeatherKind.SunFall or WeatherKind.NaturesRebound or WeatherKind.Grounding or WeatherKind.Sanctuary)
        {
            foreach (Slot s in Slots)
                if (s.Kind != WeatherKind.None && WeatherKinds.Ends(kind, s.Kind)) Reset(s);
            return;
        }
        WeatherConfig? config = PluginConfig.For(kind);
        if (config == null || !WeatherKinds.HasSlot(kind)) return;
        float seconds = WeatherKinds.Untimed(kind) ? WeatherKinds.UntimedSeconds : config.Seconds;
        Set(new Entry(kind, seconds, seconds, calledBy, position));
        if (WeatherKinds.IsIncursion(kind)) CancelDomains();
    }

    // Ends a rite going on at once, without its ended message: the server says why (see CrimsonRaids).
    internal static void End(WeatherKind kind)
    {
        if (!WeatherKinds.HasSlot(kind)) return;
        Slot s = Of(WeatherKinds.Slot(kind));
        if (s.Kind == kind) Reset(s);
    }

    // Every frame: says when a rite has run out (located ones only near their obliterator).
    internal static void Update()
    {
        foreach (EffectSlot slot in WeatherKinds.Slots)
        {
            Slot s = Of(slot);
            if (s.Kind == WeatherKind.None) continue;
            // Paused by a raid: the end moves on with the clock.
            if (slot == EffectSlot.Creatures && Time.time < s.EndTime && RaidOverlaps(s)) s.EndTime += Time.deltaTime;
            if (Time.time < s.EndTime) continue;
            bool shown = !WeatherKinds.Located(s.Kind) || NearPlayer(s);
            string message = EndedMessage(slot, s.Kind);
            Reset(s);
            if (shown && message.Length > 0) PlayerMessages.TopLeft(message);
        }
    }

    private static bool NearPlayer(Slot s)
    {
        Player? player = Player.m_localPlayer;
        return player && Utils.DistanceXZ(player!.transform.position, s.Position) <= PluginConfig.Radius(s.Kind);
    }

    private static string EndedMessage(EffectSlot slot, WeatherKind kind) => slot switch
    {
        EffectSlot.Daylight => Messages.EndedDaylight,
        EffectSlot.Wind => Messages.EndedGaleBreath,
        EffectSlot.Mistlands => Messages.EndedMistlands,
        EffectSlot.Ashlands => Messages.EndedAshlands,
        EffectSlot.Fermenting => Messages.EndedVatBoil,
        EffectSlot.Smelting => Messages.EndedPitchBellows,
        EffectSlot.Breeding => Messages.EndedKindred,
        EffectSlot.Spawners => Messages.EndedHarrowSpur,
        EffectSlot.Mending => Messages.EndedAmberMend,
        EffectSlot.Ocean => Messages.EndedAbyssalSurge,
        EffectSlot.Ward => Messages.EndedSovereignWard,
        EffectSlot.Carrying => Messages.EndedHearthHaul,
        EffectSlot.Clearing => Messages.EndedAetherClear,
        EffectSlot.Sprouting => Messages.EndedElderSprout,
        EffectSlot.Sentries => Messages.EndedSentrySight,
        EffectSlot.Sap => Messages.EndedTaprootSurge,
        EffectSlot.Anchoring => Messages.EndedMantleAnchor,
        EffectSlot.Lures => Messages.EndedPrimalHearth,
        EffectSlot.Resting => Messages.EndedHearthKindle,
        EffectSlot.Survey => Messages.EndedLeySurvey,
        // The Crimson, Ragnarök and Hoard-Bane Rites' end is told by the server, with how it went (see CrimsonArena,
        // RagnarokArena, HoardBaneArena).
        EffectSlot.Incursion => string.Empty,
        EffectSlot.Verdant => Messages.EndedVerdant,
        EffectSlot.Creatures => kind == WeatherKind.Carrion ? Messages.EndedCarrion : Messages.EndedPackward,
        _ => Messages.EndedWeather
    };

    // Adds the rite's line to what the burn tells the local player (see RiteNews), if its news reaches them.
    // position: the obliterator it burnt in; calledBy: who performed it.
    internal static void Announce(WeatherKind kind, Vector3 position, bool byLocalPlayer, string calledBy)
    {
        if (WeatherKinds.Lasts(kind) && !IsActive(kind)) return;
        // The Crimson, Ragnarök and Hoard-Bane Rites are heard of across the world, whatever Show Messages is set to:
        // those within the Blood Grounds are told to get ready, the rest that something stirs far off.
        if (WeatherKinds.IsIncursion(kind))
        {
            string line = byLocalPlayer || ShowsHere(kind) ? WeatherKinds.Performed(kind) : Messages.IncursionBegunFar;
            RiteNews.Add(RiteNews.Told(kind, byLocalPlayer, calledBy, line), RiteNews.Weight.Incursion, always: true);
            return;
        }
        if (!byLocalPlayer && !Hears(kind, position)) return;
        RiteNews.Add(RiteNews.Told(kind, byLocalPlayer, calledBy, WeatherKinds.Performed(kind)));
    }

    // Whether a rite someone else performed is news to the local player: Domain rites only within the domain, the
    // Grounding Rite only near the obliterator it burnt in, the Unmaking Rite (what it gave back) only to who burnt
    // it, the rest everywhere.
    private static bool Hears(WeatherKind kind, Vector3 position)
    {
        if (kind == WeatherKind.Unmaking) return false;
        if (kind == WeatherKind.Grounding)
        {
            Player? player = Player.m_localPlayer;
            return player && Utils.DistanceXZ(player!.transform.position, position) <= PluginConfig.Radius(kind);
        }
        return ShowsHere(kind);
    }

    private static void Reset(Slot s)
    {
        s.Kind = WeatherKind.None;
        s.EndTime = 0f;
        s.Duration = 0f;
        s.CalledBy = string.Empty;
        s.Position = Vector3.zero;
    }
}
