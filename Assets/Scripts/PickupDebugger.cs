using UnityEngine;

public class PickupDebugger : MonoBehaviour
{
    private PickupManager pickupManager;
    private int lastIndex = -1;
    private float firstG2Height = -1f;  // To track first 98Hz height
    private bool firstG2Found = false;

    void Start()
    {
        pickupManager = GetComponent<PickupManager>();
    }

    void Update()
    {
        if (pickupManager == null || pickupManager.sequence == null) return;

        // Track pickup spawns
        if (pickupManager.currentPickupIndex != lastIndex)
        {
            var sequence = pickupManager.sequence;
            int index = pickupManager.currentPickupIndex;
            
            if (index < sequence.Length)
            {
                float freq = sequence[index].frequency;
                float height = PitchHeightCalculator.GetHeightForFrequency(freq);
                
                // Track G2 (98Hz) specifically
                if (Mathf.Approximately(freq, 98.0f))
                {
                    if (!firstG2Found)
                    {
                        firstG2Height = height;
                        firstG2Found = true;
                        Debug.Log($"First G2 (98Hz) found at height: {height:F2}m");
                    }
                    else
                    {
                        Debug.Log($"Subsequent G2 (98Hz) at height: {height:F2}m (First was at: {firstG2Height:F2}m)");
                    }
                }

                Debug.Log($"Pickup #{index} spawning:\n" +
                         $"Frequency: {freq}Hz\n" +
                         $"Height: {height:F2}m\n" +
                         $"Active Pickups: {pickupManager.activePickups.Count}\n" +
                         $"Sequence Position: {index + 1}/{sequence.Length}");
            }

            lastIndex = index;
        }

        // Log if multiple pickups exist
        if (pickupManager.activePickups.Count > 1)
        {
            Debug.LogWarning($"Multiple pickups detected! Count: {pickupManager.activePickups.Count}");
            foreach (var pickup in pickupManager.activePickups)
            {
                Debug.LogWarning($"Pickup position: {pickup.transform.position}");
            }
        }
    }
}