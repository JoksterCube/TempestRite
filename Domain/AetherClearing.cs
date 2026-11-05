using System;
using JoksterCube.TempestRite.Common;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Aether-Clear Rite: while it goes on, its obliterator works as a Wisplight as big as the Domain area. The game
// keeps the Mistlands mist out of each Wisplight's ball (a Demister with a force field): no mist is made inside it, the
// force field pushes what drifts in back out, and nothing inside counts as in the mist, so creatures there see as if
// there were none. Each game places its own (nothing is sent): the mist and what creatures see are worked out by each
// game for itself.
internal static class AetherClearing
{
    private const string Feature = "Aether-Clear Rite";
    // The ball sits this high over the obliterator.
    private const float Lift = 1.5f;
    // The game's Wisplight item, whose ball's force field is copied.
    private const string WisplightItem = "Demister";

    private static GameObject? _ball;
    private static ParticleSystemForceField? _field;
    private static ParticleSystemForceField? _template;
    private static bool _templateLooked;

    internal static void ClearSession()
    {
        Remove();
        _template = null;
        _templateLooked = false;
    }

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { Run(); }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            Remove();
        }
    }

    private static void Run()
    {
        WeatherKind kind = WeatherKind.AetherClear;
        Vector3 at = WeatherState.Position(kind);
        // Within a Crimson, Ragnarök or Hoard-Bane Rite's Blood Grounds the Domain rites do nothing.
        if (!WeatherState.IsActive(kind) || WeatherState.Suspended(kind, at))
        {
            Remove();
            return;
        }
        float radius = Settings.PluginConfig.Radius(kind);
        Vector3 center = at + Vector3.up * Lift;
        if (!_ball || !_field) Place(center);
        if (_ball!.transform.position != center) _ball.transform.position = center;
        if (!Mathf.Approximately(_field!.endRange, radius)) _field.endRange = radius;
    }

    private static void Place(Vector3 center)
    {
        Remove();
        GameObject ball = new("TempestRite_AetherClear");
        ball.transform.position = center;
        // The Demister finds its force field as it wakes, so the field goes on first.
        ball.SetActive(false);
        ParticleSystemForceField field = ball.AddComponent<ParticleSystemForceField>();
        Copy(Template(), field);
        ball.AddComponent<Demister>();
        ball.SetActive(true);
        _ball = ball;
        _field = field;
    }

    private static void Remove()
    {
        if (_ball) UnityEngine.Object.Destroy(_ball);
        _ball = null;
        _field = null;
    }

    // How the Wisplight's ball pushes the mist; none if it can't be found (no mist is made inside all the same, and what
    // is there fades).
    private static ParticleSystemForceField? Template()
    {
        if (_templateLooked) return _template;
        if (!ObjectDB.instance) return null;
        _templateLooked = true;
        GameObject item = ObjectDB.instance.GetItemPrefab(WisplightItem);
        if (item && item.GetComponent<ItemDrop>() is { } drop && drop
            && drop.m_itemData.m_shared.m_equipStatusEffect is SE_Demister effect && effect.m_ballPrefab
            && effect.m_ballPrefab.GetComponentInChildren<Demister>(true) is { } demister && demister)
            _template = demister.GetComponent<ParticleSystemForceField>();
        if (!_template) Plugin.ModLogger.LogWarning($"{Feature}: the Wisplight's force field wasn't found; mist already there only fades.");
        return _template;
    }

    private static void Copy(ParticleSystemForceField? from, ParticleSystemForceField to)
    {
        if (!from)
        {
            to.shape = ParticleSystemForceFieldShape.Sphere;
            to.startRange = 0f;
            return;
        }
        to.shape = from!.shape;
        to.startRange = from.startRange;
        to.length = from.length;
        to.directionX = from.directionX;
        to.directionY = from.directionY;
        to.directionZ = from.directionZ;
        to.gravity = from.gravity;
        to.gravityFocus = from.gravityFocus;
        to.rotationSpeed = from.rotationSpeed;
        to.rotationAttraction = from.rotationAttraction;
        to.rotationRandomness = from.rotationRandomness;
        to.drag = from.drag;
        to.multiplyDragByParticleSize = from.multiplyDragByParticleSize;
        to.multiplyDragByParticleVelocity = from.multiplyDragByParticleVelocity;
        to.vectorField = from.vectorField;
        to.vectorFieldSpeed = from.vectorFieldSpeed;
        to.vectorFieldAttraction = from.vectorFieldAttraction;
    }
}
