using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MusicalPickup3POV
{
    public float frequency;      // Target frequency in Hz
    public Color color;         // Visual feedback color
    public float tolerance = 5f; // Frequency tolerance in Hz
    public bool isCollected;
}

public class PickupManager3POV : MonoBehaviour 
{
    [Header("References")]
    [SerializeField] private Transform cameraRig;
    [SerializeField] private PitchVisualizer3POV pitchVisualizer;
    
    [Header("Spawn Settings")]
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private float spawnDistance = 10f;    // Distance from camera to spawn
    [SerializeField] private float approachSpeed = 2f;     // Units per second
    [SerializeField] private Vector3 pickupScale = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("Audio Settings")]
    [SerializeField] private AudioClip baseNote;         // Monkchant/drone sound
    [SerializeField] private AudioClip successSound;
    [SerializeField] private float droneVolume = 0.7f;
    [SerializeField] private float successVolume = 0.5f;
    [SerializeField] private float baseFrequency = 130.81f;  // C3 reference for pitch shifting

    // C minor pentatonic sequence C - Eb - F - G - Bb (and back to C)
    private MusicalPickup3POV[] sequence = new MusicalPickup3POV[]
    {
        new MusicalPickup3POV { frequency = 130.81f, color = new Color(0f, 0.5f, 1f, 0.8f) },    // C3
        new MusicalPickup3POV { frequency = 155.56f, color = new Color(0f, 1f, 0f, 0.8f) },      // Eb3
        new MusicalPickup3POV { frequency = 174.61f, color = new Color(1f, 1f, 0f, 0.8f) },      // F3
        new MusicalPickup3POV { frequency = 196.00f, color = new Color(1f, 0.5f, 0f, 0.8f) },    // G3
        new MusicalPickup3POV { frequency = 233.08f, color = new Color(1f, 0f, 0f, 0.8f) },      // Bb3
        new MusicalPickup3POV { frequency = 130.81f, color = new Color(0f, 0.5f, 1f, 0.8f) },    // C3
    };

    private int currentPickupIndex = 0;
    private GameObject activePickup;
    private AudioSource successAudioSource;
    private AudioSource droneAudioSource;      // For continuous note playback
    private Transform centerEyeAnchor;

    private void OnEnable()
    {
    Debug.Log("PickupManager3POV enabled. Active pickup prefab layer: " + 
              (pickupPrefab != null ? pickupPrefab.layer.ToString() : "null"));
    }
    
    private void Start()
    {
        InitializeComponents();
        
        // Initialize height calculator with visualizer settings
        if (pitchVisualizer != null)
        {
            PitchHeightCalculator.Initialize(
                pitchVisualizer.baseHeight,
                pitchVisualizer.maxHeight,
                pitchVisualizer.minFrequency,
                pitchVisualizer.maxFrequency
            );
        }

        SpawnNextPickup();
    }

    private void InitializeComponents()
    {
        if (cameraRig == null)
            cameraRig = GameObject.Find("[BuildingBlock] Camera Rig")?.transform;

        if (cameraRig != null)
            centerEyeAnchor = cameraRig.Find("TrackingSpace/CenterEyeAnchor");

        // Setup audio
        successAudioSource = gameObject.AddComponent<AudioSource>();
        successAudioSource.playOnAwake = false;
        successAudioSource.spatialBlend = 0f;
        successAudioSource.volume = successVolume;

        droneAudioSource = gameObject.AddComponent<AudioSource>();
        droneAudioSource.playOnAwake = false;
        droneAudioSource.spatialBlend = 1f;
        droneAudioSource.volume = droneVolume;
        droneAudioSource.loop = true;
    }

    private void Update()
    {
        if (activePickup != null)
        {
            // Move pickup toward camera
            Vector3 moveDirection = (centerEyeAnchor.position - activePickup.transform.position).normalized;
            activePickup.transform.position += moveDirection * approachSpeed * Time.deltaTime;

            // Check if pickup has passed camera
            float distanceToCamera = Vector3.Distance(activePickup.transform.position, centerEyeAnchor.position);
            if (distanceToCamera < 0.5f)
            {
                // Missed pickup - respawn it
                Destroy(activePickup);
                SpawnNextPickup();
            }
        }
        else
        {
            SpawnNextPickup();
        }
    }

    private void SpawnNextPickup()
    {
        if (activePickup != null) return;

        // Update target sphere frequency
        if (pitchVisualizer != null)
        {
            pitchVisualizer.targetFrequency = sequence[currentPickupIndex].frequency;
        }

        // Update drone pitch and play
        if (droneAudioSource != null && baseNote != null)
        {
            float pitchMultiplier = sequence[currentPickupIndex].frequency / baseFrequency;
            droneAudioSource.pitch = pitchMultiplier;
            droneAudioSource.clip = baseNote;
            droneAudioSource.Play();
        }

        // Get spawn position in front of camera
        Vector3 spawnPosition = centerEyeAnchor.position + centerEyeAnchor.forward * spawnDistance;
        
        // Set correct height using shared calculator
        float targetHeight = PitchHeightCalculator.GetHeightForFrequency(sequence[currentPickupIndex].frequency);
        spawnPosition.y = targetHeight;

        // Create pickup
        activePickup = Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
        activePickup.transform.localScale = pickupScale;

        // Setup visual feedback
        var material = activePickup.GetComponentInChildren<BuddhaPickupMaterial>();
        if (material != null)
        {
            material.SetColor(sequence[currentPickupIndex].color);
        }
    }

    public void OnPickupCollected(GameObject pickup)
    {
        if (pickup != activePickup) return;

        if (successSound != null)
        {
            successAudioSource.PlayOneShot(successSound, successVolume);
        }

        sequence[currentPickupIndex].isCollected = true;
        currentPickupIndex = (currentPickupIndex + 1) % sequence.Length;

        Destroy(activePickup);
        activePickup = null;
    }

    public bool IsActivePickup(GameObject pickup)
    {
        return pickup == activePickup;
    }
}