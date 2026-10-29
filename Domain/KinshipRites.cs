using System;
using System.Collections.Generic;
using System.Globalization;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// Kinship rites: given to everyone on the server at once, wherever they are. The server tells every player (see
// WeatherSync); each game works it out for its own player, and tells everyone to show the effect on them (a glow and
// a fire, lightning, or an anvil and sparks), so all who have that player loaded see it.
// - Skål: the player raises a toast amid a stamina mead's glow and the crackle of a fire, and is Rested as if at
//   comfort 3 for the rite's time. A player rested longer already is passed by.
// - Sky-Call: a player whose forsaken power is cooling down is struck by lightning (only a picture), roars, and has it
//   ready again. A player whose power is ready already (or who has none) is passed by.
// - Anvil-Spark: an anvil rings and sparks burst around the player, who flexes and has every worn item in their
//   inventory repaired. A player with nothing to repair is passed by.
// Whatever the player is doing, they stop for it: they stop moving, stand up from a seat or bed, let go of a ship's
// helm or a saddle, stop their own emote, lower their shield, put away a drawn bow (without loosing it) and finish a
// swing already underway; then they do the emote, the effect showing on them as it starts. Until the emote is over,
// they're held still and nobody can hurt them. Once a rite gave a player something, it can't again for a while: a wait for
// each rite, shown as a status and kept through death and logging out. A player passed by gets no wait.
internal static class KinshipRites
{
    private const string Feature = "Kinship rites";
    private const float SaveInterval = 5f;

    // Where each rite's wait is saved with the character (the Sky-Call Rite's from before the others had one).
    private static readonly Dictionary<WeatherKind, string> SaveKeys = new()
    {
        [WeatherKind.Skal] = "TempestRite_Skal",
        [WeatherKind.SkyCall] = "TempestRite_SkyCall",
        [WeatherKind.AnvilSpark] = "TempestRite_AnvilSpark"
    };

    // The Rested status the Skål Rite gives is the one for this comfort.
    private const int SkalComfort = 3;
    // The mead whose glow (its status effect's start effects) the Skål Rite shows on each player.
    private const string GlowItem = "MeadStaminaMinor";
    private const string ToastEmote = "toast";
    private const string RoarEmote = "roar";
    private const string FlexEmote = "flex";
    // An emote holds the player still and unhurt for at most this long, ending sooner if the emote does.
    private const float EmoteSeconds = 7f;
    // The emote takes a moment to start.
    private const float EmoteStart = 0.75f;
    // How long the player has to stop what they're doing (a swing, a dodge, standing up...) before the emote. One who
    // still can't by then (staggered, teleporting, too heavy to move...) gets the effect without the emote.
    private const float StopSeconds = 4f;
    // How long the fire crackles around a player who drinks, the last part fading out.
    private const float CrackleSeconds = 6f;
    private const float CrackleFade = 2f;
    private const float CracklePitch = 0.85f;
    private const string FirePrefab = "fire_pit";
    // The Anvil-Spark Rite: the forge's own repair sound (hammer on anvil), and the sparks of a hit on metal, a burst of
    // them around the player's chest.
    private const string ForgePrefab = "forge";
    private const string SparksPrefab = "vfx_HitSparks";
    private const int SparkCount = 7;
    private const float SparkHeight = 1.2f;
    private const float SparkSpread = 0.6f;

    private sealed class Wait
    {
        internal float EndTime;
        internal float Duration;
        internal string CalledBy = string.Empty;
    }

    // An emote still to do, and the rite whose effect shows on the player as it starts.
    private sealed class PendingEmote(string emote, WeatherKind kind)
    {
        internal string Emote { get; } = emote;
        internal WeatherKind Kind { get; } = kind;
    }

    private sealed class Crackle(AudioSource source, float start, float volume)
    {
        internal AudioSource Source { get; } = source;
        internal float Start { get; } = start;
        internal float Volume { get; } = volume;
    }

