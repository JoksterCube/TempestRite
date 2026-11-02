using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace JoksterCube.TempestRite.Domain;

// What the kinship rites look and sound like. Each game makes all of it itself (nothing here is sent to others): the
// rite tells everyone to show it on a player (see KinshipRites.Show), and each game plays it on that player.
// - Burnt: near the obliterator a column of embers in the rite's colour rises and a light flares. (A war-drum heard by
//   everyone, DrumSound, is made but off for now.)
// - Each rite has a colour of its own: Skål firelight orange, Sky-Call purple, Anvil-Spark red, Eldhrímnir gold,
//   Rally-Horn gold.
// - Skål: the Brew of Animal Whispers' effect in firelight colours, Rested signs rising instead of its hearts; a fire
//   crackling and flickering firelight; as the toast is raised the fire flares up (the campfire's add-wood whoosh, a
//   bright flash and a column of embers).
// - Sky-Call: lightning (or only its thunder, with Show Blessing Lightning off), the camera shakes, a purple flash and
//   sparkles, then the player's own forsaken power bursts on them as when it is used; their power's icon pulses.
// - Anvil-Spark: a few repair hammers slowly circling up (see KinshipMotes) and the Anti-Sting Concoction's effect in
//   red; the forge's repair sound twice (two different ones), each with the forge's sparks and a flash, the second
//   bigger, with sparks on both hands and a small shake.
// - Eldhrímnir: the Love Potion's (troll pheromones) effect in gold, cooked meat, bread and onion soup instead of its
//   hearts, a gold light, then a bite for each food made fresh, its food slot pulsing as it is heard.
// - Rally-Horn: the Accept slot pulses when the call comes; a gold pillar and the horn's echo where a player
//   sinks into the ground; a horn blast and a gold flash as they stand up at the obliterator.
internal static class KinshipFx
{
    private const string Feature = "Kinship rites show";

    // What the Rally-Horn Rite shows (its detail): a player leaving, or standing up at the obliterator.
    internal const int RallyLeaving = 1;
    internal const int RallyRisen = 2;

    private static readonly Color SkalColor = new(1f, 0.55f, 0.18f);
    private static readonly Color SkyCallColor = new(0.62f, 0.32f, 1f);
    private static readonly Color AnvilColor = new(1f, 0.18f, 0.12f);
    private static readonly Color FeastColor = new(1f, 0.82f, 0.2f);
    private static readonly Color RallyColor = new(1f, 0.82f, 0.38f);

    // The meads whose effects the rites show, recoloured: Brew of Animal Whispers, Love Potion (troll pheromones),
    // Anti-Sting Concoction.
    private const string SkalMead = "MeadTamer";
    private const string FeastMead = "MeadTrollPheromones";
    private const string AnvilMead = "MeadBugRepellent";
    // The foods flying in the Eldhrímnir Rite's effect (only those that can be found).
    private static readonly string[] FeastItems = ["CookedMeat", "Bread", "OnionSoup"];
    private const string FirePrefab = "fire_pit";
    private const string ForgePrefab = "forge";
    private const string MixerFrom = "sfx_secretfound";
    private const float Chest = 1.2f;
    // How long the icons (tankards, hammers, food) keep coming, about as long as the emote.
    private const float MotesSeconds = 4f;
    // The Anvil-Spark Rite's hammers and red glow are over sooner, soon after the second strike.
    private const float AnvilSeconds = 1.5f;
    private const float AnvilLightFade = 1.2f;
    // When the Skål Rite's fire flares up, about as the toast is raised.
    private const float SkalFlareAt = 0.8f;
    // The pillar at the obliterator shows to players this near.
    private const float PillarRange = 80f;
    private const float DrumVolume = 0.85f;

    // ---- Each rite ----

    internal static Color ColorOf(WeatherKind kind) => kind switch
    {
        WeatherKind.Skal => SkalColor,
        WeatherKind.SkyCall => SkyCallColor,
        WeatherKind.AnvilSpark => AnvilColor,
        WeatherKind.Eldhrimnir => FeastColor,
        _ => RallyColor
    };

    // A burn performed a kinship rite at position: the column of embers and the flare, for players near.
    internal static void Pillar(WeatherKind kind, Vector3 position)
    {
        Player? me = Player.m_localPlayer;
        if (!me || Vector3.Distance(me!.transform.position, position) > PillarRange) return;
        Color color = ColorOf(kind);
        Embers(null, position + Vector3.up * 0.6f, color, seconds: 3f, rate: 70f, speed: 4.5f, lifetime: 1.8f, spread: 0.35f);
        Flash(null, position + Vector3.up * 2.5f, color, intensity: 4f, range: 14f, seconds: 3.5f, rise: 0.1f);
    }

    // The rite's effect on a player (any player this game has loaded), as its emote starts. detail: the Sky-Call
    // Rite's forsaken power, the Eldhrímnir Rite's number of foods, the Rally-Horn Rite's moment.
    internal static void Show(Player player, WeatherKind kind, int detail)
    {
        bool mine = player == Player.m_localPlayer;
        switch (kind)
        {
            case WeatherKind.Skal: Skal(player); break;
            case WeatherKind.SkyCall: SkyCall(player, detail, mine); break;
            case WeatherKind.AnvilSpark: AnvilSpark(player); break;
            case WeatherKind.Eldhrimnir: Eldhrimnir(player, detail, mine); break;
            case WeatherKind.RallyHorn: Rally(player, detail); break;
        }
    }

