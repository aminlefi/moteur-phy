using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Système de pré-fracture - découpe un cube en fragments
/// </summary>
public class FractureSystem : MonoBehaviour
{
    [Header("Fracture Parameters")]
    public int fracturesX = 2;
    public int fracturesY = 2;
    public int fracturesZ = 2;
    
    [Header("Fragment Properties")]
    public float fragmentMass = 1f;
    public Material fragmentMaterial;
    
    [Header("Constraint Properties")]
    public float constraintStiffness = 1000f;
    public float breakThreshold = 0.5f;
    
    [Header("External Force")]
    public bool applyForceOnStart = false;
    public Vector3 forceDirection = Vector3.right;
    public float forceMagnitude = 10f;
    [Header("Ground / Break Settings")]
    public bool requireGroundContact = true;
    public float groundY = 0f;
    public float groundContactThreshold = 0.05f;
    
    [Header("Collision Tuning")]
    public float collisionPositionalCorrection = 0.25f; // fraction of penetration to correct immediately
    public float collisionPenetrationSlack = 0.01f; // small tolerance before correction
    public int collisionIgnoreFramesAfterBreak = 8; // frames to ignore collisions between freshly-broken fragments

    private List<RigidFragment> fragments = new List<RigidFragment>();
    private List<SpringConstraint> constraints = new List<SpringConstraint>();
    // Track recently broken pairs (keyed by "idA_idB") to temporarily ignore collisions
    private System.Collections.Generic.Dictionary<string, int> recentBrokenPairs = new System.Collections.Generic.Dictionary<string, int>();

    // Helper to create a stable key for an unordered fragment pair
    private string GetPairKey(RigidFragment a, RigidFragment b)
    {
        int idA = a != null ? a.GetInstanceID() : 0;
        int idB = b != null ? b.GetInstanceID() : 0;
        if (idA < idB) return idA + "_" + idB;
        return idB + "_" + idA;
    }
    
    void Start()
    {
        // Obtenir dimensions du cube original
        Vector3 size = transform.localScale;
        
        // Créer les fragments
        CreateFragments(size);
        
        // Créer les contraintes entre fragments adjacents
        CreateConstraints();
        
        // Désactiver le mesh original
        GetComponent<MeshRenderer>().enabled = false;
        
        if (applyForceOnStart)
        {
            Invoke("ApplyExternalForce", 0.5f);
        }
    }
    
    void CreateFragments(Vector3 totalSize)
    {
        Vector3 fragmentSize = new Vector3(
            totalSize.x / fracturesX,
            totalSize.y / fracturesY,
            totalSize.z / fracturesZ
        );
        
        Vector3 startPos = transform.position - totalSize / 2f + fragmentSize / 2f;
        
        for (int x = 0; x < fracturesX; x++)
        {
            for (int y = 0; y < fracturesY; y++)
            {
                for (int z = 0; z < fracturesZ; z++)
                {
                    Vector3 position = startPos + new Vector3(
                        x * fragmentSize.x,
                        y * fragmentSize.y,
                        z * fragmentSize.z
                    );
                    
                    GameObject fragmentObj = CreateFragmentCube(position, fragmentSize);
                    fragmentObj.name = $"Fragment_{x}_{y}_{z}";
                    // Detach fragments from the original cube so disabling or hiding the original
                    // mesh does not hide the fragments. Parent at root (null).
                    fragmentObj.transform.parent = null;
                    
                    RigidFragment fragment = fragmentObj.AddComponent<RigidFragment>();
                    fragment.mass = fragmentMass;
                    
                    fragments.Add(fragment);
                }
            }
        }
        
        Debug.Log($"Created {fragments.Count} fragments");
    }
    
