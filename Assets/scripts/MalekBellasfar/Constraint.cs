using UnityEngine;

/// <summary>
/// Distance constraint between two rigid bodies with spring-damper model.
/// Implements paper equations for compliance Σ, error reduction Γ, and energy tracking.
/// </summary>
public class Constraint
{
    public RigidBody bodyA;
    public RigidBody bodyB;
    public Vector3 localAnchorA; // Attachment point in body A's local space
    public Vector3 localAnchorB; // Attachment point in body B's local space
    public float restDistance;   // Target distance

    // XPBD/Spring-Damper parameters (from paper)
    public float compliance = 0.0f;     // Σ (softness, 0 = rigid)
    public float gamma = 0.8f;          // Γ (error reduction, 0-1)
    public float breakThreshold = 10f;  // ε: breaks if |λ| > threshold

    // Solver state
    public float lambda = 0f;           // Accumulated constraint impulse magnitude
    public float storedEnergy = 0f;     // Potential energy E = 0.5 * φ^T * K * φ

    // Fracture state
    public bool isBroken = false;
    public Vector3 fractureImpulseA = Vector3.zero;
    public Vector3 fractureImpulseB = Vector3.zero;
    public Vector3 fracturePointA = Vector3.zero;
    public Vector3 fracturePointB = Vector3.zero;

    public Constraint(RigidBody a, RigidBody b, Vector3 anchorA, Vector3 anchorB)
    {
        bodyA = a;
        bodyB = b;
        localAnchorA = anchorA;
        localAnchorB = anchorB;

        // Initialize rest distance to current distance
        Vector3 worldA = a.LocalToWorld(anchorA);
        Vector3 worldB = b.LocalToWorld(anchorB);
        restDistance = Vector3.Distance(worldA, worldB);
    }

    /// <summary>
    /// Compute constraint violation φ (signed distance error).
    /// Returns: current_distance - rest_distance
    /// </summary>
    public float GetViolation(out Vector3 direction)
    {
        Vector3 worldA = bodyA.LocalToWorld(localAnchorA);
        Vector3 worldB = bodyB.LocalToWorld(localAnchorB);

        Vector3 diff = worldB - worldA;
        float currentDist = diff.magnitude;

        direction = currentDist > 1e-8f ? diff / currentDist : Vector3.up;
        return currentDist - restDistance;
    }

    /// <summary>
    /// Compute effective mass for constraint including rotational terms.
    /// m_eff = 1 / (1/mA + 1/mB + (rA×n)·I_A^-1·(rA×n) + (rB×n)·I_B^-1·(rB×n))
    /// </summary>
    public float ComputeEffectiveMass(Vector3 n, out Vector3 rA, out Vector3 rB)
    {
        Vector3 worldA = bodyA.LocalToWorld(localAnchorA);
        Vector3 worldB = bodyB.LocalToWorld(localAnchorB);

        rA = worldA - (bodyA.position + bodyA.rotation * bodyA.centerOfMass);
        rB = worldB - (bodyB.position + bodyB.rotation * bodyB.centerOfMass);

        // Linear contribution
        float invMassSum = bodyA.invMass + bodyB.invMass;

        // Angular contribution
        Vector3 raXn = Vector3.Cross(rA, n);
        Vector3 rbXn = Vector3.Cross(rB, n);

        // Transform to body space, apply inverse inertia, transform back
        Vector3 raXnLocalA = Quaternion.Inverse(bodyA.rotation) * raXn;
        Vector3 rbXnLocalB = Quaternion.Inverse(bodyB.rotation) * rbXn;

        Vector3 invIA = new Vector3(
            1f / Mathf.Max(1e-6f, bodyA.inertiaTensor.x),
            1f / Mathf.Max(1e-6f, bodyA.inertiaTensor.y),
            1f / Mathf.Max(1e-6f, bodyA.inertiaTensor.z)
        );
        Vector3 invIB = new Vector3(
            1f / Mathf.Max(1e-6f, bodyB.inertiaTensor.x),
            1f / Mathf.Max(1e-6f, bodyB.inertiaTensor.y),
            1f / Mathf.Max(1e-6f, bodyB.inertiaTensor.z)
        );

        Vector3 angContribA = new Vector3(
            raXnLocalA.x * invIA.x,
            raXnLocalA.y * invIA.y,
            raXnLocalA.z * invIA.z
        );
        Vector3 angContribB = new Vector3(
            rbXnLocalB.x * invIB.x,
            rbXnLocalB.y * invIB.y,
            rbXnLocalB.z * invIB.z
        );

        angContribA = bodyA.rotation * angContribA;
        angContribB = bodyB.rotation * angContribB;

        float angularTerm = Vector3.Dot(raXn, angContribA) + Vector3.Dot(rbXn, angContribB);

        float wTotal = invMassSum + angularTerm;
        return wTotal > 1e-9f ? 1f / wTotal : 0f;
    }

