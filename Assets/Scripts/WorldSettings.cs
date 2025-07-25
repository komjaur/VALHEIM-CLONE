using UnityEngine;

namespace EndlessWorld
{
    [CreateAssetMenu(menuName = "Endless World/World Settings")]
    public class WorldSettings : ScriptableObject
    {
        [Header("Chunk Geometry")]
        public int   chunkSize = 241;
        public float vertexSpacing = 1f;
        public float noiseScale = 60f;
        public float heightMultiplier = 25f;

        [Header("Biome Generation")]
        public float heatNoiseScale = 100f;
        public float wetnessNoiseScale = 100f;
        public Biome[] biomes;

        [Header("Biome Textures & Thresholds")]
        public Texture2D sandTex;
        public Texture2D grassTex;
        public Texture2D stoneTex;
        [Range(0f,1f)] public float sandHeight = 0.35f;
        [Range(0f,1f)] public float stoneHeight = 0.75f;
        public float textureTiling = 8f;

        [Header("Tree Settings")]
        public GameObject treePrefab;
        [Range(0f,1f)] public float treeMinHeight = 0.4f;
        [Range(0f,1f)] public float treeMaxHeight = 0.7f;
        [Range(0f,1f)] public float treeDensity = 0.1f;

        [Header("Water Settings")]
        public float waterHeight = 0f;
        public Color  waterColor = new(0f, 0.5f, 1f, 0.5f);

    }
}
