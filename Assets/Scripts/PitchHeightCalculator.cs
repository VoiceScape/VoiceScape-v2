using UnityEngine;

public static class PitchHeightCalculator
{
    // Core height parameters as specified in README
    public static float BaseHeight = 4f;    // Height at 100 Hz
    public static float MaxHeight = 20f;    // Maximum height
    public static float MinFrequency = 80f; // Lowest supported note
    public static float MaxFrequency = 300f;// Highest supported note

    // Calculate height using logarithmic scale
    public static float GetHeightForFrequency(float frequency)
    {
        // Ensure frequency is in valid range
        frequency = Mathf.Clamp(frequency, MinFrequency, MaxFrequency);
        
        // Calculate normalized frequency using logarithmic scale
        float normalizedFreq = (Mathf.Log(frequency) - Mathf.Log(MinFrequency)) / 
                             (Mathf.Log(MaxFrequency) - Mathf.Log(MinFrequency));
        normalizedFreq = Mathf.Clamp01(normalizedFreq);

        // Calculate final height using the formula from README
        return BaseHeight + normalizedFreq * (MaxHeight - BaseHeight);
    }

    // Initialize with custom settings if needed
    public static void Initialize(float baseHeight, float maxHeight, float minFreq, float maxFreq)
    {
        BaseHeight = baseHeight;
        MaxHeight = maxHeight;
        MinFrequency = minFreq;
        MaxFrequency = maxFreq;
        
        Debug.Log($"PitchHeightCalculator initialized with: " +
                 $"BaseHeight={BaseHeight}m, MaxHeight={MaxHeight}m, " +
                 $"MinFreq={MinFrequency}Hz, MaxFreq={MaxFrequency}Hz");
    }
}