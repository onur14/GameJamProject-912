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
    [SerializeField] private PreparationDirector preparation;
    [SerializeField, Min(1)] private int actionHealthPoints = 2;
    public bool UsesImmediateFeedback => preparation != null || tablet != null;
    public BagMiniGame BagGame => bagGame;
    public bool IsPreparationWindow => preparation != null;
    public int IncorrectActions { get; private set; }
    public CityHealth Health => cityHealth;
    public GameObject BagPanel => bagGame.gameObject;
    public GameObject SafetyPanel => safetyGame.gameObject;
    public Button BagEntryButton => bagEntryButton;
    public Button SafetyEntryButton => safetyEntryButton;
    public void EvaluateAction(bool correct)
    {
        if (preparation != null) { preparation.EvaluateBagAction(correct, actionHealthPoints); return; }
        if (tablet == null || cityHealth == null || cityHealth.IsGameOver) return;
        if (!correct) IncorrectActions++;
        int before = cityHealth.HealthPercent;
        if (correct) cityHealth.AddHealth(actionHealthPoints); else cityHealth.ApplyDamage(actionHealthPoints);
        tablet.Feedback(cityHealth.HealthPercent - before, correct);
    }

    private bool isOpen;
    public int SuccessfulTasks => PreparationRun.CompletedCount;
    public int FailedTasks => 0;
    public int AvailableTasks => PreparationRun.Visited ? 4 : 0;
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
        bagEntryButton.gameObject.SetActive(false);
        safetyEntryButton.gameObject.SetActive(false);
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
        // Legacy references may still invoke these methods; operator scenes never open them.
        if (!preparation) return;
        if (panel != bagGame.gameObject || isOpen) return;
        isOpen = true;
        bagGame.CancelDrags();
        bagGame.gameObject.SetActive(true);
        safetyGame.gameObject.SetActive(false);
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
        if (preparation != null && preparation.isActiveAndEnabled) preparation.BagClosed();
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
        if (preparation) { int before=PreparationRun.Health; PreparationRun.AdjustHealth(percentagePoints); preparation.RefreshStatus(); return PreparationRun.Health-before; }
        if (cityHealth == null) return 0;
        int previousHealth = cityHealth.HealthPercent;
        cityHealth.AddHealth(percentagePoints);
        return cityHealth.HealthPercent - previousHealth;
    }

    public void ResetForNewSession()
    {
        IncorrectActions = 0;
        Close();
        bagGame.ResetForNewSession();
        safetyGame.ResetForNewSession();
    }
}
