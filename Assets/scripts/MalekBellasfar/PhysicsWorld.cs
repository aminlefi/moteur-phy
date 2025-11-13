using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main physics simulation world managing rigid bodies and constraints.
/// Implements custom physics without using Unity's built-in physics engine.
/// </summary>
public class PhysicsWorld : MonoBehaviour
{
    [Header("Simulation Settings")]
    [Tooltip("Number of constraint solver iterations per frame")]
    public int solverIterations = 10;
    
    [Tooltip("Energy transfer ratio α for fracture (0-1, can be >1 for artistic effect)")]
    [Range(0f, 2f)]
    public float energyTransferAlpha = 1f;
    
    [Tooltip("Global gravity force")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);

    [Header("Ground Plane")]
    [Tooltip("Enable ground plane collision")]
    public bool enableGroundPlane = true;
    
    [Tooltip("Ground plane height")]
    public float groundHeight = 0f;
    
    [Tooltip("Ground restitution (bounciness)")]
    [Range(0f, 1f)]
    public float groundRestitution = 0.3f;
    
    [Tooltip("Ground friction")]
    [Range(0f, 1f)]
    public float groundFriction = 0.5f;

    [Header("Debug")]
    public bool showConstraints = true;
    public bool showEnergyInfo = true;
    public bool showGroundPlane = true;

    // Physics objects
    private List<RigidBody> rigidBodies = new List<RigidBody>();
    private List<Constraint> constraints = new List<Constraint>();
    private GroundPlane groundPlane;

    // Energy tracking
    private float totalKineticEnergy;
    private float totalPotentialEnergy;
    private float totalStoredEnergy;

