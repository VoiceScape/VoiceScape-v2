using UnityEngine;

public class ProximityDebugger : MonoBehaviour
{
    [SerializeField] private MPMAudioAnalyzer audioAnalyzer;
    [SerializeField] private PickupManager pickupManager;
    [SerializeField] private Transform playerRig;
    [SerializeField] private float proximityThreshold = 2f; // Distance to trigger logging
    [SerializeField] private float logCooldown = 0.5f; // Prevent spam logging
    
    private float nextLogTime;
    private int lastLoggedPickupIndex = -1;

    private void Start()
    {
        if (playerRig == null)
        {
            playerRig = GameObject.Find("[BuildingBlock] Camera Rig")?.transform;
            if (playerRig == null)
                Debug.LogError("Could not find player rig!");
        }
    }

    private void Update()
    {
        if (Time.time < nextLogTime) return;
        if (pickupManager == null || pickupManager.activePickups.Count == 0) return;

        // Check distance to active pickup
        GameObject pickup = pickupManager.activePickups[0];
        if (pickup == null) return;

        float distance = Vector3.Distance(playerRig.position, pickup.transform.position);
        
        // Log when close to pickup and haven't logged this one yet
        if (distance < proximityThreshold && lastLoggedPickupIndex != pickupManager.currentPickupIndex)
        {
            float playerHeight = playerRig.position.y;
            float pickupHeight = pickup.transform.position.y;
            float targetFreq = pickupManager.sequence[pickupManager.currentPickupIndex].frequency;
            float currentFreq = audioAnalyzer.IsVoiceDetected ? audioAnalyzer.Frequency : 0f;

            Debug.Log($"\nNear Pickup #{pickupManager.currentPickupIndex}:" +
                     $"\nPlayer Height: {playerHeight:F2}m" +
                     $"\nPickup Height: {pickupHeight:F2}m" +
                     $"\nHeight Diff: {Mathf.Abs(playerHeight - pickupHeight):F2}m" +
                     $"\nTarget Freq: {targetFreq:F1}Hz" +
                     $"\nCurrent Freq: {currentFreq:F1}Hz" +
                     $"\nFreq Diff: {Mathf.Abs(targetFreq - currentFreq):F1}Hz" +
                     $"\nDistance: {distance:F2}m");

            lastLoggedPickupIndex = pickupManager.currentPickupIndex;
            nextLogTime = Time.time + logCooldown;
        }
        // Reset logged index when far from pickup to allow logging again on next approach
        else if (distance > proximityThreshold * 1.5f)
        {
            lastLoggedPickupIndex = -1;
        }
    }
}