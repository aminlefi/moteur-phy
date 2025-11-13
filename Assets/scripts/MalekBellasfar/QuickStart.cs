using UnityEngine;

/// <summary>
/// Quick setup: Attach this to an empty GameObject and press Play.
/// Creates hollow cube fracture demo with different alpha values.
/// </summary>
public class QuickStart : MonoBehaviour
{
    [Header("Click 'Create Demo' in Inspector or press Play")]
    public bool createOnStart = true;

    [Header("Demo Settings")]
    [Tooltip("Create multiple cubes with different alpha values")]
    public bool createMultipleAlphas = true;
    
    [Tooltip("Single alpha value if createMultipleAlphas is false")]
    [Range(0f, 2f)]
    public float singleAlpha = 1f;

    [ContextMenu("Create Demo")]
    void CreateDemo()
    {
        // Clean up existing demos
        HollowCubeFractureCustom[] existingCubes = FindObjectsByType<HollowCubeFractureCustom>(FindObjectsSortMode.None);
        foreach (var cube in existingCubes)
        {
            DestroyImmediate(cube.gameObject);
        }

        // Also clean up old demos
        FractureDemo existing = FindFirstObjectByType<FractureDemo>();
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        // Create or find PhysicsWorld
        PhysicsWorld physicsWorld = FindFirstObjectByType<PhysicsWorld>();
        if (physicsWorld == null)
        {
            GameObject worldObj = new GameObject("PhysicsWorld");
            physicsWorld = worldObj.AddComponent<PhysicsWorld>();
            physicsWorld.enableGroundPlane = true;
            physicsWorld.showGroundPlane = true;
            physicsWorld.groundHeight = 0f;
            physicsWorld.groundRestitution = 0.3f;
            physicsWorld.groundFriction = 0.5f;
            physicsWorld.gravity = new Vector3(0, -9.81f, 0);
            
            Debug.Log("PhysicsWorld created");
        }

        // Create visual ground plane
        CreateGroundPlane();

        if (createMultipleAlphas)
        {
            // Create 4 hollow cubes with different alpha values (0, 0.5, 1, 2)
            float[] alphaValues = { 0f, 0.5f, 1f, 2f };
            float spacing = 3.5f;
            float startX = -spacing * 1.5f;

            for (int i = 0; i < alphaValues.Length; i++)
            {
                GameObject cubeObj = new GameObject($"HollowCube_Alpha{alphaValues[i]:F1}");
                HollowCubeFractureCustom cube = cubeObj.AddComponent<HollowCubeFractureCustom>();
                
                // Configure cube
                cube.alpha = alphaValues[i];
                cube.pieceCount = 10;
                cube.cubeSize = 2f;
                cube.wallThickness = 0.2f;
                cube.dropHeight = 5f;
                cube.impactVelocityThreshold = 1f;
                cube.fragmentMass = 0.5f;
                cube.explosionForce = 5f;
                cube.physicsWorld = physicsWorld;
                
                // Position cubes in a row
                cubeObj.transform.position = new Vector3(startX + (i * spacing), 0, 0);
            }

            Debug.Log("=== HOLLOW CUBE FRACTURE DEMO (CUSTOM PHYSICS) CREATED ===");
            Debug.Log("4 hollow cubes created with alpha values: 0, 0.5, 1, 2");
            Debug.Log("Left to right: increasing exaggeration effect");
            Debug.Log("Using custom physics engine (no Unity Rigidbody/Collider)");
        }
        else
        {
            // Create single cube with specified alpha
            GameObject cubeObj = new GameObject($"HollowCube_Alpha{singleAlpha:F1}");
            HollowCubeFractureCustom cube = cubeObj.AddComponent<HollowCubeFractureCustom>();
            
            cube.alpha = singleAlpha;
            cube.pieceCount = 10;
            cube.cubeSize = 2f;
            cube.wallThickness = 0.2f;
            cube.dropHeight = 5f;
            cube.impactVelocityThreshold = 1f;
            cube.fragmentMass = 0.5f;
            cube.explosionForce = 5f;
            cube.physicsWorld = physicsWorld;

            Debug.Log($"=== HOLLOW CUBE FRACTURE DEMO (CUSTOM PHYSICS) CREATED ===");
            Debug.Log($"Hollow cube created with alpha: {singleAlpha}");
        }

        Debug.Log("Press PLAY to start simulation");
        Debug.Log("Press R to restart");
    }

    void CreateGroundPlane()
    {
        // Check if ground already exists
        GameObject existingGround = GameObject.Find("GroundVisual");
        if (existingGround != null)
        {
            return; // Ground already exists
        }

        // Create visual ground only (physics handled by PhysicsWorld's GroundPlane)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "GroundVisual";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5, 1, 5);
        
        // Remove Unity collider - we use custom physics
        Collider col = ground.GetComponent<Collider>();
        if (col != null)
        {
            DestroyImmediate(col);
        }
        
        // Add a visible material
        Renderer rend = ground.GetComponent<Renderer>();
        rend.material.color = new Color(0.8f, 0.8f, 0.8f);
    }


    void Start()
    {
        if (createOnStart)
        {
            CreateDemo();
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying)
        {
            GUILayout.BeginArea(new Rect(10, 10, 450, 180));
            GUILayout.Label("=== HOLLOW CUBE FRACTURE - QUICK START ===");
            GUILayout.Label("Press Play to auto-create hollow cube fracture demo");
            GUILayout.Label("");
            GUILayout.Label("By default, 4 cubes with alpha: 0, 0.5, 1, 2");
            GUILayout.Label("(left to right, increasing exaggeration)");
            GUILayout.Label("");
            GUILayout.Label("Or click 'Create Demo' button in Inspector");
            GUILayout.Label("(Right-click on QuickStart component)");
            GUILayout.EndArea();
        }
    }

    void Update()
    {
        // Allow restarting with R key
        if (Application.isPlaying && Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}

