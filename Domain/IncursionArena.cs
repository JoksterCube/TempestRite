using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The bubble of the Crimson and Ragnarök Rites in each player's game (the server runs the fights, see CrimsonRaids and
// RagnarokRush):
// - a shield bubble over its radius, drawn by the game's own shield dome effect;
// - from when it starts, the local player stays on the side they were on: held inside, or kept out. A player who dies
//   inside comes back outside and is kept out. No portal into or out of the bubble works;
// - creatures (not players or tamed ones) stay on the side they were on when it started, or where they spawned: none
//   inside can get out, none outside can get in. Each is held by the game that owns it, which moves it;
// - its area is marked on the map, for everyone;
// - nothing can be built, repaired or torn down within it, and no rite but the Sanctuary Rite performed (see
//   RiteRefused, BuildRefused);
// - when it ends, the creatures it brought are cleared out, dropping nothing (see ClearInside).
internal static class IncursionArena
{
    private const string Feature = "Incursion bubble";
    // How far inside (or outside) the edge players are put back.
    private const float EdgeMargin = 1.5f;
    // Farther out than this, a player held inside was moved away (an admin's teleport): they are let go.
    private const float LetGoBeyond = 30f;
    private const float HeldMessageInterval = 3f;
    // A dome with no hits to flash.
    private const float NoHit = -1000f;
    // How far from the edge creatures are held, on their side.
    private const float CreatureMargin = 0.5f;
    private const float PruneInterval = 10f;
    // Enough to kill anything (as the game's own killall).
    private const float KillDamage = 1E+10f;
    // Close enough to count as the same obliterator.
    internal const float SamePlace = 2f;
    private const float PinInterval = 1f;

    // Saved on each boss the Ragnarök Rite calls.
    internal static readonly int BossMark = "TempestRite_RagnarokBoss".GetStableHashCode();

    private enum Side
    {
        Unknown = 0,
        Inside = 1,
        Outside = 2
    }

    // Which rite and where the local player's side was decided for: a new one decides again.
    private static WeatherKind _sideKind;
    private static Vector3 _sideAt;
    private static Side _side;
    private static Player? _sidePlayer;
    private static float _nextHeldMessage;
    // The last bubble the local player was held in, until its ended message comes.
    private static Vector3? _heldAt;
    // The ended message for the bubble held in now has come already (before the rite's end reached this game).
    private static bool _told;

    // Each creature seen while it goes on: whether it is held inside (or outside).
    private static readonly Dictionary<Character, bool> Sides = [];
    private static WeatherKind _sidesKind;
    private static Vector3 _sidesAt;
    private static float _nextPrune;

    private static GameObject? _domeKeyObject;
    private static ShieldGenerator? _domeKey;
    private static ShieldDomeImageEffect? _domeEffect;
    private static Vector3 _domeAt;
    private static float _domeRadius;
    private static float _drawDistance = -1f;

    private static Minimap.PinData? _areaPin;
    private static Minimap.PinData? _namePin;
    private static WeatherKind _pinKind;
    private static float _nextPin;

    internal static void ClearSession()
    {
        ResetSide();
        _heldAt = null;
        Sides.Clear();
        _nextPrune = 0f;
        _nextHeldMessage = 0f;
        HideDome();
        RemovePins();
    }

    // The rite going on in the Incursion slot (Crimson or Ragnarök), or None.
    internal static WeatherKind Kind => WeatherState.Active(EffectSlot.Incursion);

    private static bool Active => Kind != WeatherKind.None;
    private static Vector3 Center => WeatherState.Position(Kind);
    private static float Radius => PluginConfig.Radius(Kind);

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            UpdateDome();
            Hold();
            HoldCreatures();
            UpdatePins();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // ---- The local player's side ----

    private static void ResetSide()
    {
        if (_side == Side.Inside && !_told) _heldAt = _sideAt;
        _told = false;
        _sideKind = WeatherKind.None;
        _sideAt = Vector3.zero;
        _side = Side.Unknown;
        _sidePlayer = null;
    }

