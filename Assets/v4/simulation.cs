// Attach to an empty GameObject named "SimulationManager" in your Unity scene

using UnityEngine;
using System.Collections.Generic;

public class FragmentedPlateSimulation : MonoBehaviour
{
    public int fragRows = 2, fragCols = 2;
    public float plateWidth = 2f, plateHeight = 0.2f, plateDepth = 2f;
    public float plateStartY = 5f;

    public float ballRadius = 0.5f;
    public Vector3 ballPos = new Vector3(0, 0.5f, 0);

    public float gravity = -9.81f;
    public float springK = 1000f;
    public float breakThreshold = 0.5f;
    public float fragMass = 1f;

    List<Fragment> fragments = new List<Fragment>();
    List<Constraint> constraints = new List<Constraint>();
    GameObject groundObj, ballObj;

    void Start() { InitializeSimulation(); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetSimulation();
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        foreach (var frag in fragments)
        {
            frag.force = Vector3.zero;
            frag.ApplyForce(new Vector3(0, gravity * frag.mass, 0));
        }

        foreach (var c in constraints.ToArray()) c.Resolve();

        foreach (var frag in fragments) frag.Step(dt);

        foreach (var frag in fragments) HandleCollisions(frag);

        foreach (var frag in fragments) frag.UpdateRenderObj();
    }

    void InitializeSimulation()
    {
        if (groundObj == null) CreateGround();
        if (ballObj == null) CreateBall();
        CreateFragments();
        CreateConstraints();
    }

    void ResetSimulation()
    {
        // Destroy all fragment GameObjects
        foreach (var frag in fragments)
            if (frag.renderObj) Destroy(frag.renderObj);

        // Clear old
        fragments.Clear();
        constraints.Clear();

        // Reset ball and ground to visual defaults
        if (groundObj) groundObj.transform.position = new Vector3(0, -0.5f, 0);
        if (ballObj)
        {
            ballObj.transform.position = ballPos;
            ballObj.transform.localScale = Vector3.one * (2 * ballRadius);
        }

        CreateFragments();
        CreateConstraints();
    }

    void CreateGround()
    {
        groundObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        groundObj.transform.position = new Vector3(0, -0.5f, 0);
        groundObj.transform.localScale = new Vector3(10, 1, 10);
        groundObj.GetComponent<MeshRenderer>().material.color = Color.green;
    }

    void CreateBall()
    {
        ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObj.transform.position = ballPos;
        ballObj.transform.localScale = Vector3.one * (2 * ballRadius);
        ballObj.GetComponent<MeshRenderer>().material.color = Color.red;
    }

    void CreateFragments()
    {
        float dx = plateWidth / fragCols;
        float dz = plateDepth / fragRows;
        float y = plateStartY;
        for (int i = 0; i < fragRows; i++)
        {
            for (int j = 0; j < fragCols; j++)
            {
                float x = (j + 0.5f) * dx - plateWidth / 2;
                float z = (i + 0.5f) * dz - plateDepth / 2;

                Vector3 initPos = new Vector3(x, y, z);
                Vector3 size = new Vector3(dx * 0.95f, plateHeight, dz * 0.95f);
                Matrix4x4 initRot = Matrix4x4.identity;

                GameObject rend = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rend.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.gray, Color.white, Random.value);

                fragments.Add(new Fragment(initPos, initRot, size, fragMass, rend));
            }
        }
    }

    void CreateConstraints()
    {
        int w = fragCols, h = fragRows;
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                int idx = i * w + j;
                var a = fragments[idx];

                if (j < w - 1)
                {
                    var b = fragments[i * w + (j + 1)];
                    constraints.Add(new Constraint(a, b, springK, breakThreshold));
                }
                if (i < h - 1)
                {
                    var b = fragments[(i + 1) * w + j];
                    constraints.Add(new Constraint(a, b, springK, breakThreshold));
                }
            }
        }
    }

    void HandleCollisions(Fragment frag)
    {
        Vector3 toFrag = frag.position - ballPos;
        float minDist = ballRadius + 0.5f * frag.size.y;
        if (toFrag.magnitude < minDist)
        {
            Vector3 norm = toFrag.normalized;
            frag.velocity = frag.velocity - 2 * Vector3.Dot(frag.velocity, norm) * norm;
            frag.position = ballPos + norm * minDist;
        }
        if (frag.position.y - 0.5f * frag.size.y < 0)
        {
            frag.position.y = 0.5f * frag.size.y;
            if (frag.velocity.y < 0) frag.velocity.y = -frag.velocity.y * 0.2f;
        }
    }

    static Quaternion MatrixToQuaternion(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    public class Fragment
    {
        public Vector3 position;
        public Matrix4x4 rotation;
        public Vector3 size;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public float mass;
        public Vector3 force;
        public GameObject renderObj;

        public Fragment(Vector3 pos, Matrix4x4 rot, Vector3 sz, float m, GameObject rend)
        {
            position = pos;
            rotation = rot;
            size = sz;
            mass = m;
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            force = Vector3.zero;
            renderObj = rend;
        }

        public void ApplyForce(Vector3 f) { force += f; }
        public void ApplyImpulse(Vector3 impulse) { velocity += impulse / mass; }
        public void Step(float dt)
        {
            velocity += (force / mass) * dt;
            position += velocity * dt;
            if (angularVelocity != Vector3.zero)
            {
                float angle = angularVelocity.magnitude * dt;
                if (angle != 0)
                {
                    Vector3 axis = angularVelocity.normalized;
                    Matrix4x4 rot = Matrix4x4.Rotate(Quaternion.AngleAxis(Mathf.Rad2Deg * angle, axis));
                    rotation = rot * rotation;
                }
            }
            force = Vector3.zero;
        }
        public void UpdateRenderObj()
        {
            if (renderObj)
            {
                renderObj.transform.position = position;
                renderObj.transform.rotation = MatrixToQuaternion(rotation);
                renderObj.transform.localScale = size;
            }
        }
    }

    public class Constraint
    {
        Fragment a, b;
        float k, restLength, breakDist;
        bool broken = false;
        public Constraint(Fragment a, Fragment b, float k, float breakDist)
        {
            this.a = a; this.b = b; this.k = k; this.breakDist = breakDist;
            restLength = (a.position - b.position).magnitude;
        }
        public void Resolve()
        {
            if (broken) return;
            Vector3 delta = b.position - a.position;
            float dist = delta.magnitude;
            float x = dist - restLength;
            Vector3 dir = (dist == 0) ? Vector3.up : delta.normalized;

            Vector3 F = k * x * dir;
            a.ApplyForce(F);
            b.ApplyForce(-F);

            if (Mathf.Abs(x) > breakDist)
            {
                float E = 0.5f * k * x * x;
                Vector3 impulse = E * dir.normalized;
                a.ApplyImpulse(-impulse);
                b.ApplyImpulse(impulse);
                broken = true;
            }
        }
    }
}
