using System;
using System.Collections;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;
using Object = UnityEngine.Object;

namespace JoksterCube.TempestRite.Domain;

// The Reckoning rites' treasure, in the game that owns the obliterator when the server says a rite was survived: chests
// struck down by lightning around the obliterator one after another. The Ragnarök Rite gives a chest for each boss slain
// (see RagnarokRush), the Crimson Rite one with its reward (see CrimsonRaids), the Hoard-Bane Rite one with its treasure
// (see HoardBaneRush).
// - Each treasure is split on purpose into a few containers of its biome (the Treasure Chests setting): a Ragnarök
//   boss's are its own lands' (Eikthyr's the Meadows'), the others those of the biome the obliterator stands in. The
//   first is the biome's dungeon chest, the others any of its containers (chests, barrels...), each made as big as its
//   share needs, up to a blackmetal chest's size (see RagnarokLoot.Split). Wardrobes and gift boxes are never used.
// - Each lands at a random spot within 10 m of the obliterator, turned at random, found just before it strikes so it
//   keeps clear of those already down: flat ground at the obliterator's height first, then ground a little higher or
//   lower, then flat floors at other heights; never on other objects, nor too close to anything (see FindSpot).
// - The chests are marked in their ZDO: their size, they vanish once emptied and give no materials when torn down or
//   broken (see RagnarokPatches.ChestPatch), and a beam of light rises from each until it is first opened (see ChestBeam).
// - With no room within 10 m, it looks 5 m farther out, and again, until it finds some. Only when no chest kind the
//   game knows is set (or one can't be made) does the treasure go in the obliterator.
internal static class TreasureChests
{
    internal static readonly int ChestMark = "TempestRite_TreasureChest".GetStableHashCode();
    // Set by the first game that opens the chest: its beam goes.
    internal static readonly int OpenedMark = "TempestRite_TreasureChestOpened".GetStableHashCode();
    // Its size, width and height: every game makes its inventory this big as it wakes it.
    private static readonly int WidthMark = "TempestRite_TreasureChestWidth".GetStableHashCode();
    private static readonly int HeightMark = "TempestRite_TreasureChestHeight".GetStableHashCode();
    // The biggest a chest is made: a blackmetal chest's size.
    private const int MaxWidth = 8;
    private const int MaxHeight = 4;

