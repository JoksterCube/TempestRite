using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The creatures the Ragnarök and Hoard-Bane Rites can't go on without (the bosses; the minibosses and elites), in the
// game that owns each: one that fell out of the ring (through the ground, or out past its edge) is put back on the
// ground within it. One that keeps falling out isn't the players' fault:
// - a Ragnarök boss, an elite, or a miniboss whose shield is down is slain where the players can reach its drops,
//   counting as slain by the local player (and if even that fails, the server is told it was lost, see RagnarokRush and
//   HoardBaneRush, which count it as slain);
// - a miniboss still shielded is told lost to the server, which calls it again, whole and shielded.
internal static class ArenaRescue
{
    private const string Feature = "Arena rescue";
    private const float CheckInterval = 1f;
    // Deeper than this under the ground, it fell through.
    private const float FallDepth = 4f;
    // Without known ground, deeper than this under the water's surface.
    private const float LostDepth = 60f;
    // How far past the edge counts as out (the bubble holds creatures within half a meter).
    private const float EdgeSlack = 3f;
    // How many times it is put back before it is given up on, and how long it has to stay in for those to be forgotten.
    private const int PutBackTries = 3;
    private const float StayedFor = 20f;
    // Put back within this share of the radius.
    private const float BackWithin = 0.5f;
    private const int PointTries = 20;
    // How long a slain one may take to die before the server is told it was lost.
    private const float DieWait = 4f;
    // Enough to kill anything (as the game's own killall).
    private const float KillDamage = 1E+10f;

    private sealed class Tracked
    {
        internal int Tries;
        internal float LastOut;
        internal float GaveUpAt = -1f;
        internal bool Told;
    }

    private static readonly Dictionary<ZDOID, Tracked> Seen = [];
    private static float _nextCheck;

    internal static void ClearSession()
    {
        Seen.Clear();
        _nextCheck = 0f;
    }

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || Time.time < _nextCheck) return;
        _nextCheck = Time.time + CheckInterval;
        try { Check(); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Check()
    {
        WeatherKind kind = IncursionArena.Kind;
        if (kind is not (WeatherKind.Ragnarok or WeatherKind.HoardBane) || !ZNetScene.instance)
        {
            Seen.Clear();
            return;
        }
        Vector3 center = WeatherState.Position(kind);
        float radius = PluginConfig.Radius(kind);
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!character || character.IsDead()) continue;
            ZNetView nview = character.m_nview;
            if (!nview || !nview.IsValid() || !nview.IsOwner() || !Matters(character, kind)) continue;
            ZDOID id = nview.GetZDO().m_uid;
            if (!Seen.TryGetValue(id, out Tracked tracked)) Seen[id] = tracked = new Tracked();
            if (tracked.GaveUpAt >= 0f)
            {
                // Slain, but still standing a while later: the server is told.
                if (!tracked.Told && Time.time - tracked.GaveUpAt >= DieWait) TellLost(character, kind, tracked);
                continue;
            }
            if (!Out(character, center, radius))
            {
                if (tracked.Tries > 0 && Time.time - tracked.LastOut >= StayedFor) tracked.Tries = 0;
                continue;
            }
            tracked.LastOut = Time.time;
            if (++tracked.Tries <= PutBackTries)
            {
                Plugin.ModLogger.LogInfo($"{WeatherKinds.Name(kind)}: {character.name} fell out of the ring; put back ({tracked.Tries} of {PutBackTries}).");
                PutBack(character, center, radius);
                continue;
            }
            GiveUp(character, kind, center, radius, tracked);
        }
    }

    private static bool Matters(Character character, WeatherKind kind) => kind == WeatherKind.Ragnarok
        ? IncursionArena.IsRagnarokBoss(character)
        : HoardBaneArena.RoleOf(character) is HoardBaneArena.EliteRole or HoardBaneArena.BossRole;

    private static bool Out(Character character, Vector3 center, float radius)
    {
        Vector3 at = character.transform.position;
        if (Utils.DistanceXZ(at, center) > radius + EdgeSlack) return true;
        ZoneSystem zones = ZoneSystem.instance;
        if (!zones) return false;
        if (zones.GetSolidHeight(at, out float ground)) return at.y < ground - FallDepth;
        return at.y < zones.m_waterLevel - LostDepth;
    }

    // On the ground somewhere within the ring, near the obliterator.
    private static void PutBack(Character character, Vector3 center, float radius)
    {
        Vector3 to = SafePoint(center, radius);
        character.transform.position = to;
        if (!character.m_body) return;
        character.m_body.position = to;
        character.m_body.linearVelocity = Vector3.zero;
    }

    private static Vector3 SafePoint(Vector3 center, float radius)
    {
        ZoneSystem zones = ZoneSystem.instance;
        for (int i = 0; i < PointTries; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * (radius * BackWithin);
            Vector3 point = center + new Vector3(offset.x, 0f, offset.y);
            if (!zones || !zones.GetSolidHeight(point, out float ground) || ground < zones.m_waterLevel) continue;
            point.y = ground + 1f;
            return point;
        }
        Vector3 beside = center + Vector3.forward * 3f;
        beside.y = (zones && zones.GetSolidHeight(beside, out float height) ? Mathf.Max(height, center.y) : center.y) + 1f;
        return beside;
    }

    private static void GiveUp(Character character, WeatherKind kind, Vector3 center, float radius, Tracked tracked)
    {
        tracked.GaveUpAt = Time.time;
        if (kind == WeatherKind.HoardBane && HoardBaneArena.IsShielded(character))
        {
            Plugin.ModLogger.LogWarning($"Hoard-Bane Rite: {character.name} keeps falling out of the ring while shielded; it is called again.");
            TellLost(character, kind, tracked);
            return;
        }
        Plugin.ModLogger.LogWarning($"{WeatherKinds.Name(kind)}: {character.name} keeps falling out of the ring; it is slain.");
        PutBack(character, center, radius);
        // A sleeping elite would only wake (see HoardBanePatches).
        if (HoardBaneArena.IsDormant(character)) character.m_nview.GetZDO().Set(HoardBaneArena.Dormant, false);
        HitData hit = new() { m_point = character.transform.position };
        hit.m_damage.m_damage = KillDamage;
        Player? player = Player.m_localPlayer;
        if (player && !player!.IsDead() && Utils.DistanceXZ(player.transform.position, center) <= radius) hit.SetAttacker(player);
        character.Damage(hit);
    }

    private static void TellLost(Character character, WeatherKind kind, Tracked tracked)
    {
        tracked.Told = true;
        ZDOID id = character.m_nview.GetZDO().m_uid;
        ZRoutedRpc.instance?.InvokeRoutedRPC(kind == WeatherKind.Ragnarok ? Rpcs.RagnarokLost : Rpcs.HoardBaneLost, id);
    }
}
