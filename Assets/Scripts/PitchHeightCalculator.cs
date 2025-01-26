using UnityEngine;

public static class PitchHeightCalculator
{
    // Core height parameters
    public static float BaseHeight = 20f;     // Default to SceneManager values
    public static float MaxHeight = 40f;
    public static float MinFrequency = 80f;
    public static float MaxFrequency = 300f;

    // Calculate height without release behavior
    public static float GetHeightForFrequency(float frequency)
    {
        // Ensure frequency is in valid range
        frequency = Mathf.Clamp(frequency, MinFrequency, MaxFrequency);
        
        // Calculate normalized frequency using logarithmic scale
        float normalizedFreq = (Mathf.Log(frequency) - Mathf.Log(MinFrequency)) / 
                             (Mathf.Log(MaxFrequency) - Mathf.Log(MinFrequency));
        normalizedFreq = Mathf.Clamp01(normalizedFreq);
    
        // Calculate final height
        float heightRange = MaxHeight - BaseHeight;
        return BaseHeight + (normalizedFreq * heightRange);
    }

    // Initialize with current settings
    public static void Initialize(float baseHeight, float maxHeight, float minFreq, float maxFreq)
    {
        BaseHeight = baseHeight;
        MaxHeight = maxHeight;
        MinFrequency = minFreq;
        MaxFrequency = maxFreq;
        
        Debug.Log($"PitchHeightCalculator initialized with: " +
                 $"Base Height={BaseHeight}m, Max Height={MaxHeight}m, " +
                 $"Freq Range={MinFrequency}Hz-{MaxFrequency}Hz");
    }
}