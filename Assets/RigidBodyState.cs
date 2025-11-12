using UnityEngine;

public class RigidBodyState
{
    // State variables - Chapitre 2 Partie 2, page 37
    public Vector3 position;           // x(t) - Position du centre de masse
    public Quaternion orientation;     // R(t) - Orientation
    public Vector3 velocity;           // P(t)/m - Vitesse linéaire
    public Vector3 angularVelocity;    // ω(t) - Vitesse angulaire

    // Constants
    public float mass;                 // Masse
    public Matrix4x4 inertiaTensorBody; // Tenseur d'inertie dans l'espace local
    public float restitution;          // Coefficient de restitution (0-1)

    // Computed values - Chapitre 2 Partie 2, page 37
    public Vector3 momentum;           // P(t) - Quantité de mouvement
    public Vector3 angularMomentum;    // L(t) - Moment angulaire
    public Matrix4x4 inertiaTensorWorld; // I(t) - Tenseur d'inertie dans l'espace monde
    public Matrix4x4 inverseInertiaTensor; // I^-1(t)

    public RigidBodyState(float m, Vector3 pos, float rest = 0.5f)
    {
        mass = m;
        position = pos;
        orientation = Quaternion.identity;
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;
        restitution = rest;

        momentum = Vector3.zero;
        angularMomentum = Vector3.zero;
    }

    // Update computed values - Chapitre 2 Partie 2, page 37
    public void UpdateDerivedQuantities()
    {
        momentum = mass * velocity;

        // I(t) = R(t) * I_body * R(t)^T
        Matrix4x4 R = CustomMath.CreateRotationMatrix(orientation);
        inertiaTensorWorld = R * inertiaTensorBody * R.transpose;

        // Calculate inverse
        inverseInertiaTensor = inertiaTensorWorld.inverse;

        // ω(t) = I^-1(t) * L(t)
        angularVelocity = MultiplyMatrixVector(inverseInertiaTensor, angularMomentum);
    }

    public Vector3 MultiplyMatrixVector(Matrix4x4 mat, Vector3 vec)
    {
        return new Vector3(
            mat.m00 * vec.x + mat.m01 * vec.y + mat.m02 * vec.z,
            mat.m10 * vec.x + mat.m11 * vec.y + mat.m12 * vec.z,
            mat.m20 * vec.x + mat.m21 * vec.y + mat.m22 * vec.z
        );
    }

    // Set inertia tensor for sphere - Chapitre 2 Partie 2, page 33
    public void SetSphericalInertiaTensor(float radius)
    {
        float I = (2.0f / 5.0f) * mass * radius * radius;
        inertiaTensorBody = Matrix4x4.identity;
        inertiaTensorBody.m00 = I;
        inertiaTensorBody.m11 = I;
        inertiaTensorBody.m22 = I;
    }

    // Set inertia tensor for box - Chapitre 2 Partie 2, page 33
    public void SetBoxInertiaTensor(Vector3 size)
    {
        float Ix = (1.0f / 12.0f) * mass * (size.y * size.y + size.z * size.z);
        float Iy = (1.0f / 12.0f) * mass * (size.x * size.x + size.z * size.z);
        float Iz = (1.0f / 12.0f) * mass * (size.x * size.x + size.y * size.y);

        inertiaTensorBody = Matrix4x4.identity;
        inertiaTensorBody.m00 = Ix;
        inertiaTensorBody.m11 = Iy;
        inertiaTensorBody.m22 = Iz;
    }
}
