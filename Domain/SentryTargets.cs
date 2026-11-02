using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Sentry-Sight Rite: ballistas within its area aim at every hostile creature they can see, whatever trophies are set
// in them, and never at players, tamed animals or spawners (with Spare Passive Animals on, not at animals that only flee
// either). They shoot the most pressing creature first rather than the closest (see Score), only with a clear shot at
// its body (see ClearShot), and aim properly where the game's aim is off (see AimPoint, Turn).
// A ballista picks its target on the game that owns it, so ballistas there are handed to players with the mod (see
// WeatherOwnership). Ballistas with bolts now and then give off steel-blue bolts (see RiteSpeedup).
internal static class SentryTargets
{
    internal static readonly SpedWork Work = new SentryWork();

    // The ballista picking its target right now under the rite (see SentrySightPatches).
    internal static Turret? Aiming;

    // Ballistas under the rite see this many times as far as usual.
    internal const float SightMultiplier = 2f;

    internal static bool Reaches(Component turret) => turret && WeatherState.Reaches(WeatherKind.SentrySight, turret.transform.position);

    // The most pressing creature the ballista can see that it should shoot (see Score), with the game's own sight check
    // and a clear shot at its body (see ClearShot). Spawners, training dummies and anything else without a mind of its
    // own are left alone.
    internal static Character? Pick(Transform me, Vector3 eye, float hearRange, float viewRange, float viewAngle, bool alerted, bool mistVision,
        bool passiveAggressive)
    {
        bool spare = PluginConfig.SentrySpareAnimals.IsOn();
        Turret? turret = Aiming;
        Character? current = turret ? turret!.m_target : null;
        float bolt = turret ? BoltDamage(turret!) : 0f;
        ItemDrop.ItemData? ammo = turret ? turret!.m_lastAmmo ?? turret.GetAmmoItem() : null;
        float boltSpeed = ammo != null ? ammo.m_shared.m_attack.m_projectileVel : 0f;
        Character? best = null;
        float bestScore = float.MinValue;
        List<Character> all = Character.GetAllCharacters();
        foreach (Character character in all)
        {
            if (!character || character.IsPlayer() || character.IsTamed() || character.IsDead()) continue;
            if (!IsFoe(character)) continue;
            BaseAI ai = character.GetBaseAI();
            if (ai.IsSleeping()) continue;
            if (spare && ai is AnimalAI) continue;
            if (!BaseAI.CanSenseTarget(me, eye, hearRange, viewRange, viewAngle, alerted, mistVision, character, passiveAggressive, isTamed: false)) continue;
            float distance = Vector3.Distance(character.transform.position, me.position);
            // The current target's speed is measured (see MovingAt); others go by what the game reports, close enough to rank.
            Vector3 moving = character == current ? MovingAt(turret!, character) : character.GetVelocity();
            if (Walking(character)) moving.y = 0f;
            float chance = HitChance(character.GetRadius(), moving.magnitude, distance, boltSpeed);
            float score = Score(character.GetMaxHealth(), character.GetHealth(), StrongestHit(character), distance, viewRange, MoodOf(ai), bolt,
                character == current, chance);
            if (score <= bestScore) continue;
            if (turret && IsBlocked(turret!, character)) continue;
            // The same check as right before a shot, so it never locks onto a creature it then won't shoot.
            if (turret && (ammo != null
                    ? !ClearFlight(turret!, eye, character.GetCenterPoint(), character.GetCenterPoint(), ammo, character)
                    : !ClearShot(turret!, eye, character.GetCenterPoint(), character)))
                continue;
            best = character;
            bestScore = score;
        }
        return best;
    }

