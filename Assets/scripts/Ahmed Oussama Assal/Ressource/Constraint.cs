using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Constraint
{
    public Vector3 point; // Attachment point
    public float stiffness; // Stiffness K
    public float damping; // Damping B
    public float impulse; // Current impulse ?

    public Constraint(Vector3 point, float stiffness, float damping)
    {
        this.point = point;
        this.stiffness = stiffness;
        this.damping = damping;
        this.impulse = 0f;
    }

    public float ComputeEnergy(float gap)
    {
        return 0.5f * stiffness * gap * gap;
    }

    public bool ShouldFracture(float threshold)
    {
        return impulse > threshold;
    }
}