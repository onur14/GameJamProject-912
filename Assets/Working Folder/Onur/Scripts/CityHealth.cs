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

    private RectTransform healthIndicator;
    private TMP_Text healthText;
    private CanvasGroup gameOverOverlay;
    private Coroutine damageAnimation;

    public int HealthPercent { get; private set; } = StartingHealth;
    public bool IsGameOver { get; private set; }

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
        Refresh();
    }

    private void OnEnable()
    {
        if (conversation != null) conversation.DecisionResolved += OnDecisionResolved;
    }

    private void OnDisable()
    {
        if (conversation != null) conversation.DecisionResolved -= OnDecisionResolved;
    }

    private void OnDecisionResolved(bool correct)
    {
        if (correct || IsGameOver || briefing.Phase != PresidentialBriefingManager.ShiftPhase.Scenario) return;

        HealthPercent = Mathf.Max(0, HealthPercent - WrongAnswerDamage);
        Refresh();

        if (damageAnimation != null) StopCoroutine(damageAnimation);
        damageAnimation = StartCoroutine(AnimateDamage());

        if (HealthPercent == 0) EndGame();
    }

    public void ResetForNewShift()
    {
        StopAllCoroutines();
        damageAnimation = null;
        HealthPercent = StartingHealth;
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
        conversation.EndGame();
        phone.SetInteractionLocked(true);
        gameOverOverlay.gameObject.SetActive(true);
        gameOverOverlay.transform.SetAsLastSibling();
        StartCoroutine(FadeToBlack());
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
        healthIndicator.anchorMin = new Vector2(.02f, .93f);
        healthIndicator.anchorMax = new Vector2(.24f, .99f);
        healthIndicator.offsetMin = healthIndicator.offsetMax = Vector2.zero;
        healthText = indicatorObject.GetComponent<TextMeshProUGUI>();
        healthText.font = interfaceFont != null ? interfaceFont : TMP_Settings.defaultFontAsset;
        healthText.fontSize = 26;
        healthText.alignment = TextAlignmentOptions.TopLeft;
        healthText.color = new Color(.87f, .96f, .91f);
        healthText.raycastTarget = false;

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