    // The lands of the game's bosses, whose chest a Ragnarök boss's treasure comes in. Others: where the obliterator stands.
    private static readonly Dictionary<string, Heightmap.Biome> BossLands = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Eikthyr"] = Heightmap.Biome.Meadows,
        ["gd_king"] = Heightmap.Biome.BlackForest,
        ["Bonemass"] = Heightmap.Biome.Swamp,
        ["Dragon"] = Heightmap.Biome.Mountain,
        ["GoblinKing"] = Heightmap.Biome.Plains,
        ["SeekerQueen"] = Heightmap.Biome.Mistlands,
        ["Fader"] = Heightmap.Biome.AshLands,
    };

    // The first chest comes a moment after the rite ends, then one after another.
    private const float FirstDelay = 1.5f;
    private const float Pause = 0.8f;
    // Where chests may land: anywhere this far from the obliterator, at random.
    private const float Near = 2.5f;
    private const float Far = 10f;
    private const int Tries = 120;
    // With no room that far, Grow farther out, and again, up to MostFar; the ground there may be this much higher or
    // lower for each meter farther (hills).
    private const float Grow = 5f;
    private const float MostFar = 100f;
    private const float HeightPerMeter = 0.3f;
    // Floors are looked for this far above and below the obliterator.
    private const float Reach = 6f;
    // The best ground: this close to the obliterator's height, and this flat (its corners' heights apart at most).
    private const float SameHeight = 0.6f;
    private const float Flat = 0.2f;
    // Ground a little off: this close to the obliterator's height, and no rougher than this.
    private const float NearHeight = 2.5f;
    private const float Rough = 0.6f;
    // Chests stand at least this far apart, beyond their own size.
    private const float Gap = 0.4f;
    // Ranks of a spot, the best first.
    private const int Level = 0, Terrain = 1, Floor = 2, NoRoom = 3;
    // A chest's half size (width, height, depth) when its prefab can't tell.
    private static readonly Vector3 DefaultHalf = new(0.8f, 0.5f, 0.6f);
    private static readonly Dictionary<string, Vector3> Halves = [];
    // Players this far from a chest see the lightning strike it down.
    private const float StrikeSeenFrom = 150f;

    private static int _groundMask;
    private static int _roomMask;
    private static int _terrainLayer;

    internal static void Register(ZRoutedRpc rpc) => rpc.Register<Vector3>(Rpcs.TreasureChest, RPC_Strike);

    // From the game that struck a chest down: lightning where it landed.
    private static void RPC_Strike(long sender, Vector3 at)
    {
        if (Player.m_localPlayer && Utils.DistanceXZ(Player.m_localPlayer.transform.position, at) <= StrikeSeenFrom)
            BlessingLightning.Strike(at);
    }

    // On the server: has the obliterator's owner strike these chests down (each the boss it is for, or "" for none, and an
    // (item, amount) list), naming the rite in its log.
    internal static void Send(ZDO obliterator, string rite, List<(string boss, List<(string item, int amount)> loot)> chests)
    {
        ZPackage package = new();
        package.Write(rite);
        package.Write(chests.Count);
        foreach ((string boss, List<(string item, int amount)> chest) in chests)
        {
            package.Write(boss);
            package.Write(chest.Count);
            foreach ((string item, int amount) in chest)
            {
                package.Write(item);
                package.Write(amount);
            }
        }
        ZRoutedRpc.instance.InvokeRoutedRPC(obliterator.GetOwner(), obliterator.m_uid, Rpcs.TreasureChests, package);
    }

    // From the server (see Send): the rite, then each chest's boss and treasure as an (item, amount) list.
    internal static void Give(Incinerator incinerator, long sender, ZPackage package)
    {
        if (!incinerator || !incinerator.m_nview || !incinerator.m_nview.IsOwner() || !ObjectDB.instance || !ZNetScene.instance) return;
        if (ZRoutedRpc.instance == null || (sender != ZRoutedRpc.instance.GetServerPeerID() && sender != ZRoutedRpc.instance.m_id)) return;
        string rite = package.ReadString();
        List<(string boss, List<(string item, int amount)> loot)> treasure = [];
        int chests = package.ReadInt();
        for (int c = 0; c < chests; c++)
        {
            string boss = package.ReadString();
            List<(string item, int amount)> loot = [];
            int count = package.ReadInt();
            for (int i = 0; i < count; i++)
            {
                string item = package.ReadString();
                int amount = package.ReadInt();
                if (amount <= 0) continue;
                GameObject? prefab = ObjectDB.instance.GetItemPrefab(item);
                if (!prefab || !prefab!.GetComponent<ItemDrop>())
                {
                    Plugin.ModLogger.LogWarning($"{rite}: treasure \"{item}\" is not an item the game knows. Check its treasure settings.");
                    continue;
                }
                RagnarokLoot.Add(loot, prefab.name, amount);
            }
            if (loot.Count > 0) treasure.Add((boss, loot));
        }
        Transform obliterator = incinerator.transform;
        Heightmap.Biome here = WorldGenerator.instance != null ? WorldGenerator.instance.GetBiome(obliterator.position) : Heightmap.Biome.Meadows;
        Dictionary<Heightmap.Biome, List<GameObject>> kinds = Kinds();
        List<(GameObject prefab, List<(string item, int count)> stacks)> packed = [];
        foreach ((string boss, List<(string item, int amount)> loot) in treasure)
        {
            List<GameObject> containers = ChestsFor(kinds, BossLands.TryGetValue(boss, out Heightmap.Biome lands) ? lands : here);
            if (containers.Count == 0)
            {
                // No chest the game knows: in the obliterator.
                foreach ((string item, int amount) in loot) CrimsonArena.PutIn(incinerator, ObjectDB.instance.GetItemPrefab(item), amount);
                continue;
            }
            // Split on purpose: the biome's own chest first, then its other containers in a shuffled turn (each before any
            // comes again), each as big as it needs.
            List<List<(string item, int count)>> split = RagnarokLoot.Split(loot, MaxWidth * MaxHeight, StackSize, UnityEngine.Random.Range);
            List<GameObject> turn = [];
            for (int i = 0; i < split.Count; i++)
            {
                if (i == 0 || containers.Count == 1)
                {
                    packed.Add((containers[0], split[i]));
                    continue;
                }
                if (turn.Count == 0)
                {
                    turn.AddRange(containers.GetRange(1, containers.Count - 1));
                    for (int j = turn.Count - 1; j > 0; j--)
                    {
                        int k = UnityEngine.Random.Range(0, j + 1);
                        (turn[j], turn[k]) = (turn[k], turn[j]);
                    }
                }
                packed.Add((turn[0], split[i]));
                turn.RemoveAt(0);
            }
        }
        if (packed.Count == 0) return;
        Plugin.ModLogger.LogInfo($"{rite}: {packed.Count} treasure chests around the obliterator.");
        ZNetScene.instance.StartCoroutine(StrikeDown(incinerator, rite, obliterator.position, packed));
    }

    private static IEnumerator StrikeDown(Incinerator incinerator, string rite, Vector3 center,
        List<(GameObject prefab, List<(string item, int count)> stacks)> chests)
    {
        yield return new WaitForSeconds(FirstDelay);
        List<(Vector3 at, float reach)> placed = [];
        for (int i = 0; i < chests.Count; i++)
        {
            if (i > 0) yield return new WaitForSeconds(Pause);
            try
            {
                Vector3 at = center;
                // Looked for now, so it keeps clear of the chests already down.
                if (FindSpot(center, chests[i].prefab, placed, out Vector3 spot, out Quaternion turn, out float reach) &&
                    Spawn(rite, chests[i].prefab, spot, turn, chests[i].stacks))
                {
                    at = spot;
                    placed.Add((spot, reach));
                }
                else InObliterator(incinerator, center, chests[i].stacks);
                ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpcs.TreasureChest, at);
            }
            catch (Exception exception) { Plugin.ModLogger.LogError($"{rite} treasure chest failed: {exception}"); }
        }
    }

    private static bool Spawn(string rite, GameObject prefab, Vector3 at, Quaternion turn, List<(string item, int count)> stacks)
    {
        GameObject chest = Object.Instantiate(prefab, at, turn);
        ZNetView nview = chest.GetComponent<ZNetView>();
        Container container = chest.GetComponentInChildren<Container>();
        if (!nview || !nview.IsValid() || !container || container.GetInventory() == null)
        {
            Plugin.ModLogger.LogWarning($"{rite}: treasure chest \"{prefab.name}\" couldn't be made.");
            if (nview && nview.IsValid()) nview.Destroy();
            else Object.Destroy(chest);
            return false;
        }
        // Big enough for its treasure, never smaller than it is.
        int width = Mathf.Clamp(Math.Max(container.m_width, stacks.Count), 1, MaxWidth);
        int height = Mathf.Clamp(Math.Max(container.m_height, (stacks.Count + width - 1) / width), 1, MaxHeight);
        nview.GetZDO().Set(ChestMark, true);
        nview.GetZDO().Set(WidthMark, width);
        nview.GetZDO().Set(HeightMark, height);
        Mark(chest);
        Inventory inventory = container.GetInventory();
        // A dungeon chest comes with loot of its own: only the treasure goes in.
        inventory.RemoveAll();
        foreach ((string item, int count) in stacks)
        {
            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(item);
            if (inventory.AddItem(itemPrefab, count)) continue;
            // Can't be: the chest was picked for its slots. Never lost all the same.
            ItemDrop.DropItem(itemPrefab.GetComponent<ItemDrop>().m_itemData, count, at + Vector3.up * 1.5f, Quaternion.identity);
        }
        Plugin.ModLogger.LogInfo($"{rite}: {prefab.name} struck down at ({at.x:0}, {at.z:0}) with " +
            $"{string.Join(", ", stacks.ConvertAll(s => $"{s.count} {s.item}"))}.");
        return true;
    }

    private static void InObliterator(Incinerator incinerator, Vector3 center, List<(string item, int count)> stacks)
    {
        foreach ((string item, int count) in stacks)
        {
            GameObject prefab = ObjectDB.instance.GetItemPrefab(item);
            if (incinerator) CrimsonArena.PutIn(incinerator, prefab, count);
            else ItemDrop.DropItem(prefab.GetComponent<ItemDrop>().m_itemData, count, center + Vector3.up * 1.5f, Quaternion.identity);
        }
    }

    // A treasure chest: vanishes once emptied, can't be torn down with the hammer, and gives no materials if broken; a
    // beam of light rises from it until it is first opened. Called as each game wakes one (see RagnarokPatches.ChestPatch),
    // and by the game that made it.
    internal static void Mark(GameObject chest)
    {
        ZNetView nview = chest.GetComponent<ZNetView>();
        ZDO? zdo = nview ? nview.GetZDO() : null;
        int width = zdo?.GetInt(WidthMark) ?? 0;
        int height = zdo?.GetInt(HeightMark) ?? 0;
        foreach (Container container in chest.GetComponentsInChildren<Container>())
        {
            container.m_autoDestroyEmpty = true;
            if (width <= 0 || height <= 0) continue;
            // Its inventory is read from the ZDO a moment after it wakes, into this size.
            container.m_width = width;
            container.m_height = height;
            Inventory? inventory = container.GetInventory();
            if (inventory == null) continue;
            inventory.m_width = width;
            inventory.m_height = height;
        }
        if (!chest.GetComponent<ChestBeam>()) chest.AddComponent<ChestBeam>();
        Piece piece = chest.GetComponent<Piece>();
        if (!piece) return;
        piece.m_canBeRemoved = false;
        piece.m_resources = [];
    }

    internal static bool IsChest(ZNetView? nview) =>
        nview && nview!.IsValid() && nview.GetZDO().GetBool(ChestMark);

    // The chest was opened in this game: its beam goes, in every game.
    internal static void Opened(ZNetView? nview)
    {
        if (!IsChest(nview) || nview!.GetZDO().GetBool(OpenedMark)) return;
        if (!nview.IsOwner()) nview.ClaimOwnership();
        nview.GetZDO().Set(OpenedMark, true);
    }

    // Never used, whatever the setting says: they look out of place as treasure.
    private static readonly string[] NeverUsed = ["wardrobe", "warderobe", "gift", "xmas", "yule", "present"];

    // The containers of the Treasure Chests setting the game has, by biome, its own chest first
    // ("Meadows: TreasureChest_meadows / piece_chest_wood / piece_chest_barrel, ...").
    private static Dictionary<Heightmap.Biome, List<GameObject>> Kinds()
    {
        Dictionary<Heightmap.Biome, List<GameObject>> kinds = [];
        foreach (string entry in PluginConfig.RagnarokChests.Value.Split(','))
        {
            string[] parts = entry.Split(':');
            if (parts.Length != 2) continue;
            string biome = parts[0].Trim();
            if (!Enum.TryParse(biome, true, out Heightmap.Biome lands))
            {
                Plugin.ModLogger.LogWarning($"Treasure chests: \"{biome}\" is not a biome. Check the Treasure Chests setting.");
                continue;
            }
            List<GameObject> containers = [];
            foreach (string part in parts[1].Split('/'))
            {
                string name = part.Trim();
                if (name.Length == 0) continue;
                if (Array.Exists(NeverUsed, word => name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Plugin.ModLogger.LogWarning($"Treasure chests: \"{name}\" is never used as a treasure chest.");
                    continue;
                }
                GameObject? prefab = ZNetScene.instance.GetPrefab(name);
                if (!prefab || !prefab!.GetComponentInChildren<Container>() || !prefab.GetComponent<ZNetView>())
                {
                    Plugin.ModLogger.LogWarning($"Treasure chests: \"{name}\" is not a chest the game knows. Check the Treasure Chests setting.");
                    continue;
                }
                containers.Add(prefab);
            }
            if (containers.Count > 0) kinds[lands] = containers;
        }
        return kinds;
    }

    // A biome's containers; one without its own gets the Meadows', or any.
    private static List<GameObject> ChestsFor(Dictionary<Heightmap.Biome, List<GameObject>> kinds, Heightmap.Biome biome)
    {
        if (kinds.TryGetValue(biome, out List<GameObject> chests)) return chests;
        if (kinds.TryGetValue(Heightmap.Biome.Meadows, out chests)) return chests;
        foreach (List<GameObject> any in kinds.Values) return any;
        return [];
    }

    private static int StackSize(string item)
    {
        GameObject? prefab = ObjectDB.instance.GetItemPrefab(item);
        ItemDrop? drop = prefab ? prefab!.GetComponent<ItemDrop>() : null;
        return drop ? Math.Max(1, drop!.m_itemData.m_shared.m_maxStackSize) : 1;
    }

    // A random spot for a chest within Far of the obliterator, turned at random, clear of the chests already down (placed:
    // where each stands and how far it reaches): the best rank found (see RankHit), a random one of those. With none, the
    // next Grow farther out is tried, and so on up to MostFar, the ground there allowed to be higher or lower the farther
    // out. Never none: past MostFar, just beside the obliterator, whatever is there.
    private static bool FindSpot(Vector3 center, GameObject prefab, List<(Vector3 at, float reach)> placed,
        out Vector3 spot, out Quaternion turn, out float reach)
    {
        Masks();
        Vector3 half = HalfSize(prefab);
        float own = reach = new Vector2(half.x, half.z).magnitude;
        turn = Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f);
        for (float near = Near, far = Far; far <= MostFar; near = far, far += Grow)
        {
            float wider = far - Far;
            int best = NoRoom;
            List<(Vector3 at, Quaternion turn)> found = [];
            for (int t = 0; t < Tries; t++)
            {
                float angle = UnityEngine.Random.value * 360f;
                float distance = Mathf.Sqrt(UnityEngine.Random.Range(near * near, far * far));
                Vector3 at = center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
                if (placed.Exists(other => Utils.DistanceXZ(other.at, at) < other.reach + own + Gap)) continue;
                Quaternion yaw = Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f);
                int rank = Rank(at, center.y, half, yaw, wider * HeightPerMeter, out Vector3 ground);
                if (rank >= NoRoom || rank > best) continue;
                if (rank < best)
                {
                    best = rank;
                    found.Clear();
                }
                found.Add((ground, yaw));
            }
            if (found.Count == 0) continue;
            (spot, turn) = found[UnityEngine.Random.Range(0, found.Count)];
            return true;
        }
        // Nowhere at all (out at sea, say): beside the obliterator, on whatever is there.
        spot = center + turn * Vector3.forward * Near;
        if (Physics.Raycast(spot + Vector3.up * Reach, Vector3.down, out RaycastHit hit, Reach * 2f, _groundMask, QueryTriggerInteraction.Ignore))
            spot = hit.point;
        return true;
    }

    // The best height at a spot (floors may stand over each other, and over the ground), and its rank (see RankHit);
    // allowance: how much farther from the obliterator's height the ground may be.
    private static int Rank(Vector3 at, float height, Vector3 half, Quaternion yaw, float allowance, out Vector3 ground)
    {
        ground = at;
        int best = NoRoom;
        float reach = Reach + allowance;
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(at.x, height + reach, at.z), Vector3.down, reach * 2f, _groundMask,
            QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            int rank = RankHit(hit, height, half, yaw, allowance);
            if (rank < best || (rank == best && rank < NoRoom && Mathf.Abs(hit.point.y - height) < Mathf.Abs(ground.y - height)))
            {
                best = rank;
                ground = hit.point;
            }
        }
        return best;
    }

    // Level: flat, at the obliterator's height. Terrain: ground a little higher, lower or rougher. Floor: a flat built
    // floor at another height. NoRoom: steep, water, other objects (rocks, trees, tables, chests...) or something in the way.
    private static int RankHit(RaycastHit hit, float height, Vector3 half, Quaternion yaw, float allowance)
    {
        if (hit.normal.y < 0.7f) return NoRoom;
        if (ZoneSystem.instance && hit.point.y < ZoneSystem.instance.m_waterLevel) return NoRoom;
        bool terrain = hit.collider.gameObject.layer == _terrainLayer || hit.collider.GetComponentInParent<Heightmap>();
        if (!terrain && !IsFloor(hit.collider)) return NoRoom;
        // How flat: the heights under its corners.
        float low = hit.point.y, high = hit.point.y;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector3 offset = yaw * new Vector3(corner % 2 == 0 ? -half.x : half.x, 0f, corner < 2 ? -half.z : half.z);
            Vector3 from = hit.point + offset + Vector3.up;
            if (!Physics.Raycast(from, Vector3.down, out RaycastHit under, 2f, _groundMask, QueryTriggerInteraction.Ignore)) return NoRoom;
            low = Mathf.Min(low, under.point.y);
            high = Mathf.Max(high, under.point.y);
        }
        float rough = high - low, off = Mathf.Abs(hit.point.y - height);
        int rank = rough <= Flat && off <= SameHeight ? Level
            : terrain && rough <= Rough && off <= NearHeight + allowance ? Terrain
            : !terrain && rough <= Flat ? Floor
            : NoRoom;
        if (rank == NoRoom) return NoRoom;
        // Nothing in the way: from just over its highest corner up, a little bigger than the chest.
        Vector3 middle = new(hit.point.x, high + 0.05f + half.y, hit.point.z);
        Vector3 room = new(half.x + 0.1f, half.y, half.z + 0.1f);
        return Physics.CheckBox(middle, room, yaw, _roomMask, QueryTriggerInteraction.Ignore) ? NoRoom : rank;
    }

    // A built floor (wood, stone, marble... floors), not a table, a chest or a wall's top.
    private static bool IsFloor(Collider collider)
    {
        Piece piece = collider.GetComponentInParent<Piece>();
        return piece && !piece.GetComponentInChildren<Container>() &&
            Utils.GetPrefabName(piece.gameObject).IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // A chest's half size (width, height, depth), from its meshes: kept for each kind.
    private static Vector3 HalfSize(GameObject prefab)
    {
        if (Halves.TryGetValue(prefab.name, out Vector3 half)) return half;
        Matrix4x4 toRoot = prefab.transform.worldToLocalMatrix;
        bool any = false;
        Bounds all = default;
        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!filter.sharedMesh) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 matrix = toRoot * filter.transform.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = matrix.MultiplyPoint3x4(mesh.center + Vector3.Scale(mesh.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                if (!any) all = new Bounds(point, Vector3.zero);
                else all.Encapsulate(point);
                any = true;
            }
        }
        half = !any ? DefaultHalf : new Vector3(Mathf.Clamp(all.extents.x, 0.3f, 1.5f), Mathf.Clamp(all.extents.y, 0.2f, 1f),
            Mathf.Clamp(all.extents.z, 0.3f, 1.5f));
        Halves[prefab.name] = half;
        return half;
    }

    private static void Masks()
    {
        if (_groundMask != 0) return;
        _groundMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
        _roomMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "vehicle", "character", "character_net", "item");
        _terrainLayer = LayerMask.NameToLayer("terrain");
    }
}