    /// <summary>
    /// Solve constraint using Jacobian formulation with compliance and gamma.
    /// Updates velocities of both bodies and accumulates lambda.
    /// </summary>
    public void Solve(float dt)
    {
        if (isBroken) return;

        Vector3 n;
        float phi = GetViolation(out n);

        Vector3 rA, rB;
        float mEff = ComputeEffectiveMass(n, out rA, out rB);
        if (mEff < 1e-9f) return;

        // XPBD formulation with compliance and gamma
        // Δλ = -(φ + α*λ_prev + (γ/h)*φ) / (1/m_eff + α + γ/h)
        // where α = Σ / h²
        float alpha = compliance > 0f ? compliance / (dt * dt) : 0f;
        float gammaOverH = gamma > 0f ? gamma / Mathf.Max(1e-6f, dt) : 0f;

        float denom = 1f / mEff + alpha + gammaOverH;
        if (denom < 1e-9f) return;

        float deltaLambda = -(phi * (1f + gammaOverH) + alpha * lambda) / denom;
        lambda += deltaLambda;

        // Apply constraint impulse
        Vector3 impulse = n * deltaLambda;

        // Linear velocity correction
        bodyA.velocity -= impulse * bodyA.invMass;
        bodyB.velocity += impulse * bodyB.invMass;

        // Angular velocity correction
        Vector3 angularImpulseA = Vector3.Cross(rA, impulse);
        Vector3 angularImpulseB = Vector3.Cross(rB, -impulse);

        Vector3 angImpLocalA = Quaternion.Inverse(bodyA.rotation) * angularImpulseA;
        Vector3 angImpLocalB = Quaternion.Inverse(bodyB.rotation) * angularImpulseB;

        Vector3 invIA = new Vector3(
            1f / Mathf.Max(1e-6f, bodyA.inertiaTensor.x),
            1f / Mathf.Max(1e-6f, bodyA.inertiaTensor.y),
            1f / Mathf.Max(1e-6f, bodyA.inertiaTensor.z)
        );
        Vector3 invIB = new Vector3(
            1f / Mathf.Max(1e-6f, bodyB.inertiaTensor.x),
            1f / Mathf.Max(1e-6f, bodyB.inertiaTensor.y),
            1f / Mathf.Max(1e-6f, bodyB.inertiaTensor.z)
        );

        Vector3 deltaOmegaLocalA = new Vector3(
            angImpLocalA.x * invIA.x,
            angImpLocalA.y * invIA.y,
            angImpLocalA.z * invIA.z
        );
        Vector3 deltaOmegaLocalB = new Vector3(
            angImpLocalB.x * invIB.x,
            angImpLocalB.y * invIB.y,
            angImpLocalB.z * invIB.z
        );

        bodyA.angularVelocity += bodyA.rotation * deltaOmegaLocalA;
        bodyB.angularVelocity += bodyB.rotation * deltaOmegaLocalB;

        // Compute stored energy: E = 0.5 * φ^T * K * φ
        // K = γ / (h * Σ) (spring stiffness from paper)
        float K = compliance > 1e-9f ? gamma / (dt * compliance) : 1000f;
        storedEnergy = 0.5f * K * phi * phi;
    }

    /// <summary>
    /// Check if constraint should break based on lambda threshold.
    /// </summary>
    public bool ShouldBreak()
    {
        return Mathf.Abs(lambda) > breakThreshold;
    }

