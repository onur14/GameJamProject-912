using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DeskReturnDarkenAnimation : MonoBehaviour
{
    [SerializeField] private Image acilMerkezImage;
    [SerializeField] private Image maviGriImage;
    [SerializeField] private Image screenDarken;
    [SerializeField] private float zoomOutDuration = 1.2f;
    [SerializeField] private float zoomAmount = 0.08f;
    [SerializeField] private float darkenDuration = 1.4f;
    [SerializeField] private float darkenTargetAlpha = 1f;
    [SerializeField] private bool playOnStart = false;

    private void Start()
    {
        if (screenDarken != null)
            SetAlpha(screenDarken, 0f);

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
        Vector3 zoomedIn = Vector3.one * (1f + zoomAmount);

        SetAlpha(acilMerkezImage, 1f);
        SetAlpha(maviGriImage, 1f);
        SetAlpha(screenDarken, 0f);
        acilMerkezImage.rectTransform.localScale = zoomedIn;
        maviGriImage.rectTransform.localScale = zoomedIn;

        float elapsed = 0f;
        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / zoomOutDuration));
            Vector3 scale = Vector3.Lerp(zoomedIn, Vector3.one, t);
            acilMerkezImage.rectTransform.localScale = scale;
            maviGriImage.rectTransform.localScale = scale;
            SetAlpha(maviGriImage, 1f - t);
            yield return null;
        }

        SetAlpha(maviGriImage, 0f);
        SetAlpha(acilMerkezImage, 1f);
        acilMerkezImage.rectTransform.localScale = Vector3.one;
        maviGriImage.rectTransform.localScale = Vector3.one;

        elapsed = 0f;
        while (elapsed < darkenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / darkenDuration));
            SetAlpha(screenDarken, t * darkenTargetAlpha);
            yield return null;
        }

        SetAlpha(screenDarken, darkenTargetAlpha);
        SetAlpha(acilMerkezImage, 1f);
        SetAlpha(maviGriImage, 0f);
        acilMerkezImage.rectTransform.localScale = Vector3.one;
        maviGriImage.rectTransform.localScale = Vector3.one;
    }

    private static void SetAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
