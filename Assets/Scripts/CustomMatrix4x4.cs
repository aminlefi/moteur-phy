using System;
using UnityEngine;

// Minimal 4x4 matrix implementation for manual transforms (no Unity matrix ops used).
public struct CustomMatrix4x4
{
    // row-major
    private float[,] m;

    public CustomMatrix4x4(bool initializeIdentity)
    {
        m = new float[4,4];
        if (initializeIdentity)
        {
            for (int i = 0; i < 4; i++) m[i,i] = 1f;
        }
    }

    public static CustomMatrix4x4 Identity()
    {
        return new CustomMatrix4x4(true);
    }

    public static CustomMatrix4x4 Translation(Vector3 t)
    {
        var M = Identity();
        M.m[0,3] = t.x;
        M.m[1,3] = t.y;
        M.m[2,3] = t.z;
        return M;
    }

    public static CustomMatrix4x4 Scale(Vector3 s)
    {
        var M = new CustomMatrix4x4(false);
        M.m[0,0] = s.x;
        M.m[1,1] = s.y;
        M.m[2,2] = s.z;
        M.m[3,3] = 1f;
        return M;
    }

    public static CustomMatrix4x4 RotationX(float radians)
    {
        var c = Mathf.Cos(radians);
        var s = Mathf.Sin(radians);
        var M = Identity();
        M.m[1,1] = c; M.m[1,2] = -s;
        M.m[2,1] = s; M.m[2,2] = c;
        return M;
    }

    public static CustomMatrix4x4 RotationY(float radians)
    {
        var c = Mathf.Cos(radians);
        var s = Mathf.Sin(radians);
        var M = Identity();
        M.m[0,0] = c; M.m[0,2] = s;
        M.m[2,0] = -s; M.m[2,2] = c;
        return M;
    }

    public static CustomMatrix4x4 RotationZ(float radians)
    {
        var c = Mathf.Cos(radians);
        var s = Mathf.Sin(radians);
        var M = Identity();
        M.m[0,0] = c; M.m[0,1] = -s;
        M.m[1,0] = s; M.m[1,1] = c;
        return M;
    }

    public static CustomMatrix4x4 Multiply(CustomMatrix4x4 a, CustomMatrix4x4 b)
    {
        var R = new CustomMatrix4x4(false);
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                float sum = 0f;
                for (int k = 0; k < 4; k++) sum += a.m[i,k] * b.m[k,j];
                R.m[i,j] = sum;
            }
        return R;
    }

    // Apply to point (assumes w=1)
    public Vector3 MultiplyPoint(Vector3 p)
    {
        float x = m[0,0]*p.x + m[0,1]*p.y + m[0,2]*p.z + m[0,3];
        float y = m[1,0]*p.x + m[1,1]*p.y + m[1,2]*p.z + m[1,3];
        float z = m[2,0]*p.x + m[2,1]*p.y + m[2,2]*p.z + m[2,3];
        float w = m[3,0]*p.x + m[3,1]*p.y + m[3,2]*p.z + m[3,3];
        if (Mathf.Approximately(w, 0f)) return new Vector3(x, y, z);
        return new Vector3(x/w, y/w, z/w);
    }

    // Extract rotation matrix 3x3 and convert to quaternion (algorithm adapted from common conversions)
    public Quaternion ExtractRotationQuaternion()
    {
        // build 3x3
        float r00 = m[0,0], r01 = m[0,1], r02 = m[0,2];
        float r10 = m[1,0], r11 = m[1,1], r12 = m[1,2];
        float r20 = m[2,0], r21 = m[2,1], r22 = m[2,2];

        float trace = r00 + r11 + r22;
        float qw, qx, qy, qz;
        if (trace > 0f)
        {
            float s = Mathf.Sqrt(trace + 1f) * 2f; // s=4*qw
            qw = 0.25f * s;
            qx = (r21 - r12) / s;
            qy = (r02 - r20) / s;
            qz = (r10 - r01) / s;
        }
        else if ((r00 > r11) & (r00 > r22))
        {
            float s = Mathf.Sqrt(1f + r00 - r11 - r22) * 2f; // s=4*qx
            qw = (r21 - r12) / s;
            qx = 0.25f * s;
            qy = (r01 + r10) / s;
            qz = (r02 + r20) / s;
        }
        else if (r11 > r22)
        {
            float s = Mathf.Sqrt(1f + r11 - r00 - r22) * 2f; // s=4*qy
            qw = (r02 - r20) / s;
            qx = (r01 + r10) / s;
            qy = 0.25f * s;
            qz = (r12 + r21) / s;
        }
        else
        {
            float s = Mathf.Sqrt(1f + r22 - r00 - r11) * 2f; // s=4*qz
            qw = (r10 - r01) / s;
            qx = (r02 + r20) / s;
            qy = (r12 + r21) / s;
            qz = 0.25f * s;
        }
        return new Quaternion(qx, qy, qz, qw);
    }

    // convenience: compose R * T
    public static CustomMatrix4x4 ComposeRotationThenTranslation(CustomMatrix4x4 rot, Vector3 t)
    {
        var T = Translation(t);
        return Multiply(T, rot);
    }
}
