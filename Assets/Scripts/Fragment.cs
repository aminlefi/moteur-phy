using System.Linq;
using UnityEngine;

// Represents a fragment (shard) with mass and center of inertia (center of mass).
[RequireComponent(typeof(Rigidbody))]
public class Fragment : MonoBehaviour
{
    public float mass = 1f;
    public Vector3 centerOfInertiaLocal = Vector3.zero;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = Mathf.Max(0.0001f, mass);
    }

    // Call this after the mesh is assigned to compute center of inertia from mesh vertices.
    public void ComputeCenterOfInertiaFromMesh()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;
        var verts = mf.sharedMesh.vertices;
        if (verts == null || verts.Length == 0) return;
        Vector3 sum = Vector3.zero;
        foreach (var v in verts) sum += v;
        centerOfInertiaLocal = sum / verts.Length;
        // set Rigidbody center of mass (local space)
        if (rb != null)
        {
            rb.centerOfMass = centerOfInertiaLocal;
            rb.mass = Mathf.Max(0.0001f, mass);
        }
    }

    public void ApplyImpulse(Vector3 impulse)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(impulse, ForceMode.Impulse);
        }
    }
}
