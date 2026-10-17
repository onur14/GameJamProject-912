using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>First call: calm room, ringing hotspot, then a workstation focus.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class DispatchCallFlow : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ICanvasRaycastFilter
{
    public enum CallState { EnteringRoom, Idle, IncomingCall, Focusing, Conversation }
    [Header("Existing room and conversation")]
    [SerializeField] private RectTransform focusRoot;
    [SerializeField] private RectTransform roomArtwork;
    [SerializeField] private CanvasGroup surroundingDim;
    [SerializeField] private RectTransform conversationViewport;
    [SerializeField] private RectTransform conversationPanel;
    [SerializeField] private CanvasGroup conversationGroup;
    [SerializeField] private CanvasGroup ringIndicator;

    [Header("Timing (unscaled seconds)")]
    [SerializeField, Min(0f)] private float firstRingDelay = 1.5f;
    [SerializeField, Min(0.01f)] private float computerFocusDuration = 0.75f;
    [SerializeField, Min(0.01f)] private float conversationFadeDuration = 0.25f;
    [Header("Workstation focus")]
    [SerializeField, Min(1f)] private float focusedScale = 2f;
    [Tooltip("Centre of the baked-in central monitor, in normalized artwork coordinates.")]
    [SerializeField] private Vector2 monitorCentre = new Vector2(0.5f, 0.455f);
    [SerializeField] private Vector2 focusedPosition = Vector2.zero;
    [SerializeField, Range(0, 0.35f)] private float backgroundDimAmount = 0.16f;
    [Header("Phone (optional audio)")]
    [SerializeField] private AudioSource ringAudioSource;
    [SerializeField] private AudioClip ringClip;
    [SerializeField, Range(0, 1)] private float ringVolume = 0.55f;
    [Tooltip("Normalized polygon within the transparent phone rectangle. Excludes the empty desk corners.")]
    [SerializeField] private Vector2[] phoneHitPolygon = {
        new Vector2(0.04f, 0.12f), new Vector2(0.04f, 0.78f),
        new Vector2(0.17f, 0.98f), new Vector2(0.83f, 0.98f),
        new Vector2(0.98f, 0.23f), new Vector2(0.67f, 0.03f)
    };

    public CallState State { get; private set; } = CallState.EnteringRoom;
    public float FirstRingDelay => firstRingDelay;
    public bool IsPaused { get; private set; }
    public void SetPaused(bool paused) { IsPaused = paused; }
    public event System.Action FocusCompleted;
    [Header("Phone visual feedback")]
    [SerializeField, Range(0, 1f)] private float hoverOpacity = .72f;
    [SerializeField, Range(.1f, .2f)] private float hoverDuration = .14f;
    [Tooltip("Only the ring marks rotate: the phone is baked into the artwork.")]
    [SerializeField, Range(0, 4)] private float ringRotation = 2.5f;
    [SerializeField, Range(.7f, 1.6f)] private float ringCycle = 1.25f;
    private PhoneHoverGraphic hoverGraphic;
    private Coroutine hoverRoutine;
    private Quaternion ringRestRotation;
    private Image hitImage;
    private Coroutine ringingRoutine;
    private bool hovered;
    private float focusProgress;
    private Vector2 initialPosition;
    private Vector3 initialScale;
    private Vector2 conversationSize;
    public bool InteractionLocked { get; private set; }
    public void SetInteractionLocked(bool locked)
    {
        InteractionLocked = locked;
        if (locked) SetHover(false);
        if (hitImage) hitImage.raycastTarget = !locked && State == CallState.IncomingCall;
        if (conversationGroup)
        {
            conversationGroup.interactable = !locked && State == CallState.Conversation;
            conversationGroup.blocksRaycasts = !locked && State == CallState.Conversation;
        }
    }

    private void Awake()
    {
        hitImage = GetComponent<Image>();
        hitImage.raycastTarget = false;
        if (!focusRoot || !roomArtwork || !conversationViewport || !conversationPanel || !conversationGroup || !ringIndicator || !surroundingDim)
        {
            Debug.LogError("DispatchCallFlow needs its room, indicator and conversation references assigned.", this);
            enabled = false;
            return;
        }
        initialPosition = focusRoot.anchoredPosition;
        initialScale = focusRoot.localScale;
        conversationSize = conversationPanel.sizeDelta;
        SetConversation(0, false);
        ringIndicator.alpha = 0;
        ringIndicator.blocksRaycasts = false;
        ringRestRotation = ringIndicator.transform.localRotation;
        var hoverObject = new GameObject("PhoneHover", typeof(RectTransform), typeof(CanvasRenderer), typeof(PhoneHoverGraphic));
        hoverObject.transform.SetParent(transform, false);
        hoverObject.transform.SetAsFirstSibling();
        hoverGraphic = hoverObject.GetComponent<PhoneHoverGraphic>();
        hoverGraphic.rectTransform.anchorMin = Vector2.zero;
        hoverGraphic.rectTransform.anchorMax = Vector2.one;
        hoverGraphic.rectTransform.offsetMin = hoverGraphic.rectTransform.offsetMax = Vector2.zero;
        hoverGraphic.Polygon = phoneHitPolygon;
        hoverGraphic.color = new Color(.65f, .79f, .84f, 0);
        hoverGraphic.raycastTarget = false;
        surroundingDim.alpha = 0;
        surroundingDim.blocksRaycasts = false;
        if (ringAudioSource)
        {
            ringAudioSource.playOnAwake = false;
            ringAudioSource.Stop();
        }
        Canvas.ForceUpdateCanvases();
        FitConversation();
    }

    private IEnumerator Start()
    {
        // The scene's Start runs while the persistent scene curtain is still black.
        while (SceneFadeTransition.IsTransitioning) yield return null;
        State = CallState.Idle;
        float remaining = Mathf.Max(0, firstRingDelay);
        while (remaining > 0)
        {
            if (!IsPaused) remaining -= Time.unscaledDeltaTime;
            yield return null;
        }
        while (IsPaused) yield return null;
        if (!enabled || State != CallState.Idle) yield break;
        State = CallState.IncomingCall;
        hitImage.raycastTarget = !InteractionLocked;
        if (ringAudioSource && (ringClip || ringAudioSource.clip))
        {
            if (ringClip) ringAudioSource.clip = ringClip;
            ringAudioSource.volume = ringVolume;
            ringAudioSource.loop = true;
            ringAudioSource.Play();
        }
        ringingRoutine = StartCoroutine(RingVisual());
    }

    private IEnumerator RingVisual()
    {
        float elapsed = 0;
        while (State == CallState.IncomingCall)
        {
            if (IsPaused) { yield return null; continue; }
            // A quiet two-pulse cadence; only the small ring marks move in opacity.
            float phase = (elapsed % ringCycle) / ringCycle * 1.25f;
            bool pulse = phase < 0.19f || (phase > 0.32f && phase < 0.51f);
            ringIndicator.alpha = pulse ? (hovered ? 0.95f : 0.70f) : 0.06f;
            ringIndicator.transform.localRotation = ringRestRotation * Quaternion.Euler(0, 0,
                pulse ? Mathf.Sin(phase * Mathf.PI * 32) * ringRotation : 0);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) AnswerPhone();
    }
    public void OnPointerEnter(PointerEventData eventData) => SetHover(!InteractionLocked && State == CallState.IncomingCall);
    public void OnPointerExit(PointerEventData eventData) => SetHover(false);

    private void SetHover(bool value)
    {
        hovered = value;
        if (!hoverGraphic || !isActiveAndEnabled) return;
        if (hoverRoutine != null) StopCoroutine(hoverRoutine);
        hoverRoutine = StartCoroutine(FadeHover(value ? hoverOpacity : 0));
    }
    private IEnumerator FadeHover(float target)
    {
        Color c = hoverGraphic.color; float start = c.a;
        for (float t = 0; t < hoverDuration; t += Time.unscaledDeltaTime)
        {
            c.a = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, t / hoverDuration));
            hoverGraphic.color = c; yield return null;
        }
        c.a = target; hoverGraphic.color = c; hoverRoutine = null;
    }

    public void AnswerPhone()
    {
        if (IsPaused || InteractionLocked || !isActiveAndEnabled || State != CallState.IncomingCall || SceneFadeTransition.IsTransitioning) return;
        State = CallState.Focusing; // Lock before audio, visuals or coroutines are touched.
        hitImage.raycastTarget = false;
        SetHover(false);
        if (ringingRoutine != null) { StopCoroutine(ringingRoutine); ringingRoutine = null; }
        ringIndicator.alpha = 0;
        ringIndicator.transform.localRotation = ringRestRotation;
        if (ringAudioSource) ringAudioSource.Stop();
        StartCoroutine(FocusComputer());
    }

    private IEnumerator FocusComputer()
    {
        float duration = Mathf.Max(0.01f, computerFocusDuration);
        float fadeTime = Mathf.Clamp(conversationFadeDuration, 0.01f, duration);
        float elapsed = 0;
        while (elapsed < duration)
        {
            if (IsPaused) { yield return null; continue; }
            elapsed += Time.unscaledDeltaTime;
            focusProgress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
            ApplyFocus();
            SetConversation(Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - duration + fadeTime) / fadeTime)), false);
            yield return null;
        }
        focusProgress = 1;
        ApplyFocus();
        SetConversation(1, true);
        State = CallState.Conversation;
        FocusCompleted?.Invoke();
    }

    private void LateUpdate()
    {
        // Recompute from artwork coordinates, including on aspect-ratio changes.
        FitConversation();
        if (State == CallState.Focusing || State == CallState.Conversation) ApplyFocus();
    }

    private void ApplyFocus()
    {
        Rect art = roomArtwork.rect;
        Vector3 point = roomArtwork.TransformPoint(new Vector3(
            Mathf.Lerp(art.xMin, art.xMax, monitorCentre.x),
            Mathf.Lerp(art.yMin, art.yMax, monitorCentre.y), 0));
        Vector2 localMonitor = focusRoot.InverseTransformPoint(point);
        Vector2 target = initialPosition + focusedPosition - localMonitor * focusedScale;
        focusRoot.localScale = Vector3.Lerp(initialScale, initialScale * focusedScale, focusProgress);
        focusRoot.anchoredPosition = Vector2.Lerp(initialPosition, target, focusProgress);
        surroundingDim.alpha = Mathf.Lerp(0, backgroundDimAmount, focusProgress);
    }

    private void FitConversation()
    {
        if (!conversationPanel || conversationSize.x <= 0 || conversationSize.y <= 0) return;
        Vector2 area = conversationViewport.rect.size;
        float scale = Mathf.Min(area.x / conversationSize.x, area.y / conversationSize.y) * 0.96f;
        conversationPanel.localScale = Vector3.one * Mathf.Max(0.001f, scale);
    }

    private void SetConversation(float alpha, bool interactive)
    {
        conversationGroup.alpha = alpha;
        conversationGroup.interactable = interactive;
        conversationGroup.blocksRaycasts = interactive;
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (InteractionLocked || State != CallState.IncomingCall || !isActiveAndEnabled) return false;
        var rect = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, eventCamera, out Vector2 local)) return false;
        Rect bounds = rect.rect;
        if (!bounds.Contains(local)) return false;
        Vector2 p = new Vector2((local.x - bounds.xMin) / bounds.width, (local.y - bounds.yMin) / bounds.height);
        if (phoneHitPolygon == null || phoneHitPolygon.Length < 3) return true;
        bool inside = false;
        for (int i = 0, j = phoneHitPolygon.Length - 1; i < phoneHitPolygon.Length; j = i++)
        {
            Vector2 a = phoneHitPolygon[i], b = phoneHitPolygon[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
        }
        return inside;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (hitImage) hitImage.raycastTarget = false;
        if (ringAudioSource) ringAudioSource.Stop();
        if (ringIndicator) ringIndicator.alpha = 0;
        if (ringIndicator) ringIndicator.transform.localRotation = ringRestRotation;
        if (hoverGraphic) { var c = hoverGraphic.color; c.a = 0; hoverGraphic.color = c; }
    }
}
