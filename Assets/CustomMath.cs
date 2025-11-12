using UnityEngine;

public static class CustomMath
{
    // Cross product (produit vectoriel) - Chapitre 1, page 68
    public static Vector3 Cross(Vector3 u, Vector3 v)
    {
        return new Vector3(
            u.y * v.z - u.z * v.y,
            u.z * v.x - u.x * v.z,
            u.x * v.y - u.y * v.x
        );
    }

    // Create translation matrix manually - Chapitre 1, page 31
    public static Matrix4x4 CreateTranslationMatrix(Vector3 translation)
    {
        Matrix4x4 matrix = Matrix4x4.identity;
        matrix.m03 = translation.x;
        matrix.m13 = translation.y;
        matrix.m23 = translation.z;
        return matrix;
    }

    // Create rotation matrix from quaternion - Chapitre 1, page 58
    public static Matrix4x4 CreateRotationMatrix(Quaternion rotation)
    {
        float x = rotation.x, y = rotation.y, z = rotation.z, w = rotation.w;

        Matrix4x4 matrix = Matrix4x4.identity;
        matrix.m00 = 1 - 2 * (y * y + z * z);
        matrix.m01 = 2 * (x * y - z * w);
        matrix.m02 = 2 * (x * z + y * w);

        matrix.m10 = 2 * (x * y + z * w);
        matrix.m11 = 1 - 2 * (x * x + z * z);
        matrix.m12 = 2 * (y * z - x * w);

        matrix.m20 = 2 * (x * z - y * w);
        matrix.m21 = 2 * (y * z + x * w);
        matrix.m22 = 1 - 2 * (x * x + y * y);

        return matrix;
    }
}
