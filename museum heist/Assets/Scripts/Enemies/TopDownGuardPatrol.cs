using UnityEngine;

public class TopDownGuardPatrol : MonoBehaviour
{
    [Header("Activation")]
    [SerializeField] private bool _active = false;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float speed = 2.0f;
    public float waitAtPoint = 1.0f;

    [Header("Vision")]
    public float visionRange = 9f;
    [Range(1f, 180f)] public float visionAngle = 65f;
    public LayerMask visionMask = ~0;
    public Transform visionOrigin;

    [Header("Player + Damage")]
    public string playerTag = "Player";
    public int damageToPlayer = 1;
    public float damageCooldown = 1.0f; // prevents instant death
    private float _nextDamageTime;

    private Transform _player;
    private int _index;
    private float _wait;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) _player = p.transform;
    }

    public void SetActive(bool active)
    {
        _active = active;
    }

    void Update()
    {
        if (!_active) return;

        Patrol();
        DetectAndDamage();
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        var target = patrolPoints[_index];
        Vector3 to = target.position - transform.position;
        to.y = 0f;

        if (to.magnitude < 0.25f)
        {
            _wait += Time.deltaTime;
            if (_wait >= waitAtPoint)
            {
                _wait = 0f;
                _index = (_index + 1) % patrolPoints.Length;
            }
            return;
        }

        Vector3 dir = to.normalized;
        transform.position += dir * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    void DetectAndDamage()
    {
        if (_player == null) return;

        if (!CanSeePlayer(out _))
            return;

        if (Time.time < _nextDamageTime) return;
        _nextDamageTime = Time.time + damageCooldown;

        var ph = _player.GetComponent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(damageToPlayer);
    }

    public bool CanSeePlayer(out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        if (_player == null) return false;

        Vector3 origin = (visionOrigin != null) ? visionOrigin.position : (transform.position + Vector3.up * 1f);
        Vector3 toPlayer = _player.position - origin;
        float dist = toPlayer.magnitude;
        if (dist > visionRange) return false;

        Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
        float ang = Vector3.Angle(transform.forward, flat.normalized);
        if (ang > visionAngle * 0.5f) return false;

        // LOS check
        if (Physics.Raycast(origin, flat.normalized, out RaycastHit hit, visionRange, visionMask))
        {
            hitPoint = hit.point;
            return hit.collider.CompareTag(playerTag);
        }

        return false;
    }
}
