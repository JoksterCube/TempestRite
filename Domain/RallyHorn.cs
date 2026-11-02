using System;
using System.Collections.Generic;
using JoksterCube.AbilityKit;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static JoksterCube.TempestRite.Settings.Constants;
using Keys = JoksterCube.ChestKit.Common.KeyboardShortcutExtensions;

namespace JoksterCube.TempestRite.Domain;

// The Rally-Horn Rite, a kinship rite: everyone away from the obliterator it burnt in is called there.
// - Everyone on the server hears the kinship war-drum, then a horn (a joyful brass call made in code, see HornSound)
//   when it is burnt.
// - Each such player gets the call for Call Seconds: a Rally Call status, and a Rally Call slot beside the Guardian
//   Power (see AbilityKit) with its key under it, pulsing as the call comes, and a crossed-out Refuse slot beside it.
//   Letting it fade or refusing costs nothing. Only one call waits for answers at a time (see RallyPending).
// - Pressing the Rally Call Shortcut answers it, unless something stands in the way (the call then stays open): as at
//   a portal, carrying anything that can't go through one, or portals being off in the world; an arena bubble; or
//   being in a fight (an enemy onto the player, or the player attacking just now). The portal checks run again until
//   the player vanishes, so picking up a non-teleportable item while kneeling stops it.
// - While the call waits for an answer, horns fly up from the player and the horn sounds again every 10 seconds.
// - Answering: the player stops what they're doing (stands up, lets go of the helm...) and kneels, and lightning (no
//   harm done) strikes where they kneel. A moment later they sink into the ground, leaving a gold pillar and the
//   horn's echo behind (see KinshipFx), and only then the teleport starts. The call ends and the kinship
//   wait (Duration Minutes) starts.
// - While the player is in the loading screen they wait out of sight under the obliterator; once its surroundings
//   have loaded, a free spot is picked
//   around it on the same level (as the Soul-Tether Rite places tombstones), so players arriving together are spread
//   around it rather than in each other or in a wall. They arrive kneeling, struck by lightning again, and a moment
//   later stand up by themselves with a challenge and a horn blast, free to move. From kneeling down until then they are held still and nobody can hurt them.
// Everyone near sees both strikes and the kneeling.
internal static class RallyHorn
{
    private const string Feature = "Rally-Horn Rite";
    private const string SlotId = "TempestRite_RallyCall";
    private const string RefuseSlotId = "TempestRite_RallyRefuse";
    // While the call waits for an answer, horns fly up from the player this often.
    private const float FlourishEvery = 10f;
    private const string KneelEmote = "kneel";
    private const string RisenEmote = "challenge";
    // A player within this height of the obliterator (and within the Blessings Range) is there already.
    private const float HereHeight = 10f;
    // In a fight: an enemy has sensed the player, or the player attacked within this many seconds.
    private const float CombatSeconds = 5f;
    // How long the player has to stop what they're doing (standing up, a swing...) before kneeling.
    private const float StopSeconds = 3f;
    // How long they kneel before sinking, and after arriving before they are free to move.
    private const float KneelSeconds = 1.6f;
    // Sinking into the ground, kneeling: this deep (out of sight), over this long, before the teleport starts.
    private const float SinkDepth = 2.4f;
    private const float SinkSeconds = 1.4f;
    // The game moves a teleporting player only after this long; until then they are held where they sank to.
    private const float GameTeleportDelay = 2f;
    private const float RiseSeconds = 2f;
    // Where the teleport aims until a free spot is found: this far from the obliterator, a little above its level.
    private const float FirstAim = 3f;
    private const float AimLift = 0.5f;
    // Until then the player waits this far under it, out of sight of the players there (the game puts a teleporting
    // player at the aim at once, long before the loading screen ends).
    private const float HideDepth = 20f;
    // The horn heard by everyone when the rite is called (made in code, see HornSound), how loud.
    private const float HornVolume = 0.8f;
    // A free spot is picked among this many of the nearest, so players arriving together spread out.
    private const int SpotChoices = 8;
    // The room a player needs (half its size).
    private static readonly Vector3 PlayerRoom = new(0.4f, 0.95f, 0.4f);
    // A teleport still loading after this long is left to the game: the player is let go.
    private const float TeleportTimeout = 40f;
    // Lightning is shown to players at most this far from it.
    private const float StrikeRange = 150f;

