using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Optional menu controller for Credits scene.
/// Provides manual buttons to return to title scene or retry.
/// 
/// Can be expanded for additional menu functionality.
/// </summary>
public class CreditsMenuController : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 2f;

    /// <summary>
    /// Called by a "Return to Title" button - fades out and loads TitleScene.
    /// </summary>
    public void ReturnToTitle()
    {
        StartCoroutine(FadeAndLoadScene("TitleScene"));
    }

    /// <summary>
    /// Called by a "Retry" button - fades out and reloads FinalGardenScene.
    /// </summary>
    public void RetryGame()
    {
        StartCoroutine(FadeAndLoadScene("FinalGardenScene"));
    }

    /// <summary>
    /// Generic fade-and-load coroutine.
    /// </summary>
    private System.Collections.IEnumerator FadeAndLoadScene(string sceneName)
    {
        if (fadeCanvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        SceneManager.LoadScene(sceneName);
    }
}
