using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class GuardStunDevice : MonoBehaviour
{
    [Header("Target Guard")]
    public GuardDronePatrol targetGuard;

    [Header("Stun Settings")]
    public float stunDuration = 6f;

    [Header("Charge Interaction")]
    public float chargeTime = 1.2f;
    public float successWindowMin = 0.75f;
    public float successWindowMax = 1.05f;

    [Header("UI")]
    public CanvasGroup uiGroup;
    public Image chargeRing;
    public Image successWindow;
    public Image backgroundRing;
    public TMPro.TMP_Text instructionText;
    public string textBefore = "Hold [E] & release to short circuit guards";
    public string textAfter  = "Guard stunned! Take his keycard [Q]";



    private bool _playerInside;
    private float _charge;
    private bool _used;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        SetupSuccessWindow();
        SetUI(false);

        if (instructionText != null)
        instructionText.text = textBefore;

    }

void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Player")) return;

    _playerInside = true;

    // Always show UI when entering (even after it's been used)
    SetUI(true);

    // Show the correct instruction depending on state
    if (instructionText != null)
        instructionText.text = _used ? textAfter : textBefore;

    // Optional: reset charge visuals when re-entering (nice/clean)
    _charge = 0f;
    UpdateChargeUI();
}


    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInside = false;
        _charge = 0f;
        UpdateChargeUI();
        SetUI(false);
    }

    void Update()
    {
        if (_used || !_playerInside || targetGuard == null)
            return;

        if (Input.GetKey(KeyCode.E))
        {
            _charge += Time.deltaTime;
            _charge = Mathf.Clamp(_charge, 0f, chargeTime);
            UpdateChargeUI();
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            TryTriggerStun();
            _charge = 0f;
            UpdateChargeUI();
        }
    }

 void TryTriggerStun()
{
    if (_charge >= successWindowMin && _charge <= successWindowMax)
    {
        targetGuard.ApplyStun(stunDuration);
        _used = true;

        if (instructionText != null)
            instructionText.text = textAfter;

        // Keep UI visible so the player reads the next step
        // (Do NOT hide it immediately)
        Debug.Log("[StunDevice] EMP pulse successful.");
    }
    else
    {
        Debug.Log("[StunDevice] EMP pulse mis-timed.");
    }
}


    void UpdateChargeUI()
    {
        if (chargeRing == null) return;
        chargeRing.fillAmount = _charge / chargeTime;
    }

    void SetupSuccessWindow()
    {
        if (successWindow == null) return;

        float windowSize = (successWindowMax - successWindowMin) / chargeTime;
        float startOffset = successWindowMin / chargeTime;

        successWindow.fillAmount = windowSize;
        successWindow.transform.localRotation =
            Quaternion.Euler(0f, 0f, -360f * startOffset);
    }

 void SetUI(bool on)
{
    if (uiGroup == null) return;

    uiGroup.alpha = on ? 1f : 0f;
    uiGroup.blocksRaycasts = false;
    uiGroup.interactable = false;

    // ensure background ring stays visible
    if (backgroundRing != null)
        backgroundRing.enabled = on;
}

}
