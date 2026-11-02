using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Hearth-Kindle Rite: a player resting by a fire within its area (the game's Resting, which needs a fire close by)
// becomes Rested Resting Speed Multiplier times as fast, and the Rested they get while resting there lasts Rested
// Duration Multiplier times as long. Each game works it out for its own player, where they rest. Burning fires that
// warm (those the game lets you rest by) now and then give off little rest signs (see RiteSpeedup).
internal static class RestKindle
{
    internal static readonly SpedWork Work = new FireWork();

    internal static bool Reaches(Character character) =>
        character && character == Player.m_localPlayer && WeatherState.Reaches(WeatherKind.HearthKindle, character.transform.position);

    internal static float Resting => Mathf.Max(1f, PluginConfig.KindleRestingMultiplier.Value);

    internal static float Rested => Mathf.Max(1f, PluginConfig.KindleRestedMultiplier.Value);

    private sealed class FireWork() : SpedWork(WeatherKind.HearthKindle)
    {
        internal override float Speed => 1f;
        internal override bool ShowMotes => PluginConfig.ShowHearthKindleMotes.IsOn();

        internal override float? Takes(GameObject prefab)
        {
            if (!prefab.GetComponent<Fireplace>()) return null;
            foreach (EffectArea area in prefab.GetComponentsInChildren<EffectArea>(true))
                if ((area.m_type & EffectArea.Type.Heat) != 0) return 0.7f;
            return null;
        }

        internal override bool Busy(ZDO zdo, ZNetView view) => view.GetComponent<Fireplace>() is { } fire && fire && fire.IsBurning();

        internal override void Hurry(ZDO zdo, ZNetView view, double seconds) { }

        // Above the flames.
        internal override Vector3 MotesAt(ZNetView view, Vector3 position) => position + Vector3.up * 1.2f;
    }
}
