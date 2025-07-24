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

        [Header("Chunk Geometry")]
        public int   chunkSize       = 241;
        public float vertexSpacing   = 1f;
        public float noiseScale      = 60f;
        public float heightMultiplier = 25f;

        /* -------- Biomes -------- */
        [Header("Biome Textures & Thresholds")]
        public Texture2D sandTex;
        public Texture2D grassTex;
        public Texture2D stoneTex;
        [Range(0f,1f)] public float sandHeight  = 0.35f;
        [Range(0f,1f)] public float stoneHeight = 0.75f;
        public float textureTiling = 8f;

        /* -------- internals -------- */
        readonly Dictionary<Vector2Int,TerrainChunk> _loaded = new();
        TerrainChunkPool _pool;
        Material _sharedMat;

        void Start()
        {
            _pool  = GetComponent<TerrainChunkPool>();
            if (!player)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            _sharedMat = new Material(Shader.Find("EndlessWorld/HeightBlend"));
            _sharedMat.SetTexture("_Sand",  sandTex);
            _sharedMat.SetTexture("_Grass", grassTex);
            _sharedMat.SetTexture("_Stone", stoneTex);
            _sharedMat.SetFloat  ("_Tiling", textureTiling);
        }

        void Update()
        {
            if (!player) return;
            Vector2Int pChunk = WorldToChunk(player.position);

            /* Spawn window */
            for (int y=-viewDistance; y<=viewDistance; y++)
            for (int x=-viewDistance; x<=viewDistance; x++)
            {
                Vector2Int c = pChunk + new Vector2Int(x,y);
                if (_loaded.ContainsKey(c)) continue;

                TerrainChunk tc = _pool.Get(
                    chunkSize, vertexSpacing,
                    noiseScale, heightMultiplier,
                    sandHeight, stoneHeight,
                    _sharedMat, c);
                _loaded.Add(c, tc);
            }

            /* Cull fringe */
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

        Vector2Int WorldToChunk(Vector3 pos)
        {
            float chunkWorld = (chunkSize-1)*vertexSpacing;
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / chunkWorld),
                Mathf.FloorToInt(pos.z / chunkWorld));
        }
    }
}
