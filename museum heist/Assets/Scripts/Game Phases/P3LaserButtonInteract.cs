using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class P3LaserButtonInteract : MonoBehaviour
{
    [Header("Interact")]
    public KeyCode interactKey = KeyCode.E;
    public string playerTag = "Player";
    public float fallbackCheckRadius = 1.4f; // helps if trigger events are flaky

    [Header("Which laser this button controls")]
    public P3LaserEmitterStepped targetEmitter;

    [Header("Optional feedback")]
    public AudioSource audioSource;
    public AudioClip pressClip;

    [Header("Switch Handle Animation")]
    public Animator switchAnimator;                 
    public string handleTurnState = "handle_turn";
    public string handleUpState   = "handle_up";
    public bool alternateAnimEachUse = true;

    [Header("Optional UI Hint")]
    public CanvasGroup uiGroup;
    public TMP_Text hintText;
    public string hintMessage = "Press E to adjust";

    private bool _inside;
    private Transform _player;
     private bool _toggle;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Start()
    {
        SetUI(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _inside = true;
        _player = other.transform;
        SetUI(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _inside = false;
        if (_player == other.transform) _player = null;
        SetUI(false);
    }

    void Update()
    {
        // Fallback: if trigger didn't fire, still allow interaction nearby
        if (!_inside)
        {
            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag(playerTag);
                if (p != null) _player = p.transform;
            }

            if (_player != null)
            {
                float d = Vector3.Distance(transform.position, _player.position);
                if (d <= fallbackCheckRadius)
                {
                    _inside = true;
                    SetUI(true);
                }
            }
        }
        else
        {
            // if we had fallback-enabled inside, also allow leaving
            if (_player != null)
            {
                float d = Vector3.Distance(transform.position, _player.position);
                if (d > fallbackCheckRadius * 1.2f)
                {
                    _inside = false;
                    SetUI(false);
                }
            }
        }

        if (!_inside) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (audioSource != null && pressClip != null)
                audioSource.PlayOneShot(pressClip);

                PlaySwitchAnim();

            if (targetEmitter != null)
                targetEmitter.StepControl();
            else
                Debug.LogWarning("[P3LaserButtonInteract] No targetEmitter assigned.", this);
        }
    }

    private void PlaySwitchAnim()
    {
        if (switchAnimator == null) return;

        string stateToPlay = handleTurnState;

        if (alternateAnimEachUse)
        {
            stateToPlay = _toggle ? handleUpState : handleTurnState;
            _toggle = !_toggle;
        }
        switchAnimator.Play(stateToPlay, 0, 0f);
    }
    private void SetUI(bool on)
    {
        if (uiGroup != null)
        {
            uiGroup.alpha = on ? 1f : 0f;
            uiGroup.blocksRaycasts = on;
            uiGroup.interactable = on;
        }

        if (hintText != null)
            hintText.text = hintMessage;
    }
}
