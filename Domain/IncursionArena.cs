using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The bubble of the Crimson, Ragnarök and Hoard-Bane Rites in each player's game (the server runs the fights, see
// CrimsonRaids, RagnarokRush and HoardBaneRush):
// - a shield bubble over its radius, drawn by the game's own shield dome effect;
// - from when it starts, the local player stays on the side they were on: held inside, or kept out. A player who dies
//   inside comes back outside and is kept out. A player who logs in or comes back to life within it later is put
//   outside, on firm ground, unless they were inside as it began (disconnected during the fight: see IncursionWatch).
//   No portal into or out of the bubble works;
// - creatures (tamed ones too) stay on the side they were on when it started, or where they spawned: none inside can
//   get out, none outside can get in, and flying ones inside stay under the dome. Each is held by the game that owns it;
// - its edge holds them as a wall would: each physics step, whatever would carry one through it is taken out of its
//   speed, so it stops against the dome and can slide along it (see AtWall);
// - its area is marked on the map, for everyone;
// - nothing can be built, repaired or torn down within it, and no rite but the Sanctuary Rite performed (see
//   RiteRefused, BuildRefused);
// - the obliterator at its middle can't be destroyed, no creature goes after it, and a beam of light rises from it
//   (see ObliteratorBastion);
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
    // Found this far within the bubble while kept out (came back to life or logged in there): put outside on firm ground.
    private const float DeepInside = 5f;
    // Where a player put outside may stand: this far beyond the edge, at most.
    private static readonly float[] OutsideAt = [3f, 6f, 10f, 16f];
    private const float OutsideStep = 15f;
    // The wall stands this far from a character's middle at least (its own radius when bigger).
    private const float WallGap = 0.4f;
    // Farther from the edge than this (plus a step's move), the wall has nothing to do.
    private const float WallReach = 2f;
    // Pushing into the wall this hard (meters per second), or put back this far, tells the player why.
    private const float PressedSpeed = 1f;
    private const float PressedPut = 0.2f;
    private const float PressedFor = 0.25f;
    // The wall stopped being called this long ago (its patch failed): characters are put back the old way.
    private const float WallSilentFor = 0.5f;
    private const string WallFeature = "Incursion wall";

    // Saved on each boss the Ragnarök Rite calls.
    internal static readonly int BossMark = "TempestRite_RagnarokBoss".GetStableHashCode();
    // How much its health and damage are multiplied by (see RagnarokPower).
    internal static readonly int BossHealth = "TempestRite_RagnarokHealth".GetStableHashCode();
    internal static readonly int BossDamage = "TempestRite_RagnarokDamage".GetStableHashCode();
    // How many places it was raised to the strongest boss slain: as many stars, and as much tint (see RagnarokLook).
    internal static readonly int BossStars = "TempestRite_RagnarokStars".GetStableHashCode();

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
    private const float DomeSearchInterval = 1f;
    private static float _nextDomeSearch;
    private static Vector3 _domeAt;
    private static float _domeRadius;
    private static float _drawDistance = -1f;

    // Where the wall stands this frame (none while no rite goes on), and when the local player last pushed into it.
    private static bool _wallOn;
    private static Vector3 _wallCenter;
    private static float _wallRadius;
    private static float _lastWallStep = -1000f;
    private static float _pressedAt = -1000f;

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

    // The rite going on in the Incursion slot (Crimson, Ragnarök or Hoard-Bane), or None.
    internal static WeatherKind Kind => WeatherState.Active(EffectSlot.Incursion);

    // For a point within the Crimson Rite's bubble, how many players are inside it (the game's player scaling counts only
    // them there, see CrimsonDifficultyPatch; the Ragnarök Rite has its own); null outside it.
    internal static int? CrimsonPlayersInside(Vector3 point)
    {
        if (Kind != WeatherKind.Crimson) return null;
        Vector3 center = Center;
        float radius = Radius;
        if (Utils.DistanceXZ(point, center) > radius) return null;
        int count = 0;
        foreach (Player player in Player.GetAllPlayers())
            if (player && Utils.DistanceXZ(player.transform.position, center) <= radius) count++;
        return count;
    }

    private static bool Active => Kind != WeatherKind.None;

    // The rite whose bubble is sealed now, and where: the one going on, or the one whose lever was just pulled (see
    // IncursionPending). Beds, building, teleports, portals and boss altars are refused from the lever on.
    internal static bool Sealed(out WeatherKind kind, out Vector3 center)
    {
        kind = Kind;
        if (kind != WeatherKind.None)
        {
            center = Center;
            return true;
        }
        return IncursionPending.Now(out kind, out center);
    }

    // Whether a point is within the sealed bubble, this much beyond its edge too.
    private static bool SealedReaches(WeatherKind kind, Vector3 center, Vector3 point, float extra = 0f) =>
        Utils.DistanceXZ(point, center) <= PluginConfig.Radius(kind) + extra;
    private static Vector3 Center => WeatherState.Position(Kind);
    private static float Radius => PluginConfig.Radius(Kind);

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            _wallOn = Active;
            if (_wallOn)
            {
                _wallCenter = Center;
                _wallRadius = Radius;
            }
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
        {
            // Where they are when it starts. Joining while it goes on, within it: only those who were inside as it began
            // (and were disconnected) stay inside; anyone else is put outside.
            bool inside = distance < radius;
            if (inside && IncursionWatch.Late(kind))
            {
                bool? member = IncursionWatch.Participant(kind, center);
                // The server says who every few seconds.
                if (member == null) return;
                if (member == false)
                {
                    if (!MoveOut(player, center, radius, kind)) return;
                    _side = Side.Outside;
                    _sidePlayer = player;
                    return;
                }
            }
            _side = inside ? Side.Inside : Side.Outside;
        }
        else if (_sidePlayer != null && _sidePlayer != player)
            // Died and came back as a new character: from now on outside.
            _side = Side.Outside;
        _sidePlayer = player;

        // Walking: the wall holds them (see AtWall), this only tells them why. Riding or sitting: they get off once
        // through the dome. Without the wall (its patch failed): put back the old way.
        bool wall = WallWorks;
        bool pressed = Time.time - _pressedAt < PressedFor;
        if (_side == Side.Inside)
        {
            if (distance > radius + LetGoBeyond)
            {
                // Moved far away by other means: let go, and kept out from now on.
                _side = Side.Outside;
                return;
            }
            if (player.IsAttached() ? distance > radius : !wall && distance > radius - EdgeMargin * 0.5f)
                PutAt(player, center, radius - EdgeMargin, position, Messages.IncursionHeld, kind);
            else if (pressed) Tell(player, Messages.IncursionHeld, kind);
            return;
        }
        // Not walking in at the edge but well within (came back to life there): outside, on firm ground.
        if (distance < radius - DeepInside && MoveOut(player, center, radius, kind)) return;
        if (player.IsAttached() ? distance < radius : !wall && distance < radius + EdgeMargin * 0.5f)
            PutAt(player, center, radius + EdgeMargin, position, Messages.IncursionKeptOut, kind);
        else if (pressed) Tell(player, Messages.IncursionKeptOut, kind);
    }

    // Puts the local player outside the bubble on firm ground (solid, above water, not lava), nearest the way they are
    // from its middle. False when no such ground is loaded there yet.
    private static bool MoveOut(Player player, Vector3 center, float radius, WeatherKind kind)
    {
        ZoneSystem? zones = ZoneSystem.instance;
        if (!zones) return false;
        Vector3 outward = player.transform.position - center;
        outward.y = 0f;
        float facing = outward.sqrMagnitude < 0.01f ? 0f : Mathf.Atan2(outward.z, outward.x) * Mathf.Rad2Deg;
        for (int step = 0; step * OutsideStep <= 180f; step++)
            for (int sign = 1; sign >= -1; sign -= 2)
            {
                if (step == 0 && sign < 0) continue;
                float radians = (facing + sign * step * OutsideStep) * Mathf.Deg2Rad;
                Vector3 way = new(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
                foreach (float beyond in OutsideAt)
                {
                    Vector3 at = center + way * (radius + beyond);
                    if (!zones!.GetSolidHeight(at, out float ground) || ground < zones.m_waterLevel + 0.5f) continue;
                    at.y = ground;
                    if (zones.IsLava(at)) continue;
                    if (player.IsAttached()) player.AttachStop();
                    at.y += 0.5f;
                    player.transform.position = at;
                    if (player.m_body)
                    {
                        player.m_body.position = at;
                        player.m_body.linearVelocity = Vector3.zero;
                    }
                    player.m_maxAirAltitude = at.y;
                    player.Message(MessageHud.MessageType.Center, string.Format(Messages.IncursionMovedOut, WeatherKinds.Name(kind)));
                    return true;
                }
            }
        return false;
    }

    // Whether a point is within the bubble of the Crimson, Ragnarök or Hoard-Bane Rite going on (not down in a dungeon
    // below it): what is inside fights only what is inside (see IncursionFightPatches).
    internal static bool InBubble(Vector3 point)
    {
        WeatherKind kind = Kind;
        return kind != WeatherKind.None && !Character.InInterior(point) && Utils.DistanceXZ(point, WeatherState.Position(kind)) <= PluginConfig.Radius(kind);
    }

    // Why a bed can't be used here now, or null: nobody claims or sleeps in a bed within the bubble.
    internal static string? BedRefused(Vector3 at)
    {
        if (!Sealed(out WeatherKind kind, out Vector3 center) || !SealedReaches(kind, center, at, EdgeMargin)) return null;
        return string.Format(Messages.IncursionNoBed, WeatherKinds.Name(kind));
    }

    private static void PutAt(Player player, Vector3 center, float distance, Vector3 position, string message, WeatherKind kind)
    {
        if (player.IsAttached()) player.AttachStop();
        Place(player, center, distance, position);
        Tell(player, message, kind);
    }

    private static void Tell(Player player, string message, WeatherKind kind)
    {
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
            if (!character || character.IsPlayer() || character.IsDead()) continue;
            ZNetView nview = character.m_nview;
            if (!nview || !nview.IsValid()) continue;
            Vector3 position = character.transform.position;
            float distance = Utils.DistanceXZ(position, center);
            if (!Sides.TryGetValue(character, out bool inside))
            {
                // What the Ragnarök and Hoard-Bane Rites bring belongs inside, wherever it is first seen; any other boss
                // belongs outside, wherever it rose.
                inside = IsRiteCreature(character) || (distance < radius && !character.IsBoss());
                Sides[character] = inside;
            }
            // Only the game that owns it moves it; the others see where it is. The wall holds it (see AtWall); this only
            // puts it back when the wall's patch failed.
            if (!nview.IsOwner() || WallWorks) continue;
            if (inside && distance > radius - CreatureMargin) Place(character, center, radius - CreatureMargin, position);
            else if (!inside && distance < radius + CreatureMargin) Place(character, center, radius + CreatureMargin, position);
            else if (inside && character.IsFlying()) KeepUnderDome(character, center, radius - CreatureMargin, character.transform.position);
        }
    }

    // A flying creature inside stays under the dome (a sphere around the obliterator), not just within its circle: one
    // flying out over it is brought back down onto it, never into the ground.
    private static void KeepUnderDome(Character character, Vector3 center, float radius, Vector3 position)
    {
        Vector3 outward = position - center;
        if (outward.y <= 0f || outward.sqrMagnitude <= radius * radius) return;
        Vector3 target = center + outward.normalized * radius;
        if (ZoneSystem.instance && ZoneSystem.instance.GetSolidHeight(target, out float ground) && target.y < ground + 1f) return;
        character.transform.position = target;
        if (character.m_body)
        {
            character.m_body.position = target;
            character.m_body.linearVelocity = Vector3.zero;
        }
    }

    // ---- The wall ----

    private static bool WallWorks => Time.time - _lastWallStep < WallSilentFor;

    // Each physics step of a character this game owns, right after the game set its speed for the step (see
    // IncursionWallPatch): the local player and the creatures stay on their side as against a wall. Whatever part of
    // the speed would carry one through the edge is taken away (it keeps the rest, so it slides along), and one already
    // through (pushed, knocked back) is put back against it.
    internal static void AtWall(Character character, float dt)
    {
        _lastWallStep = Time.time;
        if (!_wallOn || PatchGuard.IsDisabled(WallFeature)) return;
        try
        {
            Rigidbody body = character.m_body;
            if (!body || dt <= 0f) return;
            Vector3 position = body.position;
            Vector3 outward = position - _wallCenter;
            outward.y = 0f;
            float distance = outward.magnitude;
            float gap = Mathf.Max(character.GetRadius(), WallGap);
            Vector3 velocity = body.linearVelocity;
            // Far from the edge, also after this step: nothing to hold.
            if (Mathf.Abs(distance - _wallRadius) > gap + WallReach + velocity.magnitude * dt) return;

            bool inside;
            bool local = false;
            if (character.IsPlayer())
            {
                if (character != Player.m_localPlayer || character != _sidePlayer || _side == Side.Unknown ||
                    character.IsAttached() || character.IsDead()) return;
                inside = _side == Side.Inside;
                local = true;
            }
            else if (character.IsDead() || !Sides.TryGetValue(character, out inside)) return;

            bool pressed;
            // A flying one inside, above the obliterator: held under the dome, a sphere.
            if (inside && character.IsFlying() && position.y > _wallCenter.y)
            {
                Vector3 up = position - _wallCenter;
                float reach = up.magnitude;
                if (reach < 0.01f) return;
                pressed = Stop(body, up / reach, reach, _wallRadius - gap, true, dt, true);
            }
            else
            {
                Vector3 normal = distance < 0.01f ? Vector3.forward : outward / distance;
                pressed = Stop(body, normal, distance, inside ? _wallRadius - gap : _wallRadius + gap, inside, dt, false);
            }
            if (local && pressed) _pressedAt = Time.time;
        }
        catch (Exception exception) { PatchGuard.Fail(WallFeature, exception); }
    }

    // Holds a body this far from the middle along the normal (within it, or beyond it), as a wall there would. Whether
    // it pushed into the wall hard enough to be told.
    private static bool Stop(Rigidbody body, Vector3 normal, float distance, float limit, bool within, float dt, bool flying)
    {
        // +1 when the wall is outward of it, -1 when inward.
        float sign = within ? 1f : -1f;
        float past = sign * (distance - limit);
        bool pressed = false;
        if (past > 0f)
        {
            Vector3 target = body.position - normal * (sign * past);
            ZoneSystem zones = ZoneSystem.instance;
            if (zones && zones.GetSolidHeight(target, out float ground))
            {
                // Never into the ground: a flyer that would be brought down into it is left be.
                if (flying && target.y < ground + 1f) return false;
                target.y = Mathf.Max(target.y, ground);
            }
            body.position = target;
            // Far: moved at once, not in the next step.
            if (past > EdgeMargin) body.transform.position = target;
            pressed = past > PressedPut;
            past = 0f;
        }
        Vector3 velocity = body.linearVelocity;
        float toward = sign * Vector3.Dot(velocity, normal);
        // As fast as it may go this step: right up to the wall.
        float allowed = -past / dt;
        if (toward <= allowed) return pressed;
        body.linearVelocity = velocity - normal * (sign * (toward - allowed));
        return pressed || toward - allowed > PressedSpeed;
    }

    // ---- Portals ----

    // Why a portal can't be used now, or null: within the bubble, used by a player inside it, or leading into it.
    internal static string? PortalBlocked(Vector3 portal, Vector3 player, Vector3? exit)
    {
        if (!Sealed(out WeatherKind kind, out Vector3 center)) return null;
        string name = WeatherKinds.Name(kind);
        if (SealedReaches(kind, center, portal) || SealedReaches(kind, center, player))
            return string.Format(Messages.IncursionNoPortal, name);
        if (exit is { } at && SealedReaches(kind, center, at, EdgeMargin))
            return string.Format(Messages.IncursionNoPortalIn, name);
        return null;
    }

    // ---- Being carried away ----

    // Why the local player can't be carried from one point to another now, or null: nothing carries anyone into, out of
    // or within the bubble (portals, dungeon doors, other mods), but they can still come up out of a dungeon below it.
    internal static string? TeleportBlocked(Vector3 from, Vector3 to)
    {
        if (!Sealed(out WeatherKind kind, out Vector3 center)) return null;
        bool fromIn = SealedReaches(kind, center, from, EdgeMargin);
        bool toIn = SealedReaches(kind, center, to, EdgeMargin);
        if (!fromIn && !toIn) return null;
        if (Character.InInterior(from) && !Character.InInterior(to)) return null;
        string name = WeatherKinds.Name(kind);
        if (Character.InInterior(to)) return string.Format(Messages.IncursionNoDungeon, name);
        return string.Format(fromIn ? Messages.IncursionNoTeleport : Messages.IncursionNoTeleportIn, name);
    }

    // ---- What can't be done while it goes on ----

    // Why a rite can't be performed at an obliterator here now, or null. Only one Crimson or Ragnarök Rite goes on at a
    // time, anywhere in the world; within its bubble only the Sanctuary Rite can be performed, and only at the rite's own
    // obliterator (to bail out, the iron has to be at hand, or someone outside has to perform it).
    // Checked by the player pulling the lever, before anything burns, and by the server.
    internal static string? RiteRefused(WeatherKind kind, Vector3 obliterator)
    {
        if (OtherObliterator(obliterator) is { } other) return other;
        WeatherKind going = Kind;
        if (kind == WeatherKind.Sanctuary)
        {
            // Ends Fights Only From Inside: from anywhere else it would only end the fight (raids are held off meanwhile).
            if (going != WeatherKind.None && PluginConfig.SanctuaryOnlyInside.IsOn() && !WeatherState.Reaches(going, obliterator, SamePlace))
                return string.Format(Messages.SanctuaryOnlyInside, WeatherKinds.Name(going));
            return null;
        }
        if (going == WeatherKind.None)
        {
            // Another one's lever was just pulled: it stands in the way of any other (not of itself, nor of the rest burnt
            // with it).
            if (WeatherKinds.IsIncursion(kind) && IncursionPending.Now(out WeatherKind pending, out _) && !IncursionPending.Is(kind, obliterator))
                return string.Format(Messages.IncursionBusy, WeatherKinds.Name(pending));
            return null;
        }
        string name = WeatherKinds.Name(going);
        if (WeatherState.Reaches(going, obliterator, SamePlace)) return string.Format(Messages.IncursionOnlySanctuary, name);
        if (WeatherKinds.IsIncursion(kind)) return string.Format(Messages.IncursionBusy, name);
        return null;
    }

    // Why an obliterator can't be used at all now, or null: within the sealed bubble only the rite's own obliterator
    // works. Any other there burns nothing, not even for the Sanctuary Rite (checked at the lever, see LeverPulled).
    internal static string? OtherObliterator(Vector3 at)
    {
        if (!Sealed(out WeatherKind kind, out Vector3 center) || !SealedReaches(kind, center, at, EdgeMargin)) return null;
        if (Utils.DistanceXZ(at, center) <= SamePlace) return null;
        return string.Format(Messages.IncursionOtherObliterator, WeatherKinds.Name(kind));
    }

    // RiteRefused, and for the Crimson, Ragnarök and Hoard-Bane Rites also where it would be (see IncursionSite).
    internal static string? RiteRefusedHere(WeatherKind kind, Vector3 obliterator) =>
        RiteRefused(kind, obliterator) ?? (WeatherKinds.IsIncursion(kind) ? IncursionSite.Refused(kind, obliterator) : null);

    // Why nothing can be built, repaired or torn down here now, or null: not within the bubble while it goes on.
    internal static string? BuildRefused(Vector3 at)
    {
        if (!Sealed(out WeatherKind kind, out Vector3 center) || !SealedReaches(kind, center, at, EdgeMargin)) return null;
        return string.Format(Messages.IncursionNoBuild, WeatherKinds.Name(kind));
    }

    // ---- Clearing out ----

    // When it ends, however it ends, in each game: the hostile creatures it owns still inside die, dropping nothing,
    // and what the Ragnarök and Hoard-Bane Rites brought vanishes (leaving no trophy, nor the key a boss sets as it dies).
    // The server has stopped counting by then.
    internal static void ClearInside(Vector3 center, float radius, string rite)
    {
        // Those of an earlier clearing are long dead (their death animation is over).
        Cleared.Clear();
        float reach = radius + SamePlace;
        List<Character> remaining = [];
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!character || character.IsDead() || (!Hostile(character) && !IsRiteCreature(character))) continue;
            ZNetView nview = character.m_nview;
            if (!nview || !nview.IsValid() || !nview.IsOwner()) continue;
            if (IsRiteCreature(character)) remaining.Add(character);
            // Those down in a dungeon below aren't in the fight.
            else if (Utils.DistanceXZ(character.transform.position, center) <= reach && !Character.InInterior(character.transform)) remaining.Add(character);
        }
        int vanished = 0;
        foreach (Character character in remaining)
        {
            if (IsRiteCreature(character))
            {
                ZNetScene.instance.Destroy(character.gameObject);
                vanished++;
                continue;
            }
            NoDrops(character);
            Cleared.Add(character.m_nview.GetZDO().m_uid);
            HitData hit = new() { m_point = character.transform.position };
            hit.m_damage.m_damage = KillDamage;
            character.Damage(hit);
        }
        if (remaining.Count > 0)
            Plugin.ModLogger.LogInfo($"{rite}: {remaining.Count - vanished} creatures left inside died, {vanished} bosses vanished.");
    }

    // Creatures killed by ClearInside: they don't count as kills (they may die a little later, after a death animation).
    private static readonly HashSet<ZDOID> Cleared = [];

    internal static bool WasCleared(Character character)
    {
        ZNetView nview = character.m_nview;
        return Cleared.Count > 0 && nview && nview.IsValid() && Cleared.Contains(nview.GetZDO().m_uid);
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

    // A Ragnarök Rite boss, or anything the Hoard-Bane Rite brought.
    internal static bool IsRiteCreature(Character character) => IsRagnarokBoss(character) || HoardBaneArena.RoleOf(character) != 0;

    // Creatures that count as kills and are cleared out: not players, tamed ones, animals, summons or dummies, and never
    // spawners (the game's nests and piles aren't creatures anyway; this keeps modded ones that are safe), so a rite
    // near a base takes none.
    internal static bool Hostile(Character character) =>
        character && !character.IsPlayer() && !character.IsTamed() && HostileKind(character) && !IsSpawner(character);

    // The same for a creature's prefab, as the server sees it.
    private static bool HostileKind(Character character) =>
        character.m_faction is not (Character.Faction.Players or Character.Faction.AnimalsVeg or Character.Faction.PlayerSpawned
            or Character.Faction.TrainingDummy);

    // A prefab whose creatures count as hostile and are cleared out (bosses aside), as the server sees it.
    internal static bool HostilePrefab(Character prefab) =>
        !prefab.IsPlayer() && HostileKind(prefab) && !IsSpawner(prefab) && !prefab.IsBoss();

    private static bool IsSpawner(Character character) =>
        character.GetComponentInChildren<SpawnArea>(true) || character.GetComponentInChildren<CreatureSpawner>(true);

    // On the server, when it ends: the Ragnarök Rite's bosses are taken away wherever they are, and the hostile creatures
    // inside that no player's game has (everyone left or disconnected) are taken away too. Those a player's game has
    // are cleared there (ClearInside).
    internal static void SweepOnServer(Vector3 center, float radius, string rite)
    {
        if (!ZNet.instance || !ZNet.instance.IsServer() || ZDOMan.instance == null || !ZNetScene.instance) return;
        List<ZDO> found = [];
        FindAround(center, radius + SamePlace, found);
        long self = ZDOMan.GetSessionID();
        float reach = radius + SamePlace;
        int bosses = 0, creatures = 0;
        foreach (ZDO zdo in found)
        {
            bool boss = zdo.GetBool(BossMark) || zdo.GetInt(HoardBaneArena.Role) != 0;
            if (!boss)
            {
                if (Utils.DistanceXZ(zdo.GetPosition(), center) > reach || Character.InInterior(zdo.GetPosition()) ||
                    zdo.GetBool(ZDOVars.s_tamed) || Owned(zdo, self)) continue;
                GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
                Character? character = prefab ? prefab.GetComponent<Character>() : null;
                if (!character || !HostilePrefab(character!)) continue;
                creatures++;
            }
            else bosses++;
            zdo.SetOwner(self);
            ZDOMan.instance.DestroyZDO(zdo);
        }
        if (bosses + creatures > 0)
            Plugin.ModLogger.LogInfo($"{rite}: took away {bosses} bosses and {creatures} creatures nobody's game had.");
    }

    // The world's objects in the zones reaching this far around a point.
    internal static void FindAround(Vector3 center, float reach, List<ZDO> found)
    {
        Vector2s zone = ZoneSystem.GetZone(center);
        int zones = Mathf.CeilToInt(reach / ZoneSystem.c_ZoneSize) + 1;
        for (int x = -zones; x <= zones; x++)
            for (int y = -zones; y <= zones; y++)
                ZDOMan.instance.FindObjects(new Vector2s((short)(zone.x + x), (short)(zone.y + y)), found, []);
    }

    // ---- Starting ----

    // As it starts, in each game (the server sweeps too, see SweepOnServer): the creatures already within the bubble die,
    // dropping nothing, so the fight is only what the rite brings: no free kills of a spawner's creatures, nor a rite
    // made to slay what the players couldn't. Spawners within it stay still until it ends (see IncursionSpawnPatches).
    internal static void Begun(WeatherKind kind, Vector3 center)
    {
        if (!WeatherKinds.IsIncursion(kind) || !ZNetScene.instance) return;
        ClearInside(center, PluginConfig.Radius(kind), WeatherKinds.Name(kind));
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
        string name = kind switch
        {
            WeatherKind.Ragnarok => Messages.RagnarokPin,
            WeatherKind.HoardBane => Messages.HoardBanePin,
            _ => Messages.CrimsonPin
        };
        _namePin ??= map!.AddPin(center, Minimap.PinType.RandomEvent, name, save: false, isChecked: false);
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

    // After the map draws its pins (each white): the area turns blood red for the Crimson Rite, ember for the Ragnarök Rite,
    // brass for the Hoard-Bane Rite.
    internal static void TintPins()
    {
        if (_areaPin?.m_iconElement == null) return;
        Color tint = Tint(_pinKind);
        tint.a = 0.9f;
        _areaPin.m_iconElement.color = tint;
    }

    // Each incursion rite's color: blood red for the Crimson Rite, ember for the Ragnarök Rite, brass for the Hoard-Bane Rite.
    internal static Color Tint(WeatherKind kind) => kind switch
    {
        WeatherKind.Ragnarok => new Color(1f, 0.55f, 0.15f),
        WeatherKind.HoardBane => new Color(1f, 0.78f, 0.3f),
        _ => new Color(1f, 0.15f, 0.12f)
    };

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
            WeatherKind.HoardBane => PluginConfig.HoardBaneBubble.IsOn(),
            _ => false
        };
        if (!shown || !Camera.main)
        {
            HideDome();
            return;
        }
        ShieldDomeImageEffect? effect = _domeEffect;
        if (!effect)
        {
            // A search of the whole scene: not every frame while there is none.
            if (Time.time < _nextDomeSearch) return;
            _nextDomeSearch = Time.time + DomeSearchInterval;
            effect = UnityEngine.Object.FindFirstObjectByType<ShieldDomeImageEffect>();
            if (!effect) return;
        }
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

    // To everyone not there: how it went, won or lost.
    internal static void TellFar(WeatherKind kind, bool won) =>
        PlayerMessages.Warning(string.Format(won ? Messages.IncursionWonFar : Messages.IncursionLostFar, WeatherKinds.Colored(kind)));

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
