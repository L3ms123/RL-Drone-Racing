using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RaceAgentG08 : RaceAgentBase
{
    void Start()
    {
        // ADD YOUR CODE HERE
        // ...
        
        // *********** DO NOT TOUCH THIS *************************************
        ApplyMaterials();
        
        if (debugMode)
        {
            raySensor = GetComponent<RayPerceptionSensorComponent3D>();

            // How many rays we have in the Ray Perception Sensor 3D
            NumOfRayOutputsSensor = RayPerceptionSensor
                .Perceive(raySensor.GetRayPerceptionInput(), false)
                .RayOutputs
                .Length;

            InitializeRaysRenderers();
            ActivateRaysVisualization(true);
        }

        rb = GetComponent<Rigidbody>();
        raceManager = FindFirstObjectByType<RaceManager>();

        stopDrone = false;
        checkpoint = false;
    }

    public override void OnEpisodeBegin()
    {
        // ADD YOUR CODE HERE
        // ...
        
        // *********** DO NOT TOUCH THIS *************************************
        currentStep = 0;
        
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = startingLoc.position;
        transform.rotation = startingLoc.rotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("track"))
        {
            Debug.Log("Hit!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("finishLine"))
        {
            checkpoint = false;
            finishLine = true;
            return;
        }
        
        if (other.gameObject.CompareTag("checkpoint"))
        {
            finishLine = false;
            checkpoint = true;
        }
    }
    
    public void MoveAgent(ActionSegment<float> contAct, ActionSegment<int> disAct)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var speed = contAct[0];
        var actionTurn = disAct[0];

        // Looking if the agent has to turn left or right
        switch (actionTurn)
        {
            case 1: // Turn right
                rotateDir = transform.up * 1f;
                break;
            case 2: // Turn left
                rotateDir = transform.up * -1f;
                break;
        }

        dirToGo = transform.forward;

        // Apply the actions to the agent
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 150f);
        rb.AddForce(dirToGo * baseDroneSpeed * speed, ForceMode.VelocityChange);
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (raceManager != null && (!raceManager.IsRaceActive || stopDrone))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        currentStep++;
        if (MaxStep > 0 && currentStep >= MaxStep -1)
        {
            // MaxStep is defined in the editor in the main agent script component.
            // When MaxStep is 0, it means the episode never ends.
            // We use the currentStep internal cariable to perform actions just before the episode ends.
        }
        
        // Move the agent using the actions predicted by the policy
        MoveAgent(actions.ContinuousActions, actions.DiscreteActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        var continuousActionsOut = actionsOut.ContinuousActions;

        // Speed
        continuousActionsOut[0] = Input.GetAxis("Vertical");

        // Left / Right actions
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 2;
        }
    }
}