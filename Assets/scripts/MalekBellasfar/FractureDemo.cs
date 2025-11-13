using UnityEngine;

/// <summary>
/// Demo scene that creates a pre-fractured beam that breaks under stress.
/// Demonstrates the energized fracture system.
/// </summary>
public class FractureDemo : MonoBehaviour
{
    [Header("Beam Configuration")]
    [Tooltip("Number of fragments along the beam")]
    public int segmentCount = 8;
    
    [Tooltip("Size of each fragment")]
    public Vector3 fragmentSize = new Vector3(0.5f, 0.5f, 0.5f);
    
    [Tooltip("Mass of each fragment")]
    public float fragmentMass = 1f;
    
    [Tooltip("Starting height of the beam")]
    public float beamHeight = 5f;

    [Header("Constraint Settings")]
    [Tooltip("Compliance Σ (0 = rigid, higher = softer)")]
    public float compliance = 0.0001f;
    
    [Tooltip("Error reduction Γ (0-1)")]
    [Range(0f, 1f)]
    public float gamma = 0.8f;
    
    [Tooltip("Break threshold ε for lambda")]
    public float breakThreshold = 5f;

    [Header("Interactive")]
    public bool applyImpulseOnSpace = true;
    public float impulseStrength = 10f;

    [Header("Visual Ground")]
    public bool createVisualGround = true;
    public Vector3 groundSize = new Vector3(20f, 0.1f, 20f);
    public Color groundColor = new Color(0.4f, 0.7f, 0.4f);

    private PhysicsWorld physicsWorld;

    void Start()
    {
        CreateFractureDemo();
    }

    void CreateFractureDemo()
    {
        // Get or create physics world
        physicsWorld = FindObjectOfType<PhysicsWorld>();
        if (physicsWorld == null)
        {
            GameObject worldObj = new GameObject("PhysicsWorld");
            physicsWorld = worldObj.AddComponent<PhysicsWorld>();
            physicsWorld.solverIterations = 10;
            physicsWorld.energyTransferAlpha = 1f;
            physicsWorld.enableGroundPlane = true;
            physicsWorld.groundHeight = 0f;
            physicsWorld.groundRestitution = 0.3f;
            physicsWorld.groundFriction = 0.5f;
        }

        // Create visual ground plane
        if (createVisualGround)
        {
            CreateVisualGround();
        }

        // Create beam segments
        RigidBody[] segments = new RigidBody[segmentCount];
        GameObject[] segmentObjects = new GameObject[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 position = new Vector3(i * fragmentSize.x, beamHeight, 0);
            
            // Create physics body
            Vector3 inertia = ProceduralMesh.CalculateBoxInertia(fragmentMass, fragmentSize);
            segments[i] = new RigidBody(position, Quaternion.identity, fragmentMass, inertia);
            physicsWorld.AddRigidBody(segments[i]);

            // Create visual representation
            GameObject segmentObj = new GameObject($"Segment_{i}");
            segmentObjects[i] = segmentObj;
            
            MeshFilter meshFilter = segmentObj.AddComponent<MeshFilter>();
            meshFilter.mesh = ProceduralMesh.CreateCube(fragmentSize * 0.95f); // Slightly smaller to see gaps
            
            RigidBodyRenderer renderer = segmentObj.AddComponent<RigidBodyRenderer>();
            renderer.physicsBody = segments[i];
            
            // Color gradient along beam
            float t = i / (float)(segmentCount - 1);
            renderer.bodyColor = Color.Lerp(new Color(1f, 0.5f, 0.2f), new Color(0.2f, 0.5f, 1f), t);
        }

        // Create constraints between adjacent segments
        for (int i = 0; i < segmentCount - 1; i++)
        {
            // Connect at the edges
            Vector3 anchorA = new Vector3(fragmentSize.x * 0.5f, 0, 0);
            Vector3 anchorB = new Vector3(-fragmentSize.x * 0.5f, 0, 0);
            
            Constraint constraint = new Constraint(segments[i], segments[i + 1], anchorA, anchorB);
            constraint.compliance = compliance;
            constraint.gamma = gamma;
            constraint.breakThreshold = breakThreshold;
            
            physicsWorld.AddConstraint(constraint);
        }

        Debug.Log($"Created fracture demo with {segmentCount} segments and {segmentCount - 1} constraints");
    }

    void CreateVisualGround()
    {
        GameObject ground = new GameObject("VisualGround");
        
        // Create mesh
        MeshFilter meshFilter = ground.AddComponent<MeshFilter>();
        meshFilter.mesh = ProceduralMesh.CreateCube(groundSize);
        
        // Create renderer with URP transparent material
        MeshRenderer meshRenderer = ground.AddComponent<MeshRenderer>();
        Material mat = MaterialUtility.CreateTransparentURPMaterial(groundColor, alpha: 0.7f);
        meshRenderer.material = mat;
        
        // Position at ground level
        ground.transform.position = new Vector3(0, physicsWorld.groundHeight - groundSize.y * 0.5f, 0);
    }

    void Update()
    {
        // Apply impulse to center segment when space is pressed
        if (applyImpulseOnSpace && Input.GetKeyDown(KeyCode.Space))
        {
            var bodies = physicsWorld.GetRigidBodies();
            if (bodies.Count > 0)
            {
                int centerIndex = bodies.Count / 2;
                RigidBody centerBody = bodies[centerIndex];
                
                Vector3 impulse = Vector3.down * impulseStrength;
                centerBody.ApplyImpulse(impulse, centerBody.position);
                
                Debug.Log($"Applied impulse {impulse} to segment {centerIndex}");
            }
        }

        // Restart on R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 150));
        GUILayout.Label("=== FRACTURE DEMO ===");
        GUILayout.Label("SPACE: Apply impulse to center");
        GUILayout.Label("R: Restart scene");
        GUILayout.Label("");
        
        var constraints = physicsWorld.GetConstraints();
        int activeCount = 0;
        foreach (var c in constraints)
        {
            if (!c.isBroken) activeCount++;
        }
        
        GUILayout.Label($"Constraints: {activeCount}/{constraints.Count}");
        GUILayout.Label($"Segments: {physicsWorld.GetRigidBodies().Count}");
        GUILayout.EndArea();
    }
}

