using UnityEngine;

// Breakable spring-damper constraint between two custom bodies, with energy release on break controlled by alpha.
[DisallowMultipleComponent]
public class SimpleSpringConstraint : MonoBehaviour
{
 public SimpleBody bodyA;
 public SimpleBody bodyB;
 public CustomTransform ctA; // optional explicit transforms for anchors (defaults to bodies' custom transforms)
 public CustomTransform ctB;

 [Tooltip("Local-space anchor on bodyA")] public Vector3 localAnchorA = Vector3.zero;
 [Tooltip("Local-space anchor on bodyB")] public Vector3 localAnchorB = Vector3.zero;

 [Tooltip("Rest length of the spring (auto init if <=0 and autoRestLength is true)")] public float restLength =0f;
 [Tooltip("Spring stiffness (N/unit)")] public float stiffness =5000f;
 [Tooltip("Damping along spring axis (N*s/unit)")] public float damping =25f;

 [Tooltip("Break when |spring force| exceeds this threshold (N) and velocity threshold (if >0) is satisfied")] public float breakForce =2000f;
 [Tooltip("Minimum relative speed along spring axis required to allow breaking (0 = disabled)")] public float breakVelocityThreshold =0f;
 [Tooltip("Delay in seconds before fracture checks start (prevents instant break)")] public float fractureDelay =0.0f;
 [Tooltip("Clamp applied spring force magnitude (0 = no clamp)")] public float maxForceClamp =0f;
 [Tooltip("Auto-initialize restLength from initial anchor separation if true and restLength <=0")] public bool autoRestLength = true;

 [Tooltip("Energy transfer coefficient on break.0 = rigid,1 = full potential, >1 = energetic.")]
 public float alpha =0.5f;

 [Tooltip("If true, constraint is already broken and won't apply forces")]
 public bool broken = false;

#if UNITY_EDITOR
 public Color gizmoColor = new Color(0.2f,0.8f,1f,1f);
 public Color brokenColor = new Color(1f,0.4f,0.2f,1f);
#endif

 SimplePhysicsWorld _world;
 float _timeActive; bool _initializedRestLength;

 void Reset()
 {
 if (!bodyA) bodyA = GetComponent<SimpleBody>();
 if (bodyA && !ctA) ctA = bodyA.customTransform;
 if (bodyB && !ctB) ctB = bodyB.customTransform;
 }

 void OnEnable()
 {
 _world = FindObjectOfType<SimplePhysicsWorld>();
 if (_world) _world.Register(this);
 _timeActive =0f; _initializedRestLength = false;
 }
 void OnDisable()
 {
 if (_world) _world.Unregister(this);
 _world = null;
 }

 Vector3 TransformPoint(CustomTransform ct, Vector3 local)
 {
 if (!ct) return local;
 var M = ct.LocalToWorldMatrix;
 return new Vector3(
 M.m00 * local.x + M.m01 * local.y + M.m02 * local.z + M.m03,
 M.m10 * local.x + M.m11 * local.y + M.m12 * local.z + M.m13,
 M.m20 * local.x + M.m21 * local.y + M.m22 * local.z + M.m23
 );
 }

 void InitializeRestLengthIfNeeded(Vector3 pA, Vector3 pB)
 {
 if (_initializedRestLength) return;
 if (autoRestLength && restLength <=0f)
 {
 restLength = (pB - pA).magnitude; // capture initial separation to avoid preloaded energy
 }
 _initializedRestLength = true;
 }

 public void Step(float dt)
 {
 if (broken) return;
 if (!bodyA || !bodyB) return;
 var aCT = ctA ? ctA : (bodyA ? bodyA.customTransform : null);
 var bCT = ctB ? ctB : (bodyB ? bodyB.customTransform : null);
 if (!aCT || !bCT) return;
 _timeActive += dt;

 // world-space anchors
 Vector3 pA = TransformPoint(aCT, localAnchorA);
 Vector3 pB = TransformPoint(bCT, localAnchorB);
 InitializeRestLengthIfNeeded(pA, pB);
 Vector3 d = pB - pA;
 float dist = d.magnitude;
 Vector3 n = dist >1e-6f ? d / dist : Vector3.up; // arbitrary if coincident
 float x = dist - restLength; // extension (>0 stretched)

 var imA = bodyA ? bodyA.InverseMass :0f;
 var imB = bodyB ? bodyB.InverseMass :0f;
 bool aDyn = bodyA && !bodyA.isStatic && imA >0f;
 bool bDyn = bodyB && !bodyB.isStatic && imB >0f;

 // relative velocity along spring axis
 float vrel =0f;
 if (aDyn || bDyn)
 {
 Vector3 vA = aDyn ? bodyA.velocity : Vector3.zero;
 Vector3 vB = bDyn ? bodyB.velocity : Vector3.zero;
 vrel = Vector3.Dot(vB - vA, n);
 }

 // Spring + damping force along n applied on A, opposite on B
 float Fspring = stiffness * x;
 float Fdamp = damping * vrel;
 float F = Fspring + Fdamp;
 if (maxForceClamp >0f) F = Mathf.Clamp(F, -maxForceClamp, maxForceClamp);

 // Break check uses elastic force magnitude, respecting delay & velocity threshold
 if (_timeActive >= fractureDelay && Mathf.Abs(Fspring) > breakForce && (breakVelocityThreshold <=0f || Mathf.Abs(vrel) >= breakVelocityThreshold))
 {
 // Release stored elastic potential energy (only from stretch/compression component)
 float E =0.5f * stiffness * x * x;
 float Eadd = Mathf.Max(0f, alpha) * E;
 float denom = imA + imB;
 if (denom >0f && Eadd >0f)
 {
 float J = Mathf.Sqrt(2f * Eadd / denom); // impulse magnitude
 // Impulse applied outward along +/- n to separate bodies
 if (aDyn) bodyA.velocity -= n * (J * imA);
 if (bDyn) bodyB.velocity += n * (J * imB);
 }
 broken = true;
 return;
 }

 // Apply forces as velocity changes (semi-implicit Euler)
 Vector3 Fa = n * F; // force on A
 Vector3 Fb = -Fa; // force on B
 if (aDyn)
 {
 bodyA.velocity += Fa * imA * dt;
 }
 if (bDyn)
 {
 bodyB.velocity += Fb * imB * dt;
 }
 }

#if UNITY_EDITOR
 void OnDrawGizmos()
 {
 var aCT = ctA ? ctA : (bodyA ? bodyA.customTransform : null);
 var bCT = ctB ? ctB : (bodyB ? bodyB.customTransform : null);
 if (!aCT || !bCT) return;
 Vector3 pA = TransformPoint(aCT, localAnchorA);
 Vector3 pB = TransformPoint(bCT, localAnchorB);
 Gizmos.color = broken ? brokenColor : gizmoColor;
 Gizmos.DrawLine(pA, pB);
 Gizmos.DrawSphere(pA,0.03f);
 Gizmos.DrawSphere(pB,0.03f);
 }
#endif
}
