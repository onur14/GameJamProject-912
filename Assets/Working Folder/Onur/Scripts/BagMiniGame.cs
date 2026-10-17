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

    public RectTransform DragLayer => dragLayer;
    public int AttemptsLeft => attemptsLeft;
    public bool IsResolved => resolved;

    private void Awake()
    {
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
        lastCheckTime = -10f;
        if (items != null)
            foreach (BagDraggableItem item in items)
                if (item != null) item.ResetPlacement();
        resultLabel.text = string.Empty;
        Refresh();
    }

    public void Check()
    {
        if (resolved || Time.unscaledTime - lastCheckTime < .3f) return;
        lastCheckTime = Time.unscaledTime;

        bool correct = items != null && items.Length > 0;
        if (correct)
            foreach (BagDraggableItem item in items)
                if (item == null || !item.IsCorrectlyPlaced) { correct = false; break; }

        if (correct)
        {
            resolved = true;
            int gainedHealth = window.Award(rewardPoints);
            resultLabel.text = gainedHealth > 0
                ? $"Afet çantası tamamlandı. Şehir sağlığı +{gainedHealth}."
                : "Afet çantası tamamlandı. Şehir sağlığı değişmedi.";
        }
        else
        {
            attemptsLeft--;
            if (attemptsLeft <= 0)
            {
                resolved = true;
                resultLabel.text = "Minigame başarısız. Şehir sağlığı kazanılmadı.";
            }
            else resultLabel.text = "Yerleşim eksik veya hatalı. Düzeltip tekrar kontrol edin.";
        }

        Refresh();
    }

    private void Refresh()
    {
        attemptsLabel.text = $"Kalan hak: {attemptsLeft}";
        checkButton.interactable = !resolved;
    }
}
