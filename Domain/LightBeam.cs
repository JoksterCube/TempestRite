using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// A tall beam of light, a local object in this game only: one flat strip that turns to face the camera, bright in the
// middle and soft at the edges, fading out at the top. It ignores fog, so it is seen from far away.
internal sealed class LightBeam
{
    private static readonly Dictionary<(Color, Color), Texture2D> Textures = [];

    private readonly GameObject _root;
    private readonly Material _material;
    private readonly Vector3 _at;

    private LightBeam(GameObject root, Material material, Vector3 at)
    {
        _root = root;
        _material = material;
        _at = at;
    }

    internal Vector3 At => _at;

    internal static LightBeam Make(string name, Vector3 at, float height, float width, Color core, Color halo)
    {
        GameObject root = new(name);
        root.transform.position = at;

        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(strip.GetComponent<Collider>());
        strip.transform.SetParent(root.transform, false);
        // A quad is 1 unit square around its middle.
        strip.transform.localPosition = Vector3.up * (height / 2f);
        strip.transform.localScale = new Vector3(width, height, 1f);
        MeshRenderer renderer = strip.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        // Sprites/Default draws both sides and blends by alpha.
        Shader? shader = Shader.Find("Sprites/Default");
        Material material = shader ? new Material(shader) : new Material(renderer.sharedMaterial);
        material.mainTexture = Texture(core, halo);
        material.color = Color.white;
        material.renderQueue = 3100;
        renderer.sharedMaterial = material;

        LightBeam beam = new(root, material, at);
        beam.Face();
        return beam;
    }

    internal bool Alive => _root;

    // How bright it is (0 to 1).
    internal void SetBrightness(float brightness)
    {
        if (_material) _material.color = new Color(1f, 1f, 1f, Mathf.Clamp01(brightness));
    }

    // Turn the strip about its upright axis to face the camera.
    internal void Face()
    {
        Camera? camera = Utils.GetMainCamera();
        if (!_root || !camera) return;
        Vector3 toCamera = camera!.transform.position - _at;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude > 0.0001f) _root.transform.rotation = Quaternion.LookRotation(toCamera);
    }

    internal void Destroy()
    {
        if (_root) Object.Destroy(_root);
        if (_material) Object.Destroy(_material);
    }

    // Bright core fading to a colored glow at the sides; full strength from the bottom up, fading out over the top 40%.
    private static Texture2D Texture(Color core, Color halo)
    {
        if (Textures.TryGetValue((core, halo), out Texture2D made) && made) return made;
        const int w = 64, h = 128;
        Texture2D texture = new(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };
        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            float vertical = Mathf.Clamp01((1f - v) / 0.4f);
            for (int x = 0; x < w; x++)
            {
                float d = Mathf.Abs((x + 0.5f) / w - 0.5f) * 2f;
                float coreness = Mathf.Exp(-(d * d) / 0.02f);
                float glow = Mathf.Exp(-(d * d) / 0.18f) * (1f - d);
                float alpha = Mathf.Clamp01(0.85f * coreness + 0.3f * glow) * vertical;
                Color color = Color.Lerp(halo, core, coreness);
                color.a = alpha;
                pixels[y * w + x] = color;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        Textures[(core, halo)] = texture;
        return texture;
    }
}
