using UnityEngine;

namespace EndlessWorld
{
    [CreateAssetMenu(menuName = "Endless World/Biome")]
    public class Biome : ScriptableObject
    {
        [Header("Biome Range (0..1)")]
        [Range(0f,1f)] public float minHeat;
        [Range(0f,1f)] public float maxHeat = 1f;
        [Range(0f,1f)] public float minWetness;
        [Range(0f,1f)] public float maxWetness = 1f;

        [Header("Visuals")]
        public Color color = Color.green;

        [Header("Tree Settings")]
        public GameObject treePrefab;
        [Range(0f,1f)] public float treeMinHeight = 0.4f;
        [Range(0f,1f)] public float treeMaxHeight = 0.7f;
        [Range(0f,1f)] public float treeDensity = 0.1f;

        [Header("Terrain Settings")]
        public float noiseScale = 60f;
        public float heightMultiplier = 25f;

        [Header("Biome Texture")]
        public Texture2D texture;
    }
}
