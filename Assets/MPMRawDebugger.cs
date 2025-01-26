using UnityEngine;
using System.Collections.Generic;

public class MPMRawDebugger : MonoBehaviour
{
    [SerializeField] private MPMAudioAnalyzer audioAnalyzer;
    [SerializeField] private bool showDetailedNSDF = false;
    [SerializeField] private float debugUpdateInterval = 0.1f;
    [SerializeField] private float lowFrequencyThreshold = 110f;  // Track anything below this
    
    private float nextDebugTime = 0f;
    private List<float> lastRawFrequencies = new List<float>();
    private const int MAX_HISTORY = 50;

    // Track frequency ranges specifically
    private Dictionary<string, int> frequencyRangeCounts = new Dictionary<string, int>
    {
        {"80-90Hz", 0},
        {"90-100Hz", 0},
        {"100-110Hz", 0},
        {"110-120Hz", 0}
    };

    private void Update()
    {
        if (Time.time < nextDebugTime) return;
        nextDebugTime = Time.time + debugUpdateInterval;

        if (!audioAnalyzer) return;

        // Get raw frequency data from MPM analyzer
        float currentFreq = audioAnalyzer.Frequency;
        float clarity = audioAnalyzer.Clarity;
        float confidence = audioAnalyzer.Confidence;

        // Track raw frequencies
        if (currentFreq > 0)
        {
            lastRawFrequencies.Add(currentFreq);
            if (lastRawFrequencies.Count > MAX_HISTORY)
                lastRawFrequencies.RemoveAt(0);

            // Count frequency ranges
            if (currentFreq >= 80 && currentFreq < 90) frequencyRangeCounts["80-90Hz"]++;
            else if (currentFreq >= 90 && currentFreq < 100) frequencyRangeCounts["90-100Hz"]++;
            else if (currentFreq >= 100 && currentFreq < 110) frequencyRangeCounts["100-110Hz"]++;
            else if (currentFreq >= 110 && currentFreq < 120) frequencyRangeCounts["110-120Hz"]++;

            // Detailed logging for low frequencies
            if (currentFreq < lowFrequencyThreshold)
            {
                Debug.Log($"[MPM_RAW] Low Frequency Detected:\n" +
                         $"Frequency: {currentFreq:F1}Hz\n" +
                         $"Clarity: {clarity:F3}\n" +
                         $"Confidence: {confidence:F3}\n" +
                         $"Amplitude: {audioAnalyzer.Amplitude:F3}");

                if (showDetailedNSDF)
                {
                    var debugData = audioAnalyzer.GetDebugData();
                    if (debugData != null)
                    {
                        Debug.Log($"[MPM_RAW] NSDF Details:\n" +
                                $"Max Value: {debugData.maxValue:F3}\n" +
                                $"Clarity Threshold: {debugData.clarityThreshold:F3}\n" +
                                $"Selected Peak Index: {debugData.selectedPeakIndex}\n" +
                                $"Peak Count: {debugData.peakIndices?.Count ?? 0}");
                    }
                }
            }
        }

        // Periodically show frequency distribution
        if (Time.frameCount % 100 == 0)
        {
            int totalSamples = 0;
            foreach (var count in frequencyRangeCounts.Values)
                totalSamples += count;

            if (totalSamples > 0)
            {
                string distribution = "[MPM_RAW] Frequency Distribution:\n";
                foreach (var kvp in frequencyRangeCounts)
                {
                    float percentage = (float)kvp.Value / totalSamples * 100f;
                    distribution += $"{kvp.Key}: {percentage:F1}% ({kvp.Value} samples)\n";
                }
                Debug.Log(distribution);
            }
        }

        // Show recent frequency history periodically
        if (Time.frameCount % 50 == 0 && lastRawFrequencies.Count > 0)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            float sum = 0;

            foreach (float freq in lastRawFrequencies)
            {
                min = Mathf.Min(min, freq);
                max = Mathf.Max(max, freq);
                sum += freq;
            }

            float avg = sum / lastRawFrequencies.Count;

            Debug.Log($"[MPM_RAW] Recent Frequency Stats:\n" +
                     $"Min: {min:F1}Hz\n" +
                     $"Max: {max:F1}Hz\n" +
                     $"Avg: {avg:F1}Hz\n" +
                     $"Sample Count: {lastRawFrequencies.Count}");
        }
    }
}