using UnityEngine;

/// <summary>
/// Représente un fragment d'objet fracturé avec propriétés physiques
/// </summary>
public class RigidFragment : MonoBehaviour
{
    // Propriétés physiques
    public float mass = 1f;
    public Vector3 centerOfMass = Vector3.zero;
    public Vector3 inertia = Vector3.one; // Moment d'inertie (Ix, Iy, Iz)
    
    // Cinématique
    public Vector3 velocity = Vector3.zero;
    public Vector3 angularVelocity = Vector3.zero;
    [Header("Damping")]
    public float linearDamping = 0.1f;    // fraction per second
    public float angularDamping = 0.1f;   // fraction per second
    [Header("Angular Limits")]
    public float maxAngularSpeed = 20f; // max magnitude of angular velocity (rad/s)
    public float maxAngularDelta = 10f; // max angular velocity change produced by a single impulse (rad/s)
    [Header("Ground Contact")]
    public float restitution = 0.1f; // bounciness on contact
    public float friction = 0.4f;    // simple surface friction applied on contact

    // Cached floor collider for simple ground collision detection
    private BoxCollider cachedFloorCollider = null;
    
    // Matrice de transformation custom
    private Matrix4x4Custom transformMatrix = new Matrix4x4Custom();
    
    // Position et rotation actuelles
    private Vector3 currentPosition;
    private Vector3 currentRotation; // Euler angles en radians
    
    void Start()
    {
        currentPosition = transform.position;
        currentRotation = transform.eulerAngles * Mathf.Deg2Rad;
        
        // Calculer centre de masse basé sur le mesh
        CalculateCenterOfMass();
        
        // Calculer moment d'inertie
        CalculateInertia();
    }

    void FixedUpdate()
    {
        // Intégration manuelle (Euler explicite) with simple ground collision handling
        float dt = Time.fixedDeltaTime;

        // Predict next position
        Vector3 nextPosition = currentPosition + velocity * dt;

        // Ensure we have a cached reference to the floor collider
        if (cachedFloorCollider == null)
        {
            var floorObj = GameObject.Find("Floor");
            if (floorObj != null)
            {
                cachedFloorCollider = floorObj.GetComponent<BoxCollider>();
            }
        }

        // Ground collision test (simple): clamp y so fragment does not penetrate the floor
        if (cachedFloorCollider != null)
        {
            float floorTop = cachedFloorCollider.bounds.max.y;

            // Estimate half-height of this fragment in world units using mesh bounds
            float halfHeight = 0.5f;
            var mf = GetComponent<MeshFilter>();
            if (mf != null && mf.mesh != null)
            {
                halfHeight = mf.mesh.bounds.extents.y * Mathf.Abs(transform.localScale.y);
            }

            // If next position would be below the floor top, resolve contact
            if (nextPosition.y - halfHeight <= floorTop)
            {
                // Place fragment on top of floor
                nextPosition.y = floorTop + halfHeight + 0.001f;

                // Only modify vertical velocity if moving downwards
                if (velocity.y < 0f)
                {
                    velocity.y = -velocity.y * restitution; // bounce/dampen

                    // Apply simple friction to horizontal components
                    velocity.x *= (1f - Mathf.Clamp01(friction));
                    velocity.z *= (1f - Mathf.Clamp01(friction));
                }
            }
        }

        // Commit next position and rotation
        currentPosition = nextPosition;
        currentRotation += angularVelocity * dt;

        // Apply simple exponential damping to avoid runaway velocities
        if (linearDamping > 0f)
        {
            float linFactor = Mathf.Clamp01(1f - linearDamping * dt);
            velocity *= linFactor;
        }
        if (angularDamping > 0f)
        {
            float angFactor = Mathf.Clamp01(1f - angularDamping * dt);
            angularVelocity *= angFactor;
        }

        // Construire matrice de transformation
        UpdateTransformMatrix();

        // Appliquer à Unity (pour visualisation)
        ApplyTransformToUnity();
    }

    void UpdateTransformMatrix()
    {
        // Composition des matrices: T * Rz * Ry * Rx
        Matrix4x4Custom translation = Matrix4x4Custom.Translation(currentPosition);
        Matrix4x4Custom rotX = Matrix4x4Custom.RotationX(currentRotation.x);
        Matrix4x4Custom rotY = Matrix4x4Custom.RotationY(currentRotation.y);
        Matrix4x4Custom rotZ = Matrix4x4Custom.RotationZ(currentRotation.z);
        
        transformMatrix = translation * rotZ * rotY * rotX;
    }