    // How much a ballista wants to shoot a creature, all rules multiplied together:
    // - threat: a tenth of its full health plus its strongest hit, so tough and hard-hitting creatures come first
    //   (a troll or a deathsquito over a greyling);
    // - closeness: five times the pull point-blank, falling off quickly (half by a fifth of its sight) to under one at
    //   the edge, so close foes come first unless a far one is much more dangerous;
    // - mood: creatures hitting a building count one and a half, fighting or hunting fully, wandering or fleeing half;
    // - wounded: up to twice for one nearly dead, and half again more when one bolt finishes it;
    // - the creature it already aims at keeps a quarter more, so it doesn't swing back and forth between equals;
    // - hit chance: times how likely the bolt is to land (see HitChance), so it doesn't waste bolts on small, fast or
    //   far creatures while easy ones are around.
    internal static float Score(float maxHealth, float health, float strongestHit, float distance, float viewRange, Mood mood, float boltDamage,
        bool current, float hitChance = 1f)
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        health = Mathf.Clamp(health, 0f, maxHealth);
        float threat = maxHealth / 10f + Mathf.Max(0f, strongestHit) + 1f;
        float closeness = 1f / (0.2f + Mathf.Clamp01(distance / Mathf.Max(1f, viewRange)));
        float pressing = mood switch { Mood.Raiding => 1.5f, Mood.Fighting => 1f, _ => 0.5f };
        float wounded = 2f - health / maxHealth;
        if (boltDamage > 0f && health <= boltDamage) wounded *= 1.5f;
        return threat * closeness * pressing * wounded * (current ? 1.25f : 1f) * Mathf.Clamp01(hitChance);
    }

    // How likely a bolt is to land, from a quarter to sure: a creature standing still is a sure hit; one moving can turn
    // while the bolt flies, the more likely to slip away the further it gets in that time compared to how wide it is.
    // So big and slow beats small and fast, and close beats far (a shorter flight).
    internal static float HitChance(float radius, float speed, float distance, float boltSpeed)
    {
        if (boltSpeed <= 0f) return 1f;
        float flight = distance / boltSpeed;
        float drift = 0.5f * speed * flight; // how far off a turn or stop during the flight can put it
        float size = Mathf.Max(0.2f, radius);
        return Mathf.Clamp(size / (size + drift), 0.25f, 1f);
    }

    internal enum Mood
    {
        Idle, // wandering, grazing or fleeing
        Fighting, // alerted, hunting or after someone
        Raiding // hitting a building
    }

    private static Mood MoodOf(BaseAI ai)
    {
        if (ai is not MonsterAI monster) return Mood.Idle;
        if (monster.GetStaticTarget()) return Mood.Raiding;
        return monster.IsAlerted() || monster.HuntPlayer() || monster.GetTargetCreature() ? Mood.Fighting : Mood.Idle;
    }

    private static bool IsSpawner(Character character) =>
        character.GetComponentInChildren<SpawnArea>(true) || character.GetComponentInChildren<CreatureSpawner>(true);

    // The hardest hit among the attacks the creature carries, with its star level's extra damage.
    private static float StrongestHit(Character character)
    {
        if (character is not Humanoid humanoid || humanoid.GetInventory() is not { } inventory) return 0f;
        float best = 0f;
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            best = Mathf.Max(best, item.m_shared.m_damages.GetTotalDamage());
        return best * (1f + Mathf.Max(0, character.GetLevel() - 1) * 0.5f);
    }

    private static float BoltDamage(Turret turret)
    {
        ItemDrop.ItemData? ammo = turret.m_lastAmmo;
        return ammo != null ? ammo.m_shared.m_damages.GetTotalDamage() : 0f;
    }

    // ---- Line of sight ----

    private static int _boltMask;
    private static readonly RaycastHit[] Hits = new RaycastHit[32];

    // Whether a bolt from the ballista's eye reaches the target at the point with nothing in the way but other foes: no
    // wall, ground, rock, tree, spawner, player or tamed animal. It looks at everything a bolt can hit (the game's own
    // list); the game itself only checks that the creature's head can be seen.
    internal static bool ClearShot(Turret turret, Vector3 from, Vector3 to, Character target) =>
        ClearSegment(turret, from, to, target, 0f, endsAtTarget: true, sparedOnly: false);

    // How wide a berth a bolt gives players, tamed animals and spawners, in metres.
    private const float SpareMargin = 0.35f;
    // How far past the creature the path is checked for those, should the bolt miss and fly on.
    private const float CheckBeyond = 8f;

    // Whether nothing stands in one stretch of the bolt's path. With a radius it sweeps a ball instead of a thin line;
    // endsAtTarget lets it touch the ground right under the aim point; sparedOnly only minds players, tamed animals and
    // spawners (for the stretch past the creature, where a wall would just stop the bolt).
    private static bool ClearSegment(Turret turret, Vector3 from, Vector3 to, Character target, float radius, bool endsAtTarget, bool sparedOnly)
    {
        if (_boltMask == 0)
            _boltMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net",
                "character_ghost", "hitbox", "character_noenv", "vehicle");
        Vector3 path = to - from;
        float length = path.magnitude;
        if (length < 0.05f) return true;
        Vector3 direction = path / length;
        int count = radius > 0f
            ? Physics.SphereCastNonAlloc(from, radius, direction, Hits, length, _boltMask, QueryTriggerInteraction.Ignore)
            : Physics.RaycastNonAlloc(from, direction, Hits, length, _boltMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider collider = Hits[i].collider;
            if (!collider || collider.GetComponentInParent<Turret>() == turret) continue;
            Character? hit = collider.GetComponentInParent<Character>();
            if (hit == target) continue;
            bool spared = hit ? !IsFoe(hit!) : collider.GetComponentInParent<SpawnArea>() || collider.GetComponentInParent<CreatureSpawner>();
            if (spared) return false;
            if (sparedOnly || hit) continue;
            if (endsAtTarget && Hits[i].distance > length - 0.3f) continue; // the ground right under the aim point
            return false;
        }
        return true;
    }

    // How long a ballista leaves a creature alone after its path turned out not clear, in seconds.
    private const float BlockedFor = 3f;
    private static readonly Dictionary<Turret, (Character Target, float Until)> Blocked = new();

    private static bool IsBlocked(Turret turret, Character character) =>
        Blocked.TryGetValue(turret, out (Character Target, float Until) blocked) && blocked.Target == character && Time.time < blocked.Until;

    private static void Block(Turret turret, Character character)
    {
        if (Blocked.Count > 64)
        {
            GoneTurrets.Clear();
            foreach (KeyValuePair<Turret, (Character Target, float Until)> pair in Blocked)
                if (!pair.Key || Time.time >= pair.Value.Until) GoneTurrets.Add(pair.Key!);
            foreach (Turret gone in GoneTurrets) Blocked.Remove(gone);
        }
        Blocked[turret] = (character, Time.time + BlockedFor);
    }

    private static readonly List<Vector3> PathPoints = new();

    // Whether the bolt's whole real flight to the aim is clear (as ClearShot, along its arc rather than a straight
    // line), giving players, tamed animals and spawners a wide berth all the way and for a stretch past the creature.
    internal static bool ClearFlight(Turret turret, Vector3 eye, Vector3 aim, Vector3 goal, ItemDrop.ItemData ammo, Character target)
    {
        float speed = Mathf.Max(1f, ammo.m_shared.m_attack.m_projectileVel);
        (float gravity, float drag) = FlightOf(ammo);
        FlightPath(eye, (aim - eye).normalized * speed, gravity, drag, goal, Time.fixedDeltaTime, PathPoints);
        for (int i = 1; i < PathPoints.Count; i++)
        {
            bool last = i == PathPoints.Count - 1;
            if (!ClearSegment(turret, PathPoints[i - 1], PathPoints[i], target, 0f, endsAtTarget: last, sparedOnly: false)) return false;
            if (!ClearSegment(turret, PathPoints[i - 1], PathPoints[i], target, SpareMargin, endsAtTarget: last, sparedOnly: true)) return false;
        }
        if (PathPoints.Count < 2) return true;
        Vector3 end = PathPoints[PathPoints.Count - 1];
        Vector3 onward = (end - PathPoints[PathPoints.Count - 2]).normalized;
        return ClearSegment(turret, end, end + onward * CheckBeyond, target, SpareMargin, endsAtTarget: false, sparedOnly: true);
    }

    // The bolt's flight as the game moves it, as a few points from the eye to as far out (on the map) as the goal.
    internal static void FlightPath(Vector3 from, Vector3 velocity, float gravity, float drag, Vector3 goal, float step, List<Vector3> points)
    {
        points.Clear();
        points.Add(from);
        Vector2 flat = new(goal.x - from.x, goal.z - from.z);
        float reach = flat.magnitude;
        if (reach < 0.5f || (gravity <= 0f && drag <= 0f))
        {
            points.Add(goal);
            return;
        }
        flat /= reach;
        float dt = Mathf.Max(0.005f, step);
        Vector3 position = from;
        float done = 0f;
        for (int i = 0; i < 1000; i++)
        {
            velocity += Vector3.down * (gravity * dt);
            velocity -= velocity.normalized * (velocity.sqrMagnitude * drag * dt);
            Vector3 next = position + velocity * dt;
            float out1 = Vector2.Dot(new Vector2(next.x - from.x, next.z - from.z), flat);
            if (out1 >= reach || out1 <= done)
            {
                points.Add(next);
                return;
            }
            if (i % 5 == 4) points.Add(next);
            position = next;
            done = out1;
        }
        points.Add(position);
    }

    // A creature a bolt may hit on its way: not a player, tamed animal, spawner or dummy.
    private static bool IsFoe(Character character) =>
        !character.IsPlayer() && !character.IsTamed() && character.m_faction != Character.Faction.TrainingDummy && character.GetBaseAI() &&
        !IsSpawner(character);

    // ---- Aim ----

    // How far off the ballista may point and still shoot, in degrees. The game shoots anywhere within about 50 degrees.
    private const float ShootWithin = 4f;
    // How far off the bolt may be set straight onto the aim as it leaves.
    private const float SteerWithin = 12f;

    private static readonly Dictionary<string, (float Gravity, float Drag)> Flights = new();

    // Where to point to hit the creature: the middle of its body where it will be when the bolt gets there, raised just
    // enough that the bolt's real flight (its gravity and drag, step by step as the game moves it) comes down on it.
    // The game measures the flight time on a flat map that leaves out north-south distance, aims at the feet plus half
    // a body, and ignores the drop.
    // Pure physics: the ballista's own prediction factor is left out, it only made it lead too far.
    internal static Vector3 AimPoint(Turret turret, Character target, ItemDrop.ItemData ammo) => AimPoint(turret, target, ammo, out _, out _);

    internal static Vector3 AimPoint(Turret turret, Character target, ItemDrop.ItemData ammo, out float seconds, out Vector3 velocity)
    {
        Vector3 eye = turret.m_eye.transform.position;
        Vector3 center = target.GetCenterPoint();
        velocity = MovingAt(turret, target);
        float speed = Mathf.Max(1f, ammo.m_shared.m_attack.m_projectileVel);
        (float gravity, float drag) = FlightOf(ammo);
        seconds = Vector3.Distance(center, eye) / speed;
        Vector3 goal = center;
        float lift = 0f;
        bool walking = Walking(target) && velocity.sqrMagnitude > 0.01f && ZoneSystem.instance;
        float groundNow = walking ? ZoneSystem.instance!.GetGroundHeight(center) : 0f;
        for (int i = 0; i < 5; i++)
        {
            goal = Ahead(center, velocity, seconds, walking, groundNow);
            if (gravity <= 0f && drag <= 0f)
            {
                seconds = Vector3.Distance(goal, eye) / speed;
                continue;
            }
            Vector3 aim = goal + Vector3.up * lift;
            if (!Fly(eye, (aim - eye).normalized * speed, gravity, drag, goal, Time.fixedDeltaTime, out float height, out seconds)) break;
            lift += goal.y - height;
        }
        return goal + Vector3.up * lift;
    }

    // Where the creature will be after the given time: walking ones follow the slope of the ground (running down a
    // hill it drops, so the bolt doesn't pass over its head), others keep going straight.
    internal static Vector3 Ahead(Vector3 center, Vector3 velocity, float seconds, bool walking, float groundNow)
    {
        Vector3 goal = center + velocity * seconds;
        if (!walking) return goal;
        float rise = ZoneSystem.instance.GetGroundHeight(goal) - groundNow;
        float run = new Vector2(goal.x - center.x, goal.z - center.z).magnitude;
        // No steeper than a creature can climb: a bigger step is a cliff, a roof or a gap in the terrain.
        if (Mathf.Abs(rise) <= run) goal.y += rise;
        return goal;
    }

    // The same, worked out from scratch for the creature (for the check right before a shot).
    internal static Vector3 Ahead(Character target, Vector3 velocity, float seconds)
    {
        Vector3 center = target.GetCenterPoint();
        bool walking = Walking(target) && velocity.sqrMagnitude > 0.01f && ZoneSystem.instance;
        return Ahead(center, velocity, seconds, walking, walking ? ZoneSystem.instance!.GetGroundHeight(center) : 0f);
    }

    // Moves a bolt as the game does, until it is as far out (on the map) as the goal: how high it is there and when.
    // False when it falls short or the goal is straight above or below.
    internal static bool Fly(Vector3 from, Vector3 velocity, float gravity, float drag, Vector3 goal, float step, out float height, out float seconds)
    {
        height = goal.y;
        seconds = 0f;
        Vector2 flat = new(goal.x - from.x, goal.z - from.z);
        float reach = flat.magnitude;
        if (reach < 0.5f) return false;
        flat /= reach;
        float dt = Mathf.Max(0.005f, step);
        Vector3 position = from;
        float done = 0f;
        for (int i = 0; i < 1000; i++)
        {
            velocity += Vector3.down * (gravity * dt);
            velocity -= velocity.normalized * (velocity.sqrMagnitude * drag * dt);
            Vector3 next = position + velocity * dt;
            float out1 = Vector2.Dot(new Vector2(next.x - from.x, next.z - from.z), flat);
            if (out1 >= reach)
            {
                float part = out1 > done ? (reach - done) / (out1 - done) : 1f;
                height = Mathf.Lerp(position.y, next.y, part);
                seconds = (i + part) * dt;
                return true;
            }
            if (out1 <= done) return false; // no longer getting closer
            position = next;
            done = out1;
        }
        return false;
    }

    // ---- Target speed ----

    // How far back the ballista looks to tell how fast its creature moves, in seconds.
    private const float TrackWindow = 0.25f;
    private const int TrackSamples = 24;

    // Where a ballista's creature was, most recent last, as this game sees it.
    private sealed class Track
    {
        internal Character? Target;
        internal readonly float[] Times = new float[TrackSamples];
        internal readonly Vector3[] Positions = new Vector3[TrackSamples];
        internal int Count;
        internal int Next;
    }

    private static readonly Dictionary<Turret, Track> Tracks = new();
    private static readonly List<Turret> GoneTurrets = new();

    // Notes where the ballista's creature is now (every step, also while it reloads).
    internal static void Watch(Turret turret, Character target)
    {
        if (!Tracks.TryGetValue(turret, out Track track))
        {
            if (Tracks.Count > 64) Prune();
            Tracks[turret] = track = new Track();
        }
        if (track.Target != target)
        {
            track.Target = target;
            track.Count = 0;
            track.Next = 0;
        }
        float now = Time.time;
        int last = (track.Next + TrackSamples - 1) % TrackSamples;
        if (track.Count > 0 && track.Times[last] >= now) return;
        track.Times[track.Next] = now;
        track.Positions[track.Next] = target.transform.position;
        track.Next = (track.Next + 1) % TrackSamples;
        track.Count = Mathf.Min(track.Count + 1, TrackSamples);
    }

    private static void Prune()
    {
        GoneTurrets.Clear();
        foreach (KeyValuePair<Turret, Track> pair in Tracks)
            if (!pair.Key || !pair.Value.Target) GoneTurrets.Add(pair.Key!);
        foreach (Turret gone in GoneTurrets) Tracks.Remove(gone);
    }

    // How fast the creature really moves where this game shows it (and where the bolt will meet it): from how far it
    // went over the last quarter second, not the game's reported speed, which for creatures run by another player's
    // game lags behind (still running after it stopped). Walking creatures only count their speed along the ground,
    // so steps and bobbing don't throw the aim up or down; flying and swimming ones count it all.
    internal static Vector3 MovingAt(Turret turret, Character target)
    {
        if (!Tracks.TryGetValue(turret, out Track track) || track.Target != target || track.Count < 2) return Vector3.zero;
        int newest = (track.Next + TrackSamples - 1) % TrackSamples;
        float now = track.Times[newest];
        int oldest = newest;
        for (int i = 1; i < track.Count; i++)
        {
            int index = (newest - i + TrackSamples) % TrackSamples;
            if (now - track.Times[index] > TrackWindow) break;
            oldest = index;
        }
        float span = now - track.Times[oldest];
        if (span < 0.05f) return Vector3.zero; // too soon to tell: aim where it is
        return Measured(track.Positions[oldest], track.Positions[newest], span, Walking(target));
    }

    internal static Vector3 Measured(Vector3 from, Vector3 to, float seconds, bool walking)
    {
        Vector3 velocity = (to - from) / Mathf.Max(0.001f, seconds);
        if (walking) velocity.y = 0f;
        return velocity;
    }

    internal static bool Walking(Character target) => !target.IsFlying() && !target.IsSwimming();

    internal static (float Gravity, float Drag) FlightOf(ItemDrop.ItemData ammo)
    {
        string name = ammo.m_shared.m_name;
        if (Flights.TryGetValue(name, out (float, float) flight)) return flight;
        GameObject? prefab = ammo.m_shared.m_attack.m_attackProjectile;
        flight = prefab && prefab!.GetComponent<Projectile>() is { } projectile && projectile ? (projectile.m_gravity, projectile.m_drag) : (0f, 0f);
        Flights[name] = flight;
        return flight;
    }

    // The game's turning, pointed at AimPoint, letting it shoot only once it points within ShootWithin degrees. False
    // leaves it to the game (no target, no bolts, cooling down).
    internal static bool Turn(Turret turret)
    {
        Character target = turret.m_target;
        if (!target) return false;
        Watch(turret, target);
        if (!turret.HasAmmo() || turret.IsCoolingDown() || !turret.m_eye) return false;
        turret.m_lastAmmo ??= turret.GetAmmoItem();
        ItemDrop.ItemData? ammo = turret.m_lastAmmo;
        if (ammo == null) return false;

        Vector3 aim = AimPoint(turret, target, ammo);
        Quaternion wanted = Quaternion.LookRotation((aim - turret.m_eye.transform.position).normalized, Vector3.up);
        Vector3 euler = wanted.eulerAngles;
        float baseYaw = turret.transform.rotation.eulerAngles.y;
        bool outOfReach = false;
        if (turret.m_horizontalAngle >= 0f)
        {
            float yaw = Mathf.DeltaAngle(0f, euler.y - baseYaw);
            if (Mathf.Abs(yaw) > turret.m_horizontalAngle)
            {
                wanted.eulerAngles = new Vector3(euler.x, Mathf.Sign(yaw) * turret.m_horizontalAngle + baseYaw, euler.z);
                outOfReach = true; // it can't turn that far: it would only shoot past the creature
            }
        }
        Transform body = turret.m_turretBody.transform;
        // Turns twice as fast as usual, so it is on target sooner.
        Quaternion turned = Utils.RotateTorwardsSmooth(body.rotation, wanted, turret.m_lastRotation, 2f * turret.m_turnRate * Time.fixedDeltaTime,
            turret.m_lookAcceleration, turret.m_lookDeacceleration, turret.m_lookMinDegreesDelta);
        turret.m_lastRotation = body.rotation;
        body.rotation = turret.m_baseBodyRotation * turned;
        turret.m_turretNeck.transform.rotation = turret.m_baseNeckRotation * Quaternion.Euler(0f, body.rotation.eulerAngles.y, body.rotation.eulerAngles.z);
        // The game shoots once this is at least m_shootWhenAimDiff.
        turret.m_aimDiffToTarget = !outOfReach && Quaternion.Angle(turned, wanted) <= ShootWithin ? 1f : -1f;
        return true;
    }

    // What a shot under the rite changed, to put back after it.
    internal sealed class Shot
    {
        internal Quaternion? Eye;
        internal ItemDrop.ItemData.SharedData? Ammo;
        internal float Spread;
    }

    // Just before a ballista under the rite shoots: no shot with anything but foes in the way (it looks for another
    // target soon), a bolt nearly on the aim leaves straight on it, and with no spread (the game turns each bolt a
    // random few degrees, which misses even creatures standing still).
    internal static bool BeforeShot(Turret turret, out Shot? shot)
    {
        shot = null;
        Character target = turret.m_target;
        ItemDrop.ItemData? ammo = turret.GetAmmoItem(); // what the game is about to shoot
        if (!target || ammo == null || !turret.m_eye) return true;
        Watch(turret, target); // the aim is worked out again right as the bolt leaves, with where it is this very moment
        Transform eye = turret.m_eye.transform;
        Vector3 aim = AimPoint(turret, target, ammo, out float seconds, out Vector3 velocity);
        Vector3 goal = Ahead(target, velocity, seconds);
        Vector3 forward = aim - eye.position;
        float off = Vector3.Angle(eye.forward, forward);
        if (off > SteerWithin)
        {
            // Pointing elsewhere (it just swung to a new creature): a bolt now would fly off into whatever is there.
            turret.m_aimDiffToTarget = -1f;
            return false;
        }
        if (!ClearFlight(turret, eye.position, aim, goal, ammo, target))
        {
            // Leave it alone for a while and look for another creature right away, rather than staring at it.
            Block(turret, target);
            turret.m_updateTargetTimer = 0f;
            turret.m_aimDiffToTarget = -1f;
            return false;
        }
        shot = new Shot { Ammo = ammo.m_shared, Spread = ammo.m_shared.m_attack.m_projectileAccuracy, Eye = eye.rotation };
        ammo.m_shared.m_attack.m_projectileAccuracy = 0f;
        eye.rotation = Quaternion.LookRotation(forward, Vector3.up);
        return true;
    }

    internal static void AfterShot(Turret turret, Shot? shot)
    {
        if (shot == null) return;
        if (shot.Ammo != null) shot.Ammo.m_attack.m_projectileAccuracy = shot.Spread;
        if (shot.Eye is { } rotation && turret && turret.m_eye) turret.m_eye.transform.rotation = rotation;
    }

    private sealed class SentryWork() : SpedWork(WeatherKind.SentrySight)
    {
        // Nothing is sped up: the ballistas only puff.
        internal override float Speed => 1f;
        internal override bool ShowMotes => PluginConfig.ShowSentrySightMotes.IsOn();
        internal override float? Takes(GameObject prefab) => prefab.GetComponent<Turret>() is { } turret && turret && turret.m_targetEnemies ? 0.8f : null;

        // Loaded with bolts.
        internal override bool Busy(ZDO zdo, ZNetView view) => view.GetComponent<Turret>() is { } turret && turret && turret.HasAmmo();

        internal override void Hurry(ZDO zdo, ZNetView view, double seconds) { }

        internal override Vector3 MotesAt(ZNetView view, Vector3 position) =>
            view.GetComponent<Turret>() is { } turret && turret && turret.m_eye ? turret.m_eye.transform.position + Vector3.up * 0.4f : position + Vector3.up * 1.5f;
    }
}
