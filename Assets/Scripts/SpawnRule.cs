// SpawnRule.cs
using UnityEngine;

namespace EndlessWorld
{
    /// <summary>One rule = one prefab family (e.g. pine), a height range and a density.</summary>
    [System.Serializable]
    public struct SpawnRule
    {
        public GameObject prefab;              // What to drop
        [Range(0f, 1f)] public float minH01;   // Inclusive – 0 = sea-level, 1 = mountain-top
        [Range(0f, 1f)] public float maxH01;   // Exclusive
        [Min(0f)] public float density;        // Instances / (world-unit²). 0 → off
        public Vector2 yRandom;                // Extra random offset on Y (for sunk / raised)

        public bool HeightOK(float h01) => h01 >= minH01 && h01 < maxH01;
    }
}
