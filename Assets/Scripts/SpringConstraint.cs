using UnityEngine;

// Simple spring-like rigid constraint between two fragments.
// Measures deformation, computes stored potential energy E=1/2 k x^2, and can break when threshold exceeded.
public class SpringConstraint : MonoBehaviour
{
    public Fragment A;
    public Fragment B;
    public float k = 1000f; // spring constant
    public float restLength = 0f;
    public float breakThreshold = 0.5f; // relative extension (x/restLength) or absolute if restLength==0
    public bool broken = false;

    void Start()
    {
        if (A != null && B != null && restLength <= 0f)
            restLength = Vector3.Distance(A.transform.position, B.transform.position);
    }

    // Returns current extension (x = currentDistance - restLength)
    public float CurrentExtension()
    {
        if (A == null || B == null) return 0f;
        float d = Vector3.Distance(A.transform.position, B.transform.position);
        return Mathf.Max(0f, d - restLength);
    }

    // Evaluate constraint: compute deformation, energy and break if needed.
    // Returns true if broken in this evaluation.
    public bool EvaluateAndMaybeBreak()
    {
        if (broken || A == null || B == null) return false;
        float x = CurrentExtension();
        float relative = restLength > 0f ? x / restLength : x;
        if (relative >= breakThreshold)
        {
            Break(x);
            return true;
        }
        return false;
    }

    void Break(float extension)
    {
        broken = true;
        // potential energy
        float E = 0.5f * k * extension * extension;

        // Direction from A to B
        Vector3 dir = (B.transform.position - A.transform.position).normalized;
        if (dir.sqrMagnitude == 0f) dir = Vector3.up;

        // Assumption: split stored energy equally between fragments
        float EA = E * 0.5f;
        float EB = E * 0.5f;

        float mA = Mathf.Max(0.0001f, A.mass);
        float mB = Mathf.Max(0.0001f, B.mass);

        // velocity magnitude for each fragment from E = 1/2 m v^2 => v = sqrt(2E/m)
        float vA = Mathf.Sqrt(2f * EA / mA);
        float vB = Mathf.Sqrt(2f * EB / mB);

        Vector3 impulseA = dir * (mA * vA); // impulse = m * deltaV
        Vector3 impulseB = -dir * (mB * vB);

        A.ApplyImpulse(impulseA);
        B.ApplyImpulse(impulseB);

        Debug.Log($"SpringConstraint broken between {A.name} and {B.name}. Extension={extension:0.000}, Energy={E:0.000}");
    }
}
