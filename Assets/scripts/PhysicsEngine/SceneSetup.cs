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
    // Hide the main cube GameObject from the Hierarchy to keep the Editor clean.
    mainCube.hideFlags = HideFlags.HideInHierarchy;
        
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
            // Hide the floor GameObject from the Hierarchy so it doesn't clutter the Editor.
            floor.hideFlags = HideFlags.HideInHierarchy;
        }
        // Instead of using a Unity Collider, attach a FloorInfo component and compute world bounds from the mesh.
        var mf2 = floor.GetComponent<MeshFilter>();
        if (mf2 != null && mf2.mesh != null)
        {
            // Ensure transform scale is identity so mesh bounds map predictably
            floor.transform.localScale = Vector3.one;

            // Add or update FloorInfo component used by our custom physics
            var fi = floor.GetComponent<FloorInfo>();
            if (fi == null) fi = floor.AddComponent<FloorInfo>();
            fi.UpdateFromMesh(mf2);

            Debug.LogFormat("[CreateFloor] Mesh bounds size={0}, center={1}, worldTopY={2}", mf2.mesh.bounds.size, mf2.mesh.bounds.center, fi.TopY);
        }
        else
        {
            // Fallback: ensure transform scale is identity and still create FloorInfo with approximate values
            floor.transform.localScale = Vector3.one;
            var fi = floor.GetComponent<FloorInfo>();
            if (fi == null) fi = floor.AddComponent<FloorInfo>();
            fi.worldCenter = floor.transform.position;
            fi.worldSize = new Vector3(1000f, 0.1f, 1000f);
            Debug.LogWarning("[CreateFloor] MeshFilter or mesh missing when sizing floor info; using fallback size.");
        }

    // Mark static so Unity treats it as environment (optimization)
    floor.isStatic = true;
    // Ensure the floor remains hidden in the Hierarchy when using an existing object
    floor.hideFlags = HideFlags.HideInHierarchy;
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
        Shader s = FindUsableShader(new string[] { "Sprites/Default", "Unlit/Color", "Standard", "Universal Render Pipeline/Lit" });
        Material mat = new Material(s ?? Shader.Find("Standard"));
        if (mat.HasProperty("_Color")) mat.color = new Color(0.8f, 0.3f, 0.3f); // Rouge-orangé
        return mat;
    }
    
    Material CreateFragmentMaterial()
    {
        Shader s = FindUsableShader(new string[] { "Sprites/Default", "Unlit/Color", "Standard", "Universal Render Pipeline/Lit" });
        Material mat = new Material(s ?? Shader.Find("Standard"));
        if (mat.HasProperty("_Color")) mat.color = new Color(0.7f, 0.4f, 0.2f); // Orange
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.2f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.5f);
        return mat;
    }
    
    Material CreateFloorMaterial()
    {
        Shader s = FindUsableShader(new string[] { "Sprites/Default", "Unlit/Color", "Standard", "Universal Render Pipeline/Lit" });
        Material mat = new Material(s ?? Shader.Find("Standard"));
        if (mat.HasProperty("_Color")) mat.color = new Color(0.3f, 0.3f, 0.3f); // Gris
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.1f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.3f);
        return mat;
    }

    Shader FindUsableShader(string[] candidates)
    {
        foreach (var name in candidates)
        {
            var sh = Shader.Find(name);
            if (sh != null) return sh;
        }
        return null;
    }
}
