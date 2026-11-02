using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;
using Object = UnityEngine.Object;

namespace JoksterCube.TempestRite.Domain;

// The Morgen-Pouch's animation (see PetPouch), seen by everyone with the mod:
// - Going in, a frozen copy of the creature (its looks only, nothing that thinks or collides) shrinks and spins into the
//   player's chest, while the creature itself is already gone. Each flight sets off with the sound of a portal
//   connecting, lowered.
// - Coming out, the creature's own looks grow out of the player's chest and fly to where it stands. Another game may only
//   get the creature a moment after the message, so it waits a little for it.
internal static class PouchFlight
{
    private const string Feature = "Morgen-Pouch Rite";
    private const float Seconds = 0.7f;
    private const float Smallest = 0.05f;
    private const float Arc = 0.8f;
    private const float Spin = 540f;
    private const float WaitForCreature = 3f;
    private const float SeenWithin = 80f;
    private const float SoundGap = 0.15f;
    private const string PortalPrefab = "portal_wood";
    private const float SoundPitch = 0.75f;
    private const float SoundSeconds = 6f;
    private static float _lastSound = -1f;

    private sealed class Flight
    {
        internal bool Into;
        internal float Start;
        // In: the copy's holder, destroyed at the end. Out: the creature's own looks, put back at the end.
        internal Transform Moved = null!;
        internal GameObject? Copy;
        internal Vector3 From;
        internal Vector3 Scale;
        internal Vector3 LocalPosition;
        internal Quaternion Rotation;
        internal Player? Player;
        internal Vector3 PlayerAt;
    }

    private static readonly List<Flight> Flights = [];
    private static readonly List<(ZDOID Creature, ZDOID Player, float Until)> Waiting = [];

    internal static void Register(ZRoutedRpc rpc) => rpc.Register<ZDOID, ZDOID, bool>(Rpcs.MorgenPouchFlight, RPC_Flight);

    internal static void ClearSession()
    {
        foreach (Flight flight in Flights) Finish(flight);
        Flights.Clear();
        Waiting.Clear();
    }

    // A creature about to be taken out of the world: call before destroying it.
    internal static void In(Character creature, Player player)
    {
        Fly(creature, player, into: true);
        Send(creature, player, into: true);
    }

