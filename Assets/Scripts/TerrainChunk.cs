using UnityEngine;

namespace EndlessWorld
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainChunk : MonoBehaviour
    {
        MeshFilter _mf;
        MeshFilter _waterMf;
        MeshRenderer _waterMr;
        static int _seed = 12345;   // randomise for a new world

        /* -------------------------------------------------------- */
        void Awake()
        {
            _mf = GetComponent<MeshFilter>();
            EnsureWater();
        }

        /* -------------------------------------------------------- */
        public void Build(int size, float spacing, float noiseScale, float heightMult,
                          float sandT, float stoneT, Material mat,
                          Vector2Int coord,
                          float heatScale, float wetScale, Biome[] biomes,
                          float waterHeight, Material waterMat)
        {
            /* build / refresh */
            if (_mf.sharedMesh == null || _mf.sharedMesh.vertexCount != size * size)
                _mf.sharedMesh = GenerateFlatGrid(size, spacing);

            SculptHeightsAndColors(_mf.sharedMesh, size, spacing,
                                   noiseScale, heightMult,
                                   sandT, stoneT, coord,
                                   heatScale, wetScale, biomes);

            /* place & render */
            float w = (size - 1) * spacing;
            transform.position = new Vector3(coord.x * w, 0, coord.y * w);
            gameObject.name    = $"Chunk {coord.x},{coord.y}";
            GetComponent<MeshRenderer>().sharedMaterial = mat;

            /* water */
            if (_waterMf.sharedMesh == null || _waterMf.sharedMesh.vertexCount != size * size)
                _waterMf.sharedMesh = GenerateFlatGrid(size, spacing);

            _waterMf.transform.localPosition = new Vector3(0f, waterHeight, 0f);
            _waterMr.sharedMaterial = waterMat;

            SpawnTrees(size, spacing, noiseScale, heightMult, coord,
                       heatScale, wetScale, biomes);

        }

        void EnsureWater()
        {
            Transform wt = transform.Find("Water");
            if (!wt)
            {
                GameObject w = new("Water");
                w.transform.parent = transform;
                w.transform.localPosition = Vector3.zero;
                _waterMf = w.AddComponent<MeshFilter>();
                _waterMr = w.AddComponent<MeshRenderer>();
            }
            else
            {
                _waterMf = wt.GetComponent<MeshFilter>();
                _waterMr = wt.GetComponent<MeshRenderer>();
            }
        }

        /* ------------------ helpers ------------------ */
        public static Mesh GenerateFlatGrid(int size, float spacing)
        {
            var v = new Vector3[size * size];
            var u = new Vector2[v.Length];
            var t = new int[(size - 1) * (size - 1) * 6];

            for (int y = 0, i = 0; y < size; y++)
            for (int x = 0; x < size; x++, i++)
            {
                v[i] = new Vector3(x * spacing, 0, y * spacing);
                u[i] = new Vector2((float)x / size, (float)y / size);
            }

            for (int y = 0, ti = 0, vi = 0; y < size - 1; y++, vi++)
            for (int x = 0; x < size - 1; x++, ti += 6, vi++)
            {
                t[ti + 0] = vi;
                t[ti + 1] = vi + size;
                t[ti + 2] = vi + 1;
                t[ti + 3] = vi + 1;
                t[ti + 4] = vi + size;
                t[ti + 5] = vi + size + 1;
            }

            Mesh m = new() { vertices = v, triangles = t, uv = u };
            m.RecalculateNormals();
            return m;
        }

        static void SculptHeightsAndColors(Mesh m, int size, float spacing,
                                           float noiseScale, float heightMult,
                                           float sandT, float stoneT, Vector2Int coord,
                                           float heatScale, float wetScale, Biome[] biomes)
        {
            var v = m.vertices;
            var c = m.colors == null || m.colors.Length != v.Length
                    ? new Color[v.Length] : m.colors;

            float world = (size - 1) * spacing;

            for (int y = 0, i = 0; y < size; y++)
            for (int x = 0; x < size; x++, i++)
            {
                float wx = coord.x * world + v[i].x;
                float wz = coord.y * world + v[i].z;

                float h01 = Mathf.PerlinNoise((wx + _seed) / noiseScale,
                                              (wz + _seed) / noiseScale);

                float heat = Mathf.PerlinNoise((wx + _seed*2) / heatScale,
                                               (wz + _seed*2) / heatScale);
                float wet  = Mathf.PerlinNoise((wx + _seed*3) / wetScale,
                                               (wz + _seed*3) / wetScale);

                v[i].y = h01 * heightMult;

                Color col = Color.black;
                if (biomes != null)
                {
                    foreach (var b in biomes)
                    {
                        if (heat >= b.minHeat && heat <= b.maxHeat &&
                            wet  >= b.minWetness && wet  <= b.maxWetness)
                        {
                            col = b.color;
                            break;
                        }
                    }
                }
                c[i] = col;
            }

            m.vertices = v;
            m.colors   = c;
            m.RecalculateNormals();
            m.RecalculateBounds();
        }

        void SpawnTrees(int size, float spacing, float noiseScale, float heightMult,
                        Vector2Int coord, float heatScale, float wetScale,
                        Biome[] biomes)
        {
            if (biomes == null || biomes.Length == 0)
                return;

            Transform treeParent = transform.Find("Trees");
            if (!treeParent)
            {
                treeParent = new GameObject("Trees").transform;
                treeParent.parent = transform;
                treeParent.localPosition = Vector3.zero;
            }
            for (int i = treeParent.childCount - 1; i >= 0; i--)
                Destroy(treeParent.GetChild(i).gameObject);

            float world = (size - 1) * spacing;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float wx = coord.x * world + x * spacing;
                float wz = coord.y * world + y * spacing;

                float heat = Mathf.PerlinNoise((wx + _seed*2) / heatScale,
                                               (wz + _seed*2) / heatScale);
                float wet  = Mathf.PerlinNoise((wx + _seed*3) / wetScale,
                                               (wz + _seed*3) / wetScale);

                Biome chosen = null;
                foreach (var b in biomes)
                {
                    if (heat >= b.minHeat && heat <= b.maxHeat &&
                        wet  >= b.minWetness && wet  <= b.maxWetness)
                    {
                        chosen = b;
                        break;
                    }
                }
                if (chosen == null) continue;
                if (!chosen.treePrefab || Random.value > chosen.treeDensity) continue;

                float h01 = Mathf.PerlinNoise((wx + _seed) / noiseScale,
                                              (wz + _seed) / noiseScale);
                if (h01 < chosen.treeMinHeight || h01 > chosen.treeMaxHeight) continue;

                float wy = h01 * heightMult;
                Instantiate(chosen.treePrefab, new Vector3(wx, wy, wz), Quaternion.identity,
                           treeParent);
            }
        }
    }
}

