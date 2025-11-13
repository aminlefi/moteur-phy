// PlateSimulationManual.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlateSimulationManual
/// - Pre-fractured plate (grid of fragments), springs between neighbors (manually computed).
/// - RK4 for linear integration (per fragment).
/// - Manual angular integration (as orientation quaternion and constructing 4x4 transform matrices).
/// - Spring rupture based on stretch; energy converted to impulses using Δv = sqrt(2E/m).
/// - Manual collision with static ball and ground plane (y=0).
/// - Rendering via Graphics.DrawMesh using computed 4x4 matrices (no Transform modification for physics).
/// </summary>
public class PlateSimulationManual : MonoBehaviour
{
    [Header("Plate geometry")]
    public int fragmentsX = 6;
    public int fragmentsZ = 6;
    public float fragmentSize = 0.25f;
    public float plateThickness = 0.05f;
    public float plateInitialHeight = 3.0f;

    [Header("Shard physical")]
    public float fragmentMass = 0.2f;
    public float linearDamping = 0.04f; // viscous damping
    public float angularDamping = 0.02f;

    [Header("Spring constraints")]
    public float springK = 5000f;
    public float springBreakStretch = 0.08f; // absolute extra length (meters) beyond rest which breaks
    public float springRestMultiplier = 1f; // rest length equals tile distance normally (1 * tile)
    public float springDamping = 5f; // dashpot term for springs

    [Header("Collision")]
    public Vector3 ballCenter = new Vector3(0f, 0.5f, 0f);
    public float ballRadius = 0.5f;
    [Range(0f, 1f)] public float restitution = 0.45f;
    [Range(0f, 1f)] public float friction = 0.3f;

    [Header("Rendering")]
    public Material fragmentMaterial;
    public Material ballMaterial;

