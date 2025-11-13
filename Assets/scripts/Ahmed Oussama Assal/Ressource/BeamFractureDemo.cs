using UnityEngine;

// Controller-only: configures existing scene objects for the Beam fracture test.
// Uses consistent naming: Beam A and Beam B (no Left/Right wording), no instantiation, no Unity Transform usage.
[DisallowMultipleComponent]
public class BeamFractureDemo : MonoBehaviour
{
 [Header("World (optional)")]
 public SimplePhysicsWorld world; // optional reference; if null we'll FindObjectOfType at Start (no creation)
 public Vector3 gravity = new Vector3(0,-9.81f,0);
 public int substeps =8;
 public bool applyWorldSettings = true;

 [Header("Beam A (existing)")]
 public CustomTransform beamACT;
 public SimpleBody beamABody;

 [Header("Beam B (existing)")]
 public CustomTransform beamBCT;
 public SimpleBody beamBBody;

 [Header("Breakable constraint (existing in scene)")]
 public SimpleSpringConstraint constraint; // must already be present in scene
 public bool autoComputeAnchorsFromScale = true; // uses ct.scale.x *0.5 for X-anchors
 public float constraintRestLength =0.0f;
 public float springStiffness =4000f;
 public float springDamping =40f;
 public float breakForce =1500f;
 [Range(0,2)] public float alpha =0.5f;

 [Header("Striker (existing in scene)")]
 public SimpleBody strikerBody;
 public Vector3 strikerInitialVelocity = new Vector3(0,0,25f); // high-speed impact
 public bool setStrikerVelocityOnStart = true;

 void Start()
 {
 if (!world) world = FindObjectOfType<SimplePhysicsWorld>();
 if (applyWorldSettings && world)
 {
 world.gravity = gravity;
 world.substeps = substeps;
 }
 ConfigureConstraint();
 ConfigureStriker();
 }

 void ConfigureConstraint()
 {
 if (!constraint) return;
 // Bind bodies and transforms if missing
 if (!constraint.bodyA && beamABody) constraint.bodyA = beamABody;
 if (!constraint.bodyB && beamBBody) constraint.bodyB = beamBBody;
 if (!constraint.ctA && beamACT) constraint.ctA = beamACT;
 if (!constraint.ctB && beamBCT) constraint.ctB = beamBCT;

 if (autoComputeAnchorsFromScale)
 {
 if (constraint.ctA)
 constraint.localAnchorA = new Vector3(constraint.ctA.scale.x *0.5f,0,0); // end of Beam A facing Beam B
 if (constraint.ctB)
 constraint.localAnchorB = new Vector3(-constraint.ctB.scale.x *0.5f,0,0); // end of Beam B facing Beam A
 }

 constraint.restLength = constraintRestLength;
 constraint.stiffness = springStiffness;
 constraint.damping = springDamping;
 constraint.breakForce = breakForce;
 constraint.alpha = alpha;
 }

 void ConfigureStriker()
 {
 if (setStrikerVelocityOnStart && strikerBody && !strikerBody.isStatic)
 strikerBody.velocity = strikerInitialVelocity;
 }
}
