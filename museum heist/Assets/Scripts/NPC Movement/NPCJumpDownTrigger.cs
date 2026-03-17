using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Convai.Scripts.Runtime.Core;

[RequireComponent(typeof(Collider))]
public class NPCJumpDownTrigger : MonoBehaviour
{
    [Header("Jump setup")]
    public Transform landingPoint;       // JumpDownLanding on lower floor
    public float jumpDuration = 1.0f;    // time of the jump in seconds
    public float jumpArcHeight = 1.5f;   // how high the arc goes

    [Header("Optional")]
    public ConvaiNPC convaiNPC;          // Sakura (for voice line)

    bool _hasJumped = false;

    void Reset()
    {
        // Ensure collider is trigger by default
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // Only react to Sakura / NPC
        if (_hasJumped) return;
        if (!other.CompareTag("Character")) return;

        NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
        Animator animator   = other.GetComponent<Animator>();

        if (agent == null || animator == null || landingPoint == null)
        {
            Debug.LogWarning("[NPCJumpDownTrigger] Missing components or landingPoint.");
            return;
        }

        StartCoroutine(JumpSequence(other.transform, agent, animator));
    }

    IEnumerator JumpSequence(Transform npc, NavMeshAgent agent, Animator animator)
    {
        _hasJumped = true;

        // // Optional: say something before jumping
        // if (convaiNPC != null)
        // {
        //     convaiNPC.InterruptCharacterSpeech();
        //     convaiNPC.TriggerSpeech(
        //         "[JUMP] Risky route it is. I'll drop to the floor below—follow my lead."
        //     );
        // }

        // Stop NavMesh movement while we animate the jump
        agent.enabled = false;

        Vector3 startPos = npc.position;
        Vector3 endPos   = landingPoint.position;

        // Play your jump animation state 
        animator.CrossFade(Animator.StringToHash("Jumping"), 0.1f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / jumpDuration;
            float arc = Mathf.Sin(Mathf.PI * t) * jumpArcHeight; // simple arc

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += arc;

            npc.position = pos;
            yield return null;
        }

// Snap exactly to landing point
npc.position = endPos;

// Re-sync NavMeshAgent with new position
agent.Warp(endPos);
agent.enabled = true;

// NEW: pause following until player lands
var follow = npc.GetComponent<SakuraFollowPlayer>();
if (follow != null)
{
    follow.SetFollowEnabled(false);
}

// Back to idle
animator.CrossFade(Animator.StringToHash("Idle"), 0.1f);

       // GamePhaseManager.Instance.SakuraJumpReaction();

        // Tell the phase manager we've entered Phase 2
        if (GamePhaseManager.Instance != null)
{
    GamePhaseManager.Instance.SetPhase(
        HeistPhase.Phase2,
        "Middle floor via risky jump route.",
        "Avoid detection and figure out how to disable the lasers. Either crack the terminal quick and higher risk, or take out the drone guard and safely disable the laser."
        
    );
}


    }
}
