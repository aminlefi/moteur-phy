// PlateSimulationRK4.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plate simulation using RK4 integrator for the falling plate (pre-break),
/// then spawns fragments with Unity rigidbodies on impact (post-break).
/// Implements the RK4 example pattern from the PDF.
/// </summary>
public class PlateSimulationRK4 : MonoBehaviour
{
    [Header("Plate & Fragmentation")]
    public int fragmentsX = 6;
    public int fragmentsZ = 6;
    public float fragmentSize = 0.25f;
    public float plateThickness = 0.05f;
    public float plateHeight = 3.0f;
    public float fragmentMass = 0.1f;

    [Header("Ball")]
    public float ballRadius = 0.5f;
    public float ballMass = 10f;
    public PhysicsMaterial ballPhysMat;

    [Header("Visuals")]
    public Material fragmentMaterial;
    public Material ballMaterial;
    public Material groundMaterial;

    [Header("Physical")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    [Tooltip("Linear damping (viscous) applied while integrating the plate (pre-break).")]
    public float linearDamping = 0.05f;

    [Header("Impulse")]
    [Tooltip("Multiplier for fragment impulse after break (models moment du couple).")]
    public float fragmentImpulseStrength = 3.0f;
    [Tooltip("Random torque multiplier for fragments")]
    public float fragmentTorqueStrength = 2.0f;

    // Internals
    private GameObject ground;
    private GameObject ball;
    private GameObject plateKinematic; // plate before breaking (moved by RK4)
    private List<GameObject> fragments = new List<GameObject>();
    private bool hasBroken = false;

    // RK4 state for plate (treat plate as point mass at its center of mass)
    private RK4Integrator.State plateState;
    private float simTime = 0f;

    void Start()
    {
        CreateGround();
        CreateBall();
        CreatePlateKinematic();

        // Initialize plate state (position = plate center)
        Vector3 platePos = new Vector3(0, plateHeight, 0);
        plateState = new RK4Integrator.State(platePos, Vector3.zero);
    }

    void CreateGround()
    {
        ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, 0f, 0);
        if (groundMaterial) ground.GetComponent<Renderer>().material = groundMaterial;
        // make static
        var rb = ground.AddComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void CreateBall()
    {
        ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.localScale = Vector3.one * (ballRadius * 2f);
        ball.transform.position = new Vector3(0, ballRadius, 0);
        if (ballMaterial) ball.GetComponent<Renderer>().material = ballMaterial;

        Rigidbody rb = ball.AddComponent<Rigidbody>();
        rb.mass = ballMass;
        rb.useGravity = true;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.05f;

        Collider col = ball.GetComponent<Collider>();
        if (ballPhysMat != null) col.material = ballPhysMat;
    }

    void CreatePlateKinematic()
    {
        float totalWidth = fragmentsX * fragmentSize;
        float totalDepth = fragmentsZ * fragmentSize;

        plateKinematic = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plateKinematic.name = "Plate_Kinematic";
        plateKinematic.transform.localScale = new Vector3(totalWidth, plateThickness, totalDepth);
        plateKinematic.transform.position = new Vector3(0, plateHeight, 0);
        if (fragmentMaterial) plateKinematic.GetComponent<Renderer>().material = fragmentMaterial;

        // Remove any Rigidbody (we will move it manually via RK4)
        Rigidbody rb = plateKinematic.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // Keep collider so we can compute bounds/contact geometry if needed
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (!hasBroken)
        {
            // Integrate plate translation using RK4 (plate treated as point mass)
            plateState = RK4Integrator.Integrate(plateState, simTime, dt, PlateAcceleration);
            simTime += dt;

            // Apply small linear damping (amortissement) as described in PDF
            plateState.v *= Mathf.Clamp01(1f - linearDamping * dt);

            // Update plate transform (keep orientation unchanged, but could later implement rotation)
            plateKinematic.transform.position = plateState.x;

            // Collision detection: plate bottom vs ball top (simple geometric test)
            float plateBottomY = plateKinematic.transform.position.y - (plateThickness * 0.5f);
            float ballTopY = ball.transform.position.y + ballRadius;
            if (plateBottomY <= ballTopY)
            {
                // contact detected; compute approximate contact point on surface directly above the ball center
                Vector3 contactPoint = new Vector3(ball.transform.position.x, plateBottomY, ball.transform.position.z);
                BreakPlate(contactPoint);
            }

            // Also break on hitting the ground
            if (plateBottomY <= 0f)
            {
                Vector3 contactPoint = new Vector3(plateKinematic.transform.position.x, 0f, plateKinematic.transform.position.z);
                BreakPlate(contactPoint);
            }
        }

        // Reset
        if (Input.GetKeyDown(KeyCode.R)) ResetSimulation();
    }

    // Acceleration for the plate: gravity + (simple linear drag)
    private Vector3 PlateAcceleration(float t, Vector3 x, Vector3 v)
    {
        // Using Newton's second law in point-mass form:
        // a = (sum forces) / m. We're simulating only gravity + viscous damping (amortissement).
        // Since we don't need to divide by mass when acceleration doesn't depend on mass directly, we can return g + (-c/m)*v
        // But because the plate's mass is not used here explicitly we implement damping as acceleration-proportional term
        Vector3 a = gravity - linearDamping * v;
        return a;
    }

    // Break plate into fragments and give impulses based on contact point (uses AddForceAtPosition to create torque)
    public void BreakPlate(Vector3 contactPoint)
    {
        if (hasBroken) return;
        hasBroken = true;

        // Keep reference to old plate center and scale
        Vector3 basePos = plateKinematic.transform.position;
        Vector3 baseScale = plateKinematic.transform.localScale;
        float totalWidth = baseScale.x;
        float totalDepth = baseScale.z;

        // Destroy the kinematic plate
        Destroy(plateKinematic);

        // Compute offsets so fragments tile same area
        float offsetX = totalWidth * 0.5f;
        float offsetZ = totalDepth * 0.5f;

        for (int x = 0; x < fragmentsX; x++)
        {
            for (int z = 0; z < fragmentsZ; z++)
            {
                GameObject frag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frag.name = $"Fragment_{x}_{z}";
                frag.transform.localScale = new Vector3(fragmentSize, plateThickness, fragmentSize);

                // position centered on the same plane
                Vector3 fragPos = new Vector3(
                    basePos.x + (x * fragmentSize - offsetX + fragmentSize * 0.5f),
                    basePos.y,
                    basePos.z + (z * fragmentSize - offsetZ + fragmentSize * 0.5f)
                );
                frag.transform.position = fragPos;

                if (fragmentMaterial) frag.GetComponent<Renderer>().material = fragmentMaterial;

                // Add Rigidbody so Unity handles collisions/rotation for fragments
                Rigidbody rb = frag.AddComponent<Rigidbody>();
                rb.mass = fragmentMass;
                rb.linearDamping = 0.05f;
                rb.angularDamping = 0.05f;

                // Compute a force impulse based on distance from contact point to create moment (torque) effects
                Vector3 dir = (fragPos - contactPoint);
                float dist = dir.magnitude;
                Vector3 pushDir = dir.normalized;

                // Impulse reduces with distance and adds randomness (mimic shattering)
                float impulseMagnitude = fragmentImpulseStrength * Mathf.Exp(-dist * 3f) * Random.Range(0.8f, 1.2f);

                // Use AddForceAtPosition to create both translation and rotation (moment du couple)
                Vector3 impulse = pushDir * impulseMagnitude;
                rb.AddForceAtPosition(impulse, contactPoint, ForceMode.Impulse);

                // Small random torque
                rb.AddTorque(Random.insideUnitSphere * fragmentTorqueStrength, ForceMode.Impulse);

                fragments.Add(frag);
            }
        }

        // Optionally give the ball a small reaction impulse away from contact (conservation of momentum spirit)
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            Vector3 reaction = (ball.transform.position - contactPoint).normalized * 0.5f;
            ballRb.AddForce(reaction, ForceMode.Impulse);
        }
    }

    void ResetSimulation()
    {
        // Clear fragments
        foreach (var f in fragments) if (f != null) Destroy(f);
        fragments.Clear();

        // Recreate plate
        CreatePlateKinematic();
        plateState = new RK4Integrator.State(new Vector3(0, plateHeight, 0), Vector3.zero);
        simTime = 0f;
        hasBroken = false;

        // Reset ball
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        ball.transform.position = new Vector3(0, ballRadius, 0);
    }
}
