using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Luminary Rite: every player carrying it (read off their character, so others see it too) gets a warm point light
// on them, shining in every direction.
internal static class BlessingLights
{
    private const string Feature = "Luminary Rite";
    private const float Interval = 0.5f;
    private const float Intensity = 1.5f;
    private static readonly Color Warm = new(1f, 0.8f, 0.55f);
    private static readonly Vector3 Offset = new(0f, 1.6f, 0f);

    private static readonly Dictionary<Player, Light> Lights = [];
    private static readonly List<Player> Gone = [];
    private static float _next;

    internal static void ClearSession()
    {
        foreach (Light light in Lights.Values)
            if (light) UnityEngine.Object.Destroy(light.gameObject);
        Lights.Clear();
        _next = 0f;
    }

    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || Time.time < _next) return;
        _next = Time.time + Interval;
        try
        {
            float range = PluginConfig.LuminaryRange.Value;
            foreach (Player player in Player.GetAllPlayers())
            {
                if (!player) continue;
                bool shines = !player.IsDead() && Blessings.Has(player, WeatherKind.Luminary);
                Lights.TryGetValue(player, out Light? light);
                if (shines && !light) Lights[player] = light = Make(player);
                else if (!shines && light)
                {
                    UnityEngine.Object.Destroy(light.gameObject);
                    Lights.Remove(player);
                }
                if (shines && light && light!.range != range) light.range = range;
            }

            Gone.Clear();
            foreach (KeyValuePair<Player, Light> pair in Lights)
                if (!pair.Key || !pair.Value) Gone.Add(pair.Key!);
            foreach (Player player in Gone)
            {
                if (Lights.TryGetValue(player, out Light light) && light) UnityEngine.Object.Destroy(light.gameObject);
                Lights.Remove(player);
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static Light Make(Player player)
    {
        GameObject holder = new("TempestRite_Luminary");
        holder.transform.SetParent(player.transform, false);
        holder.transform.localPosition = Offset;
        Light light = holder.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = Warm;
        light.intensity = Intensity;
        light.range = PluginConfig.LuminaryRange.Value;
        light.shadows = LightShadows.None;
        return light;
    }
}

// The Abyssal Draw Rite: items farther than the game's own pickup range, up to the Pickup Range, fly to the local
// player the way the game pulls them in close up; the game picks them up once they are near. An item goes to a closer
// player instead when they could pick it up (it is within their own range, with this blessing or without) and their
// inventory isn't full, so blessed players don't take everything from those around them.
internal static class ItemDraw
{
    private const string Feature = "Abyssal Draw Rite";
    // As fast as the game pulls items in.
    private const float Speed = 15f;
    private static readonly Collider[] Colliders = new Collider[256];

    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        Player? player = Player.m_localPlayer;
        if (!player || player!.IsDead() || player.IsTeleporting() || !Player.m_enableAutoPickup || !Blessings.IsActive(WeatherKind.AbyssalDraw)) return;
        try { Draw(player); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Draw(Player player)
    {
        float range = PluginConfig.DrawRange.Value;
        float own = player.m_autoPickupRange;
        if (range <= own) return;
        Vector3 center = player.transform.position + Vector3.up;
        int count = Physics.OverlapSphereNonAlloc(center, range, Colliders, player.m_autoPickupMask);
        Inventory inventory = player.GetInventory();
        for (int i = 0; i < count; i++)
        {
            Rigidbody body = Colliders[i].attachedRigidbody;
            if (!body) continue;
            ItemDrop item = body.GetComponent<ItemDrop>();
            if (!item || !item.m_autoPickup || item.IsPiece() || player.HaveUniqueKey(item.m_itemData.m_shared.m_name)) continue;
            ZNetView view = item.GetComponent<ZNetView>();
            if (!view || !view.IsValid()) continue;
            float distance = Vector3.Distance(item.transform.position, center);
            // Within the game's own range it does the rest.
            if (distance <= own || distance > range || ForSomeoneCloser(player, item.transform.position, distance)) continue;
            if (!item.CanPickup())
            {
                item.RequestOwn();
                continue;
            }
            if (item.InTar()) continue;
            item.Load();
            if (!inventory.CanAddItem(item.m_itemData) || item.m_itemData.GetWeight() + inventory.GetTotalWeight() > player.GetMaxCarryWeight()) continue;
            item.transform.position += (center - item.transform.position).normalized * (Speed * Time.deltaTime);
        }
    }

    private static bool ForSomeoneCloser(Player me, Vector3 position, float distance)
    {
        foreach (Player other in Player.GetAllPlayers())
        {
            if (!other || other == me || other.IsDead()) continue;
            float theirs = Vector3.Distance(position, other.transform.position + Vector3.up);
            if (theirs >= distance) continue;
            float reach = Blessings.Has(other, WeatherKind.AbyssalDraw) ? PluginConfig.DrawRange.Value : other.m_autoPickupRange;
            if (theirs <= reach && !Blessings.IsFull(other)) return true;
        }
        return false;
    }
}