    private static void Skal(Player player)
    {
        // The Brew of Animal Whispers' effect, in firelight colours, Rested signs rising instead of its hearts.
        MeadEffect(player, SkalMead, SkalColor, MotesSeconds, KinshipMotes.RestedIcon());
        Crackle(player);
        Flash(player.transform, Vector3.up * Chest, SkalColor, intensity: 2.5f, range: 7f, seconds: 6.5f, rise: 0.4f, flicker: true);
        // As the toast is raised, the fire flares up as when wood is put on it: a whoosh, a jump of flame, a bright flash
        // and a column of embers, so it stands out from the fires around.
        After(SkalFlareAt, () =>
        {
            if (!player) return;
            FlareUp(player);
            Flash(player.transform, Vector3.up * Chest, SkalColor, intensity: 6f, range: 12f, seconds: 1.4f, rise: 0.08f);
            Embers(player.transform, Vector3.up * 0.1f, SkalColor, seconds: 0.9f, rate: 90f, speed: 5f, lifetime: 1.3f, spread: 0.4f);
            Shake(player.transform.position, 10f, 0.2f);
        });
    }

    // The Second-Kindle Rite saving a player: the fire flaring up on them, a hot flash and a column of embers bursting
    // out (only shown, in this game).
    internal static void Kindle(Player player)
    {
        Color fire = new(1f, 0.42f, 0.1f);
        FlareUp(player);
        // A sledge smashing the ground, made huge and fiery: a deep slam, a deeper one under it, and the ground shaking.
        SledgeSlam(player, fire, scale: 2.2f, pitch: 0.75f);
        After(0.06f, () => { if (player) SledgeSlam(player, fire, scale: 3f, pitch: 0.55f, soundOnly: true); });
        Shake(player.transform.position, 25f, 1.4f);
        Flash(player.transform, Vector3.up * Chest, fire, intensity: 7f, range: 12f, seconds: 1.8f, rise: 0.08f, flicker: true);
        Embers(player.transform, Vector3.up * 0.2f, fire, seconds: 1.2f, rate: 120f, speed: 5.5f, lifetime: 1.4f, spread: 0.6f);
        Embers(null, player.transform.position + Vector3.up * 0.1f, new Color(1f, 0.75f, 0.3f), seconds: 0.5f, rate: 160f, speed: 3f,
            lifetime: 1f, spread: 1.6f);
    }

    // The sledges whose ground slam the Second-Kindle Rite uses: the first this game has.
    private static readonly string[] Sledges = ["SledgeDemolisher", "SledgeIron", "SledgeStagbreaker"];
    private static ItemDrop.ItemData.SharedData? _sledge;
    private static bool _sledgeMissing;

