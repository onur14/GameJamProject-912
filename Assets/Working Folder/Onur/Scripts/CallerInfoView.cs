using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class CallerInfoView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text locationLabel;
    [SerializeField] private TMP_Text emergencyLabel;
    [SerializeField] private TMP_Text summaryLabel;

    private Coroutine arrival;

    public void Show(DispatchScenarioData scenario)
    {
        if (scenario == null) { Clear(); return; }
        Set(nameLabel, scenario.callerName, "Arayan: ");
        Set(locationLabel, scenario.callerLocation, "Konum: ");
        Set(emergencyLabel, scenario.emergencyType, "Olay: ");
        Set(summaryLabel, scenario.situationSummary, "Durum: ");
        gameObject.SetActive(true);
        if (arrival != null) StopCoroutine(arrival);
        arrival = StartCoroutine(AnimateArrival());
    }

    public void Clear()
    {
        if (arrival != null) StopCoroutine(arrival);
        arrival = null;
        gameObject.SetActive(false);
    }

    private IEnumerator AnimateArrival()
    {
        var rect = (RectTransform)transform;
        var fade = GetComponent<CanvasGroup>();
        if (!fade) fade = gameObject.AddComponent<CanvasGroup>();
        fade.blocksRaycasts = false;
        for (float t = 0; t < .28f; t += Time.unscaledDeltaTime)
        {
            float p = Mathf.Clamp01(t / .28f); fade.alpha = p;
            rect.anchoredPosition = Vector2.left * Mathf.Round(18 * Mathf.Pow(1 - p, 3));
            yield return null;
        }
        rect.anchoredPosition = Vector2.zero; fade.alpha = 1; arrival = null;
    }

    private static void Set(TMP_Text label, string value, string prefix)
    {
        if (label == null) return;
        label.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
        label.text = string.IsNullOrWhiteSpace(value) ? string.Empty : prefix + value;
    }
}
