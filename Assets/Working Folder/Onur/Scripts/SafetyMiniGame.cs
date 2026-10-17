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

    private void Awake()
    {
        // Existing answer data stays in Inspector; interaction now happens on the artwork.
        if (optionAButton != null) optionAButton.gameObject.SetActive(false);
        if (optionBButton != null) optionBButton.gameObject.SetActive(false);
        if (pointNameLabel != null) pointNameLabel.gameObject.SetActive(false);
        if (descriptionLabel != null) descriptionLabel.gameObject.SetActive(false);
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
        points[index].selectedOption = points[index].selectedOption < 0 ? 1 : 1 - points[index].selectedOption;
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
            else resultLabel.text = "Noktaları tekrar kontrol et";
        }

        Refresh();
    }

    private void Refresh()
    {
        attemptsLabel.text = string.Join(" ", new string('♥', attemptsLeft).ToCharArray());
        attemptsLabel.color = new Color(.94f, .20f, .25f);
        checkButton.interactable = !resolved;
        if (points == null) return;
        foreach (ControlPoint point in points)
        {
            if (point == null) continue;
            if (point.hotspotButton != null) point.hotspotButton.interactable = !resolved;
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
