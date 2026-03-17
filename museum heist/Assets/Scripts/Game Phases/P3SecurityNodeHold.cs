using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider))]
public class P3SecurityNodeHold : MonoBehaviour
{
    public Phase3RiskyTopDownManager manager;

    [Header("Hack Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float hackSeconds = 2.5f;

    [Header("UI (worldspace)")]
    [Tooltip("Drag your HintGroup (Panel) OR the NodeUI root here. It will be SetActive(true/false).")]
    public GameObject uiRoot;

    [Tooltip("Drag ProgressFill (Image Type = Filled).")]
    public Image progressFill;   // 0..1 via fillAmount

    [Tooltip("Drag HintText (TMP). Optional.")]
    public TMP_Text hintText;

    [Header("State")]
    public bool completed;

    private bool _playerInside;
    private float _t;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Start()
    {
        SetUI(false);
        SetProgress(0f);
        if (hintText != null) hintText.text = $"Hold {interactKey} to hack";
    }

    void Update()
    {
        if (completed) return;
        if (!_playerInside) return;

        bool holding = Input.GetKey(interactKey);

        if (holding)
        {
            _t += Time.deltaTime;
            float p = Mathf.Clamp01(_t / hackSeconds);
            SetProgress(p);

            if (p >= 1f)
                Complete();
        }
        else
        {
            // reset if released
            if (_t > 0f)
            {
                _t = 0f;
                SetProgress(0f);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (completed) return;

        _playerInside = true;
        SetUI(true);
        if (hintText != null) hintText.text = $"Hold {interactKey} to hack";
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (completed) return;

        _playerInside = false;
        _t = 0f;
        SetProgress(0f);
        SetUI(false);
    }

    void Complete()
    {
        completed = true;
        _playerInside = false;

        // lock UI + show full bar briefly (optional)
        SetProgress(1f);
        SetUI(false);

        // disable trigger so it can't be used again
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // hide node visuals (optional)
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = false;

        // tell manager
        if (manager != null)
            manager.OnNodeCompleted(this);
    }

void SetUI(bool on)
{
    if (uiRoot == null) return;

    uiRoot.SetActive(true); // keep it active so CanvasGroup can control visibility

    var cg = uiRoot.GetComponent<CanvasGroup>();
    if (cg != null)
    {
        cg.alpha = on ? 1f : 0f;
        cg.blocksRaycasts = on;
        cg.interactable = on;
    }
    else
    {
        // no CanvasGroup => fallback to SetActive
        uiRoot.SetActive(on);
    }
}


    void SetProgress(float v)
    {
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(v);
    }
}
