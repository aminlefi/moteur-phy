using System.Collections.Generic;
using UnityEngine;

// Generates a simple pre-fractured cube by subdividing into smaller box shards.
// It demonstrates applying manual 4x4 transforms to position shards.
public class PreFracturedGenerator : MonoBehaviour
{
    public Vector3 size = Vector3.one;
    public int subdivisions = 2; // number of pieces per axis
    public float shardMass = 1f;
    public Material shardMaterial;
    public bool createGroundPlane = true;
    public Vector3 groundPlaneSize = new Vector3(10f, 1f, 10f);
    public Material groundMaterial;
    public bool autoGenerateOnPlay = true;
    public float verticalOffset = 0.5f; // additional lift above the ground
    [Header("Visual & Physics tweaks")]
    [Range(0f,1f)] public float shardAlpha = 1f;
    [Tooltip("Bounciness of shards (0-1)")]
    [Range(0f,1f)] public float shardBounciness = 0.2f;
    [Tooltip("Friction of shards (0-1)")]
    [Range(0f,1f)] public float shardFriction = 0.6f;
    [Tooltip("Small random initial angular velocity applied to shards for realism")]
    public float initialAngularVelocity = 1f;
    [Tooltip("Small random linear jitter applied to shards on spawn")]
    public float initialLinearJitter = 0.1f;

    // Call from inspector (context menu) or Start
    [ContextMenu("GenerateShards")]
    public void GenerateShards()
    {
        // ensure we don't duplicate on repeated calls
        ClearPreviousGenerated();

        if (subdivisions < 1) subdivisions = 1;

        // get a cube mesh from a temporary primitive
        Mesh cubeMesh = null;
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var mf = temp.GetComponent<MeshFilter>();
        if (mf != null) cubeMesh = mf.sharedMesh;
        DestroyImmediate(temp);

        float sx = size.x / subdivisions;
        float sy = size.y / subdivisions;
        float sz = size.z / subdivisions;

        Vector3 half = new Vector3(size.x, size.y, size.z) * 0.5f;

        int id = 0;
        for (int xi = 0; xi < subdivisions; xi++)
            for (int yi = 0; yi < subdivisions; yi++)
                for (int zi = 0; zi < subdivisions; zi++)
                {
                    Vector3 localCenter = new Vector3(
                        -half.x + (xi + 0.5f) * sx,
                        -half.y + (yi + 0.5f) * sy,
                        -half.z + (zi + 0.5f) * sz
                    );

                    GameObject shard = new GameObject($"Shard_{id++}");
                    shard.transform.parent = this.transform;

                    var mf2 = shard.AddComponent<MeshFilter>();
                    if (cubeMesh != null) mf2.sharedMesh = cubeMesh;
                    var mr = shard.AddComponent<MeshRenderer>();
                    if (shardMaterial != null)
                    {
                        // instantiate material per-shard so alpha changes don't affect the original asset
                        var matInstance = Instantiate(shardMaterial);
                        SetMaterialAlpha(matInstance, shardAlpha);
                        mr.sharedMaterial = matInstance;
                    }

                    // manually compute transform using custom matrix
                    CustomMatrix4x4 M = CustomMatrix4x4.Translation(localCenter);
                    // no rotation here, but could compose rotations: e.g. roll
                    Vector3 worldPos = M.MultiplyPoint(Vector3.zero);
                    shard.transform.position = transform.TransformPoint(worldPos);
                    shard.transform.rotation = M.ExtractRotationQuaternion();
                    shard.transform.localScale = new Vector3(sx, sy, sz);

                    // physics
                    var col = shard.AddComponent<BoxCollider>();
                    col.size = Vector3.one; // collider will scale with transform

                    // create and assign a PhysicMaterial to adjust bounciness/friction
                    var phys = new PhysicsMaterial($"ShardPhys_{id}");
                    phys.bounciness = shardBounciness;
                    phys.dynamicFriction = shardFriction;
                    phys.staticFriction = shardFriction;
                    phys.bounceCombine = PhysicsMaterialCombine.Maximum;
                    col.material = phys;

                    var rb = shard.AddComponent<Rigidbody>();
                    rb.mass = shardMass;
                    rb.useGravity = true;
                    rb.isKinematic = false;
                    // small random jitter so shards don't perfectly stack
                    rb.angularVelocity = UnityEngine.Random.insideUnitSphere * initialAngularVelocity;
                    rb.linearVelocity = UnityEngine.Random.insideUnitSphere * initialLinearJitter;

                    var frag = shard.AddComponent<Fragment>();
                    frag.mass = shardMass;
                    // compute center of inertia from mesh (mesh vertices are in local space of unscaled cube)
                    frag.ComputeCenterOfInertiaFromMesh();
                }

        if (createGroundPlane)
        {
            CreateGroundPlane();
            // lift the parent so shards sit above the plane
            var lowestY = transform.position.y - (size.y * 0.5f);
            float planeTopY = 0f + (groundPlaneSize.y * 0.5f);
            float shift = planeTopY - lowestY + 0.01f + verticalOffset; // small gap + user offset
            transform.position += new Vector3(0, shift, 0);
        }
    }

    void Start()
    {
        if (autoGenerateOnPlay)
        {
            GenerateShards();
        }
    }

    void ClearPreviousGenerated()
    {
        // remove child shards created previously
        var children = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++) children.Add(transform.GetChild(i).gameObject);
        foreach (var c in children)
        {
            if (c.name.StartsWith("Shard_"))
                DestroyImmediate(c);
        }

        // remove previous ground if it exists
        var existingGround = GameObject.Find("FractureGroundPlane");
        if (existingGround != null)
        {
            DestroyImmediate(existingGround);
        }
    }

    void CreateGroundPlane()
    {
        // create a simple cube to act as ground (so it has collider)
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "FractureGroundPlane";
        ground.transform.position = new Vector3(transform.position.x, groundPlaneSize.y * 0.5f, transform.position.z);
        ground.transform.localScale = groundPlaneSize;
        if (groundMaterial != null)
        {
            var mr = ground.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = groundMaterial;
        }
    // make it static and ensure collider is not a trigger
    ground.isStatic = true;
    var col = ground.GetComponent<Collider>();
    if (col != null) col.isTrigger = false;

        // ground phys material: ensure some restitution so shards can bounce
        var groundPhys = new PhysicsMaterial("GroundPhys");
        groundPhys.bounciness = Mathf.Clamp(shardBounciness * 0.5f, 0f, 1f);
        groundPhys.dynamicFriction = shardFriction;
        groundPhys.staticFriction = shardFriction;
        groundPhys.bounceCombine = PhysicsMaterialCombine.Maximum;
        if (col != null) col.material = groundPhys;
    }

    void SetMaterialAlpha(Material mat, float alpha)
    {
        if (mat == null) return;
        Color c = mat.color;
        c.a = Mathf.Clamp01(alpha);
        mat.color = c;

        // Try to set standard shader to transparent mode if available
        if (mat.shader != null && mat.shader.name.Contains("Standard"))
        {
            // _Mode 3 = Transparent
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }
}
