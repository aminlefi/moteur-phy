using UnityEngine;

public struct MyQuaternion
{
 public float x,y,z,w;
 public MyQuaternion(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}
 public static MyQuaternion Identity => new MyQuaternion(0,0,0,1);
 public static MyQuaternion FromEuler(Vector3 eulerDeg)
 {
 // Convert to radians
 float rx = eulerDeg.x * Mathf.Deg2Rad *0.5f;
 float ry = eulerDeg.y * Mathf.Deg2Rad *0.5f;
 float rz = eulerDeg.z * Mathf.Deg2Rad *0.5f;
 float cx = Mathf.Cos(rx), sx = Mathf.Sin(rx);
 float cy = Mathf.Cos(ry), sy = Mathf.Sin(ry);
 float cz = Mathf.Cos(rz), sz = Mathf.Sin(rz);
 // ZYX order
 float w = cx*cy*cz + sx*sy*sz;
 float x = sx*cy*cz - cx*sy*sz;
 float y = cx*sy*cz + sx*cy*sz;
 float z = cx*cy*sz - sx*sy*cz;
 return new MyQuaternion(x,y,z,w);
 }

 public static MyQuaternion operator *(MyQuaternion a, MyQuaternion b)
 {
 return new MyQuaternion(
 a.w*b.x + a.x*b.w + a.y*b.z - a.z*b.y,
 a.w*b.y - a.x*b.z + a.y*b.w + a.z*b.x,
 a.w*b.z + a.x*b.y - a.y*b.x + a.z*b.w,
 a.w*b.w - a.x*b.x - a.y*b.y - a.z*b.z
 );
 }

 public MyQuaternion Inverse()
 {
 float norm = x*x + y*y + z*z + w*w;
 if (norm <=0f) return Identity;
 float inv =1f / norm;
 return new MyQuaternion(-x*inv,-y*inv,-z*inv,w*inv);
 }

 public Float4x4 ToRotationMatrix()
 {
 Float4x4 m = Float4x4.Identity;
 float xx = x*x; float yy=y*y; float zz=z*z; float xy=x*y; float xz=x*z; float yz=y*z; float wx=w*x; float wy=w*y; float wz=w*z;
 m.m00 =1 -2*(yy+zz);
 m.m01 =2*(xy - wz);
 m.m02 =2*(xz + wy);
 m.m10 =2*(xy + wz);
 m.m11 =1 -2*(xx+zz);
 m.m12 =2*(yz - wx);
 m.m20 =2*(xz - wy);
 m.m21 =2*(yz + wx);
 m.m22 =1 -2*(xx+yy);
 return m;
 }
}

// Minimal4x4 matrix and transform built from scratch. Avoids Unity Transform usage in logic.
public struct Float4x4
{
 public float m00, m01, m02, m03;
 public float m10, m11, m12, m13;
 public float m20, m21, m22, m23;
 public float m30, m31, m32, m33;

 public static Float4x4 Identity
 {
 get
 {
 return new Float4x4
 {
 m00 =1, m11 =1, m22 =1, m33 =1
 };
 }
 }

 public static Float4x4 Translation(Vector3 t)
 {
 var m = Identity;
 m.m03 = t.x; m.m13 = t.y; m.m23 = t.z;
 return m;
 }

 public static Float4x4 Scale(Vector3 s)
 {
 var m = Identity;
 m.m00 = s.x; m.m11 = s.y; m.m22 = s.z;
 return m;
 }

 public static Float4x4 Multiply(Float4x4 a, Float4x4 b)
 {
 Float4x4 r = new Float4x4();
 r.m00 = a.m00*b.m00 + a.m01*b.m10 + a.m02*b.m20 + a.m03*b.m30;
 r.m01 = a.m00*b.m01 + a.m01*b.m11 + a.m02*b.m21 + a.m03*b.m31;
 r.m02 = a.m00*b.m02 + a.m01*b.m12 + a.m02*b.m22 + a.m03*b.m32;
 r.m03 = a.m00*b.m03 + a.m01*b.m13 + a.m02*b.m23 + a.m03*b.m33;
 r.m10 = a.m10*b.m00 + a.m11*b.m10 + a.m12*b.m20 + a.m13*b.m30;
 r.m11 = a.m10*b.m01 + a.m11*b.m11 + a.m12*b.m21 + a.m13*b.m31;
 r.m12 = a.m10*b.m02 + a.m11*b.m12 + a.m12*b.m22 + a.m13*b.m32;
 r.m13 = a.m10*b.m03 + a.m11*b.m13 + a.m12*b.m23 + a.m13*b.m33;
 r.m20 = a.m20*b.m00 + a.m21*b.m10 + a.m22*b.m20 + a.m23*b.m30;
 r.m21 = a.m20*b.m01 + a.m21*b.m11 + a.m22*b.m21 + a.m23*b.m31;
 r.m22 = a.m20*b.m02 + a.m21*b.m12 + a.m22*b.m22 + a.m23*b.m32;
 r.m23 = a.m20*b.m03 + a.m21*b.m13 + a.m22*b.m23 + a.m23*b.m33;
 r.m30 = a.m30*b.m00 + a.m31*b.m10 + a.m32*b.m20 + a.m33*b.m30;
 r.m31 = a.m30*b.m01 + a.m31*b.m11 + a.m32*b.m21 + a.m33*b.m31;
 r.m32 = a.m30*b.m02 + a.m31*b.m12 + a.m32*b.m22 + a.m33*b.m32;
 r.m33 = a.m30*b.m03 + a.m31*b.m13 + a.m32*b.m23 + a.m33*b.m33;
 return r;
 }

