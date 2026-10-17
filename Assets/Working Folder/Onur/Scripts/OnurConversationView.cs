using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OnurConversationView : MonoBehaviour
{
    [SerializeField] private TMP_Text callerName;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button approveButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private TMP_Text approveLabel;
    [SerializeField] private TMP_Text rejectLabel;
    [SerializeField] private RectTransform messageTemplate;
    [SerializeField] private float messageGap = 12f;
    [SerializeField] private float sidePadding = 18f;
    [SerializeField] private float bottomPadding = 14f;
    [SerializeField] private float choiceSpace = 165f;
    [SerializeField] private Color callerColor = Color.white;
    [SerializeField] private Color operatorColor = new Color(0.82f, 0.91f, 1f);

    private readonly List<RectTransform> messages = new List<RectTransform>();
    private OyunAkisiOnur hikaye;
    private RectTransform panel;
    private bool choicesVisible;

    public void Bind(OyunAkisiOnur akis)
    {
        hikaye = akis;
        panel = (RectTransform)transform;
        messageTemplate.gameObject.SetActive(false);
        Clear();
    }

    public IEnumerator ShowMessage(string message, bool fromRight, float duration)
    {
        int separator = message.IndexOf(':');
        RectTransform bubble = Instantiate(messageTemplate, panel);
        bubble.gameObject.name = fromRight ? "Operatör Mesajı" : "Kazazede Mesajı";
        bubble.gameObject.SetActive(true);
        bubble.SetAsLastSibling();
        TMP_Text nameText = bubble.Find(callerName.transform.name).GetComponent<TMP_Text>();
        TMP_Text bodyText = nameText.transform.Find(dialogueText.transform.name).GetComponent<TMP_Text>();
        nameText.text = separator < 0 ? string.Empty : message.Substring(0, separator).ToUpperInvariant();
        bodyText.text = separator < 0 ? message : message.Substring(separator + 1).TrimStart();
        bubble.GetComponent<Image>().color = fromRight ? operatorColor : callerColor;

        float width = Mathf.Min(panel.rect.width * 0.82f, 620f);
        float bodyHeight = bodyText.GetPreferredValues(bodyText.text, width - 40f, 0f).y;
        bodyText.rectTransform.sizeDelta = new Vector2(0f, Mathf.Max(42f, bodyHeight));
        float height = Mathf.Max(94f, bodyText.rectTransform.sizeDelta.y + 64f);
        bubble.anchorMin = bubble.anchorMax = new Vector2(0.5f, 0.5f);
        bubble.pivot = new Vector2(0.5f, 0.5f);
        bubble.sizeDelta = new Vector2(width, height);

        float endX = (fromRight ? 1f : -1f) * (panel.rect.width / 2f - width / 2f - sidePadding);
        float startX = (fromRight ? 1f : -1f) * (panel.rect.width / 2f + width / 2f);
        float endY = panel.rect.yMin + (choicesVisible ? choiceSpace : bottomPadding) + height / 2f;
        Vector2[] previousPositions = new Vector2[messages.Count];
        for (int index = 0; index < messages.Count; index++)
            previousPositions[index] = messages[index].anchoredPosition;

        float elapsed = 0f;
        float slideDuration = Mathf.Max(0.01f, duration);
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration));
            bubble.anchoredPosition = new Vector2(Mathf.Lerp(startX, endX, progress), endY);
            for (int index = 0; index < messages.Count; index++)
                messages[index].anchoredPosition = previousPositions[index] + Vector2.up * ((height + messageGap) * progress);
            yield return null;
        }

        bubble.anchoredPosition = new Vector2(endX, endY);
        for (int index = 0; index < messages.Count; index++)
            messages[index].anchoredPosition = previousPositions[index] + Vector2.up * (height + messageGap);
        messages.Add(bubble);
        RemovePastTop();
    }

    public void SetChoices(string approve, string reject)
    {
        approveLabel.text = approve;
        rejectLabel.text = reject;
        SetChoicesVisible(true);
    }

    public void SetChoicesVisible(bool visible)
    {
        if (visible && !choicesVisible && messages.Count > 0)
        {
            RectTransform latest = messages[messages.Count - 1];
            float lowestMessageEdge = latest.anchoredPosition.y - latest.rect.height / 2f;
            float choiceTop = panel.rect.yMin + choiceSpace;
            float shift = Mathf.Max(0f, choiceTop + messageGap - lowestMessageEdge);
            foreach (RectTransform bubble in messages)
                bubble.anchoredPosition += Vector2.up * shift;
            RemovePastTop();
        }

        choicesVisible = visible;
        approveButton.gameObject.SetActive(visible);
        rejectButton.gameObject.SetActive(visible);
    }

    public void Clear()
    {
        foreach (RectTransform bubble in messages)
        {
            bubble.gameObject.SetActive(false);
            Destroy(bubble.gameObject);
        }

        messages.Clear();
        callerName.text = string.Empty;
        dialogueText.text = string.Empty;
        SetChoicesVisible(false);
    }

    private void RemovePastTop()
    {
        while (messages.Count > 0 && messages[0].anchoredPosition.y - messages[0].rect.height / 2f >= panel.rect.yMax)
        {
            Destroy(messages[0].gameObject);
            messages.RemoveAt(0);
        }
    }

    public void KabulEdildi()
    {
        if (hikaye != null) hikaye.KabulEdildi();
    }

    public void Reddedildi()
    {
        if (hikaye != null) hikaye.Reddedildi();
    }
}
