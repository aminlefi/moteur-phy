using UnityEngine;

/// <summary>
/// Composant pour gérer les informations du sol pour la détection de collision personnalisée
/// Supporte à la fois les sols plats et les sphères
/// </summary>
public class FloorInfo : MonoBehaviour
{
    public Vector3 worldCenter;  // Position du centre en coordonnées mondiales
    public Vector3 worldSize;    // Dimensions en coordonnées mondiales
    public bool isSphere;        // True si c'est une sphère, false si c'est un sol plat
    public float sphereRadius;   // Rayon de la sphère si isSphere est true
    
    // Position Y du dessus du sol en coordonnées mondiales
    public float TopY => isSphere ? worldCenter.y + sphereRadius : worldCenter.y + worldSize.y * 0.5f;

    /// <summary>
    /// Met à jour les dimensions à partir des limites du mesh
    /// </summary>
    public void UpdateFromMesh(MeshFilter meshFilter)
    {
        if (meshFilter != null && meshFilter.mesh != null)
        {
            Bounds bounds = meshFilter.mesh.bounds;
            Transform t = meshFilter.transform;
            
            worldSize = Vector3.Scale(bounds.size, t.localScale);
            worldCenter = t.TransformPoint(bounds.center);

            if (isSphere)
            {
                // Pour une sphère, la taille est uniforme
                sphereRadius = worldSize.x * 0.5f;
            }
        }
    }

    /// <summary>
    /// Calcule la hauteur du sol à une position XZ donnée
    /// </summary>
    public float GetHeightAtPosition(Vector3 position)
    {
        if (isSphere)
        {
            // Pour une sphère, calculer la hauteur en fonction de la distance au centre
            Vector2 deltaXZ = new Vector2(position.x - worldCenter.x, position.z - worldCenter.z);
            float distanceXZ = deltaXZ.magnitude;
            
            if (distanceXZ >= sphereRadius)
            {
                return float.NegativeInfinity; // En dehors de la sphère
            }
            
            // Calculer la hauteur Y sur la surface de la sphère
            float y = Mathf.Sqrt(sphereRadius * sphereRadius - distanceXZ * distanceXZ);
            return worldCenter.y + y;
        }
        else
        {
            // Pour un sol plat, retourner simplement TopY
            return TopY;
        }
    }

    /// <summary>
    /// Calcule la normale de la surface à une position donnée
    /// </summary>
    public Vector3 GetNormalAtPosition(Vector3 position)
    {
        if (isSphere)
        {
            // Pour une sphère, la normale pointe du centre vers la position
            return (position - worldCenter).normalized;
        }
        else
        {
            // Pour un sol plat, la normale pointe toujours vers le haut
            return Vector3.up;
        }
    }
}