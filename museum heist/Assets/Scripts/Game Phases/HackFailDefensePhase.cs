using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Convai.Scripts.Runtime.Core;

public class HackFailDefensePhase : MonoBehaviour
{
    public DroneWaveManager waveManagerToClear; // assign in inspector

    [Header("NPC + Movement")]
    [Tooltip("Leave empty. Will auto-use NPCBootstrapper.ActiveNPC")]
    public ConvaiNPC activeNPC;

    [Tooltip("Optional: a follow script to disable during defense (MilaFollowPlayer / SakuraFollowPlayer etc).")]
    public MonoBehaviour followScript;

    [Tooltip("Where the NPC should stand during the emergency override.")]
    public Transform npcDefensePoint;

    [Header("Defense Gameplay")]
    public EliteWaveManager waveManager;
    public SakuraHackUI hackProgressUI;          // you can rename later; it's just a progress UI
    public LaserGateController laserGate;

    [Header("Timings")]
    public float npcHackDuration = 15f;
    public float moveBeforeTalkDelay = 0.25f;
    public float afterLineDelay = 1.5f;

    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip defenseMusicClip;

    [Header("Narrative Design Trigger Names")]
    public string nd_OnHackFailStart = "P2_HACK_FAIL_START";
    public string nd_OnHackFailComplete = "P2_HACK_FAIL_COMPLETE";

    [Header("Debug")]
    public bool debugLogs = true;

    private bool _defenseRunning;

  void Awake()
{
    if (activeNPC == null && NPCBootstrapper.ActiveNPC != null)
        activeNPC = NPCBootstrapper.ActiveNPC;

    // auto-grab follow component from whichever NPC is active
    if (followScript == null && activeNPC != null)
        followScript = activeNPC.GetComponent<SakuraFollowPlayer>();
}


    // Called by HackMinigame.FailHack(reason)
  public void OnHackFailed(string reason)
{
    if (_defenseRunning) return;
    _defenseRunning = true;

    if (activeNPC == null && NPCBootstrapper.ActiveNPC != null)
        activeNPC = NPCBootstrapper.ActiveNPC;

    if (followScript == null && activeNPC != null)
        followScript = activeNPC.GetComponent<SakuraFollowPlayer>();

    StartCoroutine(HackFailedSequence(reason));
}


    private IEnumerator HackFailedSequence(string reason)
    {
        // 1) Stop following
        if (followScript != null) followScript.enabled = false;

        // 2) Move NPC to terminal/defense point
        if (activeNPC != null && npcDefensePoint != null)
        {
            var agent = activeNPC.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(npcDefensePoint.position);

                while (Vector3.Distance(activeNPC.transform.position, npcDefensePoint.position) > 0.5f)
                    yield return null;

                agent.isStopped = true;
            }
        }

        yield return new WaitForSeconds(moveBeforeTalkDelay);

        // 3) Narrative Design: “hack failed -> defend me” (NPC-specific line lives in ND graph)
        if (activeNPC != null)
        {
            // Optional: clear any speech
            activeNPC.InterruptCharacterSpeech();

            // Fire ND trigger. Put the actual text in the ND graph for Mila & Sakura.
            activeNPC.TriggerEvent(nd_OnHackFailStart);
        }

        yield return new WaitForSeconds(afterLineDelay);

        // 4) Music
        if (musicSource != null && defenseMusicClip != null)
        {
            musicSource.clip = defenseMusicClip;
            musicSource.loop = true;
            musicSource.Play();
        }

        // 5) Start waves
        if (waveManager != null)
        {
            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
            waveManager.BeginDefense(player);
        }

        // 6) Start hack progress
        if (hackProgressUI != null)
        {
            hackProgressUI.BeginProgress(npcHackDuration, this);
        }
    }

    // Called by SakuraHackUI when bar completes
    public void OnDefenseCompleted()
    {
        if (debugLogs)
            Debug.Log("[HackFailDefensePhase] Defense completed.", this);

        // stop waves
        if (waveManager != null)
        {
            waveManager.StopDefense();
            waveManager.KillAllElites();
        }

        // stop music
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();

        // disable lasers
        if (laserGate != null)
            laserGate.DisableForSeconds(laserGate.hackDisableSeconds, "NPC emergency override");

            if (waveManagerToClear != null)
    waveManagerToClear.ForceClearAllEnemies(playDeath: true);

        // ND trigger: “done”
        if (activeNPC == null && NPCBootstrapper.ActiveNPC != null)
            activeNPC = NPCBootstrapper.ActiveNPC;

        if (activeNPC != null)
        {
            activeNPC.InterruptCharacterSpeech();
            activeNPC.TriggerEvent(nd_OnHackFailComplete);
        }

        // re-enable follow
        if (followScript != null)
            followScript.enabled = true;

        _defenseRunning = false;
    }
}
