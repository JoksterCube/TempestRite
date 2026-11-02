using System;
using JoksterCube.TempestRite.Common;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// While the Packward or Carrion Rite goes on, a line in the log now and then: what it reaches and what it steers, to
// tell why a creature isn't. (Creatures show what the rites do only by their icon, see CreatureIcons; motes on what the
// Verdant, Vat-Boil and Pitch-Bellows Rites speed up are shown by RiteSpeedup.)
internal static class RiteEffects
{
    private const string Feature = "Rite report";
    private const float ReportInterval = 10f;
    private static float _nextReport;

    internal static void ClearSession() => _nextReport = 0f;

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { Report(); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Report()
    {
        WeatherKind kind = WeatherState.Active(EffectSlot.Creatures);
        if (kind == WeatherKind.None || Time.time < _nextReport || !ZNetScene.instance || !ZNet.instance) return;
        _nextReport = Time.time + ReportInterval;
        int inReach = 0, owned = 0, steered = 0;
        long me = ZDOMan.GetSessionID();
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!character || character.IsPlayer() || !character.GetBaseAI() || !character.m_nview || !character.m_nview.IsValid()) continue;
            if (!WeatherState.Reaches(kind, character.transform.position, CreatureRites.Buffer)) continue;
            inReach++;
            if (character.m_nview.GetZDO().GetOwner() == me) owned++;
            if (CreatureRites.ShownBy(character) != CreatureRites.Shown.None) steered++;
        }
        Vector3 at = WeatherState.Position(kind);
        string doing = kind == WeatherKind.Packward ? "shunning the area" : "walking to the obliterator";
        Plugin.ModLogger.LogDebug($"{WeatherKinds.Name(kind)} at ({at.x:0}, {at.z:0}): {inReach} creatures within reach, {owned} run by this game, {steered} {doing}.");
    }
}
