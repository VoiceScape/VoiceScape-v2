using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class SplineCollectableSpawner : MonoBehaviour
{
    [Header("Spline Settings")]
    // Reference to the SplineContainer that holds your spline data.
    public SplineContainer splineContainer;
    public PickupManagerForSpline pickupManagerForSpline;

    [Header("Collectable Settings")]
    // The collectable prefab that will be spawned along the spline.
    public GameObject collectablePrefab;
    // How many collectibles to spawn along the spline (not including the endpoints).
    public int numberOfCollectables = 10;
    // Optional: Offset to adjust the position of collectibles relative to the spline.
    public float collectableOffset = 1f;

    public List<GameObject> collectables = new List<GameObject>();
    [Header("Spawn Settings")]
    // If true, the collectibles will be spawned in the Start() method.
    public bool spawnOnStart = true;
    public Vector3 targetScale;

    public int currentCollectable = 0;

    /// <summary>
    /// Spawns collectible objects evenly along the spline,
    /// avoiding the very first (t=0) and last (t=1) nodes.
    /// </summary>
    public void SpawnCollectables()
    {
        // Validate required references.
        if (splineContainer == null || splineContainer.Spline == null)
        {
            Debug.LogWarning("SplineCollectableSpawner: SplineContainer or its Spline is not assigned.");
            return;
        }

        if (collectablePrefab == null)
        {
            Debug.LogWarning("SplineCollectableSpawner: Collectable Prefab is not assigned.");
            return;
        }

        // Spawn collectibles at evenly spaced intervals along the spline,
        // avoiding the endpoints by using t = (i+1)/(numberOfCollectables+1).
        for (int i = 0; i < numberOfCollectables; i++)
        {
            // Calculate a normalized t value such that it never equals 0 or 1.
            float t = (float)(i + 1) / (numberOfCollectables + 1);

            // Evaluate the position on the spline and add an optional offset.
            Vector3 pos = splineContainer.Spline.EvaluatePosition(t);
            
            //+ PitchHeightCalculator.GetHeightForFrequency(pickupManagerForSpline.GetSequenceFrequency(i))
            
            var spawnPosition = new Vector3(pos.x,
                pos.y + PitchHeightCalculator.GetPositionForFrequency(pickupManagerForSpline.GetSequenceFrequency(i)).y,
                pos.z);

            // Optionally, set the rotation according to the spline's tangent.
            Vector3 tangent = splineContainer.Spline.EvaluateTangent(t);
            
            Quaternion spawnRotation = Quaternion.identity;
            
            // if (tangent != Vector3.zero)
            // {
            //     spawnRotation = Quaternion.LookRotation(tangent);
            // }

            // Instantiate the collectible.
            // Optionally, set the spawner as the parent to keep the hierarchy clean.
            var go = Instantiate(collectablePrefab, spawnPosition, spawnRotation, transform);
            go.transform.localScale = targetScale;
            collectables.Add(go);

            // if (i != 0)
            // {
            //     CloseCollectable(go);
            // }
            // else
            // {
            //     OpenCollectable(go);
            // }
        }
    }

    public void OpenCollectable(GameObject go)
    {
        go.SetActive(true);
    }

    
    public void CloseCollectable(GameObject go)
    {
        go.SetActive(false);
    }

    public GameObject GetCurrentCollectable()
    {
        if (currentCollectable >= collectables.Count) return null;
        
        if (collectables[currentCollectable] == null) return null;

        if (!collectables[currentCollectable].activeInHierarchy)
        {
            var go = collectables[currentCollectable];
            go.SetActive(true);
        }
            
        return collectables[currentCollectable];
    }

    public GameObject GetNextCollectable()
    {
        if (currentCollectable >= collectables.Count) return null;
        
        if (collectables[currentCollectable + 1] == null) return null;
        
        if (!collectables[currentCollectable].activeInHierarchy)
        {
            var go = collectables[currentCollectable];
            go.SetActive(true);
        }
        
        return collectables[currentCollectable + 1];
    }
}
