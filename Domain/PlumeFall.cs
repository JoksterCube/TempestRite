using System;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// The Plume-Ward Rite, a blessing: the blessed player takes Fall Damage Multiplier of their fall damage. A fall that
// would still kill them does them no harm at all, and that uses the blessing up. Their own game works it out as they
// land. A softened landing gives off a puff of pale down and its own sound (see PlumeSound); a fall it saved them from
// shatters like a breaking ward into flying feathers, with a great plume of down. Everyone with the mod nearby sees and hears it.
internal static class PlumeFall
{
    private const string Feature = "Plume-Ward Rite";
    // Landings this close together in time show one puff.
    private const float PuffEvery = 0.5f;
    private const float ShowRange = 80f;
    private const float SavedSeconds = 6f;
    // The plume of a saved fall: one great puff and a ring of smaller ones round it.
    private const float SavedPuff = 2.8f;
    private const int SavedRing = 6;
    private const float SavedRingSpread = 1.3f;
    private const float SavedRingPuff = 1.3f;
    // The ward's shards are this big next to the shield's, as feathers.
    private const float ShardSize = 0.6f;
    private const float SoundVolume = 0.38f;
    private const float SoundRange = 40f;
    private static AudioClip? _landClip;
    private static bool _landFailed;
    private static float _lastPuff;

    internal static void Register(ZRoutedRpc rpc) => rpc.Register<Vector3, bool>(Rpcs.PlumeLanding, RPC_Landing);