    // A sledge's ground slam (what its swing makes where it lands) at the player's feet: scale times as big, its
    // colours pulled half way to color, its sound lower by pitch. soundOnly: only its sound, to layer under another.
    private static void SledgeSlam(Player player, Color color, float scale, float pitch, bool soundOnly = false)
    {
        ItemDrop.ItemData.SharedData? sledge = Sledge();
        if (sledge == null) return;
        foreach (EffectList effects in new[] { sledge.m_triggerEffect, sledge.m_attack.m_triggerEffect })
            foreach (EffectList.EffectData data in effects.m_effectPrefabs)
            {
                if (!data.m_enabled || !data.m_prefab) continue;
                bool sound = data.m_prefab.GetComponentInChildren<ZSFX>(true) || data.m_prefab.GetComponentInChildren<AudioSource>(true);
                if (soundOnly && !sound) continue;
                GameObject? shown = Local(data.m_prefab, player, attach: false, player.transform.position, shown =>
                {
                    Strip<Aoe>(shown);
                    foreach (AudioSource source in shown.GetComponentsInChildren<AudioSource>(true))
                    {
                        source.spatialBlend = 1f;
                        source.maxDistance = Mathf.Max(source.maxDistance, 60f);
                    }
                    foreach (ZSFX sfx in shown.GetComponentsInChildren<ZSFX>(true))
                    {
                        sfx.m_maxConcurrentSources = 0;
                        sfx.m_minVol = sfx.m_maxVol = 1f;
                        sfx.SetPitchModifier(pitch);
                    }
                    if (soundOnly)
                    {
                        foreach (Renderer renderer in shown.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                        foreach (Light light in shown.GetComponentsInChildren<Light>(true)) light.enabled = false;
                        return;
                    }
                    shown.transform.localScale *= scale;
                    foreach (Light light in shown.GetComponentsInChildren<Light>(true))
                    {
                        light.color = color;
                        light.range *= scale;
                    }
                    foreach (ParticleSystem system in shown.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        ParticleSystem.MainModule main = system.main;
                        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                        main.startColor = Warmed(main.startColor, color);
                    }
                });
                if (shown) UnityEngine.Object.Destroy(shown, 6f);
            }
    }

    private static ItemDrop.ItemData.SharedData? Sledge()
    {
        if (_sledge != null || _sledgeMissing || !ObjectDB.instance) return _sledge;
        foreach (string name in Sledges)
            if (ObjectDB.instance.GetItemPrefab(name) is { } prefab && prefab.GetComponent<ItemDrop>() is { } drop)
            {
                _sledge = drop.m_itemData.m_shared;
                return _sledge;
            }
        _sledgeMissing = true;
        Plugin.ModLogger.LogWarning($"{Feature}: no sledge found ({string.Join(", ", Sledges)}), the Second-Kindle Rite has no slam.");
        return null;
    }

    // The same colours pulled half way to color (keeping how see-through they are).
    private static ParticleSystem.MinMaxGradient Warmed(ParticleSystem.MinMaxGradient from, Color color)
    {
        Color Warm(Color c) => WithAlpha(Color.Lerp(c, color, 0.5f), c.a);
        return from.mode switch
        {
            ParticleSystemGradientMode.Color => new ParticleSystem.MinMaxGradient(Warm(from.color)),
            ParticleSystemGradientMode.TwoColors => new ParticleSystem.MinMaxGradient(Warm(from.colorMin), Warm(from.colorMax)),
            ParticleSystemGradientMode.TwoGradients => new ParticleSystem.MinMaxGradient(Keyed(from.gradientMin, Warm(from.gradientMin.Evaluate(0.5f))),
                Keyed(from.gradientMax, Warm(from.gradientMax.Evaluate(0.5f)))),
            _ => from.gradient != null ? new ParticleSystem.MinMaxGradient(Keyed(from.gradient, Warm(from.gradient.Evaluate(0.5f)))) : from
        };
    }

    // The campfire's flare when wood is put on it, at the player.
    private static void FlareUp(Player player)
    {
        if (!_fire && ZNetScene.instance && ZNetScene.instance.GetPrefab(FirePrefab) is { } prefab) _fire = prefab.GetComponent<Fireplace>();
        if (_fire) Spawn(_fire!.m_fuelAddedEffects, player, 4f, spatial: true);
    }

    private static void SkyCall(Player player, int power, bool mine)
    {
        Vector3 at = player.transform.position;
        if (PluginConfig.ShowBlessingLightning.IsOn()) BlessingLightning.Strike(at);
        else BlessingLightning.Thunder(at);
        Shake(at, 25f, 1.2f);
        Flash(null, at + Vector3.up * 3f, SkyCallColor, intensity: 6f, range: 14f, seconds: 2.5f, rise: 0.05f);
        After(0.1f, () => { if (player) KinshipMotes.Sparkles(player, SkyCallColor, 2.5f); });
        After(0.6f, () =>
        {
            if (!player) return;
            // The player's own forsaken power bursting on them, as when they use it.
            StatusEffect? effect = ObjectDB.instance ? ObjectDB.instance.GetStatusEffect(power) : null;
            if (effect) Spawn(effect!.m_startEffects, player, 4f, spatial: true);
            Flash(player.transform, Vector3.up * Chest, SkyCallColor, intensity: 3f, range: 8f, seconds: 3f, rise: 0.1f);
            Embers(player.transform, Vector3.up * 0.1f, SkyCallColor, seconds: 1.5f, rate: 40f, speed: 3f, lifetime: 1.4f, spread: 0.5f);
        });
        if (!mine) return;
        PulseHud(GuardianIcon, 1.1f, 0.7f, 0.45f);
        PulseHud(GuardianIcon, 1.9f, 0.5f, 0.25f);
    }

    // Each strike: when, which of the forge's repair sounds (another one each time) and how high it rings, how big the
    // sparks, how bright.
    private static readonly (float At, int Clip, float Pitch, float Sparks, float Light)[] Strikes =
    [
        (0.3f, 0, 0.95f, 1.8f, 2.5f),
        (1.3f, 1, 1.1f, 2.5f, 4f)
    ];

    private static void AnvilSpark(Player player)
    {
        // A few repair hammers slowly circling up, and the Anti-Sting Concoction's effect in red.
        KinshipMotes.Hammers(player, AnvilColor, AnvilSeconds);
        List<GameObject> mead = MeadEffect(player, AnvilMead, AnvilColor, AnvilSeconds);
        // Its red light fades out as it stops, instead of going out at once a while later.
        After(AnvilSeconds, () => { foreach (GameObject shown in mead) FadeLights(shown, AnvilLightFade); });
        for (int i = 0; i < Strikes.Length; i++)
        {
            var strike = Strikes[i];
            bool last = i == Strikes.Length - 1;
            After(strike.At, () =>
            {
                if (!player) return;
                AnvilRing(player, strike.Clip, strike.Pitch);
                Vector2 around = UnityEngine.Random.insideUnitCircle * 0.3f;
                KinshipMotes.ForgeSparks(player.transform.position + new Vector3(around.x, Chest, around.y), strike.Sparks);
                Flash(player.transform, Vector3.up * Chest, AnvilColor, strike.Light, range: 6f, seconds: last ? 1.2f : 0.7f, rise: 0.03f);
                if (!last) return;
                // A glint off what they hold.
                if (player.m_visEquipment is { } gear)
                    foreach (Transform? hand in new[] { gear.m_rightHand, gear.m_leftHand })
                        if (hand) KinshipMotes.ForgeSparks(hand!.position, 1f);
                Shake(player.transform.position, 15f, 0.4f);
            });
        }
    }

    private static void Eldhrimnir(Player player, int foods, bool mine)
    {
        // The Love Potion's (troll pheromones) effect in a joyful gold, food flying instead of its hearts.
        Texture?[] icons = [.. FeastItems.Select(KinshipMotes.ItemIcon).Where(icon => icon)];
        MeadEffect(player, FeastMead, FeastColor, MotesSeconds, icons);
        Flash(player.transform, Vector3.up * Chest, FeastColor, intensity: 2.4f, range: 7f, seconds: 6f, rise: 0.5f, flicker: true);
        // A bite for each food made fresh, its slot pulsing as it is heard.
        int bites = Mathf.Clamp(foods, 1, 5);
        for (int i = 0; i < bites; i++)
        {
            int food = i;
            After(0.6f + food * 0.55f, () =>
            {
                if (!player) return;
                Spawn(player.m_consumeItemEffects, player, 3f, spatial: true, pitch: 1f + food * 0.06f);
                if (mine) PulseHud(() => FoodIcon(food), 0f, 0.5f, 0.4f);
            });
        }
    }

    private static void Rally(Player player, int moment)
    {
        Vector3 at = player.transform.position;
        if (moment == RallyLeaving)
        {
            // A pillar left where they knelt, and the horn's echo.
            Embers(null, at, RallyColor, seconds: 2.5f, rate: 50f, speed: 3.5f, lifetime: 1.5f, spread: 0.35f);
            Flash(null, at + Vector3.up * 1.5f, RallyColor, intensity: 5f, range: 10f, seconds: 3.5f, rise: 0.05f);
            if (RallyHorn.HornClip is { } clip) Play(clip, null, at, volume: 0.45f, pitch: 0.8f, seconds: 4f, fade: 1.5f);
        }
        else if (moment == RallyRisen)
        {
            Flash(player.transform, Vector3.up * Chest, RallyColor, intensity: 3f, range: 8f, seconds: 2f, rise: 0.1f);
            if (RallyHorn.HornClip is { } clip) Play(clip, player.transform, Vector3.up, volume: 0.6f, pitch: 1f, seconds: 1.6f, fade: 0.5f);
            Shake(at, 12f, 0.3f);
        }
    }

    // The Rally Call slot beside the Guardian Power pulsing a few times as the call comes.
    internal static void PulseRallySlot(string slotId)
    {
        for (int i = 0; i < 3; i++) PulseHud(() => SlotRect(slotId), 0.3f + i * 0.7f, 0.55f, 0.35f);
    }

    // ---- The drum, heard by everyone ----

    private static AudioClip? _drumClip;
    private static AudioSource? _drumSource;
    private static bool _drumFailed;

    internal static void Drum()
    {
        MakeDrum();
        if (!_drumClip) return;
        if (!_drumSource)
        {
            GameObject holder = new("TempestRite_KinshipDrum");
            UnityEngine.Object.DontDestroyOnLoad(holder);
            _drumSource = holder.AddComponent<AudioSource>();
            _drumSource.playOnAwake = false;
            _drumSource.spatialBlend = 0f;
            _drumSource.bypassReverbZones = true;
        }
        if (!_drumSource!.outputAudioMixerGroup) _drumSource.outputAudioMixerGroup = Mixer();
        _drumSource.volume = DrumVolume;
        _drumSource.PlayOneShot(_drumClip);
    }

    private static void MakeDrum()
    {
        if (_drumClip || _drumFailed) return;
        // Whatever goes wrong here only costs the drum.
        _drumFailed = true;
        try
        {
            DrumSound.Samples(out float[] left, out float[] right);
            _drumClip = MakeClip("TempestRite_KinshipDrum", left, right, DrumSound.SampleRate);
            _drumFailed = false;
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogWarning($"The kinship rites' drum could not be made, they sound no drum: {exception.Message}");
        }
    }

    // A stereo clip from samples made in code.
    internal static AudioClip MakeClip(string name, float[] left, float[] right, int sampleRate)
    {
        float[] both = new float[left.Length * 2];
        for (int i = 0; i < left.Length; i++)
        {
            both[i * 2] = left[i];
            both[i * 2 + 1] = right[i];
        }
        AudioClip clip = AudioClip.Create(name, left.Length, 2, sampleRate, false);
        SetClipData(clip, both, 0);
        clip.hideFlags = HideFlags.HideAndDontSave;
        return clip;
    }

    // AudioClip.SetData(float[], int), called by reflection: the game's Unity also has a Span version of it, which this
    // mod's .NET target can't compile against.
    private static MethodInfo? _setData;

    private static void SetClipData(AudioClip clip, float[] data, int offset)
    {
        _setData ??= typeof(AudioClip).GetMethod(nameof(AudioClip.SetData), [typeof(float[]), typeof(int)])
            ?? throw new MissingMethodException(nameof(AudioClip), nameof(AudioClip.SetData));
        _setData.Invoke(clip, [data, offset]);
    }

    // The sound effects mixer group, taken from one of the game's own sounds, so these play at its volume.
    internal static UnityEngine.Audio.AudioMixerGroup? Mixer()
    {
        GameObject? sfx = ZNetScene.instance ? ZNetScene.instance.GetPrefab(MixerFrom) : null;
        AudioSource? source = sfx ? sfx!.GetComponentInChildren<AudioSource>(true) : null;
        return source ? source!.outputAudioMixerGroup : null;
    }

    // ---- Pieces ----

    private sealed class Later(float at, Action action)
    {
        internal float At { get; } = at;
        internal Action Action { get; } = action;
    }

    private sealed class Lit(Light light, float start, float seconds, float rise, float peak, bool flicker)
    {
        internal Light Light { get; } = light;
        internal float Start { get; } = start;
        internal float Seconds { get; } = seconds;
        internal float Rise { get; } = rise;
        internal float Peak { get; } = peak;
        internal bool Flicker { get; } = flicker;
        internal float Seed { get; } = UnityEngine.Random.value * 100f;
    }

    private sealed class Sound(AudioSource source, float start, float seconds, float fade, float volume)
    {
        internal AudioSource Source { get; } = source;
        internal float Start { get; } = start;
        internal float Seconds { get; } = seconds;
        internal float Fade { get; } = fade;
        internal float Volume { get; } = volume;
    }

    private sealed class Pulse(Func<RectTransform?> find, float start, float seconds, float size)
    {
        internal Func<RectTransform?> Find { get; } = find;
        internal float Start { get; } = start;
        internal float Seconds { get; } = seconds;
        internal float Size { get; } = size;
        internal RectTransform? Rect;
        internal Vector3 BaseScale = Vector3.one;
    }

    private static readonly List<Later> Laters = [];
    private static readonly List<Lit> Lights = [];
    private static readonly List<Sound> Sounds = [];
    private static readonly List<Pulse> Pulses = [];
    private static readonly List<Later> Due = [];

    // Runs action after a moment (a few seconds at most), in this game.
    internal static void After(float seconds, Action action)
    {
        if (seconds <= 0f) action();
        else Laters.Add(new Later(Time.time + seconds, action));
    }

    // A light that rises to intensity, then fades out, over seconds; with a parent it follows it (at offset), without
    // it stays at offset in the world.
    private static void Flash(Transform? parent, Vector3 offset, Color color, float intensity, float range, float seconds,
        float rise, bool flicker = false)
    {
        GameObject holder = new("TempestRite_KinshipLight");
        if (parent) holder.transform.SetParent(parent, false);
        holder.transform.localPosition = offset;
        Light light = holder.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = 0f;
        light.shadows = LightShadows.None;
        Lights.Add(new Lit(light, Time.time, seconds, Mathf.Min(rise, seconds * 0.5f), intensity, flicker));
        UnityEngine.Object.Destroy(holder, seconds + 0.5f);
    }

    // Embers rising in color for seconds (then the last of them fade out), copied from a campfire's; with a parent they
    // follow it.
    private static void Embers(Transform? parent, Vector3 offset, Color color, float seconds, float rate, float speed,
        float lifetime, float spread)
    {
        GameObject? template = EmberTemplate();
        if (!template) return;
        // Made inside an inactive holder, so it is set up before it starts.
        GameObject holder = new("TempestRite_EmberHolder");
        holder.SetActive(false);
        GameObject embers;
        try
        {
            embers = UnityEngine.Object.Instantiate(template!, holder.transform);
            embers.name = "TempestRite_Embers";
            embers.transform.localScale = Vector3.one;
            embers.transform.localRotation = Quaternion.identity;
            foreach (ParticleSystem system in embers.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                main.loop = true;
                main.playOnAwake = true;
                main.startColor = color;
                main.startSpeed = speed;
                main.startLifetime = lifetime;
                main.maxParticles = Mathf.Max(main.maxParticles, (int)(rate * lifetime * 2f));
                ParticleSystem.EmissionModule emission = system.emission;
                emission.enabled = true;
                emission.rateOverTime = rate;
                ParticleSystem.ShapeModule shape = system.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 8f;
                shape.radius = spread;
                shape.rotation = new Vector3(-90f, 0f, 0f);
            }
            if (parent) embers.transform.SetParent(parent, false);
            else embers.transform.SetParent(null, false);
            embers.transform.localPosition = offset;
        }
        finally { UnityEngine.Object.Destroy(holder); }
        After(seconds, () =>
        {
            if (!embers) return;
            foreach (ParticleSystem system in embers.GetComponentsInChildren<ParticleSystem>(true))
                system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        });
        UnityEngine.Object.Destroy(embers, seconds + lifetime + 0.5f);
    }

    private static GameObject? _emberTemplate;
    private static bool _emberMissing;

    // The campfire's embers (its particle system named for sparks or embers).
    private static GameObject? EmberTemplate()
    {
        if (_emberTemplate || _emberMissing || !ZNetScene.instance) return _emberTemplate;
        GameObject? fire = ZNetScene.instance.GetPrefab(FirePrefab);
        List<string> names = [];
        if (fire)
            foreach (ParticleSystem system in fire!.GetComponentsInChildren<ParticleSystem>(true))
            {
                string name = system.gameObject.name.ToLowerInvariant();
                names.Add(system.gameObject.name);
                if (!name.Contains("spar") && !name.Contains("ember")) continue;
                // Only that one, without anything under it.
                if (system.transform.childCount > 0) continue;
                _emberTemplate = system.gameObject;
                break;
            }
        if (!_emberTemplate)
        {
            _emberMissing = true;
            Plugin.ModLogger.LogWarning($"No embers found in {FirePrefab} ({string.Join(", ", names)}): the kinship rites show no embers.");
        }
        return _emberTemplate;
    }

    // A clip played at a place (or following parent), at sound effects volume, fading out over its last fade seconds.
    private static void Play(AudioClip clip, Transform? parent, Vector3 offset, float volume, float pitch, float seconds,
        float fade, bool loop = false, AudioSource? like = null)
    {
        GameObject holder = new("TempestRite_KinshipSound");
        if (parent) holder.transform.SetParent(parent, false);
        holder.transform.localPosition = offset;
        AudioSource source = holder.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.spatialBlend = 1f;
        source.pitch = pitch;
        source.volume = volume;
        if (like)
        {
            source.outputAudioMixerGroup = like!.outputAudioMixerGroup;
            source.rolloffMode = like.rolloffMode;
            source.minDistance = like.minDistance;
            source.maxDistance = like.maxDistance;
        }
        else
        {
            source.outputAudioMixerGroup = Mixer();
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 8f;
            source.maxDistance = 70f;
        }
        source.Play();
        Sounds.Add(new Sound(source, Time.time, seconds, fade, volume));
        UnityEngine.Object.Destroy(holder, seconds + 0.5f);
    }

    private static Fireplace? _fire;
    private static CraftingStation? _forge;

    // The campfire's crackle, a little lower, following the player for a few seconds.
    private static void Crackle(Player player)
    {
        if (!_fire && ZNetScene.instance && ZNetScene.instance.GetPrefab(FirePrefab) is { } prefab) _fire = prefab.GetComponent<Fireplace>();
        if (!_fire) return;
        foreach (AudioSource source in _fire!.GetComponentsInChildren<AudioSource>(true))
        {
            ZSFX? sfx = source.GetComponent<ZSFX>();
            AudioClip? clip = sfx && sfx!.m_audioClips.Length > 0 ? sfx.m_audioClips[0] : source.clip;
            if (!clip || !source.loop) continue;
            Play(clip!, player.transform, Vector3.up, source.volume, 0.85f, seconds: 6f, fade: 2f, loop: true, like: source);
            return;
        }
    }


    private static void Strip<T>(GameObject shown) where T : Component
    {
        foreach (T component in shown.GetComponentsInChildren<T>(true)) UnityEngine.Object.DestroyImmediate(component);
    }

    // A mead's effect on the player, as when they drink one (its status effect's start effects), for seconds, without
    // its sounds and recoloured to color. Its hearts, if it has any, show icons instead: one kind of heart for each
    // icon, white (so the pictures keep their colours). Returns what it shows.
    private static List<GameObject> MeadEffect(Player player, string item, Color color, float seconds, params Texture?[] icons)
    {
        List<GameObject> made = [];
        GameObject? prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(item) : null;
        StatusEffect? effect = prefab && prefab!.GetComponent<ItemDrop>() is { } drop ? drop.m_itemData.m_shared.m_consumeStatusEffect : null;
        if (!effect)
        {
            Plugin.ModLogger.LogWarning($"{Feature}: {item} not found, its effect is not shown.");
            return made;
        }
        Texture[] pictures = [.. icons.Where(icon => icon).Select(icon => icon!)];
        bool hearts = false;
        EffectList.EffectData[] list = effect!.m_startEffects.m_effectPrefabs;
        foreach (EffectList.EffectData data in list)
        {
            if (!data.m_enabled || !data.m_prefab) continue;
            GameObject? shown = Local(data.m_prefab, player, data.m_attach, player.GetCenterPoint(), shown =>
            {
                Silence(shown);
                hearts |= Recolor(shown, color, pictures);
            });
            if (!shown) continue;
            made.Add(shown!);
            After(seconds, () => StopShown(shown));
        }
        // No hearts to swap: the pictures rise on their own.
        if (pictures.Length > 0 && !hearts) KinshipMotes.Rising(player, pictures, seconds);
        return made;
    }

    // An effect stops giving off particles; what is out fades, then it goes.
    internal static void StopShown(GameObject? shown)
    {
        if (!shown) return;
        foreach (ParticleSystem system in shown!.GetComponentsInChildren<ParticleSystem>(true))
            system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        UnityEngine.Object.Destroy(shown, 3f);
    }

    // An effect's lights fading out over seconds (taken from the game's flicker, which would hold them up).
    private static void FadeLights(GameObject? shown, float seconds)
    {
        if (!shown) return;
        foreach (LightFlicker flicker in shown!.GetComponentsInChildren<LightFlicker>(true)) flicker.enabled = false;
        foreach (Light light in shown.GetComponentsInChildren<Light>(true))
            if (light.intensity > 0f) Lights.Add(new Lit(light, Time.time, seconds, 0f, light.intensity, flicker: false));
    }

    // The Rally-Horn Rite's call waiting for an answer: horns flying up from the player as the Skål Rite's Rested signs
    // do, in gold. Returns what it shows (stopped once the call is answered, refused or fades).
    internal static List<GameObject> CallFlourish(Player player) =>
        MeadEffect(player, SkalMead, RallyColor, 2.5f, WeatherIcons.For(WeatherKind.RallyHorn) is { } horn ? horn.texture : null);

    private static void Silence(GameObject shown)
    {
        foreach (ZSFX sfx in shown.GetComponentsInChildren<ZSFX>(true)) UnityEngine.Object.DestroyImmediate(sfx);
        foreach (AudioSource source in shown.GetComponentsInChildren<AudioSource>(true)) UnityEngine.Object.DestroyImmediate(source);
    }

    private static bool Named(string? name, string part) => name != null && name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool IsHeart(ParticleSystem system)
    {
        if (Named(system.name, "heart")) return true;
        Material? material = system.GetComponent<ParticleSystemRenderer>() is { } renderer ? renderer.sharedMaterial : null;
        return material && (Named(material!.name, "heart") || (material.mainTexture && Named(material.mainTexture.name, "heart")));
    }

    // Every part in color (lights too); the hearts as the pictures instead, if there are any. Whether there were hearts.
    private static bool Recolor(GameObject shown, Color color, Texture[] pictures)
    {
        foreach (Light light in shown.GetComponentsInChildren<Light>(true)) light.color = color;
        List<ParticleSystem> hearts = [];
        foreach (ParticleSystem system in shown.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystemRenderer? renderer = system.GetComponent<ParticleSystemRenderer>();
            if (pictures.Length > 0 && IsHeart(system) && renderer)
            {
                hearts.Add(system);
                continue;
            }
            ParticleSystem.MainModule main = system.main;
            main.startColor = Tinted(main.startColor, color);
            ParticleSystem.ColorOverLifetimeModule overLife = system.colorOverLifetime;
            if (overLife.enabled) overLife.color = Whitened(overLife.color);
            if (!renderer || !renderer!.sharedMaterial) continue;
            Material material = renderer.material;
            foreach (string property in TintProperties)
                if (material.HasProperty(property))
                    material.SetColor(property, WithAlpha(color, material.GetColor(property).a));
        }
        foreach (ParticleSystem heart in hearts)
        {
            Picture(heart, pictures[0], pictures.Length);
            // One more kind of heart for each other picture, sharing out how many there are.
            for (int i = 1; i < pictures.Length; i++)
            {
                GameObject copy = UnityEngine.Object.Instantiate(heart.gameObject, heart.transform.parent);
                copy.name = heart.name;
                copy.transform.SetLocalPositionAndRotation(heart.transform.localPosition, heart.transform.localRotation);
                copy.transform.localScale = heart.transform.localScale;
                if (copy.GetComponent<ParticleSystem>() is { } copied) Picture(copied, pictures[i], 1);
            }
        }
        return hearts.Count > 0;
    }

    private static readonly string[] TintProperties = ["_TintColor", "_Color"];

    // A heart made a picture: the picture's own colours, the whole of it (no frames of a sheet). share: how many kinds
    // of heart share what this one gave off alone.
    private static void Picture(ParticleSystem system, Texture picture, int share)
    {
        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        Material material = renderer.material;
        material.mainTexture = picture;
        foreach (string property in TintProperties)
            if (material.HasProperty(property)) material.SetColor(property, WithAlpha(Color.white, material.GetColor(property).a));
        ParticleSystem.TextureSheetAnimationModule sheet = system.textureSheetAnimation;
        sheet.enabled = false;
        ParticleSystem.MainModule main = system.main;
        main.startColor = Whitened(main.startColor);
        ParticleSystem.ColorOverLifetimeModule overLife = system.colorOverLifetime;
        if (overLife.enabled) overLife.color = Whitened(overLife.color);
        if (share <= 1) return;
        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTimeMultiplier /= share;
    }

    private static Color WithAlpha(Color color, float alpha) => new(color.r, color.g, color.b, alpha);

    // The same fading in and out, in color (lighter toward its other end).
    private static ParticleSystem.MinMaxGradient Tinted(ParticleSystem.MinMaxGradient from, Color color)
    {
        Color light = Color.Lerp(color, Color.white, 0.35f);
        return from.mode switch
        {
            ParticleSystemGradientMode.Color => new ParticleSystem.MinMaxGradient(WithAlpha(color, from.color.a)),
            ParticleSystemGradientMode.TwoColors => new ParticleSystem.MinMaxGradient(WithAlpha(color, from.colorMin.a), WithAlpha(light, from.colorMax.a)),
            ParticleSystemGradientMode.TwoGradients => new ParticleSystem.MinMaxGradient(Keyed(from.gradientMin, color), Keyed(from.gradientMax, light)),
            _ => new ParticleSystem.MinMaxGradient(Keyed(from.gradient ?? from.gradientMax, color))
        };
    }

    // The same fading in and out, but white (for pictures, and over a start colour already tinted).
    private static ParticleSystem.MinMaxGradient Whitened(ParticleSystem.MinMaxGradient from) => from.mode switch
    {
        ParticleSystemGradientMode.Color => new ParticleSystem.MinMaxGradient(WithAlpha(Color.white, from.color.a)),
        ParticleSystemGradientMode.TwoColors => new ParticleSystem.MinMaxGradient(WithAlpha(Color.white, from.colorMin.a), WithAlpha(Color.white, from.colorMax.a)),
        ParticleSystemGradientMode.TwoGradients => new ParticleSystem.MinMaxGradient(Keyed(from.gradientMin, Color.white), Keyed(from.gradientMax, Color.white)),
        _ => new ParticleSystem.MinMaxGradient(Keyed(from.gradient ?? from.gradientMax, Color.white))
    };

    private static Gradient Keyed(Gradient? from, Color color)
    {
        Gradient gradient = new();
        GradientAlphaKey[] alphas = from?.alphaKeys is { Length: > 0 } keys ? keys : [new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)];
        gradient.SetKeys([new GradientColorKey(color, 0f), new GradientColorKey(color, 1f)], alphas);
        return gradient;
    }