// The beam of light over a treasure chest until it is first opened (see TreasureChests.Opened), in every game that shows
// it; it goes with the chest.
internal sealed class ChestBeam : MonoBehaviour
{
    private const float Height = 40f;
    private const float Width = 0.9f;
    private const float CheckEvery = 0.5f;
    private static readonly Color Core = new(1f, 0.95f, 0.7f);
    private static readonly Color Halo = new(1f, 0.7f, 0.2f);

    private ZNetView? _nview;
    private LightBeam? _beam;
    private float _nextCheck;

    private void Start()
    {
        _nview = GetComponent<ZNetView>();
        // A dedicated server draws nothing.
        if (ZNet.instance && ZNet.instance.IsDedicated()) Destroy(this);
    }

    private void Update()
    {
        if (Time.time >= _nextCheck)
        {
            _nextCheck = Time.time + CheckEvery;
            if (!_nview || !_nview!.IsValid() || _nview.GetZDO().GetBool(TreasureChests.OpenedMark))
            {
                Destroy(this);
                return;
            }
            if (_beam == null || !_beam.Alive)
                _beam = LightBeam.Make("TempestRite_ChestBeam", transform.position, Height, Width, Core, Halo);
        }
        _beam?.Face();
    }

    private void OnDestroy()
    {
        _beam?.Destroy();
        _beam = null;
    }
}
