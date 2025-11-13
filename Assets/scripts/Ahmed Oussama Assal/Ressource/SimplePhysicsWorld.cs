using System.Collections.Generic;
using UnityEngine;

// Manager that checks collisions between registered oriented box colliders each frame and integrates simple bodies.
public class SimplePhysicsWorld : MonoBehaviour
{
 public bool autoDiscover = true;
 public bool continuousDiscovery = true; // discovers newly spawned colliders
 public bool resolvePenetration = true;
 public Color contactColor = Color.red;
 public Vector3 gravity = new Vector3(0,-9.81f,0);
 [Range(1,16)] public int substeps =4; // for stability and high-speed interactions

 readonly List<SimpleAABBCollider> _colliders = new();
 readonly List<(SimpleAABBCollider, SimpleAABBCollider)> _contacts = new();
 readonly List<SimpleBody> _bodies = new();
 readonly List<SimpleSpringConstraint> _constraints = new();

 void Start()
 {
 if (autoDiscover)
 Discover();
 }

 void Discover()
 {
 _colliders.Clear();
 _colliders.AddRange(FindObjectsOfType<SimpleAABBCollider>());
 _bodies.Clear();
 _bodies.AddRange(FindObjectsOfType<SimpleBody>());
 _constraints.Clear();
 _constraints.AddRange(FindObjectsOfType<SimpleSpringConstraint>());
 }

 public void Register(SimpleAABBCollider col)
 { if (!_colliders.Contains(col)) _colliders.Add(col); }
 public void Unregister(SimpleAABBCollider col){ _colliders.Remove(col); }
 public void Register(SimpleBody body){ if (!_bodies.Contains(body)) _bodies.Add(body); }
 public void Unregister(SimpleBody body){ _bodies.Remove(body); }
 public void Register(SimpleSpringConstraint c){ if (!_constraints.Contains(c)) _constraints.Add(c); }
 public void Unregister(SimpleSpringConstraint c){ _constraints.Remove(c); }

 void Update()
 {
 if (continuousDiscovery)
 {
 var allC = FindObjectsOfType<SimpleAABBCollider>(); foreach (var c in allC) if (!_colliders.Contains(c)) _colliders.Add(c);
 var allB = FindObjectsOfType<SimpleBody>(); foreach (var b in allB) if (!_bodies.Contains(b)) _bodies.Add(b);
 var allCons = FindObjectsOfType<SimpleSpringConstraint>(); foreach (var c in allCons) if (!_constraints.Contains(c)) _constraints.Add(c);
 }

 float dt = Time.deltaTime;
 float h = dt / Mathf.Max(1, substeps);

 _contacts.Clear();
 for (int step =0; step < Mathf.Max(1, substeps); step++)
 {
 Integrate(h);
 StepConstraints(h);
 CollideAndResolve();
 }
 }

 void Integrate(float dt)
 {
 foreach (var body in _bodies)
 {
 if (!body || body.isStatic) continue;
 if (!body.customTransform) continue;
 // gravity
 body.velocity += gravity * body.gravityScale * dt;
 // linear damping (exponential per second approximation)
 if (body.damping >0f)
 {
 float k = Mathf.Clamp01(body.damping);
 body.velocity *= Mathf.Max(0f,1f - k * dt);
 }
 // integrate linear
 body.customTransform.Translate(body.velocity * dt);

 // angular damping
 if (body.angularDamping >0f)
 {
 float kA = Mathf.Clamp01(body.angularDamping);
 body.angularVelocity *= Mathf.Max(0f,1f - kA * dt);
 }
 // integrate angular: approximate by applying delta euler in world axes
 if (body.angularVelocity.sqrMagnitude >0f)
 {
 Vector3 deltaEulerDeg = body.angularVelocity * Mathf.Rad2Deg * dt;
 body.customTransform.RotateEuler(deltaEulerDeg);
 }

 body.customTransform.MarkDirty();
 }
 }

