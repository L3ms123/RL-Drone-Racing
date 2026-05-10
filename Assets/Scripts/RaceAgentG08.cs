using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RaceAgentG08 : RaceAgentBase
{
    private Vector3 initialForward;

    void Start()
    {
        // *********** DO NOT TOUCH THIS *************************************
        ApplyMaterials();

        if (debugMode)
        {
            raySensor = GetComponent<RayPerceptionSensorComponent3D>();

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
        finishLine = false;
        checkpoint = false;

        // record initial heading for direction-of-travel check at finish line
        initialForward = transform.forward;

        // *********** DO NOT TOUCH THIS *************************************
        currentStep = 0;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = startingLoc.position;
        transform.rotation = startingLoc.rotation;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Behavior Parameters Space Size must be 3.
        sensor.AddObservation(transform.InverseTransformDirection(rb.linearVelocity));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("track"))
        {
            AddReward(-3.0f);
            EndEpisode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("finishLine"))
        {
            checkpoint = false;
            finishLine = true;

            // only count crossings made while still pointing in the original direction
            bool correctDirection = Vector3.Dot(transform.forward, initialForward) > 0f;
            AddReward(correctDirection ? 15.0f : -5.0f);

            EndEpisode();
            return;
        }

        if (other.gameObject.CompareTag("checkpoint"))
        {
            finishLine = false;
            checkpoint = true;
        }
    }

    private float WallDistance(Vector3 direction, float maxDistance)
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.25f;

        if (Physics.Raycast(origin, direction, out hit, maxDistance))
            return hit.distance;

        return maxDistance;
    }

    public void MoveAgent(ActionSegment<float> contAct, ActionSegment<int> disAct)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var speed = Mathf.Clamp01(contAct[0]);
        var actionTurn = disAct[0];

        switch (actionTurn)
        {
            case 1: rotateDir = transform.up * 1f; break;
            case 2: rotateDir = transform.up * -1f; break;
        }

        // turning costs forward speed: encourages slowing into corners
        if (actionTurn != 0)
            speed *= 0.6f;

        dirToGo = transform.forward;

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

        // time penalty: makes idling/circling costly
        AddReward(-0.002f);

        // forward-speed bonus: rewards moving forward, penalises moving backward
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        AddReward(0.002f * forwardSpeed);

        // wall-proximity penalty: gradient pressure to slow into approaching walls
        float frontDistance = WallDistance(transform.forward, 5f);
        if (frontDistance < 2f)
            AddReward(-0.01f * (2f - frontDistance));

        MoveAgent(actions.ContinuousActions, actions.DiscreteActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = Input.GetAxis("Vertical");

        if (Input.GetKey(KeyCode.D))
            discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.A))
            discreteActionsOut[0] = 2;
    }
}
