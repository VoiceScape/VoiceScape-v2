using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    [Header("ADSR Envelope")]
    [SerializeField] private float attackTime = 0.1f;
    [SerializeField] private float decayTime = 0.1f;  // Shorter decay since going to full sustain
    [SerializeField] private float sustainLevel = 1.0f;  // Full sustain
    [SerializeField] private float releaseTime = 0.5f;  // Quicker release for responsiveness
    
    [Header("Tilt Movement")]
    [SerializeField] private float maxTiltSpeed = 2f;
    [SerializeField] private float tiltDeadzone = 5f;
    [SerializeField] private Vector2 tiltRange = new Vector2(0f, 45f);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Component references
    private MPMAudioAnalyzer audioAnalyzer;
    private Transform cameraTransform;
    private Transform playerRig;
    
    // State tracking
    private float currentHeight;
    private float targetHeight;
    private float lastVoiceTime;
    private float envelopeValue;
    private Vector3 currentVelocity;
    private float velocityY;

    private void Start()
    {
        InitializeComponents();
        InitializeHeight();
    }

    private void InitializeComponents()
    {
        // Find and validate AudioAnalyzer
        var audioManager = GameObject.Find("AudioManager");
        if (audioManager != null)
        {
            audioAnalyzer = audioManager.GetComponent<MPMAudioAnalyzer>();
            if (audioAnalyzer == null)
            {
                Debug.LogError("Could not find MPMAudioAnalyzer on AudioManager!");
                enabled = false;
                return;
            }
        }

        // Find VR camera references
        Transform trackingSpace = transform.parent.Find("TrackingSpace");
        if (trackingSpace != null)
        {
            cameraTransform = trackingSpace.Find("CenterEyeAnchor");
            if (cameraTransform == null)
            {
                Debug.LogError("Could not find CenterEyeAnchor!");
                enabled = false;
                return;
            }
        }

        playerRig = transform.parent;
    }

    private void InitializeHeight()
    {
        // Initialize at base height
        currentHeight = SceneManager.Instance.GetSafeHeight(SceneManager.Instance.baseHeight);
        if (playerRig != null)
        {
            Vector3 pos = playerRig.position;
            pos.y = currentHeight;
            playerRig.position = pos;
        }
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
            
            // Get target height from frequency
            float rawTargetHeight = PitchHeightCalculator.GetHeightForFrequency(audioAnalyzer.Frequency);
            targetHeight = SceneManager.Instance.GetSafeHeight(rawTargetHeight);
            
            // Update envelope
            envelopeValue = UpdateEnvelope(true);
        }
        else if (Time.time - lastVoiceTime > 1f)
        {
            envelopeValue = UpdateEnvelope(false);
            targetHeight = SceneManager.Instance.GetSafeHeight(SceneManager.Instance.baseHeight);
        }

        // Apply envelope to height difference from base
        float baseHeight = SceneManager.Instance.baseHeight;
        float heightDifference = targetHeight - baseHeight;
        float targetHeightWithEnvelope = baseHeight + (heightDifference * envelopeValue);

        // Smooth transition
        float smoothTime = audioAnalyzer.IsVoiceDetected ? attackTime : releaseTime;
        currentHeight = Mathf.SmoothDamp(currentHeight, targetHeightWithEnvelope, ref velocityY, smoothTime);
        
        // Update rig position
        if (playerRig != null)
        {
            Vector3 position = playerRig.position;
            position.y = currentHeight;
            playerRig.position = position;
        }

        if (showDebugLogs)
        {
            Debug.Log($"Height: {currentHeight:F2}m, Target: {targetHeight:F2}m, Freq: {audioAnalyzer.Frequency:F0}Hz");
        }
    }

    private float UpdateEnvelope(bool isActive)
    {
        float newValue = envelopeValue;
        
        if (isActive)
        {
            if (newValue < 1f)
                newValue += Time.deltaTime / attackTime;
            else if (newValue > sustainLevel)
                newValue -= Time.deltaTime / decayTime;
        }
        else
            newValue -= Time.deltaTime / releaseTime;
        
        return Mathf.Clamp01(newValue);
    }

    private void UpdateTiltMovement()
    {
        if (cameraTransform == null) return;

        float currentTiltAngle = Vector3.SignedAngle(Vector3.up, cameraTransform.up, Vector3.right);
        Vector3 movement = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        
        float speed = maxTiltSpeed * 0.3f;
        
        if (currentTiltAngle > tiltDeadzone)
        {
            float tiltScale = Mathf.InverseLerp(tiltDeadzone, tiltRange.y, currentTiltAngle);
            speed += maxTiltSpeed * 0.7f * tiltScale;
        }
        else if (currentTiltAngle < -tiltDeadzone)
        {
            float tiltScale = Mathf.InverseLerp(-tiltDeadzone, -tiltRange.y, currentTiltAngle);
            speed *= (1f - tiltScale);
        }
        
        Vector3 delta = movement * speed * Time.deltaTime;
        if (playerRig != null)
        {
            playerRig.position += delta;
        }
    }
}