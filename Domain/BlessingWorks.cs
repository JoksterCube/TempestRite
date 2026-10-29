using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Luminary Rite: every player carrying it (read off their character, so others see it too) shines like a lantern:
// - a warm light at head height shines out all around them, onto the ground and walls out to the Light Range;
// - a soft fill light floats beside them on the side this game's camera sees, so their body is lit too (the main light
//   is inside the body, so it doesn't light the body itself). Lighting is drawn by each game for itself, so every viewer
//   gets the fill on the side they look from.
// With Light Shadows on, floors, ceilings and walls block the main light, so it doesn't shine through to the floor below
// or the next room. Nothing within GlowShadowNear of it casts a shadow, so the player's own body and cape don't throw
// big shadows around them. The floor they stand on is farther, so it still blocks; a ceiling closer than that doesn't,
// but its far side faces away from the light, so the floor above isn't lit. The fill has no shadows (the body would
// throw one away from the camera) and reaches only a little past the body, so it doesn't reach other floors either.
internal static class BlessingLights
{
    private const string Feature = "Luminary Rite";
    private const float Interval = 0.5f;
    private static readonly Color Warm = new(1f, 0.85f, 0.6f);

    // The main light: how high above the feet it sits, how bright it is at a Light Strength of 100%, and how close to it
    // things are left out of its shadow (all of the body but the shins and feet).
    private const float GlowHeight = 1.6f;
    private const float GlowIntensity = 2f;
    private const float GlowShadowNear = 1.2f;
    // The fill: how far toward the camera and above the feet it floats, how far it reaches, how bright it is at a Body
    // Light of 100%.
    private const float FillDistance = 1.4f;
    private const float FillHeight = 1.5f;
    private const float FillRange = 2.5f;
    private const float FillIntensity = 1.6f;

    private sealed class Glow(GameObject holder, Light main, Light fill)
    {
        internal GameObject Holder { get; } = holder;
        internal Light Main { get; } = main;
        internal Light Fill { get; } = fill;
        internal bool Alive => Holder && Main && Fill;
    }

    private static readonly Dictionary<Player, Glow> Glows = [];
    private static readonly List<Player> Gone = [];
    private static float _next;

    internal static void ClearSession()
    {
        foreach (Glow glow in Glows.Values)
            if (glow.Holder) UnityEngine.Object.Destroy(glow.Holder);
        Glows.Clear();
        _next = 0f;
    }

    // Every frame: the fill follows the camera; now and then, who shines is checked again.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            if (Time.time >= _next)
            {
                _next = Time.time + Interval;
                Refresh();
            }
            if (Glows.Count > 0) PlaceFills();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Refresh()
    {
        foreach (Player player in Player.GetAllPlayers())
        {
            if (!player) continue;
            bool shines = !player.IsDead() && Blessings.Has(player, WeatherKind.Luminary);
            Glows.TryGetValue(player, out Glow? glow);
            if (shines && (glow == null || !glow.Alive))
            {
                if (glow != null && glow.Holder) UnityEngine.Object.Destroy(glow.Holder);
                Glows[player] = glow = Make(player);
            }
            else if (!shines && glow != null)
            {
                if (glow.Holder) UnityEngine.Object.Destroy(glow.Holder);
                Glows.Remove(player);
            }
            // Set again each time, so a changed setting shows at once.
            if (shines && glow != null && glow.Alive) Apply(glow);
        }

        Gone.Clear();
        foreach (KeyValuePair<Player, Glow> pair in Glows)
            if (!pair.Key || !pair.Value.Alive) Gone.Add(pair.Key!);
        foreach (Player player in Gone)
        {
            if (Glows.TryGetValue(player, out Glow glow) && glow.Holder) UnityEngine.Object.Destroy(glow.Holder);
            Glows.Remove(player);
        }
    }

    private static void PlaceFills()
    {
        Camera camera = Utils.GetMainCamera();
        if (!camera) return;
        Vector3 eye = camera.transform.position;
        foreach (KeyValuePair<Player, Glow> pair in Glows)
        {
            if (!pair.Key || !pair.Value.Alive) continue;
            Vector3 feet = pair.Key.transform.position;
            Vector3 toward = eye - feet;
            toward.y = 0f;
            toward = toward.sqrMagnitude > 0.01f ? toward.normalized : pair.Key.transform.forward;
            pair.Value.Fill.transform.position = feet + toward * FillDistance + Vector3.up * FillHeight;
        }
    }

    private static Glow Make(Player player)
    {
        GameObject holder = new("TempestRite_Luminary");
        holder.transform.SetParent(player.transform, false);

        GameObject mainObject = new("Glow");
        mainObject.transform.SetParent(holder.transform, false);
        mainObject.transform.localPosition = new Vector3(0f, GlowHeight, 0f);
        Light main = mainObject.AddComponent<Light>();
        main.type = LightType.Point;
        Style(main);

        GameObject fillObject = new("Fill");
        fillObject.transform.SetParent(holder.transform, false);
        fillObject.transform.localPosition = new Vector3(0f, FillHeight, FillDistance);
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.range = FillRange;
        fill.shadows = LightShadows.None;
        Style(fill);

        Glow glow = new(holder, main, fill);
        Apply(glow);
        return glow;
    }

    private static void Style(Light light)
    {
        light.color = Warm;
        light.renderMode = LightRenderMode.ForcePixel;
    }

    // Range, brightness and shadows, from the settings.
    private static void Apply(Glow glow)
    {
        LightShadows shadows = PluginConfig.LuminaryShadows.IsOn() ? LightShadows.Soft : LightShadows.None;
        glow.Main.range = PluginConfig.LuminaryRange.Value;
        glow.Main.intensity = GlowIntensity * PluginConfig.LuminaryStrength.Value;
        glow.Main.shadows = shadows;
        glow.Main.shadowNearPlane = GlowShadowNear;
        float fillIntensity = FillIntensity * PluginConfig.LuminaryBodyLight.Value;
        glow.Fill.intensity = fillIntensity;
        glow.Fill.enabled = fillIntensity > 0f;
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
