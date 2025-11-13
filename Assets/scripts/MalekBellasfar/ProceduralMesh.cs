using UnityEngine;

/// <summary>
/// Generates procedural meshes without using Unity primitives.
/// Used for creating visual representation of rigid bodies.
/// </summary>
public static class ProceduralMesh
{
    /// <summary>
    /// Create a cube mesh from scratch.
    /// </summary>
    public static Mesh CreateCube(Vector3 size)
    {
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralCube";

        float halfX = size.x * 0.5f;
        float halfY = size.y * 0.5f;
        float halfZ = size.z * 0.5f;

        // 24 vertices (4 per face, 6 faces) - needed for proper normals
        Vector3[] vertices = new Vector3[24];

        // Front face
        vertices[0] = new Vector3(-halfX, -halfY, halfZ);
        vertices[1] = new Vector3(halfX, -halfY, halfZ);
        vertices[2] = new Vector3(halfX, halfY, halfZ);
        vertices[3] = new Vector3(-halfX, halfY, halfZ);

        // Back face
        vertices[4] = new Vector3(halfX, -halfY, -halfZ);
        vertices[5] = new Vector3(-halfX, -halfY, -halfZ);
        vertices[6] = new Vector3(-halfX, halfY, -halfZ);
        vertices[7] = new Vector3(halfX, halfY, -halfZ);

        // Top face
        vertices[8] = new Vector3(-halfX, halfY, halfZ);
        vertices[9] = new Vector3(halfX, halfY, halfZ);
        vertices[10] = new Vector3(halfX, halfY, -halfZ);
        vertices[11] = new Vector3(-halfX, halfY, -halfZ);

        // Bottom face
        vertices[12] = new Vector3(-halfX, -halfY, -halfZ);
        vertices[13] = new Vector3(halfX, -halfY, -halfZ);
        vertices[14] = new Vector3(halfX, -halfY, halfZ);
        vertices[15] = new Vector3(-halfX, -halfY, halfZ);

        // Right face
        vertices[16] = new Vector3(halfX, -halfY, halfZ);
        vertices[17] = new Vector3(halfX, -halfY, -halfZ);
        vertices[18] = new Vector3(halfX, halfY, -halfZ);
        vertices[19] = new Vector3(halfX, halfY, halfZ);

        // Left face
        vertices[20] = new Vector3(-halfX, -halfY, -halfZ);
        vertices[21] = new Vector3(-halfX, -halfY, halfZ);
        vertices[22] = new Vector3(-halfX, halfY, halfZ);
        vertices[23] = new Vector3(-halfX, halfY, -halfZ);

        // Triangles (2 per face = 12 triangles = 36 indices)
        int[] triangles = new int[36];

        // Front
        triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
        triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;

        // Back
        triangles[6] = 4; triangles[7] = 6; triangles[8] = 5;
        triangles[9] = 4; triangles[10] = 7; triangles[11] = 6;

        // Top
        triangles[12] = 8; triangles[13] = 10; triangles[14] = 9;
        triangles[15] = 8; triangles[16] = 11; triangles[17] = 10;

        // Bottom
        triangles[18] = 12; triangles[19] = 14; triangles[20] = 13;
        triangles[21] = 12; triangles[22] = 15; triangles[23] = 14;

        // Right
        triangles[24] = 16; triangles[25] = 18; triangles[26] = 17;
        triangles[27] = 16; triangles[28] = 19; triangles[29] = 18;

        // Left
        triangles[30] = 20; triangles[31] = 22; triangles[32] = 21;
        triangles[33] = 20; triangles[34] = 23; triangles[35] = 22;

        // Normals
        Vector3[] normals = new Vector3[24];
        for (int i = 0; i < 4; i++) normals[i] = Vector3.forward;
        for (int i = 4; i < 8; i++) normals[i] = Vector3.back;
        for (int i = 8; i < 12; i++) normals[i] = Vector3.up;
        for (int i = 12; i < 16; i++) normals[i] = Vector3.down;
        for (int i = 16; i < 20; i++) normals[i] = Vector3.right;
        for (int i = 20; i < 24; i++) normals[i] = Vector3.left;

        // UVs
        Vector2[] uvs = new Vector2[24];
        for (int i = 0; i < 6; i++)
        {
            int offset = i * 4;
            uvs[offset] = new Vector2(0, 0);
            uvs[offset + 1] = new Vector2(1, 0);
            uvs[offset + 2] = new Vector2(1, 1);
            uvs[offset + 3] = new Vector2(0, 1);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// Calculate inertia tensor for a uniform-density box.
    /// I = (1/12) * m * (h² + d²) for each axis
    /// </summary>
    public static Vector3 CalculateBoxInertia(float mass, Vector3 size)
    {
        float Ix = (mass / 12f) * (size.y * size.y + size.z * size.z);
        float Iy = (mass / 12f) * (size.x * size.x + size.z * size.z);
        float Iz = (mass / 12f) * (size.x * size.x + size.y * size.y);
        return new Vector3(Ix, Iy, Iz);
    }
}

