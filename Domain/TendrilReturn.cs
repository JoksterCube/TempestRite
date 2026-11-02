using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Tendril-Draw Rite, in the blessed player's own game (it shoots, throws and owns what it throws):
// - an arrow or crossbow bolt it shoots is sometimes not taken from the inventory (Ammo Saved Chance);
// - a weapon it throws (a spear) is dropped by the game where it strikes, as usual, and that dropped item then flies
//   back to the player at Spear Return Speed and is picked up: back in the hands when they are empty (once the throw
//   ends, when it is caught point blank before that). One that strikes
//   close by is picked up at once. The dropped item is an ordinary one, so every player sees it fly, mod or not.
//   No other player can pick it up while it comes back. It is let go where it is (an ordinary item on the ground,
//   anyone's to take) when the player dies, teleports or leaves, it changes owner anyway, or it hasn't arrived in a
//   while; with no room in the inventory it falls at the player's feet.
internal static class TendrilReturn
{
    private const string Feature = "Tendril-Draw Rite";
    // Within this far of the player it is picked up at once.
    private const float AtOnce = 2.5f;
    // Flying, it is caught within this far of the player's middle.
    private const float CatchDistance = 0.8f;
    // Let go after this long in flight.
    private const float GiveUpSeconds = 20f;

    private sealed class Flight(ItemDrop item, float started)
    {
        internal ItemDrop Item { get; } = item;
        internal float Started { get; } = started;
    }

    private static readonly List<Flight> Flights = [];

    // A weapon caught while the throw was still going on, so the game couldn't take it up in the hands: it is, once
    // the throw ends, if the hands are still empty. Given up after EquipWaitSeconds.
    private const float EquipWaitSeconds = 3f;
    private static ItemDrop.ItemData? _toEquip;
    private static float _toEquipUntil;

    // Arrows and bolts: whether this shot keeps its ammo.
    internal static bool SavesAmmo(Humanoid? shooter, ItemDrop.ItemData? weapon)
    {
        if (!shooter || shooter != Player.m_localPlayer || weapon == null || !Blessings.IsActive(WeatherKind.TendrilDraw)) return false;
        if (weapon.m_shared.m_skillType is not (Skills.SkillType.Bows or Skills.SkillType.Crossbows)) return false;
        float chance = Mathf.Clamp01(PluginConfig.TendrilAmmoChance.Value);
        return chance > 0f && UnityEngine.Random.value < chance;
    }

    // Whether this thrown item, about to be dropped where it struck, comes back to its thrower.
    internal static bool Returns(Character? thrower, ItemDrop.ItemData? item) =>
        thrower && thrower == Player.m_localPlayer && item != null && item.IsWeapon() && Blessings.IsActive(WeatherKind.TendrilDraw);

    // The game just dropped a returning weapon where it struck: it starts back.
    internal static void Dropped(ItemDrop? item)
    {
        if (!item) return;
        Rigidbody body = item!.GetComponent<Rigidbody>();
        if (body)
        {
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
        }
        SetColliders(item, false);
        Flights.Add(new Flight(item, Time.time));
    }

    // Whether this dropped item is still flying back. While it is, no one else may take it: another player picks an
    // item up by asking its owner (this player) for it, and that ask is refused.
    internal static bool IsReturning(ItemDrop? item)
    {
        if (!item) return false;
        foreach (Flight flight in Flights)
            if (flight.Item == item) return true;
        return false;
    }

    // Every frame: the flying weapons move on toward the player.
    internal static void Update()
    {
        if (_toEquip != null) EquipCaught();
        if (Flights.Count == 0) return;
        if (PatchGuard.IsDisabled(Feature))
        {
            LetAllGo();
            return;
        }
        try { Fly(); }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            LetAllGo();
        }
    }

    private static void Fly()
    {
        Player? player = Player.m_localPlayer;
        bool gone = !player || player!.IsDead() || player.IsTeleporting();
        float step = Mathf.Max(1f, PluginConfig.TendrilReturnSpeed.Value) * Time.deltaTime;
        for (int i = Flights.Count - 1; i >= 0; i--)
        {
            Flight flight = Flights[i];
            ItemDrop item = flight.Item;
            if (!item)
            {
                Flights.RemoveAt(i);
                continue;
            }
            ZNetView view = item.GetComponent<ZNetView>();
            if (gone || !view || !view.IsValid() || !view.IsOwner() || Time.time - flight.Started > GiveUpSeconds)
            {
                Flights.RemoveAt(i);
                LetGo(item);
                continue;
            }

            Vector3 target = player!.GetCenterPoint();
            Vector3 toward = target - item.transform.position;
            float distance = toward.magnitude;
            bool arrived = distance <= CatchDistance || (distance <= AtOnce && Time.time - flight.Started < 0.1f);
            if (!arrived)
            {
                if (step >= distance - CatchDistance) arrived = true;
                else item.transform.position += toward / distance * step;
            }
            if (!arrived) continue;

            Flights.RemoveAt(i);
            item.transform.position = target;
            ItemDrop.ItemData caught = item.m_itemData;
            bool handsFree = HandsFree(player);
            if (!player.Pickup(item.gameObject, autoequip: true, autoPickupDelay: false))
            {
                if (!item) continue;
                item.transform.position = player.transform.position + Vector3.up * 0.3f;
                LetGo(item);
                continue;
            }
            // The game doesn't take up a weapon during an attack: a spear caught point blank comes back mid-throw.
            if (handsFree && !player.IsItemEquiped(caught))
            {
                _toEquip = caught;
                _toEquipUntil = Time.time + EquipWaitSeconds;
            }
        }
    }

    private static void EquipCaught()
    {
        Player? player = Player.m_localPlayer;
        ItemDrop.ItemData item = _toEquip!;
        if (!player || player!.IsDead() || Time.time > _toEquipUntil || !player.GetInventory().ContainsItem(item) || player.IsItemEquiped(item)
            || !HandsFree(player))
        {
            _toEquip = null;
            return;
        }
        if (player.InAttack() || player.InDodge()) return;
        _toEquip = null;
        try { player.EquipItem(item); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // As the game checks before taking up a picked up weapon: nothing in the right hand and no two-handed weapon in
    // the left, held or hidden away.
    private static bool HandsFree(Player player) =>
        player.m_rightItem == null && player.m_hiddenRightItem == null
        && (player.m_leftItem == null || !player.m_leftItem.IsTwoHanded())
        && (player.m_hiddenLeftItem == null || !player.m_hiddenLeftItem.IsTwoHanded());

    private static void LetAllGo()
    {
        foreach (Flight flight in Flights)
            if (flight.Item) LetGo(flight.Item);
        Flights.Clear();
    }

    // Back to an ordinary item: it falls where it is.
    private static void LetGo(ItemDrop item)
    {
        SetColliders(item, true);
        Rigidbody body = item.GetComponent<Rigidbody>();
        if (body) body.isKinematic = false;
    }

    // Off while it flies, so it doesn't bump the player or anything on its way.
    private static void SetColliders(ItemDrop item, bool on)
    {
        foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
            if (!collider.isTrigger) collider.enabled = on;
    }
}
