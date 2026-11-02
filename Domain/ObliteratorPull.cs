using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// Pulling the lever only asks the obliterator's owner to burn; it burns 5 to 7 seconds later, what is in it then, and may
// refuse (in use, empty). So the rites are decided by what burns, not by what was in it at the pull: right as it burns,
// the owner tells the player who pulled the lever which rites' key items are burning (see Report), and only those are
// performed, once the owner says it burnt, after all their checks are made again. A key item taken out during the lever
// animation performs nothing.
// At the pull, the player is warned early of a rite that can't be performed, and a Crimson, Ragnarök or Hoard-Bane Rite
// that can is sealed from then on (see IncursionPending). One refused as it burns doesn't stop the lever: its key items
// stay in the obliterator (see UnmakingPatch), and the rest burns and performs its rites. A Rally-Horn Rite while
// another's call still waits for answers is kept the same way (see RallyPending).
internal static class ObliteratorPull
{
    // The owner answers after 5 to 7 seconds of lever animation.
    private const float AnswerTimeout = 30f;

    private static Incinerator? _incinerator;
    private static float _pulledAt;
    // The incursion rite sealed at the pull, if any.
    private static bool _sealed;
    // What the owner said burnt (null: no word yet).
    private static Burnt? _burnt;

    private sealed class Burnt
    {
        internal readonly List<WeatherKind> Kinds = [];
        // The chest leading the Hoard-Bane Rite, which picks its fight (see HoardBaneRush).
        internal string? Chest;
        internal readonly Dictionary<WeatherKind, List<(string item, int amount)>> Keys = [];
    }

    internal static void ClearSession()
    {
        _incinerator = null;
        _sealed = false;
        _burnt = null;
    }

    // Why a rite burnt here can't be performed now (an incursion rite going on, or where it would be), or null.
    private static string? Refused(WeatherKind kind, Vector3 at) => IncursionArena.RiteRefusedHere(kind, at);

    // A refused rite's key items aren't burnt, nor lost: told so.
    private static void TellKept(string why) => PlayerMessages.Warning(why + "\n" + Messages.IncursionKeysKept);

    // In the obliterator owner's game, right before it burns (see UnmakingPatch): the rites whose key items stay in it,
    // those that can't be performed there now (an incursion rite elsewhere, or any but the Sanctuary Rite at the
    // obliterator of the one going on).
    internal static HashSet<WeatherKind> KeptKinds(Incinerator incinerator)
    {
        HashSet<WeatherKind> kept = [];
        Vector3 at = incinerator.transform.position;
        foreach (ObliteratorKeys.Picked p in new List<ObliteratorKeys.Picked>(ObliteratorKeys.PickAll(incinerator.m_container.GetInventory())))
            if (PluginConfig.IsEnabled(p.Kind) && Refused(p.Kind, at) != null) kept.Add(p.Kind);
            // Another Rally-Horn call while one still waits for answers.
            else if (PluginConfig.IsEnabled(p.Kind) && RallyPending.Refused(p.Kind) != null) kept.Add(p.Kind);
        return kept;
    }

    internal static void Pulled(Incinerator incinerator)
    {
        ClearSession();
        // Rites off: it burns as without the mod.
        if (!ObliteratorSwitch.IsOn(incinerator)) return;
        IReadOnlyList<ObliteratorKeys.Picked> picked = ObliteratorKeys.PickAll(incinerator.m_container.GetInventory());
        // The items burn either way, as without the mod.
        if (!WeatherSync.LocalPlayerMayChange())
        {
            if (picked.Count > 0) PlayerMessages.Warning(Messages.RefusedAdminsOnly);
            return;
        }
        _incinerator = incinerator;
        _pulledAt = Time.time;
        Vector3 at = incinerator.transform.position;
        Inventory inventory = incinerator.m_container.GetInventory();
        foreach (ObliteratorKeys.Picked p in new List<ObliteratorKeys.Picked>(picked))
        {
            if (!PluginConfig.IsEnabled(p.Kind)) continue;
            if (RallyPending.Refused(p.Kind) is { } pending)
            {
                TellKept(pending);
                continue;
            }
            if (Refused(p.Kind, at) is { } why)
            {
                TellKept(why);
                continue;
            }
            if (!WeatherKinds.IsIncursion(p.Kind)) continue;
            IncursionPending.Pull(p.Kind, at, IncursionPending.KeysIn(p.Kind, inventory));
            _sealed = true;
        }
    }

