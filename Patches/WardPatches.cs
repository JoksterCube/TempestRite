using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Patches;

// The Sovereign Ward Rite, in the game that owns each thing:
// - player-built pieces within the radius lose no health to anything (taking them down with the hammer isn't damage).
//   A hit on one shows a 0, and (for everyone near) the piece glows ward-blue for a moment, like an energy shield, with
//   the ward's sound where it was hit; damage that doesn't come as a hit (impacts, burning...) is stopped too, which a
//   ward doesn't do;
// - projectiles not shot by players or their tamed creatures crumble: at once when they fly in, a tenth of a second
//   after being shot within it;
// - with Sanctuary Mode on, players within it can't be hurt.
internal static class WardPatches
{
    private const string Feature = "Sovereign Ward Rite";
    // A piece glows again at most this often, however often it is hit.
    private const float GlowInterval = 0.3f;
    // The glow comes up this fast, then fades over the rest.
    private const float GlowRise = 0.06f;
    private const float GlowFade = 0.6f;
    // The ward bubble's blue.
    private static readonly Color WardBlue = new(0.3f, 0.6f, 1f);
    private const float GlowStrength = 1.5f;
    // How far from a hit players see and hear it.
    private const float GlowSeen = 80f;
    // How long a projectile shot within the area flies before it crumbles.
    private const float CrumbleAfter = 0.1f;
    // Projectiles this far outside the area are watched, so one flying in is known to have come from outside.
    private const float WatchMargin = 20f;
    private const int PruneAbove = 128;

    private static EffectList? _flash;
    private static bool _flashLooked;
    // When each piece (this game being its owner) last glowed.
    private static readonly Dictionary<ZDOID, float> LastGlow = [];
    private static readonly List<ZDOID> Faded = [];
    // The glow going on, on each piece (in every game).
    private static readonly Dictionary<WearNTear, Coroutine> Glowing = [];
    private static readonly List<WearNTear> GlowGone = [];
    // Each watched projectile: when it was shot within the area, or -1 once it has been outside.
    private static readonly Dictionary<Projectile, float> Watched = [];
    private static readonly List<Projectile> Gone = [];

    private static bool Warded(Vector3 position) =>
        !PatchGuard.IsDisabled(Feature) && WeatherState.Reaches(WeatherKind.SovereignWard, position);

    internal static void Register(ZRoutedRpc rpc) => rpc.Register<ZDOID, Vector3>(Rpcs.WardFlash, RPC_Glow);

    internal static void ClearSession()
    {
        Watched.Clear();
        LastGlow.Clear();
        Glowing.Clear();
    }

    private static bool Protects(WearNTear piece)
    {
        if (!Warded(piece.transform.position)) return false;
        Piece? built = piece.GetComponent<Piece>();
        return built && built!.IsPlacedByPlayer();
    }

