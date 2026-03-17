using UnityEngine;
using Convai.Scripts.Runtime.Core;

public class GuardTakedownZone : MonoBehaviour
{
    public GuardDronePatrol guard;
    public ConvaiNPC convaiNPC;      // Sakura 
    public string playerTag = "Player";

    private bool _playerInside;
    private bool _used;

void Awake()
{
    if (convaiNPC == null && NPCBootstrapper.ActiveNPC != null)
        convaiNPC = NPCBootstrapper.ActiveNPC;
}


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            _playerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            _playerInside = false;
    }

    void Update()
    {
        if (_used || !_playerInside) return;

        // Player presses Q to takedown
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (guard != null && guard.IsStunned)
            {
                _used = true;

                guard.DisableGuard();

                // give keycard via GamePhaseManager
                if (GamePhaseManager.Instance != null)
                    GamePhaseManager.Instance.GiveGuardKeycard();
                
                GameStateManager.Instance.p2KeyTaken = true;

                if (ChoiceUIManager.Instance != null)
                {
                    ChoiceUIManager.Instance.Show(
                        "Use keycard on orange terminal [E] to shut off lasers",
                        ""
                    );
                }



                // enable npc to follow again
                NPCFollowController.ResumeFollowing();
                
            }
   
        }
    }
}
