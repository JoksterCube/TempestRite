using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Tendril-Draw Rite, in the blessed player's own game (it shoots, throws and owns what it throws):
// - an arrow or crossbow bolt it shoots is sometimes not taken from the inventory (Ammo Saved Chance);
// - a weapon it throws (a spear) is dropped by the game where it strikes, as usual, and that dropped item then flies
//   back to the player at Spear Return Speed, butt first with its tip pointing away (straight back along its
//   way), leaving a trail of green motes (in this player's game only), and is picked up: back in the hands when
//   they are empty (once the throw ends, when it is caught point blank before that). One that strikes close by is
//   picked up at once. The dropped item is an ordinary one, so every player sees it fly, mod or not. No other
//   player can pick it up while it comes back; if the game hands it to someone else (the server often takes far or
//   floating items) it is taken back, so it always flies all the way back (waiting while its thrower teleports).
//   It is let go where it is (an ordinary item on the ground, anyone's to take) only when the player dies or
//   leaves; with no room in the inventory it falls at the player's feet.
internal static class TendrilReturn
{
    private const string Feature = "Tendril-Draw Rite";
    // Within this far of the player it is picked up at once.
    private const float AtOnce = 2.5f;
    // Flying, it is caught within this far of the player's middle.
    private const float CatchDistance = 0.8f;

    // How fast it turns to fly straight back.
    private const float TurnRate = 14f;
    // The trail: motes left behind per meter flown, and how long the last ones stay after it is caught.
    private const float TrailPerMeter = 14f;
    private const float TrailFade = 1.5f;
    private static readonly Color Green = new(0.55f, 1f, 0.45f);
    private static readonly Color Pale = new(0.85f, 1f, 0.8f);

    private sealed class Flight(ItemDrop item, float started, Vector3 shaft, Vector3 middle, ParticleSystem? trail)
    {
        internal ItemDrop Item { get; } = item;
        internal float Started { get; } = started;
        // Along the weapon toward its tip, and its middle, in the item's own space.
        internal Vector3 Shaft { get; } = shaft;
        internal Vector3 Middle { get; } = middle;
        internal ParticleSystem? Trail { get; } = trail;
    }

    private static readonly List<Flight> Flights = [];

    // A weapon caught while the throw was still going on, so the game couldn't take it up in the hands: it is, once
    // the throw ends, if the hands are still empty. Given up after EquipWaitSeconds.
    private const float EquipWaitSeconds = 3f;
    private static ItemDrop.ItemData? _toEquip;
    private static float _toEquipUntil;

    // Arrows and bolts: whether this shot keeps its ammo.
    internal static bool SavesAmmo(Humanoid? shooter, ItemDrop.ItemData? weapon)
    {
        if (!shooter || shooter != Player.m_localPlayer || weapon == null || !Blessings.IsActive(WeatherKind.TendrilDraw)) return false;
        if (weapon.m_shared.m_skillType is not (Skills.SkillType.Bows or Skills.SkillType.Crossbows)) return false;
        float chance = Mathf.Clamp01(PluginConfig.TendrilAmmoChance.Value);
        return chance > 0f && UnityEngine.Random.value < chance;
    }

    // Whether this thrown item, about to be dropped where it struck, comes back to its thrower.
    internal static bool Returns(Character? thrower, ItemDrop.ItemData? item) =>
        thrower && thrower == Player.m_localPlayer && item != null && item.IsWeapon() && Blessings.IsActive(WeatherKind.TendrilDraw);

