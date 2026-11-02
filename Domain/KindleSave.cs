using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The Second-Kindle Rite: the blow that would kill the blessed player doesn't. Their own game takes their damage, so it
// decides: the blow is dropped, they are left with Health Restored Multiplier of their health, foes within Knockback
// Radius are thrown back and staggered by a hit that does no damage (sent to whoever handles each foe, so this works
// with players without the mod too), and the blessing ends. Everyone with the mod sees fire burst from them.
internal static class KindleSave
{
    private const string Feature = "Second-Kindle Rite";
    // About a sledge's push.
    private const float PushForce = 150f;
    private const float PushRise = 0.25f;
    private static readonly List<Character> Near = [];

    internal static void Register(ZRoutedRpc rpc) => rpc.Register<ZDOID>(Rpcs.SecondKindleBurst, RPC_Burst);

    // Whether this hit would kill the local player while they carry the blessing: then it saves them instead and the hit
    // is dropped. Damage: what the hit does once the game's damage taken rate is applied.
    internal static bool Saves(Character character, HitData hit)
    {
        if (PatchGuard.IsDisabled(Feature)) return false;
        try
        {
            if (character != Player.m_localPlayer || character is not Player player || !Blessings.IsActive(WeatherKind.SecondKindle)) return false;
            if (player.IsDead() || player.IsTeleporting() || player.InGodMode() || player.InGhostMode() || player.IsDebugFlying()) return false;
            float damage = hit.GetTotalDamage() * Game.m_localDamgeTakenRate;
            if (damage <= 0.1f || player.GetHealth() - damage > 0f) return false;
            Save(player);
            return true;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return false;
        }
    }

    private static void Save(Player player)
    {
        float share = Mathf.Clamp(PluginConfig.KindleHealthShare.Value, 0.05f, 1f);
        player.SetHealth(Mathf.Max(1f, player.GetMaxHealth() * share));
        Blessings.End(WeatherKind.SecondKindle);
        PlayerMessages.Center(Messages.SecondKindleSaved);
        ThrowBack(player);
        KinshipFx.Kindle(player);
        if (player.m_nview && player.m_nview.IsValid())
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.SecondKindleBurst, player.GetZDOID());
    }

    // Foes around are pushed straight away from the player and staggered, as by a sledge, without being hurt.
    private static void ThrowBack(Player player)
    {
        float radius = Mathf.Max(0f, PluginConfig.KindleKnockbackRadius.Value);
        if (radius <= 0f) return;
        Vector3 center = player.transform.position;
        Near.Clear();
        foreach (Character character in Character.GetAllCharacters())
            if (character && character != player && !character.IsPlayer() && !character.IsDead() && !character.IsTamed()
                && Vector3.Distance(character.transform.position, center) <= radius && BaseAI.IsEnemy(player, character))
                Near.Add(character);
        foreach (Character foe in Near)
        {
            Vector3 away = foe.transform.position - center;
            away.y = 0f;
            Vector3 dir = (away.sqrMagnitude > 0.01f ? away.normalized : foe.transform.forward) + Vector3.up * PushRise;
            HitData hit = new()
            {
                m_point = foe.GetCenterPoint(),
                m_dir = dir.normalized,
                m_pushForce = PushForce,
                m_staggerMultiplier = 100f,
                m_dodgeable = false,
                m_blockable = false,
                m_ignorePVP = true
            };
            foe.Damage(hit);
        }
        Near.Clear();
    }

    // Another player was saved: their fire, as this game sees them.
    private static void RPC_Burst(long sender, ZDOID id)
    {
        if (PatchGuard.IsDisabled(Feature) || !ZNetScene.instance || !ZNet.instance || ZNet.instance.IsDedicated()) return;
        try
        {
            ZDO? zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null || zdo.GetOwner() != sender) return;
            GameObject? instance = ZNetScene.instance.FindInstance(id);
            Player? player = instance ? instance!.GetComponent<Player>() : null;
            if (!player || player == Player.m_localPlayer) return;
            KinshipFx.Kindle(player!);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