    // What answering the call is at.
    private enum Step
    {
        None,
        // Stopping what they're doing, to kneel.
        Stopping,
        // Kneeling where they stood, before sinking.
        Kneeling,
        // Sinking into the ground, kneeling, before the teleport.
        Sinking,
        // In the loading screen.
        Teleporting,
        // Kneeling at the obliterator, before they are free.
        Arrived
    }

    private static readonly AbilitySlot Slot = new(SlotId, SlotPriority.TempestRiteRallyCall);
    // Beside it, crossed out: refusing the call.
    private static readonly AbilitySlot RefuseSlot = new(RefuseSlotId, SlotPriority.TempestRiteRallyRefuse);
    private static readonly List<Vector3> Spots = [];
    private static StatusEffect? _effect;

    // The call open now, if any.
    private static bool _open;
    private static Vector3 _center;
    private static float _until;
    private static float _duration;
    private static string _calledBy = string.Empty;

    // Answering it.
    private static Step _step;
    private static float _stepUntil;
    private static Vector3 _arrivalCenter;
    private static bool _spotChosen;
    // Where they knelt, and whether they are under it now (sunk, with no collider, not yet moved by the teleport).
    private static Vector3 _sinkFrom;
    private static bool _sunk;

    internal static void Register(ZRoutedRpc rpc) => rpc.Register<Vector3>(Rpcs.RallyArrived, RPC_Strike);

    internal static void ClearSession()
    {
        _open = false;
        _step = Step.None;
        _spotChosen = false;
        _sunk = false;
        _calledBy = string.Empty;
        Spots.Clear();
        // Both go with the player and the map on logout.
        _flourish.Clear();
        _nextFlourish = 0f;
        _pin = null;
    }

    private static float LeftSeconds => Mathf.Max(0f, _until - Time.time);

    private static string KeyText => $"<color=orange>[{Keys.DisplayName(PluginConfig.RallyCallShortcut.Value)}]</color>";

    private static string RefuseKeyText => $"<color=orange>[{Keys.DisplayName(PluginConfig.RallyRefuseShortcut.Value)}]</color>";

    // The local player received the rite performed at position. Returns what it does to them, said after the rite's
    // name. A player it passes by is done with the call at once (see RallyPending); one it calls is done once they
    // answer, refuse or let it fade.
    internal static string Called(Player me, Vector3 position, bool byLocalPlayer, string calledBy)
    {
        float seconds = PluginConfig.RallyCallSeconds.Value;
        SoundHorn();
        string? passedBy = PassedBy(me, position, byLocalPlayer);
        if (passedBy != null)
        {
            RallyPending.Done();
            return passedBy;
        }

        _open = true;
        _center = position;
        _duration = seconds;
        _until = Time.time + seconds;
        _calledBy = calledBy;
        ShowStatus(me, added: true);
        KinshipFx.PulseRallySlot(SlotId);
        return Messages.RallyHornCalled;
    }

    // Why the call passes the local player by (their message), or null.
    private static string? PassedBy(Player me, Vector3 position, bool byLocalPlayer)
    {
        if (byLocalPlayer) return Messages.RallyHornByYou;
        Vector3 at = me.transform.position;
        if (Utils.DistanceXZ(at, position) <= PluginConfig.BlessingRange.Value && Mathf.Abs(at.y - position.y) <= HereHeight)
            return Messages.RallyHornHere;
        if (KinshipRites.IsActive(WeatherKind.RallyHorn))
            return Messages.RallyHornWaiting;
        // Already on the way to answering one (only after the server let a new call through in the meantime).
        return _step != Step.None ? string.Empty : null;
    }

    // Whether the local player is held still (and can't be hurt) by answering the call: from kneeling down until free.
    internal static bool Holds(Character character) => _step != Step.None && character == Player.m_localPlayer;