    // The anvil ringing as when gear is repaired at the forge, heard from where the player stands.
    // clip: which of its sounds (when it has several), so two strikes in a row sound different.
    private static void AnvilRing(Player player, int clip, float pitch)
    {
        if (!_forge && ZNetScene.instance && ZNetScene.instance.GetPrefab(ForgePrefab) is { } prefab) _forge = prefab.GetComponent<CraftingStation>();
        if (_forge) Spawn(_forge!.m_repairItemDoneEffects, player, 3f, spatial: true, pitch, clip);
    }

    // Makes the effects of a list that aren't sent to others (each game makes its own), at the player.
    // clip: which of each sound's clips plays (by default one at random, as the game picks). Networked effects (most of
    // the game's sounds) are made as only a picture or sound here, as with the meads.
    private static void Spawn(EffectList effects, Player player, float lifetime, bool spatial, float pitch = 1f, int clip = -1)
    {
        foreach (EffectList.EffectData data in effects.m_effectPrefabs)
        {
            GameObject made = data.m_prefab;
            if (!data.m_enabled || !made) continue;
            GameObject? shown = Local(made, player, data.m_attach, player.transform.position, shown =>
            {
                // A crafting station's sounds are heard as if beside you: heard from where the player stands instead.
                if (spatial)
                    foreach (AudioSource source in shown.GetComponentsInChildren<AudioSource>(true))
                        source.spatialBlend = 1f;
                foreach (ZSFX sfx in shown.GetComponentsInChildren<ZSFX>(true))
                {
                    if (!Mathf.Approximately(pitch, 1f)) sfx.SetPitchModifier(pitch);
                    if (clip >= 0 && sfx.m_audioClips is { Length: > 1 } clips) sfx.m_audioClips = [clips[clip % clips.Length]];
                }
            });
            if (shown) UnityEngine.Object.Destroy(shown, lifetime);
        }
    }

