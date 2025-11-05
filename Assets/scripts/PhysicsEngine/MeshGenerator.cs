using UnityEngine;

/// <summary>
/// Générateur de mesh custom - création manuelle de cubes sans fonctions Unity
/// </summary>
public static class MeshGenerator
{
    /// <summary>
    /// Créer un cube manuellement avec vertices et triangles
    /// </summary>
    public static Mesh CreateCubeMesh(Vector3 size)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Custom Cube";
        
        // Calcul des demi-dimensions
        float halfX = size.x / 2f;
        float halfY = size.y / 2f;
        float halfZ = size.z / 2f;
        
        // 8 vertices du cube (coins)
        // Ordre: face avant, face arrière
        Vector3[] vertices = new Vector3[24]; // 4 vertices par face × 6 faces
        
        // Face AVANT (Z+)
        vertices[0] = new Vector3(-halfX, -halfY, halfZ);  // Bas-gauche
        vertices[1] = new Vector3(halfX, -halfY, halfZ);   // Bas-droite
        vertices[2] = new Vector3(halfX, halfY, halfZ);    // Haut-droite
        vertices[3] = new Vector3(-halfX, halfY, halfZ);   // Haut-gauche
        
        // Face ARRIÈRE (Z-)
        vertices[4] = new Vector3(halfX, -halfY, -halfZ);  // Bas-droite
        vertices[5] = new Vector3(-halfX, -halfY, -halfZ); // Bas-gauche
        vertices[6] = new Vector3(-halfX, halfY, -halfZ);  // Haut-gauche
        vertices[7] = new Vector3(halfX, halfY, -halfZ);   // Haut-droite
        
        // Face HAUT (Y+)
        vertices[8] = new Vector3(-halfX, halfY, halfZ);   // Avant-gauche
        vertices[9] = new Vector3(halfX, halfY, halfZ);    // Avant-droite
        vertices[10] = new Vector3(halfX, halfY, -halfZ);  // Arrière-droite
        vertices[11] = new Vector3(-halfX, halfY, -halfZ); // Arrière-gauche
        
        // Face BAS (Y-)
        vertices[12] = new Vector3(-halfX, -halfY, -halfZ); // Arrière-gauche
        vertices[13] = new Vector3(halfX, -halfY, -halfZ);  // Arrière-droite
        vertices[14] = new Vector3(halfX, -halfY, halfZ);   // Avant-droite
        vertices[15] = new Vector3(-halfX, -halfY, halfZ);  // Avant-gauche
        
        // Face DROITE (X+)
        vertices[16] = new Vector3(halfX, -halfY, halfZ);   // Avant-bas
        vertices[17] = new Vector3(halfX, -halfY, -halfZ);  // Arrière-bas
        vertices[18] = new Vector3(halfX, halfY, -halfZ);   // Arrière-haut
        vertices[19] = new Vector3(halfX, halfY, halfZ);    // Avant-haut
        
        // Face GAUCHE (X-)
        vertices[20] = new Vector3(-halfX, -halfY, -halfZ); // Arrière-bas
        vertices[21] = new Vector3(-halfX, -halfY, halfZ);  // Avant-bas
        vertices[22] = new Vector3(-halfX, halfY, halfZ);   // Avant-haut
        vertices[23] = new Vector3(-halfX, halfY, -halfZ);  // Arrière-haut
        
        // Triangles (2 triangles par face = 6 indices par face)
        // Ordre anti-horaire pour normal sortante
        int[] triangles = new int[36]; // 6 faces × 2 triangles × 3 vertices
        
        // Face AVANT
        triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
        triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;
        
        // Face ARRIÈRE
        triangles[6] = 4; triangles[7] = 6; triangles[8] = 5;
        triangles[9] = 4; triangles[10] = 7; triangles[11] = 6;
        
        // Face HAUT
        triangles[12] = 8; triangles[13] = 10; triangles[14] = 9;
        triangles[15] = 8; triangles[16] = 11; triangles[17] = 10;
        
        // Face BAS
        triangles[18] = 12; triangles[19] = 14; triangles[20] = 13;
        triangles[21] = 12; triangles[22] = 15; triangles[23] = 14;
        
        // Face DROITE
        triangles[24] = 16; triangles[25] = 18; triangles[26] = 17;
        triangles[27] = 16; triangles[28] = 19; triangles[29] = 18;
        
        // Face GAUCHE
        triangles[30] = 20; triangles[31] = 22; triangles[32] = 21;
        triangles[33] = 20; triangles[34] = 23; triangles[35] = 22;
        
        // Normales (une par vertex)
        Vector3[] normals = new Vector3[24];
        
        // Face AVANT (Z+)
        for (int i = 0; i < 4; i++) normals[i] = Vector3.forward;
        // Face ARRIÈRE (Z-)
        for (int i = 4; i < 8; i++) normals[i] = Vector3.back;
        // Face HAUT (Y+)
        for (int i = 8; i < 12; i++) normals[i] = Vector3.up;
        // Face BAS (Y-)
        for (int i = 12; i < 16; i++) normals[i] = Vector3.down;
        // Face DROITE (X+)
        for (int i = 16; i < 20; i++) normals[i] = Vector3.right;
        // Face GAUCHE (X-)
        for (int i = 20; i < 24; i++) normals[i] = Vector3.left;
        
        // UVs (coordonnées de texture)
        Vector2[] uvs = new Vector2[24];
        for (int i = 0; i < 6; i++)
        {
            int offset = i * 4;
            uvs[offset] = new Vector2(0, 0);     // Bas-gauche
            uvs[offset + 1] = new Vector2(1, 0); // Bas-droite
            uvs[offset + 2] = new Vector2(1, 1); // Haut-droite
            uvs[offset + 3] = new Vector2(0, 1); // Haut-gauche
        }
        
        // Assigner au mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;
        
        // Recalculer bounds pour le culling
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    /// <summary>
    /// Créer un GameObject avec un cube custom
    /// </summary>
    public static GameObject CreateCubeGameObject(Vector3 position, Vector3 size, Material material = null)
    {
        // Créer GameObject vide
        GameObject obj = new GameObject("Custom Cube");
        obj.transform.position = position;
        
        // Ajouter MeshFilter et MeshRenderer manuellement
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        
        // Générer et assigner le mesh custom
        meshFilter.mesh = CreateCubeMesh(size);
        
        // Assigner matériau
        if (material != null)
        {
            meshRenderer.material = material;
        }
        else
        {
            // Créer un matériau simple par défaut
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }
        
        return obj;
    }
    
    /// <summary>
    /// Calculer le volume d'un cube (pour masse basée sur densité)
    /// </summary>
    public static float CalculateCubeVolume(Vector3 size)
    {
        return size.x * size.y * size.z;
    }
    
    /// <summary>
    /// Calculer le moment d'inertie d'un cube homogène
    /// I = (1/12) * m * (h² + d²) pour chaque axe
    /// </summary>
    public static Vector3 CalculateCubeInertia(float mass, Vector3 size)
    {
        float Ix = (1f / 12f) * mass * (size.y * size.y + size.z * size.z);
        float Iy = (1f / 12f) * mass * (size.x * size.x + size.z * size.z);
        float Iz = (1f / 12f) * mass * (size.x * size.x + size.y * size.y);
        
        return new Vector3(Ix, Iy, Iz);
    }
}
