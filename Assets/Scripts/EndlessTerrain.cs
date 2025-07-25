using System.Collections.Generic;
using UnityEngine;

namespace EndlessWorld
{
    [RequireComponent(typeof(TerrainChunkPool))]
    [RequireComponent(typeof(WaterChunkPool))]
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
        readonly Dictionary<Vector2Int,WaterChunk>   _waterLoaded = new();
        TerrainChunkPool _pool;
        WaterChunkPool _waterPool;
        Material _sharedMat;
        Material _waterMat;

        void Start()
        {
            _pool = GetComponent<TerrainChunkPool>();
            _waterPool = GetComponent<WaterChunkPool>();
            if (!player)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (!world) return;

            _sharedMat = new Material(Shader.Find("EndlessWorld/HeightBlend"));
            _sharedMat.SetTexture("_Sand",  world.sandTex);
            _sharedMat.SetTexture("_Grass", world.grassTex);
            _sharedMat.SetTexture("_Stone", world.stoneTex);
            _sharedMat.SetFloat  ("_Tiling", world.textureTiling);

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

                TerrainChunk tc = _pool.Get(
                    world.chunkSize, world.vertexSpacing,
                    world.noiseScale, world.heightMultiplier,
                    world.sandHeight, world.stoneHeight,
                    _sharedMat,
                    c,
                    world.treePrefab, world.treeMinHeight,
                    world.treeMaxHeight, world.treeDensity);
                _loaded.Add(c, tc);

                if (!_waterLoaded.ContainsKey(c))
                {
                    WaterChunk wc = _waterPool.Get(
                        world.chunkSize, world.vertexSpacing,
                        world.waterHeight, _waterMat, c);
                    _waterLoaded.Add(c, wc);
                }
            }

            /* despawn fringe */
            var toRemove = new List<Vector2Int>();
            foreach (var kv in _loaded)
            {
                if (Mathf.Abs(kv.Key.x - pChunk.x) > viewDistance + 1 ||
                    Mathf.Abs(kv.Key.y - pChunk.y) > viewDistance + 1)
                {
                    _pool.Release(kv.Value);
                    if (_waterLoaded.TryGetValue(kv.Key, out var w))
                        _waterPool.Release(w);
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var c in toRemove)
            {
                _loaded.Remove(c);
                _waterLoaded.Remove(c);
            }
        }

        Vector2Int WorldToChunk(Vector3 pos)
        {
            float chunkWorld = (world.chunkSize-1)*world.vertexSpacing;
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / chunkWorld),
                Mathf.FloorToInt(pos.z / chunkWorld));
        }
    }
}

