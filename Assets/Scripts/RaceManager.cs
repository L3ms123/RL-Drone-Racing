using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class RaceManager : MonoBehaviour
{
    public int totalLaps = 3;
    public float countdownDuration = 3f;
    public bool IsRaceActive { get; private set; } = false;

    private int currentPosition = 1;

    [Header("UI")]
    public TextMeshProUGUI countdownDisplay;
    public Transform uiContainer;
    public GameObject uiRowPrefab;

    private Dictionary<int, DroneEntry> participants = new Dictionary<int, DroneEntry>();
    private float raceStartTime;

    private class DroneEntry {
        public GameObject droneObj;
        public string displayName;
        public ColorOption color;
        public int laps;
        public bool finished;
        public float finishTime;
        public int finishPosition;
        public TextMeshProUGUI uiText;
    }

    private void Start()
    {
        // Safety check: Make sure we clear everything first
        ResetRace();

        // Use a slight delay or a "Frame Wait" to ensure all Drone Prefabs 
        // in the scene have finished their own Start() methods.
        StartCoroutine(InitializeRaceSequence());
    }
    
    private void ResetRace() 
    {
        IsRaceActive = false;
        currentPosition = 1;
        participants.Clear();
        foreach (Transform child in uiContainer) 
        {
            Destroy(child.gameObject);
        }
    }
    
    IEnumerator InitializeRaceSequence()
    {
        // Wait for the very end of the first frame
        yield return new WaitForEndOfFrame();

        // 1. Find all drones currently in the scene
        PreRegisterDrones();

        // 2. Begin the 3-2-1-GO countdown
        yield return StartCoroutine(CountdownSequence());
    }

    private void PreRegisterDrones() {
        // Find every object in the scene with the "Player" tag
        GameObject[] drones = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject drone in drones) {
            int id = drone.GetInstanceID();
        
            if (!participants.ContainsKey(id))
            {
                RaceAgentBase data = drone.GetComponent<RaceAgentBase>();
                
                GameObject newRow = Instantiate(uiRowPrefab, uiContainer);
            
                participants.Add(id, new DroneEntry {
                    droneObj = drone,
                    displayName = data.displayName,
                    color = data.selectedColor,
                    uiText = newRow.GetComponentInChildren<TextMeshProUGUI>(),
                    laps = 0
                });
            
                // Set initial UI text
                Color uiColor = GetUnityColor(data.selectedColor);
                participants[id].uiText.color = uiColor;
                participants[id].uiText.text = $"{data.displayName} | Waiting...";
            }
        }
    }
    
    IEnumerator CountdownSequence() {
        IsRaceActive = false;
        float t = countdownDuration;
        while (t > 0) {
            countdownDisplay.text = Mathf.Ceil(t).ToString();
            yield return new WaitForSeconds(1f);
            t--;
        }
        countdownDisplay.text = "GO!";
        raceStartTime = Time.time;
        IsRaceActive = true;
        yield return new WaitForSeconds(1f);
        countdownDisplay.text = "";
    }

    public void OnGateCrossed(Collider other)
    {
        int id = other.gameObject.GetInstanceID();
        
        DroneEntry entry = participants[id];

        if (!entry.finished)
        {
            RaceAgentBase data = other.GetComponent<RaceAgentBase>();
            if (data.checkpoint || entry.laps == 0)
            {
                entry.laps++;
                if (entry.laps >= totalLaps)
                {
                    data.stopDrone = true;
                    
                    entry.finished = true;
                    entry.finishTime = Time.time - raceStartTime;
                    entry.finishPosition = currentPosition;
                    currentPosition++;
                }
            }
        }
    }

    private Color GetUnityColor(ColorOption option) {
        switch (option) {
            case ColorOption.Red:   return Color.red;
            case ColorOption.Green: return Color.green;
            case ColorOption.Blue:  return Color.blue;
            case ColorOption.White: return Color.white;
            default:                return Color.white;
        }
    }
    
    void Update() {
        if (!IsRaceActive) return;
        foreach (var entry in participants.Values) {
            Color uiColor = GetUnityColor(entry.color);
            entry.uiText.color = uiColor;
            
            if (entry.finished)
            {
                entry.uiText.text = $"{entry.displayName} | L: {entry.laps}/{totalLaps} | {entry.finishTime:F2}s | FINAL: {entry.finishPosition}";
                continue;
            }
            float time = Time.time - raceStartTime;
            entry.uiText.text = $"{entry.displayName} | L: {entry.laps}/{totalLaps} | {time:F2}s";
        }
    }
}