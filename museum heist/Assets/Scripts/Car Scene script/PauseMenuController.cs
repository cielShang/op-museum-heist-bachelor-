using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("UI")]
    public CanvasGroup menuGroup;          // the pause menu panel group
    public GameObject menuRoot;            // optional (panel root), can be null

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.Escape;

    [Header("Scenes")]
    public string mainMenuSceneName = "MenuScene";

    [Header("Behavior")]
    public bool pauseTimeWhenOpen = true;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        
    }

    void Start()
    {
        SetOpen(false, force: true);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (IsOpen) Close();
            else Open();
        }
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    private void SetOpen(bool open, bool force = false)
    {
        if (!force && IsOpen == open) return;
        IsOpen = open;

        if (menuRoot != null) menuRoot.SetActive(open);

        if (menuGroup != null)
        {
            menuGroup.alpha = open ? 1f : 0f;
            menuGroup.blocksRaycasts = open;
            menuGroup.interactable = open;
        }

        if (pauseTimeWhenOpen)
            Time.timeScale = open ? 0f : 1f;

        // unlock cursor while menu open (for mouse UI)
        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
    }

    // Resume
    public void UI_Resume()
    {
        Close();
    }

    // Main Menu
    public void UI_MainMenu()
{
    Time.timeScale = 1f;

    
    if (GameStateManager.Instance != null)
        GameStateManager.Instance.ResetForNewGame();

    SceneManager.LoadScene(mainMenuSceneName);
}


    // Quit
    public void UI_Quit()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
