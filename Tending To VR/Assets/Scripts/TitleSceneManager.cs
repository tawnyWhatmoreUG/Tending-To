using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TitleSceneManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The name of your main game scene")]
    public string mainSceneName = "MainScene";

    [Header("Fade Settings")]
    [Tooltip("Duration of the fade-out in seconds")]
    public float fadeDuration = 1.5f;

    private CanvasGroup fadeCanvasGroup;
    private bool isTransitioning = false;

    void Start()
    {
        // Find or create the CanvasGroup used for fading
        fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();

        if (fadeCanvasGroup == null)
        {
            Debug.LogError("TitleSceneManager: No CanvasGroup found. " +
                           "Add a CanvasGroup component to your fade overlay UI element.");
            return;
        }

        // Start fully visible
        fadeCanvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (isTransitioning) return;

        // Detect any button press across keyboard, gamepad, or XR controllers
        if (AnyButtonPressed())
        {
            StartCoroutine(FadeAndLoadScene());
        }
    }

    private bool AnyButtonPressed()
    {
        // Keyboard - any key
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        // Gamepad - any button
        if (Gamepad.current != null)
        {
            foreach (var control in Gamepad.current.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl btn
                    && btn.wasPressedThisFrame)
                    return true;
            }
        }

        // XR Controllers - primary and secondary buttons, triggers, grips
        #if UNITY_XR
        var leftHand = InputSystem.GetDevice<UnityEngine.XR.OpenXR.Input.OpenXRController>(
            CommonUsages.LeftHand);
        var rightHand = InputSystem.GetDevice<UnityEngine.XR.OpenXR.Input.OpenXRController>(
            CommonUsages.RightHand);

        if (leftHand != null || rightHand != null)
        {
            foreach (var device in InputSystem.devices)
            {
                foreach (var control in device.allControls)
                {
                    if (control is UnityEngine.InputSystem.Controls.ButtonControl btn
                        && btn.wasPressedThisFrame)
                        return true;
                }
            }
        }
        #endif

        return false;
    }

    private IEnumerator FadeAndLoadScene()
    {
        isTransitioning = true;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;

        SceneManager.LoadScene(mainSceneName);
    }
}