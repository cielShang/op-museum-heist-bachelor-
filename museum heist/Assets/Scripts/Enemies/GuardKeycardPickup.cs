using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GuardKeycardPickup : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;
    public string playerTag = "Player";

    [Header("Optional UI")]
    public CanvasGroup uiGroup;
    public TMPro.TMP_Text hintText;

    private bool _inside;
    private bool _taken;

    void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void Start()
    {
        SetUI(false);
    }

    void Update()
    {
        if (_taken) return;
        if (!_inside) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (GamePhaseManager.Instance != null)
                GamePhaseManager.Instance.hasGuardKeycard = true;

            _taken = true;
            SetUI(false);

            
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Debug.Log("[KeycardPickup] Keycard taken.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (_taken) return;
        _inside = true;
        SetUI(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _inside = false;
        SetUI(false);
    }

    public void ResetPickup()
    {
        _taken = false;
        _inside = false;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = true;

        SetUI(false);
    }

    void SetUI(bool on)
    {
        if (uiGroup == null) return;
        uiGroup.alpha = on ? 1f : 0f;
        uiGroup.blocksRaycasts = on;
        uiGroup.interactable = on;

        if (hintText != null)
            hintText.text = "Press E to take keycard";
    }
}
