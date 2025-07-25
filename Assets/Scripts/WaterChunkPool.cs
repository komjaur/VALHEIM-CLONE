using System.Collections.Generic;
using UnityEngine;

namespace EndlessWorld
{
    public class WaterChunkPool : MonoBehaviour
    {
        readonly Stack<WaterChunk> _pool = new();

        public WaterChunk Get(int size, float spacing, float height, Material mat, Vector2Int coord, Transform parent = null)
        {
            WaterChunk wc = _pool.Count > 0 ? _pool.Pop() : new GameObject("Water").AddComponent<WaterChunk>();
            wc.transform.parent = parent ? parent : transform;
            wc.Build(size, spacing, height, mat, coord);
            wc.gameObject.SetActive(true);
            return wc;
        }

        public void Release(WaterChunk wc)
        {
            wc.gameObject.SetActive(false);
            _pool.Push(wc);
        }
    }
}

