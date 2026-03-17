using UnityEngine;
using UnityEngine.AI;
using Convai.Scripts.Runtime.Core;

public class EndSceneLockPlacement : MonoBehaviour
{
    [Header("Where to force them to be (scene transforms)")]
    public Transform playerSpot;
    public Transform npcSpot;

    [Header("If true, we hard-lock NPC position/rotation after placing")]
    public bool disableNpcMovementScripts = true;

    void Start()
    {
        // Player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && playerSpot != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.SetPositionAndRotation(playerSpot.position, playerSpot.rotation);

            if (cc != null) cc.enabled = true;
        }

        // NPC (active one)
        ConvaiNPC npc = NPCBootstrapper.ActiveNPC;
        if (npc != null && npcSpot != null)
        {
            // stop follow script if present
            var follow = npc.GetComponentInChildren<SakuraFollowPlayer>(true);
            if (follow != null)
                follow.SetFollowEnabled(false);

            // stop nav agent if present (or warp safely)
            var agent = npc.GetComponentInChildren<NavMeshAgent>(true);
            if (agent != null)
            {
                // Warp prevents agent from walking from an old position
                agent.Warp(npcSpot.position);
                agent.transform.rotation = npcSpot.rotation;

                // freeze agent movement in end scene
                agent.isStopped = true;
                agent.updatePosition = false;
                agent.updateRotation = false;
            }
            else
            {
                npc.transform.SetPositionAndRotation(npcSpot.position, npcSpot.rotation);
            }

            if (disableNpcMovementScripts)
            {
                // If anything else tries to move them, disable it here (safe)
                if (follow != null) follow.enabled = false;
            }
        }
    }
}
