using UnityEngine;

// Oriented box collider using CustomTransform (no Unity Transform / physics)
[DisallowMultipleComponent]
public class SimpleAABBCollider : MonoBehaviour
{
    public ProceduralCube sourceCube; // If null, will search on same GameObject
    public CustomTransform customTransform;

    // Center from matrix translation
    public Vector3 Center
    {
        get
        {
            var M = customTransform ? customTransform.LocalToWorldMatrix : Float4x4.Identity;
            return new Vector3(M.m03, M.m13, M.m23);
        }
    }

    // Local axes (normalized) extracted from matrix columns (rotation part)
    public Vector3 AxisX
    {
        get
        {
            var M = customTransform ? customTransform.LocalToWorldMatrix : Float4x4.Identity;
            var v = new Vector3(M.m00, M.m10, M.m20);
            return v.sqrMagnitude > 0f ? v.normalized : Vector3.right;
        }
    }
    public Vector3 AxisY
    {
        get
        {
            var M = customTransform ? customTransform.LocalToWorldMatrix : Float4x4.Identity;
            var v = new Vector3(M.m01, M.m11, M.m21);
            return v.sqrMagnitude > 0f ? v.normalized : Vector3.up;
        }
    }
    public Vector3 AxisZ
    {
        get
        {
            var M = customTransform ? customTransform.LocalToWorldMatrix : Float4x4.Identity;
            var v = new Vector3(M.m02, M.m12, M.m22);
            return v.sqrMagnitude > 0f ? v.normalized : Vector3.forward;
        }
    }

    // Half extents along local axes (scale * procedural cube half size)
    public Vector3 HalfExtents
    {
        get
        {
            var hs = sourceCube ? sourceCube.HalfSize : Vector3.one * 0.5f;
            var M = customTransform ? customTransform.LocalToWorldMatrix : Float4x4.Identity;
            float sx = new Vector3(M.m00, M.m10, M.m20).magnitude;
            float sy = new Vector3(M.m01, M.m11, M.m21).magnitude;
            float sz = new Vector3(M.m02, M.m12, M.m22).magnitude;
            return new Vector3(hs.x * sx, hs.y * sy, hs.z * sz);
        }
    }

    void Reset()
    {
        if (!sourceCube) sourceCube = GetComponent<ProceduralCube>();
        if (!customTransform) customTransform = GetComponent<CustomTransform>();
    }

    // Separating Axis Theorem for OBB vs OBB (15 axes)
    public bool Overlaps(SimpleAABBCollider other)
    {
        // Gather data
        Vector3 C1 = Center; Vector3 C2 = other.Center;
        Vector3 A1 = AxisX; Vector3 A2 = AxisY; Vector3 A3 = AxisZ;
        Vector3 B1 = other.AxisX; Vector3 B2 = other.AxisY; Vector3 B3 = other.AxisZ;
        Vector3 e1 = HalfExtents; Vector3 e2 = other.HalfExtents;

        // Rotation matrix expressing B in A's frame
        float[,] R = new float[3, 3];
        float[,] AbsR = new float[3, 3];
        const float EPS = 1e-5f;
        R[0, 0] = Vector3.Dot(A1, B1); R[0, 1] = Vector3.Dot(A1, B2); R[0, 2] = Vector3.Dot(A1, B3);
        R[1, 0] = Vector3.Dot(A2, B1); R[1, 1] = Vector3.Dot(A2, B2); R[1, 2] = Vector3.Dot(A2, B3);
        R[2, 0] = Vector3.Dot(A3, B1); R[2, 1] = Vector3.Dot(A3, B2); R[2, 2] = Vector3.Dot(A3, B3);
        for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) AbsR[i, j] = Mathf.Abs(R[i, j]) + EPS;

        // Translation vector from box1 to box2 in world, then in A's frame
        Vector3 t = C2 - C1;
        Vector3 tA = new Vector3(Vector3.Dot(t, A1), Vector3.Dot(t, A2), Vector3.Dot(t, A3));

