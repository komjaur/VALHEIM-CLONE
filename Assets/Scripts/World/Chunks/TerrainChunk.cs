using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    public Vector2Int Coord { get; private set; }
    WorldGenerator generator;

    readonly List<GameObject> spawnedObjects = new List<GameObject>();

    public void Init(WorldGenerator generator, Vector2Int coord)
    {
        this.generator = generator;
        Coord = coord;
        name = $"Chunk_{coord.x}_{coord.y}";
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        Generate();
    }

    void Generate()
    {
        float size = generator.chunkSize;
        int resolution = generator.chunkResolution;
        float[,] heights = new float[resolution + 1, resolution + 1];
        Vector3[] verts = new Vector3[(resolution + 1) * (resolution + 1)];
        Vector2[] uvs = new Vector2[verts.Length];
        int[] tris = new int[resolution * resolution * 6];

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int i = z * (resolution + 1) + x;
                float wx = Coord.x * size + (float)x / resolution * size;
                float wz = Coord.y * size + (float)z / resolution * size;
                float h = generator.SampleHeight(wx, wz);
                heights[x, z] = h;
                verts[i] = new Vector3(wx, h, wz);
                uvs[i] = new Vector2((float)x / resolution, (float)z / resolution);
            }
        }

        int t = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i0 = z * (resolution + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (resolution + 1);
                int i3 = i2 + 1;
                tris[t++] = i0; tris[t++] = i2; tris[t++] = i1;
                tris[t++] = i1; tris[t++] = i2; tris[t++] = i3;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        meshFilter.sharedMesh = mesh;
        if (!meshCollider) meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        // spawn environment
        generator.SpawnEnvironment(this);
        generator.SpawnMobs(this);
        generator.ApplyWater(this);
    }

    public void ClearObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    public void RegisterObject(GameObject obj)
    {
        spawnedObjects.Add(obj);
    }
}
