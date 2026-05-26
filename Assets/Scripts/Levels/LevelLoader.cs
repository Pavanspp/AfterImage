using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    [Header("Fade")]
    [Tooltip("The full-screen black Image used for fading.")]
    public Image fadeImage;

    [Tooltip("Duration of fade out (to black) in seconds.")]
    public float fadeOutDuration = 0.5f;

    [Tooltip("Duration of fade in (from black) in seconds.")]
    public float fadeInDuration  = 0.4f;

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    IEnumerator FadeIn()
    {
        SetAlpha(1f);
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(1f - Mathf.Clamp01(elapsed / fadeInDuration));
            yield return null;
        }

        SetAlpha(0f);
        fadeImage.raycastTarget = false;
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        fadeImage.raycastTarget = true;
        Time.timeScale = 0f;

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Clamp01(elapsed / fadeOutDuration));
            yield return null;
        }

        SetAlpha(1f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }
}