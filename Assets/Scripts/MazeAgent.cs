using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(DecisionRequester))]
public class MazeAgent : Agent
{
    [Header("Agent Settings")]
    public float moveSpeed = 8f;
    public float turnSpeed = 150f; // Reduced for better control (was 300)
    public Transform target;
    
    private CharacterController characterController;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private MazeGenerator mazeGenerator;
    private MinotaurAI minotaur;
    
    // Anti-Stuck & GPS vars
    private Vector3 lastPosCheck;
    private float stuckTimer;
    private float lastDistanceToTarget;
    private NavMeshPath navPath;

    public override void Initialize()
    {
        characterController = GetComponent<CharacterController>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        mazeGenerator = FindObjectOfType<MazeGenerator>();

        if (target == null)
        {
            GameObject finishObj = GameObject.FindGameObjectWithTag("Finish");
            if (finishObj) target = finishObj.transform;
        }

        var playerControllerScript = GetComponent<PlayerController>();
        if (playerControllerScript != null)
        {
            playerControllerScript.enabled = false;
        }
    }

    public override void OnEpisodeBegin()
    {
        characterController.enabled = false;
        transform.position = startPosition;
        transform.rotation = startRotation;
        characterController.enabled = true;

        // RETRY FINDING TARGET (Crucial because Agent spawns before EndTrigger)
        if (target == null)
        {
            GameObject finishObj = GameObject.FindGameObjectWithTag("Finish");
            if (finishObj) target = finishObj.transform;
            else Debug.LogWarning("MazeAgent: Target 'Finish' not found yet. Will retry next frame.");
        }

        if (minotaur == null) minotaur = FindObjectOfType<MinotaurAI>();
        if (minotaur != null) minotaur.ResetPosition();

        navPath = new NavMeshPath();
        stuckTimer = 0f;
        lastPosCheck = transform.position;
        if (target) lastDistanceToTarget = GetPathLengthToTarget(); // Use new helper
    }
    
    // Helper wrapper for initial distance
    float GetPathLengthToTarget()
    {
         if (target && NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, navPath))
         {
             return GetPathLength(navPath);
         }
         return Vector3.Distance(transform.position, target.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (target != null)
        {
            // 1. Target Position (LOCAL SPACE)
            // Agent needs to know where target is relative to its current facing direction
            Vector3 toTarget = target.position - transform.position;
            Vector3 localToTarget = transform.InverseTransformDirection(toTarget);
            
            // Dynamic Normalization: Use actual maze size + margin buffer
            // We use the larger dimension to keep aspect ratio consistent if maze is non-square
            float normFactor = Mathf.Max(mazeGenerator.width, mazeGenerator.height) * 4.0f; // cellSize(4) * size
            
            sensor.AddObservation(localToTarget.x / normFactor); 
            sensor.AddObservation(localToTarget.z / normFactor);

            // 2. GPS (LOCAL SPACE Direction)
            if (NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, navPath))
            {
                if (navPath.corners.Length > 1)
                {
                    Vector3 dirToNextCorner = (navPath.corners[1] - transform.position).normalized;
                    Vector3 localDir = transform.InverseTransformDirection(dirToNextCorner);
                    
                    sensor.AddObservation(localDir.x);
                    sensor.AddObservation(localDir.z);
                    
                    // Debug Draw
                    for (int i = 0; i < navPath.corners.Length - 1; i++)
                        Debug.DrawLine(navPath.corners[i], navPath.corners[i + 1], Color.green);
                }
                else
                {
                    sensor.AddObservation(Vector2.zero); // On top of target?
                }
            }
            else
            {
                sensor.AddObservation(Vector2.zero); // No path found
            }
        }
        else
        {
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(Vector2.zero); 
        }

        // 3. Wall Follow (Right Hand Rule) with Corner Handling (LOCAL SPACE)
        RaycastHit hitRight;
        RaycastHit hitFront;
        Vector3 rightDir = transform.right;
        Vector3 forwardDir = transform.forward;
        
        bool frontBlocked = Physics.Raycast(transform.position, forwardDir, out hitFront, 2.0f) && hitFront.collider.CompareTag("Wall");

