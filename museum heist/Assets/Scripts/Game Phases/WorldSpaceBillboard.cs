using UnityEngine;

public class WorldSpaceBillboard : MonoBehaviour
{
    public Camera targetCamera;
    public bool lockX = false; 
    public bool lockZ = false;

    void LateUpdate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 forward = transform.position - targetCamera.transform.position;
        if (forward.sqrMagnitude < 0.0001f) return;

        Quaternion lookRot = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 e = lookRot.eulerAngles;

        if (lockX) e.x = 0f;
        if (lockZ) e.z = 0f;

        transform.rotation = Quaternion.Euler(e);
    }
}
