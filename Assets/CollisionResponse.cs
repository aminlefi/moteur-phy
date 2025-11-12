using UnityEngine;

public static class CollisionResponse
{
    // Impulse-based collision response - Chapitre 4, page 40-42
    public static void ApplyCollisionImpulse(RigidBodyState body1, RigidBodyState body2,
        Vector3 contactPoint, Vector3 contactNormal, bool body2IsStatic = false)
    {
        // Relative velocity at contact point
        Vector3 r1 = contactPoint - body1.position;
        Vector3 v1 = body1.velocity + Vector3.Cross(body1.angularVelocity, r1);

        Vector3 v2 = Vector3.zero;
        Vector3 r2 = Vector3.zero;

        if (!body2IsStatic)
        {
            r2 = contactPoint - body2.position;
            v2 = body2.velocity + Vector3.Cross(body2.angularVelocity, r2);
        }

        Vector3 vrel = v1 - v2;
        float vrelNormal = Vector3.Dot(vrel, contactNormal);

        // Objects separating, no collision response needed
        if (vrelNormal > 0) return;

        // Coefficient of restitution
        float e = Mathf.Min(body1.restitution, body2IsStatic ? 1.0f : body2.restitution);

        // Calculate impulse magnitude - Chapitre 4, page 42
        float numerator = -(1.0f + e) * vrelNormal;

        float term1 = 1.0f / body1.mass;
        float term2 = body2IsStatic ? 0 : (1.0f / body2.mass);

        Vector3 cross1 = Vector3.Cross(r1, contactNormal);
        Vector3 term3 = body1.MultiplyMatrixVector(body1.inverseInertiaTensor, cross1);
        term3 = Vector3.Cross(term3, r1);

        Vector3 term4 = Vector3.zero;
        if (!body2IsStatic)
        {
            Vector3 cross2 = Vector3.Cross(r2, contactNormal);
            term4 = body2.MultiplyMatrixVector(body2.inverseInertiaTensor, cross2);
            term4 = Vector3.Cross(term4, r2);
        }

        float denominator = term1 + term2 + Vector3.Dot(term3 + term4, contactNormal);

        float j = numerator / denominator;

        // Apply impulse to body1
        Vector3 impulse = j * contactNormal;
        body1.velocity += impulse / body1.mass;
        body1.angularMomentum += Vector3.Cross(r1, impulse);

        // Apply impulse to body2 (if not static)
        if (!body2IsStatic)
        {
            body2.velocity -= impulse / body2.mass;
            body2.angularMomentum -= Vector3.Cross(r2, impulse);
        }

        // Separate objects to avoid interpenetration
        Vector3 separation = contactNormal * 0.01f;
        body1.position += separation;
        if (!body2IsStatic)
        {
            body2.position -= separation;
        }

        body1.UpdateDerivedQuantities();
        if (!body2IsStatic)
        {
            body2.UpdateDerivedQuantities();
        }
    }
}