    // A copy of an effect prefab at the player (following them if attached), changed by setup before it starts, with
    // what would make it a networked object (its own in every game) taken off, as with the lightning.
    private static GameObject? Local(GameObject prefab, Player player, bool attach, Vector3 at, Action<GameObject> setup)
    {
        GameObject holder = new("TempestRite_EffectHolder");
        holder.SetActive(false);
        try
        {
            GameObject shown = UnityEngine.Object.Instantiate(prefab, holder.transform);
            Strip<ZSyncTransform>(shown);
            Strip<ZNetView>(shown);
            setup(shown);
            if (attach) shown.transform.SetParent(player.transform, false);
            else shown.transform.SetParent(null, false);
            shown.transform.SetPositionAndRotation(at, player.transform.rotation);
            return shown;
        }
        finally { UnityEngine.Object.Destroy(holder); }
    }

    private static void Shake(Vector3 at, float range, float strength)
    {
        if (GameCamera.instance) GameCamera.instance.AddShake(at, range, strength, false);
    }

    // A HUD element found when its pulse starts (delay seconds from now), growing and back over seconds.
    private static void PulseHud(Func<RectTransform?> find, float delay, float seconds, float size) =>
        Pulses.Add(new Pulse(find, Time.time + delay, seconds, size));

    private static RectTransform? GuardianIcon() =>
        Hud.instance && Hud.instance.m_gpIcon ? Hud.instance.m_gpIcon.rectTransform : null;