    // A hit, as the game takes it, but with nothing taken off.
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.RPC_Damage))]
    private static class Hits
    {
        private static bool Prefix(WearNTear __instance, HitData hit)
        {
            try
            {
                ZNetView nview = __instance.m_nview;
                if (!nview || !nview.IsValid() || !nview.IsOwner()) return true;
                if (nview.GetZDO().GetFloat(ZDOVars.s_health, __instance.m_health) <= 0f || !Protects(__instance)) return true;
                hit.ApplyResistance(__instance.m_damages, out HitData.DamageModifier modifier);
                float damage = hit.GetTotalDamage();
                DamageText.instance.ShowText(modifier, hit.m_point, 0f);
                if (damage > 0f && __instance.m_triggerPrivateArea && hit.GetAttacker()) Glow(nview.GetZDO().m_uid, hit.m_point);
                return false;
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    // Everything else that would take health off (impacts, burning...).
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.ApplyDamage))]
    private static class Pieces
    {
        private static bool Prefix(WearNTear __instance, ref bool __result)
        {
            try
            {
                if (!Protects(__instance)) return true;
                __result = false;
                return false;
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    private static void Glow(ZDOID piece, Vector3 point)
    {
        if (ZRoutedRpc.instance == null) return;
        float now = Time.time;
        if (LastGlow.TryGetValue(piece, out float last) && now - last < GlowInterval) return;
        if (LastGlow.Count >= PruneAbove)
        {
            foreach (KeyValuePair<ZDOID, float> pair in LastGlow)
                if (now - pair.Value >= GlowInterval) Faded.Add(pair.Key);
            foreach (ZDOID gone in Faded) LastGlow.Remove(gone);
            Faded.Clear();
        }
        LastGlow[piece] = now;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.WardFlash, piece, point);
    }

    // In every game with a player near it: the piece glows ward-blue and the ward's sound plays where it was hit.
    private static void RPC_Glow(long sender, ZDOID id, Vector3 point)
    {
        try
        {
            Player? player = Player.m_localPlayer;
            if (!player || Vector3.Distance(player!.transform.position, point) > GlowSeen) return;
            Sound(point);
            GameObject found = ZNetScene.instance ? ZNetScene.instance.FindInstance(id) : null!;
            if (!found || found.GetComponent<WearNTear>() is not { } piece || !piece) return;
            if (Glowing.TryGetValue(piece, out Coroutine running) && running != null) piece.StopCoroutine(running);
            if (Glowing.Count >= PruneAbove)
            {
                foreach (WearNTear glowing in Glowing.Keys)
                    if (!glowing) GlowGone.Add(glowing!); // Destroyed by Unity, but still a real key.
                foreach (WearNTear gone in GlowGone) Glowing.Remove(gone);
                GlowGone.Clear();
            }
            Glowing[piece] = piece.StartCoroutine(Shimmer(piece));
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // A quick ward-blue glow over the piece's surface, fading out.
    private static IEnumerator Shimmer(WearNTear piece)
    {
        GameObject target = piece.gameObject;
        float start = Time.time;
        while (true)
        {
            float t = Time.time - start;
            if (t >= GlowRise + GlowFade) break;
            float k = t < GlowRise ? t / GlowRise : 1f - (t - GlowRise) / GlowFade;
            k *= k;
            MaterialMan.instance.SetValue(target, ShaderProps._EmissionColor, WardBlue * (GlowStrength * k));
            MaterialMan.instance.SetValue(target, ShaderProps._Color, Color.Lerp(Color.white, WardBlue, 0.6f * k));
            yield return null;
        }
        MaterialMan.instance.ResetValue(target, ShaderProps._Color);
        MaterialMan.instance.ResetValue(target, ShaderProps._EmissionColor);
        Glowing.Remove(piece);
    }

    // The ward's flash with nothing of it shown: only its sound.
    private static void Sound(Vector3 point)
    {
        EffectList? flash = WardFlash();
        if (flash == null) return;
        foreach (GameObject made in flash.Create(point, Quaternion.identity))
        {
            if (!made) continue;
            foreach (Renderer renderer in made.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (Light light in made.GetComponentsInChildren<Light>(true)) light.enabled = false;
            if (!made.GetComponentInChildren<ZSFX>(true)) UnityEngine.Object.Destroy(made);
        }
    }

    // The ward's own flash.
    private static EffectList? WardFlash()
    {
        if (_flashLooked) return _flash;
        if (!ZNetScene.instance) return null;
        _flashLooked = true;
        GameObject ward = ZNetScene.instance.GetPrefab("guard_stone");
        if (ward && ward.GetComponent<PrivateArea>() is { } area && area && area.m_flashEffect != null) _flash = area.m_flashEffect;
        else Plugin.ModLogger.LogWarning($"{Feature}: found no ward to take its sound from; hits on warded pieces will be silent.");
        return _flash;
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.FixedUpdate))]
    private static class Projectiles
    {
        private static bool Prefix(Projectile __instance)
        {
            try
            {
                if (__instance.m_didHit || !__instance.m_nview || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()) return true;
                if (PatchGuard.IsDisabled(Feature)) return true;
                Character? owner = __instance.m_owner;
                if (owner && (owner is Player || owner!.IsTamed())) return true;
                Vector3 position = __instance.transform.position;
                bool known = Watched.TryGetValue(__instance, out float shotInside);
                if (!Warded(position))
                {
                    if (known) Watched[__instance] = -1f;
                    else if (WeatherState.Reaches(WeatherKind.SovereignWard, position, WatchMargin)) Watch(__instance, -1f);
                    return true;
                }
                if (!known)
                {
                    // First seen within it: shot from inside.
                    Watch(__instance, Time.time);
                    return true;
                }
                if (shotInside >= 0f && Time.time - shotInside < CrumbleAfter) return true;
                Watched.Remove(__instance);
                __instance.m_didHit = true;
                __instance.m_hitEffects.Create(position, Quaternion.identity);
                ZNetScene.instance.Destroy(__instance.gameObject);
                return false;
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }

    private static void Watch(Projectile projectile, float shotInside)
    {
        if (Watched.Count >= PruneAbove)
        {
            foreach (Projectile watched in Watched.Keys)
                if (!watched || watched.m_didHit) Gone.Add(watched!);
            foreach (Projectile gone in Gone) Watched.Remove(gone);
            Gone.Clear();
        }
        Watched[projectile] = shotInside;
    }

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    private static class SafePlayers
    {
        private static bool Prefix(Character __instance)
        {
            try
            {
                if (__instance is not Player || !PluginConfig.SovereignSanctuary.IsOn()) return true;
                if (!__instance.m_nview || !__instance.m_nview.IsOwner()) return true;
                return !Warded(__instance.transform.position);
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }
}

// The Unmaking Rite, in the game that owns the obliterator: right before it burns, each craftable weapon, tool, shield
// and piece of armor in it is taken out, and once the rest has burnt, part of what it cost to make and upgrade it is put
// in (what doesn't fit lands next to the obliterator).
// The burn is a coroutine: it is wrapped, so the work happens in the same step as the game's burn.
[HarmonyPatch(typeof(Incinerator), nameof(Incinerator.Incinerate))]
internal static class UnmakingPatch
{
    private const string Feature = "Unmaking Rite";
    private static readonly List<UnmakingReturns.Return> Returns = [];

    private static void Postfix(Incinerator __instance, long uid, ref IEnumerator __result) => __result = Wrap(__result, __instance, uid);

    private static IEnumerator Wrap(IEnumerator burn, Incinerator incinerator, long puller)
    {
        // Step 0 pulls the lever and waits; step 1 burns.
        for (int step = 0; ; step++)
        {
            List<ItemDrop.ItemData>? kept = step == 1 ? Keep(incinerator) : null;
            // What burns now decides the rites: told before the game's own answer, on the same way to the puller.
            if (step == 1) Report(incinerator, puller);
            bool noCoal = step == 1 && PerformsRite(incinerator);
            Dictionary<GameObject, int>? back = step == 1 ? Take(incinerator) : null;
            bool more = NoCoalBurn(burn, incinerator, noCoal);
            if (back != null) Give(incinerator, back);
            if (kept != null) PutBack(incinerator, kept);
            if (!more) yield break;
            yield return burn.Current;
        }
    }

    // Items burnt for a rite give no coal: during that burn the obliterator's conversions and its default "every few
    // items make one coal" are turned off, and put back right after.
    private static bool PerformsRite(Incinerator incinerator)
    {
        if (PatchGuard.IsDisabled(NoCoalFeature)) return false;
        try
        {
            if (!incinerator.m_nview.IsValid() || !incinerator.m_nview.IsOwner() || !ObliteratorSwitch.IsOn(incinerator)) return false;
            foreach (ObliteratorKeys.Picked picked in ObliteratorKeys.PickAll(incinerator.m_container.GetInventory()))
                if (PluginConfig.IsEnabled(picked.Kind)) return true;
            return false;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(NoCoalFeature, exception);
            return false;
        }
    }

    private const string ReportFeature = "Rites decided as it burns";

    private static void Report(Incinerator incinerator, long puller)
    {
        if (PatchGuard.IsDisabled(ReportFeature)) return;
        try { ObliteratorPull.Report(incinerator, puller); }
        catch (Exception exception) { PatchGuard.Fail(ReportFeature, exception); }
    }

    private const string NoCoalFeature = "No coal from rites";
    private const string KeepFeature = "Refused rites keep their items";

    // A rite that can't be performed there now (see ObliteratorPull.KeptKinds): its key items are taken out before the
    // burn and put back after, so they aren't lost (the player who pulled the lever is told why, see ObliteratorPull).
    private static List<ItemDrop.ItemData>? Keep(Incinerator incinerator)
    {
        if (PatchGuard.IsDisabled(KeepFeature)) return null;
        try
        {
            if (!incinerator.m_nview.IsValid() || !incinerator.m_nview.IsOwner() || incinerator.m_container.IsInUse()) return null;
            if (!ObliteratorSwitch.IsOn(incinerator)) return null;
            HashSet<WeatherKind> kinds = ObliteratorPull.KeptKinds(incinerator);
            if (kinds.Count == 0) return null;
            Inventory inventory = incinerator.m_container.GetInventory();
            List<ItemDrop.ItemData> kept = [];
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
            {
                if (!kinds.Contains(ObliteratorKeys.KindOf(item))) continue;
                inventory.RemoveItem(item);
                kept.Add(item);
            }
            return kept;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(KeepFeature, exception);
            return null;
        }
    }

    private static void PutBack(Incinerator incinerator, List<ItemDrop.ItemData> kept)
    {
        try
        {
            Inventory inventory = incinerator.m_container.GetInventory();
            Vector3 outside = incinerator.transform.position + incinerator.transform.forward * 1.5f + Vector3.up;
            foreach (ItemDrop.ItemData item in kept)
                if (!inventory.AddItem(item)) ItemDrop.DropItem(item, item.m_stack, outside, Quaternion.identity);
        }
        catch (Exception exception) { PatchGuard.Fail(KeepFeature, exception); }
    }

    private static bool NoCoalBurn(IEnumerator burn, Incinerator incinerator, bool noCoal)
    {
        if (!noCoal) return burn.MoveNext();
        List<Incinerator.IncineratorConversion> conversions = incinerator.m_conversions;
        ItemDrop defaultResult = incinerator.m_defaultResult;
        incinerator.m_conversions = [];
        incinerator.m_defaultResult = null;
        try { return burn.MoveNext(); }
        finally
        {
            incinerator.m_conversions = conversions;
            incinerator.m_defaultResult = defaultResult;
        }
    }

    private static Dictionary<GameObject, int>? Take(Incinerator incinerator)
    {
        if (PatchGuard.IsDisabled(Feature)) return null;
        try
        {
            // The same checks the game makes before burning.
            if (!incinerator.m_nview.IsValid() || !incinerator.m_nview.IsOwner() || incinerator.m_container.IsInUse()) return null;
            if (!ObliteratorSwitch.IsOn(incinerator)) return null;
            Inventory inventory = incinerator.m_container.GetInventory();
            if (!PluginConfig.IsEnabled(WeatherKind.Unmaking) || !ObliteratorKeys.Contains(ObliteratorKeys.PickAll(inventory), WeatherKind.Unmaking)) return null;

            Dictionary<GameObject, int> back = [];
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
            {
                if (!UnmakingReturns.Returns(item, Returns)) continue;
                foreach (UnmakingReturns.Return part in Returns)
                {
                    GameObject prefab = part.Item.gameObject;
                    back[prefab] = (back.TryGetValue(prefab, out int had) ? had : 0) + part.Amount;
                }
                inventory.RemoveItem(item);
            }
            return back;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return null;
        }
    }

    private static void Give(Incinerator incinerator, Dictionary<GameObject, int> back)
    {
        try
        {
            Inventory inventory = incinerator.m_container.GetInventory();
            Vector3 outside = incinerator.transform.position + incinerator.transform.forward * 1.5f + Vector3.up;
            foreach (KeyValuePair<GameObject, int> pair in back)
            {
                int stack = Mathf.Max(1, pair.Key.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize);
                for (int left = pair.Value; left > 0;)
                {
                    int chunk = Mathf.Min(left, stack);
                    left -= chunk;
                    if (inventory.CanAddItem(pair.Key, chunk) && inventory.AddItem(pair.Key, chunk)) continue;
                    GameObject dropped = UnityEngine.Object.Instantiate(pair.Key, outside + UnityEngine.Random.insideUnitSphere * 0.4f, Quaternion.identity);
                    dropped.GetComponent<ItemDrop>().SetStack(chunk);
                }
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
