using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class CallerInfoView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text locationLabel;
    [SerializeField] private TMP_Text emergencyLabel;
    [SerializeField] private TMP_Text summaryLabel;
    [SerializeField] private bool sentenceMode;

    private Coroutine arrival;
    private bool textPrepared;
    private Vector2 restingPosition;
    private readonly Dictionary<TMP_FontAsset,Material> textMaterials=new Dictionary<TMP_FontAsset,Material>();

    private void Awake(){restingPosition=((RectTransform)transform).anchoredPosition;}

    public void Show(DispatchScenarioData scenario)
    {
        if (scenario == null) { Clear(); return; }
        PrepareText();
        if (sentenceMode)
        {
            Set(nameLabel, scenario.callerName, "");
            Set(locationLabel, scenario.callerLocation, "", " bölgesinden arıyor.");
            Set(emergencyLabel, scenario.emergencyType, "Olay: ", ".");
            Set(summaryLabel, scenario.situationSummary, "", EndsSentence(scenario.situationSummary) ? "" : ".");
        }
        else
        {
            Set(nameLabel, scenario.callerName, "Arayan: ");
            Set(locationLabel, scenario.callerLocation, "Konum: ");
            Set(emergencyLabel, scenario.emergencyType, "Olay: ");
            Set(summaryLabel, scenario.situationSummary, "Durum: ");
        }
        gameObject.SetActive(true);
        if (arrival != null) StopCoroutine(arrival);
        arrival = StartCoroutine(AnimateArrival());
    }

    public void Clear()
    {
        if (arrival != null) StopCoroutine(arrival);
        arrival = null;
        gameObject.SetActive(false);
    }

    private IEnumerator AnimateArrival()
    {
        var rect = (RectTransform)transform;
        var fade = GetComponent<CanvasGroup>();
        if (!fade) fade = gameObject.AddComponent<CanvasGroup>();
        fade.blocksRaycasts = false;
        for (float t = 0; t < .28f; t += Time.unscaledDeltaTime)
        {
            float p = Mathf.Clamp01(t / .28f); fade.alpha = p;
            rect.anchoredPosition = restingPosition + Vector2.left * Mathf.Round(18 * Mathf.Pow(1 - p, 3));
            yield return null;
        }
        rect.anchoredPosition = restingPosition; fade.alpha = 1; arrival = null;
    }

    private static bool EndsSentence(string s) { s = s == null ? "" : s.TrimEnd(); return s.EndsWith(".") || s.EndsWith("!") || s.EndsWith("?"); }

    private void PrepareText()
    {
        if(textPrepared)return;textPrepared=true;
        Layout(nameLabel,.80f,.91f);
        Layout(locationLabel,.66f,.78f);
        Layout(emergencyLabel,.51f,.64f);
        Layout(summaryLabel,.08f,.48f);
        foreach(var label in new[]{nameLabel,locationLabel,emergencyLabel,summaryLabel})
        {
            if(!label)continue;
            float size=label.enableAutoSizing?label.fontSizeMax:label.fontSize;
            label.fontSizeMax=Mathf.Min(size,18);label.fontSizeMin=12;label.enableAutoSizing=true;
            label.fontStyle=label==nameLabel?FontStyles.Bold:FontStyles.Normal;label.fontWeight=FontWeight.Regular;
            label.textWrappingMode=TextWrappingModes.Normal;label.lineSpacing=4;label.characterSpacing=.15f;
            label.color=new Color(.92f,.96f,.93f);label.extraPadding=true;
            if(label.font)
            {
                if(!textMaterials.TryGetValue(label.font,out var material))
                {
                    material=new Material(label.font.material){name="Caller text / restrained weight"};
                    material.SetFloat("_WeightNormal",.20f);material.SetFloat("_WeightBold",.24f);
                    material.SetFloat("_FaceDilate",.025f);material.SetFloat("_Sharpness",.15f);textMaterials.Add(label.font,material);
                }
                label.fontSharedMaterial=material;
            }
        }
    }
    private static void Layout(TMP_Text label,float bottom,float top)
    {
        if(!label)return;
        var rect=label.rectTransform;
        rect.anchorMin=new Vector2(.07f,bottom);rect.anchorMax=new Vector2(.93f,top);
        rect.offsetMin=rect.offsetMax=Vector2.zero;
        label.alignment=TextAlignmentOptions.TopLeft;
        label.overflowMode=TextOverflowModes.Truncate;
    }
    private void OnDestroy(){foreach(var material in textMaterials.Values)if(material)Destroy(material);}

    private static void Set(TMP_Text label, string value, string prefix, string suffix = "")
    {
        if (label == null) return;
        label.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
        label.text = string.IsNullOrWhiteSpace(value) ? string.Empty : prefix + value + suffix;
    }
}
