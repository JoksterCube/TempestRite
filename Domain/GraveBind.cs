using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Grave-Bind Rite's beam: after a death under it, a tall beam of light stands over the tombstone, seen from far
// away (it ignores fog), until the tombstone is emptied. Only in the dead player's own game: it is a local object,
// placed only on the local player's death, so no one else sees it. Where it stands and which tombstone it marks are
// saved with the character, so it comes back after logging out. See LightBeam.
internal static class GraveBeacon
{
    private const string Feature = "Grave-Bind beam";
    private const string SaveKey = "TempestRite_GraveBeam";
    private const float Height = 600f;
    private const float Width = 1.6f;
    private const float CheckInterval = 2f;
    // Close enough for the tombstone to be loaded.
    private const float CheckRange = 50f;
    // Checks in a row that must find the tombstone gone (objects keep loading for a while after arriving).
    private const int GoneChecks = 2;
    private static readonly Color Core = new(0.85f, 0.97f, 1f);
    private static readonly Color Halo = new(0.4f, 0.75f, 1f);
    // The tombstone appears at the dead player's middle and falls: the beam's foot is looked for this far below it,
    // and starts a little under the ground so it meets it on a slope.
    private const float GroundSearch = 20f;
    private const float GroundSink = 0.3f;

    private static LightBeam? _beam;
    private static Vector3 _at;
    // The beam's foot was put on the ground in this session.
    private static bool _grounded;
    private static ZDOID _grave = ZDOID.None;
    private static float _nextCheck;
    private static int _goneCount;
    private static Player? _owner;

    internal static void ClearSession()
    {
        Remove();
        _grave = ZDOID.None;
        _owner = null;
    }

    // A new tombstone under the Grave-Bind Rite.
    internal static void Mark(Player player, Vector3 at, ZDOID grave)
    {
        _at = at;
        _grave = grave;
        _grounded = Ground();
        Save(player);
        Place();
    }

    // Moves the beam's foot down onto the floor or ground under it (never up onto a roof). False when nothing solid
    // is loaded under it yet.
    private static bool Ground()
    {
        int mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
        Vector3 from = _at + Vector3.up * 0.5f;
        float best = float.NegativeInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(from, Vector3.down, GroundSearch, mask))
            if (!hit.collider.attachedRigidbody && hit.point.y > best) best = hit.point.y;
        if (float.IsNegativeInfinity(best)) return false;
        _at.y = best - GroundSink;
        return true;
    }

    // Saved as "x,y,z,user,id".
    private static void Save(Player player)
    {
        if (_grave == ZDOID.None)
        {
            player.m_customData.Remove(SaveKey);
            return;
        }
        System.Globalization.CultureInfo invariant = System.Globalization.CultureInfo.InvariantCulture;
        player.m_customData[SaveKey] = string.Join(",",
            _at.x.ToString("0.##", invariant), _at.y.ToString("0.##", invariant), _at.z.ToString("0.##", invariant),
            _grave.UserID.ToString(invariant), _grave.ID.ToString(invariant));
    }

    private static void Load(Player player)
    {
        _grave = ZDOID.None;
        if (!player.m_customData.TryGetValue(SaveKey, out string text)) return;
        System.Globalization.CultureInfo invariant = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.NumberStyles any = System.Globalization.NumberStyles.Any;
        string[] parts = text.Split(',');
        if (parts.Length != 5
            || !float.TryParse(parts[0], any, invariant, out float x)
            || !float.TryParse(parts[1], any, invariant, out float y)
            || !float.TryParse(parts[2], any, invariant, out float z)
            || !long.TryParse(parts[3], any, invariant, out long user)
            || !uint.TryParse(parts[4], any, invariant, out uint id)) return;
        _at = new Vector3(x, y, z);
        _grave = new ZDOID(user, id);
    }

    // The tombstone was emptied: the beam and its record go.
    private static void Forget()
    {
        Remove();
        _grave = ZDOID.None;
        if (Player.m_localPlayer) Save(Player.m_localPlayer);
    }

    private static void Place()
    {
        Remove();
        if (_grave == ZDOID.None || !PluginConfig.GraveBeacon.IsOn()) return;
        _beam = LightBeam.Make("TempestRite_GraveBeam", _at, Height, Width, Core, Halo);
        _goneCount = 0;
        _nextCheck = Time.time + CheckInterval;
    }

    private static void Remove()
    {
        _beam?.Destroy();
        _beam = null;
    }

    // Every frame: picks up the character's saved beam, faces the camera, and goes once the tombstone is gone, as seen
    // from near it with everything there loaded.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Player? player = Player.m_localPlayer;
            if (player != _owner)
            {
                _owner = player;
                Remove();
                _grounded = false;
                if (player) Load(player!);
                Place();
            }
            if (_grave == ZDOID.None) return;
            _beam?.Face();
            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + CheckInterval;

            // The setting turned on or off.
            if ((_beam != null) != PluginConfig.GraveBeacon.IsOn()) Place();
            if (!player || player!.IsTeleporting() || Utils.DistanceXZ(player.transform.position, _at) > CheckRange
                || !ZNetScene.instance || !ZNetScene.instance.IsAreaReady(_at))
            {
                _goneCount = 0;
                return;
            }
            // A beam saved before it was put on the ground (older versions) moves down once the ground is loaded.
            if (!_grounded && Ground())
            {
                _grounded = true;
                Save(player);
                Place();
            }
            if (ZNetScene.instance.FindInstance(_grave) || ZDOMan.instance.GetZDO(_grave) != null)
            {
                _goneCount = 0;
                return;
            }
            if (++_goneCount >= GoneChecks) Forget();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// The Grave-Bind Rite's tombstone: marked as bound (shown when pointing at it), and it carries a note of the gear worn
