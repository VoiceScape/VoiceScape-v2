using UnityEngine;
using TMPro;

public class PitchVisualizer3POV : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MPMAudioAnalyzer audioAnalyzer;
    [SerializeField] private Transform currentPitchSphere;
    [SerializeField] public Transform targetPitchSphere;
    [SerializeField] private Transform cameraRig;

    [Header("Target Sphere Settings")]
    [SerializeField] private bool showTargetSphere = true;
    [SerializeField] private float targetSphereDistance = 1f;
    
    [Header("Height Mapping")]
    [SerializeField] public float baseHeight = 0.5f;          
    [SerializeField] public float maxHeight = 2.5f;           
    [SerializeField] public float minFrequency = 80f;         
    [SerializeField] public float maxFrequency = 300f;        
    [SerializeField] public float targetFrequency = 130.81f;  // Default to C3
    [SerializeField] [Range(0f, 1f)] private float baseHeightConfidenceThreshold = 0.7f;  
    [SerializeField] private float releaseTime = 1f;  // Time to fall after voice stops

    private float lastVoiceTime;
    private float currentReleaseValue = 1f;

    [Header("Visual Settings")]
    [SerializeField] private float sphereBaseScale = 0.05f;
    [SerializeField] private Color targetColor = new Color(0f, 1f, 0f, 0.8f);    // Green
    [SerializeField] private Color normalColor = new Color(0f, 0.5f, 1f, 0.8f);  // Blue
    [SerializeField] private Color closeColor = new Color(1f, 1f, 0f, 0.8f);     // Yellow
    [SerializeField] private Color matchedColor = Color.red;                      // Red
    [SerializeField] private float frequencyTolerance = 5f;    // Hz tolerance for color changes
    [SerializeField] private float colorTransitionSpeed = 5f;  // Speed of color changes

    [Header("Labels")]
    [SerializeField] private TextMeshPro currentFrequencyLabel;
    [SerializeField] private TextMeshPro confidenceLabel;
    [SerializeField] private TextMeshPro minFreqLabel;
    [SerializeField] private TextMeshPro maxFreqLabel;
    [SerializeField] private bool showFrequencyLabel = true;
    [SerializeField] private bool showConfidenceLabel = true;
    [SerializeField] private float labelOffset = 0.15f;
    [SerializeField] private TextMeshPro targetFrequencyLabel;



    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showHeightDebug = true;
    [SerializeField] private Color heightMarkerColor = new Color(1f, 1f, 0f, 0.3f);
    
    // Private members
    private Material currentSphereMaterial;
    private Material targetMaterial;
    private Color currentColor;
    private float currentScale = 1f;
    private Transform centerEyeAnchor;

    private void Start()
    {
        InitializeComponents();
        InitializeMaterials();
        InitializeLabels();
        SetupLabels();

        // Set initial colors
        if (currentSphereMaterial != null)
        {
            currentSphereMaterial.color = normalColor;
            Debug.Log("Set initial sphere color to normal (blue)");
        }
    }

    private void InitializeLabels()
    {
        TextMeshPro[] allLabels = {currentFrequencyLabel, confidenceLabel, minFreqLabel, maxFreqLabel, targetFrequencyLabel};
        foreach (var label in allLabels)
        {
            if (label != null)
            {
                // Force enable the label and its parent
                label.gameObject.SetActive(true);
                if (label.transform.parent != null)
                    label.transform.parent.gameObject.SetActive(true);

                // Set text properties
                label.fontSize = 1;  // Larger size
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                
                // Ensure proper scale
                label.transform.localScale = Vector3.one;
                
                // Ensure renderer is enabled
                var renderer = label.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = true;

                Debug.Log($"Label '{label.name}' initialized: Active={label.gameObject.activeSelf}, " +
                         $"Parent Active={label.transform.parent?.gameObject.activeSelf}, " +
                         $"Scale={label.transform.localScale}, " +
                         $"Font Size={label.fontSize}, " +
                         $"Position={label.transform.position}");
            }
        }

        // Set initial texts
        if (minFreqLabel != null) minFreqLabel.text = $"{minFrequency:F0}Hz";
        if (maxFreqLabel != null) maxFreqLabel.text = $"{maxFrequency:F0}Hz";
    }

    private void InitializeComponents()
    {
        // Find AudioAnalyzer if not set
        if (audioAnalyzer == null)
        {
            audioAnalyzer = FindObjectOfType<MPMAudioAnalyzer>();
            if (audioAnalyzer == null) Debug.LogError("Could not find MPMAudioAnalyzer!");
        }

        // Find camera rig components
        if (cameraRig == null)
        {
            cameraRig = GameObject.Find("[BuildingBlock] Camera Rig")?.transform;
        }

        if (cameraRig != null)
        {
            // First try direct path matching Unity hierarchy
            centerEyeAnchor = cameraRig.Find("TrackingSpace/CenterEyeAnchor");
            
            Debug.Log($"[PitchViz3POV] Found camera rig: {cameraRig.name}");
            var trackingSpace = cameraRig.Find("TrackingSpace");
            Debug.Log($"[PitchViz3POV] Found TrackingSpace: {trackingSpace != null}");
            if (trackingSpace != null)
            {
                centerEyeAnchor = trackingSpace.Find("CenterEyeAnchor");
                Debug.Log($"[PitchViz3POV] Found CenterEyeAnchor: {centerEyeAnchor != null}");
            }
            
            // If still not found, try direct search
            if (centerEyeAnchor == null)
            {
                centerEyeAnchor = cameraRig.GetComponentInChildren<Camera>()?.transform;
                Debug.Log($"[PitchViz3POV] Found camera through GetComponentInChildren: {centerEyeAnchor != null}");
            }
        }

        if (centerEyeAnchor == null)
        {
            Debug.LogError($"Could not find CenterEyeAnchor! CameraRig found: {cameraRig != null}. Hierarchy should be: [BuildingBlock] Camera Rig/TrackingSpace/CenterEyeAnchor");
        }
        else
        {
            Debug.Log($"[PitchViz3POV] Successfully found CenterEyeAnchor: {GetTransformPath(centerEyeAnchor)}");
        }

        // Disable target sphere if needed
        if (targetPitchSphere != null)
        {
            targetPitchSphere.gameObject.SetActive(showTargetSphere);
        }
    }

    private string GetTransformPath(Transform transform)
    {
        if (transform == null) return "null";
        string path = transform.name;
        Transform parent = transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }

    private void InitializeMaterials()
    {
        // Setup materials for current sphere
        SetupSphere(currentPitchSphere, ref currentSphereMaterial, normalColor);
        
        // Setup materials for target sphere
        if (showTargetSphere)
        {
            SetupSphere(targetPitchSphere, ref targetMaterial, targetColor);
        }
    }

    private void SetupSphere(Transform sphere, ref Material material, Color color)
    {
        if (sphere == null) return;

        var renderer = sphere.GetComponent<MeshRenderer>();
        var meshFilter = sphere.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            meshFilter = sphere.gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        }

        if (renderer == null)
        {
            renderer = sphere.gameObject.AddComponent<MeshRenderer>();
        }

        material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        material.SetFloat("_Surface", 1);
        material.SetFloat("_Blend", 0);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0);
        material.renderQueue = 3000;
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.color = color;

        renderer.sharedMaterial = material;
        sphere.localScale = Vector3.one * sphereBaseScale;
    }

    private void SetupLabels()
    {
        if (minFreqLabel != null)
        {
            minFreqLabel.text = $"{minFrequency:F0}Hz";
            minFreqLabel.transform.position = new Vector3(
                minFreqLabel.transform.position.x,
                0f,
                minFreqLabel.transform.position.z
            );
        }

        if (maxFreqLabel != null)
        {
            maxFreqLabel.text = $"{maxFrequency:F0}Hz";
            maxFreqLabel.transform.position = new Vector3(
                maxFreqLabel.transform.position.x,
                maxHeight,
                maxFreqLabel.transform.position.z
            );
        }
    }

    private void Update()
    {
        if (!ValidateComponents()) return;

        UpdateTargetSphere();
        UpdateCurrentSphere();
        UpdateLabels();
        AlignLabelsToCamera();
    }

    private bool ValidateComponents()
    {
        return audioAnalyzer != null && currentPitchSphere != null && 
               (!showTargetSphere || targetPitchSphere != null) && centerEyeAnchor != null;
    }

    private void UpdateTargetSphere()
    {
        if (!showTargetSphere || targetPitchSphere == null) return;

        // Position target sphere in front of headset
        Vector3 targetPos = centerEyeAnchor.position + centerEyeAnchor.forward * targetSphereDistance;
        
        // Always use full amplitude (1.0) for target sphere height, ignoring release
        float targetHeight = PitchHeightCalculator.GetHeightForFrequency(targetFrequency);
        targetPos.y = targetHeight;
        
        targetPitchSphere.position = targetPos;

        // Update target frequency label
        if (targetFrequencyLabel != null && showFrequencyLabel)
        {
            targetFrequencyLabel.text = $"{targetFrequency:F0}Hz";
            targetFrequencyLabel.transform.position = targetPitchSphere.position + Vector3.right * labelOffset;
        }
    }

    private void UpdateCurrentSphere()
    {
        if (!audioAnalyzer.IsVoiceDetected) 
        {
            // Reset to normal color when voice not detected
            if (currentSphereMaterial != null)
            {
                currentSphereMaterial.color = normalColor;
            }
            return;
        }

        // Update height
        float currentHeight = PitchHeightCalculator.GetHeightForFrequency(audioAnalyzer.Frequency);
        Vector3 position = currentPitchSphere.position;
        position.y = currentHeight;
        currentPitchSphere.position = position;

        // Update scale based on confidence
        float confidence = audioAnalyzer.Confidence;
        currentScale = Mathf.Lerp(0.8f, 1.2f, confidence);
        currentPitchSphere.localScale = Vector3.one * sphereBaseScale * currentScale;

        // Calculate frequency difference for color update
        float freqDifference = Mathf.Abs(audioAnalyzer.Frequency - targetFrequency);

        // Determine target color
        Color targetColorForState;
        if (freqDifference <= frequencyTolerance * 0.2f && confidence > 0.8f)
        {
            targetColorForState = matchedColor;    // Red when matched
        }
        else if (freqDifference <= frequencyTolerance)
        {
            targetColorForState = closeColor;      // Yellow when close
        }
        else
        {
            targetColorForState = normalColor;     // Blue when far
        }

        // Update material color
        if (currentSphereMaterial != null)
        {
            currentSphereMaterial.color = Color.Lerp(
                currentSphereMaterial.color, 
                targetColorForState, 
                Time.deltaTime * colorTransitionSpeed
            );
        }

        UpdateLabels();
    }

    private float CalculateCurrentSphereHeight(float frequency)
    {
        // Update release value
        if (audioAnalyzer.IsVoiceDetected && audioAnalyzer.Confidence > baseHeightConfidenceThreshold)
        {
            lastVoiceTime = Time.time;
            currentReleaseValue = 1f;
        }
        else if (Time.time - lastVoiceTime > 0f)
        {
            currentReleaseValue = Mathf.Max(0f, currentReleaseValue - (Time.deltaTime / releaseTime));
        }

        // Base height with release
        float heightFromAmplitude = baseHeight * currentReleaseValue;

        float heightFromPitch = PitchHeightCalculator.GetHeightForFrequency(frequency);

        return heightFromAmplitude + heightFromPitch;
    }

    private void UpdateLabels()
    {
        if (!audioAnalyzer.IsVoiceDetected)
        {
            if (currentFrequencyLabel != null) 
            {
                currentFrequencyLabel.text = "";
            }
            if (confidenceLabel != null)
            {
                confidenceLabel.text = "";
            }
            return;
        }

        // Update frequency label
        if (currentFrequencyLabel != null && showFrequencyLabel)
        {
            currentFrequencyLabel.text = $"{audioAnalyzer.Frequency:F0}Hz";
            currentFrequencyLabel.transform.position = currentPitchSphere.position + Vector3.up * labelOffset;
        }

        // Update confidence label
        if (confidenceLabel != null && showConfidenceLabel)
        {
            confidenceLabel.text = $"{(audioAnalyzer.Confidence * 100):F0}%";
            confidenceLabel.transform.position = currentPitchSphere.position + Vector3.down * labelOffset;
        }
    }

    private void AlignLabelsToCamera()
    {
        if (Camera.main == null) return;

        TextMeshPro[] labels = { currentFrequencyLabel, confidenceLabel, minFreqLabel, maxFreqLabel };
        foreach (var label in labels)
        {
            if (label != null)
            {
                label.transform.LookAt(Camera.main.transform);
                label.transform.Rotate(0, 180, 0);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        // Draw height markers
        if (showHeightDebug)
        {
            Gizmos.color = heightMarkerColor;
            
            // Draw base height marker
            Vector3 basePos = currentPitchSphere.position;
            basePos.y = baseHeight;
            Gizmos.DrawWireCube(basePos, new Vector3(0.5f, 0.01f, 0.5f));
            
            // Draw max height marker
            Vector3 maxPos = basePos;
            maxPos.y = maxHeight;
            Gizmos.DrawWireCube(maxPos, new Vector3(0.5f, 0.01f, 0.5f));
            
            // Draw current height line
            Gizmos.DrawLine(
                new Vector3(currentPitchSphere.position.x, 0, currentPitchSphere.position.z),
                currentPitchSphere.position
            );

            // Draw target frequency height
            Gizmos.color = Color.yellow;
            float targetHeight = PitchHeightCalculator.GetHeightForFrequency(targetFrequency);
            Gizmos.DrawWireCube(new Vector3(currentPitchSphere.position.x, targetHeight, currentPitchSphere.position.z), 
                               new Vector3(0.3f, 0.01f, 0.3f));
        }

        // Draw target sphere reference
        if (showTargetSphere && centerEyeAnchor != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 targetPos = centerEyeAnchor.position + centerEyeAnchor.forward * targetSphereDistance;
            Gizmos.DrawWireSphere(targetPos, 0.1f);
            
            // Draw height reference line
            Gizmos.DrawLine(targetPos, new Vector3(targetPos.x, PitchHeightCalculator.GetHeightForFrequency(targetFrequency), targetPos.z));
        }
    }

    private void LateUpdate()
    {
        if (Camera.main == null) return;

        // Update all labels to face camera and maintain visibility
        TextMeshPro[] allLabels = {currentFrequencyLabel, confidenceLabel, minFreqLabel, maxFreqLabel};
        foreach (var label in allLabels)
        {
            if (label != null && label.gameObject.activeSelf)
            {
                // Make label face camera
                label.transform.rotation = Camera.main.transform.rotation;
                
                // Update height-based labels
                if (label == minFreqLabel)
                {
                    label.transform.position = new Vector3(currentPitchSphere.position.x - labelOffset, 0f, currentPitchSphere.position.z);
                }
                else if (label == maxFreqLabel)
                {
                    label.transform.position = new Vector3(currentPitchSphere.position.x - labelOffset, maxHeight, currentPitchSphere.position.z);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Log collision for debugging
        Debug.Log($"Collision detected between {currentPitchSphere.name} and {other.gameObject.name}");

        // Find PickupManager and check if this is an active pickup
        var pickupManager = FindObjectOfType<PickupManager3POV>();
        if (pickupManager != null && pickupManager.IsActivePickup(other.gameObject))
        {
            Debug.Log("Valid pickup collision detected!");
            pickupManager.OnPickupCollected(other.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (currentSphereMaterial != null) Destroy(currentSphereMaterial);
        if (targetMaterial != null) Destroy(targetMaterial);
    }
}