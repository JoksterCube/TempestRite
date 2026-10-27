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
            float giveBack = Mathf.Clamp01(PluginConfig.WearReductionPercent.Value / 100f);
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
// away (it ignores fog), until the tombstone is emptied. Only in the dead player's own game.
internal static class GraveBeacon
{
    private const string Feature = "Grave-Bind beam";
    private const float Height = 600f;
    private const float CheckInterval = 2f;
    // Close enough to tell whether the tombstone is still there.
    private const float CheckRange = 50f;
    private const float TombstoneReach = 8f;
    private static readonly Color Core = new(0.7f, 0.95f, 1f, 0.75f);
    private static readonly Color Halo = new(0.45f, 0.8f, 1f, 0.18f);

    private static GameObject? _beam;
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
        Column(_beam.transform, 0.35f, Core);
        Column(_beam.transform, 1.4f, Halo);
        _nextCheck = Time.time + 10f;
    }

    private static void Column(Transform parent, float width, Color color)
    {
        GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        UnityEngine.Object.Destroy(column.GetComponent<Collider>());
        column.transform.SetParent(parent, false);
        // A cylinder is 2 units tall around its middle.
        column.transform.localPosition = Vector3.up * (Height / 2f);
        column.transform.localScale = new Vector3(width, Height / 2f, width);
        MeshRenderer renderer = column.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        Shader? shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        if (shader) renderer.material = new Material(shader) { color = color, renderQueue = 3100 };
        else renderer.material.color = color;
    }

    private static void Remove()
    {
        if (_beam) UnityEngine.Object.Destroy(_beam);
        _beam = null;
    }

    // Every frame: gone once the tombstone is, as seen from near it.
    internal static void Update()
    {
        if (!_beam || Time.time < _nextCheck || PatchGuard.IsDisabled(Feature)) return;
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