        if (Physics.Raycast(transform.position, rightDir, out hitRight, 4f)) 
        {
            if (hitRight.collider.CompareTag("Wall"))
            {
                Vector3 suggestedWorldDir;

                if (frontBlocked)
                {
                    // CORNER: Turn Left
                    suggestedWorldDir = -transform.right;
                    Debug.DrawRay(transform.position, suggestedWorldDir * 2f, Color.magenta); 
                }
                else
                {
                    // FOLLOW: Tangent
                    suggestedWorldDir = Vector3.Cross(hitRight.normal, Vector3.up).normalized;
                    if (Vector3.Dot(transform.forward, suggestedWorldDir) < 0) suggestedWorldDir = -suggestedWorldDir;
                    Debug.DrawRay(transform.position, suggestedWorldDir * 2f, Color.cyan);
                }
                
                // Convert to Local for the brain
                Vector3 localSuggested = transform.InverseTransformDirection(suggestedWorldDir);
                sensor.AddObservation(localSuggested.x);
                sensor.AddObservation(localSuggested.z);
            }
            else
            {
                sensor.AddObservation(Vector2.zero); 
            }
        }
        else
        {
             sensor.AddObservation(Vector2.zero);
        }

        // Velocity (Already Local)
        Vector3 localVelocity = transform.InverseTransformDirection(characterController.velocity);
        sensor.AddObservation(localVelocity.x / moveSpeed);
        sensor.AddObservation(localVelocity.z / moveSpeed);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // TANK CONTROLS
        // Action 0: Rotate (-1 to 1)
        // Action 1: Forward (0 to 1) - We force positive
        
        float rotateAction = actionBuffers.ContinuousActions[0];
        float forwardAction = actionBuffers.ContinuousActions[1]; 

        // 1. Rotation
        transform.Rotate(0, rotateAction * turnSpeed * Time.deltaTime, 0);

        // 2. Drive Forward (Always moving)
        // Clamp to ensure minimum speed (e.g. 50% max speed minimum)
        float moveForward = Mathf.Clamp(forwardAction, 0.5f, 1.0f); 

        Vector3 move = transform.forward * moveForward;
        characterController.Move(move * moveSpeed * Time.deltaTime);

        // REWARDS
        // 1. Time Penalty (Small)
        AddReward(-0.0005f); 

        // 2. True Path Distance Reward (The "Breadcrumb" Strategy)
        if (target && NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, navPath) && navPath.status == NavMeshPathStatus.PathComplete)
        {
            float currentPathDist = GetPathLength(navPath);
            float diff = lastDistanceToTarget - currentPathDist;
            
            // Huge reward for solving the path, huge penalty for backtracking
            // We clamp it to avoid crazy spikes if path recalculation jumps, but generally we want strict guidance.
            if (Mathf.Abs(diff) < 5.0f) // Sanity check to ignore teleporting/reset spikes
            {
               AddReward(diff * 5.0f); 
            }
            
            lastDistanceToTarget = currentPathDist;
        }
        else
        {
           // Fallback to Euclidean if path fails (rare in valid maze)
           float currentDist = Vector3.Distance(transform.position, target.position);
           lastDistanceToTarget = currentDist;
        }
    }
    
    // Helper to calculate actual length of the NavMesh path
    float GetPathLength(NavMeshPath path)
    {
        float length = 0f;
        if (path.corners.Length < 2) return length;
        
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        return length;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal"); // Turn
        continuousActionsOut[1] = Input.GetAxis("Vertical");   // Forward
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Finish")) 
        {
            AddReward(1.0f);
            EndEpisode();
        }
    }
    
    void Update()
    {
        if (transform.position.y < -5f)
        {
            AddReward(-1.0f);
            EndEpisode();
        }

        // Anti-Stuck Logic
        if (Vector3.Distance(transform.position, lastPosCheck) < 0.1f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 3.0f) // Stuck for 3 seconds
            {
                AddReward(-0.5f); // Penalty for being lazy/stuck
                EndEpisode(); // Force reset
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosCheck = transform.position;
        }
    }
}
