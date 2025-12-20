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
            Vector3 toTarget = target.position - transform.position;
            Vector3 localToTarget = transform.InverseTransformDirection(toTarget);
            
            float normFactor = Mathf.Max(mazeGenerator.width, mazeGenerator.height) * 4.0f;
            sensor.AddObservation(localToTarget.x / normFactor); 
            sensor.AddObservation(localToTarget.z / normFactor);

            if (NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, navPath))
            {
                if (navPath.corners.Length > 1)
                {
                    Vector3 dirToNextCorner = (navPath.corners[1] - transform.position).normalized;
                    Vector3 localDir = transform.InverseTransformDirection(dirToNextCorner);
                    
                    sensor.AddObservation(localDir.x);
                    sensor.AddObservation(localDir.z);
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
        }
        else
        {
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(Vector2.zero); 
        }

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
                    suggestedWorldDir = -transform.right;
                }
                else
                {
                    suggestedWorldDir = Vector3.Cross(hitRight.normal, Vector3.up).normalized;
                    if (Vector3.Dot(transform.forward, suggestedWorldDir) < 0) suggestedWorldDir = -suggestedWorldDir;
                }
                
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

        Vector3 localVelocity = transform.InverseTransformDirection(characterController.velocity);
        sensor.AddObservation(localVelocity.x / moveSpeed);
        sensor.AddObservation(localVelocity.z / moveSpeed);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float rotateAction = actionBuffers.ContinuousActions[0];
        float forwardAction = actionBuffers.ContinuousActions[1]; 

        transform.Rotate(0, rotateAction * turnSpeed * Time.deltaTime, 0);

        float moveForward = Mathf.Clamp(forwardAction, 0.5f, 1.0f); 

        Vector3 move = transform.forward * moveForward;
        characterController.Move(move * moveSpeed * Time.deltaTime);

        AddReward(-0.0005f); 

        if (target && NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, navPath) && navPath.status == NavMeshPathStatus.PathComplete)
        {
            float currentPathDist = GetPathLength(navPath);
            float diff = lastDistanceToTarget - currentPathDist;
            
            if (Mathf.Abs(diff) < 5.0f) 
            {
               AddReward(diff * 5.0f); 
            }
            
            lastDistanceToTarget = currentPathDist;
        }
        else
        {
           float currentDist = Vector3.Distance(transform.position, target.position);
           lastDistanceToTarget = currentDist;
        }
    }
    
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
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
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

        if (Vector3.Distance(transform.position, lastPosCheck) < 0.1f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 3.0f)
            {
                AddReward(-0.5f);
                EndEpisode();
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosCheck = transform.position;
        }
    }
}
