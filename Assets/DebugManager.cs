using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }

    [Header("Debug Categories")]
    [SerializeField] private bool masterDebugToggle = true;
    [Space]
    [SerializeField] private bool debugHeights = true;
    [SerializeField] private bool debugPickups = true;
    [SerializeField] private bool debugProximity = true;
    [SerializeField] private bool debugVisualizer = true;
    [SerializeField] private bool debugAudio = true;

    [Header("Debug Intervals")]
    [SerializeField] private float heightLogInterval = 0.5f;
    [SerializeField] private float proximityLogInterval = 0.5f;
    [SerializeField] private float visualizerLogInterval = 0.5f;

    // Cached components
    private Transform playerRig;
    private MPMAudioAnalyzer audioAnalyzer;
    private PickupManager pickupManager;
    private PitchVisualizer pitchVisualizer;

    // Debug state tracking
    private float nextHeightLogTime;
    private float nextProximityLogTime;
    private float nextVisualizerLogTime;
    private int lastLoggedPickupIndex = -1;

    private void Awake()
    {
        Instance = this;
        Debug.Log("[VOXR_DEBUG] DebugManager initializing...");
        InitializeDebugComponents();
    }

    private void InitializeDebugComponents()
    {
        playerRig = GameObject.Find("[BuildingBlock] Camera Rig")?.transform;
        Debug.Log($"[VOXR_DEBUG] Found player rig: {(playerRig != null)}");

        // Wait for SceneManager to initialize
        StartCoroutine(WaitForSceneManager());
    }

    private System.Collections.IEnumerator WaitForSceneManager()
    {
        while (SceneManager.Instance == null)
            yield return null;

        audioAnalyzer = SceneManager.Instance.AudioAnalyzer;
        pickupManager = SceneManager.Instance.PickupManager;
        pitchVisualizer = SceneManager.Instance.PitchVisualizer;

        Debug.Log($"[VOXR_DEBUG] Component References:\n" +
                 $"Audio Analyzer: {audioAnalyzer != null}\n" +
                 $"Pickup Manager: {pickupManager != null}\n" +
                 $"Pitch Visualizer: {pitchVisualizer != null}");

        // Force an immediate debug log of initial state
        if (masterDebugToggle)
        {
            if (debugHeights) LogHeightDebug();
            if (debugVisualizer) LogVisualizerDebug();
        }
    }

    private void Update()
    {
        if (!masterDebugToggle) return;

        if (debugHeights && Time.time >= nextHeightLogTime) 
            LogHeightDebug();

        if (debugProximity && Time.time >= nextProximityLogTime) 
            LogProximityDebug();

        if (debugVisualizer && Time.time >= nextVisualizerLogTime) 
            LogVisualizerDebug();
    }

    private void LogHeightDebug()
    {
        nextHeightLogTime = Time.time + heightLogInterval;

        if (!audioAnalyzer || !pitchVisualizer || !playerRig) return;

        float playerHeight = playerRig.position.y;
        float currentVoiceHeight = PitchHeightCalculator.GetHeightForFrequency(audioAnalyzer.Frequency);
        float targetHeight = PitchHeightCalculator.GetHeightForFrequency(pitchVisualizer.targetFrequency);

        Debug.Log($"[VOXR_HEIGHT] Comparison:\n" +
                 $"Player: {playerHeight:F2}m\n" +
                 $"Voice Height: {currentVoiceHeight:F2}m\n" +
                 $"Target Height: {targetHeight:F2}m\n" +
                 $"Current Freq: {audioAnalyzer.Frequency:F1}Hz\n" +
                 $"Target Freq: {pitchVisualizer.targetFrequency:F1}Hz\n" +
                 $"Height Diff: {Mathf.Abs(playerHeight - targetHeight):F2}m");
    }

    private void LogProximityDebug()
    {
        nextProximityLogTime = Time.time + proximityLogInterval;

        if (!pickupManager || pickupManager.activePickups.Count == 0 || !audioAnalyzer) return;

        GameObject pickup = pickupManager.activePickups[0];
        if (!pickup) return;

        float distance = Vector3.Distance(playerRig.position, pickup.transform.position);
        
        // Only log when near pickup
        if (distance > 2.5f) return;

        float playerHeight = playerRig.position.y;
        float pickupHeight = pickup.transform.position.y;
        float targetFreq = pickupManager.sequence[pickupManager.currentPickupIndex].frequency;
        float currentFreq = audioAnalyzer.IsVoiceDetected ? audioAnalyzer.Frequency : 0f;

        Debug.Log($"[VOXR_PROXIMITY] Pickup #{pickupManager.currentPickupIndex}:\n" +
                 $"Player Height: {playerHeight:F2}m\n" +
                 $"Pickup Height: {pickupHeight:F2}m\n" +
                 $"Height Diff: {Mathf.Abs(playerHeight - pickupHeight):F2}m\n" +
                 $"Target Freq: {targetFreq:F1}Hz\n" +
                 $"Current Freq: {currentFreq:F1}Hz\n" +
                 $"Freq Diff: {Mathf.Abs(targetFreq - currentFreq):F1}Hz\n" +
                 $"Distance: {distance:F2}m");
    }

    private void LogVisualizerDebug()
    {
        nextVisualizerLogTime = Time.time + visualizerLogInterval;

        if (!pitchVisualizer || !audioAnalyzer) return;

        float currentSphereY = pitchVisualizer.currentPitchSphere.position.y;
        float targetSphereY = pitchVisualizer.targetPitchSphere.position.y;
        float expectedCurrentY = PitchHeightCalculator.GetHeightForFrequency(audioAnalyzer.Frequency);
        float expectedTargetY = PitchHeightCalculator.GetHeightForFrequency(pitchVisualizer.targetFrequency);

        Debug.Log($"[VOXR_VISUALIZER] Positions:\n" +
                 $"Current Sphere: {currentSphereY:F2}m (Expected: {expectedCurrentY:F2}m)\n" +
                 $"Target Sphere: {targetSphereY:F2}m (Expected: {expectedTargetY:F2}m)\n" +
                 $"Difference: {Mathf.Abs(currentSphereY - expectedCurrentY):F2}m\n" +
                 $"Labels Active: {(pitchVisualizer.currentFrequencyLabel?.gameObject.activeInHierarchy ?? false)}");
    }

    // Public methods for other scripts to log specific events
    public void LogPickupSpawn(int pickupIndex, GameObject pickup)
    {
        if (!masterDebugToggle || !debugPickups || !pickupManager) return;
        if (pickupIndex == lastLoggedPickupIndex) return;

        lastLoggedPickupIndex = pickupIndex;
        float freq = pickupManager.sequence[pickupIndex].frequency;
        float expectedHeight = PitchHeightCalculator.GetHeightForFrequency(freq);

        Debug.Log($"[VOXR_PICKUP] Spawn #{pickupIndex}:\n" +
                 $"Frequency: {freq:F1}Hz\n" +
                 $"Position: {pickup.transform.position}\n" +
                 $"Height: {pickup.transform.position.y:F2}m\n" +
                 $"Expected Height: {expectedHeight:F2}m\n" +
                 $"Height Diff: {Mathf.Abs(pickup.transform.position.y - expectedHeight):F2}m\n" +
                 $"Active Pickups: {pickupManager.activePickups.Count}");
    }

    public void LogPickupMiss(int pickupIndex, GameObject pickup)
    {
        if (!masterDebugToggle || !debugPickups) return;

        Debug.Log($"[VOXR_PICKUP] Miss #{pickupIndex}:\n" +
                 $"Position: {pickup.transform.position}\n" +
                 $"Player Position: {playerRig.position}\n" +
                 $"Distance: {Vector3.Distance(playerRig.position, pickup.transform.position):F2}m");
    }
}