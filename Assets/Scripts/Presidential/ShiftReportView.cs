using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShiftReportView : MonoBehaviour
{
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private GameObject normalMenu;
    [SerializeField] private TMP_FontAsset reportFont;
    [SerializeField] private TMP_Text performance;
    [SerializeField] private TMP_Text resources;
    [SerializeField] private TMP_Text grade;
    [SerializeField] private TMP_Text operation;
    [SerializeField] private TMP_Text events;
    [SerializeField] private TMP_Text score;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button menuButton;
    private bool navigating;
    private Coroutine typingRoutine;
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
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        Data = data;navigating = false;
        string seconds = Data.HasResponseTimes ? Data.AverageSeconds.ToString("F1", CultureInfo.GetCultureInfo("tr-TR")) + " sn" : "—";
        performance.text = $"Doğru müdahale: {Data.Correct}/{Data.Completed}\nKaçırılan talep: {Data.Missed}\nOrtalama tepki: {seconds}";
        resources.text = $"Ambulans: {Data.Ambulances}\nİtfaiye: {Data.Firefighters}\nKurtarma: {Data.RescueTeams}";
        grade.text = "VARDİYA NOTU: " + Data.Grade;
        operation.text = "OPERASYON DURUMU\n" + Data.OperationStatus;
        events.text = string.Join("\n", Data.ExplainOutcome(true));
        score.text = "PUAN  " + Data.Score + "/100";
        foreach(var label in new[]{performance,resources,grade,operation,events,score})label.maxVisibleCharacters=0;
        normalMenu.SetActive(false);
        reportPanel.SetActive(true);
        typingRoutine=StartCoroutine(TypeReport());
    }
    private IEnumerator TypeReport()
    {
        while (SceneFadeTransition.IsTransitioning) yield return null;
        yield return null;
        foreach(var label in new[]{performance,resources,grade,operation,events,score})
        {
            label.ForceMeshUpdate();
            int total=label.textInfo.characterCount;
            float visible=0;
            while(visible<total)
            {
                visible+=Time.unscaledDeltaTime*90f;
                label.maxVisibleCharacters=Mathf.Min(total,Mathf.FloorToInt(visible));
                yield return null;
            }
            label.maxVisibleCharacters=int.MaxValue;
            yield return new WaitForSecondsRealtime(.08f);
        }
        typingRoutine=null;
    }
    private void PolishLayout()
    {
        var card=grade.transform.parent;
        DispatchInsetFrame.Ensure(card);
        foreach(var label in reportPanel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (reportFont) { label.font=reportFont;label.fontSharedMaterial=reportFont.material; }
            float size=label.name=="Rapor Başlığı"?40f:label==grade?32f:
                label==performance||label==resources||label==events?26f:
                label==operation||label.GetComponentInParent<Button>()?25f:
                label==score?24f:22f;
            label.fontSize=size;
            label.fontStyle=FontStyles.Bold;
            label.enableAutoSizing=true;
            label.fontSizeMin=size-2f;
            label.fontSizeMax=size;
            label.overflowMode=TextOverflowModes.Ellipsis;
            if(label==performance||label==resources||label==score||label.GetComponentInParent<Button>())continue;
            label.alignment=TextAlignmentOptions.Top;
        }
        performance.lineSpacing=resources.lineSpacing=events.lineSpacing=2;
        score.alignment=TextAlignmentOptions.MidlineRight;
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
        if (typingRoutine != null) { StopCoroutine(typingRoutine);typingRoutine=null; }
        ShiftReportFlow.Clear();Data = null;reportPanel.SetActive(false);normalMenu.SetActive(true);
        PreparationRun.Reset();
    }
    private void OnDestroy()
    {
        replayButton.onClick.RemoveListener(Replay);
        menuButton.onClick.RemoveListener(MainMenu);
    }
}