    private static void Hold()
    {
        WeatherKind kind = Kind;
        if (kind == WeatherKind.None)
        {
            ResetSide();
            return;
        }
        Vector3 center = Center;
        if (kind != _sideKind || Utils.DistanceXZ(center, _sideAt) > SamePlace)
        {
            ResetSide();
            _sideKind = kind;
            _sideAt = center;
        }
        Player? player = Player.m_localPlayer;
        if (!player || player!.IsDead()) return;

        float radius = Radius;
        Vector3 position = player.transform.position;
        float distance = Utils.DistanceXZ(position, center);
        if (_side == Side.Unknown)
            // Where they are when it starts (or when they join while it goes on).
            _side = distance < radius ? Side.Inside : Side.Outside;
        else if (_sidePlayer != null && _sidePlayer != player)
            // Died and came back as a new character: from now on outside.
            _side = Side.Outside;
        _sidePlayer = player;

        if (_side == Side.Inside)
        {
            if (distance <= radius - EdgeMargin * 0.5f) return;
            if (distance > radius + LetGoBeyond)
            {
                // Moved far away by other means: let go, and kept out from now on.
                _side = Side.Outside;
                return;
            }
            PutAt(player, center, radius - EdgeMargin, position, Messages.IncursionHeld, kind);
            return;
        }
        if (distance >= radius + EdgeMargin * 0.5f) return;
        PutAt(player, center, radius + EdgeMargin, position, Messages.IncursionKeptOut, kind);
    }

    private static void PutAt(Player player, Vector3 center, float distance, Vector3 position, string message, WeatherKind kind)
    {
        if (player.IsAttached()) player.AttachStop();
        Place(player, center, distance, position);
        if (Time.time < _nextHeldMessage) return;
        _nextHeldMessage = Time.time + HeldMessageInterval;
        player.Message(MessageHud.MessageType.Center, string.Format(message, WeatherKinds.Name(kind)));
    }

