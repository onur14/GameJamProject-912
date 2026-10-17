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
        public Image stateImage;
        public Sprite stateA;
        public Sprite stateB;
        public TMP_Text completionTick;
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
    public bool Succeeded { get; private set; }

    private void Awake()
    {
        optionAButton.onClick.AddListener(() => SelectOption(0));
        optionBButton.onClick.AddListener(() => SelectOption(1));
        optionAButton.gameObject.SetActive(true);
        optionBButton.gameObject.SetActive(true);
        pointNameLabel.gameObject.SetActive(true);
        descriptionLabel.gameObject.SetActive(true);
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
        selectedPoint = points != null && points.Length > 0 ? 0 : -1;
        resolved = false;
        Succeeded = false;
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
        int firstIncorrect = -1;
        if (correct)
            for (int index = 0; index < points.Length; index++)
                if (points[index] == null || points[index].selectedOption != points[index].correctOption)
                { correct = false; firstIncorrect = index; break; }

        if (correct)
        {
            resolved = true;
            Succeeded = true;
            int gainedHealth = window.Award(rewardPoints);
            resultLabel.text = gainedHealth > 0
                ? $"Tamamlandı  +{gainedHealth}%"
                : "Tamamlandı";
        }
        else
        {
            attemptsLeft--;
            window.ShowMistake((RectTransform)transform);
            if (attemptsLeft <= 0)
            {
                resolved = true;
                resultLabel.text = "Hak kalmadı";
            }
            else
            {
                selectedPoint = firstIncorrect;
                resultLabel.text = "İşaretli tesisattaki kararı tekrar değerlendir.";
            }
        }

        Refresh();
    }

    private void Refresh()
    {
        attemptsLabel.text = string.Join(" ", new string('♥', attemptsLeft).ToCharArray());
        attemptsLabel.color = new Color(.94f, .20f, .25f);
        bool allAnswered = points != null && points.Length > 0;
        if (allAnswered)
            foreach (ControlPoint point in points)
                if (point == null || point.selectedOption < 0) { allAnswered = false; break; }
        checkButton.interactable = !resolved && allAnswered;
        bool hasPoint = points != null && selectedPoint >= 0 && selectedPoint < points.Length;
        optionAButton.interactable = optionBButton.interactable = hasPoint && !resolved;
        pointNameLabel.text = hasPoint ? points[selectedPoint].pointName : "Tesisat seç";
        descriptionLabel.text = hasPoint ? points[selectedPoint].description : "İncelemek için bir tesisata dokun.";
        optionALabel.text = hasPoint ? points[selectedPoint].optionA : string.Empty;
        optionBLabel.text = hasPoint ? points[selectedPoint].optionB : string.Empty;
        optionALabel.color = hasPoint && points[selectedPoint].selectedOption == 0 ? new Color(.94f,.75f,.43f) : new Color(.88f,.93f,.90f);
        optionBLabel.color = hasPoint && points[selectedPoint].selectedOption == 1 ? new Color(.94f,.75f,.43f) : new Color(.88f,.93f,.90f);
        if (points == null) return;
        foreach (ControlPoint point in points)
        {
            if (point == null) continue;
            if (point.hotspotButton != null) point.hotspotButton.interactable = !resolved;
            if (point.selectionMarker != null)
                point.selectionMarker.color = hasPoint && point == points[selectedPoint]
                    ? new Color(.35f, .9f, .92f, .95f)
                    : point.selectedOption >= 0 ? new Color(.9f, .7f, .25f, .95f)
                    : new Color(.21f, .41f, .53f, .9f);
            if (point.completionTick != null)
            {
                point.completionTick.text = point.selectedOption < 0 ? string.Empty : "✓";
                point.completionTick.color = Color.white; // Completed interaction, not correctness.
                var mark = point.completionTick.GetComponentInChildren<MiniGameCheckGraphic>(true);
                if (mark != null) mark.gameObject.SetActive(point.selectedOption >= 0);
            }
            if (point.stateImage != null)
            {
                Sprite sprite = point.selectedOption == 1 ? point.stateB : point.stateA;
                if (sprite != null) point.stateImage.sprite = sprite;
            }
        }
    }
}
