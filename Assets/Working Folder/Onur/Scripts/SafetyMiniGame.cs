using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SafetyMiniGame : MonoBehaviour
{
    [Serializable]
    public sealed class ControlPoint
    {
        public string pointName;
        [TextArea] public string description;
        public string optionA;
        public string optionB;
        [Range(0, 1)] public int correctOption;
        public Button hotspotButton;
        public Image selectionMarker;
        [NonSerialized] public int selectedOption = -1;
    }

    [SerializeField, Min(1)] private int startingAttempts = 3;
    [SerializeField, Min(0)] private int rewardPoints = 10;
    [SerializeField] private MiniGameWindow window;
    [SerializeField] private ControlPoint[] points;
    [SerializeField] private TMP_Text pointNameLabel;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private TMP_Text optionALabel;
    [SerializeField] private TMP_Text optionBLabel;
    [SerializeField] private TMP_Text attemptsLabel;
    [SerializeField] private TMP_Text resultLabel;
    [SerializeField] private Button optionAButton;
    [SerializeField] private Button optionBButton;
    [SerializeField] private Button checkButton;
    [SerializeField] private Button closeButton;

    private int attemptsLeft;
    private int selectedPoint = -1;
    private bool resolved;
    private float lastCheckTime = -10f;

    public int AttemptsLeft => attemptsLeft;
    public bool IsResolved => resolved;

    private void Awake()
    {
        optionAButton.onClick.AddListener(() => SelectOption(0));
        optionBButton.onClick.AddListener(() => SelectOption(1));
        checkButton.onClick.AddListener(Check);
        closeButton.onClick.AddListener(window.Close);
        if (points != null)
            for (int index = 0; index < points.Length; index++)
            {
                int pointIndex = index;
                if (points[index].hotspotButton != null)
                    points[index].hotspotButton.onClick.AddListener(() => SelectPoint(pointIndex));
            }
        ResetForNewSession();
    }

    public void ResetForNewSession()
    {
        attemptsLeft = Mathf.Max(1, startingAttempts);
        selectedPoint = -1;
        resolved = false;
        lastCheckTime = -10f;
        if (points != null)
            foreach (ControlPoint point in points)
                if (point != null) point.selectedOption = -1;
        resultLabel.text = string.Empty;
        Refresh();
    }

    public void SelectPoint(int index)
    {
        if (resolved || points == null || index < 0 || index >= points.Length || points[index] == null) return;
        selectedPoint = index;
        Refresh();
    }

    public void SelectOption(int option)
    {
        if (resolved || selectedPoint < 0 || option < 0 || option > 1) return;
        points[selectedPoint].selectedOption = option;
        Refresh();
    }

    public void Check()
    {
        if (resolved || Time.unscaledTime - lastCheckTime < .3f) return;
        lastCheckTime = Time.unscaledTime;

        bool correct = points != null && points.Length > 0;
        if (correct)
            foreach (ControlPoint point in points)
                if (point == null || point.selectedOption < 0 || point.selectedOption != point.correctOption)
                { correct = false; break; }

        if (correct)
        {
            resolved = true;
            int gainedHealth = window.Award(rewardPoints);
            resultLabel.text = gainedHealth > 0
                ? $"Ev güvenlik kontrolü tamamlandı. Şehir sağlığı +{gainedHealth}."
                : "Ev güvenlik kontrolü tamamlandı. Şehir sağlığı değişmedi.";
        }
        else
        {
            attemptsLeft--;
            if (attemptsLeft <= 0)
            {
                resolved = true;
                resultLabel.text = "Minigame başarısız. Şehir sağlığı kazanılmadı.";
            }
            else resultLabel.text = "Eksik veya hatalı seçim var. Düzeltip tekrar kontrol edin.";
        }

        Refresh();
    }

    private void Refresh()
    {
        attemptsLabel.text = $"Kalan hak: {attemptsLeft}";
        checkButton.interactable = !resolved;
        bool hasPoint = points != null && selectedPoint >= 0 && selectedPoint < points.Length;
        optionAButton.interactable = optionBButton.interactable = hasPoint && !resolved;
        pointNameLabel.text = hasPoint ? points[selectedPoint].pointName : "Kontrol noktası seçin";
        descriptionLabel.text = hasPoint ? points[selectedPoint].description : "Görsel üzerindeki işaretlerden birine tıklayın.";
        optionALabel.text = hasPoint ? points[selectedPoint].optionA : "Seçenek A";
        optionBLabel.text = hasPoint ? points[selectedPoint].optionB : "Seçenek B";
        optionALabel.color = hasPoint && points[selectedPoint].selectedOption == 0 ? Color.yellow : Color.white;
        optionBLabel.color = hasPoint && points[selectedPoint].selectedOption == 1 ? Color.yellow : Color.white;
        if (points == null) return;
        foreach (ControlPoint point in points)
        {
            if (point == null) continue;
            if (point.hotspotButton != null) point.hotspotButton.interactable = !resolved;
            if (point.selectionMarker != null)
                point.selectionMarker.color = point.selectedOption < 0
                    ? new Color(.21f, .41f, .53f, .9f)
                    : new Color(.9f, .7f, .25f, .95f);
        }
    }
}
