using UnityEngine;

public class P3LaserEmitter : MonoBehaviour
{
    public Transform firePoint;
    public float maxDistance = 12f;
    public LayerMask hitMask;

    [Header("Rotation")]
    public float stepAngle = 30f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctClip;

    [HideInInspector] public bool isCorrect;

    private P3MillenniumPuzzleController _controller;

    void Start()
    {
        _controller = GetComponentInParent<P3MillenniumPuzzleController>();
    }

    void Update()
    {
        CheckHit();
    }

    public void RotateEmitter()
    {
        transform.Rotate(0f, stepAngle, 0f);
        CheckHit();
    }

    void CheckHit()
    {
        isCorrect = false;

        Ray ray = new Ray(firePoint.position, firePoint.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitMask))
        {
            if (hit.collider.CompareTag("MillenniumLock"))
            {
                if (!isCorrect)
                {
                    audioSource?.PlayOneShot(correctClip);
                }
                isCorrect = true;
                _controller.EvaluateSolved();

            }
        }
    }
}