    // Moves a character to this far from the center, in the direction it is, keeping its height unless that is under
    // ground (flying creatures stay up).
    private static void Place(Character character, Vector3 center, float distance, Vector3 position)
    {
        Vector3 outward = position - center;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.01f) outward = Vector3.forward;
        Vector3 target = center + outward.normalized * distance;
        target.y = position.y;
        if (ZoneSystem.instance && ZoneSystem.instance.GetSolidHeight(target, out float ground)) target.y = Mathf.Max(target.y, ground);
        character.transform.position = target;
        if (character.m_body)
        {
            character.m_body.position = target;
            character.m_body.linearVelocity = Vector3.zero;
        }
    }

    // ---- Creatures held on their side ----

    private static void HoldCreatures()
    {
        WeatherKind kind = Kind;
        if (kind == WeatherKind.None)
        {
            Sides.Clear();
            return;
        }
        Vector3 center = Center;
        // Performed again elsewhere, or the other rite: sides start over.
        if (kind != _sidesKind || Utils.DistanceXZ(center, _sidesAt) > SamePlace) Sides.Clear();
        _sidesKind = kind;
        _sidesAt = center;
        if (Time.time >= _nextPrune)
        {
            _nextPrune = Time.time + PruneInterval;
            List<Character> gone = [];
            foreach (Character known in Sides.Keys)
                if (!known) gone.Add(known!); // Destroyed by Unity, but still a real key.
            foreach (Character known in gone) Sides.Remove(known);
        }

        float radius = Radius;
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!character || character.IsPlayer() || character.IsTamed() || character.IsDead()) continue;
            ZNetView nview = character.m_nview;
            if (!nview || !nview.IsValid()) continue;
            Vector3 position = character.transform.position;
            float distance = Utils.DistanceXZ(position, center);
            if (!Sides.TryGetValue(character, out bool inside))
            {
                // The Ragnarök Rite's bosses belong inside, wherever they are first seen.
                inside = distance < radius || IsRagnarokBoss(character);
                Sides[character] = inside;
            }
            // Only the game that owns it moves it; the others see where it is.
            if (!nview.IsOwner()) continue;
            if (inside && distance > radius - CreatureMargin) Place(character, center, radius - CreatureMargin, position);
            else if (!inside && distance < radius + CreatureMargin) Place(character, center, radius + CreatureMargin, position);
        }
    }

    // ---- Portals ----

    // Why a portal can't be used now, or null: within the bubble, used by a player inside it, or leading into it.
    internal static string? PortalBlocked(Vector3 portal, Vector3 player, Vector3? exit)
    {
        WeatherKind kind = Kind;
        if (kind == WeatherKind.None) return null;
        string name = WeatherKinds.Name(kind);
        if (WeatherState.Reaches(kind, portal) || WeatherState.Reaches(kind, player))
            return string.Format(Messages.IncursionNoPortal, name);
        if (exit is { } at && WeatherState.Reaches(kind, at, EdgeMargin))
            return string.Format(Messages.IncursionNoPortalIn, name);
        return null;
    }

    // ---- What can't be done while it goes on ----

    // Why a rite can't be performed at an obliterator here now, or null. Only one Crimson or Ragnarök Rite goes on at a
    // time, anywhere in the world; within its bubble only the Sanctuary Rite can be performed (to bail out, the iron has
    // to be at hand, someone outside has to perform it, or the enemies have to destroy the obliterator).
    // Checked by the player pulling the lever, before anything burns, and by the server.
    internal static string? RiteRefused(WeatherKind kind, Vector3 obliterator)
    {
        WeatherKind going = Kind;
        if (going == WeatherKind.None || kind == WeatherKind.Sanctuary) return null;
        string name = WeatherKinds.Name(going);
        if (WeatherState.Reaches(going, obliterator, SamePlace)) return string.Format(Messages.IncursionOnlySanctuary, name);
        if (WeatherKinds.IsIncursion(kind)) return string.Format(Messages.IncursionBusy, name);
        return null;
    }

    // Why nothing can be built, repaired or torn down here now, or null: not within the bubble while it goes on.
    internal static string? BuildRefused(Vector3 at)
    {
        WeatherKind kind = Kind;
        if (kind == WeatherKind.None || !WeatherState.Reaches(kind, at, EdgeMargin)) return null;
        return string.Format(Messages.IncursionNoBuild, WeatherKinds.Name(kind));
    }

    // ---- Clearing out ----

    // When it ends, however it ends, in each game: the hostile creatures it owns still inside die, dropping nothing,
    // and the Ragnarök Rite's bosses vanish (they leave no trophy, nor the key a boss sets as it dies). The server has
    // stopped counting by then.
    internal static void ClearInside(Vector3 center, float radius, string rite)
    {
        float reach = radius + SamePlace;
        List<Character> remaining = [];
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!Hostile(character) || character.IsDead()) continue;
            ZNetView nview = character.m_nview;
            if (!nview || !nview.IsValid() || !nview.IsOwner()) continue;
            if (Utils.DistanceXZ(character.transform.position, center) <= reach || IsRagnarokBoss(character)) remaining.Add(character);
        }
        int vanished = 0;
        foreach (Character character in remaining)
        {
            if (IsRagnarokBoss(character))
            {
                ZNetScene.instance.Destroy(character.gameObject);
                vanished++;
                continue;
            }
            NoDrops(character);
            HitData hit = new() { m_point = character.transform.position };
            hit.m_damage.m_damage = KillDamage;
            character.Damage(hit);
        }
        if (remaining.Count > 0)
            Plugin.ModLogger.LogInfo($"{rite}: {remaining.Count - vanished} creatures left inside died, {vanished} bosses vanished.");
    }

    // A creature killed this way drops nothing: neither on death nor from its ragdoll, which rolls its own loot.
    private static void NoDrops(Character character)
    {
        CharacterDrop drop = character.GetComponent<CharacterDrop>();
        if (!drop) return;
        drop.SetDropsEnabled(false);
        UnityEngine.Object.DestroyImmediate(drop);
    }

    internal static bool IsRagnarokBoss(Character character)
    {
        ZNetView nview = character.m_nview;
        return nview && nview.IsValid() && nview.GetZDO().GetBool(BossMark);
    }

    // Creatures that count as kills and are cleared out: not players, tamed ones, animals, summons or dummies, and never
    // spawners (the game's nests and piles aren't creatures anyway; this keeps modded ones that are safe), so a rite
    // near a base takes none.
    internal static bool Hostile(Character character) =>
        character && !character.IsPlayer() && !character.IsTamed() && HostileKind(character) && !IsSpawner(character);

    // The same for a creature's prefab, as the server sees it.
    private static bool HostileKind(Character character) =>
        character.m_faction is not (Character.Faction.Players or Character.Faction.AnimalsVeg or Character.Faction.PlayerSpawned
            or Character.Faction.TrainingDummy);

    private static bool IsSpawner(Character character) =>
        character.GetComponentInChildren<SpawnArea>(true) || character.GetComponentInChildren<CreatureSpawner>(true);

    // On the server, when it ends: the Ragnarök Rite's bosses are taken away wherever they are, and the hostile creatures
    // inside that no player's game has (everyone left or disconnected) are taken away too. Those a player's game has
    // are cleared there (ClearInside).
    internal static void SweepOnServer(Vector3 center, float radius, string rite)
    {
        if (!ZNet.instance || !ZNet.instance.IsServer() || ZDOMan.instance == null || !ZNetScene.instance) return;
        List<ZDO> found = [];
        Vector2s zone = ZoneSystem.GetZone(center);
        int zones = Mathf.CeilToInt((radius + SamePlace) / ZoneSystem.c_ZoneSize) + 1;
        for (int x = -zones; x <= zones; x++)
            for (int y = -zones; y <= zones; y++)
                ZDOMan.instance.FindObjects(new Vector2s((short)(zone.x + x), (short)(zone.y + y)), found, []);
        long self = ZDOMan.GetSessionID();
        float reach = radius + SamePlace;
        int bosses = 0, creatures = 0;
        foreach (ZDO zdo in found)
        {
            bool boss = zdo.GetBool(BossMark);
            if (!boss)
            {
                if (Utils.DistanceXZ(zdo.GetPosition(), center) > reach || zdo.GetBool(ZDOVars.s_tamed) || Owned(zdo, self)) continue;
                GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
                Character? character = prefab ? prefab.GetComponent<Character>() : null;
                if (!character || character!.IsPlayer() || !HostileKind(character) || IsSpawner(character)) continue;
                creatures++;
            }
            else bosses++;
            zdo.SetOwner(self);
            ZDOMan.instance.DestroyZDO(zdo);
        }
        if (bosses + creatures > 0)
            Plugin.ModLogger.LogInfo($"{rite}: took away {bosses} bosses and {creatures} creatures nobody's game had.");
    }

    // Whether a player's game has the object (and so clears it itself).
    private static bool Owned(ZDO zdo, long self)
    {
        long owner = zdo.GetOwner();
        if (owner == 0L) return false;
        if (owner == self) return !ZNet.instance.IsDedicated();
        return ZNet.instance.GetPeer(owner) != null;
    }

    // ---- The map ----

    // The area and the rite's name on everyone's map, as the game marks raids, tinted (see TintPins).
    private static void UpdatePins()
    {
        if (Time.time < _nextPin) return;
        _nextPin = Time.time + PinInterval;
        Minimap? map = Minimap.instance;
        WeatherKind kind = Kind;
        if (!map || kind == WeatherKind.None)
        {
            RemovePins();
            return;
        }
        if (kind != _pinKind) RemovePins();
        _pinKind = kind;
        Vector3 center = Center;
        _areaPin ??= map!.AddPin(center, Minimap.PinType.EventArea, string.Empty, save: false, isChecked: false);
        // Named as it is made: the map only shows names of pins made with one.
        _namePin ??= map!.AddPin(center, Minimap.PinType.RandomEvent, kind == WeatherKind.Ragnarok ? Messages.RagnarokPin : Messages.CrimsonPin,
            save: false, isChecked: false);
        _areaPin.m_pos = center;
        _areaPin.m_worldSize = Radius * 2f;
        _namePin.m_pos = center;
        _namePin.m_animate = true;
        _namePin.m_doubleSize = true;
    }

    private static void RemovePins()
    {
        Minimap? map = Minimap.instance;
        if (map)
        {
            if (_areaPin != null) map!.RemovePin(_areaPin);
            if (_namePin != null) map!.RemovePin(_namePin);
        }
        _areaPin = null;
        _namePin = null;
        _pinKind = WeatherKind.None;
    }

    // After the map draws its pins (each white): the area turns blood red for the Crimson Rite, ember for the Ragnarök Rite.
    internal static void TintPins()
    {
        if (_areaPin?.m_iconElement == null) return;
        _areaPin.m_iconElement.color = _pinKind == WeatherKind.Ragnarok ? new Color(1f, 0.55f, 0.15f, 0.9f) : new Color(1f, 0.15f, 0.12f, 0.9f);
    }

    // ---- The bubble ----

    // The game's shield dome effect draws a dome for each shield generator it is given. This one is never switched on,
    // so it only stands for the bubble there: no fuel, no blocking, no sound.
    private static void UpdateDome()
    {
        WeatherKind kind = Kind;
        bool shown = kind switch
        {
            WeatherKind.Crimson => PluginConfig.CrimsonBubble.IsOn(),
            WeatherKind.Ragnarok => PluginConfig.RagnarokBubble.IsOn(),
            _ => false
        };
        if (!shown || !Camera.main)
        {
            HideDome();
            return;
        }
        ShieldDomeImageEffect? effect = _domeEffect ? _domeEffect : UnityEngine.Object.FindFirstObjectByType<ShieldDomeImageEffect>();
        if (!effect) return;
        if (effect != _domeEffect)
        {
            HideDome();
            _domeEffect = effect;
        }
        if (!_domeKey)
        {
            _domeKeyObject = new GameObject("TempestRite_IncursionBubble");
            _domeKeyObject.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(_domeKeyObject);
            _domeKey = _domeKeyObject.AddComponent<ShieldGenerator>();
            _domeRadius = 0f;
        }
        Vector3 center = Center;
        float radius = Radius;
        if (_domeRadius == radius && _domeAt == center) return;
        _domeAt = center;
        _domeRadius = radius;
        if (_drawDistance < 0f) _drawDistance = effect!.m_drawDistance;
        // From inside, the far wall is the whole width away.
        effect!.m_drawDistance = Mathf.Max(_drawDistance, radius * 2f + 20f);
        effect.SetShieldData(_domeKey, center, radius, 1f, NoHit);
    }

    private static void HideDome()
    {
        if (_domeEffect && _domeKey) _domeEffect!.RemoveShield(_domeKey);
        if (_domeEffect && _drawDistance >= 0f) _domeEffect!.m_drawDistance = _drawDistance;
        _drawDistance = -1f;
        _domeEffect = null;
        _domeRadius = 0f;
        _domeAt = Vector3.zero;
    }

    // ---- Shared by both rites' messages ----

    internal static bool FromServer(long sender) =>
        ZNet.instance && ZRoutedRpc.instance != null && Player.m_localPlayer &&
        (sender == ZRoutedRpc.instance.GetServerPeerID() || (ZNet.instance.IsServer() && sender == ZRoutedRpc.instance.m_id));

    // Whether the local player is within this rite's bubble.
    internal static bool Here(Vector3 center, WeatherKind kind) =>
        Player.m_localPlayer && Utils.DistanceXZ(Player.m_localPlayer.transform.position, center) <= PluginConfig.Radius(kind) + SamePlace;

    // Whether the local player was held inside the rite that just ended there (also when the message comes after they
    // were let go). Forgets it.
    internal static bool WasInside(Vector3 center)
    {
        bool now = _side == Side.Inside && Utils.DistanceXZ(center, _sideAt) <= SamePlace;
        bool before = _heldAt is { } at && Utils.DistanceXZ(center, at) <= SamePlace;
        _heldAt = null;
        if (now) _told = true;
        return now || before;
    }
}
