using System;
using System.Collections.Generic;
using System.Text;
using JoksterCube.AbilityKit;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;
using Keys = JoksterCube.ChestKit.Common.KeyboardShortcutExtensions;

namespace JoksterCube.TempestRite.Domain;

// The Morgen-Pouch Rite, a blessing with an ability beside the Guardian Power (see AbilityKit):
// - Pressing its key with the pouch empty takes up to Creatures tamed creatures the player has petted within Capture
//   Range, the last petted first (a mount, a wolf, a boar, a chicken...; the mount they ride too, if petted). One that
//   can follow is picked while it follows the player; any other is picked by petting it and let go by petting it again. Those it
//   would take are marked: their hover text says so and they wear a violet Guardian Power glow (seen only by the player). The game
//   lets nobody pet anything while riding, so with nothing marked a rider is told to climb off.
//   Each is written down whole (its object's data: health, stars, name, saddle...) and taken out of the world, flying
//   into the player as it goes (see PouchFlight).
// - The ability then waits: Release Cooldown before what was taken can be let out, Capture Cooldown after letting out
//   before taking again (world time, saved with the pouch, so logging out doesn't skip it). Letting go because the
//   blessing ended or the player died doesn't wait.
// - Pressing it again lets them all out around the player, on level ground with room to stand (where the player stands
//   if there is none), as new objects made from what was written down, flying out of the player. They stay petted, so
//   the next press takes them again.
// - What the pouch holds is saved with the character (with the world it came from), so it goes through portals, over
//   the sea and through logging out. The character file is written the next frame after each change, so a crash can't
//   lose a pet (without the map, which takes the game a second to pack up).
// - When the blessing runs out (or is ended) the pouch lets go where the player stands; when the player dies, where they
//   die. In another world, the pouch keeps what it holds until the player is back in its world.
// - Summons can be pocketed. A new summon over its limit takes the one of its kind that started following first, in the
//   world or in the pouch (see Unsummons).
// - Going in or out gives off a puff of violet wisps, which everyone with the mod sees.
internal static class PetPouch
{
    private const string Feature = "Morgen-Pouch Rite";
    private const string SlotId = "TempestRite_MorgenPouch";
    private const string SaveKey = "TempestRite_MorgenPouch";
    // Which creatures a press would take is worked out this often.
    private const float PickEvery = 0.5f;
    // Creatures let out stand in rings this far from the player, this far apart at least.
    private static readonly float[] Rings = [2.5f, 4f, 5.5f];
    private const int RingSpots = 16;
    private const float Spread = 1.6f;

    private sealed class Pocketed
    {
        internal bool Mount;
        internal int Prefab;
        internal long Followed;
        // When the player last petted it (Time.time): given back when it comes out, so it stays petted.
        internal float Petted;
        internal string Name = string.Empty;
        internal byte[] Data = [];
    }