    // ---- Every frame ----

    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            // Ready long before the call: the clip is made as soon as the horn has rendered (cheap once made).
            if (!_hornClip) MakeHornClip();
            Player? me = Player.m_localPlayer;
            if (!me) return;
            UpdateCallShow(me!);
            if (_step != Step.None) Answering(me!);
            if (!_open || _step != Step.None) return;
            if (Time.time >= _until)
            {
                Close(me!);
                PlayerMessages.TopLeft(Messages.RallyCallFaded);
                return;
            }
            if (me!.IsDead()) return;
            ShowStatus(me, added: false);
            if (!CanTakeInput(me)) return;
            if (Keys.IsDownWhileMoving(PluginConfig.RallyCallShortcut.Value)) Answer(me);
            else if (Keys.IsDownWhileMoving(PluginConfig.RallyRefuseShortcut.Value)) Refuse(me);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // The Rally Call slot beside the Guardian Power, from the game's own update of that slot. It stays until the
    // player vanishes.
    internal static void UpdateHud(Hud hud)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Player? me = Player.m_localPlayer;
            bool show = _open && me && !me!.IsDead();
            // Each with its key under it.
            Slot.Update(hud, show, WeatherIcons.For(WeatherKind.RallyHorn), Messages.RallyAcceptName,
                show ? Keys.DisplayName(PluginConfig.RallyCallShortcut.Value) : string.Empty, ready: true);
            RefuseSlot.Update(hud, show, WeatherIcons.For(WeatherKind.RallyHorn), Messages.RallyRefuseName,
                show ? Keys.DisplayName(PluginConfig.RallyRefuseShortcut.Value) : string.Empty, ready: true, crossed: true);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // ---- Answering ----

    private static Vector3 Aim(Player me)
    {
        Vector3 away = me.transform.position - _center;
        away.y = 0f;
        Vector3 direction = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
        return _center + direction * FirstAim + Vector3.up * AimLift;
    }

