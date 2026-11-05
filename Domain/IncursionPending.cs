using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// From the moment the lever is pulled for a Crimson, Ragnarök or Hoard-Bane Rite until the server performs it (the
// obliterator takes 5 to 7 seconds to burn), its Blood Grounds are already sealed: nobody claims a bed in it, nothing carries a
// player into or out of it, nothing is built there, no boss altar near it answers, no raid starts, and no other such
// rite can be performed (see IncursionArena.Sealed). So nothing done in those seconds can get it refused, or abuse it.
// If it is refused all the same, its key items aren't lost: they go back into the obliterator (see Refund, and
// UnmakingPatch for those kept as it burns).
internal static class IncursionPending
{
    // Long enough for the burn and the answer; it ends sooner when the server performs it.
    private const float Seconds = 20f;

    private static WeatherKind _kind;
    private static Vector3 _at;
    private static float _until;
    // The server's: the key items burnt for it, given back if it is refused.
    private static readonly List<(string item, int amount)> Keys = [];
    // The server's: those of the rite last performed, given back if it calls nothing after all (see GiveBack).
    private static WeatherKind _burntKind;
    private static Vector3 _burntAt;
    private static readonly List<(string item, int amount)> Burnt = [];

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<ZPackage>(Rpcs.IncursionPull, RPC_Pull);
        rpc.Register<int, Vector3, float>(Rpcs.IncursionPending, RPC_Pending);
    }

    internal static void ClearSession()
    {
        _kind = WeatherKind.None;
        _at = Vector3.zero;
        _until = 0f;
        Keys.Clear();
        _burntKind = WeatherKind.None;
        Burnt.Clear();
    }

    // The rite whose lever was pulled, and where, if it isn't performed yet.
    internal static bool Now(out WeatherKind kind, out Vector3 at)
    {
        kind = Time.time < _until ? _kind : WeatherKind.None;
        at = _at;
        return kind != WeatherKind.None;
    }

    // Whether this is the rite pending there (it doesn't stand in its own way).
    internal static bool Is(WeatherKind kind, Vector3 at) =>
        Now(out WeatherKind pending, out Vector3 where) && pending == kind && Utils.DistanceXZ(where, at) <= IncursionArena.SamePlace;

    // The rite's key items in this inventory, by prefab, with their amounts.
    internal static List<(string item, int amount)> KeysIn(WeatherKind kind, Inventory inventory)
    {
        List<(string item, int amount)> keys = [];
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (ObliteratorKeys.KindOf(item) != kind || !item.m_dropPrefab) continue;
            int found = keys.FindIndex(k => k.item == item.m_dropPrefab.name);
            if (found < 0) keys.Add((item.m_dropPrefab.name, item.m_stack));
            else keys[found] = (keys[found].item, keys[found].amount + item.m_stack);
        }
        return keys;
    }

    // In the game of the player pulling the lever: the server seals it at once, and keeps what burns for it. Sent again
    // once it burnt, with the key items that really burnt (what is given back if it is refused).
    internal static void Pull(WeatherKind kind, Vector3 at, List<(string item, int amount)> keys)
    {
        if (ZRoutedRpc.instance == null) return;
        ZPackage package = new();
        package.Write((int)kind);
        package.Write(at);
        package.Write(keys.Count);
        foreach ((string item, int amount) in keys)
        {
            package.Write(item);
            package.Write(amount);
        }
        if (ZNet.instance && ZNet.instance.IsServer()) RPC_Pull(ZRoutedRpc.instance.m_id, package);
        else ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), Rpcs.IncursionPull, package);
    }

    // In the game of the player who pulled it: nothing burnt for it after all (or it was refused as it burnt, its key
    // items kept): the seal lifts.
    internal static void Cancel(Vector3 at)
    {
        if (ZRoutedRpc.instance == null) return;
        ZPackage package = new();
        package.Write((int)WeatherKind.None);
        package.Write(at);
        package.Write(0);
        if (ZNet.instance && ZNet.instance.IsServer()) RPC_Pull(ZRoutedRpc.instance.m_id, package);
        else ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), Rpcs.IncursionPull, package);
    }

    private static void RPC_Pull(long sender, ZPackage package)
    {
        if (!ZNet.instance || !ZNet.instance.IsServer()) return;
        WeatherKind kind = (WeatherKind)package.ReadInt();
        Vector3 at = package.ReadVector3();
        if (kind == WeatherKind.None)
        {
            if (!Now(out WeatherKind pending, out Vector3 sealedAt) || Utils.DistanceXZ(sealedAt, at) > IncursionArena.SamePlace) return;
            Keys.Clear();
            Plugin.ModLogger.LogInfo($"{WeatherKinds.Name(pending)}: not performed after all; its Blood Grounds' seal lifts.");
            Tell(WeatherKind.None, at, 0f);
            return;
        }
        List<(string item, int amount)> keys = [];
        int count = package.ReadInt();
        for (int i = 0; i < count; i++) keys.Add((package.ReadString(), package.ReadInt()));
        // One at a time, and none while one goes on.
        if (!WeatherKinds.IsIncursion(kind) || !PluginConfig.IsEnabled(kind) || IncursionArena.Kind != WeatherKind.None) return;
        if (Now(out _, out Vector3 where) && Utils.DistanceXZ(where, at) > IncursionArena.SamePlace) return;
        Keys.Clear();
        // Only its own key items, at most a stack of each.
        foreach ((string item, int amount) in keys)
            if (IsKey(kind, item) && amount > 0) Keys.Add((item, Math.Min(amount, MaxStack(item))));
        Plugin.ModLogger.LogInfo($"{WeatherKinds.Name(kind)}: lever pulled at ({at.x:0}, {at.z:0}); its Blood Grounds are sealed until it is performed.");
        Tell(kind, at, Seconds);
    }

    private static bool IsKey(WeatherKind kind, string item)
    {
        foreach (string key in ObliteratorKeys.Items(kind))
            if (string.Equals(key, item, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static int MaxStack(string item)
    {
        GameObject? prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(item) : null;
        ItemDrop? drop = prefab ? prefab!.GetComponent<ItemDrop>() : null;
        return drop ? Math.Max(1, drop!.m_itemData.m_shared.m_maxStackSize) : 1;
    }

    private static void Tell(WeatherKind kind, Vector3 at, float seconds) =>
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.IncursionPending, (int)kind, at, seconds);

    private static void RPC_Pending(long sender, int kind, Vector3 at, float seconds)
    {
        if (ZRoutedRpc.instance == null || (sender != ZRoutedRpc.instance.GetServerPeerID() && sender != ZRoutedRpc.instance.m_id)) return;
        _kind = (WeatherKind)kind;
        _at = at;
        _until = _kind == WeatherKind.None ? 0f : Time.time + seconds;
    }

    // On the server, as the rite there is performed or refused: no longer pending. Refused, its key items burnt for it
    // go back into the obliterator; performed, they are kept a while (see GiveBack).
    internal static void Done(WeatherKind kind, Vector3 at, bool refused)
    {
        if (!Is(kind, at)) return;
        if (refused && Keys.Count > 0) Refund(kind, at, Keys);
        _burntKind = refused ? WeatherKind.None : kind;
        _burntAt = at;
        Burnt.Clear();
        if (!refused) Burnt.AddRange(Keys);
        Keys.Clear();
        Tell(WeatherKind.None, at, 0f);
    }

    // On the server: the rite performed there calls nothing after all (the Ragnarök Rite with nobody past the early
    // bosses), so the key items burnt for it go back into the obliterator. Whether there were any to give back.
    internal static bool GiveBack(WeatherKind kind, Vector3 at)
    {
        if (_burntKind != kind || Utils.DistanceXZ(_burntAt, at) > IncursionArena.SamePlace || Burnt.Count == 0) return false;
        Refund(kind, at, Burnt);
        _burntKind = WeatherKind.None;
        Burnt.Clear();
        return true;
    }

    private static void Refund(WeatherKind kind, Vector3 at, List<(string item, int amount)> keys)
    {
        ZDOID obliterator = Obliterator(at);
        ZDO? zdo = obliterator.IsNone() ? null : ZDOMan.instance.GetZDO(obliterator);
        long owner = zdo?.GetOwner() ?? 0L;
        if (owner == 0L)
        {
            Plugin.ModLogger.LogWarning($"{WeatherKinds.Name(kind)}: no obliterator to give its key items back to.");
            return;
        }
        ZPackage package = new();
        package.Write(keys.Count);
        foreach ((string item, int amount) in keys)
        {
            package.Write(item);
            package.Write(amount);
        }
        ZRoutedRpc.instance.InvokeRoutedRPC(owner, obliterator, Rpcs.ObliteratorRefund, package);
        Plugin.ModLogger.LogInfo($"{WeatherKinds.Name(kind)}: its key items go back into the obliterator.");
    }

    private const string ObliteratorPrefab = "incinerator";

    private static ZDOID Obliterator(Vector3 at)
    {
        List<ZDO> found = [];
        ZDOMan.instance.FindObjects(ZoneSystem.GetZone(at), found, []);
        int prefab = ObliteratorPrefab.GetStableHashCode();
        foreach (ZDO zdo in found)
            if (zdo.GetPrefab() == prefab && Utils.DistanceXZ(zdo.GetPosition(), at) < IncursionArena.SamePlace) return zdo.m_uid;
        return ZDOID.None;
    }
}
