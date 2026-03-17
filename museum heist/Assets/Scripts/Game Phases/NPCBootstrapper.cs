using UnityEngine;
using Convai.Scripts.Runtime.Core;

public class NPCBootstrapper : MonoBehaviour
{
    [Header("Assign BOTH NPCs in the scene")]
    public GameObject sakuraRoot;
    public GameObject milaRoot;

    public static ConvaiNPC ActiveNPC { get; private set; }

    void Awake()
    {
        NPCSelectionManager.EnsureExists();
    }

    void Start()
    {
        var chosen = NPCSelectionManager.Instance.Selected;

        if (sakuraRoot != null) sakuraRoot.SetActive(chosen == NPCPersonality.Sakura);
        if (milaRoot != null)   milaRoot.SetActive(chosen == NPCPersonality.Mila);

        var activeRoot = (chosen == NPCPersonality.Sakura) ? sakuraRoot : milaRoot;
        ActiveNPC = activeRoot != null ? activeRoot.GetComponentInChildren<ConvaiNPC>(true) : null;

        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.convaiNPC = ActiveNPC;
            GamePhaseManager.Instance.BeginPhase1();
        }
    }
}
