using UnityEngine;

public class MainController : MonoBehaviour
{
    CubeObject cubeEuler;
    CubeObject cubeQuat;

    // Angles Euler
    float yaw = 0, pitch = 0, roll = 0;

    // Quaternion
    QuaternionRotation.MyQuaternion qRotation;

    void Start()
    {
        // Créer deux cubes
        cubeEuler = new CubeObject(new Vector3(-2, 0, 0), Color.red);
        cubeQuat = new CubeObject(new Vector3(2, 0, 0), Color.green);

        // Quaternion initial
        qRotation = new QuaternionRotation.MyQuaternion(1, 0, 0, 0);
    }

    void Update()
    {
        // --- Contrôles clavier ---
        if (Input.GetKey(KeyCode.A)) yaw += 1f;
        if (Input.GetKey(KeyCode.D)) yaw -= 1f;
        if (Input.GetKey(KeyCode.W)) pitch += 1f;
        if (Input.GetKey(KeyCode.S)) pitch -= 1f;
        if (Input.GetKey(KeyCode.Q)) roll += 1f;
        if (Input.GetKey(KeyCode.E)) roll -= 1f;

        // --- Euler rotation ---
        float[,] Rz = EulerRotation.RotationZ(roll);
        float[,] Ry = EulerRotation.RotationY(yaw);
        float[,] Rx = EulerRotation.RotationX(pitch);

        // Ordre : Yaw → Pitch → Roll (Z-Y-X)
        float[,] Reuler = EulerRotation.Multiply(Rz, EulerRotation.Multiply(Ry, Rx));
        cubeEuler.ApplyMatrix(Reuler);

        // --- Quaternion rotation ---
        QuaternionRotation.MyQuaternion qYaw = QuaternionRotation.FromAxisAngle(Vector3.up, yaw);
        QuaternionRotation.MyQuaternion qPitch = QuaternionRotation.FromAxisAngle(Vector3.right, pitch);
        QuaternionRotation.MyQuaternion qRoll = QuaternionRotation.FromAxisAngle(Vector3.forward, roll);

        // Composition des quaternions
        qRotation = QuaternionRotation.Multiply(qYaw, QuaternionRotation.Multiply(qPitch, qRoll));

        float[,] Rquat = QuaternionRotation.ToMatrix(qRotation);
        cubeQuat.ApplyMatrix(Rquat);
    }
}
