using UnityEngine;

/// <summary>
/// Lightweight component to store floor mesh world-space bounds for our custom physics.
/// We avoid using Unity Colliders and instead compute and expose the floor top Y and world bounds.
/// </summary>
public class FloorInfo : MonoBehaviour
{
    public Vector3 worldCenter = Vector3.zero;
    public Vector3 worldSize = Vector3.one;

    public float TopY => worldCenter.y + worldSize.y * 0.5f;

    /// <summary>
    /// Update the stored world-space bounds from a MeshFilter. Handles mesh center + localScale.
    /// </summary>
    public void UpdateFromMesh(MeshFilter mf)
    {
        if (mf == null || mf.mesh == null) return;
        // mesh.bounds.center is local-space center; transform to world
        worldCenter = mf.transform.TransformPoint(mf.mesh.bounds.center);
        // account for localScale when computing size in world units
        Vector3 localSize = mf.mesh.bounds.size;
        Vector3 lossyScale = mf.transform.lossyScale;
        worldSize = new Vector3(localSize.x * Mathf.Abs(lossyScale.x), localSize.y * Mathf.Abs(lossyScale.y), localSize.z * Mathf.Abs(lossyScale.z));
    }
}
