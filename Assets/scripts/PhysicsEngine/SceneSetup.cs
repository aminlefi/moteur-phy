using UnityEngine;

/// <summary>
/// Script pour setup rapide d'une scène de test
/// Attacher à un GameObject vide dans la scène
/// </summary>
public class SceneSetup : MonoBehaviour
{
    [Header("Setup automatique au démarrage")]
    public bool autoSetup = true;
    
    [Header("Cube Principal")]
    public Vector3 cubePosition = new Vector3(0, 5, 0);
    public Vector3 cubeSize = new Vector3(2, 2, 2);
    
    [Header("Caméra")]
    public Vector3 cameraPosition = new Vector3(0, 3, -10);
    public Vector3 cameraLookAt = new Vector3(0, 3, 0);
    
    [Header("Sol (visualisation)")]
    public bool createFloor = true;
    public Vector3 floorPosition = new Vector3(0, 0, 0);
    // Make the default floor very large so fragments don't fall off the edge
    public Vector3 floorSize = new Vector3(1000f, 0.1f, 1000f);
    
    void Start()
    {
        if (autoSetup)
        {
            SetupScene();
        }
    }
    
    [ContextMenu("Setup Scene")]
    void SetupScene()
    {
        // 1. Créer le cube principal avec FractureSystem
        CreateMainCube();
        
        // 2. Setup caméra
        SetupCamera();
        
        // 3. Créer sol (optionnel)
        if (createFloor)
        {
            CreateFloor();
        }
        
        // 4. Ajouter lumière directionnelle
        SetupLighting();
        
        Debug.Log("Scene setup complete!");
    }
    
    void CreateMainCube()
    {
        // Vérifier si existe déjà
        GameObject existing = GameObject.Find("Fracturable Cube");
        if (existing != null)
        {
            Debug.Log("Fracturable Cube already exists");
            return;
        }
        
        // Créer le cube MANUELLEMENT (pas de primitive Unity!)
        GameObject mainCube = MeshGenerator.CreateCubeGameObject(
            cubePosition, 
            cubeSize,
            CreateDefaultMaterial()
        );
        
        mainCube.name = "Fracturable Cube";
        
        // Ajouter FractureSystem
        FractureSystem fractureSystem = mainCube.AddComponent<FractureSystem>();
        fractureSystem.fracturesX = 2;
        fractureSystem.fracturesY = 2;
        fractureSystem.fracturesZ = 2;
        fractureSystem.fragmentMass = 1f;
        fractureSystem.constraintStiffness = 1000f;
        fractureSystem.breakThreshold = 0.5f;
        fractureSystem.fragmentMaterial = CreateFragmentMaterial();
        
        // Ajouter Demo controller
        mainCube.AddComponent<FractureDemo>();
        
        Debug.Log("Created main cube with FractureSystem");
    }
    
    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        
        cam.transform.position = cameraPosition;
        cam.transform.LookAt(cameraLookAt);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.2f, 0.3f, 0.4f);
    }
    
    void CreateFloor()
    {
        GameObject existing = GameObject.Find("Floor");
        GameObject floor = null;
        if (existing != null)
        {
            // If a floor already exists in the scene, resize/update it instead of creating another
            floor = existing;
            floor.transform.position = floorPosition;

            // Force a very large floor size at runtime to avoid small serialized values
            Vector3 forcedSize = new Vector3(1000f, 0.1f, 1000f);
            MeshFilter mf = floor.GetComponent<MeshFilter>();
            MeshRenderer mr = floor.GetComponent<MeshRenderer>();
            if (mf != null)
            {
                mf.mesh = MeshGenerator.CreateCubeMesh(forcedSize);
            }
            if (mr != null)
            {
                mr.material = CreateFloorMaterial();
                mr.enabled = true;
            }
        }
        else
        {
            // Créer sol manuellement
            Vector3 forcedSize = new Vector3(1000f, 0.1f, 1000f);
            floor = MeshGenerator.CreateCubeGameObject(
                floorPosition,
                forcedSize,
                CreateFloorMaterial()
            );
            floor.name = "Floor";
        }
        // Ensure it has a BoxCollider so physics fragments collide with it
        var bc = floor.GetComponent<BoxCollider>();
        if (bc == null) bc = floor.AddComponent<BoxCollider>();

        // Try to size the collider to the mesh bounds created by MeshGenerator.
        var mf2 = floor.GetComponent<MeshFilter>();
        if (mf2 != null && mf2.mesh != null)
        {
            // Mesh bounds are in local space; use them to size the collider so it matches the visible mesh
            bc.size = mf2.mesh.bounds.size;
            bc.center = mf2.mesh.bounds.center;
            Debug.LogFormat("[CreateFloor] Mesh bounds size={0}, center={1}", mf2.mesh.bounds.size, mf2.mesh.bounds.center);
        }
        else
        {
            // Fallback: set collider to the forced large size
            bc.size = new Vector3(1000f, 0.1f, 1000f);
            bc.center = Vector3.zero;
            Debug.LogWarning("[CreateFloor] MeshFilter or mesh missing when sizing collider; using forced fallback size.");
        }

        // Ensure transform scale is identity so collider size maps correctly
        floor.transform.localScale = Vector3.one;

        // Mark static so Unity treats it as environment (optimization)
        floor.isStatic = true;
    }
    
    void SetupLighting()
    {
        Light existingLight = FindFirstObjectByType<Light>();
        if (existingLight != null) return;
        
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
    }
    
    Material CreateDefaultMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.8f, 0.3f, 0.3f); // Rouge-orangé
        return mat;
    }
    
    Material CreateFragmentMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.7f, 0.4f, 0.2f); // Orange
        mat.SetFloat("_Metallic", 0.2f);
        mat.SetFloat("_Glossiness", 0.5f);
        return mat;
    }
    
    Material CreateFloorMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.3f, 0.3f, 0.3f); // Gris
        mat.SetFloat("_Metallic", 0.1f);
        mat.SetFloat("_Glossiness", 0.3f);
        return mat;
    }
}