    [Header("Simulation")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public bool autoStart = true;
    [Header("Impulse settings")]
    [Tooltip("Multiplier to control the impulse velocity applied when springs break (how violently shards fly apart).")]
    public float impulseMultiplier = 1f;


    // Internals
    private Mesh cubeMesh;
    private Mesh sphereMesh;

    private List<Fragment> fragments = new List<Fragment>();
    private List<Spring> springs = new List<Spring>();

    private float simTime = 0f;
    private bool running = false;

    // ---------------------------
    // Fragment and Spring types
    // ---------------------------
    private class Fragment
    {
        public int idxX, idxZ;
        public float size;
        public float mass;
        public Matrix4x4 localToWorld; // computed each frame
        public Vector3 position; // center
        public Vector3 velocity;
        public Quaternion orientation;
        public Vector3 angularVelocity; // world-space angular velocity vector (rad/s)
        public float invMass;
        public Matrix4x4 inertiaTensorBody; // body-space inertia tensor matrix
        public Matrix4x4 invInertiaBody; // inverse body inertia
        public bool isStatic = false; // can be set true for anchored shards if needed

        // For RK4 integration we store state:
        public rkintegrator.State linearState;
    }

    private class Spring
    {
        public int a, b; // fragment indices into fragments list
        public float restLength;
        public float k;
        public bool broken;
    }

    void Start()
    {
        // build simple cube mesh and sphere mesh for drawing
        cubeMesh = CreateCubeMesh();
        sphereMesh = CreateSphereMesh();

        BuildPlatePreFractured();

        // Build springs between orthogonal neighbors (grid)
        CreateGridSprings();

        if (autoStart) running = true;
    }

    void Update()
    {
        if (!running) return;

        float dt = Time.deltaTime;
        // clamp dt a bit for stability
        dt = Mathf.Min(dt, 0.02f);

        // Step simulation: we integrate springs / forces -> linear motion RK4 + rotation semi-implicit.
        SimulateStep(dt);
        simTime += dt;

        // Render fragments and ball
        RenderScene();
        // Reset on R
        if (Input.GetKeyDown(KeyCode.R)) ResetSimulation();
    }

    // ---------------------------
    // Initialization helpers
    // ---------------------------
    void BuildPlatePreFractured()
    {
        fragments.Clear();

        float totalWidth = fragmentsX * fragmentSize;
        float totalDepth = fragmentsZ * fragmentSize;
        Vector3 baseCenter = new Vector3(0f, plateInitialHeight, 0f);

        for (int ix = 0; ix < fragmentsX; ix++)
        {
            for (int iz = 0; iz < fragmentsZ; iz++)
            {
                Fragment f = new Fragment();
                f.idxX = ix;
                f.idxZ = iz;
                f.size = fragmentSize;
                f.mass = fragmentMass;
                f.invMass = 1.0f / f.mass;
                // center positions tile the plane
                float offX = (ix + 0.5f) * fragmentSize - totalWidth * 0.5f;
                float offZ = (iz + 0.5f) * fragmentSize - totalDepth * 0.5f;
                f.position = baseCenter + new Vector3(offX, 0f, offZ);
                f.velocity = Vector3.zero;
                f.orientation = Quaternion.identity;
                f.angularVelocity = Vector3.zero;

                // prepare inertia tensor for a rectangular box (centered at COM)
                // Ix = 1/12 m (h^2 + d^2)
                float w = fragmentSize;
                float h = plateThickness;
                float d = fragmentSize;
                float Ix = (1f / 12f) * f.mass * (h * h + d * d);
                float Iy = (1f / 12f) * f.mass * (w * w + d * d);
                float Iz = (1f / 12f) * f.mass * (w * w + h * h);

                // body-space inertia as diagonal matrix
                Matrix4x4 Ibody = Matrix4x4.zero;
                Ibody.m00 = Ix; Ibody.m11 = Iy; Ibody.m22 = Iz; Ibody.m33 = 1f;
                f.inertiaTensorBody = Ibody;

                Matrix4x4 invI = Matrix4x4.zero;
                invI.m00 = Ix > 0 ? 1f / Ix : 0f;
                invI.m11 = Iy > 0 ? 1f / Iy : 0f;
                invI.m22 = Iz > 0 ? 1f / Iz : 0f;
                invI.m33 = 1f;
                f.invInertiaBody = invI;

                f.linearState = new rkintegrator.State(f.position, f.velocity);

                // compute initial localToWorld
                f.localToWorld = ComputeMatrix(f.position, f.orientation, new Vector3(w, h, d));

                fragments.Add(f);
            }
        }
    }

    void CreateGridSprings()
    {
        springs.Clear();
        int cols = fragmentsX;
        int rows = fragmentsZ;

        for (int ix = 0; ix < cols; ix++)
        {
            for (int iz = 0; iz < rows; iz++)
            {
                int idx = ix * rows + iz;
                Fragment a = fragments[idx];

                // neighbor +x
                if (ix + 1 < cols)
                {
                    int idxB = (ix + 1) * rows + iz;
                    Spring s = new Spring();
                    s.a = idx;
                    s.b = idxB;
                    s.k = springK;
                    // rest length is distance between initial centers
                    s.restLength = (fragments[idxB].position - a.position).magnitude * springRestMultiplier;
                    s.broken = false;
                    springs.Add(s);
                }
                // neighbor +z
                if (iz + 1 < rows)
                {
                    int idxB = ix * rows + (iz + 1);
                    Spring s = new Spring();
                    s.a = idx;
                    s.b = idxB;
                    s.k = springK;
                    s.restLength = (fragments[idxB].position - a.position).magnitude * springRestMultiplier;
                    s.broken = false;
                    springs.Add(s);
                }
            }
        }
    }

    // ---------------------------
    // Simulation step
    // ---------------------------
    void SimulateStep(float dt)
    {
        int n = fragments.Count;

        // 1) Compute forces per fragment (reset)
        Vector3[] forces = new Vector3[n];
        Vector3[] torques = new Vector3[n];

        // gravity
        for (int i = 0; i < n; i++)
        {
            forces[i] = fragments[i].mass * gravity;
            torques[i] = Vector3.zero;
        }

        // 2) Add spring forces (for unbroken springs)
        for (int si = 0; si < springs.Count; si++)
        {
            Spring s = springs[si];
            if (s.broken) continue;

            Fragment A = fragments[s.a];
            Fragment B = fragments[s.b];

            Vector3 delta = B.position - A.position;
            float len = delta.magnitude;
            if (len <= 1e-7f) continue;
            Vector3 nrm = delta / len;
            float x = len - s.restLength; // extension (can be negative if compressed)

            // relative velocity along the spring:
            Vector3 relVel = (B.velocity - A.velocity);
            float velAlong = Vector3.Dot(relVel, nrm);

            // Hooke + dashpot
            float forceScalar = -s.k * x - springDamping * velAlong;
            Vector3 f = forceScalar * nrm;

            // Apply to fragments
            forces[s.a] += f;
            forces[s.b] -= f;

            // Check break condition (absolute stretch exceed threshold)
            if (Mathf.Abs(x) > springBreakStretch)
            {
                // break: compute stored potential energy = 1/2 k x^2
                float storedE = 0.5f * s.k * x * x;

                float m = fragments[s.a].mass;

                // impulse magnitude with multiplier applied
                float dv = Mathf.Sqrt(Mathf.Max(0f, 2f * storedE / m)) * impulseMultiplier;

                Vector3 impulseDir = (x > 0f) ? nrm : -nrm;

                // apply instant impulse velocities
                A.velocity += (-impulseDir) * dv;
                B.velocity += (impulseDir) * dv;

                // small angular impulse (scaled)
                Vector3 contactRel = (A.position - B.position) * 0.5f;
                Vector3 torqueImp = Vector3.Cross(contactRel, impulseDir * dv * m);
                A.angularVelocity += torqueImp * 0.01f;
                B.angularVelocity -= torqueImp * 0.01f;

                s.broken = true;
            }

        }

        // 3) Integrate linear motion with RK4 per fragment (acceleration = forces/mass + per-fragment damping)
        for (int i = 0; i < n; i++)
        {
            Fragment f = fragments[i];

            if (f.isStatic) continue;

            // capture forces array for closure
            Vector3 fi = forces[i];
            // define acceleration func for this fragment
            rkintegrator.AccelerationFunc aFunc = (t, x, v) =>
            {
                // spring + gravity included via fi (we treat fi as position-independent for the integrator step).
                // Add linear viscous damping: a = F/m - c * v
                Vector3 a = fi * f.invMass - linearDamping * v;
                return a;
            };

            // integrate
            f.linearState = rkintegrator.Integrate(f.linearState, simTime, dt, aFunc);

            // update principal position & velocity
            f.position = f.linearState.x;
            f.velocity = f.linearState.v;
        }

        // 4) Handle pairwise collisions with ball and ground and apply velocity corrections / impulses
        for (int i = 0; i < n; i++)
        {
            Fragment f = fragments[i];
            if (f.isStatic) continue;

            // approximate fragment as sphere for collision handling
            float approxRadius = Mathf.Sqrt(3f * (f.size * f.size)) * 0.5f; // half diagonal
            // Ground collision (plane y = 0)
            float minY = f.position.y - approxRadius;
            if (minY < 0f)
            {
                // push out and reflect normal component
                float penetration = -minY;
                f.position.y += penetration;
                // compute normal impulse
                Vector3 nrm = Vector3.up;
                float vrel = Vector3.Dot(f.velocity, nrm);
                if (vrel < 0f)
                {
                    float j = -(1f + restitution) * vrel;
                    // apply impulse
                    f.velocity += nrm * j;
                    // apply friction (tangential)
                    Vector3 tang = f.velocity - Vector3.Dot(f.velocity, nrm) * nrm;
                    f.velocity -= tang * friction;
                }
            }

            // Ball collision (static)
            Vector3 rel = f.position - ballCenter;
            float dist = rel.magnitude;
            float minDist = ballRadius + approxRadius;
            if (dist < 1e-6f) dist = 1e-6f;
            if (dist < minDist)
            {
                Vector3 nrm = rel / dist;
                float penetration = minDist - dist;
                f.position += nrm * penetration; // push out
                // relative velocity along normal (ball static => just fragment)
                float vrel = Vector3.Dot(f.velocity, nrm);
                if (vrel < 0f)
                {
                    float j = -(1f + restitution) * vrel; // impulse scalar (mass considered in v)
                    // apply instantaneous velocity change (Δv = j)
                    f.velocity += nrm * j;
                    // friction impulse approximate
                    Vector3 tang = f.velocity - Vector3.Dot(f.velocity, nrm) * nrm;
                    f.velocity -= tang * friction;
                }
            }
        }

        // 5) Integrate rotations (semi-implicit Euler on angular velocity to orientation)
        for (int i = 0; i < n; i++)
        {
            Fragment f = fragments[i];
            if (f.isStatic) continue;

            // simple angular damping
            f.angularVelocity *= Mathf.Clamp01(1f - angularDamping * dt);

            // integrate orientation: q' = 0.5 * omega_quat * q
            Vector3 w = f.angularVelocity;
            Quaternion dq = new Quaternion(w.x * 0.5f * dt, w.y * 0.5f * dt, w.z * 0.5f * dt, 0f);
            Quaternion q = f.orientation;
            Quaternion qNew = new Quaternion(
                q.x + dq.x * q.w + dq.w * q.x + dq.y * q.z - dq.z * q.y,
                q.y + dq.y * q.w + dq.w * q.y + dq.z * q.x - dq.x * q.z,
                q.z + dq.z * q.w + dq.w * q.z + dq.x * q.y - dq.y * q.x,
                q.w + dq.w * q.w - (dq.x * q.x + dq.y * q.y + dq.z * q.z) // approximate
            );
            // normalize more robustly
            f.orientation = Quaternion.Normalize(qNew);

            // Update matrix for rendering
            Vector3 dims = new Vector3(f.size, plateThickness, f.size);
            f.localToWorld = ComputeMatrix(f.position, f.orientation, dims);
        }
    }

    // Compute a 4x4 transform matrix from position, quaternion, and scale (manual composition)
    static Matrix4x4 ComputeMatrix(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        // Compose S * R * T manually (we will do TRS order as usual)
        Matrix4x4 T = Matrix4x4.identity;
        T.m03 = pos.x; T.m13 = pos.y; T.m23 = pos.z;

        Matrix4x4 R = Matrix4x4.Rotate(rot); // allowed: building rotation matrix from quaternion
        Matrix4x4 S = Matrix4x4.Scale(scale);

        // localToWorld = T * R * S  (apply scale then rotation then translation)
        return T * R * S;
    }

    // ---------------------------
    // Rendering
    // ---------------------------
    void RenderScene()
    {
        // draw ball
        if (ballMaterial != null)
        {
            Matrix4x4 ballMat = ComputeMatrix(ballCenter, Quaternion.identity, Vector3.one * (ballRadius * 2f));
            Graphics.DrawMesh(sphereMesh, ballMat, ballMaterial, 0);
        }

        // draw each fragment via matrix using Graphics.DrawMesh (no transform physics)
        if (fragmentMaterial == null) return;
        for (int i = 0; i < fragments.Count; i++)
        {
            Fragment f = fragments[i];
            Graphics.DrawMesh(cubeMesh, f.localToWorld, fragmentMaterial, 0);
        }
    }

    // ---------------------------
    // Utility mesh builders
    // ---------------------------
    static Mesh CreateCubeMesh()
    {
        // unit cube centered at origin, size 1
        Mesh m = new Mesh();
        Vector3[] verts = {
            // front
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            // back
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f)
        };
        int[] tris = {
            0,2,1, 0,3,2, // front
            4,5,6, 4,6,7, // back
            4,0,1, 4,1,5, // bottom
            3,7,6, 3,6,2, // top
            1,2,6, 1,6,5, // right
            4,7,3, 4,3,0  // left
        };
        m.vertices = verts;
        m.triangles = tris;
        m.RecalculateNormals();
        return m;
    }

