using UnityEngine;
using TMPro;

public class BuddhaPickupMaterial : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Material materialInstance;
    private TextMeshPro frequencyLabel;
    private TextMeshPro heightLabel;
    private float frequency;
    private const float LABEL_OFFSET = 0.2f;

    private void Awake()
    {
        // Get the renderer
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer == null)
            {
                Debug.LogError("No MeshRenderer found!");
                return;
            }
        }

        // Create material instance and set up for transparency
        if (meshRenderer.sharedMaterial != null)
        {
            materialInstance = new Material(meshRenderer.sharedMaterial);
            
            // Configure material for transparency
            materialInstance.SetFloat("_Surface", 1); // 0 = opaque, 1 = transparent
            materialInstance.SetFloat("_Blend", 0);   // 0 = alpha, 1 = premultiply
            
            // Set blend mode
            materialInstance.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            materialInstance.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            
            // Enable transparency keywords
            materialInstance.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            materialInstance.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            
            // Set render queue for transparency
            materialInstance.renderQueue = 3000;
            
            meshRenderer.material = materialInstance;
            Debug.Log($"Created transparent material instance for {gameObject.name}. Shader: {materialInstance.shader.name}");
        }

        // Create labels
        CreateLabel(ref frequencyLabel, "FrequencyLabel", Vector3.up * LABEL_OFFSET);
        CreateLabel(ref heightLabel, "HeightLabel", Vector3.down * LABEL_OFFSET);
    }

    private void CreateLabel(ref TextMeshPro label, string name, Vector3 offset)
    {
        GameObject labelObj = new GameObject(name);
        labelObj.transform.parent = transform;
        labelObj.transform.localPosition = offset;
        
        label = labelObj.AddComponent<TextMeshPro>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 1;
        label.color = Color.white;
    }

    public void SetFrequency(float freq)
    {
        frequency = freq;
        if (frequencyLabel != null)
        {
            frequencyLabel.text = $"{freq:F1}Hz";
        }
        if (heightLabel != null)
        {
            heightLabel.text = $"Target: {freq:F1}Hz";
            // float height = PitchHeightCalculator.GetHeightForFrequency(freq);
            // heightLabel.text = $"{height:F2}m";  // This line needs to change

        }
    }

    public void SetColor(Color color)
    {
        if (materialInstance != null)
        {
            Debug.Log($"Setting material color to {color} on {gameObject.name}");
            materialInstance.SetColor("_BaseColor", color);  // URP uses _BaseColor instead of color
            materialInstance.SetColor("_Color", color);      // Backup in case shader variant uses _Color
        }
        else
        {
            Debug.LogError("No material instance!");
        }
    }

    private void LateUpdate()
    {
        if (Camera.main != null)
        {
            // Make labels face camera
            if (frequencyLabel != null)
            {
                frequencyLabel.transform.rotation = Camera.main.transform.rotation;
            }
            if (heightLabel != null)
            {
                heightLabel.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}