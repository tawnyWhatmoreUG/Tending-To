using UnityEngine;

public class VeinRadialRevealController : MonoBehaviour
{
    public Material veinMaterial;
    public float expandSpeed = 2.0f;
    private float currentRadius = 0f;
    private bool isActive = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Trigger for testing
        {
            isActive = true;
            // Set the origin to the player's position or a specific hit point
            veinMaterial.SetVector("_Origin", transform.position);
        }

        if (isActive)
        {
            currentRadius += Time.deltaTime * expandSpeed;
            veinMaterial.SetFloat("_RevealRadius", currentRadius);
        }
    }
}