    // What stops the teleport as at a portal, said as the game says it, or null. Checked again until the player
    // vanishes: an item picked up while kneeling counts too.
    private static string? PortalBlocked(Player me)
    {
        if (ZoneSystem.instance && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals)) return "$msg_blocked";
        return me.IsTeleportable(false) ? null : "$msg_noteleport";
    }

    // Whatever would stand in the way, said as the game says it at a portal; then down on one knee.
    private static void Answer(Player me)
    {
        if (me.IsTeleporting()) return;
        if (PortalBlocked(me) is { } blocked)
        {
            me.Message(MessageHud.MessageType.Center, blocked);
            return;
        }
        if (me.IsSensed() || me.m_lastCombatTimer < CombatSeconds)
        {
            me.Message(MessageHud.MessageType.Center, Messages.RallyCallInCombat);
            return;
        }
        if (IncursionArena.TeleportBlocked(me.transform.position, Aim(me)) is { } why)
        {
            me.Message(MessageHud.MessageType.Center, why);
            return;
        }
        _step = Step.Stopping;
        _stepUntil = Time.time + StopSeconds;
    }

    private static void Answering(Player me)
    {
        if (me.IsDead())
        {
            Abort(me, stopEmote: false);
            return;
        }
        // Picked up something that can't go through a portal (or portals were turned off): back on their feet, the
        // call still open.
        if ((_step == Step.Stopping || _step == Step.Kneeling || _step == Step.Sinking) && PortalBlocked(me) is { } blocked)
        {
            Abort(me, stopEmote: true);
            me.Message(MessageHud.MessageType.Center, blocked);
            return;
        }
        switch (_step)
        {
            case Step.Stopping:
                StopWhatTheyDo(me);
                if (ReadyToKneel(me) && me.StartEmote(KneelEmote, oneshot: false))
                {
                    Strike(me.transform.position);
                    _step = Step.Kneeling;
                    _stepUntil = Time.time + KneelSeconds;
                }
                else if (Time.time >= _stepUntil)
                {
                    Abort(me, stopEmote: false);
                    me.Message(MessageHud.MessageType.Center, Messages.RallyCallCannot);
                }
                break;
            case Step.Kneeling:
                if (Time.time < _stepUntil) break;
                // A pillar and the horn's echo left where they knelt, for everyone near, as they sink.
                KinshipRites.Show(me, WeatherKind.RallyHorn, KinshipFx.RallyLeaving);
                _sinkFrom = me.transform.position;
                _sunk = true;
                SetSolid(me, false);
                _step = Step.Sinking;
                _stepUntil = Time.time + SinkSeconds;
                break;
            case Step.Sinking:
                // Slow at first, faster as they go under.
                float sunk = 1f - Mathf.Clamp01((_stepUntil - Time.time) / SinkSeconds);
                HoldAt(me, _sinkFrom + Vector3.down * (SinkDepth * sunk * sunk));
                if (Time.time < _stepUntil) break;
                Vector3 aim = Aim(me);
                // False when it can't after all (it says why): back on their feet, the call still open.
                if (!me.TeleportTo(aim + Vector3.down * HideDepth, Facing(aim, _center), distantTeleport: true))
                {
                    Abort(me, stopEmote: true);
                    return;
                }
                _arrivalCenter = _center;
                _spotChosen = false;
                _step = Step.Teleporting;
                _stepUntil = Time.time + TeleportTimeout;
                string calledBy = _calledBy;
                Close(me);
                KinshipRites.StartWait(WeatherKind.RallyHorn, calledBy);
                PlayerMessages.TopLeft(string.Format(Messages.RallyCallAnswered, WeatherKinds.Colored(WeatherKind.RallyHorn)));
                break;
            case Step.Teleporting:
                // Held under ground until the game moves them, then solid again where it puts them.
                if (_sunk)
                {
                    if (me.IsTeleporting() && me.m_teleportTimer <= GameTeleportDelay) HoldAt(me, _sinkFrom + Vector3.down * SinkDepth);
                    else
                    {
                        _sunk = false;
                        SetSolid(me, true);
                    }
                }
                if (me.IsTeleporting())
                {
                    ChooseSpot(me);
                    if (Time.time >= _stepUntil) _step = Step.None;
                    return;
                }
                Arrive(me);
                break;
            case Step.Arrived:
                // Then they stand up by themselves, with a challenge and a horn blast.
                if (Time.time >= _stepUntil)
                {
                    Abort(me, stopEmote: true);
                    me.StartEmote(RisenEmote);
                    KinshipRites.Show(me, WeatherKind.RallyHorn, KinshipFx.RallyRisen);
                }
                // Right out of the loading screen the player may not be able to kneel yet.
                else if (!me.InEmote()) me.StartEmote(KneelEmote, oneshot: false);
                break;
        }
    }

    // Back to how it was before answering, up out of the ground if they sank; the call stays open (unless it ran out
    // meanwhile).
    private static void Abort(Player me, bool stopEmote)
    {
        _step = Step.None;
        if (_sunk)
        {
            _sunk = false;
            HoldAt(me, _sinkFrom);
            SetSolid(me, true);
        }
        if (stopEmote && me.InEmote()) me.StopEmote();
    }

    // Without a collider the player passes into the ground instead of standing on it.
    private static void SetSolid(Player me, bool solid)
    {
        if (me.m_collider) me.m_collider.enabled = solid;
    }

    // Put right there, not falling (and not counting the drop as a fall).
    private static void HoldAt(Player me, Vector3 at)
    {
        me.transform.position = at;
        me.m_maxAirAltitude = at.y;
        if (!me.m_body) return;
        me.m_body.position = at;
        me.m_body.linearVelocity = Vector3.zero;
    }

    // Off the helm or saddle, up from a seat or bed, out of their own emote.
    private static void StopWhatTheyDo(Player me)
    {
        if (me.GetDoodadController() != null) me.StopDoodadControl();
        if (me.IsAttached() && !me.m_sleeping) me.AttachStop();
        if (me.InEmote()) me.StopEmote();
    }

    private static bool ReadyToKneel(Player me) =>
        me.CanMove() && !me.InAttack() && !me.IsDrawingBow() && !me.InDodge() && !me.IsAttached() && !me.IsAttachedToShip()
        && me.GetDoodadController() == null && !me.InEmote() && me.IsOnGround();

    // In the loading screen, once the obliterator's surroundings have loaded: where to set the player down.
    private static void ChooseSpot(Player me)
    {
        if (_spotChosen || !ZNetScene.instance || !ZNetScene.instance.IsAreaReady(_arrivalCenter)) return;
        _spotChosen = true;
        // No free spot: up beside the obliterator, where the game finds the floor.
        Vector3 spot = PickSpot(out Vector3 free) ? free : Aim(me);
        me.m_teleportTargetPos = spot;
        me.m_teleportTargetRot = Facing(spot, _arrivalCenter);
    }

    // Out of the loading screen: kneeling, struck by lightning, held a moment longer.
    private static void Arrive(Player me)
    {
        // The teleport ended before a spot could be picked: moved there now.
        if (!_spotChosen && PickSpot(out Vector3 late))
        {
            me.transform.SetPositionAndRotation(late, Facing(late, _arrivalCenter));
            if (me.m_body)
            {
                me.m_body.position = late;
                me.m_body.linearVelocity = Vector3.zero;
            }
        }
        _step = Step.Arrived;
        _stepUntil = Time.time + RiseSeconds;
        if (!me.InEmote()) me.StartEmote(KneelEmote, oneshot: false);
        Strike(me.transform.position);
    }

    private static bool PickSpot(out Vector3 spot)
    {
        SoulTether.FindSpots(_arrivalCenter, Spots, PlayerRoom);
        spot = Spots.Count > 0 ? Spots[UnityEngine.Random.Range(0, Mathf.Min(Spots.Count, SpotChoices))] : Vector3.zero;
        bool found = Spots.Count > 0;
        Spots.Clear();
        return found;
    }

    // Standing at, looking toward the obliterator.
    private static Quaternion Facing(Vector3 at, Vector3 center)
    {
        Vector3 toward = center - at;
        toward.y = 0f;
        return toward.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toward.normalized) : Quaternion.identity;
    }

    // ---- The horn, for everyone ----

    // The horn (see HornSound) is rendered on a worker thread at game start; its clip is made here, on the main
    // thread, as soon as that is done, so the call itself only has to press play.
    private static AudioClip? _hornClip;
    private static AudioSource? _hornSource;
    private static bool _hornFailed;

    // The horn, for its echo where a player vanishes and its blast as they stand up (see KinshipFx).
    internal static AudioClip? HornClip => _hornClip;

    private static void MakeHornClip()
    {
        if (_hornClip || _hornFailed || !HornSound.TryGet(out float[] left, out float[] right)) return;
        // Whatever goes wrong here only costs the horn, not the rite.
        _hornFailed = true;
        try
        {
            _hornClip = KinshipFx.MakeClip("TempestRite_RallyHorn", left, right, HornSound.SampleRate);
            _hornFailed = false;
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogWarning($"The {Feature}'s horn could not be made, it sounds no horn: {exception.Message}");
        }
    }

    // Heard the same everywhere (not from a place in the world), so the whole server hears the call, in stereo. Only
    // here, never sent to others; at the game's sound effects volume.
    private static void SoundHorn()
    {
        MakeHornClip();
        if (!_hornClip) return;
        if (!_hornSource)
        {
            GameObject holder = new("TempestRite_RallyHorn");
            UnityEngine.Object.DontDestroyOnLoad(holder);
            _hornSource = holder.AddComponent<AudioSource>();
            _hornSource.playOnAwake = false;
            _hornSource.spatialBlend = 0f;
            _hornSource.volume = HornVolume;
            _hornSource.bypassReverbZones = true;
        }
        // The sound effects mixer group, taken from one of the game's own sounds.
        if (!_hornSource!.outputAudioMixerGroup) _hornSource.outputAudioMixerGroup = KinshipFx.Mixer();
        _hornSource.clip = _hornClip;
        _hornSource.Play();
    }

    // ---- Lightning, for everyone near ----

    private static void Strike(Vector3 position) =>
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.RallyArrived, position);

    private static void RPC_Strike(long sender, Vector3 position)
    {
        if (PatchGuard.IsDisabled(Feature) || !ZNet.instance || ZNet.instance.IsDedicated()) return;
        if (!PluginConfig.ShowBlessingLightning.IsOn()) return;
        try
        {
            Player? me = Player.m_localPlayer;
            if (me && Vector3.Distance(me!.transform.position, position) > StrikeRange) return;
            BlessingLightning.Strike(position);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // ---- What shows while the call is open ----

    private static readonly List<GameObject> _flourish = [];
    private static float _nextFlourish;
    private static Minimap.PinData? _pin;

    // Horns flying up from the player, and the horn sounding, every FlourishEvery seconds while the call waits for an
    // answer (see KinshipFx),
    // ended at once when they answer, refuse or it fades; and the obliterator marked on the map as the game marks a
    // raid, until then.
    private static void UpdateCallShow(Player me)
    {
        bool waiting = _open && _step == Step.None && !me.IsDead();
        if (waiting && Time.time >= _nextFlourish)
        {
            // The horn sounds again with each flourish but the first (it sounded as the call came).
            if (_nextFlourish > 0f) SoundHorn();
            _nextFlourish = Time.time + FlourishEvery;
            _flourish.Clear();
            _flourish.AddRange(KinshipFx.CallFlourish(me));
        }
        else if (!waiting && _nextFlourish > 0f)
        {
            _nextFlourish = 0f;
            foreach (GameObject shown in _flourish) KinshipFx.StopShown(shown);
            _flourish.Clear();
        }
        UpdatePin();
    }

    // The call let go on purpose: as if it faded, costing nothing.
    private static void Refuse(Player me)
    {
        Close(me);
        PlayerMessages.TopLeft(Messages.RallyCallRefused);
    }

    private static void UpdatePin()
    {
        Minimap? map = Minimap.instance;
        if (!_open || !map)
        {
            RemovePin();
            return;
        }
        if (_pin == null)
        {
            // Named as it is made: the map only shows names of pins made with one. The rite's own horn for its icon.
            _pin = map!.AddPin(_center, Minimap.PinType.RandomEvent, Messages.RallyHornPin, save: false, isChecked: false);
            if (WeatherIcons.For(WeatherKind.RallyHorn) is { } icon) _pin.m_icon = icon;
            _pin.m_animate = true;
            _pin.m_doubleSize = true;
        }
        _pin.m_pos = _center;
    }

    private static void RemovePin()
    {
        if (_pin != null && Minimap.instance) Minimap.instance.RemovePin(_pin);
        _pin = null;
    }

    // ---- The call's status ----

    // The call is over for the local player (answered, refused, faded): they owe the server no more answer.
    private static void Close(Player me)
    {
        if (_open) RallyPending.Done();
        _open = false;
        if (_effect && me) StatusCommands.RemoveQuietly(me.GetSEMan(), _effect!.NameHash());
    }

    // Its status was cleared (clearstatus): the call is let go, as if it faded. Not once the player is answering it.
    internal static void Dismiss()
    {
        Player? me = Player.m_localPlayer;
        if (!_open || _step != Step.None || !me) return;
        Close(me!);
        PlayerMessages.TopLeft(Messages.RallyCallFaded);
    }

    // Put back whenever it is missing (it goes with death), counting down with the call.
    private static void ShowStatus(Player me, bool added)
    {
        if (me.IsDead()) return;
        if (!_effect)
        {
            _effect = ScriptableObject.CreateInstance<StatusEffect>();
            _effect.name = StatusCommands.RallyCallName;
            _effect.hideFlags = HideFlags.HideAndDontSave;
        }
        _effect!.m_name = Messages.RallyCallName;
        // The HUD only lists effects with an icon.
        if (!_effect.m_icon) _effect.m_icon = WeatherIcons.For(WeatherKind.RallyHorn);
        SEMan seMan = me.GetSEMan();
        StatusEffect? current = seMan.GetStatusEffect(_effect.NameHash());
        if (!current)
        {
            current = seMan.AddStatusEffect(_effect, true);
            added = true;
        }
        if (!current) return;
        current!.m_ttl = _duration;
        current.m_time = _duration - LeftSeconds;
        if (added)
        {
            string who = string.IsNullOrEmpty(_calledBy) ? Messages.SomeoneName : _calledBy;
            current.m_tooltip = string.Format(Messages.StatusTooltipRallyCall, who, KeyText,
                TimeText.Duration((PluginConfig.For(WeatherKind.RallyHorn)?.Seconds) ?? 0f), RefuseKeyText);
        }
    }

    // ---- Input ----

    // Not while typing, in a menu or the map, asleep, or in a cutscene.
    internal static bool CanTakeInput(Player me)
    {
        if (!Hud.instance || !Hud.instance.m_rootObject || !Hud.instance.m_rootObject.activeInHierarchy) return false;
        if (Minimap.IsOpen() || me.IsSleeping() || me.InCutscene()) return false;
        if ((Chat.instance && Chat.instance.HasFocus()) || global::Console.IsVisible() || TextInput.IsVisible() || Menu.IsVisible()
            || InventoryGui.IsVisible() || StoreGui.IsVisible()) return false;
        GameObject? selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (!selected) return true;
        TMP_InputField? field = selected!.GetComponent<TMP_InputField>();
        return !(field && field!.isFocused);
    }
}
