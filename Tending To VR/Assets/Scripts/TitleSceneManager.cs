using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class TitleSceneManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The name of your main game scene")]
    public string mainSceneName = "MainScene";

    [Header("Fade Settings")]
    [Tooltip("Duration of the fade-in from black when the title scene opens")]
    public float fadeInDuration = 1.5f;
    [Tooltip("Duration of the fade-out to black when transitioning")]
    public float fadeDuration = 1.5f;
    [Tooltip("Seconds to hold on black before loading the next scene")]
    public float holdDuration = 0.3f;
    [Tooltip("Assign the fade overlay CanvasGroup here")]
    public CanvasGroup fadeCanvasGroup;

    [Header("Teleport Trigger")]
    [Tooltip("Assign the TeleportationAnchor that triggers the transition to the main scene")]
    public TeleportationAnchor teleportAnchor;

    [Header("Audio")]
    [Tooltip("Optional sound to play when the transition begins")]
    public AudioClip transitionSound;
    private AudioSource audioSource;

    private bool isTransitioning = false;

    void Start()
    {
        // Fall back to searching children if not assigned in the Inspector
        if (fadeCanvasGroup == null)
            fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();

        if (fadeCanvasGroup == null)
        {
            Debug.LogError("TitleSceneManager: No CanvasGroup found. " +
                           "Add a CanvasGroup component to your fade overlay UI element.");
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (teleportAnchor != null)
            teleportAnchor.teleporting.AddListener(OnTeleportedToAnchor);
        else
            Debug.LogWarning("TitleSceneManager: No TeleportationAnchor assigned.");

        // Start fully black and fade in
        fadeCanvasGroup.alpha = 1f;
        StartCoroutine(FadeIn());
    }

    void OnDestroy()
    {
        if (teleportAnchor != null)
            teleportAnchor.teleporting.RemoveListener(OnTeleportedToAnchor);
    }

    private void OnTeleportedToAnchor(TeleportingEventArgs args)
    {
        if (!isTransitioning)
            StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator FadeIn()
    {
        isTransitioning = true;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
            fadeCanvasGroup.alpha = 1f - t;
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        isTransitioning = false;
    }

    private IEnumerator FadeAndLoadScene()
    {
        isTransitioning = true;

        if (transitionSound != null)
            audioSource.PlayOneShot(transitionSound);

        // Begin loading in the background immediately without activating yet
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainSceneName);
        asyncLoad.allowSceneActivation = false;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration);
            fadeCanvasGroup.alpha = t;
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(holdDuration);

        // Wait until the scene is fully ready (Unity caps async progress at 0.9 before activation)
        while (asyncLoad.progress < 0.9f)
            yield return null;

        asyncLoad.allowSceneActivation = true;
    }
}