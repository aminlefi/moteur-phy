using UnityEngine;
using System.Collections.Generic;

public class AnimationController : MonoBehaviour
{
    public List<RigidBody3DState> objects;
    public Matrix4x4 projectionMatrix;
    public Matrix4x4 viewMatrix;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize objects
        objects = new List<RigidBody3DState>();

        // Set up camera
        projectionMatrix = Math3D.ProjectionMatrix(60f, 16f / 9f, 0.1f, 100f);
        viewMatrix = Math3D.ViewMatrix(new Vector3(0, 5, 10), Vector3.zero, Vector3.up);
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var obj in objects)
        {
            obj.UpdatePosition();
            RenderObject(obj);
        }
    }

    void RenderObject(RigidBody3DState obj)
    {
        // Apply transformations
        Matrix4x4 modelMatrix = Matrix4x4.TRS(obj.position, Quaternion.identity, Vector3.one);
        Matrix4x4 mvp = projectionMatrix * viewMatrix * modelMatrix;

        // Transform vertices
        Vector3[] transformedVertices = new Vector3[obj.cube.vertices.Length];
        for (int i = 0; i < obj.cube.vertices.Length; i++)
        {
            transformedVertices[i] = Math3D.MultiplyMatrixVector3(mvp, obj.cube.vertices[i]);
        }

        // Simulate rendering (e.g., log vertex positions)
        Debug.Log($"Rendered object at {obj.position}");
    }
}
