using UnityEngine;

/// <summary>
/// Visual representation of a rigid body in the scene.
/// Syncs transform with physics simulation state.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RigidBodyRenderer : MonoBehaviour
{
    public RigidBody physicsBody;
    public Color bodyColor = Color.gray;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material material;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        // Create URP material using utility
        material = MaterialUtility.CreateURPMaterial(bodyColor, smoothness: 0.5f, metallic: 0.2f);
        meshRenderer.material = material;
    }

    void Update()
    {
        if (physicsBody != null)
        {
            // Sync Unity transform with physics simulation
            transform.position = physicsBody.position;
            transform.rotation = physicsBody.rotation;
        }
    }

    void OnDrawGizmos()
    {
        if (physicsBody == null) return;

        // Draw center of mass
        Gizmos.color = Color.red;
        Vector3 com = physicsBody.position + physicsBody.rotation * physicsBody.centerOfMass;
        Gizmos.DrawSphere(com, 0.05f);

        // Draw velocity vector
        if (physicsBody.velocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(physicsBody.position, physicsBody.velocity * 0.5f);
        }

        // Draw angular velocity
        if (physicsBody.angularVelocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(physicsBody.position, physicsBody.angularVelocity * 0.2f);
        }
    }
}

