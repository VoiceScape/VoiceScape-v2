// VocalRangeData.cs
using UnityEngine;
using System.Collections.Generic;

public class VocalRangeData : MonoBehaviour
{
    public static VocalRangeData Instance { get; private set; }  // Singleton pattern
    
    public float lowestSuccessfulFrequency = float.MaxValue;
    public float highestSuccessfulFrequency = float.MinValue;
    public List<float> successfulFrequencies = new List<float>();

    private void Awake()
    {
        // Singleton pattern to persist between scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetFloat("VocalRangeLowest", lowestSuccessfulFrequency);
        PlayerPrefs.SetFloat("VocalRangeHighest", highestSuccessfulFrequency);
        PlayerPrefs.Save();
        
        Debug.Log($"Saved vocal range: {lowestSuccessfulFrequency:F1}Hz to {highestSuccessfulFrequency:F1}Hz");
    }

    public void LoadFromPlayerPrefs()
    {
        lowestSuccessfulFrequency = PlayerPrefs.GetFloat("VocalRangeLowest", float.MaxValue);
        highestSuccessfulFrequency = PlayerPrefs.GetFloat("VocalRangeHighest", float.MinValue);
        
        Debug.Log($"Loaded vocal range: {lowestSuccessfulFrequency:F1}Hz to {highestSuccessfulFrequency:F1}Hz");
    }

    public void ResetRange()
    {
        lowestSuccessfulFrequency = float.MaxValue;
        highestSuccessfulFrequency = float.MinValue;
        successfulFrequencies.Clear();
    }
}