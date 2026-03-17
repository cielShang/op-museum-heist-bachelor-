using UnityEngine;
using TMPro;

public class ChoiceUIManager : MonoBehaviour
{
    public static ChoiceUIManager Instance;

    [Header("UI")]
    public CanvasGroup group;
    public TMP_Text safeText;
    public TMP_Text riskyText;
    private Object _lockOwner;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Show(string safe, string risky)
    {
        if (safeText != null) safeText.text = safe;
        if (riskyText != null) riskyText.text = risky;

        SetVisible(true);
    }



    
    public void Lock(Object owner) => _lockOwner = owner;
    public void Unlock(Object owner) { if (_lockOwner == owner) _lockOwner = null; }

    
    public void Show(string safe, string risky, Object requester = null)
    {
        
        if (_lockOwner != null && _lockOwner != requester) return;

        if (safeText != null) safeText.text = safe;
        if (riskyText != null) riskyText.text = risky;

        SetVisible(true);
    }

    public void Hide(Object requester = null)
    {
        
        if (_lockOwner != null && _lockOwner != requester) return;

        SetVisible(false);
    }

public void ShowP2Choices_Default()
{
    Show(
        "BLUE: Steal the key by stunning the guard's system.",
        "ORANGE: Hack the terminal. Failure will have consequences"
    );
}

public void ShowP2Choices_AfterKey()
{
    Show(
        "BLUE: Use keycard on terminal",
        "."
    );
}

    void SetVisible(bool on)
    {
        if (group == null) return;
        group.alpha = on ? 1f : 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }
}
