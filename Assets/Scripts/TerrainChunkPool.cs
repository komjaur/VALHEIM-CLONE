using System.Collections.Generic;
using UnityEngine;

namespace EndlessWorld
{
    public class TerrainChunkPool : MonoBehaviour
    {
        readonly Stack<TerrainChunk> _pool = new();

        public TerrainChunk Get(int size, float spacing, float noiseScale,
                                float heightMult, float sandT, float stoneT,
                                Material mat,
                                Vector2Int coord)
        {
            TerrainChunk tc = _pool.Count > 0
                              ? _pool.Pop()
                              : new GameObject("Chunk").AddComponent<TerrainChunk>();

            tc.transform.parent = transform;
            tc.Build(size, spacing, noiseScale, heightMult,
                     sandT, stoneT, mat, coord);

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
