using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PickupCollector3POV : MonoBehaviour
{
    private PickupManager3POV pickupManager;
    private SphereCollider triggerCollider;
    
    [SerializeField] private float collectionRadius = 0.5f;
    [SerializeField] private bool showDebugSphere = true;

    private void Start()
    {
        pickupManager = FindObjectOfType<PickupManager3POV>();
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
        Debug.Log($"OnTriggerEnter with {other.gameObject.name}");
        
        if (pickupManager == null)
        {
            Debug.LogError("PickupCollector3POV: pickupManager is null during collision!");
            return;
        }

        if (pickupManager.IsActivePickup(other.gameObject))
        {
            Debug.Log($"Valid pickup detected - collecting {other.gameObject.name}");
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