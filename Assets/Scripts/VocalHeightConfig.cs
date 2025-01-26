using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New VocalHeightConfig", menuName = "MonoBehaviour Script/VocalHeightConfig")]
public class VocalHeightConfig : ScriptableObject
{
    [Header("Height Range")]
    [Tooltip("Minimum flying height (should be above max terrain height)")]
    public float baseHeight = 20f;
    
    [Tooltip("Maximum flying height")]
    public float maxHeight = 40f;

    [Header("Frequency Range")]
    [Tooltip("Lowest tracked frequency")]
    public float minFrequency = 80f;
    
    [Tooltip("Highest tracked frequency")]
    public float maxFrequency = 300f;

    [Header("Safety Buffers")]
    [Tooltip("Minimum distance to keep above terrain")]
    public float terrainBuffer = 5f;
    
    [Tooltip("Extra height buffer for pickup spawning")]
    public float spawnHeightBuffer = 10f;

    // Utility methods for height calculations
    public float GetSafeSpawnHeight(Vector3 position)
    {
        float terrainHeight = GetTerrainHeightAt(position);
        return Mathf.Max(baseHeight, terrainHeight + spawnHeightBuffer);
    }

    public float GetSafeHeight(float targetHeight, Vector3 position)
    {
        float terrainHeight = GetTerrainHeightAt(position);
        return Mathf.Max(targetHeight, terrainHeight + terrainBuffer);
    }

    private float GetTerrainHeightAt(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up * 1000f, Vector3.down, 
            out RaycastHit hit, 2000f, LayerMask.GetMask("Terrain")))
        {
            return hit.point.y;
        }
        return 0f;
    }

    private void OnValidate()
    {
        // Ensure heights are valid
        if (maxHeight <= baseHeight)
        {
            maxHeight = baseHeight + 10f;
            Debug.LogWarning($"{name}: Max height must be greater than base height!");
        }

        // Ensure frequencies are valid
        if (maxFrequency <= minFrequency)
        {
            maxFrequency = minFrequency + 100f;
            Debug.LogWarning($"{name}: Max frequency must be greater than min frequency!");
        }

        // Ensure buffers are positive
        if (terrainBuffer < 0) terrainBuffer = 0;
        if (spawnHeightBuffer < 0) spawnHeightBuffer = 0;
    }
}