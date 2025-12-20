using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class MinotaurAI : MonoBehaviour
{
    [Header("Core")]
    public Transform target;
    public float catchDistance = 2.0f;

    [Header("Pathfinding Settings")]
    public float pathUpdateRate = 0.05f; // Fast updates for high speed tracking
    public float targetMoveThreshold = 0.5f;
    public float startDelay = 0.1f; // No mercy - start immediately

    // Internal
    private NavMeshAgent agent;
    private bool isReady = false;
    private float lastPathUpdateTime = 0f;
    private Vector3 lastTargetPosition;
    private Vector3 initialPosition;

    void Awake()
    {
        initialPosition = transform.position;
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Re-enable auto movement for reliability at low speeds
        agent.updatePosition = true; 
        agent.updateRotation = true;

        StartCoroutine(InitializeRoutine());
    }

    IEnumerator InitializeRoutine()
    {
        yield return new WaitForEndOfFrame();

        Animator anim = GetComponentInChildren<Animator>();
        if (anim) anim.applyRootMotion = false;

        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
            if (target == null && Camera.main) target = Camera.main.transform;
        }

        if (agent != null && !agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        if (target != null) lastTargetPosition = target.position;
        
        isReady = true;
        Debug.Log("[MinotaurAI] Ready and Charging!");
    }

    void Update()
    {
        if (!isReady || target == null) return;

        if (agent.isOnNavMesh)
        {
            agent.SetDestination(target.position);
        }

        if (!agent.pathPending && agent.remainingDistance <= catchDistance)
        {
             CatchPlayer();
        }
    }

    void CatchPlayer()
    {
        Debug.Log("Minotaur Caught Player!");

        if (target != null) 
        {
            MazeAgent mazeAgent = target.GetComponent<MazeAgent>();
            if (mazeAgent != null)
            {
                 mazeAgent.AddReward(-1.0f);
                 mazeAgent.EndEpisode(); 
                 return;
            }
        }

        isReady = false; 
        agent.isStopped = true; 
        
        if (GameManager.Instance) GameManager.Instance.GameOver();
        enabled = false;
    }
    public void ApplyStun(float duration)
    {
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isReady = false; 
        
        if(agent != null && agent.isOnNavMesh) 
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        Debug.Log($"[MinotaurAI] STUNNED for {duration} seconds!");

        yield return new WaitForSeconds(duration);

        if(agent != null && agent.isOnNavMesh) 
        {
            agent.isStopped = false;
        }
        
        isReady = true;
        
        lastPathUpdateTime = 0f; 
    }
    public void ResetPosition()
    {
        if (agent != null)
        {
             agent.Warp(initialPosition);
             agent.ResetPath();
             lastTargetPosition = initialPosition;
        }
    }
}