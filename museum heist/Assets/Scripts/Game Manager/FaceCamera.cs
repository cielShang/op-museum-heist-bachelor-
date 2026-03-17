using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    public Camera cam;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        transform.forward = cam.transform.forward;
    }
}
