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

        Texture2DArray _biomeArray;
        Material       _sharedMat;
        Material       _waterMat;

        /* ─────────────────────────────────── */

        void Start()
        {
            _pool = GetComponent<TerrainChunkPool>();
            if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (!world || world.biomes == null || world.biomes.Length == 0) return;

            /* ---------- create texture array ---------- */
            int texSize = world.biomes[0].texture ? world.biomes[0].texture.width : 128;
            _biomeArray = new Texture2DArray(
                texSize, texSize, world.biomes.Length,
                TextureFormat.RGBA32, /* generate mips */ true, /* sRGB */ false);

            for (int i = 0; i < world.biomes.Length; ++i)
            {
                Texture src = world.biomes[i].texture ? world.biomes[i].texture
                                                      : Texture2D.whiteTexture;

                // turn *any* source texture (read-only, compressed, NPOT, …) into
                // a readable RGBA32 buffer of the right size:
                Color[] pixels = ExtractPixelsToRGBA32(src, texSize);

                _biomeArray.SetPixels(pixels, i, 0);   // fill the slice (mip 0)
            }
            _biomeArray.Apply(/* update mipmaps */ true, /* make immutable */ true);

            /* ---------- shared material ---------- */
            _sharedMat = new Material(Shader.Find("EndlessWorld/HeightBlendArray"));
            float chunkWorld = (world.chunkSize - 1) * world.vertexSpacing;
            _sharedMat.SetFloat("_ChunkSize", chunkWorld);
            _sharedMat.SetFloat("_Tiling", world.textureTiling);
            _sharedMat.SetTexture("_BiomeTexArr", _biomeArray);

            /* ---------- water material ---------- */
            _waterMat = new Material(Shader.Find("Unlit/Color"))
            {
                color = world.waterColor
            };
        }

        /* ─────────────────────────────────── */
        /* ----- Helper: copies any texture into a Color[] ----- */

        static Color[] ExtractPixelsToRGBA32(Texture src, int targetSize)
        {
            // Allocate a temporary RenderTexture large enough to hold the scaled copy
            RenderTexture rt = RenderTexture.GetTemporary(
                targetSize, targetSize, 0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);

            // Copy & scale (GPU path, succeeds with any texture type/format)
            Graphics.Blit(src, rt);

            // Read back into a readable Texture2D
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D tmp = new Texture2D(
                targetSize, targetSize, TextureFormat.RGBA32, false, false);
            tmp.ReadPixels(new Rect(0, 0, targetSize, targetSize), 0, 0);
            tmp.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            return tmp.GetPixels();
        }

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
