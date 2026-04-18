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
    private Coroutine _fadeCoroutine;

    private void OnEnable()
    {
        GameManager.OnStageChanged += OnStageChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStageChanged -= OnStageChanged;
    }

    /// <summary>
    /// Called when the stage changes. If we've reached Relax and it's complete, trigger credits.
    /// </summary>
    private void OnStageChanged(Stage stage)
    {
        // Only trigger once per game session
        if (_hasTriggeredCredits)
            return;

        // Check if we've reached the final Relax stage
        if (stage == Stage.Relax)
        {
            _hasTriggeredCredits = true;
            // Subscribe to interaction complete to know when wine glass is picked up
            StartCoroutine(WaitForRelaxCompletion());
        }
    }

    /// <summary>
    /// Waits for the Relax stage interaction to complete, then triggers the credits transition.
    /// </summary>
    private IEnumerator WaitForRelaxCompletion()
    {
        // Wait a short moment to ensure the stage is fully initialized
        yield return new WaitForSeconds(0.5f);

        // Now fade to black and load credits scene
        yield return StartCoroutine(FadeToBlackAndLoadCredits());
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