    private static readonly Dictionary<WeatherKind, Wait> Waits = [];
    private static Player? _owner;
    private static float _nextSave;
    private static bool _changed;
    // The rite emote going on: from when it shows, until when at most.
    private static float _emoteFrom = -1f;
    private static float _emoteUntil;
    // Emotes still to do, one after another, the first not before _nextEmoteAt. _stopBy is when the player must have
    // stopped what they're doing for the next one (-1 until they start to).
    private static readonly Queue<PendingEmote> PendingEmotes = new();
    private static float _nextEmoteAt;
    private static float _stopBy = -1f;
    private static readonly List<Crackle> Crackles = [];
    private static readonly List<ItemDrop.ItemData> Worn = [];

    internal static void Register(ZRoutedRpc rpc) => rpc.Register<ZPackage>(Rpcs.KinshipShown, RPC_Shown);

    internal static void ClearSession()
    {
        Waits.Clear();
        _owner = null;
        _nextSave = 0f;
        _changed = false;
        _emoteFrom = -1f;
        _emoteUntil = 0f;
        PendingEmotes.Clear();
        _nextEmoteAt = 0f;
        _stopBy = -1f;
        foreach (Crackle crackle in Crackles)
            if (crackle.Source) UnityEngine.Object.Destroy(crackle.Source.gameObject);
        Crackles.Clear();
        _fire = null;
        _forge = null;
    }

    // What a kinship rite leaves that lasts: the wait until it can give the local player something again.
    internal static bool IsActive(WeatherKind kind) =>
        Waits.TryGetValue(kind, out Wait wait) && Time.time < wait.EndTime && Player.m_localPlayer;

    internal static float Remaining(WeatherKind kind) => IsActive(kind) ? Waits[kind].EndTime - Time.time : 0f;

    internal static float Duration(WeatherKind kind) => IsActive(kind) ? Waits[kind].Duration : 0f;

    internal static string CalledBy(WeatherKind kind) => IsActive(kind) ? Waits[kind].CalledBy : string.Empty;

    // How long a rite can't give a player anything again after it did, in seconds. The Skål Rite's time is the Rested
    // it gives, so its wait has a setting of its own.
    private static float WaitSeconds(WeatherKind kind) => kind == WeatherKind.Skal
        ? PluginConfig.SkalCooldown.Value * 60f
        : PluginConfig.For(kind)?.Seconds ?? 0f;

    private static void StartWait(WeatherKind kind, string calledBy)
    {
        float seconds = WaitSeconds(kind);
        if (seconds <= 0f) return;
        Waits[kind] = new Wait { EndTime = Time.time + seconds, Duration = seconds, CalledBy = calledBy };
        _changed = true;
    }

    // Whether the local player is taken up by a kinship rite now: stopping what they do for its emote, or doing it.
    // Meanwhile they're held still and can't be hurt.
    internal static bool Holds(Character character) =>
        character == Player.m_localPlayer && (PendingEmotes.Count > 0 || InRiteEmote((Player)character));

    internal static bool Shielded(Character character) => Holds(character);

    private static bool InRiteEmote(Player me)
    {
        if (_emoteFrom < 0f) return false;
        if (Time.time < _emoteUntil && (Time.time < _emoteFrom || me.InEmote())) return true;
        // The emote has ended (or was cut short).
        _emoteFrom = -1f;
        return false;
    }

    // Each step while held: whatever the player is doing stops. Called with their controls, which are then ignored.
    internal static void Settle(Player me)
    {
        me.m_autoRun = false;
        me.m_blocking = false;
        me.m_queuedAttackTimer = 0f;
        me.m_queuedSecondAttackTimer = 0f;
        me.m_queuedDodgeTimer = 0f;
        // A drawn bow is lowered without loosing the arrow, as when the player raises their shield.
        if (me.m_attackDrawTime > 0f)
        {
            me.m_attackDrawTime = -1f;
            string? draw = me.GetCurrentWeapon()?.m_shared.m_attack.m_drawAnimationState;
            if (!string.IsNullOrEmpty(draw)) me.m_zanim.SetBool(draw, false);
        }
        if (PendingEmotes.Count == 0 || InRiteEmote(me)) return;
        // Getting ready for the next emote: off the helm or saddle, up from a seat or bed (not while asleep), out of
        // their own emote (sitting on the ground, dancing...).
        if (me.GetDoodadController() != null) me.StopDoodadControl();
        if (me.IsAttached() && !me.m_sleeping) me.AttachStop();
        me.StopEmote();
    }

