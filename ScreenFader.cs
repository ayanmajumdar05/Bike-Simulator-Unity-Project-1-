using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;
    private Image fadeImage;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        Instance = this;
        fadeImage = GetComponent<Image>();
    }

    public void FadeOutIn(float duration, System.Action onMidFade)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeSequence(duration, onMidFade));
    }

    private IEnumerator FadeSequence(float duration, System.Action onMidFade)
    {
        float half = duration / 2f;

        // Fade to black
        yield return Fade(0f, 1f, half);

        onMidFade?.Invoke(); // Do reset during black screen

        // Fade back in
        yield return Fade(1f, 0f, half);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0;
        Color color = fadeImage.color;

        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(from, to, elapsed / duration);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, to);
    }
}
