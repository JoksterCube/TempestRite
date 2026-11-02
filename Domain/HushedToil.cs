using System;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Hush-Felling Rite: the wild ignores a blessed player's toil.
// - Trees, logs, rocks, ore veins and bushes being hit or broken make no noise at the blessed player (the game gives
//   that noise to the closest player, from the game that owns the object, so it is checked on them as written on their
//   character). That covers logs crashing into things as they fall too. Spawners (nests, body piles...) are not toil.
// - The blessed player's own swing makes no noise when it is with a tool (an axe, pickaxe, scythe...) and hits no
//   creature; a tool's swing makes its usual noise for a while after a fight.
// - While gathering (a few seconds after hitting a tree, log, rock or bush, or picking something, unless they fought
//   since), wild creatures (not bosses or tamed) see the blessed player only from 1/Notice Multiplier as far. Whether
//   they gather is written on their character, as creatures are run by the game that owns them.
internal static class HushedToil
{
    private const string Feature = "Hush-Felling Rite";
    // Gathering lasts this long after the last hit or pick.
    private const float GatherSeconds = 8f;
    // Tool swings make their usual noise for this long after a fight.
    private const float FightSeconds = 6f;
    private static readonly int GatheringKey = "TempestRite_Gathering".GetStableHashCode();

    private static float _lastGather = float.NegativeInfinity;
    private static float _lastFight = float.NegativeInfinity;

    // Set while the game works out a hit on a tree, log, rock or bush: noise it gives a blessed player is dropped.
    internal static int ToilDepth;

    // The local player's attack going on, while the game starts it (Start: its swing noise) or works out its hits (Hit).
    internal static Attack? Starting;
    internal static Attack? Hitting;
    // Whether the attack being worked out has hit a creature.
    internal static bool HitCreature;

    internal static void ClearSession()
    {
        _lastGather = float.NegativeInfinity;
        _lastFight = float.NegativeInfinity;
        ToilDepth = 0;
        Starting = null;
        Hitting = null;
        HitCreature = false;
    }

    internal static bool Active => Blessings.IsActive(WeatherKind.HushFelling);

    // Whether a character carries the blessing (other players: as written on them).
    internal static bool Hushed(Character? character) => Blessings.Has(character, WeatherKind.HushFelling);

    internal static void Gathered() => _lastGather = Time.time;

    internal static void Fought() => _lastFight = Time.time;

    private static bool Gathering => Time.time - _lastGather < GatherSeconds && _lastGather > _lastFight;

    private static bool RecentlyFought => Time.time - _lastFight < FightSeconds;

    // Whether a player carries the blessing and is gathering: the local player as kept here, others as written on them.
    internal static bool GatheringHushed(Player player)
    {
        if (player == Player.m_localPlayer) return Active && Gathering;
        if (!Hushed(player)) return false;
        ZDO? zdo = player.m_nview ? player.m_nview.GetZDO() : null;
        return zdo != null && zdo.GetBool(GatheringKey);
    }

    // A creature that ignores the blessed while they gather: not a player, boss or tamed one.
    internal static bool Wild(Transform? creature)
    {
        if (!creature) return false;
        Character? character = creature!.GetComponent<Character>();
        return character && !character!.IsPlayer() && !character.IsBoss() && !character.IsTamed();
    }

    // How far a wild creature sees a gathering blessed player.
    internal static float SightRange(float range) => range / Mathf.Max(1f, PluginConfig.HushNoticeMultiplier.Value);

    // Whether noise the local player makes now is dropped.
    internal static bool Silent()
    {
        if (!Active) return false;
        if (Hitting != null) return !HitCreature && IsTool(Hitting);
        if (Starting != null) return IsTool(Starting) && !RecentlyFought;
        return false;
    }

    // An axe, pickaxe or anything else that chops or mines, or a scythe's harvesting swing.
    private static bool IsTool(Attack attack)
    {
        if (attack.m_harvest) return true;
        ItemDrop.ItemData? weapon = attack.m_weapon;
        if (weapon == null) return false;
        ItemDrop.ItemData.SharedData shared = weapon.m_shared;
        return shared.m_damages.m_chop > 0f || shared.m_damages.m_pickaxe > 0f
            || shared.m_skillType is Skills.SkillType.WoodCutting or Skills.SkillType.Pickaxes;
    }

    // Every frame: writes on the local player whether they gather under the blessing.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Player? player = Player.m_localPlayer;
            if (!player || !player!.m_nview || !player.m_nview.IsValid() || !player.m_nview.IsOwner()) return;
            bool gathering = !player.IsDead() && Active && Gathering;
            ZDO zdo = player.m_nview.GetZDO();
            if (zdo.GetBool(GatheringKey) != gathering) zdo.Set(GatheringKey, gathering);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