    void ApplyTransformToUnity()
    {
        // Extraire position de la matrice
        transform.position = currentPosition;
        
        // Extraire rotation (conversion radians -> degrés)
        transform.eulerAngles = currentRotation * Mathf.Rad2Deg;
    }

    void CalculateCenterOfMass()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            Vector3[] vertices = meshFilter.mesh.vertices;
            Vector3 sum = Vector3.zero;
            
            foreach (Vector3 vertex in vertices)
            {
                sum += vertex;
            }
            
            centerOfMass = sum / vertices.Length;
        }
    }
    
    void CalculateInertia()
    {
        // Utiliser les dimensions du mesh pour calculer le moment d'inertie
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            Bounds bounds = meshFilter.mesh.bounds;
            Vector3 size = bounds.size;
            
            // I = (1/12) * m * (h² + d²) pour chaque axe
            inertia = MeshGenerator.CalculateCubeInertia(mass, size);

            // Ensure no zero components (defensive)
            inertia.x = Mathf.Max(inertia.x, 1e-6f);
            inertia.y = Mathf.Max(inertia.y, 1e-6f);
            inertia.z = Mathf.Max(inertia.z, 1e-6f);
        }
    }

    /// <summary>
    /// Appliquer une impulsion au fragment
    /// </summary>
    public void ApplyImpulse(Vector3 impulse, Vector3 worldPoint)
    {
        // Defensive: avoid zero mass
        float m = Mathf.Max(1e-6f, mass);

        // Δv = J / m
        velocity += impulse / m;

        // Calculer couple pour rotation
        Vector3 r = worldPoint - (currentPosition + centerOfMass);
        Vector3 torque = Vector3.Cross(r, impulse);

        // Compute Δω = I_world^{-1} * torque.
        // We have a diagonal inertia tensor in body space: I_body = diag(Ix, Iy, Iz).
        // I_world^{-1} = R * I_body^{-1} * R^T.
        // So Δω = R * (I_body^{-1} * (R^T * torque)).

        // Current rotation quaternion (convert currentRotation radians -> degrees for Quaternion.Euler)
        Quaternion rot = Quaternion.Euler(currentRotation * Mathf.Rad2Deg);

        // Torque in body-local coordinates
        Vector3 torqueLocal = Quaternion.Inverse(rot) * torque;

        // Body-space inertia (ensure non-zero) - already clamped in CalculateInertia()
        float Ix = Mathf.Max(1e-6f, inertia.x);
        float Iy = Mathf.Max(1e-6f, inertia.y);
        float Iz = Mathf.Max(1e-6f, inertia.z);

        Vector3 deltaOmegaLocal = new Vector3(torqueLocal.x / Ix, torqueLocal.y / Iy, torqueLocal.z / Iz);

        // Optionally clamp per-impulse angular delta to avoid huge jumps from single large torques
        if (deltaOmegaLocal.magnitude > maxAngularDelta)
        {
            deltaOmegaLocal = deltaOmegaLocal.normalized * maxAngularDelta;
        }

        // Transform back to world space
        Vector3 deltaOmegaWorld = rot * deltaOmegaLocal;

        angularVelocity += deltaOmegaWorld;

        // Clamp max angular speed to keep simulation stable
        if (angularVelocity.magnitude > maxAngularSpeed)
        {
            angularVelocity = angularVelocity.normalized * maxAngularSpeed;
        }

        // Debug: print resulting velocities to help verify impulses are applied
        Debug.LogFormat("[ApplyImpulse] {0} impulse={1} atPoint={2} => velocity={3}, Δω={4}, angularVelocity={5}",
            gameObject.name, impulse, worldPoint, velocity, deltaOmegaWorld, angularVelocity);
    }

    /// <summary>
    /// Obtenir la position mondiale d'un point local
    /// </summary>
    public Vector3 GetWorldPoint(Vector3 localPoint)
    {
        return transformMatrix.MultiplyPoint(localPoint);
    }

    void OnDrawGizmos()
    {
        // Visualiser le centre de masse
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentPosition + centerOfMass, 0.05f);
        }
    }

    /// <summary>
    /// Apply a positional correction to the fragment's internal position used by the integrator.
    /// This should be used instead of setting transform.position directly so the physics state
    /// remains coherent (currentPosition, transform and internal matrices).
    /// </summary>
    public void ApplyPositionCorrection(Vector3 delta)
    {
        currentPosition += delta;
        UpdateTransformMatrix();
        ApplyTransformToUnity();
    }
}
