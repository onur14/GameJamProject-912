using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DeskPhotoTransition : MonoBehaviour
{
    [SerializeField] private Image acilMerkezImage;
    [SerializeField] private Image maviGriImage;
    [SerializeField] private float holdDuration = 1.6f;
    [SerializeField] private float fadeDuration = 1.8f;
    [SerializeField] private float zoomAmount = 0.05f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private UnityEvent onFinished;

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlayOnce());
    }

    private IEnumerator PlayOnce()
    {
        SetAlpha(acilMerkezImage, 1f);
        SetAlpha(maviGriImage, 0f);
        acilMerkezImage.rectTransform.localScale = Vector3.one;
        maviGriImage.rectTransform.localScale = Vector3.one;

        Vector3 zoomScale = Vector3.one * (1f + zoomAmount);
        float total = holdDuration + fadeDuration;
        float elapsed = 0f;

        while (elapsed < total)
        {
            elapsed += Time.deltaTime;
            float totalT = Mathf.Clamp01(elapsed / total);
            acilMerkezImage.rectTransform.localScale = Vector3.Lerp(Vector3.one, zoomScale, totalT);

            if (elapsed > holdDuration)
            {
                float fadeT = Mathf.SmoothStep(0f, 1f, (elapsed - holdDuration) / fadeDuration);
                SetAlpha(maviGriImage, fadeT);
            }

            yield return null;
        }

        FreezeOnMaviGri();
        onFinished?.Invoke();
    }

    private void FreezeOnMaviGri()
    {
        SetAlpha(maviGriImage, 1f);
        SetAlpha(acilMerkezImage, 0f);
        acilMerkezImage.rectTransform.localScale = Vector3.one;
        maviGriImage.rectTransform.localScale = Vector3.one;
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