    static Mesh CreateSphereMesh()
    {
        // simple built-in approximate sphere generation using icosphere-ish pattern is longer;
        // we can rely on Unity built-in primitive sphere mesh via GameObject workaround for convenience of rendering.
        // But to avoid CreatePrimitive (teacher asked no premade), we create a low-poly UV sphere programmatically.
        Mesh m = new Mesh();

        int lon = 16;
        int lat = 12;
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int y = 0; y <= lat; y++)
        {
            float v = (float)y / lat;
            float phi = Mathf.PI * (v - 0.5f);
            for (int x = 0; x <= lon; x++)
            {
                float u = (float)x / lon;
                float theta = 2f * Mathf.PI * u;
                Vector3 p = new Vector3(
                    Mathf.Cos(phi) * Mathf.Cos(theta),
                    Mathf.Sin(phi),
                    Mathf.Cos(phi) * Mathf.Sin(theta)
                );
                verts.Add(p * 0.5f);
            }
        }

        for (int y = 0; y < lat; y++)
        {
            for (int x = 0; x < lon; x++)
            {
                int a = y * (lon + 1) + x;
                int b = a + lon + 1;
                tris.Add(a);
                tris.Add(b + 1);
                tris.Add(b);

                tris.Add(a);
                tris.Add(a + 1);
                tris.Add(b + 1);
            }
        }

        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        return m;
    }

    // ---------------------------
    // Utilities
    // ---------------------------
    void ResetSimulation()
    {
        BuildPlatePreFractured();
        CreateGridSprings();
        simTime = 0f;
        running = true;
    }
}
