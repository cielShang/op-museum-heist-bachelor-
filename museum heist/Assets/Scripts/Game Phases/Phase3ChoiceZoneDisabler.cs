using UnityEngine;

public class Phase3ChoiceZoneDisabler : MonoBehaviour
{
    public static Phase3ChoiceZoneDisabler Instance;

    public GameObject riskyZone;
    public GameObject safeZoneA;
    public GameObject safeZoneB;

    void Awake()
    {
        Instance = this;
    }

    public static void DisableAll()
    {
        if (Instance == null) return;

        Instance.riskyZone.SetActive(false);
        Instance.safeZoneA.SetActive(false);
        Instance.safeZoneB.SetActive(false);
    }
}