    void Start()
    {
        // Initialize ground plane
        if (enableGroundPlane)
        {
            groundPlane = new GroundPlane(Vector3.up, groundHeight);
            groundPlane.restitution = groundRestitution;
            groundPlane.friction = groundFriction;
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Step 1: Apply external forces (gravity)
        ApplyExternalForces(dt);

        // Step 2: Solve constraints (iterative)
        SolveConstraints(dt);

        // Step 3: Check for fractures and apply fracture impulses
        ProcessFractures();

        // Step 4: Integrate motion
        IntegrateMotion(dt);

        // Step 5: Apply ground plane collisions
        ApplyGroundCollisions();

        // Step 6: Update energy tracking
        UpdateEnergyTracking();
    }

    /// <summary>
    /// Apply external forces like gravity to all bodies.
    /// </summary>
    void ApplyExternalForces(float dt)
    {
        foreach (var body in rigidBodies)
        {
            if (body != null)
            {
                body.velocity += gravity * dt;
            }
        }
    }

    /// <summary>
    /// Iteratively solve all active constraints.
    /// </summary>
    void SolveConstraints(float dt)
    {
        for (int iter = 0; iter < solverIterations; iter++)
        {
            foreach (var constraint in constraints)
            {
                if (constraint != null && !constraint.isBroken)
                {
                    constraint.Solve(dt);
                }
            }
        }
    }

    /// <summary>
    /// Check constraints for fracture and apply energy-based impulses.
    /// </summary>
    void ProcessFractures()
    {
        List<Constraint> toRemove = new List<Constraint>();

        foreach (var constraint in constraints)
        {
            if (constraint == null || constraint.isBroken) continue;

            if (constraint.ShouldBreak())
            {
                constraint.ApplyFractureImpulse(energyTransferAlpha);
                toRemove.Add(constraint);
            }
        }

        // Remove broken constraints
        foreach (var broken in toRemove)
        {
            constraints.Remove(broken);
        }
    }

    /// <summary>
    /// Integrate rigid body motion using Newton-Euler equations.
    /// </summary>
    void IntegrateMotion(float dt)
    {
        foreach (var body in rigidBodies)
        {
            if (body != null)
            {
                // No additional forces here (already applied in ApplyExternalForces)
                body.Integrate(Vector3.zero, Vector3.zero, dt);
            }
        }
    }

    /// <summary>
    /// Apply ground plane collisions to all rigid bodies.
    /// </summary>
    void ApplyGroundCollisions()
    {
        if (!enableGroundPlane || groundPlane == null) return;

        foreach (var body in rigidBodies)
        {
            if (body != null)
            {
                // Estimate half-extent (rough approximation)
                float halfExtent = 0.25f; // Could be improved with actual bounds
                groundPlane.ApplyCollision(body, halfExtent);
            }
        }
    }

    /// <summary>
    /// Calculate total energy in the system for debugging.
    /// </summary>
    void UpdateEnergyTracking()
    {
        totalKineticEnergy = 0f;
        totalPotentialEnergy = 0f;
        totalStoredEnergy = 0f;

        foreach (var body in rigidBodies)
        {
            if (body == null) continue;

            // Linear kinetic energy: 0.5 * m * v^2
            totalKineticEnergy += 0.5f * body.mass * body.velocity.sqrMagnitude;

            // Rotational kinetic energy: 0.5 * ω^T * I * ω
            Vector3 omegaLocal = Quaternion.Inverse(body.rotation) * body.angularVelocity;
            float rotKE = 0.5f * (
                body.inertiaTensor.x * omegaLocal.x * omegaLocal.x +
                body.inertiaTensor.y * omegaLocal.y * omegaLocal.y +
                body.inertiaTensor.z * omegaLocal.z * omegaLocal.z
            );
            totalKineticEnergy += rotKE;

            // Gravitational potential energy: m * g * h
            totalPotentialEnergy += body.mass * Mathf.Abs(gravity.y) * body.position.y;
        }

        foreach (var constraint in constraints)
        {
            if (constraint != null && !constraint.isBroken)
            {
                totalStoredEnergy += constraint.storedEnergy;
            }
        }
    }

    /// <summary>
    /// Add a rigid body to the simulation.
    /// </summary>
    public void AddRigidBody(RigidBody body)
    {
        if (!rigidBodies.Contains(body))
        {
            rigidBodies.Add(body);
        }
    }

    /// <summary>
    /// Add a constraint to the simulation.
    /// </summary>
    public void AddConstraint(Constraint constraint)
    {
        if (!constraints.Contains(constraint))
        {
            constraints.Add(constraint);
        }
    }

    /// <summary>
    /// Get all rigid bodies (read-only).
    /// </summary>
    public IReadOnlyList<RigidBody> GetRigidBodies() => rigidBodies.AsReadOnly();

    /// <summary>
    /// Get all constraints (read-only).
    /// </summary>
    public IReadOnlyList<Constraint> GetConstraints() => constraints.AsReadOnly();

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw constraints
        if (showConstraints)
        {
            foreach (var constraint in constraints)
            {
                if (constraint != null)
                {
                    constraint.DrawGizmo();
                }
            }
        }

        // Draw ground plane
        if (showGroundPlane && enableGroundPlane && groundPlane != null)
        {
            groundPlane.DrawGizmo(20f);
        }
    }

    void OnGUI()
    {
        if (!showEnergyInfo) return;

        int activeConstraints = 0;
        foreach (var c in constraints)
        {
            if (c != null && !c.isBroken) activeConstraints++;
        }

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== PHYSICS ENGINE DEBUG ===");
        GUILayout.Label($"Rigid Bodies: {rigidBodies.Count}");
        GUILayout.Label($"Active Constraints: {activeConstraints} / {constraints.Count}");
        GUILayout.Label($"Kinetic Energy: {totalKineticEnergy:F2} J");
        GUILayout.Label($"Potential Energy: {totalPotentialEnergy:F2} J");
        GUILayout.Label($"Stored Energy: {totalStoredEnergy:F2} J");
        GUILayout.Label($"Total Energy: {(totalKineticEnergy + totalPotentialEnergy + totalStoredEnergy):F2} J");
        GUILayout.EndArea();
    }
}

