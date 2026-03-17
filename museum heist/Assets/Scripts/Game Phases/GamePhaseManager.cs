using UnityEngine;
using Convai.Scripts.Runtime.Core;

public enum HeistPhase { None = 0, Phase1 = 1, Phase2 = 2 }

public class GamePhaseManager : MonoBehaviour
{
    public static GamePhaseManager Instance { get; private set; }

    [Header("Assign your Convai NPC here (Sakura)")]
    public ConvaiNPC convaiNPC;

    [Header("Phase 2 choices")]
    public bool usingKeycardPlan;  // true only when player chose "take keycard" path

    [Header("Runtime state (read-only)")]
    public HeistPhase currentPhase = HeistPhase.None;
    public string currentLocation = "";
    public string nextStep = "";

    [Header("Intro timing")]
    public float firstPushDelay = 1.0f;   // small delay so Convai is ready
    
    [Header("Items")]
    public bool hasGuardKeycard;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // // set internal state
        // currentPhase   = HeistPhase.Phase1;
        // currentLocation = "Top exhibition level (start zone)";
        // nextStep        = "Reach the lower floor without alerting guards. Options: stairs (safe) or jump left (risky).";

        // // start background music
        // if (HeistMusicManager.Instance != null)
        //     HeistMusicManager.Instance.PlayAmbient();

        // if (convaiNPC != null)
        //     Invoke(nameof(StartPhase1Intro), firstPushDelay);
        // else
        //     Debug.LogWarning("[GamePhaseManager] No Convai NPC assigned.");
    }
    public void SetKeycardPlan(bool enabled) => usingKeycardPlan = enabled;

public void BeginPhase1()
{
    currentPhase    = HeistPhase.Phase1;
    currentLocation = "Top exhibition level (start zone)";
    nextStep        = "Reach the lower floor without alerting guards. Options: stairs (safe) or jump left (risky).";

    ChoiceUIManager.Instance.Show(
    "BLUE: Sneak through the corridor",
    "ORANGE: Go up the stairs to jump down. Be careful not to be seen."
);
    // GameRunLogger.Instance.Log("Phase1", "PhaseStart", "Phase 1 started");
    GameRunLogger.Instance.PhaseStart("Phase1");


    if (HeistMusicManager.Instance != null)
        HeistMusicManager.Instance.PlayAmbient();

    // if (convaiNPC != null)
    //     Invoke(nameof(StartPhase1Intro), firstPushDelay);
    else
        Debug.LogWarning("[GamePhaseManager] No Convai NPC assigned.");
}

    void StartPhase1Intro()
    {
        if (convaiNPC == null) return;

        var follow = convaiNPC.GetComponent<SakuraFollowPlayer>();
        if (follow != null) follow.SetTalking(true);
        // One single prompt that both sets context and asks her to talk
        string prompt =
            "You are Sakura, the player's heist partner. " +
            "Current game phase: 1, on top floor of the museum. " +
            "You already know the overall plan from your knowledge base. " +
            "In one short line (max 15 words) greet player and say you're above the target area. " +
            "Then, in 1 short sentence ask if they want the safe walk to the stairs or the risky but thrilling jump. " +
            "In 1 short sentence nudge towards the jump option shortly. No details unless asked";

        convaiNPC.TriggerSpeech(prompt);

        // stop talking after a few seconds
        StartCoroutine(EndTalkingAfterDelay(follow, 5f));
    }

    private System.Collections.IEnumerator EndTalkingAfterDelay (SakuraFollowPlayer follow, float delay )
    {
        
        if (follow == null) yield break;
        yield return new WaitForSeconds(delay);
        follow.SetTalking(false);
    }

    
    public void SetPhase(HeistPhase phase, string location, string next)
    {
        currentPhase  = phase;
        currentLocation = location;
        nextStep       = next;

        if (convaiNPC == null)
        {
            Debug.LogWarning("[GamePhaseManager] No Convai NPC assigned.");
            return;
        }

    }

    public void GiveGuardKeycard()
{
    hasGuardKeycard = true;

  
}

public void UseGuardKeycard()
{
    hasGuardKeycard = false;
}

}
