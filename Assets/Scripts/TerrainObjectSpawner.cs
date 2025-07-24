// TerrainObjectSpawner.cs
using UnityEngine;
using Random = UnityEngine.Random;

namespace EndlessWorld
{
    /// <summary>Light-weight scatterer; attaches itself to the same GO as TerrainChunk.</summary>
    [RequireComponent(typeof(TerrainChunk))]
    public class TerrainObjectSpawner : MonoBehaviour
    {
        readonly System.Collections.Generic.List<Transform> _spawned = new();

        /// <remarks>
        ///  Called from TerrainChunk.Build every time the chunk is (re)used.
        ///  We *re-scatter* because vertex heights can change with a different coord.
        /// </remarks>
        public void Initialize(int size, float spacing,
                               float noiseScale, float heightMult,
                               Vector2Int coord, SpawnRule[] rules)
        {
            // wipe previous instances from the pool reuse cycle
            foreach (var t in _spawned) if (t) Destroy(t.gameObject);
            _spawned.Clear();

            if (rules == null || rules.Length == 0) return;

            float worldW = (size - 1) * spacing;
            float area    = worldW * worldW;

            // Probability approach: pick N tries based on the rule’s density × area.
            foreach (var r in rules)
            {
                if (!r.prefab || r.density <= 0f) continue;

                int target = Mathf.CeilToInt(r.density * area);
                for (int i = 0; i < target; i++)
                {
                    // random point in the chunk
                    float localX = Random.value * worldW;
                    float localZ = Random.value * worldW;

                    float wx = coord.x * worldW + localX;
                    float wz = coord.y * worldW + localZ;

                    // Same height function we used for the mesh
                    float h01 = Mathf.PerlinNoise((wx + 12345) / noiseScale,
                                                  (wz + 12345) / noiseScale);
                    if (!r.HeightOK(h01)) continue;

                    float y = h01 * heightMult + Random.Range(r.yRandom.x, r.yRandom.y);
                    Vector3 pos = new(wx, y, wz);

                    // Optional little rotation for variety
                    Quaternion rot = Quaternion.Euler(0, Random.value * 360f, 0);

                    // Parent under the chunk so despawning works automatically
                    Transform inst = Instantiate(r.prefab, pos, rot, transform).transform;
                    _spawned.Add(inst);
                }
            }
        }
    }
}