 void StepConstraints(float dt)
 {
 for (int i =0; i < _constraints.Count; i++)
 {
 var c = _constraints[i]; if (!c) continue;
 c.Step(dt);
 }
 }

 void CollideAndResolve()
 {
 int n = _colliders.Count;
 for (int i =0; i < n -1; i++)
 {
 var a = _colliders[i]; if (!a) continue;
 for (int j = i +1; j < n; j++)
 {
 var b = _colliders[j]; if (!b) continue;
 if (a.Overlaps(b))
 {
 _contacts.Add((a,b));
 if (resolvePenetration)
 Resolve(a,b);
 }
 }
 }
 }

 // Robust oriented box resolution using full SAT (15 axes) + simple impulse that can add rotation
 void Resolve(SimpleAABBCollider a, SimpleAABBCollider b)
 {
 var at = a.customTransform; var bt = b.customTransform;
 var abody = a.GetComponent<SimpleBody>(); var bbody = b.GetComponent<SimpleBody>();
 if (!at || !bt) return;

 // delta from A to B
 Vector3 delta = b.Center - a.Center; if (delta == Vector3.zero) delta = Vector3.up *0.0001f;
 // Axes of both boxes
 Vector3 aAxX = a.AxisX; Vector3 aAxY = a.AxisY; Vector3 aAxZ = a.AxisZ;
 Vector3 bAxX = b.AxisX; Vector3 bAxY = b.AxisY; Vector3 bAxZ = b.AxisZ;
 Vector3 aExt = a.HalfExtents; Vector3 bExt = b.HalfExtents;

 // Candidate axes:3 from A,3 from B,9 cross products
 Vector3[] baseAxes = { aAxX, aAxY, aAxZ, bAxX, bAxY, bAxZ };
 Vector3[] crossAxes = new Vector3[9]
 {
 Vector3.Cross(aAxX, bAxX), Vector3.Cross(aAxX, bAxY), Vector3.Cross(aAxX, bAxZ),
 Vector3.Cross(aAxY, bAxX), Vector3.Cross(aAxY, bAxY), Vector3.Cross(aAxY, bAxZ),
 Vector3.Cross(aAxZ, bAxX), Vector3.Cross(aAxZ, bAxY), Vector3.Cross(aAxZ, bAxZ),
 };

 float bestOverlap = float.MaxValue; Vector3 bestAxis = Vector3.zero;
 const float EPS =1e-6f;

 // Helper to evaluate an axis
 void ConsiderAxis(Vector3 axis)
 {
 float mag2 = axis.sqrMagnitude; if (mag2 < EPS) return; // ignore near-degenerate axes
 Vector3 n = axis.normalized;
 float dist = Mathf.Abs(Vector3.Dot(delta, n));
 // Projection radii
 float rA = Mathf.Abs(Vector3.Dot(n, aAxX)) * aExt.x + Mathf.Abs(Vector3.Dot(n, aAxY)) * aExt.y + Mathf.Abs(Vector3.Dot(n, aAxZ)) * aExt.z;
 float rB = Mathf.Abs(Vector3.Dot(n, bAxX)) * bExt.x + Mathf.Abs(Vector3.Dot(n, bAxY)) * bExt.y + Mathf.Abs(Vector3.Dot(n, bAxZ)) * bExt.z;
 float overlap = rA + rB - dist; if (overlap <=0f) return;
 if (overlap < bestOverlap)
 {
 float sign = Mathf.Sign(Vector3.Dot(delta, n));
 bestOverlap = overlap; bestAxis = n * (sign ==0f ?1f : sign);
 }
 }

 for (int i =0; i < baseAxes.Length; i++) ConsiderAxis(baseAxes[i]);
 for (int i =0; i < crossAxes.Length; i++) ConsiderAxis(crossAxes[i]);

 if (bestAxis == Vector3.zero) return;
 Vector3 nrm = bestAxis; // normal from A to B
 Vector3 mtv = nrm * bestOverlap;

 // split positional correction by inverse mass
 float imA = abody ? abody.InverseMass :0f; float imB = bbody ? bbody.InverseMass :0f; float invSum = imA + imB;
 float wa = (invSum >0f) ? imA / invSum :0.5f; float wb = (invSum >0f) ? imB / invSum :0.5f;
 at.Translate(-mtv * wa); at.MarkDirty();
 bt.Translate( mtv * wb); bt.MarkDirty();

 // Compute approximate contact point using support points along collision normal
 Vector3 SupportPoint(SimpleAABBCollider col, Vector3 dir)
 {
 var c = col.Center; var he = col.HalfExtents; var x = col.AxisX; var y = col.AxisY; var z = col.AxisZ;
 float sx = Mathf.Sign(Vector3.Dot(dir, x));
 float sy = Mathf.Sign(Vector3.Dot(dir, y));
 float sz = Mathf.Sign(Vector3.Dot(dir, z));
 return c + x * he.x * sx + y * he.y * sy + z * he.z * sz;
 }
 Vector3 pA = SupportPoint(a, nrm);
 Vector3 pB = SupportPoint(b, -nrm);
 Vector3 contactPoint =0.5f * (pA + pB);

 // Compute and apply a single normal impulse to cancel closing velocity; includes angular response
 if (abody || bbody)
 {
 Vector3 vA = abody ? abody.velocity : Vector3.zero;
 Vector3 vB = bbody ? bbody.velocity : Vector3.zero;
 Vector3 wA = abody ? abody.angularVelocity : Vector3.zero;
 Vector3 wB = bbody ? bbody.angularVelocity : Vector3.zero;
 Vector3 cA = a.Center; Vector3 cB = b.Center;
 Vector3 rA = contactPoint - cA;
 Vector3 rB = contactPoint - cB;
 Vector3 velAatP = vA + Vector3.Cross(wA, rA);
 Vector3 velBatP = vB + Vector3.Cross(wB, rB);
 float vRelN = Vector3.Dot(velBatP - velAatP, nrm);
 if (vRelN <0f)
 {
 float k = imA + imB;
 if (abody && imA >0f)
 {
 Vector3 rnA = Vector3.Cross(rA, nrm);
 Vector3 Iinv_rnA = abody.InverseInertiaWorldTimes(rnA);
 k += Vector3.Dot(rnA, Iinv_rnA);
 }
 if (bbody && imB >0f)
 {
 Vector3 rnB = Vector3.Cross(rB, nrm);
 Vector3 Iinv_rnB = bbody.InverseInertiaWorldTimes(rnB);
 k += Vector3.Dot(rnB, Iinv_rnB);
 }
 if (k >1e-6f)
 {
 float restitution =0.0f; // keep fully inelastic for stability; tune if desired
 float J = -(1f + restitution) * vRelN / k;
 Vector3 impulse = J * nrm;
 // linear
 if (abody && imA >0f) abody.velocity -= impulse * imA;
 if (bbody && imB >0f) bbody.velocity += impulse * imB;
 // angular
 if (abody && imA >0f)
 {
 Vector3 dL = Vector3.Cross(rA, -impulse);
 abody.angularVelocity += abody.InverseInertiaWorldTimes(dL);
 }
 if (bbody && imB >0f)
 {
 Vector3 dL = Vector3.Cross(rB, impulse);
 bbody.angularVelocity += bbody.InverseInertiaWorldTimes(dL);
 }
 }
 }
 }
 }

#if UNITY_EDITOR
 void OnDrawGizmos()
 {
 Gizmos.color = contactColor;
 foreach (var pair in _contacts)
 {
 var a = pair.Item1; var b = pair.Item2;
 Vector3 p = (a.Center + b.Center) *0.5f;
 Gizmos.DrawSphere(p,0.05f);
 }
 }
#endif
}
