using UnityEngine;

// Simple custom rigid body: integrates velocity and supports gravity and simple angular dynamics. No Unity physics/Transform.
[DisallowMultipleComponent]
public class SimpleBody : MonoBehaviour
{
    public CustomTransform customTransform;
    public SimpleAABBCollider colliderRef; // optional link

    [Tooltip("If true, body is not moved by integration and treated as infinite mass in resolves.")]
    public bool isStatic = false;
    [Tooltip("Mass in kg. Non-positive means infinite mass (static).")]
    public float mass =1f;
    [Tooltip("Gravity multiplier.")]
    public float gravityScale =1f;
    [Tooltip("Linear velocity in world units/sec.")]
    public Vector3 velocity = Vector3.zero;
    [Tooltip("Simple linear damping0..1 per second.")]
    [Range(0,1)] public float damping =0.0f;

    [Header("Angular (world-space)")]
    [Tooltip("Angular velocity in radians/sec around world X/Y/Z (approximate integration)")]
    public Vector3 angularVelocity = Vector3.zero;
    [Tooltip("Angular damping0..1 per second")]
    [Range(0,1)] public float angularDamping =0.0f;

    public float InverseMass
    {
        get
        {
            if (isStatic || mass <=0f) return 0f;
            return 1f / mass;
        }
    }

    void Reset()
    {
        if (!customTransform) customTransform = GetComponent<CustomTransform>();
        if (!colliderRef) colliderRef = GetComponent<SimpleAABBCollider>();
    }

    // Apply an impulse at a world-space point (instantaneous). Updates linear and angular velocity.
    public void ApplyImpulseAtPoint(Vector3 worldPoint, Vector3 impulse)
    {
        if (isStatic || mass <=0f) return;
        // Linear
        velocity += impulse * InverseMass;
        // Angular: dw = I^{-1} * (r x J)
        Vector3 center = GetCenter();
        Vector3 r = worldPoint - center;
        Vector3 L = Vector3.Cross(r, impulse); // angular impulse
        Vector3 dw = InverseInertiaWorldTimes(L);
        angularVelocity += dw;
    }

    // Apply a force at point over dt (accumulates as impulse)
    public void ApplyForceAtPoint(Vector3 worldPoint, Vector3 force, float dt)
    {
        if (isStatic || mass <=0f) return;
        Vector3 imp = force * dt;
        ApplyImpulseAtPoint(worldPoint, imp);
    }

    Vector3 GetCenter()
    {
        if (colliderRef != null)
        {
            return colliderRef.Center;
        }
        if (customTransform != null)
        {
            var M = customTransform.LocalToWorldMatrix;
            return new Vector3(M.m03, M.m13, M.m23);
        }
        return Vector3.zero;
    }

    public Vector3 InverseInertiaWorldTimes(Vector3 v)
    {
        // Compute I_body inverse for a box of current dimensions aligned with collider axes.
        // If no collider, approximate using scale as axes aligned to world.
        float m = Mathf.Max(0f, mass);
        if (m <=0f) return Vector3.zero;

        Vector3 axX = Vector3.right, axY = Vector3.up, axZ = Vector3.forward;
        float sx =1f, sy =1f, sz =1f;
        if (colliderRef != null)
        {
            axX = colliderRef.AxisX; axY = colliderRef.AxisY; axZ = colliderRef.AxisZ;
            Vector3 he = colliderRef.HalfExtents; // world half extents along local axes
            sx = he.x *2f; sy = he.y *2f; sz = he.z *2f; // full lengths
        }
        else if (customTransform != null)
        {
            // use scale as full lengths
            sx = Mathf.Abs(customTransform.scale.x);
            sy = Mathf.Abs(customTransform.scale.y);
            sz = Mathf.Abs(customTransform.scale.z);
        }

        // Box inertia about center in body frame: Ixx =1/12 m (b^2 + c^2)
        float Ixx = (m * (sy*sy + sz*sz)) /12f;
        float Iyy = (m * (sx*sx + sz*sz)) /12f;
        float Izz = (m * (sx*sx + sy*sy)) /12f;

        // Inverse in body frame
        float invIxx = Ixx >1e-8f ?1f/Ixx :0f;
        float invIyy = Iyy >1e-8f ?1f/Iyy :0f;
        float invIzz = Izz >1e-8f ?1f/Izz :0f;

        // Transform v to body frame using axes, multiply by inv inertia, then back to world: R * invI * R^T * v
        // Body frame components
        float vx = Vector3.Dot(v, axX);
        float vy = Vector3.Dot(v, axY);
        float vz = Vector3.Dot(v, axZ);
        Vector3 wBody = new Vector3(vx * invIxx, vy * invIyy, vz * invIzz);
        // Back to world
        Vector3 wWorld = wBody.x * axX + wBody.y * axY + wBody.z * axZ;
        return wWorld;
    }
}