    private static RectTransform? FoodIcon(int index)
    {
        Image[]? icons = Hud.instance ? Hud.instance.m_foodIcons : null;
        return icons != null && index < icons.Length && icons[index] ? icons[index].rectTransform : null;
    }

    // An AbilityKit slot beside the Guardian Power, by its id (named "AbilityKit_Slot|priority|id").
    private static RectTransform? SlotRect(string slotId)
    {
        Transform? row = Hud.instance && Hud.instance.m_gpRoot ? Hud.instance.m_gpRoot.parent : null;
        if (!row) return null;
        foreach (Transform child in row!)
            if (child.name.StartsWith(JoksterCube.AbilityKit.AbilitySlot.NamePrefix, StringComparison.Ordinal)
                && child.name.EndsWith("|" + slotId, StringComparison.Ordinal))
                return child as RectTransform;
        return null;
    }

    // ---- Every frame ----

    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            float now = Time.time;
            RunDue(now);
            UpdateLights(now);
            UpdateSounds(now);
            UpdatePulses(now);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void RunDue(float now)
    {
        if (Laters.Count == 0) return;
        for (int i = Laters.Count - 1; i >= 0; i--)
        {
            if (now < Laters[i].At) continue;
            Due.Add(Laters[i]);
            Laters.RemoveAt(i);
        }
        // Each on its own: one failing doesn't stop the others.
        for (int i = Due.Count - 1; i >= 0; i--)
        {
            try { Due[i].Action(); }
            catch (Exception exception) { Plugin.ModLogger.LogWarning($"{Feature}: {exception.Message}"); }
        }
        Due.Clear();
    }

