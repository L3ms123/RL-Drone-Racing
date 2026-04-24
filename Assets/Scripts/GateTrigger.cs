using UnityEngine;

public class GateTrigger : MonoBehaviour {
    public RaceManager manager; // Drag the Scoreboard object here

    void OnTriggerEnter(Collider other) {
        manager.OnGateCrossed(other); // Pass the collision data to the manager
    }
}