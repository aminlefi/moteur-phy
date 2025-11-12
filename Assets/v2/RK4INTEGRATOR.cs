// RK4Integrator.cs
using UnityEngine;

/// <summary>
/// RK4 integrator for a second-order system:
/// dx/dt = v
/// dv/dt = a(t, x, v)
/// 
/// This follows the RK4 formulas from the PDF chapter (used for the mass-spring / RK4 examples).
/// </summary>
public static class RK4Integrator
{
    public struct State
    {
        public Vector3 x; // position
        public Vector3 v; // velocity

        public State(Vector3 pos, Vector3 vel) { x = pos; v = vel; }
    }

    public struct Derivative
    {
        public Vector3 dx; // derivative of position = velocity
        public Vector3 dv; // derivative of velocity = acceleration
    }

    // Delegate for acceleration: a = a(t, x, v)
    public delegate Vector3 AccelerationFunc(float t, Vector3 x, Vector3 v);

    // Evaluate derivative
    private static Derivative Evaluate(State state, float t, float dt, Derivative d, AccelerationFunc acc)
    {
        State s;
        s.x = state.x + d.dx * dt;
        s.v = state.v + d.dv * dt;

        Derivative outD;
        outD.dx = s.v;
        outD.dv = acc(t + dt, s.x, s.v);
        return outD;
    }

    // Integrate one step with RK4
    public static State Integrate(State initial, float t, float dt, AccelerationFunc acc)
    {
        Derivative a = new Derivative { dx = initial.v, dv = acc(t, initial.x, initial.v) };
        Derivative b = Evaluate(initial, t, dt * 0.5f, a, acc);
        Derivative c = Evaluate(initial, t, dt * 0.5f, b, acc);
        Derivative d = Evaluate(initial, t, dt, c, acc);

        Vector3 dxdt = (1.0f / 6.0f) * (a.dx + 2f * (b.dx + c.dx) + d.dx);
        Vector3 dvdt = (1.0f / 6.0f) * (a.dv + 2f * (b.dv + c.dv) + d.dv);

        State newState;
        newState.x = initial.x + dxdt * dt;
        newState.v = initial.v + dvdt * dt;

        return newState;
    }
}