    private static void UpdateLights(float now)
    {
        for (int i = Lights.Count - 1; i >= 0; i--)
        {
            Lit lit = Lights[i];
            float t = now - lit.Start;
            if (!lit.Light || t >= lit.Seconds)
            {
                Lights.RemoveAt(i);
                continue;
            }
            float level = t < lit.Rise ? t / lit.Rise : 1f - (t - lit.Rise) / (lit.Seconds - lit.Rise);
            level = Mathf.SmoothStep(0f, 1f, level);
            if (lit.Flicker) level *= 0.75f + 0.25f * Mathf.PerlinNoise(now * 7f, lit.Seed);
            lit.Light.intensity = lit.Peak * level;
        }
    }

    private static void UpdateSounds(float now)
    {
        for (int i = Sounds.Count - 1; i >= 0; i--)
        {
            Sound sound = Sounds[i];
            float left = sound.Start + sound.Seconds - now;
            if (!sound.Source || left <= 0f)
            {
                if (sound.Source) sound.Source.Stop();
                Sounds.RemoveAt(i);
                continue;
            }
            sound.Source.volume = sound.Volume * (sound.Fade > 0f ? Mathf.Clamp01(left / sound.Fade) : 1f);
        }
    }

    private static void UpdatePulses(float now)
    {
        for (int i = Pulses.Count - 1; i >= 0; i--)
        {
            Pulse pulse = Pulses[i];
            if (now < pulse.Start) continue;
            if (!pulse.Rect)
            {
                pulse.Rect = pulse.Find();
                if (!pulse.Rect)
                {
                    Pulses.RemoveAt(i);
                    continue;
                }
                // Another pulse on it still going: grow from its size before that one.
                pulse.BaseScale = Pulses.Find(p => p != pulse && p.Rect == pulse.Rect) is { } other
                    ? other.BaseScale
                    : pulse.Rect!.localScale;
            }
            float t = (now - pulse.Start) / pulse.Seconds;
            if (t >= 1f)
            {
                pulse.Rect!.localScale = pulse.BaseScale;
                Pulses.RemoveAt(i);
                continue;
            }
            pulse.Rect!.localScale = pulse.BaseScale * (1f + pulse.Size * Mathf.Sin(Mathf.PI * t));
        }
    }

    internal static void ClearSession()
    {
        Laters.Clear();
        Due.Clear();
        foreach (Lit lit in Lights) if (lit.Light) UnityEngine.Object.Destroy(lit.Light.gameObject);
        Lights.Clear();
        foreach (Sound sound in Sounds) if (sound.Source) UnityEngine.Object.Destroy(sound.Source.gameObject);
        Sounds.Clear();
        foreach (Pulse pulse in Pulses) if (pulse.Rect) pulse.Rect!.localScale = pulse.BaseScale;
        Pulses.Clear();
        _fire = null;
        _forge = null;
        _sledge = null;
        _sledgeMissing = false;
        _emberTemplate = null;
        _emberMissing = false;
    }
}
