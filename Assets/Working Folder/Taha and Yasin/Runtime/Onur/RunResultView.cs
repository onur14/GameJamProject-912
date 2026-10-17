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

    private bool shown;

    private void Awake()
    {
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
        Show("GÖREV TAMAMLANDI", "Vardiyadaki tüm vakalar tamamlandı.");
    }

    public void ShowFailure()
    {
        Show("GÖREV BAŞARISIZ", "Şehir sağlığı %0'a düştü.");
    }

    private void Show(string title, string reason)
    {
        if (shown) return;
        shown = true;
        if (minigames != null) minigames.Close();
        titleLabel.text = title;
        reasonLabel.text = reason;
        int calls = briefing != null ? briefing.CompletedScenarios : 0;
        int correct = briefing != null ? briefing.CorrectDecisions : 0;
        int tasks = minigames != null ? minigames.SuccessfulTasks : 0;
        int healthValue = health != null ? health.HealthPercent : 0;
        summaryLabel.text = $"Tamamlanan çağrı  {calls}/8\nDoğru karar  {correct}\nMinigame  {tasks}/2\nŞehir sağlığı  %{healthValue}";
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    private void Replay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    private void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}
