using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class AiNavigation : MonoBehaviour
{
    public Transform target;
    public NavMeshAgent agent;
    private bool hasStopped = true;

    public UnityEvent<Vector3> OnStop;

    private void Start()
    {
        if (target != null)
            SetTarget(target);
    }

    void Update()
    {
        CheckStop();
    }

    private void CheckStop()
    {
        if (target != null)
        { 
            if (!hasStopped && !agent.hasPath && !agent.pathPending)
            {
                hasStopped = true;
                OnStop?.Invoke(target.position);
            }
        }
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
        agent.SetDestination(target.position);
        hasStopped = false;
    }

    private void OnValidate()
    {
        agent ??= GetComponent<NavMeshAgent>();
    }
}