// at death (weapons put away included). Once it is looted by its owner (Take All, or emptied by hand) that gear is put
// back on as before the death. The note lives on the tombstone, so it lasts through logging out. Items come back out
// of the grave as new objects, so they are matched by what they are and where they sat in the inventory.
internal static class GraveGear
{
    private const string Feature = "Grave-Bind gear";
    private static readonly int BoundKey = "TempestRite_GraveBound".GetStableHashCode();
    private static readonly int GearKey = "TempestRite_GraveGear".GetStableHashCode();

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

        private Worn(string name, int quality, int variant, long crafter, int worldLevel, Vector2i slot)
        {
            Name = name;
            Quality = quality;
            Variant = variant;
            Crafter = crafter;
            WorldLevel = worldLevel;
            Slot = slot;
        }

        internal bool Matches(ItemDrop.ItemData item) =>
            item.m_shared.m_name == Name && item.m_quality == Quality && item.m_variant == Variant &&
            item.m_crafterID == Crafter && item.m_worldLevel == WorldLevel;

        // "name|quality|variant|crafter|worldLevel|x|y"
        internal string Write() => string.Join("|", Name, Quality, Variant, Crafter, WorldLevel, Slot.x, Slot.y);

        internal static Worn? Read(string line)
        {
            string[] parts = line.Split('|');
            if (parts.Length != 7
                || !int.TryParse(parts[1], out int quality) || !int.TryParse(parts[2], out int variant)
                || !long.TryParse(parts[3], out long crafter) || !int.TryParse(parts[4], out int worldLevel)
                || !int.TryParse(parts[5], out int x) || !int.TryParse(parts[6], out int y)) return null;
            return new Worn(parts[0], quality, variant, crafter, worldLevel, new Vector2i(x, y));
        }
    }

    // Noted at death, until the tombstone is made.
    private static readonly List<Worn> Pending = new();

    internal static void ClearSession() => Pending.Clear();

    // Before the game takes the gear off and moves it into the tombstone.
    internal static void Remember(Player player)
    {
        Pending.Clear();
        foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            if (item.m_equipped || item == player.m_hiddenRightItem || item == player.m_hiddenLeftItem)
                Pending.Add(new Worn(item));
    }

    // The tombstone just made: marked as bound, with the gear noted on it.
    internal static void Bind(TombStone tombstone)
    {
        ZDO zdo = tombstone.m_nview.GetZDO();
        zdo.Set(BoundKey, true);
        List<string> lines = new();
        foreach (Worn worn in Pending) lines.Add(worn.Write());
        if (lines.Count > 0) zdo.Set(GearKey, string.Join("\n", lines));
        Pending.Clear();
    }

    internal static bool IsBound(TombStone tombstone) =>
        tombstone.m_nview && tombstone.m_nview.IsValid() && tombstone.m_nview.GetZDO().GetBool(BoundKey);

    // Puts the gear noted on the tombstone back on from the inventory, then takes the note off.
    internal static void Reequip(TombStone tombstone)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Player? player = Player.m_localPlayer;
            if (!player || player!.IsDead() || !IsBound(tombstone) || !tombstone.IsOwner()) return;
            ZDO zdo = tombstone.m_nview.GetZDO();
            string text = zdo.GetString(GearKey);
            if (string.IsNullOrEmpty(text)) return;
            zdo.Set(GearKey, string.Empty);
            if (!PluginConfig.GraveReequip.IsOn()) return;

            List<ItemDrop.ItemData> items = player.GetInventory().GetAllItems();
            HashSet<ItemDrop.ItemData> used = new();
            int equipped = 0;
            foreach (string line in text.Split('\n'))
            {
                if (Worn.Read(line) is not Worn worn) continue;
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
    }
}
