using UnityEngine;

/// <summary>
/// Matrice 4x4 custom pour transformations manuelles (pas de fonctions Unity)
/// </summary>
public class Matrix4x4Custom
{
    public float[,] m = new float[4, 4];

    public Matrix4x4Custom()
    {
        Identity();
    }

    public void Identity()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                m[i, j] = (i == j) ? 1f : 0f;
            }
        }
    }

    // Matrice de translation
    public static Matrix4x4Custom Translation(Vector3 translation)
    {
        Matrix4x4Custom mat = new Matrix4x4Custom();
        mat.m[0, 3] = translation.x;
        mat.m[1, 3] = translation.y;
        mat.m[2, 3] = translation.z;
        return mat;
    }

    // Matrice de rotation autour de l'axe X
    public static Matrix4x4Custom RotationX(float angleRadians)
    {
        Matrix4x4Custom mat = new Matrix4x4Custom();
        float cos = Mathf.Cos(angleRadians);
        float sin = Mathf.Sin(angleRadians);
        
        mat.m[1, 1] = cos;
        mat.m[1, 2] = -sin;
        mat.m[2, 1] = sin;
        mat.m[2, 2] = cos;
        
        return mat;
    }

    // Matrice de rotation autour de l'axe Y
    public static Matrix4x4Custom RotationY(float angleRadians)
    {
        Matrix4x4Custom mat = new Matrix4x4Custom();
        float cos = Mathf.Cos(angleRadians);
        float sin = Mathf.Sin(angleRadians);
        
        mat.m[0, 0] = cos;
        mat.m[0, 2] = sin;
        mat.m[2, 0] = -sin;
        mat.m[2, 2] = cos;
        
        return mat;
    }

    // Matrice de rotation autour de l'axe Z
    public static Matrix4x4Custom RotationZ(float angleRadians)
    {
        Matrix4x4Custom mat = new Matrix4x4Custom();
        float cos = Mathf.Cos(angleRadians);
        float sin = Mathf.Sin(angleRadians);
        
        mat.m[0, 0] = cos;
        mat.m[0, 1] = -sin;
        mat.m[1, 0] = sin;
        mat.m[1, 1] = cos;
        
        return mat;
    }

    // Multiplication de matrices
    public static Matrix4x4Custom operator *(Matrix4x4Custom a, Matrix4x4Custom b)
    {
        Matrix4x4Custom result = new Matrix4x4Custom();
        
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                result.m[i, j] = 0;
                for (int k = 0; k < 4; k++)
                {
                    result.m[i, j] += a.m[i, k] * b.m[k, j];
                }
            }
        }
        
        return result;
    }

    // Appliquer la transformation à un point
    public Vector3 MultiplyPoint(Vector3 point)
    {
        float x = m[0, 0] * point.x + m[0, 1] * point.y + m[0, 2] * point.z + m[0, 3];
        float y = m[1, 0] * point.x + m[1, 1] * point.y + m[1, 2] * point.z + m[1, 3];
        float z = m[2, 0] * point.x + m[2, 1] * point.y + m[2, 2] * point.z + m[2, 3];
        
        return new Vector3(x, y, z);
    }

    // Convertir en Matrix4x4 Unity (pour visualisation uniquement)
    public Matrix4x4 ToUnityMatrix()
    {
        Matrix4x4 mat = new Matrix4x4();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                mat[i, j] = m[i, j];
            }
        }
        return mat;
    }
}
