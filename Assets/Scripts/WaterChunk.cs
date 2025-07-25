using UnityEngine;

namespace EndlessWorld
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WaterChunk : MonoBehaviour
    {
        MeshFilter _mf;

        void Awake() => _mf = GetComponent<MeshFilter>();

        public void Build(int size, float spacing, float height, Material mat, Vector2Int coord)
        {
            if (_mf.sharedMesh == null || _mf.sharedMesh.vertexCount != size * size)
                _mf.sharedMesh = TerrainChunk.GenerateFlatGrid(size, spacing);

            float w = (size - 1) * spacing;
            transform.position = new Vector3(coord.x * w, height, coord.y * w);
            gameObject.name    = $"Water {coord.x},{coord.y}";
            GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}

