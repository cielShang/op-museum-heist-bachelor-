using System.Collections;
using UnityEngine;
using TMPro;
using Convai.Scripts.Runtime.Core;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;


public class EndSceneManager : MonoBehaviour
{
    public static EndSceneManager Instance { get; private set; }

    [Header("ND Triggers (same trigger for both is fine)")]
    public string milaTrigger = "END_SPEECH";
    public string sakuraTrigger = "END_SPEECH";

    [Header("End Screen UI")]
    public CanvasGroup endScreenGroup;
    public TMP_Text endScreenText;

    [Header("Input")]
    public KeyCode mainMenuKey = KeyCode.Alpha1;
    public KeyCode quitKey = KeyCode.Alpha2;          
    public KeyCode toggleEndScreenKey = KeyCode.Escape; 

    [Header("Speech Finish Detection")]
    public float silenceSettleSeconds = 0.6f;
    public float maxWaitSeconds = 180f;

   
private bool _savedCursorVisible;
private CursorLockMode _savedCursorLockMode;
private bool _cursorStateSaved;


    private bool _endScreenActive;   
    private bool _endScreenVisible;  

    void Awake()
    {
        Instance = this;
    }

private void Start()
{
    NPCSelectionManager.EnsureExists();
    SetEndScreen(false);
    ApplyGameplayInputState(); // ensure correct state on entry
    StartCoroutine(BeginEndSpeechRoutine());

    if (GameRunLogger.Instance != null)
{
    GameRunLogger.Instance.Log("Game", "End", "ReachedEndScreen", "");
    GameRunLogger.Instance.WriteFile();
}

}


    public void ForceShowEndScreen()
    {
        // make it active and visible immediately
        _endScreenActive = true;
        ShowEndScreenNow();
    }

    IEnumerator BeginEndSpeechRoutine()
    {
        yield return null;

        ConvaiNPC npc = NPCBootstrapper.ActiveNPC;
        if (npc == null)
        {
            Debug.LogError("[EndSceneManager] ActiveNPC is null - check NPCBootstrapper wiring.");
            yield break;
        }

        string trigger = (NPCSelectionManager.Instance.Selected == NPCPersonality.Mila)
            ? milaTrigger
            : sakuraTrigger;

        npc.InterruptCharacterSpeech();
        npc.TriggerEvent(trigger);

        yield return StartCoroutine(WaitUntilNpcFinishedSpeaking(npc));

        _endScreenActive = true;
        ShowEndScreenNow();
    }

    IEnumerator WaitUntilNpcFinishedSpeaking(ConvaiNPC npc)
    {
        AudioSource voice = npc.GetComponentInChildren<AudioSource>(true);
        if (voice == null)
        {
            Debug.LogWarning("[EndSceneManager] No AudioSource found under ActiveNPC. Falling back to maxWaitSeconds.");
            yield return new WaitForSeconds(maxWaitSeconds);
            yield break;
        }

        float startTime = Time.time;
        float silentTimer = 0f;

        while (Time.time - startTime < maxWaitSeconds)
        {
            if (voice.isPlaying)
            {
                silentTimer = 0f;
            }
            else
            {
                silentTimer += Time.deltaTime;
                if (silentTimer >= silenceSettleSeconds)
                    yield break;
            }

            yield return null;
        }

        Debug.LogWarning("[EndSceneManager] Hit maxWaitSeconds. Showing end screen anyway.");
    }

void ShowEndScreenNow()
{
    if (!_endScreenActive) return;

    _endScreenVisible = true;
    SetEndScreen(true);

    // Make UI clickable while end screen is visible
    Cursor.visible = true;
    Cursor.lockState = CursorLockMode.None;
}

void HideEndScreen()
{
    _endScreenVisible = false;
    SetEndScreen(false);

    ApplyGameplayInputState(); // <-- THIS is the important part
}



    void ToggleEndScreen()
    {
        if (!_endScreenActive) return; // only allow toggling after it’s available
        if (_endScreenVisible) HideEndScreen();
        else ShowEndScreenNow();
    }

    void Update()
    {
        if (!_endScreenActive) return;

        
        if (Input.GetKeyDown(toggleEndScreenKey))
            ToggleEndScreen();

        // prevents accidental presses
        if (_endScreenVisible)
        {
            if (Input.GetKeyDown(mainMenuKey))
    GoToMainMenu();


            if (Input.GetKeyDown(quitKey))
                QuitGame();
        }
    }
 private void GoToMainMenu()
{
    // ensure gameplay isn't left paused / cursor weird
    ApplyGameplayInputState();
    Time.timeScale = 1f;

    
    if (GameStateManager.Instance != null)
        GameStateManager.Instance.ResetForNewGame();

    SceneManager.LoadScene("MenuScene");
}

    private void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetEndScreen(bool on)
    {
        if (endScreenGroup == null) return;

        endScreenGroup.alpha = on ? 1f : 0f;
        endScreenGroup.blocksRaycasts = on;
        endScreenGroup.interactable = on;
    }

    private void ApplyGameplayInputState()
{
    
    Cursor.visible = false;
    Cursor.lockState = CursorLockMode.Locked;

    
    if (EventSystem.current != null)
        EventSystem.current.SetSelectedGameObject(null);

    
    Time.timeScale = 1f;
}

}
