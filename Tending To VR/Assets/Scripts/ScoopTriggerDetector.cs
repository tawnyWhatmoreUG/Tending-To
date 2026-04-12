using UnityEngine;

/// <summary>
/// Attach this to the scoop GameObject (the one with a collider that enters the flower bed).
/// It forwards trigger events to the FertiliserController.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ScoopTriggerDetector : MonoBehaviour
{
    [Tooltip("Reference to the FertiliserController (on the bucket GameObject)")]
    public FertiliserController fertiliserController;

    private void Start()
    {
        Debug.Log($"ScoopTriggerDetector: Script active on '{gameObject.name}'");
        
        // Check for collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Debug.Log($"ScoopTriggerDetector: Collider found - Type: {col.GetType().Name}, IsTrigger: {col.isTrigger}");
        }
        else
        {
            Debug.LogError("ScoopTriggerDetector: NO COLLIDER FOUND!");
        }
        
        // Check for Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }
        if (rb != null)
        {
            Debug.Log($"ScoopTriggerDetector: Rigidbody found - IsKinematic: {rb.isKinematic}");
        }
        else
        {
            Debug.LogWarning("ScoopTriggerDetector: NO RIGIDBODY FOUND - Triggers may not work! Add a Rigidbody (can be kinematic)");
        }
        
        // Check controller reference
        if (fertiliserController == null)
        {
            Debug.LogError("ScoopTriggerDetector: FertiliserController reference is NULL! Assign it in the Inspector.");
        }
        else
        {
            Debug.Log($"ScoopTriggerDetector: FertiliserController reference is set to '{fertiliserController.gameObject.name}'");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"ScoopTriggerDetector: OnTriggerEnter with '{other.gameObject.name}' (tag: '{other.tag}')");
        
        if (fertiliserController != null)
        {
            fertiliserController.OnScoopTriggerEnter(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"ScoopTriggerDetector: OnTriggerExit with '{other.gameObject.name}' (tag: '{other.tag}')");
        
        if (fertiliserController != null)
        {
            fertiliserController.OnScoopTriggerExit(other);
        }
    }
}
