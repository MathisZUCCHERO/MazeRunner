using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(DecisionRequester))]
public class MazeAgent : Agent
{
    [Header("Agent Settings")]
    public float moveSpeed = 5f;
    public Transform target;
    
    private CharacterController characterController;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private MazeGenerator mazeGenerator;

    public override void Initialize()
    {
        characterController = GetComponent<CharacterController>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        mazeGenerator = FindObjectOfType<MazeGenerator>();

        // Disable original PlayerController to avoid conflict
        var playerControllerScript = GetComponent<PlayerController>();
        if (playerControllerScript != null)
        {
            playerControllerScript.enabled = false;
        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent position if fell or timed out
        if (transform.position.y < -5)
        {
            characterController.enabled = false;
            transform.position = startPosition;
            transform.rotation = startRotation;
            characterController.enabled = true;
        }

        // Ideally ask MazeGenerator to reset/respawn, but for now just reset position
        // If the maze is dynamic per episode, we would call mazeGenerator.GenerateAndBuild() here.
        // For static training in same maze, just reset position.
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target relative position (3 floats)
        if (target != null)
        {
            Vector3 relativePosition = target.position - transform.position;
            sensor.AddObservation(relativePosition.x / 40f); // Normalization approx
            sensor.AddObservation(relativePosition.z / 40f);
        }
        else
        {
            sensor.AddObservation(Vector2.zero);
        }

        // Agent local velocity (2 floats - x, z) normalized
        Vector3 localVelocity = transform.InverseTransformDirection(characterController.velocity);
        sensor.AddObservation(localVelocity.x / moveSpeed);
        sensor.AddObservation(localVelocity.z / moveSpeed);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2 continuous
        float moveX = actionBuffers.ContinuousActions[0];
        float moveZ = actionBuffers.ContinuousActions[1];

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        characterController.Move(move * moveSpeed * Time.deltaTime);

        // Small reward for existing to encourage finishing fast (implemented as penalty per step usually, or small positive for progress)
        // Here: Penalty for time step to encourage shortest path
        AddReward(-0.001f);

        // Distance reward (optional, can help shaping)
        // float distanceToTarget = Vector3.Distance(transform.position, target.position);
        // AddReward((100f - distanceToTarget) * 0.0001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Finish")) // Assuming EndTrigger has this tag or similar
        {
            AddReward(1.0f);
            EndEpisode();
        }
    }
    
    // Safety check for falling off
    void Update()
    {
        if (transform.position.y < -5f)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }
}
