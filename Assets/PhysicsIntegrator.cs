using UnityEngine;

public static class PhysicsIntegrator
{
    // RK4 Integration - Chapitre 2 Partie 1, page 14-21
    public static void IntegrateRK4(RigidBodyState state, Vector3 force, Vector3 torque, float dt)
    {
        // Store initial state
        Vector3 x0 = state.position;
        Vector3 v0 = state.velocity;
        Quaternion q0 = state.orientation;
        Vector3 w0 = state.angularVelocity;

        // k1
        Vector3 k1_v = force / state.mass;
        Vector3 k1_x = v0;
        Vector3 k1_w = state.MultiplyMatrixVector(state.inverseInertiaTensor, torque);
        Quaternion k1_q = DerivativeQuaternion(q0, w0);

        // k2
        state.position = x0 + k1_x * (dt / 2.0f);
        state.velocity = v0 + k1_v * (dt / 2.0f);
        state.orientation = AddQuaternions(q0, ScaleQuaternion(k1_q, dt / 2.0f));
        state.angularVelocity = w0 + k1_w * (dt / 2.0f);
        state.UpdateDerivedQuantities();

        Vector3 k2_v = force / state.mass;
        Vector3 k2_x = state.velocity;
        Vector3 k2_w = state.MultiplyMatrixVector(state.inverseInertiaTensor, torque);
        Quaternion k2_q = DerivativeQuaternion(state.orientation, state.angularVelocity);

        // k3
        state.position = x0 + k2_x * (dt / 2.0f);
        state.velocity = v0 + k2_v * (dt / 2.0f);
        state.orientation = AddQuaternions(q0, ScaleQuaternion(k2_q, dt / 2.0f));
        state.angularVelocity = w0 + k2_w * (dt / 2.0f);
        state.UpdateDerivedQuantities();

        Vector3 k3_v = force / state.mass;
        Vector3 k3_x = state.velocity;
        Vector3 k3_w = state.MultiplyMatrixVector(state.inverseInertiaTensor, torque);
        Quaternion k3_q = DerivativeQuaternion(state.orientation, state.angularVelocity);

        // k4
        state.position = x0 + k3_x * dt;
        state.velocity = v0 + k3_v * dt;
        state.orientation = AddQuaternions(q0, ScaleQuaternion(k3_q, dt));
        state.angularVelocity = w0 + k3_w * dt;
        state.UpdateDerivedQuantities();

        Vector3 k4_v = force / state.mass;
        Vector3 k4_x = state.velocity;
        Vector3 k4_w = state.MultiplyMatrixVector(state.inverseInertiaTensor, torque);
        Quaternion k4_q = DerivativeQuaternion(state.orientation, state.angularVelocity);

        // Final update - Chapitre 2 Partie 1, page 21
        state.position = x0 + (dt / 6.0f) * (k1_x + 2.0f * k2_x + 2.0f * k3_x + k4_x);
        state.velocity = v0 + (dt / 6.0f) * (k1_v + 2.0f * k2_v + 2.0f * k3_v + k4_v);
        state.angularVelocity = w0 + (dt / 6.0f) * (k1_w + 2.0f * k2_w + 2.0f * k3_w + k4_w);

        // Quaternion integration
        Quaternion dq = ScaleQuaternion(
            AddQuaternions(
                AddQuaternions(k1_q, ScaleQuaternion(k2_q, 2.0f)),
                AddQuaternions(ScaleQuaternion(k3_q, 2.0f), k4_q)
            ),
            dt / 6.0f
        );
        state.orientation = AddQuaternions(q0, dq);
        state.orientation = NormalizeQuaternion(state.orientation);

        state.UpdateDerivedQuantities();
    }

    // Derivative of quaternion - Chapitre 1, page 57
    private static Quaternion DerivativeQuaternion(Quaternion q, Vector3 omega)
    {
        Quaternion omegaQ = new Quaternion(omega.x, omega.y, omega.z, 0);
        Quaternion result = MultiplyQuaternions(omegaQ, q);
        return new Quaternion(result.x * 0.5f, result.y * 0.5f, result.z * 0.5f, result.w * 0.5f);
    }

    private static Quaternion MultiplyQuaternions(Quaternion a, Quaternion b)
    {
        return new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
            a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z
        );
    }

    private static Quaternion AddQuaternions(Quaternion a, Quaternion b)
    {
        return new Quaternion(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
    }

    private static Quaternion ScaleQuaternion(Quaternion q, float scale)
    {
        return new Quaternion(q.x * scale, q.y * scale, q.z * scale, q.w * scale);
    }

    private static Quaternion NormalizeQuaternion(Quaternion q)
    {
        float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        return new Quaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag);
    }
}
