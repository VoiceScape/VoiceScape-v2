using UnityEngine;
using TMPro;

public class HeightDebugger : MonoBehaviour 
{
    [Header("References")]
    [SerializeField] private Transform playerRig;
    [SerializeField] private MPMAudioAnalyzer audioAnalyzer;
    [SerializeField] private PitchVisualizer pitchVisualizer;
    [SerializeField] private Transform currentPitchSphere;
    [SerializeField] private Transform targetPitchSphere;

    [Header("Debug Display")]
    [SerializeField] private TextMeshProUGUI debugText;
    
    private void Update()
    {
        if (!ValidateComponents()) return;

        float playerHeight = playerRig.position.y;
        float currentPitchHeight = currentPitchSphere.position.y;
        float targetPitchHeight = targetPitchSphere.position.y;
        
        float currentVoicePitchHeight = PitchHeightCalculator.GetHeightForFrequency(audioAnalyzer.Frequency);
        float targetFreqHeight = PitchHeightCalculator.GetHeightForFrequency(pitchVisualizer.targetFrequency);

        string debugInfo = $"Heights:\n" +
                         $"Player: {playerHeight:F2}m\n" +
                         $"Current Voice: {currentVoicePitchHeight:F2}m\n" +
                         $"Target: {targetFreqHeight:F2}m\n" +
                         $"Current Sphere: {currentPitchHeight:F2}m\n" +
                         $"Target Sphere: {targetPitchHeight:F2}m\n" +
                         $"Voice Freq: {audioAnalyzer.Frequency:F1}Hz\n" +
                         $"Target Freq: {pitchVisualizer.targetFrequency:F1}Hz";

        debugText.text = debugInfo;

        // Draw debug lines
        Debug.DrawLine(playerRig.position, 
            playerRig.position + Vector3.right * 2f, Color.blue, Time.deltaTime);
        Debug.DrawLine(currentPitchSphere.position, 
            currentPitchSphere.position + Vector3.right * 2f, Color.green, Time.deltaTime);
        Debug.DrawLine(targetPitchSphere.position, 
            targetPitchSphere.position + Vector3.right * 2f, Color.red, Time.deltaTime);
    }

    private bool ValidateComponents()
    {
        return playerRig != null && audioAnalyzer != null && 
               pitchVisualizer != null && debugText != null &&
               currentPitchSphere != null && targetPitchSphere != null;
    }
}