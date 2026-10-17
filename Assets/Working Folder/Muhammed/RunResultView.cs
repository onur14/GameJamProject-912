using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RunResultView : MonoBehaviour
{
    [SerializeField] private PresidentialBriefingManager briefing;
    [SerializeField] private CityHealth health;
    [SerializeField] private MiniGameWindow minigames;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text reasonLabel;
    [SerializeField] private TMP_Text summaryLabel;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private string menuSceneName = "OnurAnaMenu";
    [SerializeField] private Cizirti cizirti;
    private bool shown;

    private void Awake()
    {
        ShiftReportFlow.Clear();
        var card=titleLabel.transform.parent;
        DispatchInsetFrame.Ensure(card);
        foreach(var label in card.GetComponentsInChildren<TMP_Text>(true))label.alignment=TextAlignmentOptions.Center;
        panel.SetActive(false);
    }

    private void OnEnable()
    {
        if (briefing != null) briefing.RunCompleted += ShowSuccess;
        if (health != null) health.GameEnded += ShowFailure;
        replayButton.onClick.AddListener(Replay);
        menuButton.onClick.AddListener(MainMenu);
    }

    private void OnDisable()
    {
        if (briefing != null) briefing.RunCompleted -= ShowSuccess;
        if (health != null) health.GameEnded -= ShowFailure;
        replayButton.onClick.RemoveListener(Replay);
        menuButton.onClick.RemoveListener(MainMenu);
    }

    public void ShowSuccess()
    {
        if (shown) return;
        if (ShiftReportFlow.Complete(briefing, health, minigames)) { shown = true; return; }
        Show("GÖREV TAMAMLANDI", "Vardiyadaki tüm vakalar tamamlandı.");
    }

    public void ShowFailure()
    {
        Show("HALK BİLİNCİ TÜKENDİ", "Son vakanın başlangıç kaydı korunuyor.");
        var label = replayButton.GetComponentInChildren<TMP_Text>();
        if (label) label.text = "SON VAKADAN DEVAM ET";
    }

    private void Show(string title, string reason)
    {
        if (cizirti) cizirti.TelefonKapat();
        if (shown) return;
        shown = true;
        if (minigames != null) minigames.Close();
       

        titleLabel.text = title;
        reasonLabel.text = reason;
        int calls = briefing != null ? briefing.CompletedScenarios : 0;
        int correct = briefing != null ? briefing.CorrectDecisions : 0;
        int tasks = minigames != null ? minigames.SuccessfulTasks : 0;
        int healthValue = health != null ? health.HealthPercent : 0;
        if(briefing!=null && briefing.Resources!=null)
        {
            var data=ShiftReportData.Capture(briefing,FindAnyObjectByType<QuickQuestionManager>(),minigames,health,SceneManager.GetActiveScene().path);
            summaryLabel.text=string.Join("\n",data.ExplainOutcome(healthValue>0&&calls>=8));
        }
        else summaryLabel.text=$"Tamamlanan çağrı  {calls} (Toplam 8)\nDoğru karar  {correct}\nMinigame  {tasks} (Toplam 2)\nHalk bilinci  %{healthValue}";
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    private void Replay()
    {
        Time.timeScale = 1f;
        var checkpoint = FindFirstObjectByType<DispatchCurseDirector>();
        if (health && health.IsGameOver && checkpoint && checkpoint.CanRetry)
        {
            panel.SetActive(false); shown = false;
            checkpoint.RetryCurrentScenario();
            return;
        }
        PreparationRun.StartNewGame(SceneManager.GetActiveScene().path);
    }

    private void MainMenu()
    {
        Time.timeScale = 1f;
        PreparationRun.Reset();
        SceneManager.LoadScene(menuSceneName);
    }
}
