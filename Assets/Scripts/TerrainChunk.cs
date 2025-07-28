using UnityEngine;

namespace EndlessWorld
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainChunk : MonoBehaviour
    {
        MeshFilter   _mf;
        MeshFilter   _waterMf;
        MeshRenderer _waterMr;

        static int _seed = 12345;   // change for a new procedural world

        /* ──────────────────────── */

        void Awake()
        {
            _mf = GetComponent<MeshFilter>();
            EnsureWater();
        }

        /* ──────────────────────── */

        public void Build(int size, float spacing, float noiseScale, float heightMult,
                          Material mat,
                          Vector2Int coord,
                          float heatScale, float wetScale, Biome[] biomes,
                          float waterHeight, Material waterMat)
        {
            /* build mesh if needed */
            if (_mf.sharedMesh == null || _mf.sharedMesh.vertexCount != size * size)
                _mf.sharedMesh = GenerateFlatGrid(size, spacing);

            /* sculpt heights and apply biome colours */
            SculptHeightsAndColors(_mf.sharedMesh, size, spacing,
                                   coord,
                                   heatScale, wetScale, biomes);

            /* place chunk */
            float w = (size - 1) * spacing;
            transform.position = new Vector3(coord.x * w, 0, coord.y * w);
            gameObject.name    = $"Chunk {coord.x},{coord.y}";
            GetComponent<MeshRenderer>().sharedMaterial = mat;

            /* water plane */
            if (_waterMf.sharedMesh == null || _waterMf.sharedMesh.vertexCount != size * size)
                _waterMf.sharedMesh = GenerateFlatGrid(size, spacing);

            _waterMf.transform.localPosition = new Vector3(0f, waterHeight, 0f);
            _waterMr.sharedMaterial = waterMat;

            /* trees */
            SpawnTrees(size, spacing, noiseScale, heightMult,
                       coord, heatScale, wetScale, biomes);
        }

        /* ──────────────────────── */

        void EnsureWater()
        {
            Transform wt = transform.Find("Water");
            if (!wt)
            {
                GameObject w = new("Water");
                w.transform.parent        = transform;
                w.transform.localPosition = Vector3.zero;
                _waterMf                  = w.AddComponent<MeshFilter>();
                _waterMr                  = w.AddComponent<MeshRenderer>();
            }
            else
            {
                _waterMf = wt.GetComponent<MeshFilter>();
                _waterMr = wt.GetComponent<MeshRenderer>();
            }
        }

        /* ──────────────────────── */
        /* -------- helpers ------- */

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

        static float RangeWeight(float value, float min, float max, float fade)
        {
            if (value < min - fade || value > max + fade) return 0f;
            if (value < min) return Mathf.InverseLerp(min - fade, min, value);
            if (value > max) return Mathf.InverseLerp(max + fade, max, value);
            return 1f;
        }

        static Biome ClosestBiome(float heat, float wet, Biome[] biomes, out int index)
        {
            index = 0;
            if (biomes == null || biomes.Length == 0)
                return null;

            Biome closest   = biomes[0];
            float bestDist2 = float.MaxValue;

            for (int i = 0; i < biomes.Length; ++i)
            {
                var b  = biomes[i];
                float dh = heat < b.minHeat     ? b.minHeat     - heat :
                           heat > b.maxHeat     ? heat          - b.maxHeat : 0f;
                float dw = wet  < b.minWetness ? b.minWetness  - wet  :
                           wet  > b.maxWetness ? wet           - b.maxWetness : 0f;
                float d2 = dh * dh + dw * dw;
                if (d2 < bestDist2)
                {
                    bestDist2 = d2;
                    closest   = b;
                    index     = i;
                }
            }
            return closest;
        }

        /* ----- calculate heights and vertex colours ----- */
        static void SculptHeightsAndColors(Mesh m, int size, float spacing,
                                           Vector2Int coord,
                                           float heatScale, float wetScale, Biome[] biomes)
        {
            var v = m.vertices;
            var c = m.colors == null || m.colors.Length != v.Length
                    ? new Color[v.Length]
                    : m.colors;

            float world = (size - 1) * spacing;
            var   heights = new float[v.Length];

            for (int y = 0, i = 0; y < size; y++)
            for (int x = 0; x < size; x++, i++)
            {
                float wx   = coord.x * world + v[i].x;
                float wz   = coord.y * world + v[i].z;

                float heat = Mathf.PerlinNoise((wx + _seed * 2) / heatScale,
                                               (wz + _seed * 2) / heatScale);
                float wet  = Mathf.PerlinNoise((wx + _seed * 3) / wetScale,
                                               (wz + _seed * 3) / wetScale);

                float hSum = 0f, wSum = 0f;
                Color col  = Color.black;

                int   domIndex  = 0;
                float domWeight = -1f;

                if (biomes != null && biomes.Length > 0)
                {
                    for (int b = 0; b < biomes.Length; ++b)
                    {
                        var biome = biomes[b];
                        float bw = RangeWeight(heat, biome.minHeat, biome.maxHeat, 0.05f) *
                                   RangeWeight(wet , biome.minWetness, biome.maxWetness, 0.05f);
                        if (bw <= 0f) continue;

                        if (bw > domWeight)
                        {
                            domWeight = bw;
                            domIndex  = b;
                        }

                        float bh = Mathf.PerlinNoise((wx + _seed) / biome.noiseScale,
                                                     (wz + _seed) / biome.noiseScale) *
                                   biome.heightMultiplier + biome.baseHeight;
                        hSum += bh * bw;
                        col  += biome.color * bw;
                        wSum += bw;
                    }
                }

                if (wSum > 0f)
                {
                    heights[i] = hSum / wSum;
                    col       /= wSum;
                    col.a      = 1f;
                }
                else if (biomes != null && biomes.Length > 0)
                {
                    Biome cb = ClosestBiome(heat, wet, biomes, out domIndex);
                    float bh = Mathf.PerlinNoise((wx + _seed) / cb.noiseScale,
                                                 (wz + _seed) / cb.noiseScale) *
                               cb.heightMultiplier + cb.baseHeight;
                    heights[i] = bh;
                    col        = cb.color;
                    col.a      = 1f;
                }
                else
                {
                    heights[i] = 0f;
                    col        = Color.gray;
                    col.a      = 1f;
                }

                c[i] = col;
            }

            /* simple smoothing pass on heights */
            for (int y = 0, i = 0; y < size; y++)
            for (int x = 0; x < size; x++, i++)
            {
                float sum = 0f; int count = 0;
                for (int yy = -1; yy <= 1; yy++)
                for (int xx = -1; xx <= 1; xx++)
                {
                    int nx = x + xx, ny = y + yy;
                    if (nx < 0 || nx >= size || ny < 0 || ny >= size) continue;
                    sum += heights[ny * size + nx];
                    ++count;
                }
                v[i].y = sum / count;
            }

            m.vertices = v;
            m.colors   = c;
            m.RecalculateNormals();
            m.RecalculateBounds();
        }

        /* ──────────────────────── */

        void SpawnTrees(int size, float spacing, float noiseScale, float heightMult,
                        Vector2Int coord, float heatScale, float wetScale,
                        Biome[] biomes)
        {
            if (biomes == null || biomes.Length == 0) return;

            Transform treeParent = transform.Find("Trees");
            if (!treeParent)
            {
                treeParent = new GameObject("Trees").transform;
                treeParent.parent        = transform;
                treeParent.localPosition = Vector3.zero;
            }
            for (int i = treeParent.childCount - 1; i >= 0; --i)
                Destroy(treeParent.GetChild(i).gameObject);

            float world = (size - 1) * spacing;

            for (int y = 0; y < size; ++y)
            for (int x = 0; x < size; ++x)
            {
                float wx = coord.x * world + x * spacing;
                float wz = coord.y * world + y * spacing;

                float heat = Mathf.PerlinNoise((wx + _seed * 2) / heatScale,
                                               (wz + _seed * 2) / heatScale);
                float wet  = Mathf.PerlinNoise((wx + _seed * 3) / wetScale,
                                               (wz + _seed * 3) / wetScale);

                Biome chosen = null;
                for (int b = 0; b < biomes.Length; ++b)
                {
                    var bio = biomes[b];
                    if (heat >= bio.minHeat && heat <= bio.maxHeat &&
                        wet  >= bio.minWetness && wet  <= bio.maxWetness)
                    {
                        chosen = bio;
                        break;
                    }
                }
                if (chosen == null) chosen = biomes[0];   // fallback

                if (!chosen.treePrefab || Random.value > chosen.treeDensity) continue;

                float h01 = Mathf.PerlinNoise((wx + _seed) / noiseScale,
                                              (wz + _seed) / noiseScale);
                if (h01 < chosen.treeMinHeight || h01 > chosen.treeMaxHeight) continue;

                float wy = h01 * heightMult;
                Instantiate(chosen.treePrefab,
                            new Vector3(wx, wy, wz),
                            Quaternion.identity,
                            treeParent);
            }
        }
    }
}
