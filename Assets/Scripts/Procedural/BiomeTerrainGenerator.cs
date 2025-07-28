using UnityEngine;

namespace ProceduralTerrain
{
    /// <summary>
    /// Generates a procedurally generated terrain mesh using Perlin noise
    /// and biome blending. Attach this to a GameObject with a MeshFilter and
    /// MeshRenderer. Designed for Unity 2022+.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class BiomeTerrainGenerator : MonoBehaviour
    {
        [System.Serializable]
        public class Biome
        {
            public string name = "Biome";
            public float heightMultiplier = 10f;   // Max height for this biome
            public float roughnessScale = 20f;     // Noise scale controlling roughness
            public Texture2D texture;              // Optional surface texture
            [Range(0f, 1f)] public float minMask = 0f; // Lower biome noise threshold
            [Range(0f, 1f)] public float maxMask = 1f; // Upper biome noise threshold
        }

        [Header("Grid Settings")]
        [Min(2)] public int resolution = 128;     // Number of vertices per side
        public float vertexSpacing = 1f;          // World spacing between vertices

        [Header("Noise Settings")]
        public float heightNoiseScale = 20f;      // Scale for elevation noise map
        public float biomeNoiseScale = 100f;      // Scale for biome distribution
        public Vector2 noiseOffset;               // Offsets for noise maps

        [Header("Biomes")]
        public Biome[] biomes;

        void Start()
        {
            if (biomes == null || biomes.Length == 0)
                SetupDefaultBiomes();

            GenerateTerrain();
        }

        // Creates three default biomes similar to Valheim
        void SetupDefaultBiomes()
        {
            biomes = new Biome[3];
            biomes[0] = new Biome
            {
                name = "Plains",
                heightMultiplier = 5f,
                roughnessScale = 40f,
                minMask = 0f,
                maxMask = 0.33f
            };
            biomes[1] = new Biome
            {
                name = "Forest",
                heightMultiplier = 12f,
                roughnessScale = 25f,
                minMask = 0.25f,
                maxMask = 0.66f
            };
            biomes[2] = new Biome
            {
                name = "Mountains",
                heightMultiplier = 30f,
                roughnessScale = 10f,
                minMask = 0.6f,
                maxMask = 1f
            };
        }

        // Generates the terrain mesh using Perlin noise and biome blending
        void GenerateTerrain()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh mesh = new Mesh { name = "ProceduralTerrain" };

            int size = resolution;
            Vector3[] vertices = new Vector3[size * size];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[(size - 1) * (size - 1) * 6];

            // ----- build grid -----
            for (int z = 0, i = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++, i++)
                {
                    vertices[i] = new Vector3(x * vertexSpacing, 0f, z * vertexSpacing);
                    uvs[i] = new Vector2((float)x / (size - 1), (float)z / (size - 1));
                }
            }

            for (int z = 0, ti = 0, vi = 0; z < size - 1; z++, vi++)
            {
                for (int x = 0; x < size - 1; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 1] = vi + size;
                    triangles[ti + 2] = vi + 1;
                    triangles[ti + 3] = vi + 1;
                    triangles[ti + 4] = vi + size;
                    triangles[ti + 5] = vi + size + 1;
                }
            }

            // ----- apply heights via biome blending -----
            for (int z = 0, i = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++, i++)
                {
                    Vector2 world = new Vector2(x * vertexSpacing, z * vertexSpacing) + noiseOffset;
                    // Biome mask determines how much each biome influences this point
                    float biomeVal = Mathf.PerlinNoise(world.x / biomeNoiseScale, world.y / biomeNoiseScale);

                    float heightSum = 0f;
                    float weightSum = 0f;

                    // Blend contribution from all biomes
                    foreach (var biome in biomes)
                    {
                        float w = BiomeWeight(biomeVal, biome.minMask, biome.maxMask);
                        if (w <= 0f) continue;

                        float hNoise = Mathf.PerlinNoise(world.x / biome.roughnessScale,
                                                         world.y / biome.roughnessScale);
                        float height = hNoise * biome.heightMultiplier;
                        heightSum += height * w;
                        weightSum += w;
                    }

                    float finalHeight = weightSum > 0f ? heightSum / weightSum : 0f;
                    vertices[i].y = finalHeight;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
        }

        // Calculates the blend weight of a biome based on the biome noise value
        // Includes a small fade region so transitions are smooth
        static float BiomeWeight(float value, float min, float max)
        {
            const float fade = 0.05f; // how quickly one biome fades into another
            if (value < min - fade || value > max + fade)
                return 0f;
            if (value < min)
                return Mathf.InverseLerp(min - fade, min, value);
            if (value > max)
                return Mathf.InverseLerp(max + fade, max, value);
            return 1f;
        }
    }
}
