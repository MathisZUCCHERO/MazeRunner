using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MinotaurAI : MonoBehaviour
{
    [Header("Settings")]
    public Transform target; // The Player
    public float catchDistance = 1.5f;
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 6.0f;

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Auto-find player if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        agent.speed = chaseSpeed;
    }

    void Update()
    {
        if (target == null) return;

        // Simple State Machine: Always Chase for this MVP
        // In a full game, you might add Patrol vs Chase logic based on distance/vision
        
        agent.SetDestination(target.position);

        // Check if caught
        if (!agent.pathPending && agent.remainingDistance <= catchDistance)
        {
            CatchPlayer();
        }
    }

    void CatchPlayer()
    {
        // Prevent multiple calls
        enabled = false; 
        agent.isStopped = true;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}
