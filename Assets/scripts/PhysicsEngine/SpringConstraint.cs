using UnityEngine;

/// <summary>
/// Contrainte type ressort entre deux fragments (comme dans le paper)
/// </summary>
public class SpringConstraint
{
    public RigidFragment fragmentA;
    public RigidFragment fragmentB;
    
    // Points d'attache locaux
    public Vector3 localPointA;
    public Vector3 localPointB;
    
    // Propriétés du ressort
    public float stiffness = 1000f;  // k - rigidité
    public float restLength;         // Longueur au repos
    public float breakThreshold = 5f; // Seuil de rupture
    // Debug guard: clamp maximum Δv applied on break to avoid exploding fragments
    public float maxBreakDeltaV = 8f;
    // Fraction of the stored energy that is actually converted to impulse on break
    // Increased default to encourage separation while still clamping extremes
    public float breakEnergyFraction = 0.5f;
    
    public bool isBroken = false;
    
    public SpringConstraint(RigidFragment a, RigidFragment b, Vector3 localA, Vector3 localB)
    {
        fragmentA = a;
        fragmentB = b;
        localPointA = localA;
        localPointB = localB;
        
        // Calculer la longueur au repos initiale
        Vector3 worldA = a.GetWorldPoint(localA);
        Vector3 worldB = b.GetWorldPoint(localB);
        restLength = Vector3.Distance(worldA, worldB);
    }
    
    /// <summary>
    /// Mesurer la violation de la contrainte (déformation)
    /// </summary>
    public float MeasureViolation(out Vector3 direction)
    {
        Vector3 worldA = fragmentA.GetWorldPoint(localPointA);
        Vector3 worldB = fragmentB.GetWorldPoint(localPointB);
        
        Vector3 diff = worldB - worldA;
        float currentLength = diff.magnitude;
        
        direction = diff.normalized;
        
        // x = déformation = longueur actuelle - longueur repos
        float deformation = currentLength - restLength;
        
        return deformation;
    }
    
    /// <summary>
    /// Calculer l'énergie potentielle stockée: E = 1/2 * k * x²
    /// </summary>
    public float CalculatePotentialEnergy()
    {
        Vector3 dir;
        float x = MeasureViolation(out dir);
        
        // E = 1/2 * k * x²
        float energy = 0.5f * stiffness * x * x;
        
        return energy;
    }
    
    /// <summary>
    /// Vérifier si la contrainte doit se rompre
    /// </summary>
    public bool ShouldBreak()
    {
        Vector3 dir;
        float deformation = Mathf.Abs(MeasureViolation(out dir));
        
        return deformation > breakThreshold;
    }
    
    /// <summary>
    /// Rompre la contrainte et appliquer les impulsions
    /// selon formule: ΔV = √(2E/m)
    /// </summary>
    public void Break()
    {
        // Avoid re-breaking
        if (isBroken) return;

        // Mark early to avoid re-entrancy during the same step
        isBroken = true;

        // Obtain world points and direction/deformation
        Vector3 worldA = fragmentA.GetWorldPoint(localPointA);
        Vector3 worldB = fragmentB.GetWorldPoint(localPointB);
        Vector3 diff = worldB - worldA;
        float currentLength = diff.magnitude;

        Vector3 direction = (currentLength > 1e-8f) ? diff / currentLength : Vector3.up;

        float deformation = currentLength - restLength; // signed
        float x = Mathf.Abs(deformation);

    // Stored potential energy: E = 1/2 * k * x^2
    float energy = 0.5f * stiffness * x * x;

    // Use only a fraction of the stored energy to prevent cumulative explosions
    energy *= Mathf.Clamp01(breakEnergyFraction);

        // defensive: avoid zero mass (use small epsilon)
        float mA = Mathf.Max(1e-6f, fragmentA.mass);
        float mB = Mathf.Max(1e-6f, fragmentB.mass);

        // Δv per fragment
        float deltaVA = Mathf.Sqrt(2f * energy / mA);
        float deltaVB = Mathf.Sqrt(2f * energy / mB);

        // Clamp extreme values to avoid fragments instantly flying out of view (debugging guard)
        if (maxBreakDeltaV > 0f)
        {
            deltaVA = Mathf.Min(deltaVA, maxBreakDeltaV);
            deltaVB = Mathf.Min(deltaVB, maxBreakDeltaV);
        }

        // impulses (J = m * Δv)
        Vector3 impulseA = -direction * deltaVA * mA;
        Vector3 impulseB =  direction * deltaVB * mB;

        Debug.LogFormat("[SpringConstraint.Break] x={0:F4}, E={1:F4}, dir={2}, mA={3:F3}, mB={4:F3}, ΔVA={5:F4}, ΔVB={6:F4}, JA={7}, JB={8}",
            x, energy, direction, mA, mB, deltaVA, deltaVB, impulseA, impulseB);

        // Apply impulses at contact points
    fragmentA.ApplyImpulse(impulseA, worldA);
    fragmentB.ApplyImpulse(impulseB, worldB);

        Debug.Log("[SpringConstraint] Constraint broken and impulses applied.");
    }

