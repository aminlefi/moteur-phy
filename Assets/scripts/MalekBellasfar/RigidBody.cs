using UnityEngine;

/// <summary>
/// Custom RigidBody implementation following Newton-Euler equations.
/// Does not use Unity's built-in physics components.
/// </summary>
public class RigidBody
{
    // State variables
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public Vector3 velocity = Vector3.zero;
    public Vector3 angularVelocity = Vector3.zero; // rad/s in world space

    // Mass properties
    public float mass = 1f;
    public Vector3 inertiaTensor = Vector3.one; // Diagonal inertia tensor in body space (Ix, Iy, Iz)
    public Vector3 centerOfMass = Vector3.zero; // Local space offset

    // Cached inverse values for efficiency
    public float invMass => mass > 1e-6f ? 1f / mass : 0f;

    public RigidBody()
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
    }

    public RigidBody(Vector3 pos, Quaternion rot, float m, Vector3 inertia)
    {
        position = pos;
        rotation = rot;
        mass = Mathf.Max(1e-6f, m);
        inertiaTensor = new Vector3(
            Mathf.Max(1e-6f, inertia.x),
            Mathf.Max(1e-6f, inertia.y),
            Mathf.Max(1e-6f, inertia.z)
        );
    }

    /// <summary>
    /// Apply external force and torque, then integrate motion using Newton-Euler equations.
    /// v += (F / m) * dt
    /// ω += I⁻¹ * τ * dt
    /// position += v * dt
    /// rotation = Δq * rotation (where Δq comes from ω)
    /// </summary>
    public void Integrate(Vector3 force, Vector3 torque, float dt)
    {
        // Linear motion: v += (F / m) * dt
        velocity += force * invMass * dt;

        // Angular motion: ω += I_world⁻¹ * τ * dt
        // I_world⁻¹ = R * I_body⁻¹ * R^T
        Vector3 torqueLocal = Quaternion.Inverse(rotation) * torque;
        Vector3 invInertia = new Vector3(
            1f / Mathf.Max(1e-6f, inertiaTensor.x),
            1f / Mathf.Max(1e-6f, inertiaTensor.y),
            1f / Mathf.Max(1e-6f, inertiaTensor.z)
        );
        Vector3 angularAccelLocal = new Vector3(
            torqueLocal.x * invInertia.x,
            torqueLocal.y * invInertia.y,
            torqueLocal.z * invInertia.z
        );
        Vector3 angularAccelWorld = rotation * angularAccelLocal;
        angularVelocity += angularAccelWorld * dt;

        // Update position: position += v * dt
        position += velocity * dt;

        // Update rotation: q_new = Δq * q_old
        float omegaMag = angularVelocity.magnitude;
        if (omegaMag > 1e-6f)
        {
            float angle = omegaMag * dt;
            Vector3 axis = angularVelocity / omegaMag;
            Quaternion deltaQ = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis);
            rotation = (deltaQ * rotation).normalized;
        }
    }

    /// <summary>
    /// Apply impulse J at world point p.
    /// Δv = J / m
    /// Δω = I_world⁻¹ * (r × J)
    /// </summary>
    public void ApplyImpulse(Vector3 impulse, Vector3 worldPoint)
    {
        // Linear impulse
        velocity += impulse * invMass;

            // Angular impulse
        Vector3 r = worldPoint - (position + rotation * centerOfMass);
        Vector3 torqueImpulse = Vector3.Cross(r, impulse);

        Vector3 torqueLocal = Quaternion.Inverse(rotation) * torqueImpulse;
        Vector3 invInertia = new Vector3(
            1f / Mathf.Max(1e-6f, inertiaTensor.x),
            1f / Mathf.Max(1e-6f, inertiaTensor.y),
            1f / Mathf.Max(1e-6f, inertiaTensor.z)
        );
        Vector3 angularImpulseLocal = new Vector3(
            torqueLocal.x * invInertia.x,
            torqueLocal.y * invInertia.y,
            torqueLocal.z * invInertia.z
        );
        angularVelocity += rotation * angularImpulseLocal;
    }

    /// <summary>
    /// Transform a local point to world space.
    /// </summary>
    public Vector3 LocalToWorld(Vector3 localPoint)
    {
        return position + rotation * localPoint;
    }

    /// <summary>
    /// Get velocity at a world point (includes linear + angular contribution).
    /// </summary>
    public Vector3 GetVelocityAtPoint(Vector3 worldPoint)
    {
        Vector3 r = worldPoint - (position + rotation * centerOfMass);
        return velocity + Vector3.Cross(angularVelocity, r);
    }
}

