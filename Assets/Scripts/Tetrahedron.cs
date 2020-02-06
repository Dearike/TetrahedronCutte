using UnityEngine;

public class Tetrahedron : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private float radius;

    private void Start()
    {
        GameObject tetrahedron = new GameObject(
            "Tetrahedron", 
            typeof(MeshFilter), 
            typeof(MeshRenderer), 
            typeof(MeshCollider), 
            typeof(RotationByMouse), 
            typeof(DrawBounds));

        tetrahedron.transform.parent = transform;
        tetrahedron.tag = "Sliceable";

        Mesh tetrahedronMesh = GetTetrahedron(radius);

        tetrahedron.GetComponent<MeshFilter>().mesh = tetrahedronMesh;
        tetrahedron.GetComponent<MeshCollider>().sharedMesh = tetrahedronMesh;
        tetrahedron.GetComponent<MeshRenderer>().material = material;
    }

    public static Mesh GetTetrahedron(float radius)
    {
        var tetrahedralAngle = Mathf.PI * 109.4712f / 180;
        var segmentAngle = Mathf.PI * 2 / 3;
        var currentAngle = 0f;

        var v = new Vector3[4];
        v[0] = new Vector3(0, radius, 0);

        for (var i = 1; i <= 3; i++)
        {
            v[i] = new Vector3(radius * Mathf.Sin(currentAngle) * Mathf.Sin(tetrahedralAngle),
                                radius * Mathf.Cos(tetrahedralAngle),
                                radius * Mathf.Cos(currentAngle) * Mathf.Sin(tetrahedralAngle));
            currentAngle = currentAngle + segmentAngle;
        }

        var combine = new CombineInstance[4];
        combine[0].mesh = GetTriangle(v[0], v[1], v[2]);
        combine[1].mesh = GetTriangle(v[1], v[3], v[2]);
        combine[2].mesh = GetTriangle(v[0], v[2], v[3]);
        combine[3].mesh = GetTriangle(v[0], v[3], v[1]);

        var mesh = new Mesh();
        mesh.CombineMeshes(combine, true, false);
        mesh.name = "Tetrahedron";
        return mesh;
    }

    public static Mesh GetTriangle(Vector3 vertex0, Vector3 vertex1, Vector3 vertex2)
    {
        var normal = Vector3.Cross((vertex1 - vertex0), (vertex2 - vertex0)).normalized;

        var mesh = new Mesh
        {
            vertices = new[] { vertex0, vertex1, vertex2 },
            normals = new[] { normal, normal, normal },
            uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1) },
            triangles = new[] { 0, 1, 2 }
        };

        return mesh;
    }
}