    // The local player is landing and would take damage: less of it, or none if it would kill them (ending the blessing).
    internal static void Soften(Character character, ref float damage)
    {
        if (PatchGuard.IsDisabled(Feature) || damage <= 0f) return;
        try
        {
            if (!character || character != Player.m_localPlayer || !Blessings.IsActive(WeatherKind.PlumeWard)) return;
            damage *= Mathf.Clamp01(PluginConfig.PlumeWardFallDamage.Value);
            bool fatal = damage >= character.GetHealth();
            if (fatal)
            {
                damage = 0f;
                Blessings.End(WeatherKind.PlumeWard);
                PlayerMessages.Center(Messages.PlumeWardSaved);
            }
            else if (Time.time - _lastPuff < PuffEvery) return;
            _lastPuff = Time.time;
            Vector3 at = character.transform.position;
            Land(at, fatal);
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.PlumeLanding, at, fatal);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void RPC_Landing(long sender, Vector3 at, bool fatal)
    {
        if (PatchGuard.IsDisabled(Feature) || !ZNet.instance || ZNet.instance.IsDedicated() || sender == ZDOMan.GetSessionID()) return;
        try
        {
            Player? me = Player.m_localPlayer;
            if (me && Vector3.Distance(me!.transform.position, at) < ShowRange) Land(at, fatal);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // The sounds always play; the down and the flash only with Show Plume-Ward Down.
    private static void Land(Vector3 at, bool fatal)
    {
        bool show = PluginConfig.ShowPlumeWardDown.IsOn();
        Color color = DomainVisuals.RiteColor(WeatherKind.PlumeWard);
        if (fatal)
        {
            Saved(at, show);
            if (show)
            {
                DomainVisuals.Burst(WeatherKind.PlumeWard, at + Vector3.up * 0.4f, color, SavedPuff, always: true);
                for (int i = 0; i < SavedRing; i++)
                {
                    float angle = i * Mathf.PI * 2f / SavedRing;
                    Vector3 outward = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    DomainVisuals.Burst(WeatherKind.PlumeWard, at + outward * SavedRingSpread + Vector3.up * 0.2f, color, SavedRingPuff, always: true);
                }
            }
        }
        else if (show) DomainVisuals.Burst(WeatherKind.PlumeWard, at + Vector3.up * 0.3f, color, 1.3f, always: true);
        LandSound(at);
    }

    // The landing's own sound (made once), heard where it was.
    private static void LandSound(Vector3 at)
    {
        if (!_landClip && !_landFailed)
        {
            // Whatever goes wrong here only costs the sound.
            _landFailed = true;
            try
            {
                PlumeSound.Samples(out float[] left, out float[] right);
                _landClip = KinshipFx.MakeClip("TempestRite_PlumeLanding", left, right, PlumeSound.SampleRate);
                _landFailed = false;
            }
            catch (Exception exception)
            {
                Plugin.ModLogger.LogWarning($"The {Feature}'s landing sound could not be made, it lands silently: {exception.Message}");
            }
        }
        if (!_landClip) return;
        GameObject holder = new("TempestRite_PlumeSound");
        holder.transform.position = at + Vector3.up * 0.5f;
        AudioSource source = holder.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = KinshipFx.Mixer();
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 6f;
        source.maxDistance = SoundRange;
        source.volume = SoundVolume;
        source.PlayOneShot(_landClip);
        UnityEngine.Object.Destroy(holder, _landClip!.length + 0.5f);
    }

    // The ward breaking where the fall was saved: the shatter of the Staff of Protection's shield, without its shards
    // when the down is hidden.
    private static void Saved(Vector3 at, bool show)
    {
        SE_Shield? shield = null;
        if (ObjectDB.instance)
            foreach (StatusEffect effect in ObjectDB.instance.m_StatusEffects)
                if (effect is SE_Shield found) { shield = found; break; }
        if (!shield)
        {
            Plugin.ModLogger.LogWarning($"No shield status effect found: the {Feature} saves without its breaking sound.");
            return;
        }
        foreach (EffectList.EffectData effect in shield!.m_breakEffects.m_effectPrefabs)
        {
            if (!effect.m_enabled || !effect.m_prefab) continue;
            GameObject made = SpawnLocal(effect.m_prefab, at + Vector3.up);
            if (!show)
            {
                foreach (Renderer renderer in made.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                foreach (Light light in made.GetComponentsInChildren<Light>(true)) light.enabled = false;
            }
            else Featherize(made);
            // TimedDestruction only removes networked copies, so the local copy removes itself.
            UnityEngine.Object.Destroy(made, made.TryGetComponent(out TimedDestruction timed) ? timed.m_timeout : SavedSeconds);
        }
    }

    // The shield's blue shards and sparks as pale feathers flying the same way, its light in the rite's color.
    private static void Featherize(GameObject made)
    {
        Color color = DomainVisuals.RiteColor(WeatherKind.PlumeWard);
        Material? feather = DomainVisuals.FeatherMaterial();
        foreach (Light light in made.GetComponentsInChildren<Light>(true)) light.color = color;
        foreach (ParticleSystem system in made.GetComponentsInChildren<ParticleSystem>(true))
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.startSizeMultiplier *= ShardSize;
            ParticleSystem.ColorOverLifetimeModule overLife = system.colorOverLifetime;
            if (overLife.enabled) overLife.color = White(overLife.color);
            if (feather && system.TryGetComponent(out ParticleSystemRenderer renderer))
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.material = feather;
                renderer.trailMaterial = feather;
            }
            system.Play(false);
        }
    }

    // A fade over life kept, its colors white so it doesn't tint the feathers.
    private static ParticleSystem.MinMaxGradient White(ParticleSystem.MinMaxGradient gradient)
    {
        Gradient? source = gradient.mode is ParticleSystemGradientMode.Gradient or ParticleSystemGradientMode.TwoGradients
            ? gradient.gradientMax : null;
        if (source == null) return new ParticleSystem.MinMaxGradient(Color.white);
        Gradient result = new() { mode = source.mode };
        result.SetKeys([new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f)], source.alphaKeys);
        return new ParticleSystem.MinMaxGradient(result);
    }

    // A copy for this player only, never one shared with others even if the prefab is networked.
    private static GameObject SpawnLocal(GameObject prefab, Vector3 at)
    {
        ZNetView.m_forceDisableInit = true;
        try { return UnityEngine.Object.Instantiate(prefab, at, Quaternion.identity); }
        finally { ZNetView.m_forceDisableInit = false; }
    }
}
