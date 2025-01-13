using UnityEngine;
using TMPro;

public class PitchVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MPMAudioAnalyzer audioAnalyzer;
    [SerializeField] private Transform currentPitchSphere;
    [SerializeField] public Transform targetPitchSphere;
    [SerializeField] private Transform playerRig; // Reference to [BuildingBlock] Camera Rig
    
    [Header("Layout")]
    [SerializeField] private float visualizerDistance = 1f;  // Distance in front of player
    [SerializeField] private float sphereBaseScale = 0.05f;  // Size of spheres
    
    [Header("Frequency Settings")]
    [SerializeField] public float targetFrequency = 130.81f; // Default to C3
    [SerializeField] private float frequencyTolerance = 5f;  // Hz tolerance
    
    [Header("Colors")]
    [SerializeField] private Color targetColor = new Color(0f, 1f, 0f, 0.8f);     // Bright green
    [SerializeField] private Color normalColor = new Color(0f, 0.5f, 1f, 0.8f);   // Sky blue
    [SerializeField] private Color closeColor = new Color(1f, 1f, 0f, 0.8f);      // Yellow
    [SerializeField] private Color matchedColor = Color.red;                       // Perfect match

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.1f;
    [SerializeField] private float colorSmoothTime = 0.1f;
    [SerializeField] private float scaleSmooth = 5f;

    [Header("Label References")]
    [SerializeField] private TextMeshPro currentFrequencyLabel;
    [SerializeField] private TextMeshPro confidenceLabel;
    [SerializeField] private TextMeshPro targetFrequencyLabel;  // Add this line
    [SerializeField] private float labelOffset = 0.15f;
    
    private Vector3 currentVelocity;
    private float currentScale = 1f;
    private Transform centerEyeAnchor;
    private Material currentSphereMaterial;
    private Material targetMaterial;
    private Color currentColor;
    
    void Start()
    {
        Debug.Log("PitchVisualizer Start called");
        
        if (!ValidateComponents()) 
        {
            Debug.LogError("Failed component validation");
            return;
        }

        // Find player rig if not set
        if (playerRig == null)
        {
            playerRig = GameObject.Find("[BuildingBlock] Camera Rig")?.transform;
            Debug.Log($"Found player rig: {(playerRig != null ? playerRig.name : "null")}");
        }
        
        // Find CenterEyeAnchor
        centerEyeAnchor = playerRig?.Find("TrackingSpace/CenterEyeAnchor");
        Debug.Log($"Found centerEyeAnchor: {(centerEyeAnchor != null ? centerEyeAnchor.name : "null")}");
        
        InitializeMaterials();
        CreateLabels();

        Debug.Log($"Current sphere position: {currentPitchSphere.position}");
        Debug.Log($"Target sphere position: {targetPitchSphere.position}");
    }

    bool ValidateComponents()
    {
        Debug.Log($"Validating - AudioAnalyzer: {audioAnalyzer != null}, CurrentSphere: {currentPitchSphere != null}, TargetSphere: {targetPitchSphere != null}");
        
        if (audioAnalyzer == null)
        {
            Debug.LogError("Missing AudioAnalyzer reference!");
            enabled = false;
            return false;
        }
        if (currentPitchSphere == null)
        {
            Debug.LogError("Missing CurrentPitchSphere reference!");
            enabled = false;
            return false;
        }
        if (targetPitchSphere == null)
        {
            Debug.LogError("Missing TargetPitchSphere reference!");
            enabled = false;
            return false;
        }
        return true;
    }

    void InitializeMaterials()
    {
        Debug.Log("Starting material initialization");
        
        // Setup current sphere material
        var currentRenderer = currentPitchSphere.GetComponent<MeshRenderer>();
        if (currentRenderer == null)
        {
            Debug.Log("Adding MeshRenderer to current sphere");
            currentRenderer = currentPitchSphere.gameObject.AddComponent<MeshRenderer>();
        }
        
        if (currentPitchSphere.GetComponent<MeshFilter>() == null)
        {
            Debug.Log("Adding MeshFilter to current sphere");
            var meshFilter = currentPitchSphere.gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        }

        currentSphereMaterial = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        SetupMaterial(currentSphereMaterial, true);
        currentRenderer.sharedMaterial = currentSphereMaterial;
        currentPitchSphere.localScale = Vector3.one * sphereBaseScale;
        Debug.Log($"Current sphere material initialized, scale: {sphereBaseScale}");

        // Setup target sphere material
        var targetRenderer = targetPitchSphere.GetComponent<MeshRenderer>();
        if (targetRenderer == null)
        {
            Debug.Log("Adding MeshRenderer to target sphere");
            targetRenderer = targetPitchSphere.gameObject.AddComponent<MeshRenderer>();
        }
        
        if (targetPitchSphere.GetComponent<MeshFilter>() == null)
        {
            Debug.Log("Adding MeshFilter to target sphere");
            var meshFilter = targetPitchSphere.gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        }

        targetMaterial = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        SetupMaterial(targetMaterial, false);
        targetRenderer.sharedMaterial = targetMaterial;
        targetPitchSphere.localScale = Vector3.one * sphereBaseScale;
        Debug.Log($"Target sphere material initialized, scale: {sphereBaseScale}");
    }

    void SetupMaterial(Material material, bool isCurrent)
    {
        material.SetFloat("_Surface", 1); // Transparent
        material.SetFloat("_Blend", 0);   // Alpha blend
        material.SetFloat("_ZWrite", 0);
        material.renderQueue = 3000;
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        
        // Set colors
        material.color = isCurrent ? normalColor : targetColor;
    }

    void Update()
    {
        if (!ValidateComponents() || centerEyeAnchor == null) return;

        UpdateSpherePositions();
        UpdateVisuals();
        UpdateLabels();
    }

    void UpdateSpherePositions()
    {
        // Position spheres in front of player
        Vector3 forward = Vector3.ProjectOnPlane(centerEyeAnchor.forward, Vector3.up).normalized;
        Vector3 basePosition = centerEyeAnchor.position + (forward * visualizerDistance);

        // Update current pitch sphere position
        if (audioAnalyzer.IsVoiceDetected)
        {
            // Match player's actual height
            Vector3 targetPos = basePosition;
            targetPos.y = centerEyeAnchor.position.y; // Use actual camera/head height instead of rig height
            currentPitchSphere.position = Vector3.SmoothDamp(
                currentPitchSphere.position,
                targetPos,
                ref currentVelocity,
                positionSmoothTime
            );
        }

        // Update target pitch sphere position
        Vector3 targetSpherePos = basePosition;
        targetSpherePos.y = PitchHeightCalculator.GetHeightForFrequency(targetFrequency);
        targetPitchSphere.position = targetSpherePos;
    }

    void UpdateVisuals()
    {
        if (!audioAnalyzer.IsVoiceDetected)
        {
            currentSphereMaterial.color = normalColor;
            currentPitchSphere.localScale = Vector3.one * sphereBaseScale;
            return;
        }

        // Update current sphere color based on pitch match
        float freqDifference = Mathf.Abs(audioAnalyzer.Frequency - targetFrequency);
        Color targetColorForState = GetColorForPitchMatch(freqDifference);
        currentSphereMaterial.color = Color.Lerp(
            currentSphereMaterial.color,
            targetColorForState,
            Time.deltaTime / colorSmoothTime
        );

        // Update scale based on confidence
        float targetScale = Mathf.Lerp(0.8f, 1.2f, audioAnalyzer.Confidence);
        currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * scaleSmooth);
        currentPitchSphere.localScale = Vector3.one * (sphereBaseScale * currentScale);

        // Keep target sphere consistent
        targetMaterial.color = targetColor;
        targetPitchSphere.localScale = Vector3.one * sphereBaseScale;
    }

    Color GetColorForPitchMatch(float freqDifference)
    {
        if (freqDifference <= 2f && audioAnalyzer.Confidence > 0.8f)
            return matchedColor;
        if (freqDifference <= frequencyTolerance)
            return closeColor;
        return normalColor;
    }

    void CreateLabels()
    {
         Debug.Log("CreateLabels called"); // Add this line
        
        // Current Frequency Label
        if (currentFrequencyLabel == null)
        {
            var labelObj = new GameObject("CurrentFreqLabel");
            labelObj.transform.parent = transform;
            currentFrequencyLabel = labelObj.AddComponent<TextMeshPro>();
            SetupLabel(currentFrequencyLabel);
        }

        // Confidence Label
        if (confidenceLabel == null)
        {
            var labelObj = new GameObject("ConfidenceLabel");
            labelObj.transform.parent = transform;
            confidenceLabel = labelObj.AddComponent<TextMeshPro>();
            SetupLabel(confidenceLabel);
        }

        // Target Frequency Label
        if (targetFrequencyLabel == null)
        {
            var labelObj = new GameObject("TargetFreqLabel");
            labelObj.transform.parent = transform;
            targetFrequencyLabel = labelObj.AddComponent<TextMeshPro>();
            SetupLabel(targetFrequencyLabel);
        }
    }

    void SetupLabel(TextMeshPro label)
    {
        label.fontSize = 2;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.transform.localScale = Vector3.one * 0.1f;
        label.fontStyle = FontStyles.Bold;
        label.outlineWidth = 0.2f;
        label.outlineColor = Color.black;
        label.renderer.sortingOrder = 1;
    }

    void UpdateLabels()
    {
        Debug.Log($"UpdateLabels called. Voice detected: {audioAnalyzer.IsVoiceDetected}");
        Debug.Log($"Current Frequency Label exists: {currentFrequencyLabel != null}");
        Debug.Log($"Confidence Label exists: {confidenceLabel != null}");

        if (!audioAnalyzer.IsVoiceDetected)
        {
            if (currentFrequencyLabel != null) currentFrequencyLabel.text = "";
            if (confidenceLabel != null) confidenceLabel.text = "";
            return;
        }

        // Update text and positions
        if (currentFrequencyLabel != null)
        {
            currentFrequencyLabel.text = $"{audioAnalyzer.Frequency:F0}Hz";
            currentFrequencyLabel.transform.position = currentPitchSphere.position + Vector3.right * labelOffset;
            currentFrequencyLabel.transform.rotation = Camera.main.transform.rotation;
            Debug.Log($"Set current frequency label: {currentFrequencyLabel.text} at position {currentFrequencyLabel.transform.position}");
            
            currentFrequencyLabel.fontSize = 2;
            currentFrequencyLabel.outlineWidth = 0.2f;
            currentFrequencyLabel.outlineColor = Color.black;
            currentFrequencyLabel.color = Color.white;
            currentFrequencyLabel.transform.localScale = Vector3.one * 0.1f;
        }

        if (confidenceLabel != null)
        {
            confidenceLabel.text = $"{(audioAnalyzer.Confidence * 100):F0}%";
            confidenceLabel.transform.position = currentPitchSphere.position + Vector3.left * labelOffset;
            confidenceLabel.transform.rotation = Camera.main.transform.rotation;
            Debug.Log($"Set confidence label: {confidenceLabel.text} at position {confidenceLabel.transform.position}");
            
            confidenceLabel.fontSize = 2;
            confidenceLabel.outlineWidth = 0.2f;
            confidenceLabel.outlineColor = Color.black;
            confidenceLabel.color = Color.white;
            confidenceLabel.transform.localScale = Vector3.one * 0.1f;
        }
    }

    void OnDestroy()
    {
        if (currentSphereMaterial != null) Destroy(currentSphereMaterial);
        if (targetMaterial != null) Destroy(targetMaterial);
    }
}