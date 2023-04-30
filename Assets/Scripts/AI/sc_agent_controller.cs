using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class sc_agent_controller : MonoBehaviour
{
    /* Control NavMeshAgent Action and Animation */

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    public Vector3 Target
    {
        private set;
        get;
    }

    public bool isAnimate = false;
    public bool showDebug = false;

    private void Start()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        Target = transform.position;

        if (isAnimate && animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }
    
    /// <summary>
    /// Move the agent to the desire position
    /// </summary>
    /// <param name="dest"> destination to go </param>
    public void GoTo(Vector3 dest)
    {
        if (!CanReachPoint(dest))
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(dest, out hit, 1.0f, NavMesh.AllAreas))
            {
                dest = hit.position;
            }
        }

        Target = dest;
        agent.SetDestination(dest);
    }

    /// <summary>
    /// Move the agent to a random position
    /// </summary>
    public void GoToRandom(float minimal, float maximal)
    {
        Vector3 target = agent.transform.position 
            + Random.insideUnitSphere * Random.Range(minimal, maximal);

        while (!CanReachPoint(target))
        {
            target = agent.transform.position
                + Random.insideUnitSphere * Random.Range(minimal, maximal);
        }

        GoTo(target);
    }

    /// <summary>
    /// Check if a point is reacheable
    /// </summary>
    /// <param name="target">point to check</param>
    /// <returns> result </returns>
    public bool CanReachPoint(Vector3 target)
    {
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(target, path);

        return path.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// Check if the agent is arrived at destination within a certain radius
    /// </summary>
    /// <param name="target"> destination to check </param>
    /// <param name="distance"> radius to check around target </param>
    /// <returns></returns>
    public bool IsArrived(Vector3 target, float distance)
    {
        var _pos = agent.transform.position;
        var _tar = target; // make var to not affect reference

        // ignore y axis
        _pos.y = 0;
        _tar.y = 0;

        return ((_tar - _pos).sqrMagnitude < distance * distance);
    }

    /// <summary>
    /// Change speed of the agent
    /// </summary>
    /// <param name="speed"> new speed </param>
    public void SetSpeed(float speed)
    {
        // check speed
        if (speed < 0)
        {
            speed = 0;
        }

        if (isAnimate)
        {
            animator.SetFloat("speed", speed);
        }

        agent.speed = speed;
    }


    public void OnDrawGizmos()
    {
        if (showDebug)
        {
            if (agent.hasPath)
            {
                Vector3[] corners = agent.path.corners;
                if (corners.Length >= 2)
                {
                    float height = agent.height;
                    Gizmos.color = Color.red;
                    for (int i = 1; i < corners.Length; i++)
                    {
                        Gizmos.DrawLine(corners[i - 1] + Vector3.up * height / 2, corners[i] + Vector3.up * height / 2);
                    }
                }
            }
        }
    }
}
