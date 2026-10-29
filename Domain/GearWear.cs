using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Carapace Temper Rite: the game wears gear down in many places (swings, hits taken, blocks, building, tools). Rather
// than catching each one, every frame the local player's equipped gear is compared with the frame before, and part of
// what it lost is given back. Repairs and newly equipped gear are only noted.
internal static class GearWear
{
    private const string Feature = "Carapace Temper Rite";
    private static readonly Dictionary<ItemDrop.ItemData, float> Last = [];
    private static readonly Dictionary<ItemDrop.ItemData, float> Now = [];

    internal static void ClearSession() => Last.Clear();

    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        Player? player = Player.m_localPlayer;
        if (!player || player!.IsDead() || !Blessings.IsActive(WeatherKind.CarapaceTemper))
        {
            Last.Clear();
            return;
        }
        try
        {
            // The gear wears by the Wear Multiplier of the usual: the rest is given back.
            float giveBack = 1f - Mathf.Clamp01(PluginConfig.WearMultiplier.Value);
            Now.Clear();
            foreach (ItemDrop.ItemData item in player.GetInventory().GetEquippedItems())
            {
                if (!item.m_shared.m_useDurability) continue;
                if (Last.TryGetValue(item, out float before) && item.m_durability < before)
                    item.m_durability = Mathf.Min(item.GetMaxDurability(), item.m_durability + (before - item.m_durability) * giveBack);
                Now[item] = item.m_durability;
            }
            Last.Clear();
            foreach (KeyValuePair<ItemDrop.ItemData, float> pair in Now) Last[pair.Key] = pair.Value;
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// The Grave-Bind Rite's beam: after a death under it, a tall beam of light stands over the tombstone, seen from far
// away (it ignores fog), until the tombstone is emptied. Only in the dead player's own game: it is a local object,
// placed only on the local player's death, so no one else sees it.
// One flat strip that turns to face the camera, bright in the middle and soft at the edges, fading out at the top.
internal static class GraveBeacon
{
    private const string Feature = "Grave-Bind beam";
    private const float Height = 600f;
    private const float Width = 1.6f;
    private const float CheckInterval = 2f;
    // Close enough to tell whether the tombstone is still there.
    private const float CheckRange = 50f;
    private const float TombstoneReach = 8f;
    private static readonly Color Core = new(0.85f, 0.97f, 1f);
    private static readonly Color Halo = new(0.4f, 0.75f, 1f);

    private static GameObject? _beam;
    private static Material? _material;
    private static Texture2D? _texture;
    private static Vector3 _at;
    private static float _nextCheck;
    private static readonly Collider[] Hits = new Collider[64];

    internal static void ClearSession() => Remove();

    internal static void Place(Vector3 at)
    {
        Remove();
        if (!PluginConfig.GraveBeacon.IsOn()) return;
        _at = at;
        _beam = new GameObject("TempestRite_GraveBeam");
        _beam.transform.position = at;

        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Quad);
        UnityEngine.Object.Destroy(strip.GetComponent<Collider>());
        strip.transform.SetParent(_beam.transform, false);
        // A quad is 1 unit square around its middle.
        strip.transform.localPosition = Vector3.up * (Height / 2f);
        strip.transform.localScale = new Vector3(Width, Height, 1f);
        MeshRenderer renderer = strip.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        // Sprites/Default draws both sides and blends by alpha.
        Shader? shader = Shader.Find("Sprites/Default");
        _material = shader ? new Material(shader) : new Material(renderer.sharedMaterial);
        _material.mainTexture = Texture();
        _material.color = Color.white;
        _material.renderQueue = 3100;
        renderer.sharedMaterial = _material;

        Face();
        _nextCheck = Time.time + 10f;
    }

    // Bright core fading to a blue glow at the sides; fades in just above the ground and out over the top 40%.
    private static Texture2D Texture()
    {
        if (_texture) return _texture!;
        const int w = 64, h = 128;
        _texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };
        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            float vertical = Mathf.Clamp01(v / 0.01f) * Mathf.Clamp01((1f - v) / 0.4f);
            for (int x = 0; x < w; x++)
            {
                float d = Mathf.Abs((x + 0.5f) / w - 0.5f) * 2f;
                float core = Mathf.Exp(-(d * d) / 0.02f);
                float glow = Mathf.Exp(-(d * d) / 0.18f) * (1f - d);
                float alpha = Mathf.Clamp01(0.85f * core + 0.3f * glow) * vertical;
                Color color = Color.Lerp(Halo, Core, core);
                color.a = alpha;
                pixels[y * w + x] = color;
            }
        }
        _texture.SetPixels(pixels);
        _texture.Apply();
        return _texture;
    }

    // Turn the strip about its upright axis to face the camera.
    private static void Face()
    {
        Camera? camera = Utils.GetMainCamera();
        if (!_beam || !camera) return;
        Vector3 toCamera = camera!.transform.position - _at;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude > 0.0001f) _beam!.transform.rotation = Quaternion.LookRotation(toCamera);
    }

    private static void Remove()
    {
        if (_beam) UnityEngine.Object.Destroy(_beam);
        if (_material) UnityEngine.Object.Destroy(_material);
        _beam = null;
        _material = null;
    }

    // Every frame: faces the camera; gone once the tombstone is, as seen from near it.
    internal static void Update()
    {
        if (!_beam || PatchGuard.IsDisabled(Feature)) return;
        try { Face(); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        if (Time.time < _nextCheck) return;
        _nextCheck = Time.time + CheckInterval;
        try
        {
            if (!PluginConfig.GraveBeacon.IsOn())
            {
                Remove();
                return;
            }
            Player? player = Player.m_localPlayer;
            if (!player || Utils.DistanceXZ(player!.transform.position, _at) > CheckRange) return;
            int count = Physics.OverlapSphereNonAlloc(_at, TombstoneReach, Hits);
            for (int i = 0; i < count; i++)
                if (Hits[i] && Hits[i].GetComponentInParent<TombStone>()) return;
            Remove();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// The Grave-Bind Rite gives back the gear: as the tombstone is made, the gear worn (weapons put away included) is noted,
// and once that tombstone is looted (Take All, or emptied by hand) it is put back on as before the death. Items come
// back out of the grave as new objects, so they are matched by what they are and where they sat in the inventory.
internal static class GraveGear
{
    private const string Feature = "Grave-Bind gear";
    // How far the tombstone may be from where it was made (the game pulls it back past 4 m).
    private const float TombstoneReach = 8f;

    private readonly struct Worn
    {
        internal readonly string Name;
        internal readonly int Quality;
        internal readonly int Variant;
        internal readonly long Crafter;
        internal readonly int WorldLevel;
        internal readonly Vector2i Slot;

        internal Worn(ItemDrop.ItemData item)
        {
            Name = item.m_shared.m_name;
            Quality = item.m_quality;
            Variant = item.m_variant;
            Crafter = item.m_crafterID;
            WorldLevel = item.m_worldLevel;
            Slot = item.m_gridPos;
        }

        internal bool Matches(ItemDrop.ItemData item) =>
            item.m_shared.m_name == Name && item.m_quality == Quality && item.m_variant == Variant &&
            item.m_crafterID == Crafter && item.m_worldLevel == WorldLevel;
    }

    private static readonly List<Worn> Gear = new();
    private static Vector3 _at;

    internal static void ClearSession() => Gear.Clear();

    // Before the game takes the gear off and moves it into the tombstone.
    internal static void Remember(Player player, Vector3 at)
    {
        Gear.Clear();
        if (!PluginConfig.GraveReequip.IsOn()) return;
        _at = at;
        foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            if (item.m_equipped || item == player.m_hiddenRightItem || item == player.m_hiddenLeftItem)
                Gear.Add(new Worn(item));
    }

    // Whether this tombstone is the one the gear went into.
    internal static bool IsTheGrave(TombStone tombstone) =>
        Gear.Count > 0 && tombstone.IsOwner() && Vector3.Distance(tombstone.transform.position, _at) <= TombstoneReach;

    // Puts the noted gear back on from the inventory, then forgets it.
    internal static void Reequip()
    {
        if (Gear.Count == 0 || PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Player? player = Player.m_localPlayer;
            if (!player || player!.IsDead() || !PluginConfig.GraveReequip.IsOn()) return;
            List<ItemDrop.ItemData> items = player.GetInventory().GetAllItems();
            HashSet<ItemDrop.ItemData> used = new();
            int equipped = 0;
            foreach (Worn worn in Gear)
            {
                ItemDrop.ItemData? found = null;
                foreach (ItemDrop.ItemData item in items)
                {
                    if (used.Contains(item) || !worn.Matches(item)) continue;
                    // The one in its old slot first, else any.
                    if (item.m_gridPos == worn.Slot) { found = item; break; }
                    found ??= item;
                }
                if (found == null) continue;
                used.Add(found);
                if (player.IsItemEquiped(found) || player.EquipItem(found)) equipped++;
            }
            if (equipped > 0) PlayerMessages.TopLeft(Constants.Messages.GraveBindReequipped);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        finally { Gear.Clear(); }
    }
}