    GameObject CreateFragmentCube(Vector3 position, Vector3 size)
    {
        // Utiliser notre générateur de mesh CUSTOM (pas de primitives Unity!)
        Vector3 adjustedSize = size * 0.95f; // Légèrement plus petit pour voir les gaps
        
    GameObject cube = MeshGenerator.CreateCubeGameObject(position, adjustedSize, fragmentMaterial);
    // Hide runtime fragment GameObjects from the Hierarchy so they don't clutter the Editor view
    // They remain active and the components still run, but will not appear in the Hierarchy window.
    cube.hideFlags = HideFlags.HideInHierarchy;
        // Ensure renderer has a valid material. If the provided fragmentMaterial is null or uses an incompatible shader
        // the mesh can appear magenta. Assign a safe fallback material if needed.
        var mr = cube.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            // Prefer explicit fragmentMaterial if provided and its shader appears available.
            if (fragmentMaterial != null && fragmentMaterial.shader != null && Shader.Find(fragmentMaterial.shader.name) != null)
            {
                mr.material = fragmentMaterial;
            }
            else
            {
                // Robust fallback order: common safe shaders
                string[] candidates = new string[] {
                    "Sprites/Default",
                    "Unlit/Color",
                    "Universal Render Pipeline/Unlit",
                    "Universal Render Pipeline/Lit",
                    "HDRP/Lit",
                    "Standard",
                    "Legacy Shaders/Diffuse"
                };

                Shader chosen = null;
                foreach (var name in candidates)
                {
                    chosen = Shader.Find(name);
                    if (chosen != null) break;
                }

                if (chosen != null)
                {
                    Material fallback = new Material(chosen);
                    if (fallback.HasProperty("_Color")) fallback.color = Color.grey;
                    mr.material = fallback;
                }
                else
                {
                    // Final fallback: create a default material without a shader (rare)
                    mr.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
                    if (mr.material != null && mr.material.HasProperty("_Color")) mr.material.color = Color.grey;
                    Debug.LogWarning("No suitable shader found for fragment material; fragments may render magenta. If you're using URP/HDRP ensure the correct pipeline shaders are available.");
                }
            }

            mr.enabled = true;
        }

