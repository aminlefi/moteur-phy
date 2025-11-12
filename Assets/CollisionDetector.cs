using UnityEngine;

public static class CollisionDetector
{
    // Ground plane: Y = 0
    // Sphere-Plane collision - Chapitre 4, page 8-11
    public static bool DetectSpherePlaneCollision(Vector3 sphereCenter, float radius,
        Vector3 planeNormal, float planeDistance, out Vector3 contactPoint, out Vector3 contactNormal)
    {
        // Distance from sphere center to plane - Chapitre 3, page 26
        float distance = Vector3.Dot(sphereCenter, planeNormal) - planeDistance;

        contactNormal = planeNormal;
        contactPoint = sphereCenter - planeNormal * radius;

        return distance <= radius;
    }

    // Sphere-Sphere collision - Chapitre 4, page 23
    public static bool DetectSphereSphereCollision(Vector3 center1, float radius1,
        Vector3 center2, float radius2, out Vector3 contactPoint, out Vector3 contactNormal)
    {
        Vector3 direction = center2 - center1;
        float distance = direction.magnitude;

        contactNormal = direction.normalized;
        contactPoint = center1 + contactNormal * radius1;

        return distance <= (radius1 + radius2);
    }
}
