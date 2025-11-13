using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(CustomTransform))]
public class ProceduralCube : MonoBehaviour
{
    public Vector3 size = Vector3.one;
    public Material material; // base material (will be instantiated per-cube at runtime)
    public UnityEngine.Color color = default; // editable in inspector
    public Vector3 HalfSize => size *0.5f;
    private CustomTransform _ct;
    private Mesh _mesh;
    private Vector3[] _baseVertices;
    private Material _runtimeMaterial; // per-instance material used for rendering

    void Awake()
    {
        _ct = GetComponent<CustomTransform>();
        _ct.CacheInitialState();
        Build();
        if (_mesh != null) _mesh.MarkDynamic();
        ApplyTransformToMesh();
    }

    void LateUpdate() { ApplyTransformToMesh(); }

#if UNITY_EDITOR
    void OnValidate()
    {
        // reflect color changes from inspector in edit/play mode
        var mr = GetComponent<MeshRenderer>();
        if (mr == null) return;
        if (_runtimeMaterial != null)
        {
            _runtimeMaterial.color = color;
        }
        else if (mr.sharedMaterial != null)
        {
            mr.sharedMaterial.color = color;
        }
    }
#endif

    void Build()
    {
        float hx = size.x *0.5f, hy = size.y *0.5f, hz = size.z *0.5f;
        _baseVertices = new Vector3[]
        {
            new Vector3(-hx, -hy, hz), new Vector3( hx, -hy, hz), new Vector3( hx, hy, hz), new Vector3(-hx, hy, hz),
            new Vector3( hx, -hy, hz), new Vector3( hx, -hy, -hz), new Vector3( hx, hy, -hz), new Vector3( hx, hy, hz),
            new Vector3( hx, -hy, -hz), new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3( hx, hy, -hz),
            new Vector3(-hx, -hy, -hz), new Vector3(-hx, -hy, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, hy, -hz),
            new Vector3(-hx, hy, hz), new Vector3( hx, hy, hz), new Vector3( hx, hy, -hz), new Vector3(-hx, hy, -hz),
            new Vector3(-hx, -hy, -hz), new Vector3( hx, -hy, -hz), new Vector3( hx, -hy, hz), new Vector3(-hx, -hy, hz),
        };
        Vector2[] uv = new Vector2[]
        {
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1),
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1),
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1),
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1),
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1),
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1),
        };
        int[] tris = new int[] {0,1,2,0,2,3,4,5,6,4,6,7,8,9,10,8,10,11,12,13,14,12,14,15,16,17,18,16,18,19,20,21,22,20,22,23 };
        _mesh = new Mesh { name = "ProceduralCube" };
        _mesh.vertices = _baseVertices; _mesh.uv = uv; _mesh.triangles = tris; _mesh.RecalculateNormals(); _mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = _mesh;

        // Ensure a base material exists to avoid NRE if not assigned in inspector
        var mr = GetComponent<MeshRenderer>();
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
        }

        // Create a per-instance runtime material so color can differ per cube
        _runtimeMaterial = new Material(material);
        _runtimeMaterial.color = color;
        mr.material = _runtimeMaterial; // assigns the instance
    }

    private void ApplyTransformToMesh()
    {
        if (_mesh == null || _baseVertices == null || _ct == null) return;
        var worldVerts = new Vector3[_baseVertices.Length];
        var M = _ct.LocalToWorldMatrix; // no Unity Transform considered
        for (int i =0; i < _baseVertices.Length; i++) worldVerts[i] = M.TransformPoint(_baseVertices[i]);
        _mesh.vertices = worldVerts; _mesh.RecalculateBounds(); _mesh.RecalculateNormals();
    }
}