    /// <summary>
    /// Compute fracture impulse direction using torque × force (paper Eq. 6).
    /// l̂ = - (τ × f) / ||τ × f||
    /// </summary>
    public Vector3 ComputeFractureDirection(out Vector3 force, out Vector3 torque)
    {
        Vector3 n;
        GetViolation(out n);

        // Approximate force from accumulated lambda
        force = n * lambda;

        // Compute torques at contact points
        Vector3 worldA = bodyA.LocalToWorld(localAnchorA);
        Vector3 worldB = bodyB.LocalToWorld(localAnchorB);
        Vector3 rA = worldA - (bodyA.position + bodyA.rotation * bodyA.centerOfMass);
        Vector3 rB = worldB - (bodyB.position + bodyB.rotation * bodyB.centerOfMass);

        torque = Vector3.Cross(rA, -force) + Vector3.Cross(rB, force);

        // l̂ = - (τ × f) / ||τ × f||
        Vector3 cross = Vector3.Cross(torque, force);
        float crossMag = cross.magnitude;

        if (crossMag > 1e-6f)
        {
            return -cross / crossMag;
        }
        else
        {
            // Fallback: use constraint direction or random
            return n.sqrMagnitude > 1e-8f ? n : Random.onUnitSphere;
        }
    }

    /// <summary>
    /// Apply fracture impulse based on stored energy (paper Eq. 5 and 8).
    /// Converts elastic potential energy to kinetic energy.
    /// </summary>
    public void ApplyFractureImpulse(float energyTransferRatio = 1f)
    {
        if (isBroken) return;

        Vector3 force, torque;
        Vector3 direction = ComputeFractureDirection(out force, out torque);

        // Compute impulse magnitude: µ = sqrt(2 * α * m_G * E)
        // where m_G = (G * M^-1 * G^T)^-1 is the generalized inverse mass
        Vector3 rA, rB;
        float mG = ComputeEffectiveMass(direction, out rA, out rB);

        float mu = Mathf.Sqrt(2f * energyTransferRatio * mG * Mathf.Max(0f, storedEnergy));

        // Split impulse between bodies based on mass ratio
        float totalMass = bodyA.mass + bodyB.mass;
        Vector3 impulseA = -direction * mu * (bodyB.mass / totalMass);
        Vector3 impulseB = direction * mu * (bodyA.mass / totalMass);

        // Store fracture data for visualization
        fracturePointA = bodyA.LocalToWorld(localAnchorA);
        fracturePointB = bodyB.LocalToWorld(localAnchorB);
        fractureImpulseA = impulseA;
        fractureImpulseB = impulseB;

        // Apply impulses
        bodyA.ApplyImpulse(impulseA, fracturePointA);
        bodyB.ApplyImpulse(impulseB, fracturePointB);

        isBroken = true;

        Debug.Log($"Constraint fractured! λ={lambda:F2}, E={storedEnergy:F3}, µ={mu:F2}, dir={direction}");
    }

    /// <summary>
    /// Draw constraint for debugging.
    /// </summary>
    public void DrawGizmo()
    {
        Vector3 worldA = bodyA.LocalToWorld(localAnchorA);
        Vector3 worldB = bodyB.LocalToWorld(localAnchorB);

        if (!isBroken)
        {
            // Color based on stress
            float stress = Mathf.Abs(lambda) / breakThreshold;
            if (stress > 0.8f)
                Gizmos.color = Color.red;
            else if (stress > 0.5f)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.green;

            Gizmos.DrawLine(worldA, worldB);
            Gizmos.DrawSphere(worldA, 0.05f);
            Gizmos.DrawSphere(worldB, 0.05f);
        }
        else
        {
            // Draw fracture impulse vectors
            Gizmos.color = Color.cyan;
            if (fractureImpulseA.sqrMagnitude > 1e-6f)
            {
                Gizmos.DrawRay(fracturePointA, fractureImpulseA.normalized * 0.5f);
            }
            if (fractureImpulseB.sqrMagnitude > 1e-6f)
            {
                Gizmos.DrawRay(fracturePointB, fractureImpulseB.normalized * 0.5f);
            }
        }
    }
}

