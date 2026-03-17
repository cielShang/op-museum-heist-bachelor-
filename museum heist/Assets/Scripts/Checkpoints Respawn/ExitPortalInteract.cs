using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExitPortalInteract : MonoBehaviour
{
    [Header("UI Panel (press 1 to leave)")]
    public CanvasGroup panelGroup;

    [Header("Input")]
    public KeyCode confirmKey = KeyCode.Alpha1;

    [Header("Scene")]
    public string endSceneName = "EndScene";

    private bool _playerInside;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Start()
    {
        SetPanel(false); // hidden by default
    }

    void Update()
    {
        if (!_playerInside) return;

        if (Input.GetKeyDown(confirmKey))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(endSceneName);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInside = true;
        SetPanel(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInside = false;
        SetPanel(false);
    }

    private void SetPanel(bool on)
    {
        if (panelGroup == null) return;

        panelGroup.alpha = on ? 1f : 0f;
        panelGroup.interactable = on;
        panelGroup.blocksRaycasts = on;
    }
}
