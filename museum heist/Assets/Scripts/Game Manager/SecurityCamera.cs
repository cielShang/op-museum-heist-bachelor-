using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    [Header("Parts")]
    public Transform cameraHead;
    public Transform detectionOrigin;

    [Header("Vision")]
    public float viewDistance = 12f;
    public float viewAngle = 45f;
    public LayerMask detectionMask;

    [Header("Detection Effects")]
    public int damageOnDetection = 1;
    public float alarmCooldown = 3f;

    [Header("Movement")]
    [Tooltip("Base sweep speed for THIS camera")]
    public float sweepSpeed = 0.6f;

    [Tooltip("Max vertical sweep angle (degrees)")]
    public float sweepAngle = 25f;

    [Tooltip("Random speed variation per camera")]
    [Range(0f, 1f)]
    public float randomSpeedVariance = 0.25f;

    [Tooltip("Random phase offset so cameras don't sync")]
    public bool randomizePhase = true;

    [Tooltip("Delay before this camera starts moving")]
    public float startDelay = 0f;

    [Header("Debug")]
    public bool debugDrawRay = true;

    float _alarmTimer;
    float _phaseOffset;
    float _speedMultiplier = 1f;
    float _startTimer;

    void Start()
    {
        if (randomizePhase)
            _phaseOffset = Random.Range(0f, 100f);

        _speedMultiplier = 1f + Random.Range(-randomSpeedVariance, randomSpeedVariance);
    }

    void Update()
    {
        RotateCamera();
        DetectPlayer();

        if (_alarmTimer > 0f)
            _alarmTimer -= Time.deltaTime;
    }

    void RotateCamera()
    {
        if (cameraHead == null) return;

        if (_startTimer < startDelay)
        {
            _startTimer += Time.deltaTime;
            return;
        }

        float t = Time.time * sweepSpeed * _speedMultiplier + _phaseOffset;
        float angle = Mathf.Sin(t) * sweepAngle;

        cameraHead.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    void DetectPlayer()
    {
        if (_alarmTimer > 0f) return;
        if (detectionOrigin == null) return;

        Vector3 origin = detectionOrigin.position;
        Vector3 forward = detectionOrigin.forward;

        if (Physics.Raycast(origin, forward, out RaycastHit hit, viewDistance, detectionMask))
        {
            if (!hit.collider.CompareTag("Player")) return;

            _alarmTimer = alarmCooldown;

            PlayerHealth ph = hit.collider.GetComponent<PlayerHealth>();
            if (ph != null && damageOnDetection > 0)
                ph.TakeDamage(damageOnDetection);
        }

        if (debugDrawRay)
            Debug.DrawRay(origin, forward * viewDistance, Color.red);
    }
}
