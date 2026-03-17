using UnityEngine;

public class P3LaserEmitterInteract : MonoBehaviour
{
    public P3LaserEmitter emitter;
    public KeyCode interactKey = KeyCode.E;

    bool _inside;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            _inside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _inside = false;
    }

    void Update()
    {
        if (_inside && Input.GetKeyDown(interactKey))
        {
            emitter.RotateEmitter();
        }
    }
}
