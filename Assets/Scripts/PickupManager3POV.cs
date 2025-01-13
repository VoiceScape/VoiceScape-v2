using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class MusicalPickup3POV
{
    public float frequency;      
    public Color color;         
    public float tolerance = 5f; 
    public bool isCollected;
    public int missedAttempts; 
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
    private bool isProcessingPickup = false;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip baseNote;         // Monkchant/drone sound
    [SerializeField] private AudioClip successSound;
    [SerializeField] private float droneVolume = 0.7f;
    [SerializeField] private float successVolume = 0.5f;
    [SerializeField] private float baseFrequency = 130.81f;  // C3 reference for pitch shifting

    [Header("UI Elements")]
    [SerializeField] private GameObject vocalRangePanel;
    [SerializeField] private TextMeshProUGUI rangeReportText;
    [SerializeField] private Button continueButton;
    [SerializeField] private float fadeInDuration = 0.5f;

    private CanvasGroup panelCanvasGroup;
    private bool isComplete = false;
    
    public MusicalPickup3POV[] sequence;

    private int currentPickupIndex = 0;
    private GameObject activePickup;
    private AudioSource successAudioSource;
    private AudioSource droneAudioSource;      // For continuous note playback
    private Transform centerEyeAnchor;
    private bool sequenceComplete = false;
    private bool reportShown = false;

    private void OnEnable()
    {
    Debug.Log("PickupManager3POV enabled. Active pickup prefab layer: " + 
              (pickupPrefab != null ? pickupPrefab.layer.ToString() : "null"));
    }
    
    private void Awake()
    {
        // Initialize sequence
        sequence = new MusicalPickup3POV[]
        {
            // Lower octave
            new MusicalPickup3POV { frequency = 130.81f, color = new Color(0f, 0.5f, 1f, 0.8f) },    // C3
            new MusicalPickup3POV { frequency = 155.56f, color = new Color(0f, 1f, 0f, 0.8f) },      // Eb3
            new MusicalPickup3POV { frequency = 174.61f, color = new Color(1f, 1f, 0f, 0.8f) },      // F3
            new MusicalPickup3POV { frequency = 196.00f, color = new Color(1f, 0.5f, 0f, 0.8f) },    // G3
            new MusicalPickup3POV { frequency = 233.08f, color = new Color(1f, 0f, 0f, 0.8f) },      // Bb3
            new MusicalPickup3POV { frequency = 261.63f, color = new Color(0f, 0.5f, 1f, 0.8f) },    // C4
            // Higher octave
            new MusicalPickup3POV { frequency = 311.13f, color = new Color(0f, 1f, 0f, 0.8f) },      // Eb4
            new MusicalPickup3POV { frequency = 349.23f, color = new Color(1f, 1f, 0f, 0.8f) },      // F4
            /* new MusicalPickup3POV { frequency = 392.00f, color = new Color(1f, 0.5f, 0f, 0.8f) },    // G4
            new MusicalPickup3POV { frequency = 466.16f, color = new Color(1f, 0f, 0f, 0.8f) },      // Bb4
            new MusicalPickup3POV { frequency = 523.25f, color = new Color(0f, 0.5f, 1f, 0.8f) },    // C5
            new MusicalPickup3POV { frequency = 261.63f, color = new Color(0f, 0.5f, 1f, 0.8f) },    // C4 (return to middle) */
        };
    }
    
    private void Start()
    {
        InitializeComponents();
        
        // Ensure VocalRangeData exists
        if (VocalRangeData.Instance == null)
        {
            GameObject dataObject = new GameObject("VocalRangeData");
            dataObject.AddComponent<VocalRangeData>();
        }

        // Hide report panel and ensure references exist
        if (vocalRangePanel != null)
        {
            panelCanvasGroup = vocalRangePanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = vocalRangePanel.AddComponent<CanvasGroup>();
            vocalRangePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Vocal Range Panel reference not set in inspector!");
        }
        
        // Reset any data from previous runs
        VocalRangeData.Instance.ResetRange();

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
        if (sequenceComplete)
        {
            if (!reportShown)
            {
                ShowVocalRangeReport();
                reportShown = true;
            }
            return;
        }

        if (activePickup != null)
        {
            // Store current height before movement
            float currentHeight = activePickup.transform.position.y;

            // Calculate movement in X/Z plane only
            Vector3 toCamera = centerEyeAnchor.position - activePickup.transform.position;
            toCamera.y = 0; // Project onto X/Z plane
            Vector3 moveDirection = toCamera.normalized;

            // Move pickup
            Vector3 newPosition = activePickup.transform.position + moveDirection * approachSpeed * Time.deltaTime;
            newPosition.y = currentHeight; // Maintain original height
            activePickup.transform.position = newPosition;

            // Check if pickup has passed camera (using X/Z distance only)
            toCamera = centerEyeAnchor.position - activePickup.transform.position;
            toCamera.y = 0; // Check horizontal distance only
            if (toCamera.magnitude < 0.5f)
            {
                sequence[currentPickupIndex].missedAttempts++;
                Debug.Log($"Pickup missed ({sequence[currentPickupIndex].missedAttempts} attempts) at height {activePickup.transform.position.y:F2}");
                
                if (sequence[currentPickupIndex].missedAttempts >= 2)
                {
                    // Advance to next note after two misses
                    Debug.Log($"Moving to next note after {sequence[currentPickupIndex].missedAttempts} failed attempts at {sequence[currentPickupIndex].frequency}Hz");
                    currentPickupIndex++;
                    
                    // Check if we've reached the end
                    if (currentPickupIndex >= sequence.Length)
                    {
                        sequenceComplete = true;
                        Destroy(activePickup);
                        return;
                    }
                }

                Destroy(activePickup);
                activePickup = null;
            }
        }
        else if (!sequenceComplete)
        {
            SpawnNextPickup();
        }
    }

    private void SpawnNextPickup()
    {
        if (activePickup != null) return;

        // Get spawn position in front of camera with random X offset
        float xOffset = Random.Range(-2f, 2f);  // Random offset between -2 and 2 meters
        Vector3 spawnPosition = centerEyeAnchor.position + centerEyeAnchor.forward * spawnDistance;
        spawnPosition.x += xOffset;

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
        
        // Calculate height with additional debug info
        float currentFreq = sequence[currentPickupIndex].frequency;
        float targetHeight = PitchHeightCalculator.GetHeightForFrequency(currentFreq);
        spawnPosition.y = targetHeight;

        Debug.Log($"Spawning pickup {currentPickupIndex}: Freq={currentFreq:F1}Hz, " +
                $"Height={targetHeight:F2}m, SpawnPos={spawnPosition}");

        // Create pickup
        activePickup = Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
        activePickup.transform.localScale = pickupScale;

        // Setup visual feedback
        var material = activePickup.GetComponentInChildren<BuddhaPickupMaterial>();
        if (material != null)
        {
            material.SetColor(sequence[currentPickupIndex].color);
        }

        // Verify final position matches calculated height
        if (Mathf.Abs(activePickup.transform.position.y - targetHeight) > 0.01f)
        {
            Debug.LogWarning($"Pickup height mismatch after spawn! Expected: {targetHeight:F2}, Got: {activePickup.transform.position.y:F2}");
            Vector3 correctedPos = activePickup.transform.position;
            correctedPos.y = targetHeight;
            activePickup.transform.position = correctedPos;
        }
    }

    public void OnPickupCollected(GameObject pickup)
    {
        if (isProcessingPickup || sequenceComplete)
        {
            Debug.Log("Ignoring pickup collection - already processing or sequence complete");
            return;
        }
        
        isProcessingPickup = true;
        
        if (pickup == null)
        {
            Debug.LogWarning("OnPickupCollected called with null pickup");
            isProcessingPickup = false;
            return;
        }

        if (activePickup == null)
        {
            Debug.LogWarning("OnPickupCollected called but no active pickup exists");
            isProcessingPickup = false;
            return;
        }

        if (pickup != activePickup)
        {
            Debug.LogWarning($"Pickup collected was not active pickup. Active: {activePickup.name}, Collected: {pickup.name}");
            isProcessingPickup = false;
            return;
        }

        Debug.Log($"Successfully collecting pickup {currentPickupIndex + 1} of {sequence.Length}");

        // Play success sound
        if (successSound != null && successAudioSource != null)
        {
            successAudioSource.PlayOneShot(successSound, successVolume);
            Debug.Log("Playing success sound");
        }

        // Track successful frequency
        float currentFreq = sequence[currentPickupIndex].frequency;
        VocalRangeData.Instance.lowestSuccessfulFrequency = 
            Mathf.Min(VocalRangeData.Instance.lowestSuccessfulFrequency, currentFreq);
        VocalRangeData.Instance.highestSuccessfulFrequency = 
            Mathf.Max(VocalRangeData.Instance.highestSuccessfulFrequency, currentFreq);
        VocalRangeData.Instance.successfulFrequencies.Add(currentFreq);

        sequence[currentPickupIndex].isCollected = true;
        
        // Cleanup current pickup
        Destroy(activePickup);
        activePickup = null;

        // Advance to next pickup
        currentPickupIndex++;
        Debug.Log($"Advanced to pickup index {currentPickupIndex} of {sequence.Length}");
        
        // Check if sequence is complete AFTER incrementing
        if (currentPickupIndex >= sequence.Length)
        {
            Debug.Log("Final pickup collected, marking sequence complete");
            sequenceComplete = true;
        }

        isProcessingPickup = false;
    }

    private void ShowVocalRangeReport()
    {
        Debug.Log("ShowVocalRangeReport called");
        
        if (pitchVisualizer != null)
        {
            pitchVisualizer.gameObject.SetActive(false);
        }

        if (vocalRangePanel == null)
        {
            Debug.LogError("vocalRangePanel is null!");
            return;
        }

        // Stop all audio first - with double check
        if (droneAudioSource != null)
        {
            droneAudioSource.Stop();
            droneAudioSource.clip = null;  // Clear the clip to ensure it stops
            droneAudioSource.enabled = false;  // Disable the audio source
            Debug.Log("Stopped drone audio");
        }
        if (successAudioSource != null)
        {
            successAudioSource.Stop();
            successAudioSource.enabled = false;
            Debug.Log("Stopped success audio");
        }

        // Ensure VocalRangeData exists
        if (VocalRangeData.Instance == null)
        {
            Debug.LogError("VocalRangeData.Instance is null!");
            return;
        }

        // Calculate results with safety checks
        int totalAttempts = sequence?.Length ?? 0;
        int successfulNotes = VocalRangeData.Instance.successfulFrequencies?.Count ?? 0;
        
        if (totalAttempts == 0)
        {
            Debug.LogError("No sequence data available!");
            return;
        }

        float successRate = (float)successfulNotes / totalAttempts * 100f;
        Debug.Log($"Calculating results: {successfulNotes} successful notes out of {totalAttempts} attempts");

        // Update text with results
        if (rangeReportText != null)
        {
            string report = "<b>Vocal Range Assessment</b>\n\n";
            
            if (successfulNotes > 0)
            {
                report += $"Lowest Note: {VocalRangeData.Instance.lowestSuccessfulFrequency:F1} Hz\n" +
                        $"Highest Note: {VocalRangeData.Instance.highestSuccessfulFrequency:F1} Hz\n\n";
            }
            else
            {
                report += "No successful notes recorded\n\n";
            }
            
            report += $"Notes Hit: {successfulNotes} out of {totalAttempts}\n" +
                    $"Success Rate: {successRate:F1}%";

            rangeReportText.text = report;
            Debug.Log($"Updated report text: {report}");
        }
        else
        {
            Debug.LogError("rangeReportText reference is missing!");
            return;
        }

        // Configure canvas for VR with correct scale and position
        Canvas canvas = vocalRangePanel.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = centerEyeAnchor?.GetComponent<Camera>();
            
            if (centerEyeAnchor != null)
            {
                vocalRangePanel.transform.parent = null;
                Vector3 position = centerEyeAnchor.position + centerEyeAnchor.forward * 5f;
                vocalRangePanel.transform.position = position;
                vocalRangePanel.transform.rotation = centerEyeAnchor.rotation;
                vocalRangePanel.transform.localScale = Vector3.one * 0.01f;
            }
        }

        vocalRangePanel.SetActive(true);
        Debug.Log("Activated vocal range panel");

        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueToNextScene);
            Debug.Log("Set up continue button");
        }
    }

    private void OnReportContinue()
    {
        vocalRangePanel.SetActive(false);
        // Optional: Add any additional actions here (scene transition, etc.)
    }

    private IEnumerator ShowReportAnimation()
    {
        vocalRangePanel.SetActive(true);
        panelCanvasGroup.alpha = 0;

        float elapsed = 0;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = elapsed / fadeInDuration;
            yield return null;
        }

        panelCanvasGroup.alpha = 1;
    }

    private void OnContinueToNextScene()
    {
        // Save data before transition
        VocalRangeData.Instance.SaveToPlayerPrefs();
        
        // Load next scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("UnityAudioProto2"); // Adjust scene name as needed
    }

    public bool IsActivePickup(GameObject pickup)
    {
        return pickup == activePickup;
    }
}