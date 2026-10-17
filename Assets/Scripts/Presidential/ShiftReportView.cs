using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShiftReportView : MonoBehaviour
{
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private GameObject normalMenu;
    [SerializeField] private TMP_Text performance;
    [SerializeField] private TMP_Text resources;
    [SerializeField] private TMP_Text grade;
    [SerializeField] private TMP_Text operation;
    [SerializeField] private TMP_Text events;
    [SerializeField] private TMP_Text score;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button menuButton;
    private bool navigating;
    public ShiftReportData Data { get; private set; }
    public bool IsVisible => reportPanel.activeSelf;
    public Button ReplayButton => replayButton;
    public Button MenuButton => menuButton;

    private void Awake()
    {
        PolishLayout();
        reportPanel.SetActive(false);
        replayButton.onClick.AddListener(Replay);
        menuButton.onClick.AddListener(MainMenu);
    }
    private void Start()
    {
        Show(ShiftReportFlow.Pending);
    }
    public void Show(ShiftReportData data)
    {
        if (data == null) return;
        Data = data;navigating = false;
        string seconds = Data.HasResponseTimes ? Data.AverageSeconds.ToString("F1", CultureInfo.GetCultureInfo("tr-TR")) + " sn" : "—";
        performance.text = $"Doğru Müdahale: {Data.Correct} (Toplam {Data.Completed})\nKaçırılan Talep: {Data.Missed}\nOrtalama Tepki Süresi: {seconds}";
        resources.text = $"Ambulans: {Data.Ambulances}\nİtfaiye: {Data.Firefighters}\nKurtarma Ekibi: {Data.RescueTeams}";
        grade.text = "VARDİYA NOTU: " + Data.Grade;
        operation.text = "OPERASYON DURUMU\n" + Data.OperationStatus;
        events.text = string.Join("\n", Data.ExplainOutcome(true));
        score.text = "Puan: " + Data.Score + " (100 üzerinden)";
        normalMenu.SetActive(false);
        reportPanel.SetActive(true);
    }
    private void PolishLayout()
    {
        var card=grade.transform.parent;
        DispatchInsetFrame.Ensure(card);
        foreach(var label in card.GetComponentsInChildren<TMP_Text>(true))
        {
            if(label==performance||label==resources||label.GetComponentInParent<Button>())continue;
            label.alignment=TextAlignmentOptions.Top;
        }
        events.alignment=TextAlignmentOptions.Top;
        var heading=card.Find("Olay Başlığı");
        if(heading)heading.GetComponent<TMP_Text>().text="VARDİYA ÖZETİ";
    }
    public void Replay()
    {
        if (navigating || SceneFadeTransition.IsTransitioning || Data == null || !Application.CanStreamedLevelBeLoaded(PreparationRun.ScenePath)) return;
        navigating = true;
        string destination = Data.GameplayScene;
        ShiftReportFlow.Clear();Time.timeScale = 1;
        PreparationRun.StartNewGame(destination, .4f, .65f);
    }
    public void MainMenu()
    {
        if (navigating || SceneFadeTransition.IsTransitioning) return;
        ShiftReportFlow.Clear();Data = null;reportPanel.SetActive(false);normalMenu.SetActive(true);
        PreparationRun.Reset();
    }
    private void OnDestroy()
    {
        replayButton.onClick.RemoveListener(Replay);
        menuButton.onClick.RemoveListener(MainMenu);
    }
}