    /// <summary>
    /// Compute the impulses that would be applied on break, without actually applying them.
    /// Marks the constraint broken to avoid repeated computation.
    /// </summary>
    public void ComputeBreakImpulses(out Vector3 impulseA, out Vector3 impulseB, out Vector3 worldA, out Vector3 worldB)
    {
        // Avoid re-computing
        if (isBroken)
        {
            impulseA = Vector3.zero; impulseB = Vector3.zero; worldA = Vector3.zero; worldB = Vector3.zero;
            return;
        }

        isBroken = true;

        worldA = fragmentA.GetWorldPoint(localPointA);
        worldB = fragmentB.GetWorldPoint(localPointB);
        Vector3 diff = worldB - worldA;
        float currentLength = diff.magnitude;

        Vector3 direction = (currentLength > 1e-8f) ? diff / currentLength : Vector3.up;

        float deformation = currentLength - restLength; // signed
        float x = Mathf.Abs(deformation);

        float energy = 0.5f * stiffness * x * x;
        energy *= Mathf.Clamp01(breakEnergyFraction);

        float mA = Mathf.Max(1e-6f, fragmentA.mass);
        float mB = Mathf.Max(1e-6f, fragmentB.mass);

        float deltaVA = Mathf.Sqrt(2f * energy / mA);
        float deltaVB = Mathf.Sqrt(2f * energy / mB);

        if (maxBreakDeltaV > 0f)
        {
            deltaVA = Mathf.Min(deltaVA, maxBreakDeltaV);
            deltaVB = Mathf.Min(deltaVB, maxBreakDeltaV);
        }

        impulseA = -direction * deltaVA * mA;
        impulseB =  direction * deltaVB * mB;

        Debug.LogFormat("[SpringConstraint.ComputeBreakImpulses] x={0:F4}, E={1:F4}, dir={2}, JA={3}, JB={4}", x, energy, direction, impulseA, impulseB);
    }
    
    /// <summary>
    /// Dessiner la contrainte en mode Gizmos
    /// </summary>
    public void DrawGizmo()
    {
        if (isBroken) return;
        
        Vector3 worldA = fragmentA.GetWorldPoint(localPointA);
        Vector3 worldB = fragmentB.GetWorldPoint(localPointB);
        
        Vector3 dir;
        float violation = MeasureViolation(out dir);
        
        // Couleur selon la violation
        if (Mathf.Abs(violation) > breakThreshold * 0.8f)
            Gizmos.color = Color.red;
        else if (Mathf.Abs(violation) > breakThreshold * 0.5f)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.green;
        
        Gizmos.DrawLine(worldA, worldB);
        Gizmos.DrawSphere(worldA, 0.03f);
        Gizmos.DrawSphere(worldB, 0.03f);
    }
}