    // Whether the player has stopped everything an emote can't start during.
    private static bool ReadyToEmote(Player me) =>
        me.CanMove() && !me.InAttack() && !me.IsDrawingBow() && !me.InDodge() && !me.IsAttached() && !me.IsAttachedToShip()
        && me.GetDoodadController() == null && !me.InEmote();

    // One burn performed these kinship rites; the local player receives them all.
    internal static void Performed(IReadOnlyList<WeatherKind> kinds, bool byLocalPlayer, string calledBy)
    {
        if (kinds.Count == 0 || PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Player? me = Player.m_localPlayer;
            if (!me || me!.IsDead()) return;
            string who = byLocalPlayer ? Messages.YouName : string.IsNullOrEmpty(calledBy) ? Messages.SomeoneName : calledBy;
            foreach (WeatherKind kind in kinds)
            {
                string what = WeatherKinds.Colored(kind);
                string message = kind switch
                {
                    WeatherKind.Skal => Skal(me, calledBy, who, what),
                    WeatherKind.SkyCall => SkyCall(me, calledBy, who, what),
                    WeatherKind.AnvilSpark => AnvilSpark(me, calledBy, who, what),
                    _ => string.Empty
                };
                if (message.Length > 0) PlayerMessages.Center(message);
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static string Waiting(string format, WeatherKind kind, string who, string what) =>
        string.Format(format, who, what, TimeText.Duration(Remaining(kind)));

    // Each returns the message for the local player.
    private static string Skal(Player me, string calledBy, string who, string what)
    {
        if (IsActive(WeatherKind.Skal)) return Waiting(Messages.SkalWaiting, WeatherKind.Skal, who, what);
        float seconds = PluginConfig.For(WeatherKind.Skal)?.Seconds ?? 0f;
        if (RestedLeft(me) >= seconds) return string.Format(Messages.SkalRestedAlready, who, what);

        Rest(me, seconds);
        StartWait(WeatherKind.Skal, calledBy);
        QueueEmote(ToastEmote, WeatherKind.Skal);
        return string.Format(Messages.SkalReceived, who, what);
    }

    private static float RestedLeft(Player me)
    {
        StatusEffect? rested = me.GetSEMan().GetStatusEffect(SEMan.s_statusEffectRested);
        return rested ? rested!.m_ttl - rested.m_time : 0f;
    }

    // Rested as if at comfort 3 (the game's message says so), for at least the given time.
    private static void Rest(Player me, float seconds)
    {
        SEMan seman = me.GetSEMan();
        int comfort = me.m_comfortLevel;
        me.m_comfortLevel = Mathf.Max(comfort, SkalComfort);
        try { seman.AddStatusEffect(SEMan.s_statusEffectRested, resetTime: true); }
        // The game works the player's own comfort out again every couple of seconds anyway.
        finally { me.m_comfortLevel = comfort; }
        StatusEffect? rested = seman.GetStatusEffect(SEMan.s_statusEffectRested);
        if (!rested || seconds <= rested!.m_ttl - rested.m_time) return;
        rested.m_ttl = seconds;
        rested.m_time = 0f;
    }

    private static string SkyCall(Player me, string calledBy, string who, string what)
    {
        if (!me.m_guardianSE) return string.Format(Messages.SkyCallNoPower, who, what);
        if (IsActive(WeatherKind.SkyCall)) return Waiting(Messages.SkyCallWaiting, WeatherKind.SkyCall, who, what);
        if (me.m_guardianPowerCooldown <= 0f) return string.Format(Messages.SkyCallReady, who, what);

        me.m_guardianPowerCooldown = 0f;
        StartWait(WeatherKind.SkyCall, calledBy);
        QueueEmote(RoarEmote, WeatherKind.SkyCall);
        return string.Format(Messages.SkyCallRenewed, who, what);
    }

    // Every worn item in the inventory, equipped ones too, back to full, as the game repairs at a workbench or forge
    // (only items that can be repaired at all), but needing neither.
    private static string AnvilSpark(Player me, string calledBy, string who, string what)
    {
        if (IsActive(WeatherKind.AnvilSpark)) return Waiting(Messages.AnvilSparkWaiting, WeatherKind.AnvilSpark, who, what);
        Worn.Clear();
        me.GetInventory().GetWornItems(Worn);
        int repaired = 0;
        foreach (ItemDrop.ItemData item in Worn)
        {
            if (!item.m_shared.m_canBeReparied) continue;
            item.m_durability = item.GetMaxDurability();
            repaired++;
        }
        Worn.Clear();
        if (repaired == 0) return string.Format(Messages.AnvilSparkNothing, who, what);

        StartWait(WeatherKind.AnvilSpark, calledBy);
        QueueEmote(FlexEmote, WeatherKind.AnvilSpark);
        return string.Format(Messages.AnvilSparkRepaired, who, what, repaired);
    }

    // When one burn performs several, the emotes follow one another: toast, roar, flex.
    private static void QueueEmote(string emote, WeatherKind kind)
    {
        if (PendingEmotes.Count == 0)
        {
            _nextEmoteAt = Time.time;
            _stopBy = -1f;
        }
        PendingEmotes.Enqueue(new PendingEmote(emote, kind));
    }

    // Starts the next emote once the last one is over and the player has stopped what they were doing, showing the
    // rite's effect on them. A player who couldn't stop in time gets only the effect.
    private static void NextEmote(Player me)
    {
        if (Time.time < _nextEmoteAt || InRiteEmote(me)) return;
        if (_stopBy < 0f) _stopBy = Time.time + StopSeconds;
        bool ready = !me.IsDead() && ReadyToEmote(me);
        if (!ready && Time.time < _stopBy && !me.IsDead()) return;

        PendingEmote next = PendingEmotes.Dequeue();
        _nextEmoteAt = Time.time + EmoteStart;
        _stopBy = -1f;
        if (ready && me.StartEmote(next.Emote))
        {
            _emoteFrom = Time.time + EmoteStart;
            _emoteUntil = Time.time + EmoteSeconds;
        }
        Show(me, next.Kind);
    }

    // ---- Everyone sees it ----

    // Tells everyone (this game too) to show the rite's effect on the local player.
    private static void Show(Player me, WeatherKind kind)
    {
        if (ZRoutedRpc.instance == null || !me.m_nview || !me.m_nview.IsValid()) return;
        ZPackage package = new();
        package.Write(me.m_nview.GetZDO().m_uid);
        package.Write((int)kind);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.KinshipShown, package);
    }

    private static void RPC_Shown(long sender, ZPackage package)
    {
        if (PatchGuard.IsDisabled(Feature) || !ZNetScene.instance || !ZNet.instance || ZNet.instance.IsDedicated()) return;
        try
        {
            ZDOID id = package.ReadZDOID();
            WeatherKind kind = (WeatherKind)package.ReadInt();
            // Only a player's own game shows effects on them.
            ZDO? zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null || zdo.GetOwner() != sender) return;
            GameObject? instance = ZNetScene.instance.FindInstance(id);
            Player? player = instance ? instance!.GetComponent<Player>() : null;
            if (!player || player!.IsDead()) return;
            if (kind == WeatherKind.Skal)
            {
                Glow(player);
                StartCrackle(player);
            }
            else if (kind == WeatherKind.SkyCall && PluginConfig.ShowBlessingLightning.IsOn())
                BlessingLightning.Strike(player.transform.position);
            else if (kind == WeatherKind.AnvilSpark)
            {
                AnvilRing(player);
                SparkBurst(player);
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Makes the effects of a list that aren't sent to others (each game makes its own), at the player.
    private static void Spawn(EffectList effects, Player player, float lifetime, bool spatial)
    {
        foreach (EffectList.EffectData data in effects.m_effectPrefabs)
        {
            GameObject made = data.m_prefab;
            if (!data.m_enabled || !made || made.GetComponent<ZNetView>()) continue;
            Transform at = player.transform;
            GameObject shown = data.m_attach
                ? UnityEngine.Object.Instantiate(made, at.position, at.rotation, at)
                : UnityEngine.Object.Instantiate(made, at.position, at.rotation);
            // A crafting station's sounds are heard as if beside you: heard from where the player stands instead.
            if (spatial)
                foreach (AudioSource source in shown.GetComponentsInChildren<AudioSource>(true))
                    source.spatialBlend = 1f;
            UnityEngine.Object.Destroy(shown, lifetime);
        }
    }

    private static Fireplace? _fire;
    private static CraftingStation? _forge;

    // The campfire, for its crackle.
    private static Fireplace? Fire()
    {
        if (_fire) return _fire;
        GameObject? prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(FirePrefab) : null;
        _fire = prefab ? prefab!.GetComponent<Fireplace>() : null;
        if (!_fire) Plugin.ModLogger.LogWarning($"{FirePrefab} not found: the Skål Rite plays no fire.");
        return _fire;
    }

    // The forge, for the ring of its anvil.
    private static CraftingStation? Forge()
    {
        if (_forge) return _forge;
        GameObject? prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(ForgePrefab) : null;
        _forge = prefab ? prefab!.GetComponent<CraftingStation>() : null;
        if (!_forge) Plugin.ModLogger.LogWarning($"{ForgePrefab} not found: the Anvil-Spark Rite plays no anvil.");
        return _forge;
    }

    // The stamina mead's glow on the player, as when they drink one.
    private static void Glow(Player player)
    {
        GameObject? prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(GlowItem) : null;
        StatusEffect? effect = prefab && prefab!.GetComponent<ItemDrop>() is { } drop ? drop.m_itemData.m_shared.m_consumeStatusEffect : null;
        if (!effect)
        {
            Plugin.ModLogger.LogWarning($"{GlowItem} not found: the Skål Rite shows no glow.");
            return;
        }
        Spawn(effect!.m_startEffects, player, EmoteSeconds, spatial: false);
    }

    // The anvil ringing as when gear is repaired at the forge, heard from where the player stands.
    private static void AnvilRing(Player player)
    {
        if (Forge() is { } forge) Spawn(forge.m_repairItemDoneEffects, player, EmoteSeconds, spatial: true);
    }

    // Sparks as of a hammer on metal, bursting all around the player's chest.
    private static void SparkBurst(Player player)
    {
        GameObject? sparks = ZNetScene.instance.GetPrefab(SparksPrefab);
        if (!sparks || sparks!.GetComponent<ZNetView>())
        {
            Plugin.ModLogger.LogWarning($"{SparksPrefab} not found: the Anvil-Spark Rite shows no sparks.");
            return;
        }
        Vector3 chest = player.transform.position + Vector3.up * SparkHeight;
        for (int i = 0; i < SparkCount; i++)
        {
            // Spread evenly around the player, a little up or down, each facing outward.
            float angle = (i + UnityEngine.Random.value * 0.5f) * Mathf.PI * 2f / SparkCount;
            Vector3 outward = new(Mathf.Cos(angle), UnityEngine.Random.Range(-0.2f, 0.6f), Mathf.Sin(angle));
            Vector3 at = chest + new Vector3(outward.x, outward.y * 0.5f, outward.z) * SparkSpread;
            UnityEngine.Object.Destroy(UnityEngine.Object.Instantiate(sparks, at, Quaternion.LookRotation(outward)), EmoteSeconds);
        }
    }

    // The campfire's crackle, a little lower, following the player for a few seconds.
    private static void StartCrackle(Player player)
    {
        Fireplace? fire = Fire();
        if (fire == null) return;
        AudioSource? loop = null;
        AudioClip? clip = null;
        foreach (AudioSource source in fire.GetComponentsInChildren<AudioSource>(true))
        {
            ZSFX? sfx = source.GetComponent<ZSFX>();
            clip = sfx && sfx!.m_audioClips.Length > 0 ? sfx.m_audioClips[0] : source.clip;
            if (!clip || !source.loop) continue;
            loop = source;
            break;
        }
        if (!loop) return;

        GameObject holder = new("TempestRite_SkalFire");
        holder.transform.SetParent(player.transform, false);
        holder.transform.localPosition = Vector3.up;
        AudioSource crackle = holder.AddComponent<AudioSource>();
        crackle.clip = clip;
        crackle.outputAudioMixerGroup = loop!.outputAudioMixerGroup;
        crackle.loop = true;
        crackle.spatialBlend = 1f;
        crackle.rolloffMode = loop.rolloffMode;
        crackle.minDistance = loop.minDistance;
        crackle.maxDistance = loop.maxDistance;
        crackle.pitch = CracklePitch;
        crackle.volume = loop.volume;
        crackle.Play();
        Crackles.Add(new Crackle(crackle, Time.time, loop.volume));
        UnityEngine.Object.Destroy(holder, CrackleSeconds + 0.5f);
    }

    private static void FadeCrackles()
    {
        for (int i = Crackles.Count - 1; i >= 0; i--)
        {
            Crackle crackle = Crackles[i];
            float left = crackle.Start + CrackleSeconds - Time.time;
            if (!crackle.Source || left <= 0f)
            {
                if (crackle.Source) UnityEngine.Object.Destroy(crackle.Source.gameObject);
                Crackles.RemoveAt(i);
                continue;
            }
            crackle.Source.volume = crackle.Volume * Mathf.Clamp01(left / CrackleFade);
        }
    }

    // ---- Every frame ----

    private static readonly List<WeatherKind> Ended = [];

    // Picks up the character's saved waits, says when one ends, writes them down; does the rites' emotes; fades the fire.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            if (Crackles.Count > 0) FadeCrackles();
            Player? player = Player.m_localPlayer;
            if (player != _owner)
            {
                _owner = player;
                Waits.Clear();
                PendingEmotes.Clear();
                if (player) Load(player!);
            }
            if (!player) return;
            if (PendingEmotes.Count > 0) NextEmote(player!);
            foreach (KeyValuePair<WeatherKind, Wait> pair in Waits)
                if (Time.time >= pair.Value.EndTime) Ended.Add(pair.Key);
            foreach (WeatherKind kind in Ended)
            {
                Waits.Remove(kind);
                _changed = true;
                PlayerMessages.TopLeft(kind switch
                {
                    WeatherKind.Skal => Messages.SkalEnded,
                    WeatherKind.AnvilSpark => Messages.AnvilSparkEnded,
                    _ => Messages.SkyCallEnded
                });
            }
            Ended.Clear();
            if (_changed || Time.time >= _nextSave) Save(player!);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Each saved with the character as "remaining,duration,calledBy".
    private static void Save(Player player)
    {
        _changed = false;
        _nextSave = Time.time + SaveInterval;
        foreach (KeyValuePair<WeatherKind, string> pair in SaveKeys)
        {
            float remaining = Waits.TryGetValue(pair.Key, out Wait wait) ? wait.EndTime - Time.time : 0f;
            if (remaining <= 0f) player.m_customData.Remove(pair.Value);
            else player.m_customData[pair.Value] = Format(remaining, wait!.Duration, wait.CalledBy);
        }
    }

    private static void Load(Player player)
    {
        foreach (KeyValuePair<WeatherKind, string> pair in SaveKeys)
        {
            if (!player.m_customData.TryGetValue(pair.Value, out string text)
                || !TryParse(text, out float remaining, out float duration, out string calledBy)) continue;
            Waits[pair.Key] = new Wait { EndTime = Time.time + remaining, Duration = duration, CalledBy = calledBy };
        }
    }

    internal static string Format(float remaining, float duration, string calledBy) =>
        remaining.ToString("0.#", CultureInfo.InvariantCulture) + "," + duration.ToString("0.#", CultureInfo.InvariantCulture) + "," +
        calledBy.Replace(",", " ").Replace("\n", " ");

    internal static bool TryParse(string text, out float remaining, out float duration, out string calledBy)
    {
        remaining = duration = 0f;
        calledBy = string.Empty;
        string[] parts = text.Split([','], 3);
        if (parts.Length < 3
            || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out remaining)
            || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out duration)
            || remaining <= 0f) return false;
        duration = Mathf.Max(duration, remaining);
        calledBy = parts[2];
        return true;
    }
}