        cube.SetActive(true);
        return cube;
    }
    
    void CreateConstraints()
    {
        // Créer contraintes entre fragments adjacents
        for (int i = 0; i < fragments.Count; i++)
        {
            for (int j = i + 1; j < fragments.Count; j++)
            {
                float distance = Vector3.Distance(
                    fragments[i].transform.position,
                    fragments[j].transform.position
                );
                
                // Vérifier si fragments sont adjacents (distance ~ taille fragment)
                float maxDistance = transform.localScale.x / fracturesX * 1.5f;
                
                if (distance < maxDistance)
                {
                    // Points de contact au centre de chaque fragment
                    Vector3 localA = Vector3.zero;
                    Vector3 localB = Vector3.zero;
                    
                    SpringConstraint constraint = new SpringConstraint(
                        fragments[i], 
                        fragments[j],
                        localA,
                        localB
                    );
                    
                    constraint.stiffness = constraintStiffness;
                    constraint.breakThreshold = breakThreshold;
                    
                    constraints.Add(constraint);
                }
            }
        }

        Debug.Log($"Created {constraints.Count} constraints");
    }
    
    void FixedUpdate()
    {
        // Vérifier rupture des contraintes but only apply impulses once per fragment and optionally only when
        // fragments are in contact with the ground.
    var toRemove = new List<SpringConstraint>();
    // Accumulate impulses and weighted application points per fragment so we can apply
    // the net impulse at a meaningful contact location (helps fragments separate).
    var pendingImpulses = new System.Collections.Generic.Dictionary<RigidFragment, Vector3>();
    var pendingPointWeightedSum = new System.Collections.Generic.Dictionary<RigidFragment, Vector3>();
    var pendingPointWeight = new System.Collections.Generic.Dictionary<RigidFragment, float>();

        for (int i = 0; i < constraints.Count; i++)
        {
            var c = constraints[i];
            if (c == null) continue;

            if (c.isBroken)
            {
                toRemove.Add(c);
                continue;
            }

            if (!c.ShouldBreak()) continue;

            // If required, only break constraints when at least one attached fragment is touching the ground
            bool allowBreak = true;
            if (requireGroundContact)
            {
                // Use the actual floor top from FloorInfo if available, otherwise fall back to groundY
                float floorTop = groundY;
                GameObject floorObj = GameObject.Find("Floor");
                if (floorObj != null)
                {
                    var fi = floorObj.GetComponent<FloorInfo>();
                    if (fi != null) floorTop = fi.TopY;
                }

                // Compute fragment bottom (center.y - halfHeight) using mesh bounds where possible
                Vector3 aPos = c.fragmentA.transform.position;
                Vector3 bPos = c.fragmentB.transform.position;

                float aHalf = 0.5f;
                var mfA = c.fragmentA.GetComponent<MeshFilter>();
                if (mfA != null && mfA.mesh != null)
                    aHalf = mfA.mesh.bounds.extents.y * Mathf.Abs(c.fragmentA.transform.localScale.y);
                float bHalf = 0.5f;
                var mfB = c.fragmentB.GetComponent<MeshFilter>();
                if (mfB != null && mfB.mesh != null)
                    bHalf = mfB.mesh.bounds.extents.y * Mathf.Abs(c.fragmentB.transform.localScale.y);

                bool aOnGround = (aPos.y - aHalf) <= floorTop + groundContactThreshold;
                bool bOnGround = (bPos.y - bHalf) <= floorTop + groundContactThreshold;
                allowBreak = aOnGround || bOnGround;
            }

            if (!allowBreak) continue;

            // Compute impulses but do not apply them yet; accumulate per fragment

            Vector3 worldA, worldB, impA, impB;
            c.ComputeBreakImpulses(out impA, out impB, out worldA, out worldB);

            // Accumulate impulse vector
            if (!pendingImpulses.ContainsKey(c.fragmentA)) pendingImpulses[c.fragmentA] = Vector3.zero;
            if (!pendingImpulses.ContainsKey(c.fragmentB)) pendingImpulses[c.fragmentB] = Vector3.zero;
            pendingImpulses[c.fragmentA] += impA;
            pendingImpulses[c.fragmentB] += impB;

            // Accumulate weighted application points (weight by impulse magnitude)
            float wA = impA.magnitude;
            float wB = impB.magnitude;
            if (!pendingPointWeightedSum.ContainsKey(c.fragmentA)) pendingPointWeightedSum[c.fragmentA] = Vector3.zero;
            if (!pendingPointWeightedSum.ContainsKey(c.fragmentB)) pendingPointWeightedSum[c.fragmentB] = Vector3.zero;
            if (!pendingPointWeight.ContainsKey(c.fragmentA)) pendingPointWeight[c.fragmentA] = 0f;
            if (!pendingPointWeight.ContainsKey(c.fragmentB)) pendingPointWeight[c.fragmentB] = 0f;

            pendingPointWeightedSum[c.fragmentA] += worldA * wA;
            pendingPointWeightedSum[c.fragmentB] += worldB * wB;
            pendingPointWeight[c.fragmentA] += wA;
            pendingPointWeight[c.fragmentB] += wB;

            toRemove.Add(c);
        }

        // Apply accumulated impulses once per fragment, at a weighted contact point if available
        foreach (var kv in pendingImpulses)
        {
            var frag = kv.Key;
            var totalImpulse = kv.Value;

            // Compute weighted application point from accumulated contributions
            Vector3 applicationPoint = frag.transform.position;
            if (pendingPointWeight.ContainsKey(frag) && pendingPointWeight[frag] > 1e-6f)
            {
                applicationPoint = pendingPointWeightedSum[frag] / pendingPointWeight[frag];
            }

            // Add a small randomized perturbation to the impulse vector so fragments don't move perfectly symmetrically
            Vector3 randomPerturb = Random.onUnitSphere * totalImpulse.magnitude * 0.02f;

            // Apply the impulse
            frag.ApplyImpulse(totalImpulse + randomPerturb, applicationPoint);

            // Immediately nudge fragment position a small amount away from the contact point so it starts separated
            // Compute direction from application point toward fragment center; if zero fallback to random
            Vector3 sepDir = frag.transform.position - applicationPoint;
            if (sepDir.sqrMagnitude < 1e-6f) sepDir = Random.onUnitSphere;
            sepDir.Normalize();
            // Separation magnitude scales with impulse magnitude and fragment mass but clamped to avoid popping
            float sepMag = Mathf.Clamp(0.02f * (totalImpulse.magnitude / (frag.mass + 1f)), 0.01f, 0.25f);
            frag.ApplyPositionCorrection(sepDir * sepMag);

            Debug.LogFormat("[FractureSystem] Applied accumulated impulse {0} (+perturb {1}) atPoint={2} to {3} and nudged by {4}", totalImpulse, randomPerturb, applicationPoint, frag.name, sepDir * sepMag);
        }

        // Remove broken constraints from active list
        if (toRemove.Count > 0)
        {
            for (int i = 0; i < toRemove.Count; i++)
            {
                constraints.Remove(toRemove[i]);
            }
        }

        // Appliquer gravité à tous les fragments
        foreach (var fragment in fragments)
        {
            fragment.velocity += Physics.gravity * Time.fixedDeltaTime;
        }

        // Resolve simple pairwise collisions between fragments so they interact naturally
        ResolveFragmentCollisions();
    }

    /// <summary>
    /// Simple sphere-based collision detection & resolution between fragments.
    /// Uses ApplyImpulse(...) on fragments to produce collision response.
    /// </summary>
    void ResolveFragmentCollisions()
    {
        int n = fragments.Count;
        if (n < 2) return;

    // Parameters (from public tuning fields)
    float penetrationSlack = collisionPenetrationSlack;
    float positionalCorrection = collisionPositionalCorrection; // fraction of penetration to correct immediately

        for (int i = 0; i < n; i++)
        {
            var A = fragments[i];
            if (A == null) continue;

            var mfA = A.GetComponent<MeshFilter>();
            float rA = 0.5f;
            if (mfA != null && mfA.mesh != null)
            {
                var ext = mfA.mesh.bounds.extents;
                Vector3 sc = A.transform.localScale;
                rA = Mathf.Max(ext.x * Mathf.Abs(sc.x), ext.y * Mathf.Abs(sc.y), ext.z * Mathf.Abs(sc.z));
            }

            Vector3 posA = A.transform.position;

            for (int j = i + 1; j < n; j++)
            {
                var B = fragments[j];
                if (B == null) continue;

                var mfB = B.GetComponent<MeshFilter>();
                float rB = 0.5f;
                if (mfB != null && mfB.mesh != null)
                {
                    var extB = mfB.mesh.bounds.extents;
                    Vector3 scB = B.transform.localScale;
                    rB = Mathf.Max(extB.x * Mathf.Abs(scB.x), extB.y * Mathf.Abs(scB.y), extB.z * Mathf.Abs(scB.z));
                }

                Vector3 posB = B.transform.position;
                Vector3 nAB = posB - posA;
                float dist = nAB.magnitude;
                float rSum = rA + rB;
                if (dist < Mathf.Epsilon) nAB = Vector3.up; else nAB /= dist;

                float penetration = rSum - dist;
                if (penetration > -penetrationSlack)
                {
                    // If these two fragments were just broken apart, skip collision resolution for a few frames
                    string pk = GetPairKey(A, B);
                    if (recentBrokenPairs.ContainsKey(pk) && Time.frameCount - recentBrokenPairs[pk] <= collisionIgnoreFramesAfterBreak)
                    {
                        continue;
                    }
                    // positional correction (split between A and B)
                    float corr = positionalCorrection * Mathf.Max(penetration - penetrationSlack, 0f);
                    Vector3 correction = nAB * corr;
                    // move objects out of penetration by adjusting their internal integrator positions
                    A.ApplyPositionCorrection(-correction * 0.5f);
                    B.ApplyPositionCorrection(correction * 0.5f);

                    // Relative velocity
                    Vector3 rv = B.velocity - A.velocity;
                    float velAlongNormal = Vector3.Dot(rv, nAB);
                    if (velAlongNormal > 0f)
                    {
                        // they are separating already
                        continue;
                    }

                    // Restitution (use min)
                    float e = Mathf.Min(A.restitution, B.restitution);

                    // Compute impulse scalar
                    float invMassA = 1f / Mathf.Max(1e-6f, A.mass);
                    float invMassB = 1f / Mathf.Max(1e-6f, B.mass);
                    float impulseScalar = -(1f + e) * velAlongNormal / (invMassA + invMassB);

                    Vector3 impulse = impulseScalar * nAB;

                    // Apply impulses at contact points (approx midpoint)
                    Vector3 contactPoint = posA + nAB * (rA - 0.5f * penetration);
                    A.ApplyImpulse(-impulse, contactPoint);
                    B.ApplyImpulse(impulse, contactPoint);

                    // Simple tangential (friction) impulse: reduce sliding along tangent
                    Vector3 tangential = rv - velAlongNormal * nAB;
                    if (tangential.sqrMagnitude > 1e-6f)
                    {
                        Vector3 tangentDir = tangential.normalized;
                        float invMassSum = invMassA + invMassB;
                        // jt = -v_rel_tangent / (invMassSum)
                        float jt = -Vector3.Dot(rv, tangentDir) / Mathf.Max(1e-6f, invMassSum);
                        // clamp tangential impulse by a fraction of normal impulse (Coulomb-like)
                        float maxT = Mathf.Abs(impulseScalar) * 0.5f;
                        float jtClamped = Mathf.Clamp(jt, -maxT, maxT);
                        Vector3 tangentialImpulse = jtClamped * tangentDir;

                        A.ApplyImpulse(-tangentialImpulse, contactPoint);
                        B.ApplyImpulse(tangentialImpulse, contactPoint);
                    }
                }
            }
        }
    }
    
    void ApplyExternalForce()
    {
        // Appliquer une force au fragment central
        int centralIndex = fragments.Count / 2;
        if (centralIndex < fragments.Count)
        {
            Vector3 impulse = forceDirection.normalized * forceMagnitude;
            fragments[centralIndex].ApplyImpulse(impulse, fragments[centralIndex].transform.position);
            
            Debug.Log($"Applied impulse {impulse} to central fragment");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Dessiner toutes les contraintes
        foreach (var constraint in constraints)
        {
            constraint.DrawGizmo();
        }
    }
}
