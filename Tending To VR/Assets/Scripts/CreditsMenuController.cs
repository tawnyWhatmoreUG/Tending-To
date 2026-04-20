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
    /// Keeps the black overlay alive through the scene transition to prevent a flash of the default scene.
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

            // Persist the black overlay into the next scene so there is no flash
            DontDestroyOnLoad(fadeCanvasGroup.transform.root.gameObject);
        }

        // Preload the scene without activating it so we control exactly when it appears
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Activate the scene while the screen is still black
        asyncLoad.allowSceneActivation = true;
        yield return asyncLoad;

        // Fade back in now that the new scene is fully active
        if (fadeCanvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / fadeDuration));
                yield return null;
            }
            fadeCanvasGroup.alpha = 0f;
            Destroy(fadeCanvasGroup.transform.root.gameObject);
        }
    }
}
