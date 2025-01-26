using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance { get; private set; }

    [Header("Height Settings")]
    public float baseHeight = 20f;         // Set this well above terrain
    public float maxHeight = 40f;          // Maximum flying height
    public float minFrequency = 80f;       // Lowest tracked frequency
    public float maxFrequency = 300f;      // Highest tracked frequency

    [Header("Scene References")]
    [SerializeField] private MPMAudioAnalyzer audioAnalyzer;
    [SerializeField] private PlayerMovementController playerController;
    [SerializeField] private PickupManager pickupManager;
    [SerializeField] private PitchVisualizer pitchVisualizer;

    private void Awake()
    {
        Instance = this;
        InitializeHeightSettings();
        ValidateComponents();
    }

    private void Start()
    {
        FindComponents();
        InitializeAudioAnalyzer();
    }

    private void InitializeHeightSettings()
    {
        PitchHeightCalculator.Initialize(
            baseHeight,
            maxHeight,
            minFrequency,
            maxFrequency
        );
    }

    private void FindComponents()
    {
        audioAnalyzer ??= FindFirstObjectByType<MPMAudioAnalyzer>();
        playerController ??= FindFirstObjectByType<PlayerMovementController>();
        pickupManager ??= FindFirstObjectByType<PickupManager>();
        pitchVisualizer ??= FindFirstObjectByType<PitchVisualizer>();
    }

    private void ValidateComponents()
    {
        // Could add additional validation if needed
    }

    private void InitializeAudioAnalyzer()
    {
        if (audioAnalyzer != null)
        {
            audioAnalyzer.minFrequency = minFrequency;
            audioAnalyzer.maxFrequency = maxFrequency;
        }
    }

    // Simplified height getters - no terrain checks
    public float GetSafeHeight(float targetHeight)
    {
        return targetHeight;  // Just return the target height directly
    }

    // Access to core components for other scripts
    public MPMAudioAnalyzer AudioAnalyzer => audioAnalyzer;
    public PlayerMovementController PlayerController => playerController;
    public PickupManager PickupManager => pickupManager;
    public PitchVisualizer PitchVisualizer => pitchVisualizer;
}