 public Vector3 TransformPoint(Vector3 p)
 {
 return new Vector3(
 m00*p.x + m01*p.y + m02*p.z + m03,
 m10*p.x + m11*p.y + m12*p.z + m13,
 m20*p.x + m21*p.y + m22*p.z + m23
 );
 }
}

[DisallowMultipleComponent]
public class CustomTransform : MonoBehaviour
{
 [SerializeField] private Vector3 _position = Vector3.zero;
 [SerializeField] private Vector3 _euler = Vector3.zero;
 [SerializeField] private Vector3 _scale = Vector3.one;
 public Vector3 position { get => _position; set { _position = value; MarkDirty(); } }
 public Vector3 euler { get => _euler; set { _euler = value; rotation = MyQuaternion.FromEuler(_euler); MarkDirty(); } }
 public Vector3 scale { get => _scale; set { _scale = value; MarkDirty(); } }
 public MyQuaternion rotation = MyQuaternion.Identity; // internal sync from euler

 private Vector3 _initialPosition; private MyQuaternion _initialRotation; private Vector3 _initialScale; private Vector3 _initialEuler; private bool _cachedInitial;
 private bool _dirty = true; private Float4x4 _matrix;
 public Float4x4 LocalToWorldMatrix { get { if (_dirty) Rebuild(); return _matrix; } }

 void Awake(){ CacheInitialState(); }
 void OnValidate(){ // editor-time changes
 rotation = MyQuaternion.FromEuler(_euler); MarkDirty(); }

 public void CacheInitialState(){ _initialPosition = _position; _initialRotation = rotation; _initialScale = _scale; _initialEuler = _euler; _cachedInitial = true; }

 public Float4x4 InverseDeltaMatrix()
 {
 if (!_cachedInitial) CacheInitialState();
 Vector3 dPos = _position - _initialPosition;
 rotation = MyQuaternion.FromEuler(_euler); _initialRotation = MyQuaternion.FromEuler(_initialEuler);
 MyQuaternion dRot = rotation * _initialRotation.Inverse();
 Vector3 dScale = new Vector3(_scale.x / _initialScale.x, _scale.y / _initialScale.y, _scale.z / _initialScale.z);
 var invS = Float4x4.Scale(new Vector3(1f / dScale.x,1f / dScale.y,1f / dScale.z));
 var invR = dRot.Inverse().ToRotationMatrix();
 var invT = Float4x4.Translation(-dPos);
 return Float4x4.Multiply(Float4x4.Multiply(invS, invR), invT);
 }

 public void MarkDirty(){ _dirty = true; }
 public void Translate(Vector3 d){ _position += d; MarkDirty(); }
 public void RotateEuler(Vector3 deltaEuler){ _euler += deltaEuler; rotation = MyQuaternion.FromEuler(_euler); MarkDirty(); }
 public void SetEuler(Vector3 newEuler){ _euler = newEuler; rotation = MyQuaternion.FromEuler(_euler); MarkDirty(); }
 public void SetRotation(MyQuaternion q){ rotation = q; MarkDirty(); }
 public void SetScale(Vector3 s){ _scale = s; MarkDirty(); }

 private void Rebuild()
 {
 rotation = MyQuaternion.FromEuler(_euler);
 var T = Float4x4.Translation(_position);
 var R = rotation.ToRotationMatrix();
 var S = Float4x4.Scale(_scale);
 _matrix = Float4x4.Multiply(Float4x4.Multiply(T, R), S);
 _dirty = false;
 }
}
