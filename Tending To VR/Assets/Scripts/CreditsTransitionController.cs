using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages the transition from the game to the Credits scene.
/// 
/// Triggers when the Relax stage is complete (after wine glass interaction).
/// Fades to black and loads CreditsScene after fade completes.
/// </summary>
public class CreditsTransitionController : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 3f;

    private bool _hasTriggeredCredits = false;

    private void OnEnable()
    {
        WineGlassInteractable.OnWineGlassHeld += TriggerCredits;
    }

    private void OnDisable()
    {
        WineGlassInteractable.OnWineGlassHeld -= TriggerCredits;
    }

    /// <summary>
    /// Fades the screen to black and loads the CreditsScene.
    /// </summary>
    private IEnumerator FadeToBlackAndLoadCredits()
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogError("[CreditsTransitionController] fadeCanvasGroup is not assigned. Cannot fade to black.");
            yield break;
        }

        // Fade to black
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;

        // Load Credits scene
        SceneManager.LoadScene("CreditsScene");
    }

    /// <summary>
    /// Public method to manually trigger credits (for debugging or other edge cases).
    /// </summary>
    public void TriggerCredits()
    {
        if (!_hasTriggeredCredits)
        {
            _hasTriggeredCredits = true;
            StartCoroutine(FadeToBlackAndLoadCredits());
        }
    }
}
