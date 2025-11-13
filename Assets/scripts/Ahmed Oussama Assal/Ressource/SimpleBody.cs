using UnityEngine;

// Simple custom rigid body: integrates velocity and supports gravity. No Unity physics/Transform.
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
}
