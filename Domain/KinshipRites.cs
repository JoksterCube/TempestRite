using System;
using System.Collections.Generic;
using System.Globalization;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// Kinship rites: given to everyone on the server at once, wherever they are. The server tells every player (see
// WeatherSync); each game works it out for its own player, and tells everyone to show the effect on them (lights,
// embers, sounds..., see KinshipFx), so all who have that player loaded see it. A pillar of embers rises from the obliterator.
// - Skål: the player raises a toast amid a stamina mead's glow and the crackle of a fire, and is Rested as if at
//   comfort 3 for the rite's time. A player rested longer already is passed by.
// - Sky-Call: a player whose forsaken power is cooling down is struck by lightning (only a picture), roars, and has it
//   ready again. A player whose power is ready already (or who has none) is passed by.
// - Anvil-Spark: an anvil rings and sparks burst around the player, who flexes and has every worn item in their
//   inventory repaired. A player with nothing to repair is passed by.
// - Eldhrímnir: the player cheers amid a health mead's glow and the sound of eating, and every food they have eaten is
//   fresh again, as if just eaten. A player who has eaten nothing, or whose foods are all still fresh (none used up
//   more than the Free Use Multiplier), is passed by.
// - Rally-Horn: no emote; everyone away from the obliterator is offered a call to answer (see RallyHorn). Only
//   answering it starts the wait.
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
        [WeatherKind.AnvilSpark] = "TempestRite_AnvilSpark",
        [WeatherKind.Eldhrimnir] = "TempestRite_Eldhrimnir",
        [WeatherKind.RallyHorn] = "TempestRite_RallyHorn"
    };

    // The Rested status the Skål Rite gives is the one for this comfort.
    private const int SkalComfort = 3;
    private const string ToastEmote = "toast";
    private const string RoarEmote = "roar";
    private const string FlexEmote = "flex";
    private const string CheerEmote = "cheer";
    // An emote holds the player still and unhurt for at most this long, ending sooner if the emote does.
    private const float EmoteSeconds = 7f;
    // The emote takes a moment to start.
    private const float EmoteStart = 0.75f;
    // How long the player has to stop what they're doing (a swing, a dodge, standing up...) before the emote. One who
    // still can't by then (staggered, teleporting, too heavy to move...) gets the effect without the emote.
    private const float StopSeconds = 4f;
    private sealed class Wait
    {
        internal float EndTime;
        internal float Duration;
        internal string CalledBy = string.Empty;
    }

    // An emote still to do, and the rite whose effect shows on the player as it starts.
    private sealed class PendingEmote(string emote, WeatherKind kind, int detail)
    {
        internal string Emote { get; } = emote;
        internal WeatherKind Kind { get; } = kind;
        internal int Detail { get; } = detail;
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
        KinshipFx.ClearSession();
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

    internal static void StartWait(WeatherKind kind, string calledBy)
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
    // position: the obliterator it burnt in.
    internal static void Performed(IReadOnlyList<WeatherKind> kinds, Vector3 position, bool byLocalPlayer, string calledBy)
    {
        if (kinds.Count == 0 || PatchGuard.IsDisabled(Feature)) return;
        try
        {
            Player? me = Player.m_localPlayer;
            if (!me || me!.IsDead())
            {
                foreach (WeatherKind kind in kinds) if (kind == WeatherKind.RallyHorn) RallyPending.Done();
                return;
            }
            // Each rite's own pillar at the obliterator. (The war-drum, KinshipFx.Drum, is off for now.)
            foreach (WeatherKind kind in kinds)
            {
                KinshipFx.Pillar(kind, position);
                string line = kind switch
                {
                    WeatherKind.Skal => Skal(me, calledBy),
                    WeatherKind.SkyCall => SkyCall(me, calledBy),
                    WeatherKind.AnvilSpark => AnvilSpark(me, calledBy),
                    WeatherKind.Eldhrimnir => Eldhrimnir(me, calledBy),
                    WeatherKind.RallyHorn => RallyHorn.Called(me, position, byLocalPlayer, calledBy),
                    _ => string.Empty
                };
                // What it did to the player is the burn's message, over any other rite's (see RiteNews).
                if (line.Length > 0) RiteNews.Add(RiteNews.Told(kind, byLocalPlayer, calledBy, line), RiteNews.Weight.Kinship);
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Each returns what it did to the local player, said after the rite's name.
    private static string Skal(Player me, string calledBy)
    {
        if (IsActive(WeatherKind.Skal)) return Messages.SkalWaiting;
        float seconds = PluginConfig.For(WeatherKind.Skal)?.Seconds ?? 0f;
        if (RestedLeft(me) >= seconds) return Messages.SkalRestedAlready;

        Rest(me, seconds);
        StartWait(WeatherKind.Skal, calledBy);
        QueueEmote(ToastEmote, WeatherKind.Skal);
        return Messages.SkalReceived;
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

    private static string SkyCall(Player me, string calledBy)
    {
        if (!me.m_guardianSE) return Messages.SkyCallNoPower;
        if (IsActive(WeatherKind.SkyCall)) return Messages.SkyCallWaiting;
        if (me.m_guardianPowerCooldown <= 0f) return Messages.SkyCallReady;

        me.m_guardianPowerCooldown = 0f;
        StartWait(WeatherKind.SkyCall, calledBy);
        QueueEmote(RoarEmote, WeatherKind.SkyCall, me.m_guardianSE.NameHash());
        return Messages.SkyCallRenewed;
    }

    // Every worn item in the inventory, equipped ones too, back to full, as the game repairs at a workbench or forge
    // (only items that can be repaired at all), but needing neither.
    private static string AnvilSpark(Player me, string calledBy)
    {
        if (IsActive(WeatherKind.AnvilSpark)) return Messages.AnvilSparkWaiting;
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
        if (repaired == 0) return Messages.AnvilSparkNothing;

        StartWait(WeatherKind.AnvilSpark, calledBy);
        QueueEmote(FlexEmote, WeatherKind.AnvilSpark);
        return Messages.AnvilSparkRepaired;
    }

    // Every food the player has eaten fresh again, all it gives back to full, as when the game lets a food be eaten
    // again. Only if some food had more than the Free Use Multiplier of its time used up: otherwise the player is passed by.
    private static string Eldhrimnir(Player me, string calledBy)
    {
        List<Player.Food> foods = me.GetFoods();
        if (foods.Count == 0) return Messages.EldhrimnirNoFood;
        if (IsActive(WeatherKind.Eldhrimnir)) return Messages.EldhrimnirWaiting;

        float freeUse = Mathf.Clamp01(PluginConfig.EldhrimnirFreeUse.Value);
        bool used = false;
        foreach (Player.Food food in foods)
        {
            ItemDrop.ItemData.SharedData shared = food.m_item.m_shared;
            if (shared.m_foodBurnTime > 0f && 1f - food.m_time / shared.m_foodBurnTime > freeUse) used = true;
        }
        if (!used) return Messages.EldhrimnirFresh;

        foreach (Player.Food food in foods)
        {
            ItemDrop.ItemData.SharedData shared = food.m_item.m_shared;
            food.m_time = shared.m_foodBurnTime;
            food.m_health = shared.m_food;
            food.m_stamina = shared.m_foodStamina;
            food.m_eitr = shared.m_foodEitr;
        }
        me.UpdateFood(0f, forceUpdate: true);

        StartWait(WeatherKind.Eldhrimnir, calledBy);
        QueueEmote(CheerEmote, WeatherKind.Eldhrimnir, foods.Count);
        return Messages.EldhrimnirReceived;
    }

    // One burn performs one kinship rite, but burns close together (at different obliterators) can overlap: their
    // emotes follow one another.
    private static void QueueEmote(string emote, WeatherKind kind, int detail = 0)
    {
        if (PendingEmotes.Count == 0)
        {
            _nextEmoteAt = Time.time;
            _stopBy = -1f;
        }
        PendingEmotes.Enqueue(new PendingEmote(emote, kind, detail));
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
        Show(me, next.Kind, next.Detail);
    }

    // ---- Everyone sees it ----

    // Tells everyone (this game too) to show the rite's effect on the local player (see KinshipFx). detail: what the
    // rite needs to show it (the Sky-Call Rite's forsaken power, the Eldhrímnir Rite's number of foods, the Rally-Horn
    // Rite's moment).
    internal static void Show(Player me, WeatherKind kind, int detail)
    {
        if (ZRoutedRpc.instance == null || !me.m_nview || !me.m_nview.IsValid()) return;
        ZPackage package = new();
        package.Write(me.m_nview.GetZDO().m_uid);
        package.Write((int)kind);
        package.Write(detail);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.KinshipShown, package);
    }

    private static void RPC_Shown(long sender, ZPackage package)
    {
        if (PatchGuard.IsDisabled(Feature) || !ZNetScene.instance || !ZNet.instance || ZNet.instance.IsDedicated()) return;
        try
        {
            ZDOID id = package.ReadZDOID();
            WeatherKind kind = (WeatherKind)package.ReadInt();
            int detail = package.ReadInt();
            // Only a player's own game shows effects on them.
            ZDO? zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null || zdo.GetOwner() != sender) return;
            GameObject? instance = ZNetScene.instance.FindInstance(id);
            Player? player = instance ? instance!.GetComponent<Player>() : null;
            if (!player || player!.IsDead()) return;
            KinshipFx.Show(player, kind, detail);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // ---- Every frame ----

    private static readonly List<WeatherKind> Ended = [];

    // Picks up the character's saved waits, says when one ends, writes them down; does the rites' emotes and their show.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            KinshipFx.Update();
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
                PlayerMessages.TopLeft(EndedMessage(kind));
            }
            Ended.Clear();
            if (_changed || Time.time >= _nextSave) Save(player!);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Its status was cleared (clearstatus): the wait is over now.
    internal static void EndWait(WeatherKind kind)
    {
        if (!Waits.Remove(kind)) return;
        _changed = true;
        PlayerMessages.TopLeft(EndedMessage(kind));
    }

    private static string EndedMessage(WeatherKind kind) => kind switch
    {
        WeatherKind.Skal => Messages.SkalEnded,
        WeatherKind.AnvilSpark => Messages.AnvilSparkEnded,
        WeatherKind.Eldhrimnir => Messages.EldhrimnirEnded,
        WeatherKind.RallyHorn => Messages.RallyHornEnded,
        _ => Messages.SkyCallEnded
    };

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
