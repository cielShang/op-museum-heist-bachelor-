using UnityEngine;
using UnityEngine.SceneManagement;
using Convai.Scripts.Runtime.Core;

public class DemoManager : MonoBehaviour
{
    public static DemoManager Instance;

    [Header("Scene")]
    public string mainGameSceneName = "MainGame";

    [Header("UI")]
    public TMPro.TMP_Text objectiveText;

    [Header("Convai Triggers")]
    public string demoStartTrigger = "DEMO_START";
    public string demoSafeChoiceTrigger = "DEMO_SAFE";
    public string demoRiskyChoiceTrigger = "DEMO_RISKY";
    public string demoEndTrigger = "DEMO_END";

    void Awake()
    {
        Instance = this;
    }

void Start()
{
    SetObjective("Move with WASD, Jump with Space.");

    var selected = NPCSelectionManager.Instance.Selected;

    //  start demo timing
    GameRunLogger.Instance?.SceneStart($"Demo_{selected}");

    // log which NPC
    GameRunLogger.Instance?.Log("Demo", "Demo_Start", "NPC", selected.ToString());
}



    public void SetObjective(string text)
    {
        if (objectiveText != null)
            objectiveText.text = text;
    }

    public void FireND(string trigger)
    {
        if (NPCBootstrapper.ActiveNPC != null)
            NPCBootstrapper.ActiveNPC.TriggerEvent(trigger);
    }

public void FinishDemo()
{
    var selected = NPCSelectionManager.Instance.Selected;

    FireND(demoEndTrigger);

    // Log demo duration
    GameRunLogger.Instance?.SceneEnd($"Demo_{selected}");

    // Log what scene comes next
    GameRunLogger.Instance?.Log("Scene", "Scene_Load", "NextScene", mainGameSceneName);

    SceneManager.LoadScene(mainGameSceneName);
}

}
