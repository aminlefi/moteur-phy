using UnityEngine;

/// <summary>
/// Hollow cube fracture demo using custom physics engine (no Unity Rigidbody/Collider).
/// Creates a hollow cube that fractures into pieces with alpha-controlled exaggeration.
/// </summary>
public class HollowCubeFractureCustom : MonoBehaviour
{
    [Header("Fracture Settings")]
    [Range(0f, 2f)]
    [Tooltip("Alpha parameter: 0=natural, 0.5=moderate, 1=strong, 2=exaggerated")]
    public float alpha = 0f;
    
    [Range(5, 20)]
    public int pieceCount = 10;
    
    [Header("Hollow Cube Settings")]
    public float cubeSize = 2f;
    public float wallThickness = 0.2f;
    public float dropHeight = 5f;
    
    [Header("Physics Settings")]
    public float impactVelocityThreshold = 1f;
    public float fragmentMass = 0.5f;
    public float explosionForce = 5f;
    
    [Header("Visual Settings")]
    public Material fragmentMaterial;
    
    [Header("References")]
    public PhysicsWorld physicsWorld;

    private RigidBody physicsBody;
    private bool hasFractured = false;
    private GameObject visualObject;
    private float lastY;

    void Start()
    {
        // Find or create physics world
        if (physicsWorld == null)
        {
            physicsWorld = FindFirstObjectByType<PhysicsWorld>();
            if (physicsWorld == null)
            {
                Debug.LogError("PhysicsWorld not found! Please add PhysicsWorld component to scene.");
                return;
            }
        }

        CreateHollowCube();
    }

    void CreateHollowCube()
    {
        // Calculate inertia for hollow cube
        float hollowMass = fragmentMass * pieceCount;
        Vector3 inertia = CalculateHollowCubeInertia(hollowMass, cubeSize, wallThickness);
        
        // Create physics body
        physicsBody = new RigidBody(
            transform.position + Vector3.up * dropHeight,
            Quaternion.identity,
            hollowMass,
            inertia
        );
        
        // Add to physics world
        physicsWorld.AddRigidBody(physicsBody);
        
        // Create visual representation
        CreateVisualHollowCube();
        
        lastY = physicsBody.position.y;
    }

    void CreateVisualHollowCube()
    {
        visualObject = new GameObject($"HollowCube_Visual_Alpha{alpha:F1}");
        visualObject.transform.SetParent(transform);
        
        // Create 6 walls
        CreateWall(visualObject.transform, Vector3.forward * (cubeSize/2 - wallThickness/2), Vector3.zero, new Vector3(cubeSize, cubeSize, wallThickness));
        CreateWall(visualObject.transform, Vector3.back * (cubeSize/2 - wallThickness/2), Vector3.zero, new Vector3(cubeSize, cubeSize, wallThickness));
        CreateWall(visualObject.transform, Vector3.left * (cubeSize/2 - wallThickness/2), Vector3.up * 90, new Vector3(cubeSize, cubeSize, wallThickness));
        CreateWall(visualObject.transform, Vector3.right * (cubeSize/2 - wallThickness/2), Vector3.up * 90, new Vector3(cubeSize, cubeSize, wallThickness));
        CreateWall(visualObject.transform, Vector3.up * (cubeSize/2 - wallThickness/2), Vector3.right * 90, new Vector3(cubeSize, cubeSize, wallThickness));
        CreateWall(visualObject.transform, Vector3.down * (cubeSize/2 - wallThickness/2), Vector3.right * 90, new Vector3(cubeSize, cubeSize, wallThickness));
    }

    void CreateWall(Transform parent, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.SetParent(parent);
        wall.transform.localPosition = position;
        wall.transform.localEulerAngles = rotation;
        wall.transform.localScale = scale;
        
        // Remove Unity collider
        Collider col = wall.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        if (fragmentMaterial != null)
        {
            wall.GetComponent<Renderer>().material = fragmentMaterial;
        }
    }

    Vector3 CalculateHollowCubeInertia(float mass, float size, float thickness)
    {
        // Simplified hollow cube inertia (approximate as thin shell)
        float I = (mass * size * size) / 6f;
        return new Vector3(I, I, I);
    }

