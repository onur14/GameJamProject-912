using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BagMiniGame : MonoBehaviour
{
    [SerializeField, Min(1)] private int startingAttempts = 3;
    [SerializeField, Min(0)] private int rewardPoints = 10;
    [SerializeField] private MiniGameWindow window;
    [SerializeField] private RectTransform dragLayer;
    [SerializeField] private BagDraggableItem[] items;
    [SerializeField] private TMP_Text attemptsLabel;
    [SerializeField] private TMP_Text resultLabel;
    [SerializeField] private Button checkButton;
    [SerializeField] private Button closeButton;

    private int attemptsLeft;
    private bool resolved;
    private float lastCheckTime = -10f;
    private BagDropZone bagArea;
    public BagDropZone BagArea => bagArea;
    public void OnItemPlacement(bool correct) { if (!resolved) window.EvaluateAction(correct); }
    public void HighlightBag(bool active) { if (bagArea != null) bagArea.SetHighlighted(active); }

    public RectTransform DragLayer => dragLayer;
    public int AttemptsLeft => attemptsLeft;
    public bool IsResolved => resolved;
    public bool Succeeded { get; private set; }
    public void CancelDrags()
    {
        if (items == null) return;
        foreach (var item in items) if (item != null) item.CancelDrag();
    }

    private void Awake()
    {
        foreach (var zone in GetComponentsInChildren<BagDropZone>(true))
            if (zone.IsBagArea) { bagArea = zone; break; }
        checkButton.onClick.AddListener(Check);
        closeButton.onClick.AddListener(window.Close);
        ResetForNewSession();
    }

    private void OnDestroy()
    {
        checkButton.onClick.RemoveListener(Check);
        closeButton.onClick.RemoveListener(window.Close);
    }

    public void ResetForNewSession()
    {
        attemptsLeft = Mathf.Max(1, startingAttempts);
        resolved = false;
        Succeeded = false;
        lastCheckTime = -10f;
        CancelDrags();
        if (items != null)
        {
            // Shuffle existing non-overlapping slots only in preparation.
            if(window.IsPreparationWindow)
                for(int i=items.Length-1;i>0;i--)
                {
                    int j=Random.Range(0,i+1);if(!items[i]||!items[j])continue;
                    Vector2 position=items[i].StartingPosition;items[i].StartingPosition=items[j].StartingPosition;items[j].StartingPosition=position;
                }
            foreach(BagDraggableItem item in items)if(item)item.ResetPlacement();
        }
        resultLabel.text = string.Empty;
        Refresh();
    }

    public void Check()
    {
        if (resolved || Time.unscaledTime - lastCheckTime < .3f) return;
        lastCheckTime = Time.unscaledTime;
        CancelDrags();

        bool correct = items != null && items.Length > 0;
        if (correct)
            foreach (BagDraggableItem item in items)
                if (item == null || !item.IsCorrectlyPlaced) { correct = false; break; }

        if (correct)
        {
            resolved = true;
            Succeeded = true;
            int gainedHealth = window.UsesImmediateFeedback ? 0 : window.Award(rewardPoints);
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
            else resultLabel.text = "Yerleşimi düzelt";
        }

        Refresh();
    }

    private void Refresh()
    {
        attemptsLabel.text = string.Join(" ", new string('♥', attemptsLeft).ToCharArray());
        attemptsLabel.color = new Color(.98f, .60f, .55f);
        checkButton.interactable = !resolved;
    }
}
