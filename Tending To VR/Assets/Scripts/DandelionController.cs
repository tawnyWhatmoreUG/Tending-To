using System;
using UnityEngine;
using UnityEngine.UI;

public class DandelionController : MonoBehaviour
{
    /// <summary>
    /// Fired when all dandelions are cleared.
    /// </summary>
    public event Action OnPathCleared;

    public Slider progressBar;
    public MeshRenderer[] dandelionRenderers;
    public WeedkillerController weedkillScript;
    public AudioSource completionAudio; // Assign the canvas AudioSource here
    
    [Range(0, 1)] private float progress = 0f;
    public float spraySpeed = 0.2f;

    void Update() {
// Only run if the nozzle is actually in the player's hand
    if (weedkillScript != null && weedkillScript.IsCurrentlyEquipped) {
        
        // Only progress if particles are spraying
        if (weedkillScript.mistParticles.isEmitting) {
            float distance = Vector3.Distance(weedkillScript.mistParticles.transform.position, transform.position);
            
            // Only progress if the player is actually pointing at the dandelions
            if (distance < 2.5f) { 
                UpdateProgress();
            }
        }
    }
    }

    void UpdateProgress() {
        if (progress < 1f) {
            progress += spraySpeed * Time.deltaTime;
            progressBar.value = progress;
            progressBar.gameObject.SetActive(true);

            // Update Material (Assumes "_Smoothness" or "_Metallic" makes it look wet)
            foreach (var r in dandelionRenderers) {
                r.material.SetFloat("_Smoothness", progress); 
            }

            if (progress >= 1f) {
                CompleteTask();
            }
        }
    }

    void CompleteTask() {
        // Play completion sound
        if (completionAudio != null) {
            completionAudio.Play();
        }
        
        progressBar.gameObject.SetActive(false);
        weedkillScript.ResetTool();
        progress = 0; // Reset for next time if needed

        // Notify listeners that the path is cleared
        OnPathCleared?.Invoke();
    }
}