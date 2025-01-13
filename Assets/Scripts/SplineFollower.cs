using UnityEngine;
using UnityEngine.Splines;

public class SplineFollower : MonoBehaviour
{
    [Header("Spline Settings")]
    // Reference to the SplineContainer that holds your spline data.
    public SplineContainer splineContainer;

    [Header("Movement Settings")]
    // Duration (in seconds) for the object to travel the complete spline.
    public float duration = 5f;
    
    [Header("Line Renderer Settings")]
    // Reference to the LineRenderer component for visualizing the spline.
    public LineRenderer lineRenderer;
    // How many points to sample along the spline for the line.
    public int samplePoints = 50;

    // Private variables to manage movement.
    private float elapsedTime = 0f;
    private bool isMoving = false;  // Movement will not start until StartMovement() is called.

    void Start()
    {
        // Draw the spline path at the start of the game if a LineRenderer is assigned.
        DrawSpline();
        transform.position = splineContainer.Spline.EvaluatePosition(0);
    }

    void Update()
    {
        // If needed, you can also trigger the movement via keyboard here (e.g., Space key).
        // if (Input.GetKeyDown(KeyCode.Space)) StartMovement();
        
        // Only process movement when isMoving is true.
        if (!isMoving)
            return;

        // Ensure that the SplineContainer and its spline are properly assigned.
        if (splineContainer == null || splineContainer.Spline == null)
        {
            Debug.LogWarning("SplineContainer or its Spline is not assigned.");
            return;
        }
        
        // Update elapsed time and compute the normalized parameter t (from 0 to 1).
        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / duration);

        // Evaluate the spline at the current t value.
        Vector3 position = splineContainer.Spline.EvaluatePosition(t);
        transform.position = position;

        // Optionally update rotation based on the spline's tangent.
        Vector3 tangent = splineContainer.Spline.EvaluateTangent(t);
        if (tangent != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(tangent);
        }

        // Stop movement at the end of the spline or loop as desired.
        if (t >= 1f)
        {
            // To loop the movement, uncomment the following line:
            // elapsedTime = 0f;
            
            // Otherwise, stop moving:
            isMoving = false;
        }
    }

    /// <summary>
    /// Call this method to begin moving the GameObject along the spline.
    /// You can hook this up to a UI Button onClick event, or call it from another script.
    /// </summary>
    public void StartMovement()
    {
        // Reset timer and start the movement.
        elapsedTime = 0f;
        isMoving = true;
    }

    /// <summary>
    /// Draws the spline path using the LineRenderer component.
    /// This method samples the spline at a set number of points and assigns them to the LineRenderer.
    /// </summary>
    private void DrawSpline()
    {
        if (splineContainer == null || splineContainer.Spline == null || lineRenderer == null)
        {
            Debug.LogWarning("Missing SplineContainer, its Spline, or LineRenderer.");
            return;
        }

        // Set the number of points in the LineRenderer.
        lineRenderer.positionCount = samplePoints;
        Vector3[] positions = new Vector3[samplePoints];

        // Sample the spline at evenly spaced intervals.
        for (int i = 0; i < samplePoints; i++)
        {
            float t = (float)i / (samplePoints - 1);
            positions[i] = splineContainer.Spline.EvaluatePosition(t);
        }

        // Assign the positions to the LineRenderer.
        lineRenderer.SetPositions(positions);
    }
}