    private static readonly AbilitySlot Slot = new(SlotId, SlotPriority.TempestRitePetPouch);
    private static readonly List<Pocketed> Held = [];
    // When a creature started following its player (world time ticks), and whom: written on the creature by the game that
    // owns it, so it travels with it (also into the pouch) and isn't renewed when it follows the same player again after
    // loading.
    private static readonly int FollowedAtKey = "TempestRite_FollowedAt".GetStableHashCode();
    private static readonly int FollowedByKey = "TempestRite_FollowedBy".GetStableHashCode();
    // When the local player last petted each creature (Time.time), and those a press would take.
    private static readonly Dictionary<ZDOID, float> PettedAt = [];
    private static readonly List<Character> Picks = [];
    // Those marked, with the glow they wear.
    private static readonly List<(Character Creature, GameObject Aura)> Glowing = [];
    private static float _nextPick;
    private static bool _savePending;
    private static long _world;
    // World time (seconds) from which the ability can be used again.
    private static double _readyAt;
    private static Player? _owner;

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<Vector3>(Rpcs.MorgenPouchPuff, RPC_Puff);
        PouchFlight.Register(rpc);
    }

    internal static void ClearSession()
    {
        Held.Clear();
        PettedAt.Clear();
        Picks.Clear();
        Glowing.Clear();
        PouchOutline.ClearSession();
        PouchFlight.ClearSession();
        _savePending = false;
        _owner = null;
        _world = 0L;
        _readyAt = 0.0;
    }

    internal static string KeyText => $"<color=orange>[{Keys.DisplayName(PluginConfig.MorgenPouchShortcut.Value)}]</color>";

    // For the status tooltip: what the pouch holds, if anything.
    internal static string Carrying()
    {
        if (Held.Count == 0) return string.Empty;
        return "\n" + string.Format(Messages.MorgenPouchCarrying, Names(Held));
    }

    // Seconds until the ability can be used again.
    private static float Wait() => ZNet.instance ? Mathf.Max(0f, (float)(_readyAt - ZNet.instance.GetTimeSeconds())) : 0f;

    private static void WaitFor(float seconds) => _readyAt = ZNet.instance.GetTimeSeconds() + Mathf.Max(0f, seconds);

    private static bool InThisWorld => Held.Count == 0 || (ZNet.instance && _world == ZNet.instance.GetWorldUID());

    // ---- Every frame (after the blessings are read) ----

    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            PouchFlight.Update();
            Player? me = Player.m_localPlayer;
            if (me != _owner)
            {
                _owner = me;
                Held.Clear();
                _savePending = false;
                if (me) Load(me!);
            }
            if (me && _savePending) SaveCharacter(me!);
            if (!me || me!.IsDead())
            {
                Picks.Clear();
                Glow();
                return;
            }
            bool blessed = Blessings.IsActive(WeatherKind.MorgenPouch);
            if (!blessed)
            {
                Picks.Clear();
                Glow();
                if (Held.Count > 0 && InThisWorld && Settled(me)) Release(me, Messages.MorgenPouchEjected);
                return;
            }
            if (Held.Count == 0) Mark(me);
            else Picks.Clear();
            Glow();
            if (!RallyHorn.CanTakeInput(me) || !Keys.IsDownWhileMoving(PluginConfig.MorgenPouchShortcut.Value)) return;
            if (Held.Count > 0 && !InThisWorld) me.Message(MessageHud.MessageType.Center, Messages.MorgenPouchOtherWorld);
            else if (Wait() > 0f) me.Message(MessageHud.MessageType.Center, string.Format(Messages.MorgenPouchWait, AbilitySlot.TimeText(Wait())));
            else if (Held.Count == 0) Take(me);
            else if (Settled(me))
            {
                WaitFor(PluginConfig.MorgenPouchCaptureCooldown.Value);
                Release(me, Messages.MorgenPouchReleased);
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // The slot beside the Guardian Power, while blessed: Pocket or Release, with how many are marked or held out of how
    // many it holds, and its key under it.
    internal static void UpdateHud(Hud hud)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Player? me = Player.m_localPlayer;
            bool show = me && !me!.IsDead() && Blessings.IsActive(WeatherKind.MorgenPouch);
            int room = Room();
            string name = Held.Count > 0
                ? string.Format(Messages.MorgenPouchRelease, Held.Count, room)
                : string.Format(Messages.MorgenPouchTake, Mathf.Min(Picks.Count, room), room);
            // The key under it, or the time left while it waits.
            float wait = show ? Wait() : 0f;
            string under = !show ? string.Empty : wait > 0f ? AbilitySlot.TimeText(wait) : Keys.DisplayName(PluginConfig.MorgenPouchShortcut.Value);
            Slot.Update(hud, show, WeatherIcons.For(WeatherKind.MorgenPouch), name, under, ready: InThisWorld && wait <= 0f);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // The local player died: what the pouch holds comes out where they fell.
    internal static void Died(Player player)
    {
        if (PatchGuard.IsDisabled(Feature) || player != Player.m_localPlayer || Held.Count == 0 || !InThisWorld) return;
        try { Release(player, Messages.MorgenPouchEjected); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // A tamed creature was told to follow or stop (by the game that owns it): a new player to follow starts its time.
    internal static void Commanded(Tameable tameable)
    {
        if (!tameable.m_nview || !tameable.m_nview.IsOwner() || tameable.m_nview.GetZDO() is not { } zdo) return;
        string follow = zdo.GetString(ZDOVars.s_follow);
        if (string.IsNullOrEmpty(follow))
        {
            if (zdo.GetString(FollowedByKey).Length > 0) zdo.Set(FollowedByKey, string.Empty);
            return;
        }
        if (zdo.GetString(FollowedByKey) == follow) return;
        zdo.Set(FollowedByKey, follow);
        zdo.Set(FollowedAtKey, ZNet.instance.GetTime().Ticks);
    }

    // When it started following its player: as written by Commanded, else when it was spawned (followed before the mod,
    // or owned by a game without it).
    private static long Followed(ZDO zdo)
    {
        long at = zdo.GetLong(FollowedAtKey);
        return at != 0L ? at : zdo.GetLong(ZDOVars.s_spawnTime);
    }

    // ---- In ----

    // The local player petted a tamed creature. One that can follow is told to follow or stay by it: picked while it
    // follows the player (see Pick), the petting only says when. Any other is picked by one petting and let go by the
    // next.
    internal static void Petted(Tameable tameable, Humanoid user)
    {
        if (user != Player.m_localPlayer || !tameable.m_nview || tameable.m_nview.GetZDO() is not { } zdo) return;
        if (tameable.m_commandable || !PettedAt.Remove(zdo.m_uid)) PettedAt[zdo.m_uid] = Time.time;
        _nextPick = 0f;
    }

    // Whether this creature goes into the pouch on the next press (for its hover text).
    internal static bool Picked(Character character) => Picks.Contains(character);

    private static void Take(Player me)
    {
        Pick(me);
        if (Picks.Count == 0)
        {
            // Nobody can pet anything while riding.
            me.Message(MessageHud.MessageType.Center, me.IsRiding() ? Messages.MorgenPouchNothingRiding : Messages.MorgenPouchNothing);
            return;
        }
        Character? ridden = me.GetDoodadController() is Sadle sadle && sadle ? sadle.GetCharacter() : null;
        foreach (Character creature in Picks)
        {
            if (!creature) continue;
            bool mount = creature == ridden;
            if (mount) me.StopDoodadControl();
            Pocket(me, creature, mount);
        }
        Picks.Clear();
        if (Held.Count == 0) return;
        _world = ZNet.instance.GetWorldUID();
        WaitFor(PluginConfig.MorgenPouchReleaseCooldown.Value);
        Saved(me);
        me.Message(MessageHud.MessageType.Center, string.Format(Messages.MorgenPouchTaken, Names(Held)));
    }

    // The tamed creatures picked (following the player, for those that can follow; else petted once more than let go),
    // alive, within Capture Range and nobody else riding them: the last petted first, up to Creatures.
    private static void Pick(Player me)
    {
        Picks.Clear();
        Vector3 here = me.transform.position;
        float range = PluginConfig.MorgenPouchRange.Value;
        long rider = me.GetPlayerID();
        List<(Character Creature, float Petted)> found = [];
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!character || character.IsPlayer() || character.IsDead() || !character.IsTamed()) continue;
            if (character.GetComponent<Tameable>() is not { } tameable || !character.m_nview || character.m_nview.GetZDO() is not { } zdo) continue;
            bool known = PettedAt.TryGetValue(zdo.m_uid, out float petted);
            if (tameable.m_commandable)
            {
                if (zdo.GetString(ZDOVars.s_follow) != me.GetPlayerName()) continue;
                // Following since before this session: after those petted.
                if (!known) petted = float.MinValue;
            }
            else if (!known) continue;
            long user = zdo.GetLong(ZDOVars.s_user);
            if ((user != 0L && user != rider) || Vector3.Distance(here, character.transform.position) > range) continue;
            found.Add((character, petted));
        }
        found.Sort((x, y) => y.Petted.CompareTo(x.Petted));
        int room = Room();
        for (int i = 0; i < found.Count && Picks.Count < room; i++) Picks.Add(found[i].Creature);
    }

    private static int Room() => Mathf.Max(1, PluginConfig.MorgenPouchCount.Value);

    // While blessed with the pouch empty: which creatures a press would take, now and then.
    private static void Mark(Player me)
    {
        if (Time.time < _nextPick) return;
        _nextPick = Time.time + PickEvery;
        Pick(me);
    }

    // Those a press would take wear a violet Guardian Power glow (see PouchAura); those no longer marked lose it.
    private static void Glow()
    {
        bool on = PluginConfig.ShowMorgenPouchGlow.IsOn();
        for (int i = Glowing.Count - 1; i >= 0; i--)
        {
            var (creature, aura) = Glowing[i];
            if (on && creature && aura && Picks.Contains(creature)) continue;
            if (aura) UnityEngine.Object.Destroy(aura);
            Glowing.RemoveAt(i);
        }
        if (!on)
        {
            PouchOutline.Show(Glowing, 0f);
            return;
        }
        foreach (Character creature in Picks)
        {
            if (!creature || Glowing.Exists(glowing => glowing.Creature == creature)) continue;
            if (PouchAura.Make(creature) is { } aura) Glowing.Add((creature, aura));
        }
        PouchAura.Pulse(Glowing);
    }

    private static void Unglow(Character creature)
    {
        for (int i = Glowing.Count - 1; i >= 0; i--)
        {
            if (Glowing[i].Creature != creature) continue;
            if (Glowing[i].Aura) UnityEngine.Object.Destroy(Glowing[i].Aura);
            Glowing.RemoveAt(i);
        }
    }

    private static void Pocket(Player me, Character creature, bool mount)
    {
        ZNetView view = creature.m_nview;
        if (!view || !view.IsValid()) return;
        view.ClaimOwnership();
        ZDO zdo = view.GetZDO();
        if (mount) zdo.Set(ZDOVars.s_user, 0L);
        ZPackage package = new();
        zdo.Serialize(package);
        PettedAt.TryGetValue(zdo.m_uid, out float petted);
        PettedAt.Remove(zdo.m_uid);
        Held.Add(new Pocketed
        {
            Mount = mount,
            Prefab = zdo.GetPrefab(),
            Followed = Followed(zdo),
            Petted = petted,
            Name = CleanName(Localization.instance.Localize(creature.GetHoverName())),
            Data = package.GetArray()
        });
        Unglow(creature);
        PouchFlight.In(creature, me);
        Puff(creature.transform.position + Vector3.up * 0.8f, send: true);
        ZNetScene.instance.Destroy(creature.gameObject);
    }

    // ---- Out ----

    // Spots around the player on level, dry ground with room to stand and no wall in between, the one in front first,
    // at least Spread apart; where the player stands for any it can't find.
    private static List<Vector3> Spots(Player me, int count)
    {
        Vector3 here = me.transform.position;
        int mask = me.m_placeRayMask;
        float facing = Mathf.Atan2(me.transform.forward.z, me.transform.forward.x);
        List<Vector3> spots = [];
        foreach (float ring in Rings)
        {
            // In front, then turning both ways round to the back.
            for (int k = 0; k < RingSpots && spots.Count < count; k++)
            {
                int side = (k + 1) / 2 * (k % 2 == 0 ? -1 : 1);
                float angle = facing + side * Mathf.PI * 2f / RingSpots;
                Vector3 probe = here + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring;
                if (Stable(here, probe, mask, out Vector3 spot) && FarFrom(spots, spot)) spots.Add(spot);
            }
            if (spots.Count >= count) break;
        }
        while (spots.Count < count) spots.Add(here);
        return spots;
    }

    private static bool Stable(Vector3 here, Vector3 probe, int mask, out Vector3 spot)
    {
        spot = probe;
        if (!Physics.Raycast(new Vector3(probe.x, here.y + 2f, probe.z), Vector3.down, out RaycastHit hit, 5f, mask)) return false;
        if (hit.normal.y < 0.8f || Mathf.Abs(hit.point.y - here.y) > 1.5f) return false;
        if (Floating.GetLiquidLevel(hit.point) > hit.point.y + 0.3f) return false;
        Vector3 chest = here + Vector3.up;
        if (Physics.Linecast(chest, hit.point + Vector3.up, mask)) return false;
        if (Physics.CheckSphere(hit.point + Vector3.up * 1.2f, 0.6f, mask)) return false;
        spot = hit.point + Vector3.up * 0.1f;
        return true;
    }

    private static bool FarFrom(List<Vector3> spots, Vector3 spot)
    {
        foreach (Vector3 other in spots)
            if (Vector3.Distance(other, spot) < Spread) return false;
        return true;
    }

    // Whether the ground around the player has loaded (right after logging in or a portal it may not have).
    private static bool Settled(Player me) =>
        !me.IsTeleporting() && ZNetScene.instance && ZNetScene.instance.IsAreaReady(me.transform.position);

    private static void Release(Player me, string message)
    {
        Vector3 here = me.transform.position;
        List<Vector3> spots = Spots(me, Held.Count);
        List<Pocketed> left = [];
        List<Pocketed> out_ = [];
        for (int i = 0; i < Held.Count; i++)
        {
            Pocketed pocketed = Held[i];
            // Facing the player.
            Vector3 facing = here - spots[i];
            facing.y = 0f;
            Quaternion turn = facing.sqrMagnitude > 0.01f ? Quaternion.LookRotation(facing) : me.transform.rotation;
            if (Spawn(pocketed, spots[i], turn) is { } zdo)
            {
                out_.Add(pocketed);
                // Still petted: the next press takes it again.
                PettedAt[zdo.m_uid] = pocketed.Petted;
                _nextPick = 0f;
                PouchFlight.Out(zdo.m_uid, me);
            }
            else left.Add(pocketed);
        }
        Held.Clear();
        // A creature whose prefab is gone (a mod removed) stays in the pouch rather than being lost.
        Held.AddRange(left);
        Puff(me.GetCenterPoint(), send: true);
        Saved(me);
        if (out_.Count > 0) me.Message(MessageHud.MessageType.Center, string.Format(message, Names(out_)));
    }

    private static ZDO? Spawn(Pocketed pocketed, Vector3 at, Quaternion turn)
    {
        if (!ZNetScene.instance.GetPrefab(pocketed.Prefab))
        {
            Plugin.ModLogger.LogWarning($"{Feature}: no prefab {pocketed.Prefab} for {pocketed.Name}; kept in the pouch.");
            return null;
        }
        ZDO zdo = ZDOMan.instance.CreateNewZDO(at, pocketed.Prefab);
        zdo.Deserialize(new ZPackage(pocketed.Data));
        zdo.SetPosition(at);
        zdo.SetRotation(turn);
        zdo.Set(ZDOVars.s_user, 0L);
        zdo.IncreaseDataRevision();
        ZNetScene.instance.CreateObject(zdo);
        return zdo;
    }

    // ---- Summons ----

    // A new summon is over its kind's limit (the game is about to take the oldest of the player's away): the pouch's
    // count too, and those that started following first go, from the world or the pouch. False: nothing in the pouch is of its kind, so the
    // game does it as usual.
    internal static bool Unsummons(Tameable tameable, int max)
    {
        if (PatchGuard.IsDisabled(Feature) || Held.Count == 0 || !tameable.m_character || !tameable.m_nview || !tameable.m_nview.IsOwner()) return false;
        Player? me = Player.m_localPlayer;
        if (!me || tameable.m_nview.GetZDO().GetString(ZDOVars.s_follow) != me!.GetPlayerName()) return false;
        try
        {
            string kind = tameable.m_character.m_name;
            List<(long Ticks, Tameable? World, Pocketed? Pocket)> all = [];
            foreach (Pocketed pocketed in Held)
                if (CharacterName(pocketed.Prefab) == kind) all.Add((pocketed.Followed, null, pocketed));
            if (all.Count == 0) return false;
            foreach (Character character in Character.GetAllCharacters())
            {
                if (!character || character.m_name != kind || !character.m_nview || character.m_nview.GetZDO() is not { } zdo) continue;
                if (zdo.GetString(ZDOVars.s_follow) != me.GetPlayerName() || character.GetComponent<Tameable>() is not { } world) continue;
                // The new summon hasn't been stamped yet: it is the newest.
                all.Add((world == tameable ? long.MaxValue : Followed(zdo), world, null));
            }
            all.Sort((a, b) => a.Ticks.CompareTo(b.Ticks));
            int excess = all.Count - max;
            List<Pocketed> lost = [];
            for (int i = 0; i < excess; i++)
            {
                if (all[i].World is { } world && world) world.UnSummon();
                else if (all[i].Pocket is { } pocketed && Held.Remove(pocketed)) lost.Add(pocketed);
            }
            if (excess > 0) me.Message(MessageHud.MessageType.Center, tameable.m_maxSummonReached);
            if (lost.Count > 0)
            {
                PlayerMessages.TopLeft(string.Format(Messages.MorgenPouchSummonLost, Names(lost)));
                Saved(me);
            }
            return true;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return false;
        }
    }

    private static string CharacterName(int prefab) =>
        ZNetScene.instance && ZNetScene.instance.GetPrefab(prefab) is { } go && go.GetComponent<Character>() is { } character ? character.m_name : string.Empty;

    // ---- Puffs ----

    private static void Puff(Vector3 at, bool send)
    {
        DomainVisuals.Burst(WeatherKind.MorgenPouch, at, DomainVisuals.RiteColor(WeatherKind.MorgenPouch), 1.4f, always: true);
        if (send && ZRoutedRpc.instance != null) ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.MorgenPouchPuff, at);
    }

    private static void RPC_Puff(long sender, Vector3 at)
    {
        if (PatchGuard.IsDisabled(Feature) || !ZNet.instance || ZNet.instance.IsDedicated() || sender == ZDOMan.GetSessionID()) return;
        try
        {
            Player? me = Player.m_localPlayer;
            if (me && Vector3.Distance(me!.transform.position, at) < 80f) Puff(at, send: false);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // ---- Saving ----

    private static string Names(List<Pocketed> pocketed)
    {
        StringBuilder text = new();
        foreach (Pocketed one in pocketed)
        {
            if (text.Length > 0) text.Append(", ");
            text.Append(one.Name);
        }
        return text.ToString();
    }

    private static string CleanName(string name) => name.Replace("\n", " ").Replace("\r", " ");

    // Written on the character at once, and the character file written the next frame: the pouch and the world must
    // never disagree for long.
    private static void Saved(Player me)
    {
        Save(me);
        _savePending = true;
    }

    // Only the character itself: the game's own save also packs up the whole explored map, which hangs the game for about
    // a second. The map is saved as usual by the game's own saves.
    private static void SaveCharacter(Player me)
    {
        _savePending = false;
        if (SaveSystem.HasSessionFlag(SaveSystemSessionFlags.DontSaveCharacter)) return;
        if (!Game.instance || Game.instance.GetPlayerProfile() is not { } profile) return;
        profile.SavePlayerData(me);
        profile.Save();
    }

    // "world,ready at" on the first line, then "mount,prefab,followed ticks,data,name" lines. Kept while empty too as long
    // as the ability waits.
    private static void Save(Player player)
    {
        if (Held.Count == 0 && Wait() <= 0f)
        {
            player.m_customData.Remove(SaveKey);
            return;
        }
        StringBuilder text = new();
        text.Append(_world).Append(',').Append(_readyAt.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
        foreach (Pocketed pocketed in Held)
            text.Append(pocketed.Mount ? '1' : '0').Append(',')
                .Append(pocketed.Prefab).Append(',')
                .Append(pocketed.Followed).Append(',')
                .Append(Convert.ToBase64String(pocketed.Data)).Append(',')
                .Append(pocketed.Name).Append('\n');
        player.m_customData[SaveKey] = text.ToString();
    }

    private static void Load(Player player)
    {
        if (!player.m_customData.TryGetValue(SaveKey, out string text)) return;
        string[] lines = text.Split('\n');
        string[] head = lines[0].Split(',');
        if (!long.TryParse(head[0], out _world)) return;
        if (head.Length > 1)
            double.TryParse(head[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _readyAt);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split([','], 5);
            if (parts.Length < 5 || !int.TryParse(parts[1], out int prefab) || !long.TryParse(parts[2], out long ticks)) continue;
            byte[] data;
            try { data = Convert.FromBase64String(parts[3]); }
            catch (FormatException) { continue; }
            // Petted in the order they were held: the first most lately.
            Held.Add(new Pocketed { Mount = parts[0] == "1", Prefab = prefab, Followed = ticks, Petted = Time.time - i, Data = data, Name = parts[4] });
        }
    }
}