    // In the obliterator owner's game, in the same step as the burn, once the refused rites' key items are taken out:
    // tells the player who pulled the lever what is burning now.
    internal static void Report(Incinerator incinerator, long puller)
    {
        if (!incinerator.m_nview.IsValid() || !incinerator.m_nview.IsOwner() || incinerator.m_container.IsInUse()) return;
        if (!ObliteratorSwitch.IsOn(incinerator)) return;
        Inventory inventory = incinerator.m_container.GetInventory();
        List<ObliteratorKeys.Picked> picked = [.. ObliteratorKeys.PickAll(inventory)];
        ZPackage package = new();
        package.Write(picked.Count);
        string chest = string.Empty;
        foreach (ObliteratorKeys.Picked p in picked)
        {
            package.Write((int)p.Kind);
            List<(string item, int amount)> keys = WeatherKinds.IsIncursion(p.Kind) ? IncursionPending.KeysIn(p.Kind, inventory) : [];
            package.Write(keys.Count);
            foreach ((string item, int amount) in keys)
            {
                package.Write(item);
                package.Write(amount);
            }
            if (p.Kind == WeatherKind.HoardBane) chest = (p.Item.m_dropPrefab ? p.Item.m_dropPrefab.name : ChestOf(p.Item)) ?? string.Empty;
        }
        package.Write(chest);
        incinerator.m_nview.InvokeRPC(puller, Rpcs.ObliteratorBurnt, package);
    }

    // In the game of the player who pulled the lever: the owner's word on what is burning, which comes just before its
    // answer.
    internal static void Burning(Incinerator incinerator, long sender, ZPackage package)
    {
        if (_incinerator != incinerator || !incinerator.m_nview.IsValid() || incinerator.m_nview.GetZDO().GetOwner() != sender) return;
        Burnt burnt = new();
        int count = package.ReadInt();
        for (int i = 0; i < count; i++)
        {
            WeatherKind kind = (WeatherKind)package.ReadInt();
            List<(string item, int amount)> keys = [];
            int keyCount = package.ReadInt();
            for (int k = 0; k < keyCount; k++) keys.Add((package.ReadString(), package.ReadInt()));
            if (!burnt.Kinds.Contains(kind)) burnt.Kinds.Add(kind);
            burnt.Keys[kind] = keys;
        }
        string chest = package.ReadString();
        burnt.Chest = chest.Length > 0 ? chest : null;
        _burnt = burnt;
    }

    // response: Incinerator.Response (0 fail / in use, 1 burnt, 2 burnt into something, 3 empty).
    internal static void Answered(Incinerator incinerator, int response)
    {
        if (_incinerator != incinerator) return;
        Burnt? burnt = _burnt;
        bool wasSealed = _sealed;
        bool inTime = Time.time - _pulledAt <= AnswerTimeout;
        ClearSession();
        Vector3 at = incinerator.transform.position;
        bool didBurn = inTime && response is (int)Incinerator.Response.Success or (int)Incinerator.Response.Conversion;

        // Only what burnt, and only what passes every check now.
        List<WeatherKind> kinds = [];
        WeatherKind incursion = WeatherKind.None;
        if (didBurn && burnt != null && WeatherSync.LocalPlayerMayChange())
            foreach (WeatherKind kind in burnt.Kinds)
            {
                if (!PluginConfig.IsEnabled(kind)) continue;
                if (RallyPending.Refused(kind) is { } pending)
                {
                    TellKept(pending);
                    continue;
                }
                if (WeatherKinds.IsIncursion(kind))
                {
                    if (Refused(kind, at) is { } why)
                    {
                        TellKept(why);
                        continue;
                    }
                    incursion = kind;
                }
                kinds.Add(kind);
            }

        // The seal is for what burnt, with the key items that really burnt (given back if the server refuses it).
        if (incursion != WeatherKind.None) IncursionPending.Pull(incursion, at, burnt!.Keys[incursion]);
        else if (wasSealed) IncursionPending.Cancel(at);
        if (kinds.Count == 0) return;
        // Told first: the server reads it as the rite is performed.
        if (burnt!.Chest != null && kinds.Contains(WeatherKind.HoardBane)) HoardBaneRush.SendChest(burnt.Chest, at);
        WeatherSync.RequestChange(kinds, at);
    }

    // An item without a drop prefab: which of the rite's key items it is.
    private static string? ChestOf(ItemDrop.ItemData item)
    {
        foreach (string key in ObliteratorKeys.Items(WeatherKind.HoardBane))
            if (ObliteratorKeys.IsItem(item, key)) return key;
        return null;
    }
}
