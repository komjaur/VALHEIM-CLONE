using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages loading/unloading of terrain chunks and provides utility
/// methods for sampling terrain height and spawning objects.
/// </summary>
public class WorldGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Material terrainMaterial;

    [Header("World Settings")]
    public float chunkSize = 20f;
    public int chunkResolution = 64;
    public int viewDistanceInChunks = 3;
    public float biomeScale = 0.001f;
    public float waterLevel = 0f;

    [Header("Biomes")]
    public BiomeDefinition[] biomes;

    readonly Dictionary<Vector2Int, TerrainChunk> loadedChunks = new Dictionary<Vector2Int, TerrainChunk>();
    readonly Queue<TerrainChunk> chunkPool = new Queue<TerrainChunk>();

    Vector2Int lastPlayerChunk;

    void Start()
    {
        UpdateVisibleChunks(true);
    }

    void Update()
    {
        Vector2Int current = GetChunkCoordFromPosition(player.position);
        if (current != lastPlayerChunk)
        {
            UpdateVisibleChunks();
            lastPlayerChunk = current;
        }
    }

    Vector2Int GetChunkCoordFromPosition(Vector3 pos)
    {
        return new Vector2Int(Mathf.FloorToInt(pos.x / chunkSize), Mathf.FloorToInt(pos.z / chunkSize));
    }

    void UpdateVisibleChunks(bool force = false)
    {
        Vector2Int playerChunk = GetChunkCoordFromPosition(player.position);
        HashSet<Vector2Int> needed = new HashSet<Vector2Int>();
        for (int y = -viewDistanceInChunks; y <= viewDistanceInChunks; y++)
        {
            for (int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
            {
                Vector2Int coord = playerChunk + new Vector2Int(x, y);
                needed.Add(coord);
                if (!loadedChunks.ContainsKey(coord))
                    LoadChunk(coord);
            }
        }

        var keys = new List<Vector2Int>(loadedChunks.Keys);
        foreach (var coord in keys)
        {
            if (!needed.Contains(coord))
                UnloadChunk(coord);
        }
    }

    void LoadChunk(Vector2Int coord)
    {
        TerrainChunk chunk;
        if (chunkPool.Count > 0)
        {
            chunk = chunkPool.Dequeue();
            chunk.gameObject.SetActive(true);
        }
        else
        {
            GameObject go = new GameObject();
            go.AddComponent<MeshRenderer>();
            chunk = go.AddComponent<TerrainChunk>();
        }
        loadedChunks.Add(coord, chunk);
        chunk.transform.parent = transform;
        chunk.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
        chunk.Init(this, coord);
    }

    void UnloadChunk(Vector2Int coord)
    {
        if (loadedChunks.TryGetValue(coord, out TerrainChunk chunk))
        {
            chunk.ClearObjects();
            chunk.gameObject.SetActive(false);
            chunkPool.Enqueue(chunk);
            loadedChunks.Remove(coord);
        }
    }

    /// <summary>
    /// Sample blended biome height at world coordinates.
    /// </summary>
    public float SampleHeight(float x, float z)
    {
        float biomeValue = NoiseUtils.Perlin(x, z, biomeScale);
        float height = 0f;
        float total = 0f;
        foreach (var biome in biomes)
        {
            float weight = Mathf.InverseLerp(biome.minThreshold, biome.maxThreshold, biomeValue);
            weight = Mathf.Clamp01(weight);
            // smooth weight
            weight = weight * weight * (3f - 2f * weight);
            float roughnessNoise = NoiseUtils.Perlin(x, z, biome.roughness);
            float h = biome.heightCurve.Evaluate(roughnessNoise) * biome.heightMultiplier;
            height += h * weight;
            total += weight;
        }
        if (total > 0f)
            height /= total;
        return height;
    }

    /// <summary>
    /// Spawn environmental objects like trees based on chunk's dominant biome.
    /// </summary>
    public void SpawnEnvironment(TerrainChunk chunk)
    {
        BiomeDefinition biome = GetDominantBiome(chunk.Coord);
        if (biome == null || biome.treePrefabs == null)
            return;

        foreach (var prefab in biome.treePrefabs)
        {
            Vector3 pos = new Vector3(
                chunk.Coord.x * chunkSize + Random.Range(0f, chunkSize),
                0f,
                chunk.Coord.y * chunkSize + Random.Range(0f, chunkSize));
            pos.y = SampleHeight(pos.x, pos.z);
            GameObject go = Instantiate(prefab, pos, Quaternion.identity, chunk.transform);
            chunk.RegisterObject(go);
        }
    }

    /// <summary>
    /// Spawn mobs for the chunk's dominant biome.
    /// </summary>
    public void SpawnMobs(TerrainChunk chunk)
    {
        BiomeDefinition biome = GetDominantBiome(chunk.Coord);
        if (biome == null || biome.mobPrefabs == null)
            return;

        foreach (var prefab in biome.mobPrefabs)
        {
            Vector3 pos = new Vector3(
                chunk.Coord.x * chunkSize + Random.Range(0f, chunkSize),
                0f,
                chunk.Coord.y * chunkSize + Random.Range(0f, chunkSize));
            pos.y = SampleHeight(pos.x, pos.z);
            GameObject go = Instantiate(prefab, pos, Quaternion.identity, chunk.transform);
            chunk.RegisterObject(go);
        }
    }

    BiomeDefinition GetDominantBiome(Vector2Int coord)
    {
        float centerX = coord.x * chunkSize + chunkSize * 0.5f;
        float centerZ = coord.y * chunkSize + chunkSize * 0.5f;
        float biomeValue = NoiseUtils.Perlin(centerX, centerZ, biomeScale);
        BiomeDefinition best = null;
        float bestW = 0f;
        foreach (var biome in biomes)
        {
            float w = Mathf.InverseLerp(biome.minThreshold, biome.maxThreshold, biomeValue);
            w = Mathf.Clamp01(w);
            if (w > bestW)
            {
                bestW = w;
                best = biome;
            }
        }
        return best;
    }

    /// <summary>
    /// Add a water plane for the chunk.
    /// </summary>
    public void ApplyWater(TerrainChunk chunk)
    {
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.transform.parent = chunk.transform;
        water.transform.localScale = Vector3.one * (chunkSize / 10f);
        Vector3 pos = new Vector3(
            chunk.Coord.x * chunkSize + chunkSize * 0.5f,
            waterLevel,
            chunk.Coord.y * chunkSize + chunkSize * 0.5f);
        water.transform.position = pos;
        chunk.RegisterObject(water);
    }
}
