using UnityEngine;

public enum Phase3Route { None, Safe, Risky }

public class Phase3Manager : MonoBehaviour
{
    public static Phase3Manager Instance;

    [Header("State")]
    public Phase3Route activeRoute = Phase3Route.None;
    public bool introDone = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool RouteChosen => activeRoute != Phase3Route.None;
}
