using UnityEngine;

[CreateAssetMenu(menuName = "World/Biome")]
public class BiomeDefinition : ScriptableObject
{
    public string biomeName = "New Biome";

    [Range(0f,1f)]
    public float minThreshold = 0f;
    [Range(0f,1f)]
    public float maxThreshold = 1f;

    public float heightMultiplier = 5f;
    public float roughness = 1f;
    public AnimationCurve heightCurve = AnimationCurve.Linear(0,0,1,1);

    public Texture2D texture;
    public GameObject[] treePrefabs;
    public GameObject[] mobPrefabs;
}
