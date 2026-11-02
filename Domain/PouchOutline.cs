using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace JoksterCube.TempestRite.Domain;

// A thin violet outline around the creatures the Morgen-Pouch would take (see PetPouch), pulsing with their glow (see
// PouchAura). Drawn by this game's camera only, so only this player sees it. The same way PingMark outlines what is
// pinged: the creatures are written into a stencil mask, then drawn again shifted a little in eight directions only
// outside it, then the mask is cleared.
internal static class PouchOutline
{
    private const string Feature = "Morgen-Pouch Rite";
    private const CameraEvent RenderEvent = CameraEvent.AfterForwardAlpha;
    // The deferred renderer uses this stencil bit too, so the whole screen's is cleared first (as PingMark does).
    private const int StencilBit = 64;
    private const float Width = 1.5f;
    private const float Diagonal = 0.70710678f;
    private const int MaxRenderers = 64;
    private static readonly Color Violet = new(0.8f, 0.6f, 1f);
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private static readonly Vector2[] Directions =
    [
        new(1f, 0f), new(-1f, 0f), new(0f, 1f), new(0f, -1f),
        new(Diagonal, Diagonal), new(-Diagonal, Diagonal), new(Diagonal, -Diagonal), new(-Diagonal, -Diagonal)
    ];

    private static readonly List<GameObject> Targets = [];
    private static readonly List<Renderer> Found = [];
    private static readonly List<(Renderer Renderer, int SubMeshes)> Drawn = [];
    private static float _alpha;

    private static CommandBuffer? _buffer;
    private static Camera? _camera;
    private static Shader? _shader;
    private static Material? _clear, _mask, _outline;
    private static Mesh? _screen;
    private static bool _hooked, _missing;

    // The creatures to outline this frame, and how strong (0 to 1).
    internal static void Show(List<(Character Creature, GameObject Aura)> marked, float alpha)
    {
        Targets.Clear();
        foreach (var (creature, _) in marked)
            if (creature) Targets.Add(creature.gameObject);
        _alpha = alpha;
        if (Targets.Count == 0 || _hooked) return;
        Camera.onPreRender += OnPreRender;
        _hooked = true;
    }

    internal static void ClearSession() => Targets.Clear();

    private static void OnPreRender(Camera camera)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { Draw(camera); }
        catch (Exception exception)
        {
            _buffer?.Clear();
            PatchGuard.Fail(Feature, exception);
        }
    }

    private static void Draw(Camera camera)
    {
        if (!GameCamera.instance || camera.gameObject != GameCamera.instance.gameObject) return;
        if (camera != _camera) Attach(camera);
        _buffer!.Clear();
        if (Targets.Count == 0 || !Materials()) return;
        Collect();
        if (Drawn.Count == 0) return;

        Color color = Violet;
        color.a = _alpha;
        _outline!.SetColor(ColorId, color);
        Matrix4x4 view = camera.worldToCameraMatrix;
        Matrix4x4 projection = camera.projectionMatrix;
        Vector2 pixel = new(2f * Width / camera.pixelWidth, 2f * Width / camera.pixelHeight);

        _buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        _buffer.DrawMesh(_screen!, Matrix4x4.identity, _clear!);
        _buffer.SetViewProjectionMatrices(view, projection);
        DrawAll(_mask!);
        foreach (Vector2 direction in Directions)
        {
            Matrix4x4 shift = Matrix4x4.Translate(new Vector3(direction.x * pixel.x, direction.y * pixel.y, 0f));
            _buffer.SetViewProjectionMatrices(view, shift * projection);
            DrawAll(_outline);
        }
        _buffer.SetViewProjectionMatrices(view, projection);
        DrawAll(_clear!);
    }

    // The creatures' own meshes that are on screen: not their glow, nor a level of detail not shown now.
    private static void Collect()
    {
        Drawn.Clear();
        foreach (GameObject target in Targets)
        {
            if (!target) continue;
            target.GetComponentsInChildren(false, Found);
            foreach (Renderer renderer in Found)
            {
                if (Drawn.Count >= MaxRenderers) return;
                if (!renderer || !renderer.enabled || !renderer.isVisible) continue;
                Mesh? mesh = renderer switch
                {
                    SkinnedMeshRenderer skinned => skinned.sharedMesh,
                    MeshRenderer when renderer.TryGetComponent(out MeshFilter filter) => filter.sharedMesh,
                    _ => null
                };
                if (mesh) Drawn.Add((renderer, mesh!.subMeshCount));
            }
        }
    }

    private static void DrawAll(Material material)
    {
        foreach (var (renderer, subMeshes) in Drawn)
            for (int i = 0; i < subMeshes; i++)
                _buffer!.DrawRenderer(renderer, material, i);
    }

    private static void Attach(Camera camera)
    {
        if (_camera && _buffer != null) _camera!.RemoveCommandBuffer(RenderEvent, _buffer);
        _buffer ??= new CommandBuffer { name = "TempestRite Morgen-Pouch Outline" };
        camera.AddCommandBuffer(RenderEvent, _buffer);
        _camera = camera;
    }

    private static bool Materials()
    {
        if (_clear && _mask && _outline && _screen) return true;
        if (_missing) return false;
        // UI/Default ships with every Unity build and has stencil, color mask and depth test settings.
        _shader ??= Shader.Find("UI/Default");
        if (!_shader)
        {
            _missing = true;
            Plugin.ModLogger.LogWarning($"{Feature}: no UI/Default shader: marked creatures go without an outline.");
            return false;
        }
        _clear = Make(CompareFunction.Always, StencilOp.Zero, false, Color.clear);
        _mask = Make(CompareFunction.Always, StencilOp.Replace, false, Color.clear);
        _outline = Make(CompareFunction.NotEqual, StencilOp.Keep, true, Violet);
        _screen = new Mesh
        {
            name = "TempestRite Stencil Clear",
            hideFlags = HideFlags.HideAndDontSave,
            vertices = [new(-1f, -1f, 0f), new(-1f, 1f, 0f), new(1f, 1f, 0f), new(1f, -1f, 0f)],
            triangles = [0, 1, 2, 0, 2, 3]
        };
        return true;
    }

    private static Material Make(CompareFunction comparison, StencilOp operation, bool writeColor, Color color)
    {
        Material material = new(_shader) { hideFlags = HideFlags.HideAndDontSave };
        material.SetColor(ColorId, color);
        material.SetFloat("_Stencil", StencilBit);
        material.SetFloat("_StencilComp", (float)comparison);
        material.SetFloat("_StencilOp", (float)operation);
        material.SetFloat("_StencilReadMask", StencilBit);
        material.SetFloat("_StencilWriteMask", StencilBit);
        material.SetFloat("_ColorMask", writeColor ? (float)ColorWriteMask.All : 0f);
        material.SetFloat("unity_GUIZTestMode", (float)CompareFunction.Always);
        return material;
    }
}
