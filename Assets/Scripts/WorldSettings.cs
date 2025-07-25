using UnityEngine;

namespace EndlessWorld
{
    [CreateAssetMenu(menuName = "Endless World/World Settings")]
    public class WorldSettings : ScriptableObject
    {
        [Header("Chunk Geometry")]
        public int   chunkSize = 241;
        public float vertexSpacing = 1f;
        // geometry settings are now defined per biome

        [Header("Biome Generation")]
        public float heatNoiseScale = 100f;
        public float wetnessNoiseScale = 100f;
        public Biome[] biomes;


        [Header("Texture Settings")]
        public float textureTiling = 8f;

        [Header("Water Settings")]
        public float waterHeight = 0f;
        public Color  waterColor = new(0f, 0.5f, 1f, 0.5f);

    }
}
