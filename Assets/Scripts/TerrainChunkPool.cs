using System.Collections.Generic;
using UnityEngine;

namespace EndlessWorld
{
    public class TerrainChunkPool : MonoBehaviour
    {
        readonly Stack<TerrainChunk> _pool = new();

        public TerrainChunk Get(int size, float spacing, float noiseScale,
                                float heightMult, Material mat,
                                Vector2Int coord,
                                float heatScale, float wetScale, Biome[] biomes,
                                float waterHeight, Material waterMat,
                                Transform parent = null)
        {
            TerrainChunk tc = _pool.Count > 0
                              ? _pool.Pop()
                              : new GameObject("Chunk").AddComponent<TerrainChunk>();

            tc.transform.parent = parent ? parent : transform;
            tc.Build(size, spacing, noiseScale, heightMult,
                     mat, coord,
                     heatScale, wetScale, biomes,
                     waterHeight, waterMat);

            tc.gameObject.SetActive(true);
            return tc;
        }

        public void Release(TerrainChunk tc)
        {
            tc.gameObject.SetActive(false);
            _pool.Push(tc);
        }
    }
}
