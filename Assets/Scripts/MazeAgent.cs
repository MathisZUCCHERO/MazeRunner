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

        var playerControllerScript = GetComponent<PlayerController>();
        if (playerControllerScript != null)
        {
            playerControllerScript.enabled = false;
        }
    }

    public override void OnEpisodeBegin()
    {
        if (transform.position.y < -5)
        {
            characterController.enabled = false;
            transform.position = startPosition;
            transform.rotation = startRotation;
            characterController.enabled = true;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (target != null)
        {
            Vector3 relativePosition = target.position - transform.position;
            sensor.AddObservation(relativePosition.x / 40f); 
            sensor.AddObservation(relativePosition.z / 40f);
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
        float moveX = actionBuffers.ContinuousActions[0];
        float moveZ = actionBuffers.ContinuousActions[1];

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        characterController.Move(move * moveSpeed * Time.deltaTime);

        AddReward(-0.001f);
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
    }
}
