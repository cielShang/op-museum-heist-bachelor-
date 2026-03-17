using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class P3BypassNodeInteract : MonoBehaviour
{
    [Header("Laser Group to Control (SAFE ROUTE)")]
    public P3LaserGroupPatternController laserGroup;

    [Header("Interact")]
    public KeyCode interactKey = KeyCode.E;
    public float interactCooldown = 0.35f;

    [Header("Switch Handle Animation")]
    public Animator switchAnimator;                 
    public string handleTurnState = "handle_turn";
    public string handleUpState   = "handle_up";
    public bool alternateAnimEachUse = true;

    [Header("UI (optional)")]
    public CanvasGroup uiGroup;
    public TMPro.TMP_Text hintText;

    private bool _inside;
    private bool _cooldown;
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

    void Update()
    {
        if (!_inside) return;
        if (_cooldown) return;

        if (Input.GetKeyDown(interactKey))
            UseNode();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _inside = true;
        SetUI(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _inside = false;
        SetUI(false);
    }

    private void UseNode()
    {
        if (laserGroup == null)
        {
            Debug.LogWarning("[P3BypassNodeInteract] No laserGroup assigned.", this);
            StartCoroutine(Cooldown());
            return;
        }

        // 1) Cycle laser pattern ONLY (no drop)
        laserGroup.CyclePatternNoAutoDrop();

        // 2) Play handle animation
        PlaySwitchAnim();

        // 3) Update hint (optional correctness)
        RefreshHint();

        StartCoroutine(Cooldown());
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

    private IEnumerator Cooldown()
    {
        _cooldown = true;
        yield return new WaitForSeconds(interactCooldown);
        _cooldown = false;

        if (_inside) RefreshHint();
    }

    private void RefreshHint()
    {
        if (hintText == null) return;

        if (laserGroup != null && laserGroup.IsCurrentlyCorrect())
            hintText.text = "Reroute lasers with E.";
        else
            hintText.text = "Press E to reroute lasers";
    }

    private void SetUI(bool on)
    {
        if (uiGroup != null)
        {
            uiGroup.alpha = on ? 1f : 0f;
            uiGroup.blocksRaycasts = on;
            uiGroup.interactable = on;
        }

        if (on) RefreshHint();
    }
}
