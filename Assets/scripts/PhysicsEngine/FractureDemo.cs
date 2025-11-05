using UnityEngine;

/// <summary>
/// Script de démonstration pour tester le système de fracture
/// Utilise les touches du clavier pour interactions
/// </summary>
public class FractureDemo : MonoBehaviour
{
    private FractureSystem fractureSystem;
    
    [Header("UI Info")]
    public bool showDebugInfo = true;
    
    void Start()
    {
        fractureSystem = GetComponent<FractureSystem>();
    }
    
    void Update()
    {
        // Touche ESPACE: Appliquer une impulsion aléatoire
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ApplyRandomImpulse();
        }
        
        // Touche R: Restart la scène
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }
    
    void ApplyRandomImpulse()
    {
        RigidFragment[] fragments = FindObjectsByType<RigidFragment>(FindObjectsSortMode.None);
        
        if (fragments.Length > 0)
        {
            // Choisir un fragment aléatoire
            RigidFragment randomFragment = fragments[Random.Range(0, fragments.Length)];
            
            // Direction aléatoire
            Vector3 randomDir = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;
            
            // Magnitude aléatoire
            float magnitude = Random.Range(5f, 15f);
            
            Vector3 impulse = randomDir * magnitude;
            randomFragment.ApplyImpulse(impulse, randomFragment.transform.position);
            
            Debug.Log($"Applied random impulse: {impulse} to {randomFragment.name}");
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.Label("=== FRACTURE PHYSICS DEMO ===");
        GUILayout.Label("ESPACE: Appliquer impulsion aléatoire");
        GUILayout.Label("R: Restart");
        GUILayout.Label("");
        
        int fragmentCount = FindObjectsByType<RigidFragment>(FindObjectsSortMode.None).Length;
        GUILayout.Label($"Fragments: {fragmentCount}");
        
        FractureSystem[] systems = FindObjectsByType<FractureSystem>(FindObjectsSortMode.None);
        if (systems.Length > 0)
        {
            var allConstraints = systems[0]
                .GetType()
                .GetField("constraints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(systems[0]) as System.Collections.Generic.List<SpringConstraint>;
            
            if (allConstraints != null)
            {
                int brokenCount = 0;
                foreach (var c in allConstraints)
                {
                    if (c.isBroken) brokenCount++;
                }
                GUILayout.Label($"Contraintes: {allConstraints.Count - brokenCount}/{allConstraints.Count}");
            }
        }
        
        GUILayout.EndArea();
    }
}