    // A creature just let out (its object made from the pouch).
    internal static void Out(ZDOID creature, Player player)
    {
        if (Find(creature) is { } character) Fly(character, player, into: false);
        else Waiting.Add((creature, player.GetZDOID(), Time.time + WaitForCreature));
        if (ZRoutedRpc.instance != null) ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.MorgenPouchFlight, creature, player.GetZDOID(), false);
    }

    private static void Send(Character creature, Player player, bool into)
    {
        if (ZRoutedRpc.instance == null || !creature.m_nview || creature.m_nview.GetZDO() is not { } zdo) return;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.MorgenPouchFlight, zdo.m_uid, player.GetZDOID(), into);
    }

    private static void RPC_Flight(long sender, ZDOID creature, ZDOID player, bool into)
    {
        if (PatchGuard.IsDisabled(Feature) || !ZNet.instance || ZNet.instance.IsDedicated() || sender == ZDOMan.GetSessionID()) return;
        try
        {
            if (into)
            {
                if (Find(creature) is { } character && FindPlayer(player) is { } who && Near(who.transform.position)) Fly(character, who, into: true);
            }
            else Waiting.Add((creature, player, Time.time + WaitForCreature));
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // ---- Every frame ----

    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            for (int i = Waiting.Count - 1; i >= 0; i--)
            {
                var (creature, player, until) = Waiting[i];
                if (Find(creature) is { } character)
                {
                    Waiting.RemoveAt(i);
                    if (FindPlayer(player) is { } who && Near(who.transform.position)) Fly(character, who, into: false);
                }
                else if (Time.time > until) Waiting.RemoveAt(i);
            }
            for (int i = Flights.Count - 1; i >= 0; i--)
            {
                Flight flight = Flights[i];
                if (!flight.Moved || Step(flight))
                {
                    Finish(flight);
                    Flights.RemoveAt(i);
                }
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Moves a flight on; true once it has landed.
    private static bool Step(Flight flight)
    {
        float t = Mathf.Clamp01((Time.time - flight.Start) / Seconds);
        if (flight.Player) flight.PlayerAt = flight.Player!.GetCenterPoint();
        if (flight.Into)
        {
            // Speeding up as it is drawn in.
            float e = t * t;
            flight.Moved.position = Vector3.Lerp(flight.From, flight.PlayerAt, e) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * Arc);
            flight.Moved.localScale = flight.Scale * Mathf.Lerp(1f, Smallest, e);
            flight.Moved.rotation = Quaternion.AngleAxis(Spin * e, Vector3.up) * flight.Rotation;
        }
        else
        {
            // Slowing down as it lands where it stands.
            float e = 1f - (1f - t) * (1f - t);
            Vector3 home = flight.Moved.parent ? flight.Moved.parent.TransformPoint(flight.LocalPosition) : flight.LocalPosition;
            flight.Moved.position = Vector3.Lerp(flight.PlayerAt, home, e) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * Arc);
            flight.Moved.localScale = flight.Scale * Mathf.Lerp(Smallest, 1f, e);
        }
        return t >= 1f;
    }

    private static void Finish(Flight flight)
    {
        if (flight.Copy) Object.Destroy(flight.Copy);
        else if (flight.Moved)
        {
            flight.Moved.localPosition = flight.LocalPosition;
            flight.Moved.localScale = flight.Scale;
        }
    }

    private static void Fly(Character creature, Player player, bool into)
    {
        if (!creature || !player) return;
        GameObject visual = creature.m_visual ? creature.m_visual : creature.gameObject;
        Flight flight = new() { Into = into, Start = Time.time, Player = player, PlayerAt = player.GetCenterPoint() };
        if (into)
        {
            GameObject? copy = Copy(visual);
            if (!copy) return;
            flight.Copy = copy;
            flight.Moved = copy!.transform;
            flight.From = copy.transform.position;
            flight.Scale = copy.transform.localScale;
            flight.Rotation = copy.transform.rotation;
        }
        else
        {
            if (visual == creature.gameObject) return;
            flight.Moved = visual.transform;
            flight.LocalPosition = visual.transform.localPosition;
            flight.Scale = visual.transform.localScale;
            // Starts small, before it is drawn even once.
            visual.transform.localScale = flight.Scale * Smallest;
            visual.transform.position = flight.PlayerAt;
        }
        Flights.Add(flight);
        // A sound where it sets off; once for creatures setting off together, so it doesn't get louder.
        if (Time.time - _lastSound < SoundGap) return;
        _lastSound = Time.time;
        Sound(into ? visual.transform.position + Vector3.up * 0.8f : flight.PlayerAt);
    }

    // The sound of a portal connecting, lower: only its sound, a copy for this game only.
    private static void Sound(Vector3 at)
    {
        TeleportWorld? portal = ZNetScene.instance && ZNetScene.instance.GetPrefab(PortalPrefab) is { } prefab && prefab
            ? prefab.GetComponent<TeleportWorld>()
            : null;
        if (!portal || portal!.m_connected == null)
        {
            WarnNoSound();
            return;
        }
        foreach (EffectList.EffectData effect in portal.m_connected.m_effectPrefabs)
        {
            if (!effect.m_enabled || !effect.m_prefab || !effect.m_prefab.GetComponentInChildren<ZSFX>(true)) continue;
            GameObject made;
            ZNetView.m_forceDisableInit = true;
            try { made = Object.Instantiate(effect.m_prefab, at, Quaternion.identity); }
            finally { ZNetView.m_forceDisableInit = false; }
            foreach (Renderer renderer in made.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (Light light in made.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (ZSFX sound in made.GetComponentsInChildren<ZSFX>(true)) sound.SetPitchModifier(SoundPitch);
            Object.Destroy(made, SoundSeconds);
        }
    }

    private static bool _warnedNoSound;

    private static void WarnNoSound()
    {
        if (_warnedNoSound) return;
        _warnedNoSound = true;
        Plugin.ModLogger.LogWarning($"{Feature}: no {PortalPrefab} found: its flights go without a sound.");
    }

    // A copy of a creature's looks, in its current pose, with nothing that runs, collides or animates: made under an
    // inactive holder, so none of its scripts ever start.
    private static GameObject? Copy(GameObject visual)
    {
        GameObject holder = new("TempestRite_MorgenPouchFlight");
        holder.SetActive(false);
        holder.transform.SetPositionAndRotation(visual.transform.position, visual.transform.rotation);
        holder.transform.localScale = visual.transform.lossyScale;
        try
        {
            GameObject copy = Object.Instantiate(visual, holder.transform, false);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one;
            Strip<MonoBehaviour>(copy);
            Strip<Animator>(copy);
            Strip<Joint>(copy);
            Strip<Collider>(copy);
            Strip<Rigidbody>(copy);
            holder.SetActive(true);
            return holder;
        }
        catch (Exception exception)
        {
            Object.Destroy(holder);
            Plugin.ModLogger.LogWarning($"{Feature}: no flight for {visual.name}: {exception.Message}");
            return null;
        }
    }

    private static void Strip<T>(GameObject copy) where T : Component
    {
        T[] parts = copy.GetComponentsInChildren<T>(true);
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            try { Object.DestroyImmediate(parts[i]); }
            catch (Exception) { /* one that another still needs: harmless on a copy that never runs */ }
        }
    }

    private static Character? Find(ZDOID id) =>
        ZNetScene.instance && ZNetScene.instance.FindInstance(id) is { } go && go && go.GetComponent<Character>() is { } character && character ? character : null;

    private static Player? FindPlayer(ZDOID id)
    {
        foreach (Player player in Player.GetAllPlayers())
            if (player && player.GetZDOID() == id) return player;
        return null;
    }

    private static bool Near(Vector3 at) => Player.m_localPlayer && Vector3.Distance(Player.m_localPlayer.transform.position, at) < SeenWithin;
}
