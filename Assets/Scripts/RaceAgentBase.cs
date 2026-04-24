using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

public enum ColorOption { White, Red, Green, Blue }
public class RaceAgentBase : Agent
{
    // Ray Perception Sensor 3D
    protected RayPerceptionSensorComponent3D raySensor;

    protected GameObject[] lineRendererObjsSensor1;
    protected LineRenderer[] lineRenderersSensor1;
    protected int NumOfRayOutputsSensor;

    protected int currentStep = 0;
    
    [Header("Settings")]
    public string displayName;
    public ColorOption selectedColor;
    public float baseDroneSpeed;
    public Transform startingLoc;
    public bool debugMode = true;
    
    [Header("Drone Parts")]
    public MeshRenderer bodyBack;
    public MeshRenderer bodyFront;

    [Header("Front Materials")]
    public Material whiteFront;
    public Material redFront;
    public Material greenFront;
    public Material blueFront;

    [Header("Back Materials")]
    public Material whiteBack;
    public Material redBack;
    public Material greenBack;
    public Material blueBack;
    
    [Header("General Materials")]
    public Material redMaterial;
    public Material greenMaterial;
    
    protected RaceManager raceManager;
    protected Rigidbody rb;
    
    public bool stopDrone = false;
    public bool finishLine = false;
    public bool checkpoint = false;
    
    private void FixedUpdate()
    {
        // Build ray information for the Ray Perception Sensor 3D
        if (debugMode)
        {
            RayCastInfoVisualizer(raySensor);
        }
    }
    
    protected void OnValidate()
    {
        // We wrap this in a check to ensure we don't get errors 
        // if the MeshRenderers aren't assigned yet.
        if (bodyBack != null && bodyFront != null)
        {
            ApplyMaterials();
        }
    }
    
    protected void InitializeRaysRenderers()
    {
        // SENSOR 1
        lineRendererObjsSensor1 = new GameObject[NumOfRayOutputsSensor];
        lineRenderersSensor1 = new LineRenderer[NumOfRayOutputsSensor];

        for (int i = 0; i < lineRenderersSensor1.Length; i++)
        {
            lineRendererObjsSensor1[i] = new GameObject("Sensor1Ray" + i);
            lineRendererObjsSensor1[i].transform.parent = transform;
            lineRenderersSensor1[i] = lineRendererObjsSensor1[i].AddComponent<LineRenderer>();
            lineRenderersSensor1[i].positionCount = 2;

            // Set Line Renderer width
            lineRenderersSensor1[i].startWidth = 0.05f; // Adjust as needed
            lineRenderersSensor1[i].endWidth = 0.05f; // Adjust as needed
        }
    }

    public void ActivateRaysVisualization(bool activate)
    {
        // SENSOR 1
        if (lineRendererObjsSensor1 != null)
        {
            // The ray renderers are already created, therefore let's go to activate/deactivate them
            for (int i = 0; i < lineRendererObjsSensor1.Length; i++)
            {
                lineRendererObjsSensor1[i].SetActive(activate);
            }
        }
        else
        {
            if (activate)
            {
                // The renderers are not initialized but we want to activate them. We have to create them first.
                InitializeRaysRenderers();
                for (int i = 0; i < lineRendererObjsSensor1.Length; i++)
                {
                    lineRendererObjsSensor1[i].SetActive(true);
                }
            }
            // If the renderers are not there and we are requested to deactivate them (active = false), we don't have to do anything.
            // Probably we will never arrive here.
        }
    }

    protected List<object> RayCastInfoVisualizer(RayPerceptionSensorComponent3D rayComponent)
    {
        List<object> raysInfo = new();

        var rayOutputs = RayPerceptionSensor
            .Perceive(rayComponent.GetRayPerceptionInput(), false)
            .RayOutputs;

        if (rayOutputs != null)
        {
            var lengthOfRayOutputs = RayPerceptionSensor
                .Perceive(rayComponent.GetRayPerceptionInput(), false)
                .RayOutputs
                .Length;

            // We want the rays to go from left to right in the list and they are from right to left in rayOutputs. This is why we
            // go from the last to the first in the loop and we use a secondary index 'j' going up for the final list
            int j = 0;
            for (int i = lengthOfRayOutputs - 1; i >= 0; i--)
            {
                var rayDirection = rayOutputs[i].EndPositionWorld - rayOutputs[i].StartPositionWorld;
                var scaledRayLength = rayDirection.magnitude;
                float rayHitDistance = rayOutputs[i].HitFraction * scaledRayLength;
                Vector3 rayEndPosition;

                Material rayColor;

                List<object> rayInfo;

                GameObject goHit = rayOutputs[i].HitGameObject;
                if (goHit != null)
                {
                    rayEndPosition = rayOutputs[i].StartPositionWorld +
                                     (rayHitDistance / scaledRayLength) * rayDirection;
                    rayColor = redMaterial;
                    rayInfo = new List<object>
                        { j, 1, new { name = goHit.name, tag = goHit.tag, distance = rayHitDistance } };
                }
                else
                {
                    rayEndPosition = rayOutputs[i].EndPositionWorld;
                    rayColor = greenMaterial;
                    rayInfo = new List<object> { j, 0, null };
                }

                if (debugMode)
                {
                    // Set the positions of the line renderer
                    lineRenderersSensor1[i].SetPosition(0, rayOutputs[i].StartPositionWorld);
                    lineRenderersSensor1[i].SetPosition(1, rayEndPosition);

                    // Set the material based on whether the ray hit something
                    lineRenderersSensor1[i].material = rayColor;
                }

                // Format [RayIndex, Hit, ObjectHitInfo]
                raysInfo.Add(rayInfo);
                j++;
            }
        }

        return raysInfo;
    }

    protected void ApplyMaterials()
    {
        switch (selectedColor)
        {
            case ColorOption.White:
                bodyFront.sharedMaterial = whiteFront;
                bodyBack.sharedMaterial = whiteBack;
                break;
            
            case ColorOption.Red:
                bodyFront.sharedMaterial = redFront;
                bodyBack.sharedMaterial = redBack;
                break;

            case ColorOption.Green:
                bodyFront.sharedMaterial = greenFront;
                bodyBack.sharedMaterial = greenBack;
                break;

            case ColorOption.Blue:
                bodyFront.sharedMaterial = blueFront;
                bodyBack.sharedMaterial = blueBack;
                break;
        }
    }
}
