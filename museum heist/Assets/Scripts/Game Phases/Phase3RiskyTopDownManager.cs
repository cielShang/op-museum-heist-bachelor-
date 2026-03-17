using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Convai.Scripts.Runtime.Addons; // ConvaiPlayerMovement
using TMPro;

public class Phase3RiskyTopDownManager : MonoBehaviour
{
    [Header("Cameras")]
    public Camera fpsCamera;
    public Camera topDownCamera;

    [Header("Player")]
    public GameObject player;
    public ConvaiPlayerMovement fpsMovement;
    public TopDownPlayerController topDownController;
    public NavMeshAgent playerAgent;

    [Header("Enemies")]
    public Phase3TopDownEnemy[] enemies;

    [Header("Nodes (Hold E)")]
    public List<P3SecurityNodeHold> nodes = new List<P3SecurityNodeHold>();

    [Header("Vitrine Blocker / Unlock Terminal")]
    [Tooltip("The blocker GameObject that prevents reaching the vitrine path.")]
    public GameObject vitrineBlocker;

    [Header("Vitrine Pulse + ND")]
    public P3PulseObject vitrinePulse;     // assign (or auto-find)
    public string ndReachedVitrineEvent = "P3_RISKY_TO_VITRINE"; // your event name


    [Tooltip("Trigger collider placed at the blocker/terminal spot (player presses E here after nodes).")]
    public Collider vitrineUnlockTrigger;

    [Tooltip("Key to interact with the vitrine unlock after nodes are done.")]
    public KeyCode vitrineUnlockKey = KeyCode.E;

    [Header("Vitrine Glow / Highlight (optional but recommended)")]
    [Tooltip("Assign a Renderer (or multiple via children) that should pulse/glow when unlocked.")]
    public Renderer[] vitrineHighlightRenderers;

    [Tooltip("Emission color used for highlight. Only works if the material supports emission.")]
    public Color highlightEmissionColor = Color.cyan;

    public float pulseSpeed = 2.0f;
    public float pulseMin = 0.2f;
    public float pulseMax = 2.0f;

    [Header("UI (Big Objective Text)")]
    [Tooltip("Big on-screen objective. Use TextMeshProUGUI.")]
    public TMP_Text objectiveText;

    [Tooltip("Optional: show/hide this canvas group (for fading).")]
    public CanvasGroup objectiveCanvasGroup;

    [Tooltip("Font size suggestion for build readability. Manager can set it at runtime if you want.")]
    public int objectiveFontSize = 46;

    [Header("Debug")]
    public bool debugLogs = true;

    private bool _active;
    private int _completedNodes;
    private bool _nodesComplete;
    private bool _playerInVitrineUnlockZone;

    // Cache materials so we don't instantiate every frame
    private readonly List<Material> _highlightMats = new List<Material>();

