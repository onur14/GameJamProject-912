using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Presentation only. Decision truth stays in OyunAkisiOnur and scenario data.
public sealed class DispatchVisualFeedback : MonoBehaviour
{
    [SerializeField] private OyunAkisiOnur conversation;
    [SerializeField] private DispatchCallFlow phone;
    [SerializeField] private RectTransform conversationPanel;
    [SerializeField, Range(0, 4)] private float shakePixels = 2;
    [SerializeField, Range(.25f, .45f)] private float wrongDuration = .32f;
    [SerializeField, Range(.08f, .18f)] private float wrongOpacity = .10f;
    [SerializeField, Range(.2f, .35f)] private float correctDuration = .26f;
    [SerializeField, Range(.02f, .12f)] private float coolOpacity = .065f;
    [SerializeField, Range(.2f, .35f)] private float activationDuration = .28f;
    [SerializeField, Range(0,.025f)] private float monitorLuminance = .012f;
    [SerializeField, Range(4,12)] private float monitorCycle = 7.5f;
    private Image warning, confirmation;
    private Image monitorGlow;
    private Coroutine pulse;
    private Coroutine ambience;
    private Vector2 restPosition;
    private bool hasRestPosition;
    public int DecisionPulseCount { get; private set; }

    private void Awake()
    {
        warning = Overlay("DecisionWarning", transform, new Color(.46f, .13f, .10f, 0));
        confirmation = Overlay("MonitorActivation", conversationPanel, new Color(.65f, .83f, .91f, 0));
        monitorGlow=Overlay("MonitorLuminance",conversationPanel.parent,new Color(.55f,.74f,.83f,0));
        monitorGlow.transform.SetAsFirstSibling();
    }
    private static Image Overlay(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
        var r = image.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        return image;
    }
    private void OnEnable()
    {
        if (conversation) conversation.DecisionResolved += Decision;
        if (phone) phone.FocusCompleted += Activate;
        ambience=StartCoroutine(MonitorIdle());
    }
    private void OnDisable()
    {
        if (conversation) conversation.DecisionResolved -= Decision;
        if (phone) phone.FocusCompleted -= Activate;
        if (pulse != null) StopCoroutine(pulse);
        if (ambience != null) StopCoroutine(ambience);ambience=null;Alpha(monitorGlow,0);
        Restore();
    }
    private IEnumerator MonitorIdle()
    {
        var interval=new WaitForSecondsRealtime(.1f);
        while(true)
        {
            Alpha(monitorGlow,monitorLuminance*(.65f+.35f*Mathf.Sin(Time.unscaledTime/monitorCycle*Mathf.PI*2)));
            yield return interval;
        }
    }
    private void Activate() => Begin(false, activationDuration);
    private void Decision(bool correct) { DecisionPulseCount++; Begin(!correct, correct ? correctDuration : wrongDuration); }
    private void Begin(bool wrong, float duration)
    {
        if (pulse != null) StopCoroutine(pulse);
        Restore();
        restPosition = conversationPanel.anchoredPosition; hasRestPosition = true;
        pulse = StartCoroutine(Animate(wrong, duration));
    }
    private IEnumerator Animate(bool wrong, float duration)
    {
        var image = wrong ? warning : confirmation;
        image.transform.SetAsLastSibling();
        float scale = Mathf.Max(.001f, conversationPanel.lossyScale.x);
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            float p = t / duration;
            float envelope = p < .18f ? Mathf.SmoothStep(0, 1, p / .18f) : 1 - Mathf.SmoothStep(0, 1, (p - .18f) / .82f);
            Alpha(image, envelope * (wrong ? wrongOpacity : coolOpacity));
            if (wrong)
            {
                // Quantize in screen pixels; never touch the camera or room transform.
                float x = Mathf.Round(Mathf.Sin(p * Mathf.PI * 8) * shakePixels * (1 - p));
                float y = Mathf.Round(Mathf.Sin(p * Mathf.PI * 6) * shakePixels * .5f * (1 - p));
                conversationPanel.anchoredPosition = restPosition + new Vector2(x, y) / scale;
            }
            yield return null;
        }
        Restore(); pulse = null;
    }
    private static void Alpha(Image image, float alpha) { if (!image) return; var c = image.color; c.a = alpha; image.color = c; }
    private void Restore()
    {
        Alpha(warning, 0); Alpha(confirmation, 0);
        if (hasRestPosition && conversationPanel) conversationPanel.anchoredPosition = restPosition;
        hasRestPosition = false;
    }
}
