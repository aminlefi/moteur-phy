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

 readonly List<SimpleAABBCollider> _colliders = new();
 readonly List<(SimpleAABBCollider, SimpleAABBCollider)> _contacts = new();
 readonly List<SimpleBody> _bodies = new();

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
 }

 public void Register(SimpleAABBCollider col)
 { if (!_colliders.Contains(col)) _colliders.Add(col); }
 public void Unregister(SimpleAABBCollider col){ _colliders.Remove(col); }
 public void Register(SimpleBody body){ if (!_bodies.Contains(body)) _bodies.Add(body); }
 public void Unregister(SimpleBody body){ _bodies.Remove(body); }

 void Update()
 {
 if (continuousDiscovery)
 {
 var allC = FindObjectsOfType<SimpleAABBCollider>(); foreach (var c in allC) if (!_colliders.Contains(c)) _colliders.Add(c);
 var allB = FindObjectsOfType<SimpleBody>(); foreach (var b in allB) if (!_bodies.Contains(b)) _bodies.Add(b);
 }

 float dt = Time.deltaTime;
 Integrate(dt);

 _contacts.Clear();
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

 void Integrate(float dt)
 {
 foreach (var body in _bodies)
 {
 if (!body || body.isStatic) continue;
 if (!body.customTransform) continue;
 // gravity
 body.velocity += gravity * body.gravityScale * dt;
 // damping (exponential per second approximation)
 if (body.damping >0f)
 {
 float k = Mathf.Clamp01(body.damping);
 body.velocity *= Mathf.Max(0f,1f - k * dt);
 }
 // integrate
 body.customTransform.Translate(body.velocity * dt);
 body.customTransform.MarkDirty();
 }
 }

 // Robust oriented box resolution using full SAT (15 axes)
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
 float overlap = rA + rB - dist; if (overlap <=0f) return; // shouldn't happen since Overlaps checked, but keep safe
 if (overlap < bestOverlap)
 {
 // Ensure axis points from A to B for consistent resolution direction
 float sign = Mathf.Sign(Vector3.Dot(delta, n));
 bestOverlap = overlap; bestAxis = n * (sign ==0f ?1f : sign);
 }
 }

 // Evaluate face normals
 for (int i =0; i < baseAxes.Length; i++) ConsiderAxis(baseAxes[i]);
 // Evaluate cross-product axes
 for (int i =0; i < crossAxes.Length; i++) ConsiderAxis(crossAxes[i]);

 if (bestAxis == Vector3.zero) return;
 Vector3 mtv = bestAxis * bestOverlap;
 // split by inverse mass
 float imA = abody ? abody.InverseMass :0f; float imB = bbody ? bbody.InverseMass :0f; float invSum = imA + imB;
 float wa = (invSum >0f) ? imA / invSum :0.5f; float wb = (invSum >0f) ? imB / invSum :0.5f;
 at.Translate(-mtv * wa); at.MarkDirty();
 bt.Translate( mtv * wb); bt.MarkDirty();
 // basic collision response: zero velocity into separation direction
 if (abody && !abody.isStatic)
 abody.velocity -= Vector3.Project(abody.velocity, bestAxis);
 if (bbody && !bbody.isStatic)
 bbody.velocity -= Vector3.Project(bbody.velocity, bestAxis);
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
