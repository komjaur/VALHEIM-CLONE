using System.Collections.Generic;
using UnityEngine;

namespace EndlessWorld
{
    [RequireComponent(typeof(TerrainChunkPool))]
    public class EndlessTerrain : MonoBehaviour
    {
        /* -------- Viewer & world params -------- */
        [Header("Player & View")]
        public Transform player;
        [Min(1)] public int viewDistance = 4;

        [Header("World Settings")]
        public WorldSettings world;

        /* -------- internals -------- */
        readonly Dictionary<Vector2Int,TerrainChunk> _loaded = new();
        TerrainChunkPool _pool;
        Dictionary<Biome,Material> _biomeMats;
        Material _waterMat;

        void Start()
        {
            _pool = GetComponent<TerrainChunkPool>();
            if (!player)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (!world) return;

            _biomeMats = new Dictionary<Biome, Material>();
            foreach (var b in world.biomes)
            {
                var mat = new Material(Shader.Find("EndlessWorld/HeightBlend"));
                if (b.texture) mat.SetTexture("_MainTex", b.texture);
                mat.SetFloat("_Tiling", world.textureTiling);
                _biomeMats[b] = mat;
            }

            _waterMat = new Material(Shader.Find("Unlit/Color"));
            _waterMat.color = world.waterColor;
        }

        void Update()
        {
            if (!player || !world) return;
            Vector2Int pChunk = WorldToChunk(player.position);

            /* spawn window */
            for (int y=-viewDistance; y<=viewDistance; y++)
            for (int x=-viewDistance; x<=viewDistance; x++)
            {
                Vector2Int c = pChunk + new Vector2Int(x,y);
                if (_loaded.ContainsKey(c)) continue;

                Biome biome = ChooseBiome(c);
                Material mat = (biome != null && _biomeMats.ContainsKey(biome))
                                ? _biomeMats[biome]
                                : null;

                TerrainChunk chunk = _pool.Get(
                    world.chunkSize, world.vertexSpacing,
                    biome != null ? biome.noiseScale : 60f,
                    biome != null ? biome.heightMultiplier : 25f,
                    mat,
                    c,
                    world.heatNoiseScale, world.wetnessNoiseScale, world.biomes,
                    world.waterHeight, _waterMat,
                    transform);

                _loaded.Add(c, chunk);
            }

            /* despawn fringe */
            var toRemove = new List<Vector2Int>();
            foreach (var kv in _loaded)
            {
                if (Mathf.Abs(kv.Key.x - pChunk.x) > viewDistance + 1 ||
                    Mathf.Abs(kv.Key.y - pChunk.y) > viewDistance + 1)
                {
                    _pool.Release(kv.Value);
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var c in toRemove)
            {
                _loaded.Remove(c);
            }
        }

        Vector2Int WorldToChunk(Vector3 pos)
        {
            float chunkWorld = (world.chunkSize-1)*world.vertexSpacing;
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / chunkWorld),
                Mathf.FloorToInt(pos.z / chunkWorld));
        }

        Biome ChooseBiome(Vector2Int coord)
        {
            if (world == null || world.biomes == null || world.biomes.Length == 0)
                return null;

            float worldSize = (world.chunkSize - 1) * world.vertexSpacing;
            float wx = coord.x * worldSize;
            float wz = coord.y * worldSize;

            const int seed = 12345; // must match TerrainChunk
            float heat = Mathf.PerlinNoise((wx + seed*2) / world.heatNoiseScale,
                                           (wz + seed*2) / world.heatNoiseScale);
            float wet  = Mathf.PerlinNoise((wx + seed*3) / world.wetnessNoiseScale,
                                           (wz + seed*3) / world.wetnessNoiseScale);

            foreach (var b in world.biomes)
            {
                if (heat >= b.minHeat && heat <= b.maxHeat &&
                    wet  >= b.minWetness && wet  <= b.maxWetness)
                {
                    return b;
                }
            }

            return world.biomes[0];
        }
    }
}