        // Test axes A1,A2,A3
        for (int i = 0; i < 3; i++)
        {
            float ra = i == 0 ? e1.x : (i == 1 ? e1.y : e1.z);
            float rb = e2.x * AbsR[i, 0] + e2.y * AbsR[i, 1] + e2.z * AbsR[i, 2];
            if (Mathf.Abs(i == 0 ? tA.x : (i == 1 ? tA.y : tA.z)) > ra + rb) return false;
        }
        // Test axes B1,B2,B3
        for (int i = 0; i < 3; i++)
        {
            float ra = e1.x * AbsR[0, i] + e1.y * AbsR[1, i] + e1.z * AbsR[2, i];
            float rb = i == 0 ? e2.x : (i == 1 ? e2.y : e2.z);
            float tProj = Mathf.Abs(tA.x * R[0, i] + tA.y * R[1, i] + tA.z * R[2, i]);
            if (tProj > ra + rb) return false;
        }
        // Test cross products A_i x B_j (9 axes)
        // Helper lambda
        bool CrossTest(float tVal, float ra, float rb) { return Mathf.Abs(tVal) <= ra + rb; }

        // Precompute for speed
        float tX = tA.x; float tY = tA.y; float tZ = tA.z; float aex = e1.x, aey = e1.y, aez = e1.z; float bex = e2.x, bey = e2.y, bez = e2.z;

        // A1 x B1
        if (!CrossTest(tZ * R[1, 0] - tY * R[2, 0], aey * AbsR[2, 0] + aez * AbsR[1, 0], bey * AbsR[0, 2] + bez * AbsR[0, 1])) return false;
        // A1 x B2
        if (!CrossTest(tZ * R[1, 1] - tY * R[2, 1], aey * AbsR[2, 1] + aez * AbsR[1, 1], bex * AbsR[0, 2] + bez * AbsR[0, 0])) return false;
        // A1 x B3
        if (!CrossTest(tZ * R[1, 2] - tY * R[2, 2], aey * AbsR[2, 2] + aez * AbsR[1, 2], bex * AbsR[0, 1] + bey * AbsR[0, 0])) return false;
        // A2 x B1
        if (!CrossTest(tX * R[2, 0] - tZ * R[0, 0], aex * AbsR[2, 0] + aez * AbsR[0, 0], bey * AbsR[1, 2] + bez * AbsR[1, 1])) return false;
        // A2 x B2
        if (!CrossTest(tX * R[2, 1] - tZ * R[0, 1], aex * AbsR[2, 1] + aez * AbsR[0, 1], bex * AbsR[1, 2] + bez * AbsR[1, 0])) return false;
        // A2 x B3
        if (!CrossTest(tX * R[2, 2] - tZ * R[0, 2], aex * AbsR[2, 2] + aez * AbsR[0, 2], bex * AbsR[1, 1] + bey * AbsR[1, 0])) return false;
        // A3 x B1
        if (!CrossTest(tY * R[0, 0] - tX * R[1, 0], aex * AbsR[1, 0] + aey * AbsR[0, 0], bey * AbsR[2, 2] + bez * AbsR[2, 1])) return false;
        // A3 x B2
        if (!CrossTest(tY * R[0, 1] - tX * R[1, 1], aex * AbsR[1, 1] + aey * AbsR[0, 1], bex * AbsR[2, 2] + bez * AbsR[2, 0])) return false;
        // A3 x B3
        if (!CrossTest(tY * R[0, 2] - tX * R[1, 2], aex * AbsR[1, 2] + aey * AbsR[0, 2], bex * AbsR[2, 1] + bey * AbsR[2, 0])) return false;

        return true; // no separating axis found
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 c = Center; Vector3 hx = AxisX * HalfExtents.x; Vector3 hy = AxisY * HalfExtents.y; Vector3 hz = AxisZ * HalfExtents.z;
        //8 corners
        Vector3 c000 = c - hx - hy - hz;
        Vector3 c001 = c - hx - hy + hz;
        Vector3 c010 = c - hx + hy - hz;
        Vector3 c011 = c - hx + hy + hz;
        Vector3 c100 = c + hx - hy - hz;
        Vector3 c101 = c + hx - hy + hz;
        Vector3 c110 = c + hx + hy - hz;
        Vector3 c111 = c + hx + hy + hz;
        // edges
        DrawEdge(c000, c001); DrawEdge(c000, c010); DrawEdge(c000, c100);
        DrawEdge(c111, c101); DrawEdge(c111, c011); DrawEdge(c111, c110);
        DrawEdge(c100, c101); DrawEdge(c100, c110); DrawEdge(c010, c011); DrawEdge(c010, c110); DrawEdge(c001, c011); DrawEdge(c001, c101);
    }
    void DrawEdge(Vector3 a, Vector3 b) { Gizmos.DrawLine(a, b); }
#endif
}
