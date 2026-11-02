using System;
using JoksterCube.TempestRite.Common;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// While a Crimson, Ragnarök or Hoard-Bane Rite goes on, the obliterator it was started at:
// - can't be destroyed: it loses no health to anything, losing the structure below it included, and isn't torn down
//   (in the game that owns it, see ObliteratorBastionPatches). Once the rite ends it is an ordinary piece again: if
//   nothing holds it up by then, it falls;
// - is never picked as a target by any creature, and creatures already after it let it go;
// - sends a beam of light straight up, in the rite's color, for every player (a local object in each game).
internal static class ObliteratorBastion
{
    private const string Feature = "Obliterator bastion";
    private const string BeamFeature = "Obliterator beam";
    private static readonly int Prefab = "incinerator".GetStableHashCode();
    private const float BeamHeight = 600f;
    private const float BeamWidth = 2.4f;
    // The beam starts this far below the obliterator, so it meets the ground even where the ground dips.
    private const float BeamSink = 2f;
    // A slow pulse: its brightness swings between these, once every this many seconds.
    private const float PulseLow = 0.7f;
    private const float PulseHigh = 1f;
    private const float PulsePeriod = 3f;

    private static LightBeam? _beam;
    private static WeatherKind _beamKind;

    internal static void ClearSession() => HideBeam();

    // Whether this is the obliterator of the incursion rite going on.
    internal static bool Guards(Component? piece)
    {
        if (!piece || PatchGuard.IsDisabled(Feature)) return false;
        try
        {
            WeatherKind kind = IncursionArena.Kind;
            if (kind == WeatherKind.None) return false;
            if (Utils.DistanceXZ(piece!.transform.position, WeatherState.Position(kind)) >= IncursionArena.SamePlace) return false;
            ZNetView nview = piece.GetComponentInParent<ZNetView>();
            return nview && nview.IsValid() && nview.GetZDO().GetPrefab() == Prefab;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return false;
        }
    }

    // Every frame: the beam stands while an incursion rite goes on.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(BeamFeature)) return;
        try
        {
            WeatherKind kind = IncursionArena.Kind;
            if (kind == WeatherKind.None || !Player.m_localPlayer)
            {
                HideBeam();
                return;
            }
            Vector3 at = WeatherState.Position(kind) + Vector3.down * BeamSink;
            if (_beam == null || !_beam.Alive || _beamKind != kind || Vector3.Distance(_beam.At, at) > IncursionArena.SamePlace)
            {
                HideBeam();
                Color tint = IncursionArena.Tint(kind);
                _beam = LightBeam.Make("TempestRite_ObliteratorBeam", at, BeamHeight, BeamWidth, Color.Lerp(tint, Color.white, 0.7f), tint);
                _beamKind = kind;
            }
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * (2f * Mathf.PI / PulsePeriod));
            _beam.SetBrightness(Mathf.Lerp(PulseLow, PulseHigh, wave));
            _beam.Face();
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(BeamFeature, exception);
            HideBeam();
        }
    }

    private static void HideBeam()
    {
        _beam?.Destroy();
        _beam = null;
        _beamKind = WeatherKind.None;
    }
}
