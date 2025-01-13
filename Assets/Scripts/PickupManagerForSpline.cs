using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MusicalPickupSpline
{
    public float frequency;      // Target frequency in Hz
    public Color color;         // Visual feedback color
    public float tolerance = 5f; // Frequency tolerance in Hz
    public bool isCollected;
}

public class PickupManagerForSpline : MonoBehaviour 
{
    [Header("References")]
    [SerializeField] private Transform cameraRig;
    [SerializeField] private PitchVisualizerForSpline pitchVisualizer;
    public PickupCollectorForSpline pickupCollectorForSpline; 
    
    [Header("Spawn Settings")]
    //[SerializeField] private GameObject pickupPrefab;
    [SerializeField] private SplineCollectableSpawner splineCollectableSpawner;
    //[SerializeField] private float spawnDistance = 10f;    // Distance from camera to spawn
    [SerializeField] private float approachSpeed = 2f;     // Units per second
    [SerializeField] private Vector3 pickupScale = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("Audio Settings")]
    [SerializeField] private AudioClip baseNote;         // Monkchant/drone sound
    [SerializeField] private AudioClip successSound;
    [SerializeField] private float droneVolume = 0.7f;
    [SerializeField] private float successVolume = 0.5f;
    [SerializeField] private float baseFrequency = 130.81f;  // C3 reference for pitch shifting

    // C minor pentatonic sequence C - Eb - F - G - Bb (and back to C)
    private MusicalPickupSpline[] sequence = new MusicalPickupSpline[]
    {
        new MusicalPickupSpline { frequency = 130.81f, color = new Color(0f, 0.5f, 1f, 0.8f) },    // C3
        new MusicalPickupSpline { frequency = 155.56f, color = new Color(0f, 1f, 0f, 0.8f) },      // Eb3
        new MusicalPickupSpline { frequency = 174.61f, color = new Color(1f, 1f, 0f, 0.8f) },      // F3
        new MusicalPickupSpline { frequency = 196.00f, color = new Color(1f, 0.5f, 0f, 0.8f) },    // G3
        new MusicalPickupSpline { frequency = 233.08f, color = new Color(1f, 0f, 0f, 0.8f) },      // Bb3
        new MusicalPickupSpline { frequency = 130.81f, color = new Color(0f, 0.5f, 1f, 0.8f) },    // C3
    };

    public int currentPickupIndex = 0;
    [SerializeField] private GameObject activePickup;
    private AudioSource successAudioSource;
    private AudioSource droneAudioSource;      // For continuous note playback
    //private Transform centerEyeAnchor;

    private void OnEnable()
    {
    Debug.Log("PickupManagerForSpline enabled. Active pickup prefab layer: " + 
              (splineCollectableSpawner.collectablePrefab != null ? splineCollectableSpawner.collectablePrefab.layer.ToString() : "null"));
    }
    
    private void Start()
    {
        InitializeComponents();
        
        //Initialize height calculator with visualizer settings
        if (pitchVisualizer != null)
        {
            PitchHeightCalculator.Initialize(
                pitchVisualizer.baseHeight,
                pitchVisualizer.maxHeight,
                pitchVisualizer.minFrequency,
                pitchVisualizer.maxFrequency,
                pitchVisualizer.maxVerticalAngle,
                pitchVisualizer.verticalOffset,
                pitchVisualizer.visualizerDistance
            );
        }

        // Optionally spawn collectibles when the game starts.
        if (splineCollectableSpawner.spawnOnStart)
        {
            splineCollectableSpawner.SpawnCollectables();
        }
        
        GetNextActivePickup();
    }

    private void InitializeComponents()
    {
        if (cameraRig == null)
            cameraRig = GameObject.Find("[BuildingBlock] Camera Rig")?.transform;

        //if (cameraRig != null)
            //centerEyeAnchor = cameraRig.Find("TrackingSpace/CenterEyeAnchor");

        // Setup audio
        successAudioSource = gameObject.AddComponent<AudioSource>();
        successAudioSource.playOnAwake = false;
        successAudioSource.spatialBlend = 0f;
        successAudioSource.volume = successVolume;
        successAudioSource.loop = true;

        droneAudioSource = gameObject.AddComponent<AudioSource>();
        droneAudioSource.playOnAwake = false;
        droneAudioSource.spatialBlend = 1f;
        droneAudioSource.volume = droneVolume;
        droneAudioSource.loop = false;
    }

    private void Update()
    {
        if (activePickup != null)
        {
            // Move pickup toward camera
            // Vector3 moveDirection = (pickupCollectorForSpline.transform.position - activePickup.transform.position).normalized;
            // activePickup.transform.position += moveDirection * approachSpeed * Time.deltaTime;
        }
        else
        {
            GetNextActivePickup();
        }
    }

    public float GetSequenceFrequency()
    {
        var freq = sequence[currentPickupIndex].frequency;
        
        return freq;
    }
    
    public float GetSequenceFrequency(int value)
    {
        var freq = sequence[value].frequency;
        
        return freq;
    }

    private void GetNextActivePickup()
    {
        if (activePickup != null) return;
        
        // Update target sphere frequency
        if (pitchVisualizer != null)
        {
            pitchVisualizer.targetFrequency = sequence[currentPickupIndex].frequency;
        }
        
        activePickup = splineCollectableSpawner.GetCurrentCollectable();

        if (activePickup == null) return;
        
        // Setup visual feedback
        var material = activePickup.GetComponentInChildren<BuddhaPickupMaterial>();
        if (material != null)
        {
            //material.SetColor(sequence[currentPickupIndex].color);
        }
        
        
        // Update drone pitch and play
        if (droneAudioSource != null && baseNote != null)
        {
            float pitchMultiplier = sequence[currentPickupIndex].frequency / baseFrequency;
            droneAudioSource.pitch = pitchMultiplier;
            droneAudioSource.clip = baseNote;
            droneAudioSource.Play();
        }

        
        //
        // var pos = activePickup.transform.position;
        //
        // pos.y = activePickup.transform.position.y + 
        //         PitchHeightCalculator.GetPositionForFrequency(sequence[currentPickupIndex].frequency).y;
        //
        // activePickup.transform.position = new Vector3(pos.x, pos.y, pos.z);
        
        
    }

    /*
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
        Vector3 spawnPosition = pickupCollectorForSpline.transform.position + pickupCollectorForSpline.transform.forward;
        
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
    */

    public void OnPickupCollected(GameObject pickup)
    {
        if (pickup != activePickup) return;
        
        //splineCollectableSpawner.CloseCollectable(activePickup);
        
        if (successSound != null)
        {
            successAudioSource.PlayOneShot(successSound, successVolume);
        }

        sequence[currentPickupIndex].isCollected = true;
        currentPickupIndex = (currentPickupIndex + 1) % sequence.Length;
        
        splineCollectableSpawner.currentCollectable = currentPickupIndex;
        
        activePickup = null;
        
        GetNextActivePickup();
    }

    public bool IsActivePickup(GameObject pickup)
    {
        return pickup == activePickup;
    }
}