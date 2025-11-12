using System.Collections.Generic;
using UnityEngine;

public class PlateBreakingSimulation : MonoBehaviour
{
    [Header("Simulation Parameters")]
    public int fragmentsX = 5;
    public int fragmentsZ = 5;
    public float fragmentSize = 0.2f;
    public float plateHeight = 3.0f;
    public float ballRadius = 0.5f;
    public float fragmentMass = 0.1f;
    public float ballMass = 10.0f;

    [Header("Physics Parameters")]
    public float gravity = 9.81f;
    public float breakThreshold = 5.0f;
    public float timeStep = 0.01f;

    // Physics state
    private RigidBodyState ballState;
    private List<RigidBodyState> fragmentStates = new List<RigidBodyState>();
    private List<GameObject> fragmentObjects = new List<GameObject>();
    private GameObject ballObject;

    // Ground plane: Y = 0, normal = (0,1,0)
    private Vector3 groundNormal = Vector3.up;
    private float groundDistance = 0.0f;

    void Start()
    {
        InitializeBall();
        InitializePlateFragments();
    }

    void InitializeBall()
    {
        // Create ball visual
        ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObject.name = "Ball";
        ballObject.transform.localScale = Vector3.one * ballRadius * 2.0f;
        DestroyImmediate(ballObject.GetComponent<Collider>());

        // Create physics state - Chapitre 2 Partie 2
        ballState = new RigidBodyState(ballMass, new Vector3(0, ballRadius, 0), 0.3f);
        ballState.SetSphericalInertiaTensor(ballRadius);
        ballState.UpdateDerivedQuantities();
    }

    void InitializePlateFragments()
    {
        float plateWidth = fragmentsX * fragmentSize;
        float plateDepth = fragmentsZ * fragmentSize;

        for (int x = 0; x < fragmentsX; x++)
        {
            for (int z = 0; z < fragmentsZ; z++)
            {
                // Calculate position
                Vector3 pos = new Vector3(
                    (x - fragmentsX / 2.0f) * fragmentSize,
                    plateHeight,
                    (z - fragmentsZ / 2.0f) * fragmentSize
                );

                // Create visual object
                GameObject frag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frag.name = $"Fragment_{x}_{z}";
                frag.transform.localScale = Vector3.one * fragmentSize * 0.9f;
                frag.transform.position = pos;
                DestroyImmediate(frag.GetComponent<Collider>());
                fragmentObjects.Add(frag);

                // Create physics state
                RigidBodyState state = new RigidBodyState(fragmentMass, pos, 0.2f);
                Vector3 size = Vector3.one * fragmentSize;
                state.SetBoxInertiaTensor(size);
                state.UpdateDerivedQuantities();
                fragmentStates.Add(state);
            }
        }
    }

    void FixedUpdate()
    {
        float dt = timeStep;

        // Update ball physics
        UpdateRigidBody(ballState, dt);

        // Update fragment physics
        foreach (var fragState in fragmentStates)
        {
            UpdateRigidBody(fragState, dt);
        }

        // Collision detection and response
        HandleCollisions();

        // Update visual representations
        UpdateVisuals();
    }

    void UpdateRigidBody(RigidBodyState state, float dt)
    {
        // Apply gravity - Chapitre 2 Partie 1, page 6
        Vector3 force = new Vector3(0, -gravity * state.mass, 0);
        Vector3 torque = Vector3.zero;

        // Integrate using RK4 - Chapitre 2 Partie 1, page 14-21
        PhysicsIntegrator.IntegrateRK4(state, force, torque, dt);
    }

    void HandleCollisions()
    {
        // Ball-ground collision - Chapitre 4, page 8-11
        Vector3 contactPoint, contactNormal;
        if (CollisionDetector.DetectSpherePlaneCollision(ballState.position, ballRadius,
            groundNormal, groundDistance, out contactPoint, out contactNormal))
        {
            CollisionResponse.ApplyCollisionImpulse(ballState, null, contactPoint, contactNormal, true);
        }

        // Fragment-ground collisions
        for (int i = 0; i < fragmentStates.Count; i++)
        {
            var frag = fragmentStates[i];
            float fragRadius = fragmentSize * 0.5f;

            if (CollisionDetector.DetectSpherePlaneCollision(frag.position, fragRadius,
                groundNormal, groundDistance, out contactPoint, out contactNormal))
            {
                CollisionResponse.ApplyCollisionImpulse(frag, null, contactPoint, contactNormal, true);
            }

            // Fragment-ball collision - Chapitre 4, page 23
            if (CollisionDetector.DetectSphereSphereCollision(frag.position, fragRadius,
                ballState.position, ballRadius, out contactPoint, out contactNormal))
            {
                CollisionResponse.ApplyCollisionImpulse(frag, ballState, contactPoint, contactNormal, false);
            }
        }

        // Fragment-fragment collisions
        for (int i = 0; i < fragmentStates.Count; i++)
        {
            for (int j = i + 1; j < fragmentStates.Count; j++)
            {
                float fragRadius = fragmentSize * 0.5f;
                if (CollisionDetector.DetectSphereSphereCollision(
                    fragmentStates[i].position, fragRadius,
                    fragmentStates[j].position, fragRadius,
                    out contactPoint, out contactNormal))
                {
                    CollisionResponse.ApplyCollisionImpulse(fragmentStates[i], fragmentStates[j],
                        contactPoint, contactNormal, false);
                }
            }
        }
    }

    void UpdateVisuals()
    {
        // Update ball visual
        ballObject.transform.position = ballState.position;
        ballObject.transform.rotation = ballState.orientation;

        // Update fragment visuals
        for (int i = 0; i < fragmentObjects.Count; i++)
        {
            fragmentObjects[i].transform.position = fragmentStates[i].position;
            fragmentObjects[i].transform.rotation = fragmentStates[i].orientation;
        }
    }
}
