using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The kinship rites' particles made in code, the way the Domain rites make their motes (a Sprites/Default material on
// a particle system), some showing the game's own icons; and those icons for the meads' effects (see KinshipFx):
// - Anvil-Spark: a few repair hammers (the crafting stations' repair button) slowly circling up around the player,
//   and the forge's sparks at each strike.
// - Sky-Call: small purple and white sparkles flickering around the player for a few seconds after the strike.
internal static class KinshipMotes
{
    private const string Feature = "Kinship rites motes";
    private const float RenderQueue = 3100;
    private const string RepairKey = "RepairButton";
    private const string RestedKey = "Rested";

    // Made once and kept (the same in every world); null for an icon that couldn't be found.
    private static readonly Dictionary<string, Texture?> Textures = [];
    private static readonly Dictionary<string, Material?> Materials = [];

    // ---- Each rite ----

    internal static void Hammers(Player player, Color tint, float seconds)
    {
        ParticleSystem? hammers = Make(player.transform, Vector3.up * 0.4f, Material(RepairKey, RepairIcon()), local: true);
        if (!hammers) return;
        ParticleSystem.MainModule main = hammers!.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 2.6f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.38f, 0.46f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Color.Lerp(Color.white, tint, 0.3f));
        Ring(hammers, 0.85f);
        // Slowly round the player, rising, rocking a little as if tapping.
        Velocity(hammers, up: 0.4f, orbit: 1f);
        Spin(hammers, 0.6f);
        Run(hammers, rate: 1.2f, burst: 2, seconds);
    }

    // Pictures rising gently around the player, swaying (when a mead's effect has no hearts to show them).
    internal static void Rising(Player player, Texture[] pictures, float seconds)
    {
        for (int i = 0; i < pictures.Length; i++)
        {
            ParticleSystem? rising = Make(player.transform, Vector3.up * 0.8f, Material("Rising" + pictures[i].name, pictures[i]), local: false);
            if (!rising) continue;
            ParticleSystem.MainModule main = rising!.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 2.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.35f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            ParticleSystem.ShapeModule shape = rising.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.5f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            ParticleSystem.NoiseModule noise = rising.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 0.4f;
            Run(rising, rate: 4f / pictures.Length, burst: 2, seconds);
        }
    }

    internal static void Sparkles(Player player, Color color, float seconds)
    {
        ParticleSystem? sparkles = Make(player.transform, Vector3.up, DomainVisuals.StarMaterial(), local: false);
        if (!sparkles) return;
        ParticleSystem.MainModule main = sparkles!.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
        ParticleSystem.ShapeModule shape = sparkles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.8f;
        ParticleSystem.NoiseModule noise = sparkles.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 1.5f;
        // Twinkling.
        ParticleSystem.SizeOverLifetimeModule twinkle = sparkles.sizeOverLifetime;
        twinkle.enabled = true;
        twinkle.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.2f, 1.2f),
            new Keyframe(0.4f, 0.6f), new Keyframe(0.6f, 1.1f), new Keyframe(1f, 0f)));
        Run(sparkles, rate: 45f, burst: 20, seconds);
    }

    // The emitting stops; what is out fades out on its own.
    internal static void Stop(GameObject? motes)
    {
        if (!motes) return;
        foreach (ParticleSystem system in motes!.GetComponentsInChildren<ParticleSystem>(true))
            system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        UnityEngine.Object.Destroy(motes, 3f);
    }

    // The forge's sparks (as the Pitch-Bellows Rite shows at the obliterator), scale times as big, at a point.
    internal static void ForgeSparks(Vector3 at, float scale)
    {
        GameObject? prefab = DomainVisuals.ForgeSparks();
        if (!prefab) return;
        GameObject sparks = UnityEngine.Object.Instantiate(prefab!, at, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
        sparks.transform.localScale = prefab!.transform.localScale * scale;
        foreach (ParticleSystem system in sparks.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = system.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
        UnityEngine.Object.Destroy(sparks, 5f);
    }

    // ---- Pieces ----

    // A particle system showing material, at offset on parent, not emitting yet; its particles fade in and out.
    // local: they move with the parent (circling it).
    private static ParticleSystem? Make(Transform parent, Vector3 offset, Material? material, bool local)
    {
        if (!material) return null;
        GameObject go = new("TempestRite_KinshipMotes");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = offset;
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 5f;
        main.playOnAwake = false;
        main.simulationSpace = local ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
        main.maxParticles = 120;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        Gradient fade = new();
        fade.SetKeys(
            [new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f)],
            [new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f)]);
        ParticleSystem.ColorOverLifetimeModule overLife = particles.colorOverLifetime;
        overLife.enabled = true;
        overLife.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    // Born on a flat ring round the center.
    private static void Ring(ParticleSystem particles, float radius)
    {
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 0f;
        shape.rotation = new Vector3(90f, 0f, 0f);
    }

    private static void Velocity(ParticleSystem particles, float up, float orbit)
    {
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(up);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(orbit);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f);
    }

    private static void Spin(ParticleSystem particles, float most)
    {
        ParticleSystem.RotationOverLifetimeModule spin = particles.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-most, most);
    }

    // Emits for seconds (a burst at once), then stops; gone once the last have faded.
    private static void Run(ParticleSystem particles, float rate, int burst, float seconds)
    {
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;
        particles.Play();
        if (burst > 0) particles.Emit(burst);
        GameObject go = particles.gameObject;
        KinshipFx.After(seconds, () => Stop(go));
    }

    // ---- Icons ----

    // An item's own icon (CookedMeat, Bread...).
    internal static Texture? ItemIcon(string prefab) => Icon(prefab, () =>
    {
        GameObject? item = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(prefab) : null;
        ItemDrop? drop = item ? item!.GetComponent<ItemDrop>() : null;
        return drop ? drop!.m_itemData.GetIcon() : null;
    });

    // The Rested status's icon.
    internal static Texture? RestedIcon() => Icon(RestedKey, () =>
    {
        StatusEffect? rested = ObjectDB.instance ? ObjectDB.instance.GetStatusEffect(SEMan.s_statusEffectRested) : null;
        return rested ? rested!.m_icon : null;
    });

    // The hammer on the crafting stations' repair button: the button's own picture (not its frame or glow).
    private static Texture? RepairIcon() => Icon(RepairKey, () =>
    {
        InventoryGui? gui = InventoryGui.instance;
        if (!gui || !gui!.m_repairButton) return null;
        UnityEngine.UI.Image? frame = gui.m_repairButton.GetComponent<UnityEngine.UI.Image>();
        UnityEngine.UI.Image? found = null;
        foreach (UnityEngine.UI.Image image in gui.m_repairButton.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            if (image == frame || image == gui.m_repairButtonGlow || !image.sprite) continue;
            found = image;
            string name = image.sprite.name.ToLowerInvariant();
            if (name.Contains("repair") || name.Contains("hammer")) break;
        }
        return found ? found!.sprite : null;
    });

    // An icon's pixels as a texture of their own (a game icon sits in a shared atlas). Not kept while the game isn't
    // loaded enough to have it yet, so it is tried again.
    private static Texture? Icon(string key, Func<Sprite?> find)
    {
        if (Textures.TryGetValue(key, out Texture? made)) return made;
        Sprite? sprite = find();
        if (!sprite)
        {
            if (!ObjectDB.instance || !InventoryGui.instance) return null;
            Plugin.ModLogger.LogWarning($"{Feature}: no icon for {key}, it is not shown.");
            Textures[key] = null;
            return null;
        }
        Texture? texture = null;
        try { texture = Copy(sprite!); }
        catch (Exception exception) { Plugin.ModLogger.LogWarning($"{Feature}: the icon for {key} could not be copied ({exception.Message})."); }
        Textures[key] = texture;
        return texture;
    }

    // A Sprites/Default material showing a texture.
    internal static Material? Material(string key, Texture? texture)
    {
        if (!texture) return null;
        if (Materials.TryGetValue(key, out Material? made) && made) return made;
        Shader? shader = Shader.Find("Sprites/Default");
        if (!shader) return null;
        Material material = new(shader)
        {
            name = $"TempestRite_Kinship{key}",
            mainTexture = texture,
            renderQueue = (int)RenderQueue,
            hideFlags = HideFlags.HideAndDontSave
        };
        Materials[key] = material;
        return material;
    }

    private static Material? SpriteMaterial(string key, Sprite? sprite) => sprite ? Material(key, sprite!.texture) : null;

    // A sprite's own pixels as a texture of their own, read back through the graphics card (the atlas can't be read).
    private static Texture2D Copy(Sprite sprite)
    {
        Texture2D source = sprite.texture;
        Rect rect = sprite.textureRect;
        RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture previous = RenderTexture.active;
        try
        {
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            int width = Mathf.RoundToInt(rect.width), height = Mathf.RoundToInt(rect.height);
            Texture2D copy = new(width, height, TextureFormat.RGBA32, false)
            {
                name = $"TempestRite_Kinship{sprite.name}",
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            copy.ReadPixels(new Rect(rect.x, rect.y, width, height), 0, 0);
            copy.Apply(false, true);
            return copy;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }
    }
}
