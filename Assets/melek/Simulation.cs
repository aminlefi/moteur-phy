using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simulates a plate falling on a ball and shattering upon impact,
/// using rigid body physics consistent with the course content.
/// </summary>
public class PlateSimulation : MonoBehaviour
{
    [Header("Simulation Parameters")]
    public int fragmentsX = 6;
    public int fragmentsZ = 6;
    public float fragmentSize = 0.25f;
    public float plateHeight = 3.0f;
    public float fragmentMass = 0.1f;
    public float groundY = 0.0f;

    [Header("Ball Settings")]
    public float ballRadius = 0.5f;
    public float ballMass = 10f;
    public PhysicsMaterial ballPhysicsMaterial;

    [Header("Visuals")]
    public Material fragmentMaterial;
    public Material ballMaterial;
    public Material groundMaterial;

    private List<GameObject> fragments = new List<GameObject>();
    private GameObject ball;
    private GameObject plateParent;
    private bool hasBroken = false;

    void Start()
    {
        CreateGround();
        CreateBall();
        CreateSolidPlate(); // Start as one rigid plate
    }

    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, groundY, 0);
        if (groundMaterial)
            ground.GetComponent<Renderer>().material = groundMaterial;

        Rigidbody rb = ground.AddComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void CreateBall()
    {
        ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.localScale = Vector3.one * (ballRadius * 2f);
        ball.transform.position = new Vector3(0, groundY + ballRadius, 0);
        if (ballMaterial)
            ball.GetComponent<Renderer>().material = ballMaterial;

        Rigidbody rb = ball.AddComponent<Rigidbody>();
        rb.mass = ballMass;
        rb.useGravity = true;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.05f;

        if (ballPhysicsMaterial)
            ball.GetComponent<Collider>().material = ballPhysicsMaterial;
    }

    void CreateSolidPlate()
    {
        // Create a single solid cube representing the plate before breaking
        plateParent = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plateParent.name = "Plate_Solid";
        float totalWidth = fragmentsX * fragmentSize;
        float totalDepth = fragmentsZ * fragmentSize;
        plateParent.transform.localScale = new Vector3(totalWidth, 0.05f, totalDepth);
        plateParent.transform.position = new Vector3(0, plateHeight, 0);

        if (fragmentMaterial)
            plateParent.GetComponent<Renderer>().material = fragmentMaterial;

        Rigidbody rb = plateParent.AddComponent<Rigidbody>();
        rb.mass = fragmentsX * fragmentsZ * fragmentMass;
        rb.useGravity = true;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.05f;

        // Add collision trigger for breaking
        plateParent.AddComponent<PlateImpactDetector>().Initialize(this);
    }

    public void BreakPlate(Vector3 contactPoint)
    {
        if (hasBroken) return;
        hasBroken = true;

        // Remove the solid plate
        Vector3 basePos = plateParent.transform.position;
        Vector3 baseScale = plateParent.transform.localScale;
        Destroy(plateParent);

        // Create small fragments at the same position
        float offsetX = baseScale.x * 0.5f;
        float offsetZ = baseScale.z * 0.5f;

        for (int x = 0; x < fragmentsX; x++)
        {
            for (int z = 0; z < fragmentsZ; z++)
            {
                GameObject frag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frag.name = $"Fragment_{x}_{z}";
                frag.transform.localScale = new Vector3(fragmentSize, 0.05f, fragmentSize);
                frag.transform.position = new Vector3(
                    basePos.x + (x * fragmentSize - offsetX + fragmentSize / 2),
                    basePos.y,
                    basePos.z + (z * fragmentSize - offsetZ + fragmentSize / 2)
                );

                if (fragmentMaterial)
                    frag.GetComponent<Renderer>().material = fragmentMaterial;

                Rigidbody rb = frag.AddComponent<Rigidbody>();
                rb.mass = fragmentMass;
                rb.useGravity = true;
                rb.linearDamping = 0.05f;
                rb.angularDamping = 0.05f;

                // Add explosion-like force from contact point (moment du couple)
                Vector3 dir = (frag.transform.position - contactPoint).normalized;
                rb.AddForce(dir * Random.Range(1f, 3f), ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

                fragments.Add(frag);
            }
        }
    }

    void Update()
    {
        // Reset simulation
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetSimulation();
        }
    }

    void ResetSimulation()
    {
        // Clean old objects
        foreach (GameObject frag in fragments)
            if (frag != null) Destroy(frag);
        fragments.Clear();

        if (plateParent != null)
            Destroy(plateParent);

        hasBroken = false;

        // Reset ball position
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        ball.transform.position = new Vector3(0, groundY + ballRadius, 0);

        // Recreate solid plate
        CreateSolidPlate();
    }
}

/// <summary>
/// Detects when the solid plate hits the ball or ground, then triggers the breaking.
/// </summary>
public class PlateImpactDetector : MonoBehaviour
{
    private PlateSimulation simulation;

    public void Initialize(PlateSimulation sim)
    {
        simulation = sim;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Contains("Ball") || collision.gameObject.name.Contains("Ground"))
        {
            // Break when first impact happens
            simulation.BreakPlate(collision.contacts[0].point);
        }
    }
}