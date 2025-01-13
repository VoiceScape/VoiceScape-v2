using UnityEngine;

public static class PitchHeightCalculatorLinear
{
    // Core height parameters
    public static float BaseHeight = 4.0f;
    public static float MaxHeight = 20.0f;
    public static float MinFrequency = 100f;
    public static float MaxFrequency = 600f;

    // Calculate basic height without release behavior
    public static float GetHeightForFrequency(float frequency)
    {
        // Ensure frequency is in valid range
        frequency = Mathf.Clamp(frequency, MinFrequency, MaxFrequency);
        
        // Calculate normalized frequency using logarithmic scale
        float normalizedFreq = (Mathf.Log(frequency) - Mathf.Log(MinFrequency)) / 
                             (Mathf.Log(MaxFrequency) - Mathf.Log(MinFrequency));
        normalizedFreq = Mathf.Clamp01(normalizedFreq);

        // Calculate final height
        float heightFromPitch = normalizedFreq * (MaxHeight - BaseHeight);
        return BaseHeight + heightFromPitch;
    }

    // Initialize with current settings
    public static void Initialize(float baseHeight, float maxHeight, float minFreq, float maxFreq)
    {
        BaseHeight = baseHeight;
        MaxHeight = maxHeight;
        MinFrequency = minFreq;
        MaxFrequency = maxFreq;
    }
}