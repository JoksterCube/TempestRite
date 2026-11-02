using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The game's own status console commands work on the mod's statuses:
// - addstatus SE_TempestRite_<blessing> gives the local player that blessing for its full time, as if received at an
//   obliterator (again: back to its full time). SE_TempestRite_SoulTether pulls their tombstones to where they stand.
//   Only blessings are listed: everything else is the world's or comes from someone else.
// - clearstatus (and anything else that clears all statuses, or removes one of the mod's) ends the local player's
//   blessings, kinship rite waits and an open rally call. Rites everyone shares (weather, Domain, the arena rites...)
//   are the world's, not the player's: their statuses come back the next moment, so nobody leaves a fight this way.
internal static class StatusCommands
{
    private const string Feature = "Status commands";
    private const string Prefix = "SE_TempestRite_";
    internal const string RallyCallName = Prefix + "RallyCall";
    private static readonly int RallyCallHash = RallyCallName.GetStableHashCode();

    // The status of each rite, by its hash; the blessings among them can be added.
    private static readonly Dictionary<int, WeatherKind> Kinds = [];
    private static StatusEffect? _soulTether;
    // Set while the mod itself takes off one of its statuses (a blessing ended, statuses hidden...).
    private static bool _ours;

    internal static string Name(WeatherKind kind) => Prefix + kind;

    private static bool TryKind(int hash, out WeatherKind kind)
    {
        if (Kinds.Count == 0)
            foreach (WeatherKind each in WeatherKinds.All)
                Kinds[Name(each).GetStableHashCode()] = each;
        return Kinds.TryGetValue(hash, out kind);
    }

    // Whether addstatus can give it: a blessing.
    private static bool Addable(WeatherKind kind) => WeatherKinds.Personal(kind);

    // Takes one of the mod's statuses off without that counting as clearing it.
    internal static void RemoveQuietly(SEMan seMan, int hash)
    {
        _ours = true;
        try { seMan.RemoveStatusEffect(hash, true); }
        finally { _ours = false; }
    }

    // Lists the blessings' statuses with the game's, so addstatus finds them (and offers them on Tab).
    internal static void Register(ObjectDB db)
    {
        if (PatchGuard.IsDisabled(Feature) || !db) return;
        try
        {
            foreach (WeatherKind kind in WeatherKinds.All)
            {
                if (!Addable(kind)) continue;
                StatusEffect effect = WeatherKinds.Lasts(kind) ? WeatherStatus.Template(kind) : SoulTetherTemplate();
                int hash = effect.NameHash();
                if (!db.m_StatusEffects.Exists(e => e && e.NameHash() == hash)) db.m_StatusEffects.Add(effect);
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static StatusEffect SoulTetherTemplate()
    {
        if (_soulTether) return _soulTether!;
        _soulTether = ScriptableObject.CreateInstance<StatusEffect>();
        _soulTether.name = Name(WeatherKind.SoulTether);
        _soulTether.m_name = WeatherKinds.Name(WeatherKind.SoulTether);
        _soulTether.hideFlags = HideFlags.HideAndDontSave;
        return _soulTether;
    }

    private static Player? Me(SEMan seMan)
    {
        Player? me = Player.m_localPlayer;
        return me && seMan.m_character == me && !me!.IsDead() ? me : null;
    }

    // A status is being added by its name (addstatus, or another mod). False: the mod handles it, the game adds nothing
    // (the blessing's own status shows the next moment, with its time and tooltip).
    internal static bool Adding(SEMan seMan, int hash)
    {
        if (PatchGuard.IsDisabled(Feature) || !TryKind(hash, out WeatherKind kind)) return true;
        try
        {
            Player? me = Me(seMan);
            if (!me || !Addable(kind)) return false;
            if (WeatherKinds.InstantBlessing(kind)) SoulTether.Summoned();
            else Blessings.Give(kind, me!.GetPlayerName());
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        return false;
    }

    // One status was taken off by something else than the mod.
    internal static void Removed(SEMan seMan, int hash)
    {
        if (_ours || PatchGuard.IsDisabled(Feature) || !Me(seMan)) return;
        try
        {
            if (hash == RallyCallHash) RallyHorn.Dismiss();
            else if (TryKind(hash, out WeatherKind kind)) End(kind);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Every status is being cleared (clearstatus). Death clears them too, but ends blessings its own way.
    internal static void Clearing(SEMan seMan)
    {
        if (PatchGuard.IsDisabled(Feature) || !Me(seMan)) return;
        try
        {
            foreach (WeatherKind kind in WeatherKinds.All) End(kind);
            RallyHorn.Dismiss();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void End(WeatherKind kind)
    {
        if (WeatherKinds.Personal(kind)) Blessings.End(kind);
        else if (WeatherKinds.ForEveryone(kind)) KinshipRites.EndWait(kind);
    }
}
