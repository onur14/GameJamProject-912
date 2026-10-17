using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CityHealth : MonoBehaviour
{
    private const int StartingHealth = 100;
    private const int WrongAnswerDamage = 40;

    [SerializeField] private OyunAkisiOnur conversation;
    [SerializeField] private PresidentialBriefingManager briefing;
    [SerializeField] private DispatchCallFlow phone;
    [SerializeField] private TMP_FontAsset interfaceFont;

    [SerializeField] private TMP_FontAsset healthFont;
    [SerializeField] private OnurConversationView choiceView;
    private RectTransform missionNote;
    private Image noteBorder;
    private CanvasGroup noteGroup;
    private bool noteShown;

    private RectTransform healthIndicator;
    private TMP_Text healthText;
    private CanvasGroup gameOverOverlay;
    private Coroutine damageAnimation;

    public int HealthPercent { get; private set; } = StartingHealth;
    public bool IsGameOver { get; private set; }
    public int WrongDecisionCount { get; private set; }
    public int TimedOutDecisionCount { get; private set; }
    public event System.Action GameEnded;

    public bool AddHealth(int percentagePoints)
    {
        if (IsGameOver || percentagePoints <= 0 || HealthPercent >= StartingHealth) return false;
        HealthPercent = Mathf.Min(StartingHealth, HealthPercent + percentagePoints);
        Refresh();
        return true;
    }

    private void Awake()
    {
        if (conversation == null || briefing == null || phone == null)
        {
            Debug.LogError("Şehir sağlığı: sahne referansları eksik.", this);
            enabled = false;
            return;
        }

        BuildInterface();
        if (PreparationRun.preparationComplete) HealthPercent = Mathf.Max(1, PreparationRun.Health);
        Refresh();
    }

    private void OnEnable()
    {
        if (conversation != null) { conversation.DecisionResolved += OnDecisionResolved; conversation.DecisionTimedOut += OnDecisionTimedOut; }
        if (choiceView != null) choiceView.ChoicesShown += ShowMissionNote;
    }

    private void OnDisable()
    {
        if (conversation != null) { conversation.DecisionResolved -= OnDecisionResolved; conversation.DecisionTimedOut -= OnDecisionTimedOut; }
        if (choiceView != null) choiceView.ChoicesShown -= ShowMissionNote;
    }

    private void OnDecisionTimedOut()
    {
        if (!IsGameOver && briefing.Phase == PresidentialBriefingManager.ShiftPhase.Scenario) { TimedOutDecisionCount++; ApplyDamage(5); }
    }

    private void OnDecisionResolved(bool correct)
    {
        if (correct || IsGameOver || briefing.Phase != PresidentialBriefingManager.ShiftPhase.Scenario) return;
        WrongDecisionCount++;
        ApplyDamage(WrongAnswerDamage);
    }

    public bool ApplyDamage(int percentagePoints)
    {
        if (IsGameOver || percentagePoints <= 0) return false;
        HealthPercent = Mathf.Max(0, HealthPercent - percentagePoints);
        Refresh();
        if (damageAnimation != null) StopCoroutine(damageAnimation);
        damageAnimation = StartCoroutine(AnimateDamage());
        if (HealthPercent == 0) EndGame();
        return true;
    }

    public void ResetForNewShift()
    {
        StopAllCoroutines();
        damageAnimation = null;
        noteShown = false;
        if (missionNote != null) missionNote.gameObject.SetActive(false);
        HealthPercent = StartingHealth;
        WrongDecisionCount = 0;TimedOutDecisionCount = 0;
        IsGameOver = false;
        if (healthIndicator != null)
        {
            healthIndicator.localScale = Vector3.one;
            healthIndicator.localRotation = Quaternion.identity;
        }
        if (gameOverOverlay != null) gameOverOverlay.gameObject.SetActive(false);
        Refresh();
    }

    private void EndGame()
    {
        IsGameOver = true;
        if (missionNote != null) missionNote.gameObject.SetActive(false);
        conversation.EndGame();
        phone.SetInteractionLocked(true);
        gameOverOverlay.gameObject.SetActive(true);
        gameOverOverlay.transform.SetAsLastSibling();
        StartCoroutine(FadeToBlack());
        GameEnded?.Invoke();
    }

    private void Refresh()
    {
        if (healthText != null) healthText.text = $"ŞEHİR SAĞLIĞI\n%{HealthPercent}";
    }

    private void BuildInterface()
    {
        var indicatorObject = new GameObject("Şehir Sağlığı", typeof(RectTransform), typeof(TextMeshProUGUI));
        healthIndicator = (RectTransform)indicatorObject.transform;
        healthIndicator.SetParent(transform, false);
        healthIndicator.anchorMin = new Vector2(.02f, .88f);
        healthIndicator.anchorMax = new Vector2(.24f, .99f);
        healthIndicator.offsetMin = healthIndicator.offsetMax = Vector2.zero;
        healthText = indicatorObject.GetComponent<TextMeshProUGUI>();
        healthText.font = healthFont != null ? healthFont : interfaceFont != null ? interfaceFont : TMP_Settings.defaultFontAsset;
        healthText.fontSize = 26;
        healthText.fontStyle |= FontStyles.Bold; healthText.fontWeight = FontWeight.Heavy;
        healthText.alignment = TextAlignmentOptions.TopLeft;
        healthText.color = new Color(.87f, .96f, .91f);
        healthText.raycastTarget = false;

        var subtitle = NoteText("Şehir Hedefi", healthIndicator, "Şehri afete hazırla", 15);
        subtitle.rectTransform.anchorMin = new Vector2(0, 0);
        subtitle.rectTransform.anchorMax = new Vector2(1, .3f);
        subtitle.rectTransform.offsetMin = subtitle.rectTransform.offsetMax = Vector2.zero;
        subtitle.color = new Color(.65f, .82f, .82f);
        BuildMissionNote();

        var overlayObject = new GameObject("Oyun Bitti Ekranı", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        var overlayRect = (RectTransform)overlayObject.transform;
        overlayRect.SetParent(transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
        overlayObject.GetComponent<Image>().color = Color.black;
        gameOverOverlay = overlayObject.GetComponent<CanvasGroup>();
        gameOverOverlay.alpha = 0;

        var titleObject = new GameObject("Oyun Bitti Yazısı", typeof(RectTransform), typeof(TextMeshProUGUI));
        var titleRect = (RectTransform)titleObject.transform;
        titleRect.SetParent(overlayRect, false);
        titleRect.anchorMin = new Vector2(.2f, .4f);
        titleRect.anchorMax = new Vector2(.8f, .6f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
        var title = titleObject.GetComponent<TextMeshProUGUI>();
        title.font = interfaceFont != null ? interfaceFont : TMP_Settings.defaultFontAsset;
        title.fontSize = 72;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        title.text = "OYUN BİTTİ";
        title.raycastTarget = false;
        overlayObject.SetActive(false);
    }

    private TMP_Text NoteText(string name, RectTransform parent, string text, float size)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent, false);
        label.font = healthFont != null ? healthFont : interfaceFont != null ? interfaceFont : TMP_Settings.defaultFontAsset;
        label.fontSize = size;
        label.fontStyle |= FontStyles.Bold; label.fontWeight = FontWeight.Heavy;
        label.text = text;
        label.color = new Color(.93f, .95f, .89f);
        label.alignment = TextAlignmentOptions.TopLeft;
        label.raycastTarget = false;
        return label;
    }

    private void BuildMissionNote()
    {
        var root = new GameObject("İlk Karar Bilgi Notu", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        missionNote = (RectTransform)root.transform;
        missionNote.SetParent(transform, false);
        missionNote.anchorMin = missionNote.anchorMax = new Vector2(.37f, .80f);
        missionNote.pivot = new Vector2(0, .5f);
        missionNote.sizeDelta = new Vector2(500, 128);
        noteBorder = root.GetComponent<Image>();
        noteBorder.color = new Color(.95f, .67f, .24f);
        noteBorder.raycastTarget = false;
        noteGroup = root.GetComponent<CanvasGroup>();
        noteGroup.blocksRaycasts = false;
        noteGroup.interactable = false;
        var inner = new GameObject("Not Zemini", typeof(RectTransform), typeof(Image));
        var innerRect = (RectTransform)inner.transform;
        innerRect.SetParent(missionNote, false);
        innerRect.anchorMin = Vector2.zero; innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = Vector2.one * 3; innerRect.offsetMax = Vector2.one * -3;
        inner.GetComponent<Image>().color = new Color(.055f, .095f, .14f, .98f);
        inner.GetComponent<Image>().raycastTarget = false;
        var body = NoteText("Görev Notu", innerRect,
            "<color=#F2B54D>! GÖREV NOTU</color>\nDoğru ve yanlış cevapların kaynaklarını etkiler.\nŞehir sağlığı %0'a düşerse kaybedersin.\nAna görevin: şehri doğru bilinçlendirmek ve kaynakları yönetmek.", 14);
        body.rectTransform.anchorMin = Vector2.zero; body.rectTransform.anchorMax = Vector2.one;
        body.rectTransform.offsetMin = new Vector2(14, 12); body.rectTransform.offsetMax = new Vector2(-14, -14);
        root.SetActive(false);
    }

    private void ShowMissionNote()
    {
        if (noteShown || IsGameOver || missionNote == null) return;
        noteShown = true;
        StartCoroutine(AnimateMissionNote());
    }

    private IEnumerator AnimateMissionNote()
    {
        missionNote.gameObject.SetActive(true);
        // Only the note shakes and flashes; choices remain clickable throughout.
        var canvasRect = (RectTransform)transform;
        float startX = canvasRect.rect.width + missionNote.rect.width;
        for (float t = 0; t < .65f; t += Time.unscaledDeltaTime)
        {
            float p = Mathf.Clamp01(t / .65f);
            float shake = p > .6f ? Mathf.Sin(p * 80) * 4 * (1 - p) : 0;
            missionNote.anchoredPosition = new Vector2(Mathf.Lerp(startX, 0, 1 - Mathf.Pow(1 - p, 4)) + shake, 0);
            noteGroup.alpha = Mathf.Clamp01(p * 5);
            noteBorder.color = Color.Lerp(new Color(.95f, .67f, .24f), Color.white, Mathf.Max(0, Mathf.Sin(p * Mathf.PI * 6)) * (1 - p));
            yield return null;
        }
        missionNote.anchoredPosition = Vector2.zero;
        noteGroup.alpha = 1;
        noteBorder.color = new Color(.95f, .67f, .24f);
        float remaining = 18;
        while (remaining > 0 && !IsGameOver)
        {
            if (choiceView == null || !choiceView.IsPaused) remaining -= Time.unscaledDeltaTime;
            yield return null;
        }
        for (float t = 0; t < .25f; t += Time.unscaledDeltaTime)
        {
            noteGroup.alpha = 1 - t / .25f;
            yield return null;
        }
        missionNote.gameObject.SetActive(false);
    }

    private IEnumerator AnimateDamage()
    {
        const float duration = .4f;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = elapsed / duration;
            healthIndicator.localScale = Vector3.one * (1f + .3f * Mathf.Sin(progress * Mathf.PI));
            healthIndicator.localRotation = Quaternion.Euler(0, 0, 7f * Mathf.Sin(progress * Mathf.PI * 4f) * (1f - progress));
            yield return null;
        }

        healthIndicator.localScale = Vector3.one;
        healthIndicator.localRotation = Quaternion.identity;
        damageAnimation = null;
    }

    private IEnumerator FadeToBlack()
    {
        const float duration = .55f;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            gameOverOverlay.alpha = elapsed / duration;
            yield return null;
        }

        gameOverOverlay.alpha = 1;
    }
}