    void Awake()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && playerAgent == null) playerAgent = player.GetComponent<NavMeshAgent>();
        if (player != null && fpsMovement == null) fpsMovement = player.GetComponent<ConvaiPlayerMovement>();
        if (player != null && topDownController == null) topDownController = player.GetComponent<TopDownPlayerController>();

        CacheHighlightMaterials();
        SetHighlight(false);

        if (objectiveText != null)
            objectiveText.fontSize = objectiveFontSize;

        SetObjectiveVisible(false);
    }

    void Update()
    {
        if (!_active) return;

        // Pulse highlight once nodes are complete
        if (_nodesComplete)
            PulseHighlight();

        // If player is standing at the vitrine unlock trigger, allow E to finalize
        if (_nodesComplete && _playerInVitrineUnlockZone && Input.GetKeyDown(vitrineUnlockKey))
        {
            if (debugLogs) Debug.Log("[P3 Risky] Player unlocked vitrine path via E.");
            FinalizeUnlockAndReturnToFPS();
        }
    }

    public void StartRiskyPhase3()
    {
        if (_active) return;
        _active = true;
        _nodesComplete = false;
        _playerInVitrineUnlockZone = false;

        if (debugLogs) Debug.Log("[P3 Risky] StartRiskyPhase3()", this);

        // cameras
        if (fpsCamera != null) fpsCamera.enabled = false;
        if (topDownCamera != null) topDownCamera.enabled = true;

        // disable fps movement
        if (fpsMovement != null) fpsMovement.enabled = false;

        // enable topdown controller
        if (topDownController != null)
        {
            topDownController.topDownCamera = topDownCamera;
            topDownController.enabled = true;
        }

       
        if (playerAgent != null)
        {
            // If your TopDownPlayerController does NOT use NavMeshAgent, keep this disabled.
            // If it DOES use NavMeshAgent, keep enabled.
            // playerAgent.enabled = true;
        }

        // activate enemies
        if (enemies != null)
        {
            foreach (var e in enemies)
                if (e != null) e.SetActive(true);
        }

        // reset nodes
        _completedNodes = 0;

        // If nodes list is empty, auto-find them in scene (optional safety)
        if (nodes == null || nodes.Count == 0)
        {
            nodes = new List<P3SecurityNodeHold>(FindObjectsOfType<P3SecurityNodeHold>(true));
            if (debugLogs) Debug.Log("[P3 Risky] Auto-found nodes in scene: " + nodes.Count);
        }

        foreach (var n in nodes)
        {
            if (n == null) continue;
            n.manager = this;
            n.completed = false;
            n.gameObject.SetActive(true);
        }

        // ensure blocker exists at start
        if (vitrineBlocker != null) vitrineBlocker.SetActive(true);

        // disable highlight at start
        SetHighlight(false);

            if (vitrineBlocker != null && vitrinePulse == null)
        vitrinePulse = vitrineBlocker.GetComponent<P3PulseObject>();

    if (vitrinePulse != null)
        vitrinePulse.SetPulse(false);


        // UI objective
        // SetObjectiveVisible(true);
        // SetObjectiveText("Hit enemies[Q] and disable the 3 Nodes.");
        if (ChoiceUIManager.Instance != null)
        {
            ChoiceUIManager.Instance.Lock(this);
            ChoiceUIManager.Instance.Show(
                "",
                "Sneak behind enemies & Hit [Q]. Then look at the arrows."
            );
        }
    }

    // Called by P3SecurityNodeHold.Complete()
    public void OnNodeCompleted(P3SecurityNodeHold node)
    {
        _completedNodes++;

        int total = (nodes != null) ? nodes.Count : 0;
        if (debugLogs) Debug.Log($"[P3 Risky] Node completed {_completedNodes}/{total}", this);

        if (total > 0)
            // SetObjectiveText($"Security nodes hacked: {_completedNodes}/{total}");

             if (ChoiceUIManager.Instance != null)
        {
            ChoiceUIManager.Instance.Show(
                $"Security nodes hacked: {_completedNodes}/{total}",
                " "
            );
        }

        if (nodes != null && _completedNodes >= nodes.Count)
            OnAllNodesCompleted();
    }

    private void OnAllNodesCompleted()
    {
        _nodesComplete = true;

        if (debugLogs) Debug.Log("[P3 Risky] All nodes completed. Waiting for vitrine unlock interaction.", this);

        // Start highlighting the blocker/terminal area so player knows where to go
        SetHighlight(true);

        // Update objective
        // SetObjectiveText("All nodes hacked! Unlock the way!");

                if (ChoiceUIManager.Instance != null)
        {
            ChoiceUIManager.Instance.Show(
                "",
                "All nodes hacked! Unlock the way! "
            );
        }
        if (_completedNodes >= nodes.Count)
{
    // Start pulsing to attract attention
    if (vitrinePulse != null)
        vitrinePulse.SetPulse(true);

    // If you have an objective text system, switch it here too:
    // SetObjective("Go to the vitrine and press E to open the path!");
    
    // IMPORTANT: do NOT switch camera back here anymore (you wanted it to stay top-down)
    // So do NOT call CompleteRiskyTopDown() here.
}

    }

    // Hook this to your vitrineUnlockTrigger collider (see instructions below)
    public void SetPlayerInVitrineUnlockZone(bool inside)
    {
        _playerInVitrineUnlockZone = inside;

        if (!_nodesComplete) return;

        if (inside)
            // SetObjectiveText("Press [E] to unlock the vitrine path.");
             if (ChoiceUIManager.Instance != null)
        {
            ChoiceUIManager.Instance.Show(
                "",
                "Press [E] to unlock the path. "
            );
        }
        // else
        //     SetObjectiveText("Path is open. Move towards the vitrine.");
    }

    private void FinalizeUnlockAndReturnToFPS()
    {
        // open path
        if (vitrineBlocker != null) vitrineBlocker.SetActive(false);
        var npc = NPCBootstrapper.ActiveNPC;
        if (npc != null)
            npc.TriggerEvent("P3_RISKY_TO_VITRINE");

                if (ChoiceUIManager.Instance != null)
        {
            ChoiceUIManager.Instance.Hide(this);
            ChoiceUIManager.Instance.Unlock(this);
        }


        // stop enemies (freeze them)
        if (enemies != null)
        {
            foreach (var e in enemies)
                if (e != null) e.SetActive(false);
        }

        // stop highlight
        SetHighlight(false);

        // hide objective (or keep it, your choice)
        // SetObjectiveText("Unlock the path.");
        // Optionally fade out after a bit
        // SetObjectiveVisible(false);

        // camera back
        if (topDownCamera != null) topDownCamera.enabled = false;
        if (fpsCamera != null) fpsCamera.enabled = true;

        // disable topdown controller
        if (topDownController != null) topDownController.enabled = false;

        // re-enable fps movement
        if (fpsMovement != null) fpsMovement.enabled = true;

        _active = false;
    }

    // ---------- UI Helpers ----------
    private void SetObjectiveText(string msg)
    {
        if (objectiveText != null)
            objectiveText.text = msg;
    }

    private void SetObjectiveVisible(bool visible)
    {
        if (objectiveCanvasGroup != null)
        {
            objectiveCanvasGroup.alpha = visible ? 1f : 0f;
            objectiveCanvasGroup.blocksRaycasts = false;
            objectiveCanvasGroup.interactable = false;
        }
        else if (objectiveText != null)
        {
            objectiveText.enabled = visible;
        }
    }

    // ---------- Highlight Helpers ----------
    private void CacheHighlightMaterials()
    {
        _highlightMats.Clear();
        if (vitrineHighlightRenderers == null) return;

        foreach (var r in vitrineHighlightRenderers)
        {
            if (r == null) continue;

            // Using .materials creates instances (fine for highlight)
            foreach (var m in r.materials)
            {
                if (m != null && !_highlightMats.Contains(m))
                    _highlightMats.Add(m);
            }
        }
    }

    private void SetHighlight(bool on)
    {
        if (_highlightMats.Count == 0) return;

        foreach (var m in _highlightMats)
        {
            if (m == null) continue;

            // Ensure emission is enabled
            if (on)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", highlightEmissionColor * pulseMin);
            }
            else
            {
                // turn emission down
                m.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    private void PulseHighlight()
    {
        if (_highlightMats.Count == 0) return;

        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1
        float intensity = Mathf.Lerp(pulseMin, pulseMax, t);

        foreach (var m in _highlightMats)
        {
            if (m == null) continue;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", highlightEmissionColor * intensity);
        }
    }
}
