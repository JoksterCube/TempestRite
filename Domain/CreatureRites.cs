using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// Which creatures the Packward and Carrion Rites steer, and what each one is doing, as every game can tell (for icons
// and effects). The steering itself is in CreaturePatches, run by the game that owns the creature.
internal static class CreatureRites
{
    // Close enough to the obliterator: a drawn creature mills about within this.
    internal const float Arrived = 10f;
    // Packward: beyond the radius, a creature keeps out of this much more: it doesn't follow a target inside, and one
    // that fled goes this far out before it turns around.
    internal const float Buffer = 8f;

    // Saved on the creature by its owner: what the Packward Rite has it do (a Mood).
    internal static readonly int MoodKey = "TempestRite_Packward".GetStableHashCode();

    internal enum Mood
    {
        None = 0,
        // Inside the area: finds its way out.
        Fleeing = 1,
        // Its target is inside the area: it waits at the edge.
        HoldingBack = 2
    }

    internal enum Shown
    {
        None,
        Scared,
        Drawn
    }

    // Saved on a creature by its owner when it gets hurt under the Packward Rite (world time, in ticks): its pack
    // nearby fights along.
    internal static readonly int HurtKey = "TempestRite_PackwardHurt".GetStableHashCode();

    // Not players, bosses, tamed creatures, raid creatures, or those the rite's list leaves out.
    internal static bool Steered(Character character, WeatherKind kind)
    {
        if (!character || character.IsPlayer() || character.IsBoss() || character.IsTamed()) return false;
        ZNetView view = character.m_nview;
        return view && view.IsValid() && Steered(view.GetZDO(), kind);
    }

    // The same, from the creature's saved data alone (for objects not loaded as creatures, like ownership).
    internal static bool Steered(ZDO zdo, WeatherKind kind)
    {
        if (zdo.GetBool(ZDOVars.s_eventCreature)) return false;
        int prefab = zdo.GetPrefab();
        return kind == WeatherKind.Packward
            ? !PluginConfig.PackwardExcludedPrefabs().Contains(prefab)
            : !PluginConfig.CarrionExcludedPrefabs().Contains(prefab);
    }

    // Whether a creature was hurt under the Packward Rite within the fight-back time.
    internal static bool HurtLately(ZDO zdo)
    {
        long hurt = zdo.GetLong(HurtKey, 0L);
        if (hurt <= 0L || !ZNet.instance) return false;
        double ago = (ZNet.instance.GetTime().Ticks - hurt) / (double)System.TimeSpan.TicksPerSecond;
        return ago >= 0.0 && ago < PluginConfig.PackwardFightSeconds.Value;
    }

    internal static Mood MoodOf(ZDO zdo) => (Mood)zdo.GetInt(MoodKey, 0);

    // Saved only when it changes, so nothing is sent while it stays the same.
    internal static void SetMood(ZDO zdo, Mood mood)
    {
        if (MoodOf(zdo) != mood) zdo.Set(MoodKey, (int)mood);
    }

    // Whether a Carrion Rite draws this creature right now: within reach, not there yet, not alerted, no target.
    internal static bool Drawn(Character character, BaseAI ai)
    {
        if (!WeatherState.IsActive(WeatherKind.Carrion)) return false;
        Vector3 position = character.transform.position;
        if (!WeatherState.Reaches(WeatherKind.Carrion, position)) return false;
        if (Utils.DistanceXZ(position, WeatherState.Position(WeatherKind.Carrion)) <= Arrived) return false;
        return !ai.IsAlerted() && !ai.HaveTarget() && Steered(character, WeatherKind.Carrion);
    }

    // What a creature shows, in any game.
    internal static Shown ShownBy(Character character)
    {
        if (!character || character.IsPlayer()) return Shown.None;
        BaseAI? ai = character.GetBaseAI();
        if (!ai || !character.m_nview || !character.m_nview.IsValid()) return Shown.None;
        if (WeatherState.IsActive(WeatherKind.Packward))
        {
            if (MoodOf(character.m_nview.GetZDO()) == Mood.None) return Shown.None;
            // Left over from a rite that ended or moved: ignored.
            bool near = WeatherState.Reaches(WeatherKind.Packward, character.transform.position, Buffer + 4f);
            return near && Steered(character, WeatherKind.Packward) ? Shown.Scared : Shown.None;
        }
        return Drawn(character, ai!) ? Shown.Drawn : Shown.None;
    }
}
