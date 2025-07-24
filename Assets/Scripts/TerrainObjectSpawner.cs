using UnityEngine;

namespace EndlessWorld
{
    [System.Serializable]
    public class SpawnRule
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float minHeight = 0f;
        [Range(0f, 1f)] public float maxHeight = 1f;
        [Range(0f, 1f)] public float density   = 0.1f;
    }

    /// <summary>
    /// Spawns objects on terrain chunks based on height thresholds.
    /// </summary>
    public class TerrainObjectSpawner : MonoBehaviour
    {
        SpawnRule[] _rules;

        int _size;
        float _spacing;
        float _noiseScale;
        float _heightMult;
        Vector2Int _coord;

        static int _seed = 12345; // must match TerrainChunk

        public void Initialize(int size, float spacing, float noiseScale,
                               float heightMult, Vector2Int coord,
                               SpawnRule[] rules)
        {
            _size       = size;
            _spacing    = spacing;
            _noiseScale = noiseScale;
            _heightMult = heightMult;
            _coord      = coord;
            _rules      = rules;

            ClearObjects();
            SpawnObjects();
        }

        void ClearObjects()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);
        }

        void SpawnObjects()
        {
            if (_rules == null) return;

            float world = (_size - 1) * _spacing;

            foreach (var rule in _rules)
            {
                if (!rule.prefab) continue;

                int attempts = Mathf.RoundToInt(rule.density * _size * _size);

                for (int i = 0; i < attempts; i++)
                {
                    float x  = Random.Range(0f, world);
                    float z  = Random.Range(0f, world);
                    float wx = _coord.x * world + x;
                    float wz = _coord.y * world + z;

                    float h01 = Mathf.PerlinNoise((wx + _seed) / _noiseScale,
                                                  (wz + _seed) / _noiseScale);

                    if (h01 < rule.minHeight || h01 > rule.maxHeight)
                        continue;

                    float h   = h01 * _heightMult;
                    Vector3 pos = new Vector3(x, h, z) + transform.position;

                    Instantiate(rule.prefab, pos, Quaternion.identity, transform);
                }
            }
        }
    }
}
