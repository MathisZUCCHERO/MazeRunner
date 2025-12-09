using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class MinotaurAI : MonoBehaviour
{
    [Header("Core")]
    public Transform target;
    public float catchDistance = 2.0f;
    
    // Internal
    private NavMeshAgent agent;
    private bool isReady = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Start Initialization Routine
        StartCoroutine(InitializeRoutine());
    }
    
    IEnumerator InitializeRoutine()
    {
        // wait for end of frame to ensure all Start() methods ran
        yield return new WaitForEndOfFrame();
        
        // Disable Animator Root Motion if present
        Animator anim = GetComponentInChildren<Animator>();
        if (anim) anim.applyRootMotion = false;
        
        // Find Target if missing
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
            if (target == null && Camera.main) target = Camera.main.transform;
        }
        
        // Ensure Agent is on NavMesh
        if (agent != null && !agent.isOnNavMesh)
        {
            Debug.LogWarning("[MinotaurAI] Agent not on NavMesh, attempting Warp...");
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogError("[MinotaurAI] FAILED to place agent on NavMesh.");
            }
        }
        
        isReady = true;
        Debug.Log("[MinotaurAI] Ready!");
        
        // Repath loop
        while (enabled)
        {
            if (isReady && target != null && agent.isOnNavMesh)
            {
                agent.SetDestination(target.position);
            }
            yield return new WaitForSeconds(0.2f); // Repath 5 times a second
        }
    }

    void Update()
    {
        if (!isReady || target == null) return;
        
        // Kill Check
        float d = Vector3.Distance(transform.position, target.position);
        if (d < catchDistance)
        {
            Debug.Log("Minotaur Caught Player!");
            if (GameManager.Instance) GameManager.Instance.GameOver();
            enabled = false;
        }
    }
}
