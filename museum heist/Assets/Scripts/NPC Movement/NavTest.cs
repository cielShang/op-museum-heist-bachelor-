using UnityEngine;
using UnityEngine.AI;

public class NavTest : MonoBehaviour
{
    public Transform target;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (target == null) return;

        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(target.position, path))
        {
            Debug.DrawLine(agent.transform.position, target.position, Color.green);
            if (path.status != NavMeshPathStatus.PathComplete)
                Debug.LogWarning(" Path incomplete or invalid: " + path.status);
        }
        else
        {
            Debug.LogError(" Path calculation failed entirely.");
        }
    }
}
