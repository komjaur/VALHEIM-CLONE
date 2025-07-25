using System.Collections.Generic;
using UnityEngine;

namespace EndlessWorld
{
    [RequireComponent(typeof(TerrainChunkPool))]
    public class EndlessTerrain : MonoBehaviour
    {
        /* ───────── Public settings ───────── */
        [Header("Player & View")]
        public Transform player;
        [Min(1)] public int viewDistance = 4;

        [Header("World Settings")]
        public WorldSettings world;

        /* ───────── Internals ───────── */
        readonly Dictionary<Vector2Int, TerrainChunk> _loaded = new();
        TerrainChunkPool _pool;

        Material       _sharedMat;
        Material       _waterMat;

        /* ─────────────────────────────────── */

        void Start()
        {
            _pool = GetComponent<TerrainChunkPool>();
            if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (!world || world.biomes == null || world.biomes.Length == 0) return;

            /* ---------- shared material ---------- */
            _sharedMat = new Material(Shader.Find("EndlessWorld/VertexColor"));
            float chunkWorld = (world.chunkSize - 1) * world.vertexSpacing;
            _sharedMat.SetFloat("_ChunkSize", chunkWorld);

            /* ---------- water material ---------- */
            _waterMat = new Material(Shader.Find("Unlit/Color"))
            {
                color = world.waterColor
            };
        }

        /* ─────────────────────────────────── */
        /* ─────────────────────────────────── */

        void Update()
        {
            if (!player || !world) return;
            Vector2Int pChunk = WorldToChunk(player.position);

            /* ----- spawn window ----- */
            for (int y = -viewDistance; y <= viewDistance; ++y)
            for (int x = -viewDistance; x <= viewDistance; ++x)
            {
                Vector2Int c = pChunk + new Vector2Int(x, y);
                if (_loaded.ContainsKey(c)) continue;

                TerrainChunk chunk = _pool.Get(
                    world.chunkSize, world.vertexSpacing,
                    60f, 25f,                 // dummy (pool signature)
                    _sharedMat,
                    c,
                    world.heatNoiseScale, world.wetnessNoiseScale, world.biomes,
                    world.waterHeight, _waterMat,
                    transform);

                _loaded.Add(c, chunk);
            }

            /* ----- despawn fringe ----- */
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
            foreach (var c in toRemove) _loaded.Remove(c);
        }

        /* ─────────────────────────────────── */

        Vector2Int WorldToChunk(Vector3 pos)
        {
            float chunkWorld = (world.chunkSize - 1) * world.vertexSpacing;
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / chunkWorld),
                Mathf.FloorToInt(pos.z / chunkWorld));
        }
    }
}
