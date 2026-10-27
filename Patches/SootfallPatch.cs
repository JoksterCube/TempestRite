using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// The Sootfall Rite: in the Ashlands, the smoke and ash particles that come with its weather are turned off, and the
// heat shimmer drawn over the screen near lava is not shown. Only the look: the heat still burns as usual.
internal static class SootfallPatch
{
    private const string Feature = "Sootfall";

    // The weather particles turned off, to turn them back on when the rite ends or the player leaves the Ashlands.
    private static GameObject[]? _suppressed;

    internal static void ClearSession() => _suppressed = null;

    private static bool Active => !PatchGuard.IsDisabled(Feature) && WeatherState.Active(EffectSlot.Ashlands) == WeatherKind.Sootfall;

    // The Ashlands and the sea around them, where the camera is (as the game decides its weather).
    private static bool InAshlands()
    {
        Camera? camera = Utils.GetMainCamera();
        if (!camera) return false;
        Vector3 position = camera!.transform.position;
        return WorldGenerator.IsAshlands(position.x, position.z);
    }

    // After the game switched its weather's particles on: off again while the rite goes on in the Ashlands. The game
    // still counts them as the current ones, so it doesn't switch them on again every frame.
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetEnv))]
    private static class NoAshlandsSmoke
    {
        private static void Postfix(EnvMan __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                GameObject[]? current = __instance.m_currentPSystems;
                if (Active && InAshlands())
                {
                    if (current == null || current == _suppressed) return;
                    __instance.SetParticleArrayEnabled(current, false);
                    _suppressed = current;
                }
                else if (_suppressed != null)
                {
                    if (_suppressed == current) __instance.SetParticleArrayEnabled(current, true);
                    _suppressed = null;
                }
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // The game shows the heat shimmer from the local player's heat level; it is hidden again right after.
    [HarmonyPatch(typeof(Character), nameof(Character.UpdateHeatEffects))]
    private static class NoHeatShimmer
    {
        private static void Postfix(Character __instance)
        {
            if (!Active || __instance != Player.m_localPlayer) return;
            try
            {
                GameCamera camera = GameCamera.instance;
                if (camera && camera.m_heatDistortImageEffect && camera.m_heatDistortImageEffect.enabled)
                    camera.m_heatDistortImageEffect.enabled = false;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}
