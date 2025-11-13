using UnityEngine;

/// <summary>
/// Simple ground plane for collision detection.
/// Does not use Unity Collider - custom collision detection.
/// </summary>
public class GroundPlane
{
    public Vector3 normal = Vector3.up;
    public float distance = 0f; // Distance from origin along normal
    public float restitution = 0.3f; // Bounciness
    public float friction = 0.5f; // Surface friction

    public GroundPlane(Vector3 planeNormal, float planeDistance)
    {
        normal = planeNormal.normalized;
        distance = planeDistance;
    }

    /// <summary>
    /// Check if a point is below the plane and return penetration depth.
    /// </summary>
    public float GetPenetrationDepth(Vector3 point)
    {
        // Distance from plane: d = n · p - distance
        // Negative means below plane
        float d = Vector3.Dot(normal, point) - distance;
        return -d; // Return positive penetration
    }

    /// <summary>
    /// Apply collision response to a rigid body if it penetrates the plane.
    /// </summary>
    public void ApplyCollision(RigidBody body, float boxHalfExtent = 0.25f)
    {
        // Simple sphere approximation for collision
        Vector3 bodyBottom = body.position; // Could be improved with actual mesh bounds
        
        float penetration = GetPenetrationDepth(bodyBottom) - boxHalfExtent;
        
        if (penetration > 0f)
        {
            // Correct position
            body.position += normal * penetration;
            
            // Get velocity at contact point
            Vector3 velocity = body.velocity;
            float velAlongNormal = Vector3.Dot(velocity, normal);
            
            if (velAlongNormal < 0f) // Moving into plane
            {
                // Reflect velocity with restitution
                Vector3 normalVel = normal * velAlongNormal;
                Vector3 tangentVel = velocity - normalVel;
                
                // Apply restitution to normal component
                normalVel = -normalVel * restitution;
                
                // Apply friction to tangent component
                tangentVel *= (1f - friction);
                
                body.velocity = normalVel + tangentVel;
                
                // Damp angular velocity on contact
                body.angularVelocity *= (1f - friction * 0.5f);
            }
        }
    }

    /// <summary>
    /// Draw the plane for visualization.
    /// </summary>
    public void DrawGizmo(float size = 10f)
    {
        Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        
        Vector3 center = normal * distance;
        
        // Create a simple grid
        Vector3 right = Vector3.Cross(normal, Vector3.forward);
        if (right.sqrMagnitude < 0.01f)
            right = Vector3.Cross(normal, Vector3.up);
        right = right.normalized;
        
        Vector3 forward = Vector3.Cross(normal, right).normalized;
        
        // Draw grid lines
        for (int i = -5; i <= 5; i++)
        {
            Vector3 start1 = center + right * (i * size / 5f) - forward * size;
            Vector3 end1 = center + right * (i * size / 5f) + forward * size;
            Gizmos.DrawLine(start1, end1);
            
            Vector3 start2 = center + forward * (i * size / 5f) - right * size;
            Vector3 end2 = center + forward * (i * size / 5f) + right * size;
            Gizmos.DrawLine(start2, end2);
        }
        
        // Draw normal
        Gizmos.color = Color.green;
        Gizmos.DrawRay(center, normal * 0.5f);
    }
}

