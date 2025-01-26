using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PickupCollector3POV : MonoBehaviour
{
    private PickupManager3POV2 pickupManager;
    private SphereCollider triggerCollider;
    
    [SerializeField] private float collectionRadius = 0.5f;
    [SerializeField] private bool showDebugSphere = true;

    private void Start()
    {
        pickupManager = FindObjectOfType<PickupManager3POV2>();
        if (pickupManager == null)
        {
            Debug.LogError("PickupCollector3POV: Could not find PickupManager3POV in scene!");
            enabled = false;
            return;
        }

        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = collectionRadius;

        Debug.Log($"PickupCollector3POV initialized with radius {collectionRadius}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"PickupCollector3POV hit: {other.gameObject.name}");
        
        // Check if it's an active pickup
        if (pickupManager.IsActivePickup(other.gameObject))
        {
            Debug.Log("Valid pickup detected - collecting!");
            pickupManager.OnPickupCollected(other.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugSphere) return;
        
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, collectionRadius);
    }
}