    // The game just dropped a returning weapon where it struck: it starts back.
    internal static void Dropped(ItemDrop? item)
    {
        if (!item) return;
        Rigidbody body = item!.GetComponent<Rigidbody>();
        if (body)
        {
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
        }
        SetColliders(item, false);
        (Vector3 shaft, Vector3 middle) = Shape(item);
        ParticleSystem? trail = null;
        try { trail = Trail(item.transform.TransformPoint(middle)); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        Flights.Add(new Flight(item, Time.time, shaft, middle, trail));
    }

    // The weapon's length runs along the longest side of its biggest mesh; its tip is the end that struck farther from
    // the thrower (it flew tip first).
    private static (Vector3 Shaft, Vector3 Middle) Shape(ItemDrop item)
    {
        Transform root = item.transform;
        MeshFilter? body = null;
        float biggest = 0f;
        foreach (MeshFilter filter in item.GetComponentsInChildren<MeshFilter>())
        {
            if (!filter.sharedMesh) continue;
            float size = filter.sharedMesh.bounds.size.sqrMagnitude;
            if (size <= biggest) continue;
            biggest = size;
            body = filter;
        }
        if (!body) return (Vector3.forward, Vector3.zero);
        Bounds bounds = body!.sharedMesh.bounds;
        Vector3 scaled = Vector3.Scale(bounds.size, body.transform.lossyScale);
        Vector3 axis = scaled.x >= scaled.y && scaled.x >= scaled.z ? Vector3.right : scaled.y >= scaled.z ? Vector3.up : Vector3.forward;
        Vector3 world = body.transform.TransformDirection(axis).normalized;
        Player? player = Player.m_localPlayer;
        if (player && Vector3.Dot(world, root.position - player!.transform.position) < 0f) world = -world;
        return (root.InverseTransformDirection(world).normalized, root.InverseTransformPoint(body.transform.TransformPoint(bounds.center)));
    }

    // Green motes left hanging behind it as it flies, fading out.
    private static ParticleSystem? Trail(Vector3 at)
    {
        Material? material = DomainVisuals.EmberMaterial();
        if (!material) return null;
        GameObject go = new("TempestRite_TendrilTrail");
        go.transform.position = at;
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(Green, Pale);
        main.gravityModifier = -0.03f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 10f;
        emission.rateOverDistance = TrailPerMeter;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 0.8f;

        Gradient fade = new();
        fade.SetKeys(
            [new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f)],
            [new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.95f, 0.1f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0f, 1f)]);
        ParticleSystem.ColorOverLifetimeModule overLife = particles.colorOverLifetime;
        overLife.enabled = true;
        overLife.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particles.Play();
        return particles;
    }

    // The flight is over: the trail stops and its last motes fade where they hang.
    private static void EndTrail(Flight flight)
    {
        ParticleSystem? trail = flight.Trail;
        if (!trail) return;
        trail!.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        UnityEngine.Object.Destroy(trail.gameObject, TrailFade);
    }

    // Whether this dropped item is still flying back. While it is, no one else may take it: another player picks an
    // item up by asking its owner (this player) for it, and that ask is refused.
    internal static bool IsReturning(ItemDrop? item)
    {
        if (!item) return false;
        foreach (Flight flight in Flights)
            if (flight.Item == item) return true;
        return false;
    }

    // Every frame: the flying weapons move on toward the player.
    internal static void Update()
    {
        if (_toEquip != null) EquipCaught();
        if (Flights.Count == 0) return;
        if (PatchGuard.IsDisabled(Feature))
        {
            LetAllGo();
            return;
        }
        try { Fly(); }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            LetAllGo();
        }
    }

    private static void Fly()
    {
        Player? player = Player.m_localPlayer;
        bool gone = !player || player!.IsDead();
        float step = Mathf.Max(1f, PluginConfig.TendrilReturnSpeed.Value) * Time.deltaTime;
        for (int i = Flights.Count - 1; i >= 0; i--)
        {
            Flight flight = Flights[i];
            ItemDrop item = flight.Item;
            if (!item)
            {
                Flights.RemoveAt(i);
                EndTrail(flight);
                continue;
            }
            ZNetView view = item.GetComponent<ZNetView>();
            if (!view || !view.IsValid())
            {
                Flights.RemoveAt(i);
                EndTrail(flight);
                continue;
            }
            if (gone)
            {
                Flights.RemoveAt(i);
                EndTrail(flight);
                view.ClaimOwnership();
                LetGo(item);
                continue;
            }
            // The game can hand a far or floating item to someone else (often the server); it is taken back, or it
            // would hang where it is, out of anyone's reach.
            if (!view.IsOwner()) view.ClaimOwnership();
            // While its thrower teleports it waits, then flies on.
            if (player!.IsTeleporting()) continue;

            Vector3 target = player.GetCenterPoint();
            Vector3 toward = target - item.transform.position;
            float distance = toward.magnitude;
            bool arrived = distance <= CatchDistance || (distance <= AtOnce && Time.time - flight.Started < 0.1f);
            if (!arrived)
            {
                if (step >= distance - CatchDistance) arrived = true;
                else
                {
                    Transform at = item.transform;
                    at.position += toward / distance * step;
                    // Turned to fly straight back, tip pointing away from the player.
                    Vector3 away = at.position - target;
                    if (away.sqrMagnitude > 0.0001f)
                    {
                        Quaternion facing = Quaternion.FromToRotation(at.TransformDirection(flight.Shaft), away.normalized) * at.rotation;
                        at.rotation = Quaternion.Slerp(at.rotation, facing, 1f - Mathf.Exp(-TurnRate * Time.deltaTime));
                    }
                    if (flight.Trail) flight.Trail!.transform.position = at.TransformPoint(flight.Middle);
                }
            }
            if (!arrived) continue;

            Flights.RemoveAt(i);
            EndTrail(flight);
            item.transform.position = target;
            view.ClaimOwnership();
            ItemDrop.ItemData caught = item.m_itemData;
            bool handsFree = HandsFree(player);
            if (!player.Pickup(item.gameObject, autoequip: true, autoPickupDelay: false))
            {
                if (!item) continue;
                item.transform.position = player.transform.position + Vector3.up * 0.3f;
                LetGo(item);
                continue;
            }
            // The game doesn't take up a weapon during an attack: a spear caught point blank comes back mid-throw.
            if (handsFree && !player.IsItemEquiped(caught))
            {
                _toEquip = caught;
                _toEquipUntil = Time.time + EquipWaitSeconds;
            }
        }
    }

    private static void EquipCaught()
    {
        Player? player = Player.m_localPlayer;
        ItemDrop.ItemData item = _toEquip!;
        if (!player || player!.IsDead() || Time.time > _toEquipUntil || !player.GetInventory().ContainsItem(item) || player.IsItemEquiped(item)
            || !HandsFree(player))
        {
            _toEquip = null;
            return;
        }
        if (player.InAttack() || player.InDodge()) return;
        _toEquip = null;
        try { player.EquipItem(item); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // As the game checks before taking up a picked up weapon: nothing in the right hand and no two-handed weapon in
    // the left, held or hidden away.
    private static bool HandsFree(Player player) =>
        player.m_rightItem == null && player.m_hiddenRightItem == null
        && (player.m_leftItem == null || !player.m_leftItem.IsTwoHanded())
        && (player.m_hiddenLeftItem == null || !player.m_hiddenLeftItem.IsTwoHanded());

    private static void LetAllGo()
    {
        foreach (Flight flight in Flights)
        {
            EndTrail(flight);
            if (flight.Item) LetGo(flight.Item);
        }
        Flights.Clear();
    }

    // Back to an ordinary item: it falls where it is.
    private static void LetGo(ItemDrop item)
    {
        SetColliders(item, true);
        Rigidbody body = item.GetComponent<Rigidbody>();
        if (body) body.isKinematic = false;
    }

    // Off while it flies, so it doesn't bump the player or anything on its way.
    private static void SetColliders(ItemDrop item, bool on)
    {
        foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
            if (!collider.isTrigger) collider.enabled = on;
    }
}
