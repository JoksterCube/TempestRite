using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JoksterCube.TempestRite.Domain;

// The Morgen-Pouch's mark on the creatures a press would take (see PetPouch): the soft glow and light of the game's
// Guardian Power activation (fx_GP_Activation: only its "gloria" glow and its point light, no sparks, sound or shake),
// recolored violet and sized to the creature. Each holds one steady glow that this class brightens and dims itself, all on
// one clock, so every marked creature pulses together, rising and falling evenly, with a thin outline (PouchOutline).
// A local object on the creature, so only this player sees it.
internal static class PouchAura
{
    private const string Feature = "Morgen-Pouch Rite";
    private const string PrefabName = "fx_GP_Activation";
    private const string GlowPart = "gloria";
    private const string LightPart = "Point light";
    private static readonly Color Violet = new(0.62f, 0.3f, 1f);
    // The glow grows with the creature's body (its collider: the average of its length and thickness), so a moose's
    // (about 2.9 m) is about 1.5 times the Guardian Power's, a wolf's about 0.75, a chicken's about 0.4.
    private const float SizePerMeter = 1f / 1.88f;
    private const float MinSize = 0.3f;
    private const float MaxSize = 3f;
    // The Guardian Power's glow is about 5 m wide and fills the screen: a third of it.
    private const float GlowScale = 0.35f;
    // How strong the glow and light are at the bottom and top of each pulse, and how long one pulse takes.
    private const float GlowLow = 0f;
    private const float GlowHigh = 0.14f;
    private const float LightIntensity = 0.5f;
    private const float LightLow = 0f;
    private const float LightRange = 2f;
    private const float PulseSeconds = 3f;
    // The outline around them (see PouchOutline), on the same pulse.
    private const float OutlineLow = 0f;
    private const float OutlineHigh = 0.1f;
    // The one glow each creature holds lives this long (it is removed with the aura long before).
    private const float GlowLifetime = 100000f;

    private static GameObject? _prefab;
    private static bool _warned;
    private static readonly ParticleSystem.Particle[] One = new ParticleSystem.Particle[1];

    internal static GameObject? Make(Character creature)
    {
        if (Prefab() is not { } prefab) return null;
        float size = Mathf.Clamp(Body(creature) * SizePerMeter, MinSize, MaxSize);
        GameObject aura = new("TempestRite_MorgenPouchAura");
        aura.transform.SetParent(creature.transform, false);
        aura.transform.position = creature.GetCenterPoint();
        if (prefab.transform.Find(GlowPart) is { } glowPart) Glow(Part(glowPart.gameObject, aura), size);
        if (prefab.transform.Find(LightPart) is { } lightPart && Part(lightPart.gameObject, aura).GetComponent<Light>() is { } light)
        {
            light.color = Violet;
            light.intensity = LightIntensity;
            light.range = LightRange * size;
        }
        return aura;
    }

    // How big the creature is: the average of its body's length and thickness, in meters.
    private static float Body(Character creature)
    {
        if (creature.m_collider is not { } body || !body) return 2f * creature.GetRadius();
        float scale = body.transform.lossyScale.x;
        float thickness = 2f * body.radius * scale;
        float length = Mathf.Max(body.height * scale, thickness);
        return (length + thickness) / 2f;
    }

    // All glows and lights on one clock: up and down at an even pace, softened at the turns.
    internal static void Pulse(List<(Character Creature, GameObject Aura)> auras)
    {
        float phase = Time.time / PulseSeconds % 1f;
        float wave = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(2f * phase - 1f));
        Color32 color = new Color(Violet.r, Violet.g, Violet.b, Mathf.Lerp(GlowLow, GlowHigh, wave));
        float intensity = LightIntensity * Mathf.Lerp(LightLow, 1f, wave);
        PouchOutline.Show(auras, Mathf.Lerp(OutlineLow, OutlineHigh, wave));
        foreach (var (_, aura) in auras)
        {
            if (!aura) continue;
            foreach (Light light in aura.GetComponentsInChildren<Light>()) light.intensity = intensity;
            foreach (ParticleSystem particles in aura.GetComponentsInChildren<ParticleSystem>())
            {
                if (particles.GetParticles(One) == 0) particles.Emit(1);
                if (particles.GetParticles(One) == 0) continue;
                One[0].startColor = color;
                particles.SetParticles(One, 1);
            }
        }
    }

    // A copy of one part of the effect under the aura, without its scripts (flicker, fading out, removing itself).
    private static GameObject Part(GameObject part, GameObject aura)
    {
        GameObject copy = Object.Instantiate(part, aura.transform, false);
        copy.transform.localPosition = Vector3.zero;
        foreach (MonoBehaviour script in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.Destroy(script);
        foreach (AudioSource audio in copy.GetComponentsInChildren<AudioSource>(true)) Object.Destroy(audio);
        return copy;
    }

    // The glow, violet, sized to the creature and following it: one particle that never fades or changes by itself
    // (Pulse sets its strength), instead of the Guardian Power's single flash.
    private static void Glow(GameObject glow, float size)
    {
        foreach (ParticleSystem particles in glow.GetComponentsInChildren<ParticleSystem>(true))
        {
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = 1;
            main.startLifetime = GlowLifetime;
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor = new Color(Violet.r, Violet.g, Violet.b, GlowLow);
            main.startSizeMultiplier *= size * GlowScale;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
            fade.enabled = false;
            ParticleSystem.SizeOverLifetimeModule grow = particles.sizeOverLifetime;
            grow.enabled = false;
            particles.Play(false);
            particles.Emit(1);
        }
    }

    private static GameObject? Prefab()
    {
        if (_prefab) return _prefab;
        if (ZNetScene.instance && ZNetScene.instance.GetPrefab(PrefabName) is { } found) return _prefab = found;
        // Else from a Guardian Power's own start effects.
        if (ObjectDB.instance)
            foreach (StatusEffect effect in ObjectDB.instance.m_StatusEffects)
                if (effect && effect.m_startEffects?.m_effectPrefabs is { } prefabs)
                    foreach (EffectList.EffectData data in prefabs)
                        if (data.m_prefab && data.m_prefab.name == PrefabName) return _prefab = data.m_prefab;
        if (!_warned)
        {
            _warned = true;
            Plugin.ModLogger.LogWarning($"{Feature}: no {PrefabName} found: marked creatures go without a glow.");
        }
        return null;
    }
}
