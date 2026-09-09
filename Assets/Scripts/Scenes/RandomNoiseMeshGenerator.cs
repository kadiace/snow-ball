using UnityEngine;
using UnityEngine.Rendering;

public class RandomNoiseMeshGenerator : MonoBehaviour
{
    [Header("Area")]
    [SerializeField] private float _areaSize = 500f;
    [SerializeField] private int _resolution = 100;

    [Header("Terrain Height")]
    [SerializeField] private float _upperHeight = 100f;
    [SerializeField] private float _lowerHeight = -100f;
    [SerializeField] private float _depth = 100f;

    [Header("Noise")]
    [SerializeField] private float _scale = 40f;

    [Header("Visual")]
    [SerializeField] private Material _material;

    private void Start()
    {
        float offsetX = Random.Range(0f, 10000f);
        float offsetZ = Random.Range(0f, 10000f);

        CreateTerrain(
            "UpperTerrain",
            _upperHeight,
            -1f,
            offsetX,
            offsetZ,
            true
        );

        CreateTerrain(
            "LowerTerrain",
            _lowerHeight,
            1f,
            offsetX,
            offsetZ,
            false
        );
    }

    private void CreateTerrain(
        string objectName,
        float baseHeight,
        float heightDirection,
        float offsetX,
        float offsetZ,
        bool flipTriangles)
    {
        GameObject terrainObject = new GameObject(objectName);
        terrainObject.transform.SetParent(transform);
        terrainObject.transform.localPosition = Vector3.zero;

        MeshFilter meshFilter =
            terrainObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            terrainObject.AddComponent<MeshRenderer>();

        meshRenderer.sharedMaterial = _material;

        MeshCollider meshCollider =
            terrainObject.AddComponent<MeshCollider>();

        Mesh mesh = GenerateMesh(
            baseHeight,
            heightDirection,
            offsetX,
            offsetZ,
            flipTriangles
        );

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private Mesh GenerateMesh(
        float baseHeight,
        float heightDirection,
        float offsetX,
        float offsetZ,
        bool flipTriangles)
    {
        Mesh mesh = new Mesh();

        mesh.indexFormat = IndexFormat.UInt32;

        int vertexCountPerSide = _resolution + 1;

        Vector3[] vertices =
            new Vector3[vertexCountPerSide * vertexCountPerSide];

        Vector2[] uvs =
            new Vector2[vertices.Length];

        int[] triangles =
            new int[_resolution * _resolution * 6];

        float step =
            _areaSize / _resolution;

        int vertexIndex = 0;

        for (int z = 0; z <= _resolution; z++)
        {
            for (int x = 0; x <= _resolution; x++)
            {
                float worldX =
                    x * step - _areaSize * 0.5f;

                float worldZ =
                    z * step - _areaSize * 0.5f;

                float noiseX =
                    worldX / _scale + offsetX;

                float noiseZ =
                    worldZ / _scale + offsetZ;

                float noise =
                    Mathf.PerlinNoise(noiseX, noiseZ);

                float y =
                    baseHeight +
                    noise * _depth * heightDirection;

                vertices[vertexIndex] =
                    new Vector3(worldX, y, worldZ);

                uvs[vertexIndex] =
                    new Vector2(
                        (float)x / _resolution,
                        (float)z / _resolution
                    );

                vertexIndex++;
            }
        }

        int triangleIndex = 0;

        for (int z = 0; z < _resolution; z++)
        {
            for (int x = 0; x < _resolution; x++)
            {
                int current =
                    z * vertexCountPerSide + x;

                int next =
                    current + vertexCountPerSide;

                if (!flipTriangles)
                {
                    // 위를 바라보는 면
                    triangles[triangleIndex++] = current;
                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = current + 1;

                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = next + 1;
                }
                else
                {
                    // 아래를 바라보는 면
                    triangles[triangleIndex++] = current;
                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = next;

                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = next + 1;
                    triangles[triangleIndex++] = next;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
