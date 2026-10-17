using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class MiniGameWindow : MonoBehaviour
{
    [SerializeField] private Button bagEntryButton;
    [SerializeField] private Button safetyEntryButton;
    [SerializeField] private CanvasGroup overlay;
    [SerializeField] private BagMiniGame bagGame;
    [SerializeField] private SafetyMiniGame safetyGame;
    [SerializeField] private CityHealth cityHealth;
    [SerializeField] private DispatchCallFlow phone;
    [SerializeField] private OnurConversationView conversationView;
    [SerializeField] private QuickQuestionView quickQuestionView;

    [SerializeField] private DispatchTablet tablet;
    [SerializeField, Min(1)] private int actionHealthPoints = 2;
    public bool UsesImmediateFeedback => tablet != null;
    public CityHealth Health => cityHealth;
    public GameObject BagPanel => bagGame.gameObject;
    public GameObject SafetyPanel => safetyGame.gameObject;
    public Button BagEntryButton => bagEntryButton;
    public Button SafetyEntryButton => safetyEntryButton;
    public void EvaluateAction(bool correct)
    {
        if (tablet == null || cityHealth == null || cityHealth.IsGameOver) return;
        int before = cityHealth.HealthPercent;
        if (correct) cityHealth.AddHealth(actionHealthPoints); else cityHealth.ApplyDamage(actionHealthPoints);
        tablet.Feedback(cityHealth.HealthPercent - before, correct);
    }

    private bool wasPhonePaused;
    private bool wasConversationPaused;
    private bool wasQuickQuestionPaused;
    private bool isOpen;
    public int SuccessfulTasks => (bagGame != null && bagGame.Succeeded ? 1 : 0)
        + (safetyGame != null && safetyGame.Succeeded ? 1 : 0);
    [SerializeField, Range(0f, 3f)] private float mistakeShakePixels = 2f;
    [SerializeField, Range(.1f, .3f)] private float mistakeShakeDuration = .2f;
    private Coroutine mistake;
    private RectTransform shakenPanel;
    private Vector2 restingPosition;

    private void Awake()
    {
        bagEntryButton.onClick.AddListener(OpenBag);
        safetyEntryButton.onClick.AddListener(OpenSafety);
        overlay.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        bagEntryButton.onClick.RemoveListener(OpenBag);
        safetyEntryButton.onClick.RemoveListener(OpenSafety);
    }

    public void OpenBag() { Open(bagGame.gameObject); }
    public void OpenSafety() { Open(safetyGame.gameObject); }

    private void Open(GameObject panel)
    {
        if (cityHealth == null || cityHealth.IsGameOver) return;
        if (tablet != null)
        {
            isOpen = true;
            bagGame.CancelDrags();
            bagGame.gameObject.SetActive(panel == bagGame.gameObject);
            safetyGame.gameObject.SetActive(panel == safetyGame.gameObject);
            tablet.ShowGame(panel);
            return;
        }
        if (isOpen) return;
        isOpen = true;
        wasPhonePaused = phone != null && phone.IsPaused;
        wasConversationPaused = conversationView != null && conversationView.IsPaused;
        wasQuickQuestionPaused = quickQuestionView != null && quickQuestionView.IsPaused;
        if (phone != null) phone.SetPaused(true);
        if (conversationView != null) conversationView.SetPaused(true);
        if (quickQuestionView != null) quickQuestionView.SetPaused(true);
        bagGame.gameObject.SetActive(panel == bagGame.gameObject);
        safetyGame.gameObject.SetActive(panel == safetyGame.gameObject);
        overlay.gameObject.SetActive(true);
        overlay.transform.SetAsLastSibling();
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        StopMistake();
        bagGame.CancelDrags();
        overlay.gameObject.SetActive(false);
        bagGame.gameObject.SetActive(false);
        safetyGame.gameObject.SetActive(false);
        if (tablet != null) { tablet.ShowMenu(); return; }
        if (phone != null) phone.SetPaused(wasPhonePaused);
        if (conversationView != null) conversationView.SetPaused(wasConversationPaused);
        if (quickQuestionView != null) quickQuestionView.SetPaused(wasQuickQuestionPaused);
    }
    private void OnDisable() { Close(); }

    public void ShowMistake(RectTransform panel)
    {
        StopMistake();
        shakenPanel = panel;
        restingPosition = panel.anchoredPosition;
        mistake = StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        float scale = Mathf.Max(.001f, shakenPanel.lossyScale.x);
        for (float t = 0; t < mistakeShakeDuration; t += Time.unscaledDeltaTime)
        {
            float p = t / mistakeShakeDuration;
            float x = Mathf.Round(Mathf.Sin(p * Mathf.PI * 5f) * mistakeShakePixels * (1f - p));
            shakenPanel.anchoredPosition = restingPosition + Vector2.right * (x / scale);
            yield return null;
        }
        shakenPanel.anchoredPosition = restingPosition;
        shakenPanel = null; mistake = null;
    }

    private void StopMistake()
    {
        if (mistake != null) StopCoroutine(mistake);
        if (shakenPanel != null) shakenPanel.anchoredPosition = restingPosition;
        shakenPanel = null; mistake = null;
    }

    public int Award(int percentagePoints)
    {
        if (cityHealth == null) return 0;
        int previousHealth = cityHealth.HealthPercent;
        cityHealth.AddHealth(percentagePoints);
        return cityHealth.HealthPercent - previousHealth;
    }

    public void ResetForNewSession()
    {
        Close();
        bagGame.ResetForNewSession();
        safetyGame.ResetForNewSession();
    }
}
