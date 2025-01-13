using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    [Header("ADSR Envelope")]
    [SerializeField] private float attackTime = 0.1f;           // How quickly height responds to voice
    [SerializeField] private float decayTime = 0.2f;            // Transition to sustain level
    [SerializeField] private float sustainLevel = 0.8f;         // Percentage of max amplitude
    [SerializeField] private float releaseTime = 1.0f;          // How slowly height falls
    [SerializeField] private float fallDelay = 1f;              // Time before falling starts
    
    [Header("Pitch Modulation")]
    [SerializeField] private float pitchModulationStrength = 1f;// How much pitch affects height
    [SerializeField] private float pitchSmoothTime = 0.1f;      // Smoothing for pitch changes
    
    [Header("Tilt Movement")]
    [SerializeField] private float maxTiltSpeed = 2f;           // Maximum movement speed
    [SerializeField] private float tiltDeadzone = 5f;           // Degrees of tilt to ignore
    [SerializeField] private Vector2 tiltRange = new Vector2(0f, 45f); // Min/Max tilt angles
    
    [Header("Ground Following")]
    [SerializeField] private float terrainCheckDistance = 100f; // How far to check for ground
    [SerializeField] private LayerMask terrainLayer;            // Layer for terrain detection
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool showTiltDebug = false;
    [SerializeField] private bool showVoiceDebug = false;
    
    // Component references
    private MPMAudioAnalyzer audioAnalyzer;
    private Transform cameraTransform;  // CenterEyeAnchor
    private Transform playerRig;        // Parent Camera Rig
    
    // State tracking
    private float currentHeight;
    private float targetHeight;
    private float lastVoiceTime;
    private float envelopeValue;
    private Vector3 currentVelocity;
    private float smoothedPitchOffset;
    private float velocityY;

    private void Start()
    {
        if (showDebugLogs) Debug.Log("PlayerMovementController starting initialization...");

        InitializeComponents();
        
        // Initialize starting position at base height
        currentHeight = PitchHeightCalculator.BaseHeight;
        if (playerRig != null)
        {
            playerRig.position = new Vector3(
                playerRig.position.x, 
                PitchHeightCalculator.BaseHeight, 
                playerRig.position.z
            );
            if (showDebugLogs) Debug.Log($"Set initial rig position to: {playerRig.position}");
        }
    }

    private void InitializeComponents()
    {
        // Find the AudioManager and validate hierarchy
        var audioManager = GameObject.Find("AudioManager");
        if (showDebugLogs) Debug.Log($"AudioManager found: {audioManager != null}");

        if (audioManager != null)
        {
            audioAnalyzer = audioManager.GetComponent<MPMAudioAnalyzer>();
            if (showDebugLogs) Debug.Log($"MPMAudioAnalyzer component found: {audioAnalyzer != null}");
            
            if (audioAnalyzer == null)
            {
                Debug.LogError("Could not find MPMAudioAnalyzer on AudioManager!");
                enabled = false;
                return;
            }
        }
        else
        {
            Debug.LogError("Could not find AudioManager in scene!");
            enabled = false;
            return;
        }

        // Find VR camera references using exact path
        Transform trackingSpace = transform.parent.Find("TrackingSpace");
        if (showDebugLogs) Debug.Log($"TrackingSpace found: {trackingSpace != null}");

        if (trackingSpace != null)
        {
            cameraTransform = trackingSpace.Find("CenterEyeAnchor");
            if (showDebugLogs) Debug.Log($"CenterEyeAnchor found: {cameraTransform != null}");
            if (cameraTransform == null)
            {
                Debug.LogError("Could not find CenterEyeAnchor! Check VR camera rig hierarchy.");
                enabled = false;
                return;
            }
        }

        playerRig = transform.parent;
        if (showDebugLogs) Debug.Log($"Player rig assigned: {playerRig != null}, name: {playerRig?.name}");
    }

    private void Update()
    {
        UpdateVoiceHeight();
        UpdateTiltMovement();
    }

    private void UpdateVoiceHeight()
    {
        if (audioAnalyzer.IsVoiceDetected)
        {
            lastVoiceTime = Time.time;
            
            // Get full target height from pitch - no amplitude scaling
            float baseTargetHeight = PitchHeightCalculator.GetHeightForFrequency(audioAnalyzer.Frequency);
            
            // Add pitch modulation if needed
            float pitchOffset = (audioAnalyzer.Frequency - PitchHeightCalculator.MinFrequency) / 
                              (PitchHeightCalculator.MaxFrequency - PitchHeightCalculator.MinFrequency) * 
                              pitchModulationStrength;
            smoothedPitchOffset = Mathf.SmoothDamp(smoothedPitchOffset, pitchOffset, ref velocityY, pitchSmoothTime);
            
            // Update envelope
            envelopeValue = UpdateEnvelope(true);
            
            // Use full height when voice is detected
            targetHeight = baseTargetHeight * envelopeValue;

            if (showVoiceDebug) 
            {
                Debug.Log($"Voice - Freq: {audioAnalyzer.Frequency:F0}Hz, " +
                         $"Height: {targetHeight:F1}m");
            }
        }
        else if (Time.time - lastVoiceTime > fallDelay)
        {
            // Update envelope for release
            envelopeValue = UpdateEnvelope(false);
            targetHeight = PitchHeightCalculator.BaseHeight * envelopeValue;
        }

        // Smooth height movement
        float smoothTime = audioAnalyzer.IsVoiceDetected ? attackTime : releaseTime;
        currentHeight = Mathf.SmoothDamp(currentHeight, targetHeight, ref velocityY, smoothTime);
        
        // Update position of the entire rig
        Vector3 position = playerRig.position;
        position.y = currentHeight + GetTerrainHeight();
        playerRig.position = position;
    }


    private float UpdateEnvelope(bool isActive)
    {
        float newEnvelopeValue = envelopeValue;
        
        if (isActive)
        {
            if (newEnvelopeValue < 1f)
            {
                // Attack phase
                newEnvelopeValue += Time.deltaTime / attackTime;
            }
            else if (newEnvelopeValue > sustainLevel)
            {
                // Decay phase
                newEnvelopeValue -= Time.deltaTime / decayTime;
            }
        }
        else
        {
            // Release phase
            newEnvelopeValue -= Time.deltaTime / releaseTime;
        }
        
        return Mathf.Clamp01(newEnvelopeValue);
    }

    private void UpdateTiltMovement()
    {
        if (cameraTransform == null) return;

        // Get head tilt angle
        float currentTiltAngle = Vector3.SignedAngle(Vector3.up, cameraTransform.up, Vector3.right);
        
        // Calculate movement direction (forward in gaze direction)
        Vector3 movement = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        
        // Base movement speed
        float speed = maxTiltSpeed * 0.3f; // 30% of max speed as base speed
        
        // Modify speed based on tilt
        if (currentTiltAngle > tiltDeadzone) // Forward tilt
        {
            // Scale additional speed by forward tilt amount
            float tiltScale = Mathf.InverseLerp(tiltDeadzone, tiltRange.y, currentTiltAngle);
            speed += maxTiltSpeed * 0.7f * tiltScale; // Add up to 70% more speed
        }
        else if (currentTiltAngle < -tiltDeadzone) // Backward tilt
        {
            // Rapidly reduce speed based on backward tilt
            float tiltScale = Mathf.InverseLerp(-tiltDeadzone, -tiltRange.y, currentTiltAngle);
            speed *= (1f - tiltScale); // Reduce speed to 0 at max backward tilt
        }
        
        // Apply movement
        Vector3 delta = movement * speed * Time.deltaTime;
        playerRig.position += delta;

        if (showTiltDebug)
        {
            Debug.Log($"Tilt: angle={currentTiltAngle:F1}°, speed={speed:F2}");
        }
    }

    private float GetTerrainHeight()
    {
        if (Physics.Raycast(playerRig.position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, terrainCheckDistance, terrainLayer))
        {
            return hit.point.y;
        }
        return 0f;
    }
}