using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MusicalPickup
{
    public float frequency;      // Target frequency in Hz
    public Color color;         // Visual feedback color
    public float tolerance = 5f; // Frequency tolerance in Hz
    public bool isCollected;
}

public class PickupManager : MonoBehaviour 
{
    [Header("References")]
    [SerializeField] private Transform cameraRig;
    [SerializeField] private PitchVisualizer pitchVisualizer;

    [Header("Layout")]
    [SerializeField] private float visualizerDistance = 40f;
    [SerializeField] private float depthSpacing = 5f;

    [Header("Pickup Configuration")]
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private Vector3 pickupScale = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("Audio Settings")]
    [SerializeField] private AudioClip baseNote;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private float droneVolume = 0.7f;
    [SerializeField] private float successVolume = 0.5f;
    [SerializeField] private float baseFrequency = 130.81f;  // C3

    private AudioSource droneAudioSource;
    private AudioSource successAudioSource;
    public List<GameObject> activePickups = new List<GameObject>();
    public int currentPickupIndex = 0;
    private Transform centerEyeAnchor;

    // Musical sequence: G2 → Bb2 → C3 → Eb3 → C3 → Bb2 → G2
    public MusicalPickup[] sequence = new MusicalPickup[]
    {
        new MusicalPickup { frequency = 98.00f,  color = new Color(0f, 0f, 1f, 0.8f) },    // G2
        new MusicalPickup { frequency = 116.54f, color = new Color(0f, 1f, 0f, 0.8f) },    // Bb2
        new MusicalPickup { frequency = 130.81f, color = new Color(1f, 1f, 0f, 0.8f) },    // C3
        new MusicalPickup { frequency = 155.56f, color = new Color(1f, 0f, 0f, 0.8f) },    // Eb3
        new MusicalPickup { frequency = 130.81f, color = new Color(1f, 1f, 0f, 0.8f) },    // C3
        new MusicalPickup { frequency = 116.54f, color = new Color(0f, 1f, 0f, 0.8f) },    // Bb2
        new MusicalPickup { frequency = 98.00f,  color = new Color(0f, 0f, 1f, 0.8f) }     // G2
    };

    private void Start()
    {
        Debug.Log("PickupManager Start called");
        
        if (cameraRig == null)
        {
            cameraRig = GameObject.Find("[BuildingBlock] Camera Rig")?.transform;
            Debug.Log($"Found camera rig: {(cameraRig != null ? cameraRig.name : "null")}");
        }

        // Find CenterEyeAnchor
        centerEyeAnchor = cameraRig.Find("TrackingSpace/CenterEyeAnchor");
        if (centerEyeAnchor == null)
        {
            Debug.LogError("Could not find CenterEyeAnchor!");
            return;
        }

        InitializeAudioSources();
        
        // Delay the start of the pickup sequence
        StartCoroutine(DelayedStart());
    }

    private System.Collections.IEnumerator DelayedStart()
    {
        Debug.Log("Waiting 10 seconds before starting pickup sequence...");
        yield return new WaitForSeconds(10.0f);
        Debug.Log("Starting pickup sequence");
        SpawnCurrentPickup();  // Spawn first pickup
    }

    private void InitializeAudioSources()
    {
        // Setup drone audio source
        droneAudioSource = gameObject.AddComponent<AudioSource>();
        droneAudioSource.playOnAwake = false;
        droneAudioSource.spatialBlend = 0f;  // Non-spatial
        droneAudioSource.volume = droneVolume;
        droneAudioSource.loop = true;
        
        // Setup success audio source
        successAudioSource = gameObject.AddComponent<AudioSource>();
        successAudioSource.playOnAwake = false;
        successAudioSource.spatialBlend = 0f;
        successAudioSource.volume = successVolume;
        
        Debug.Log("Audio sources initialized for drone and success sounds");
    }

    private void Update()
    {
        if (centerEyeAnchor == null) return;

        // Only manage one pickup at a time
        if (activePickups.Count == 0 || activePickups[0] == null)
        {
            // Spawn new pickup in front of player
            SpawnCurrentPickup();
            return;
        }

        UpdateActivePickup();
    }

    private void UpdateActivePickup()
    {
        // Get the active pickup
        GameObject pickup = activePickups[0];
        
        // Check if pickup is behind player or too far away
        Vector3 toPickup = pickup.transform.position - centerEyeAnchor.position;
        float angle = Vector3.Angle(centerEyeAnchor.forward, toPickup);
        float distance = toPickup.magnitude;

        if (angle > 90f || distance > visualizerDistance * 2f)  // Behind player or too far
        {
            // Remove old pickup
            Destroy(pickup);
            activePickups.Clear();
            
            // Will spawn new one next frame
            Debug.Log("Pickup missed - respawning");
        }
        else
        {
            // Make pickup face the player
            pickup.transform.LookAt(new Vector3(centerEyeAnchor.position.x, 
                                              pickup.transform.position.y, 
                                              centerEyeAnchor.position.z));
            pickup.transform.Rotate(0, 180, 0);
        }
    }

    private float CalculatePickupHeight(float frequency)
    {
        if (pitchVisualizer == null)
        {
            Debug.LogError("Missing PitchVisualizer!");
            return SceneManager.Instance.baseHeight;  // Use SceneManager's baseHeight instead
        }

        Vector3 targetPos = pitchVisualizer.GetPositionForFrequency(frequency);
        Debug.LogError($"[PICKUP_HEIGHT] For {frequency}Hz:" +
                      $"\nCalculated height: {targetPos.y}");
        return targetPos.y;
    }


    private void SpawnCurrentPickup()
    {   
        if (currentPickupIndex >= sequence.Length)
        {
            Debug.Log("Sequence complete!");
            return;
        }

        // Update pitch visualizer target
        if (pitchVisualizer != null)
        {
            float freq = sequence[currentPickupIndex].frequency;
            pitchVisualizer.targetFrequency = freq;
        }

        // Calculate spawn position in front of player
        Vector3 forward = Vector3.ProjectOnPlane(centerEyeAnchor.forward, Vector3.up).normalized;
        Vector3 spawnPos = centerEyeAnchor.position + (forward * visualizerDistance);

        // Set height directly from frequency
        float currentFrequency = sequence[currentPickupIndex].frequency;
        spawnPos.y = PitchHeightCalculator.GetHeightForFrequency(currentFrequency);

        Debug.Log($"Spawning pickup - Freq: {currentFrequency}Hz, Height: {spawnPos.y:F2}m");

        GameObject pickupObj = Instantiate(pickupPrefab);
        pickupObj.transform.position = spawnPos;
        pickupObj.transform.rotation = Quaternion.LookRotation(-forward);
        pickupObj.transform.localScale = pickupScale;

        // Setup collider
        BoxCollider collider = pickupObj.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = pickupObj.AddComponent<BoxCollider>();
        }
        collider.isTrigger = true;
        collider.size = Vector3.one * 3f;  // Make collider generous

        // Visual feedback
        BuddhaPickupMaterial buddhaPickup = pickupObj.GetComponent<BuddhaPickupMaterial>();
        if (buddhaPickup == null)
        {
            buddhaPickup = pickupObj.GetComponentInChildren<BuddhaPickupMaterial>();
        }
        
        if (buddhaPickup != null)
        {
            buddhaPickup.SetColor(sequence[currentPickupIndex].color);
            buddhaPickup.SetFrequency(sequence[currentPickupIndex].frequency);
        }

        activePickups.Add(pickupObj);
        
        // Play drone for current note
        UpdateActivePickupAudio();

        Debug.Log($"Spawned pickup for note {currentPickupIndex} at {spawnPos}");
    }

    private void UpdateActivePickupAudio()
    {
        if (droneAudioSource.isPlaying)
        {
            droneAudioSource.Stop();
        }

        if (currentPickupIndex < sequence.Length)
        {
            float pitchMultiplier = sequence[currentPickupIndex].frequency / baseFrequency;
            droneAudioSource.pitch = pitchMultiplier;
            droneAudioSource.clip = baseNote;
            droneAudioSource.Play();
            Debug.Log($"Playing drone for pickup {currentPickupIndex} at frequency {sequence[currentPickupIndex].frequency}Hz");
        }
    }

    public int GetPickupIndex(GameObject pickup)
    {
        return activePickups.IndexOf(pickup);
    }

    public void OnPickupCollected(int index)
    {
        if (index != 0)
        {
            Debug.Log($"Wrong pickup collected. Expected 0, got {index}");
            return;
        }

        Debug.Log($"Collected pickup for note {currentPickupIndex}");

        if (successSound != null && successAudioSource != null)
        {
            successAudioSource.PlayOneShot(successSound, successVolume);
        }

        sequence[currentPickupIndex].isCollected = true;
        currentPickupIndex++;

        if (activePickups.Count > 0 && activePickups[0] != null)
        {
            Destroy(activePickups[0]);
            activePickups.Clear();
        }

        if (currentPickupIndex >= sequence.Length)
        {
            OnSequenceComplete();
        }
    }

    private void OnSequenceComplete()
    {
        Debug.Log("Musical sequence completed!");
        droneAudioSource.Stop();
    }
}