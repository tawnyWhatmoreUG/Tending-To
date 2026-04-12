using System;
using UnityEngine;
using UnityEngine.UI;

public class SlugPelletBedController : MonoBehaviour
{
    /// <summary>
    /// Fired when this bed is complete (progress reaches 100%).
    /// </summary>
    public event Action OnBedComplete;

    [Header("References")]
    public Slider progressBar;
    public SlugPelletController slugPelletController;
    public AudioSource completionAudio;
    
    [Range(0, 1)] private float progress = 0f;
    public float spraySpeed = 0.15f;

    void Update()
    {
        // Only run if the bottle is equipped and actively spraying
        if (slugPelletController != null && slugPelletController.IsCurrentlyEquipped)
        {
            if (slugPelletController.IsSpraying)
            {
                if (IsPelletInBedArea())
                {
                    UpdateProgress();
                }
            }
        }
    }

    bool IsPelletInBedArea()
    {
        Transform sprayAnchor = slugPelletController.sprayAnchor;
        if (sprayAnchor == null) return false;

        float distanceToBed = Vector3.Distance(sprayAnchor.position, transform.position);
        return distanceToBed < 3f;
    }

    void UpdateProgress()
    {
        if (progress < 1f)
        {
            progress += spraySpeed * Time.deltaTime;
            progressBar.value = progress;
            progressBar.gameObject.SetActive(true);

            if (progress >= 1f)
            {
                CompleteTask();
            }
        }
    }

    void CompleteTask()
    {
        // Play completion sound
        if (completionAudio != null)
        {
            completionAudio.Play();
        }
        
        progressBar.gameObject.SetActive(false);
        slugPelletController.ResetTool();
        progress = 0; // Reset for next time

        // Notify listeners that this bed is complete
        OnBedComplete?.Invoke();
    }
}
