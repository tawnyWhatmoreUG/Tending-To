using UnityEngine;

public class BlueCapsule : MonoBehaviour
{
    [Header("Physics")]
    public float lifetime = 30f; // How long before the pellet disappears
    
    private float spawnTime;
    private Rigidbody rb;

    void Start()
    {
        spawnTime = Time.time;
        rb = GetComponent<Rigidbody>();
        
        // Ensure the rigidbody exists and is set up
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Optional: Add some randomness to the physics for more natural scattering
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None;
        }
    }

    void Update()
    {
        // Destroy the pellet after its lifetime expires
        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Stop spinning on first contact with any surface
        if (rb != null)
        {
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
}
