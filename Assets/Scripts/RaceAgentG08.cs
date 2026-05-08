using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RaceAgentG08_Rally : RaceAgentBase
{
    private Transform[] checkpoints;
    private int nextCheckpointIndex = 0;
    private float previousDistanceToCheckpoint = 0f;

    private int noProgressSteps = 0;

    void Start(){
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
        FindCheckpoints();
    }

    private void FindCheckpoints()
    {
        GameObject[] checkpointObjects = GameObject.FindGameObjectsWithTag("checkpoint");

        checkpoints = new Transform[checkpointObjects.Length];

        for (int i = 0; i < checkpointObjects.Length; i++)
            checkpoints[i] = checkpointObjects[i].transform;

        System.Array.Sort(checkpoints, (a, b) => a.name.CompareTo(b.name));
    }

    public override void OnEpisodeBegin()
    {
        finishLine = false;
        checkpoint = false;

        nextCheckpointIndex = 0;
        noProgressSteps = 0;
        currentStep = 0;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = startingLoc.position;
        transform.rotation = startingLoc.rotation;

        if (checkpoints != null && checkpoints.Length > 0)
        {
            previousDistanceToCheckpoint = Vector3.Distance(
                transform.position,
                checkpoints[nextCheckpointIndex].position
            );
        }
    }
    
    

    public override void CollectObservations(VectorSensor sensor)
    {
        // change behavior parameters space size to 9
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(localVelocity);

        float frontWall = WallDistance(transform.forward, 6f);
        float leftWall = WallDistance(Quaternion.Euler(0, -35, 0) * transform.forward, 6f);
        float rightWall = WallDistance(Quaternion.Euler(0, 35, 0) * transform.forward, 6f);

        sensor.AddObservation(1f - (frontWall / 6f));
        sensor.AddObservation(1f - (leftWall / 6f));
        sensor.AddObservation(1f - (rightWall / 6f));

        Vector3 dirToCheckpoint =
            (checkpoints[nextCheckpointIndex].position - transform.position).normalized;

        sensor.AddObservation(transform.InverseTransformDirection(dirToCheckpoint));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("track"))
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("finishLine"))
        {
            AddReward(5.0f);
            EndEpisode();
            return;
        }

        if (other.gameObject.CompareTag("checkpoint"))
        {
            if (other.transform == checkpoints[nextCheckpointIndex])
            {
                AddReward(1.5f);

                nextCheckpointIndex++;

                if (nextCheckpointIndex >= checkpoints.Length)
                    nextCheckpointIndex = 0;

                previousDistanceToCheckpoint = Vector3.Distance(
                    transform.position,
                    checkpoints[nextCheckpointIndex].position
                );

                noProgressSteps = 0;
            }
            else
            {
                AddReward(-0.5f);
            }
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
        float speedInput = Mathf.Clamp01(contAct[0]);

        Vector3 rotateDir = Vector3.zero;

        switch (disAct[0])
        {
            case 1: rotateDir = transform.up; break;
            case 2: rotateDir = -transform.up; break;
        }

        transform.Rotate(rotateDir, 180f * Time.fixedDeltaTime);

        rb.AddForce(
            transform.forward * baseDroneSpeed * speedInput,
            ForceMode.VelocityChange
        );
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

        // pequeña penalización por tiempo
        AddReward(-0.001f);

        // PROGRESO HACIA CHECKPOINT
        Vector3 target = checkpoints[nextCheckpointIndex].position;
        float currentDistance = Vector3.Distance(transform.position, target);

        float progress = previousDistanceToCheckpoint - currentDistance;

        AddReward(progress * 0.02f);

        previousDistanceToCheckpoint = currentDistance;

        // DETECCIÓN DE ESTANCAMIENTO
        if (progress < 0.0001f)
            noProgressSteps++;
        else
            noProgressSteps = 0;

        if (noProgressSteps > 200)
        {
            AddReward(-1.0f);
            EndEpisode();
            return;
        }

        // LIMITE DE EPISODIO
        if (MaxStep > 0 && currentStep >= MaxStep)
        {
            EndEpisode();
            return;
        }

        MoveAgent(actions.ContinuousActions, actions.DiscreteActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuous = actionsOut.ContinuousActions;
        var discrete = actionsOut.DiscreteActions;

        continuous[0] = Mathf.Clamp01(Input.GetAxis("Vertical"));

        discrete[0] = 0;

        if (Input.GetKey(KeyCode.D))
            discrete[0] = 1;
        else if (Input.GetKey(KeyCode.A))
            discrete[0] = 2;
    }
}