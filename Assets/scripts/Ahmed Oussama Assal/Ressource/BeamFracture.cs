using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BeamFracture : MonoBehaviour
{
    public RigidBody3DState beamPiece1;
    public RigidBody3DState beamPiece2;
    public List<Constraint> constraints;
    public float fractureThreshold = 10f;
    public float energyConversionFactor = 0.8f;

    void Start()
    {
        // Initialize beam pieces and constraints
        beamPiece1 = new RigidBody3DState();
        beamPiece2 = new RigidBody3DState();
        constraints = InitializeConstraints();
    }

    void Update()
    {
        foreach (var constraint in constraints)
        {
            float gap = ComputeGap(constraint);
            float energy = constraint.ComputeEnergy(gap);

            if (constraint.ShouldFracture(fractureThreshold))
            {
                ApplyFractureImpulse(constraint, energy);
                constraints.Remove(constraint);
            }
        }
    }

    float ComputeGap(Constraint constraint)
    {
        // Compute the gap (?_i) between the two beam pieces at the constraint point
        Vector3 relativePosition = beamPiece2.position - beamPiece1.position;
        return Vector3.Dot(relativePosition, constraint.point.normalized);
    }

    void ApplyFractureImpulse(Constraint constraint, float energy)
    {
        // Convert energy into kinetic energy
        float impulseMagnitude = Mathf.Sqrt(2 * energyConversionFactor * energy);
        Vector3 impulseDirection = ComputeImpulseDirection(constraint);

        // Apply impulse to both beam pieces
        beamPiece1.ApplyImpulse(-impulseMagnitude * impulseDirection, constraint.point);
        beamPiece2.ApplyImpulse(impulseMagnitude * impulseDirection, constraint.point);
    }

    Vector3 ComputeImpulseDirection(Constraint constraint)
    {
        // Compute direction orthogonal to torque and force
        Vector3 torque = Vector3.Cross(beamPiece1.position, beamPiece2.position);
        Vector3 force = beamPiece2.position - beamPiece1.position;

        if (torque.magnitude < 1e-6f)
            return Vector3.Cross(force, Vector3.up).normalized;

        return Vector3.Cross(torque, force).normalized;
    }

    List<Constraint> InitializeConstraints()
    {
        // Define constraints between the two beam pieces
        return new List<Constraint>
        {
            new Constraint(new Vector3(0, 1, 0), 1000f, 0.5f),
            new Constraint(new Vector3(0, -1, 0), 1000f, 0.5f)
        };
    }
}