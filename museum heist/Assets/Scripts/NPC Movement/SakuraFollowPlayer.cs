using UnityEngine;
using UnityEngine.AI;

public class SakuraFollowPlayer : MonoBehaviour
{
    public Transform player;
    public float followDistance = 3f;
    public float stopDistance = 1.8f;
    public float updateInterval = 0.2f;

    [Header("Animation State Names")]
    public string idleStateName = "Idle";
    public string walkStateName = "Walking";
    public string talkingStateName = "Talking";

    private NavMeshAgent _agent;
    private Animator _animator;
    private float _timer;

    private int _walkHash;
    private int _idleHash;
    private int _talkHash;

    private bool _followEnabled = true;
    private bool _isTalking = false;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        _walkHash = Animator.StringToHash(walkStateName);
        _idleHash = Animator.StringToHash(idleStateName);
        _talkHash = Animator.StringToHash(talkingStateName);
    }

    void Update()
    {
        if (player == null || _agent == null)
            return;

        _timer += Time.deltaTime;
        if (_timer < updateInterval) return;
        _timer = 0f;

        // FOLLOW LOGIC
        if (_followEnabled)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            if (dist > followDistance)
            {
                _agent.isStopped = false;
                _agent.SetDestination(player.position);
            }
            else if (dist < stopDistance)
            {
                _agent.isStopped = true;
            }
        }

        // ANIMATION LOGIC
        float speed = _agent.velocity.magnitude;
        bool isMoving = speed > 0.05f && !_agent.isStopped;

        if (isMoving)
        {
            // Walking (even if she happens to be talking)
            _animator.CrossFade(_walkHash, 0.1f);
        }
        else
        {
            // Standing still
            if (_isTalking)
                _animator.CrossFade(_talkHash, 0.1f);
            else
                _animator.CrossFade(_idleHash, 0.1f);
        }
    }

    public void SetFollowEnabled(bool enabled)
    {
        _followEnabled = enabled;

        if (!enabled)
            _agent.isStopped = true;
    }

    public void SetTalking(bool talking)
    {
        _isTalking = talking;
    }

    // These two are for Convai events / UnityEvent bindings

    public void OnConvaiSpeechStart()
    {
        SetTalking(true);
    }

    public void OnConvaiSpeechEnd()
    {
        SetTalking(false);
    }
}
