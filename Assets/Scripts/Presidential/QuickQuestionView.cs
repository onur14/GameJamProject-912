using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class QuickQuestionView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip arrivalClip;
    [SerializeField] private AudioClip warningTickClip;

    [Header("Visual Styling")]
    [SerializeField] private Color frameColor = new Color(.12f, .18f, .22f, .98f);
    [SerializeField] private Color innerColor = new Color(.035f, .055f, .075f, .98f);
    [SerializeField] private Color headerBarColor = new Color(.08f, .13f, .17f, 1f);
    [SerializeField] private Color amberAccent = new Color(.96f, .69f, .30f, 1f);
    [SerializeField] private Color coolCyan = new Color(.77f, .90f, .96f, 1f);
    [SerializeField] private Color paperText = new Color(.85f, .91f, .88f, 1f);
    [SerializeField] private Color buttonBgColor = new Color(.10f, .16f, .20f, 1f);
    [SerializeField] private Color timerCoolColor = new Color(.35f, .65f, .85f, 1f);
    [SerializeField] private Color timerAmberColor = new Color(.90f, .65f, .25f, 1f);
    [SerializeField] private Color timerRedColor = new Color(.85f, .30f, .25f, 1f);

    [Header("Animation")]
    [SerializeField, Range(.15f, .4f)] private float entranceDuration = .25f;
    [SerializeField, Range(.15f, .4f)] private float exitDuration = .25f;
    [SerializeField, Range(.4f, 1.2f)] private float answerFeedbackDuration = .65f;
    [SerializeField, Range(.5f, 1.5f)] private float timeoutFeedbackDuration = .85f;

    // Runtime created components
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Image borderPulseImage;
    private TMP_Text categoryLabel;
    private TMP_Text statusTag;
    private TMP_Text questionBody;
    private TMP_Text feedbackLabel;
    private Button buttonA;
    private TMP_Text labelA;
    private Button buttonB;
    private TMP_Text labelB;
    private Image timerBarFill;
    private RectTransform timerBarRect;

    // State
    private QuickQuestionConfig currentConfig;
    private DispatchResources currentResources;
    private Func<int, bool> decisionCallback;
    private Func<int, bool> canChoose;
    private bool visible;
    private Action timeoutCallback;
    private Coroutine activeRoutine;
    private bool isHandlingResponse;
    private float questionDuration;
    private float elapsedResponseTime;
    public bool IsPaused { get; private set; }
    public void SetPaused(bool paused) { IsPaused = paused; }
    private Vector2 restingPosition;

    public bool IsActive => visible;
    public bool Ready => visible && canvasGroup.interactable && !isHandlingResponse && !IsPaused;
    public float ElapsedResponseTime => Mathf.Clamp(elapsedResponseTime, 0, questionDuration);
    public float RemainingFraction => timerBarFill ? timerBarFill.rectTransform.anchorMax.x : 0;
    public Button ButtonA => buttonA;
    public Button ButtonB => buttonB;
    public TMP_Text QuestionBody => questionBody;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        panelRect = GetComponent<RectTransform>();
        restingPosition = panelRect.anchoredPosition;
        BuildUI();
        HideImmediate();
    }

    public void HideImmediate()
    {
        if (activeRoutine != null) { StopCoroutine(activeRoutine); activeRoutine = null; }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        isHandlingResponse = false;
        if (panelRect) panelRect.anchoredPosition = restingPosition;
        if (borderPulseImage) borderPulseImage.color = Color.clear;
        IsPaused = false;
        if(currentResources)currentResources.Changed-=UpdateButtonAvailability;
        visible = false;
    }

    private void OnDisable(){HideImmediate();}
    private void BuildUI()
    {
        if (categoryLabel != null) return;

        // Base frame background
        var baseImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        baseImage.color = frameColor;
        baseImage.raycastTarget = true; // Catches clicks within its own bounds only

        // Border pulse outline image
        var pulseGo = new GameObject("BorderPulse", typeof(RectTransform), typeof(Image));
        pulseGo.transform.SetParent(transform, false);
        var pulseRect = (RectTransform)pulseGo.transform;
        pulseRect.anchorMin = Vector2.zero;
        pulseRect.anchorMax = Vector2.one;
        pulseRect.offsetMin = new Vector2(-2, -2);
        pulseRect.offsetMax = new Vector2(2, 2);
        borderPulseImage = pulseGo.GetComponent<Image>();
        borderPulseImage.color = new Color(amberAccent.r, amberAccent.g, amberAccent.b, 0f);
        borderPulseImage.raycastTarget = false;

        // Inner display area
        var innerGo = new GameObject("InnerDisplay", typeof(RectTransform), typeof(Image));
        innerGo.transform.SetParent(transform, false);
        var innerRect = (RectTransform)innerGo.transform;
        innerRect.anchorMin = new Vector2(0.015f, 0.02f);
        innerRect.anchorMax = new Vector2(0.985f, 0.98f);
        innerRect.offsetMin = innerRect.offsetMax = Vector2.zero;
        var innerImage = innerGo.GetComponent<Image>();
        innerImage.color = innerColor;
        innerImage.raycastTarget = false;

        // Header background bar
        var headerGo = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
        headerGo.transform.SetParent(innerGo.transform, false);
        var headerRect = (RectTransform)headerGo.transform;
        headerRect.anchorMin = new Vector2(0f, 0.84f);
        headerRect.anchorMax = Vector2.one;
        headerRect.offsetMin = headerRect.offsetMax = Vector2.zero;
        var headerImage = headerGo.GetComponent<Image>();
        headerImage.color = headerBarColor;
        headerImage.raycastTarget = false;

        // Category title (e.g. "SAHA TALEBİ")
        categoryLabel = CreateText("CategoryLabel", headerGo.transform, new Vector2(0.04f, 0.1f), new Vector2(0.70f, 0.9f), 17, amberAccent);
        categoryLabel.fontStyle = FontStyles.Bold;
        DispatchTextStyle.Apply(categoryLabel, true);
        categoryLabel.alignment = TextAlignmentOptions.MidlineLeft;

        // Status tag (e.g. "[ ! ACİL ]")
        statusTag = CreateText("StatusTag", headerGo.transform, new Vector2(0.68f, 0.1f), new Vector2(0.96f, 0.9f), 14, coolCyan);
        statusTag.alignment = TextAlignmentOptions.MidlineRight;
        statusTag.text = "[ ! ]";
        categoryLabel.enableAutoSizing=true;categoryLabel.fontSizeMin=13;categoryLabel.fontSizeMax=17;

        // Question content text
        questionBody = CreateText("QuestionText", innerGo.transform, new Vector2(0.05f, 0.51f), new Vector2(0.95f, 0.81f), 19, paperText);
        questionBody.alignment = TextAlignmentOptions.TopLeft;
        questionBody.textWrappingMode = TextWrappingModes.Normal;

        // Feedback label (e.g. "GÖNDERİLDİ", "TALEP REDDEDİLDİ", "YANITLANMADI")
        feedbackLabel = CreateText("FeedbackLabel", innerGo.transform, new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.40f), 22, amberAccent);
        feedbackLabel.alignment = TextAlignmentOptions.Center;
        feedbackLabel.fontStyle = FontStyles.Bold;
        DispatchTextStyle.Apply(feedbackLabel, true);
        feedbackLabel.alpha = 0f;

        // Choice Buttons Area
        var buttonsContainer = new GameObject("ButtonsContainer", typeof(RectTransform));
        buttonsContainer.transform.SetParent(innerGo.transform, false);
        var bContainerRect = (RectTransform)buttonsContainer.transform;
        bContainerRect.anchorMin = new Vector2(0.04f, 0.14f);
        bContainerRect.anchorMax = new Vector2(0.96f, 0.49f);
        bContainerRect.offsetMin = bContainerRect.offsetMax = Vector2.zero;

        // Button A (Left)
        buttonA = CreateButton("ChoiceAButton", buttonsContainer.transform, new Vector2(0f, .54f), new Vector2(1f, 1f), out labelA);
        buttonA.onClick.AddListener(() => OnChoiceClicked(0));

        // Button B (Right)
        buttonB = CreateButton("ChoiceBButton", buttonsContainer.transform, new Vector2(0f, 0f), new Vector2(1f, .46f), out labelB);
        buttonB.onClick.AddListener(() => OnChoiceClicked(1));

        // Timer Progress Bar Container (at bottom)
        var timerBarGo = new GameObject("TimerBar", typeof(RectTransform), typeof(Image));
        timerBarGo.transform.SetParent(innerGo.transform, false);
        timerBarRect = (RectTransform)timerBarGo.transform;
        timerBarRect.anchorMin = new Vector2(0.04f, 0.04f);
        timerBarRect.anchorMax = new Vector2(0.96f, 0.09f);
        timerBarRect.offsetMin = timerBarRect.offsetMax = Vector2.zero;
        var timerBg = timerBarGo.GetComponent<Image>();
        timerBg.color = new Color(.06f, .09f, .12f, 1f);
        timerBg.raycastTarget = false;

        // Timer Bar Fill
        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(timerBarGo.transform, false);
        var fillRect = (RectTransform)fillGo.transform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        fillRect.pivot = new Vector2(0f, 0.5f);
        timerBarFill = fillGo.GetComponent<Image>();
        timerBarFill.color = timerCoolColor;
        timerBarFill.raycastTarget = false;
    }

    private TMP_Text CreateText(string name, Transform parent, Vector2 min, Vector2 max, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        DispatchTextStyle.Apply(text);text.lineSpacing=2;
        return text;
    }

    private Button CreateButton(string name, Transform parent, Vector2 min, Vector2 max, out TMP_Text label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = buttonBgColor;
        img.raycastTarget = true;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        colors.pressedColor = new Color(.72f, .77f, .80f, 1f);
        colors.disabledColor = new Color(.35f, .38f, .40f, .8f);
        btn.colors = colors;

        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        label = CreateText("Label", go.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), 16, paperText);
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12;
        label.fontSizeMax = 18;
        return btn;
    }

    public void Show(QuickQuestionConfig config, DispatchResources resources, Func<int,bool> onDecision, Action onTimeout, Func<int,bool> availability)
    {
        BuildUI();
        if (activeRoutine != null) StopCoroutine(activeRoutine);

        currentConfig = config;
        if(currentResources)currentResources.Changed-=UpdateButtonAvailability;
        currentResources = resources;
        if(currentResources)currentResources.Changed+=UpdateButtonAvailability;
        canChoose=availability;
        decisionCallback = onDecision;
        timeoutCallback = onTimeout;
        isHandlingResponse = false;
        questionDuration = Mathf.Max(1f, config.timerDuration);
        elapsedResponseTime = 0;
        timerBarFill.rectTransform.anchorMax=Vector2.one;timerBarFill.color=timerCoolColor;

        // Apply content
        statusTag.text = config.isDisinformation ? "TEYİT" : "ACİL";
        categoryLabel.text = config.categoryTitle;
        questionBody.text = config.questionText;
        labelA.text = config.choiceALabel;
        labelB.text = config.choiceBLabel;
        feedbackLabel.alpha = 0f;
        feedbackLabel.text = string.Empty;

        // Activate components
        buttonA.gameObject.SetActive(true);
        buttonB.gameObject.SetActive(true);
        UpdateButtonAvailability();

        visible=true;
        activeRoutine = StartCoroutine(RunQuestionCycle());

        // Optional arrival sound
        if (audioSource && arrivalClip)
        {
            audioSource.PlayOneShot(arrivalClip);
        }
    }

    private void UpdateButtonAvailability()
    {
        if (currentResources == null || currentConfig == null) return;

        bool canA = canChoose != null && canChoose(0);
        bool canB = canChoose != null && canChoose(1);

        buttonA.interactable = !isHandlingResponse && canA;
        buttonB.interactable = !isHandlingResponse && canB;

        labelA.color = canA ? paperText : new Color(.5f, .55f, .55f, .7f);
        labelB.color = canB ? paperText : new Color(.5f, .55f, .55f, .7f);

        if (!canA && !currentConfig.choiceAEffect.IsZero)
            labelA.text = currentConfig.choiceALabel + "\n(KULLANILAMIYOR)";
        else
            labelA.text = currentConfig.choiceALabel;

        if (!canB && !currentConfig.choiceBEffect.IsZero)
            labelB.text = currentConfig.choiceBLabel + "\n(KULLANILAMIYOR)";
        else
            labelB.text = currentConfig.choiceBLabel;
    }

    private IEnumerator RunQuestionCycle()
    {
        // 1. Entrance animation (fade in & subtle slide from left)
        panelRect.anchoredPosition = restingPosition + new Vector2(-52f, 0f);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / entranceDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            canvasGroup.alpha = smooth;
            panelRect.anchoredPosition = Vector2.Lerp(restingPosition + new Vector2(-52f, 0f), restingPosition, smooth);

            // Amber border pulse
            if (borderPulseImage)
            {
                float pulseAlpha = Mathf.Sin(t * Mathf.PI) * 0.85f;
                borderPulseImage.color = new Color(amberAccent.r, amberAccent.g, amberAccent.b, pulseAlpha);
            }
            yield return null;
        }

        panelRect.anchoredPosition = restingPosition;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        if (borderPulseImage) borderPulseImage.color = new Color(amberAccent.r, amberAccent.g, amberAccent.b, 0f);

        // 2. Active countdown loop
        float remaining = questionDuration;
        bool tickPlayed = false;

        while (remaining > 0f && !isHandlingResponse)
        {
            if (IsPaused) { yield return null; continue; }
            elapsedResponseTime += Time.unscaledDeltaTime;
            remaining = questionDuration - ElapsedResponseTime;
            float progress = Mathf.Clamp01(remaining / questionDuration);

            bool critical = progress <= .3f;
            statusTag.text = (currentConfig.isDisinformation ? "TEYİT " : "ACİL ") + Mathf.CeilToInt(remaining) + "s";
            statusTag.color = critical ? timerRedColor : coolCyan;
            if (borderPulseImage) borderPulseImage.color = critical
                ? new Color(timerRedColor.r, timerRedColor.g, timerRedColor.b, .3f + .4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 9)))
                : Color.clear;
            panelRect.anchoredPosition = restingPosition + (critical
                ? Vector2.right * Mathf.Round(Mathf.Sin(Time.unscaledTime * 35)) : Vector2.zero);

            // Update timer bar fill & color
            if (timerBarFill)
            {
                timerBarFill.rectTransform.anchorMax = new Vector2(progress, 1f);

                if (progress > 0.40f)
                {
                    timerBarFill.color = timerCoolColor;
                }
                else if (progress > 0.20f)
                {
                    timerBarFill.color = timerAmberColor;
                }
                else
                {
                    // Subtle pulse on critical red
                    float redPulse = 0.82f + 0.18f * Mathf.Sin(Time.unscaledTime * 12f);
                    timerBarFill.color = new Color(timerRedColor.r * redPulse, timerRedColor.g * redPulse, timerRedColor.b * redPulse, 1f);

                    if (!tickPlayed && audioSource && warningTickClip)
                    {
                        tickPlayed = true;
                        audioSource.PlayOneShot(warningTickClip, 0.4f);
                    }
                }
            }

            yield return null;
        }

        // 3. Timeout handling if not answered
        if (!isHandlingResponse)
        {
            isHandlingResponse = true;
            canvasGroup.interactable = false;
            buttonA.interactable = false;
            buttonB.interactable = false;

            // Timer bar to zero
            if (timerBarFill)
            {
                timerBarFill.rectTransform.anchorMax = new Vector2(0f, 1f);
                timerBarFill.color = timerRedColor;
            }

            // Show timeout feedback
            feedbackLabel.text = currentConfig.timeoutFeedback;
            feedbackLabel.color = currentConfig.isDisinformation ? timerRedColor : timerAmberColor;
            feedbackLabel.alpha = 1f;
            buttonA.gameObject.SetActive(false);
            buttonB.gameObject.SetActive(false);

            timeoutCallback?.Invoke();

            yield return new WaitForSecondsRealtime(timeoutFeedbackDuration);
            yield return StartCoroutine(AnimateExit());
        }
    }

    private void OnChoiceClicked(int choiceIndex)
    {
        // Strict double-click prevention
        if (!Ready || ElapsedResponseTime>=questionDuration || canChoose==null || !canChoose(choiceIndex)) return;
        isHandlingResponse = true;

        canvasGroup.interactable = false;
        buttonA.interactable = false;
        buttonB.interactable = false;

        string feedback = choiceIndex == 0 ? currentConfig.choiceAFeedback : currentConfig.choiceBFeedback;
        feedbackLabel.text = feedback;
        bool wrong = currentConfig.isDisinformation && choiceIndex != currentConfig.correctChoiceIndex;
        feedbackLabel.color = wrong ? timerRedColor : coolCyan;
        feedbackLabel.alpha = 1f;

        buttonA.gameObject.SetActive(false);
        buttonB.gameObject.SetActive(false);

        // Invoke decision callback once
        if(decisionCallback==null || !decisionCallback(choiceIndex)){isHandlingResponse=false;canvasGroup.interactable=true;feedbackLabel.alpha=0;buttonA.gameObject.SetActive(true);buttonB.gameObject.SetActive(true);UpdateButtonAvailability();return;}

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(DismissAfterFeedback());
    }

    private IEnumerator DismissAfterFeedback()
    {
        yield return new WaitForSecondsRealtime(answerFeedbackDuration);
        yield return StartCoroutine(AnimateExit());
    }

    private IEnumerator AnimateExit()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        Vector2 startPos = panelRect.anchoredPosition;
        Vector2 endPos = restingPosition + new Vector2(-20f, 0f);

        while (elapsed < exitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / exitDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            canvasGroup.alpha = 1f - smooth;
            panelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, smooth);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        panelRect.anchoredPosition = restingPosition;
        if(currentResources)currentResources.Changed-=UpdateButtonAvailability;
        visible = false;
        activeRoutine = null;
        isHandlingResponse = false;
    }
}
