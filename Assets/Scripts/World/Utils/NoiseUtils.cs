using UnityEngine;

public static class NoiseUtils
{
    /// <summary>
    /// Gets 2D Perlin noise value for the given world coordinates and scale.
    /// </summary>
    public static float Perlin(float x, float z, float scale, int seed = 0)
    {
        x *= scale;
        z *= scale;
        float noise = Mathf.PerlinNoise(x + seed, z + seed);
        return noise;
    }
}
