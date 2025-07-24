using UnityEngine;

namespace EndlessWorld
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainChunk : MonoBehaviour
    {
        MeshFilter   _mf;
        MeshCollider _mc;

        /* Dedicated mesh that ONLY the collider uses – never shared with renderer  */
        Mesh _colliderMesh;

        static int _seed = 12345;        // change for new world

        /* --------------------------------------------------------------------- */
        void Awake()
        {
            _mf = GetComponent<MeshFilter>();

            _mc = GetComponent<MeshCollider>();
            if (!_mc) _mc = gameObject.AddComponent<MeshCollider>();

            /* One sane flag is enough – obsolete InflateConvexMesh removed       */
            _mc.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation;
        }

        /* 9-parameter Build – signature unchanged so pooling still compiles      */
        public void Build(int size, float spacing, float noiseScale, float heightMult,
                          float sandT, float stoneT, Material mat, Vector2Int coord,
                          bool withCollider)
        {
            /* -------------------------------------------------- build / refresh */
            if (_mf.sharedMesh == null || _mf.sharedMesh.vertexCount != size * size)
                _mf.sharedMesh = GenerateFlatGrid(size, spacing);

            SculptHeightsAndColors(_mf.sharedMesh, size, spacing,
                                   noiseScale, heightMult,
                                   sandT, stoneT, coord);

            /* --------------------------------------------------- place & render */
            float w = (size - 1) * spacing;
            transform.position = new Vector3(coord.x * w, 0, coord.y * w);
            gameObject.name    = $"Chunk {coord.x},{coord.y}";
            GetComponent<MeshRenderer>().sharedMaterial = mat;

            /* -------------------------------------------------- physics collider */
            if (withCollider)
            {
                if (_colliderMesh == null)
                    _colliderMesh = new Mesh { name = "ChunkColliderMesh" };

                /* Copy render mesh → collider mesh.  
                   We duplicate *once* per rebuild, so physics owns its own data. */
                _colliderMesh.Clear();
                _colliderMesh.vertices  = _mf.sharedMesh.vertices;
                _colliderMesh.triangles = _mf.sharedMesh.triangles;
                _colliderMesh.RecalculateNormals();
                _colliderMesh.RecalculateBounds();

                _mc.sharedMesh = _colliderMesh;   // assign after fully populated
                _mc.enabled    = true;
            }
            else
            {
                _mc.enabled = false;
            }
        }

        /* --------------------------------------------------------------------- */
        /* --------------------------- helpers ---------------------------------- */

        static Mesh GenerateFlatGrid(int size, float spacing)
        {
            var verts = new Vector3[size * size];
            var uvs   = new Vector2[verts.Length];
            var tris  = new int[(size - 1) * (size - 1) * 6];

            for (int y = 0, i = 0; y < size; y++)
            for (int x = 0; x < size; x++, i++)
            {
                verts[i] = new Vector3(x * spacing, 0, y * spacing);
                uvs[i]   = new Vector2((float)x / size, (float)y / size);
            }

            for (int y = 0, ti = 0, vi = 0; y < size - 1; y++, vi++)
            for (int x = 0; x < size - 1; x++, ti += 6, vi++)
            {
                tris[ti + 0] = vi;
                tris[ti + 1] = vi + size;
                tris[ti + 2] = vi + 1;
                tris[ti + 3] = vi + 1;
                tris[ti + 4] = vi + size;
                tris[ti + 5] = vi + size + 1;
            }

            Mesh m = new() { vertices = verts, triangles = tris, uv = uvs };
            m.RecalculateNormals();
            return m;
        }

        static void SculptHeightsAndColors(Mesh m, int size, float spacing,
                                           float noiseScale, float heightMult,
                                           float sandT, float stoneT, Vector2Int coord)
        {
            var v  = m.vertices;
            var col = m.colors == null || m.colors.Length != v.Length
                      ? new Color[v.Length] : m.colors;

            float world = (size - 1) * spacing;

            for (int y = 0, i = 0; y < size; y++)
            for (int x = 0; x < size; x++, i++)
            {
                float wx = coord.x * world + v[i].x;
                float wz = coord.y * world + v[i].z;

                float h01 = Mathf.PerlinNoise((wx + _seed) / noiseScale,
                                              (wz + _seed) / noiseScale);

                v[i].y = h01 * heightMult;

                float sel = h01 < sandT     ? 0f :
                            h01 < stoneT    ? 0.5f : 1f;    // 0 sand, .5 grass, 1 rock
                col[i] = new Color(0f, sel, 0f, 1f);
            }

            m.vertices = v;
            m.colors   = col;
            m.RecalculateNormals();
            m.RecalculateBounds();
        }

        /* --------------------------------------------------------------------- */
        /* Prevent tiny leaks when the application quits or chunk is destroyed   */
        void OnDestroy()
        {
            if (_colliderMesh != null)
                Destroy(_colliderMesh);
        }
    }
}