    void Update()
    {
        if (physicsBody == null || hasFractured) return;

        // Sync visual with physics
        visualObject.transform.position = physicsBody.position;
        visualObject.transform.rotation = physicsBody.rotation;

        // Check for ground impact
        float currentY = physicsBody.position.y;
        
        // Detect impact with ground (crossing ground plane with downward velocity)
        float groundLevel = physicsWorld.groundHeight + cubeSize * 0.5f;
        if (currentY <= groundLevel && lastY > groundLevel)
        {
            float impactVelocity = Mathf.Abs(physicsBody.velocity.y);
            
            Debug.Log($"Ground impact detected! Velocity: {impactVelocity:F2} m/s, Threshold: {impactVelocityThreshold}");
            
            if (impactVelocity >= impactVelocityThreshold)
            {
                Vector3 impactPoint = new Vector3(physicsBody.position.x, groundLevel, physicsBody.position.z);
                FractureIntoPieces(impactPoint);
                hasFractured = true;
            }
        }

        lastY = currentY;
    }

    void FractureIntoPieces(Vector3 impactPoint)
    {
        Vector3 center = physicsBody.position;
        
        for (int i = 0; i < pieceCount; i++)
        {
            // Random size variation
            float sizeVariation = Random.Range(0.8f, 1.2f);
            float fragmentSize = (cubeSize / Mathf.Sqrt(pieceCount)) * sizeVariation;
            
            // Random position within cube bounds
            Vector3 randomOffset = new Vector3(
                Random.Range(-cubeSize/2, cubeSize/2),
                Random.Range(-cubeSize/2, cubeSize/2),
                Random.Range(-cubeSize/2, cubeSize/2)
            );
            Vector3 fragmentPos = center + randomOffset;
            
            // Create fragment physics body
            Vector3 fragmentInertia = CalculateCubeInertia(fragmentMass, fragmentSize);
            RigidBody fragBody = new RigidBody(
                fragmentPos,
                Random.rotation,
                fragmentMass,
                fragmentInertia
            );
            
            // Calculate explosion direction with alpha influence
            Vector3 directionFromImpact = (fragmentPos - impactPoint).normalized;
            Vector3 directionFromCenter = (fragmentPos - center).normalized;
            
            // EQUATION 1: Direction blending based on alpha
            Vector3 explosionDir = Vector3.Lerp(directionFromImpact, directionFromCenter, alpha * 0.5f);
            
            // EQUATION 2: Force multiplier based on alpha
            float forceMultiplier = 1f + (alpha * 2f); // 0→1x, 0.5→2x, 1→3x, 2→5x
            
            // Apply explosion velocity (impulse)
            fragBody.velocity = explosionDir * explosionForce * forceMultiplier;
            
            // EQUATION 3: Torque/angular velocity scaling based on alpha
            fragBody.angularVelocity = Random.insideUnitSphere * 5f * (1f + alpha);
            
            // Add to physics world
            physicsWorld.AddRigidBody(fragBody);
            
            // Create visual representation
            CreateFragmentVisual(fragBody, fragmentSize, i);
        }
        
        // Destroy original hollow cube
        if (visualObject != null) Destroy(visualObject);
        Destroy(gameObject);
    }

    void CreateFragmentVisual(RigidBody fragBody, float size, int index)
    {
        GameObject fragObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fragObj.name = $"Fragment_{index}_Alpha{alpha:F1}";
        fragObj.transform.localScale = Vector3.one * size;
        
        // Remove Unity collider
        Collider col = fragObj.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Add renderer component to sync with physics
        RigidBodyRenderer rbRenderer = fragObj.AddComponent<RigidBodyRenderer>();
        rbRenderer.physicsBody = fragBody;
        
        // Apply material or random color
        if (fragmentMaterial != null)
        {
            rbRenderer.bodyColor = fragmentMaterial.color;
        }
        else
        {
            rbRenderer.bodyColor = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 1f);
        }
    }

    Vector3 CalculateCubeInertia(float mass, float size)
    {
        // Cube inertia: I = (1/6) * m * s^2
        float I = (mass * size * size) / 6f;
        return new Vector3(I, I, I);
    }

    void OnGUI()
    {
        if (hasFractured) return;
        
        GUILayout.BeginArea(new Rect(10, 150, 400, 100));
        GUILayout.Label($"Alpha {alpha:F1}: Height={physicsBody.position.y:F2}m, Vel={physicsBody.velocity.y:F2}m/s");
        GUILayout.EndArea();
    }
}

