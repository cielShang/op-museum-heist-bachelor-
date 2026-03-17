using UnityEngine;

public enum P3LaserStepType
{
    Emitter1_MoveX_Negative,
    Emitter2_MoveX_Positive,
    Emitter3_RotateX_Negative
}

public class P3LaserEmitterStepped : MonoBehaviour
{
    [Header("Puzzle Wiring")]
    public P3MillenniumPuzzleController controller;

    [Header("Locking")]
    public bool isLocked;
    public AudioClip lockedBeep; // optional
    public AudioSource audioSource;

    [Header("Ray Setup")]
    public Transform rayOrigin;       // child "RayOrigin"
    public Transform rayDirection;    // child "RayDirection"
    public float rayDistance = 30f;
    public LayerMask hitMask;
    public Collider lockTargetCollider;

    [Header("Alignment")]
    public float alignTolerance = 0.08f;
    public bool IsAligned { get; private set; }

    [Header("Laser Visual")]
    public GameObject laserVisualRoot;

    [Header("Stepped Control")]
    public P3LaserStepType stepType = P3LaserStepType.Emitter1_MoveX_Negative;
    public float stepMoveX = 0.10f;
    public float stepRotateX = 22f;

    [Header("Debug")]
    public bool debugDrawRay = true;

    private bool _lockedByAlignment; // prevents repeated locking beeps

    void Awake()
    {
        if (rayOrigin == null) rayOrigin = transform;
        if (rayDirection == null) rayDirection = transform;
    }

    void Update()
    {
        UpdateAlignment();
    }

    // Called by your button script
    public void StepControl()
    {
        if (isLocked) return;

        switch (stepType)
        {
            case P3LaserStepType.Emitter1_MoveX_Negative:
            {
                Vector3 p = transform.localPosition;
                p.x -= stepMoveX;
                transform.localPosition = p;
                break;
            }
            case P3LaserStepType.Emitter2_MoveX_Positive:
            {
                Vector3 p = transform.localPosition;
                p.x += stepMoveX;
                transform.localPosition = p;
                break;
            }
            case P3LaserStepType.Emitter3_RotateX_Negative:
            {
                Vector3 e = transform.localEulerAngles;
                e.x -= stepRotateX;
                transform.localEulerAngles = e;
                break;
            }
        }

        UpdateAlignment();
        if (controller != null) controller.EvaluateSolved();
    }

    private void UpdateAlignment()
    {
        if (lockTargetCollider == null)
        {
            IsAligned = false;
            return;
        }

        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayDirection.forward;

        // Do the raycast
        if (!Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, hitMask, QueryTriggerInteraction.Collide))
        {
            IsAligned = false;
            DrawDebug(origin, dir);
            return;
        }

        if (hit.collider != lockTargetCollider)
        {
            IsAligned = false;
            DrawDebug(origin, dir);
            return;
        }

        // “Aligned” if hit point is close enough to target center
        float d = Vector3.Distance(hit.point, lockTargetCollider.bounds.center);
        float allowed = lockTargetCollider.bounds.extents.magnitude + alignTolerance;
        IsAligned = d <= allowed;

        DrawDebug(origin, dir);

        // Auto-lock the moment it becomes aligned
        if (IsAligned && !_lockedByAlignment)
        {
            _lockedByAlignment = true;
            SetLocked(true);

            // Optional: disable beam visual so player sees "this beam is done"
            // DisableLaserVisual();

            // Let controller re-check immediately
            if (controller != null) controller.EvaluateSolved();
        }
    }

    private void DrawDebug(Vector3 origin, Vector3 dir)
    {
        if (!debugDrawRay) return;
        Debug.DrawRay(origin, dir * rayDistance, IsAligned ? Color.green : Color.red);
    }

    public void DisableLaserVisual()
    {
        if (laserVisualRoot != null)
            laserVisualRoot.SetActive(false);
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (locked && audioSource != null && lockedBeep != null)
            audioSource.PlayOneShot(lockedBeep);
    }
}
