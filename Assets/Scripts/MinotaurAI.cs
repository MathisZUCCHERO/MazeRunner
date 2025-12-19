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
    public float pathUpdateRate = 0.2f; 
    public float targetMoveThreshold = 0.5f;

    // Internal
    private NavMeshAgent agent;
    private bool isReady = false;
    private float lastPathUpdateTime = 0f;
    private Vector3 lastTargetPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false; 
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
        Debug.Log("[MinotaurAI] Ready!");
    }

    void Update()
    {
        if (!isReady || target == null) return;

        // --- 1. Logique de Déplacement ---
        if (Time.time >= lastPathUpdateTime + pathUpdateRate)
        {
            if (!agent.pathPending)
            {
                float distanceMoved = Vector3.Distance(target.position, lastTargetPosition);
                // Si remainingDistance est très petit, c'est qu'on a fini le chemin précédent
                bool reachedDestination = agent.remainingDistance < 0.5f;

                if (distanceMoved > targetMoveThreshold || !agent.hasPath || reachedDestination)
                {
                    agent.SetDestination(target.position);
                    lastTargetPosition = target.position;
                    lastPathUpdateTime = Time.time;
                }
            }
        }

        if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= catchDistance)
        {
            Debug.Log("Minotaur is at " + agent.remainingDistance + " the catchDistance is " + catchDistance);
             CatchPlayer();
        }
    }

    void CatchPlayer()
    {
        Debug.Log("Minotaur Caught Player!");

        // Training logic: check if caught object is our ML Agent
        if (target != null) 
        {
            MazeAgent mazeAgent = target.GetComponent<MazeAgent>();
            if (mazeAgent != null)
            {
                 // Punish hard
                 mazeAgent.AddReward(-1.0f);
                 mazeAgent.EndEpisode(); 
                 
                 // Reset minotaur logic partially if needed, or simply return
                 // Usually EndEpisode triggers agent reset which might reset scene or just agent
                 // To prevent spamming catch, we can warp minotaur or wait for scene reset
                 
                 // IMPORTANT: Do NOT call GameManager.GameOver()
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
}