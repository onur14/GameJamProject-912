using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class DispatchOpeningNotice
{
    public static IEnumerator Show(Canvas canvas, TMP_FontAsset font)
    {
        var root = new GameObject("Açılış Bilgi Notu", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image), typeof(CanvasGroup));
        var rect = (RectTransform)root.transform; rect.SetParent(canvas.rootCanvas.transform, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var layer = root.GetComponent<Canvas>(); layer.overrideSorting = true; layer.sortingOrder = 1800;
        root.GetComponent<Image>().color = new Color(.015f,.025f,.04f,.99f);
        var group = root.GetComponent<CanvasGroup>();
        var textObject = new GameObject("Afet Riskini Azaltma Notu", typeof(RectTransform), typeof(TextMeshProUGUI));
        var text = textObject.GetComponent<TextMeshProUGUI>(); text.transform.SetParent(root.transform, false);
        var tr = text.rectTransform; tr.anchorMin = new Vector2(.13f,.27f); tr.anchorMax = new Vector2(.87f,.75f); tr.offsetMin = tr.offsetMax = Vector2.zero;
        text.font = font ? font : TMP_Settings.defaultFontAsset; text.fontStyle = FontStyles.Bold; text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true; text.fontSizeMin = 20; text.fontSizeMax = 38; text.raycastTarget = false;
        text.text = "Doğal tehlikelerin afete dönüşmesinde\n<color=#F2B54D>maruz kalma ve kırılganlık</color> belirleyicidir.\n\nHazırlık ve güvenli kararlarla riski azaltabiliriz.\n<size=65%>UNDRR yaklaşımından uyarlanmıştır</size>\n<color=#F2B54D>#NoNaturalDisasters</color>";
        var disclaimerObject = new GameObject("Kurgusal Numara Uyarısı", typeof(RectTransform), typeof(TextMeshProUGUI));
        var disclaimer = disclaimerObject.GetComponent<TextMeshProUGUI>(); disclaimer.transform.SetParent(root.transform, false);
        var disclaimerRect = disclaimer.rectTransform; disclaimerRect.anchorMin = new Vector2(.13f,.11f); disclaimerRect.anchorMax = new Vector2(.87f,.18f);
        disclaimerRect.offsetMin = disclaimerRect.offsetMax = Vector2.zero;
        disclaimer.font = text.font; disclaimer.fontSize = 20; disclaimer.alignment = TextAlignmentOptions.Center;
        disclaimer.color = new Color(.92f,.93f,.88f); disclaimer.raycastTarget = false;
        disclaimer.text = "912 kurgusal oyun merkezidir · Gerçek acil durumlarda 112'yi arayın.";
        var barObject = new GameObject("Işık Çizgisi", typeof(RectTransform), typeof(Image));
        var bar = (RectTransform)barObject.transform; bar.SetParent(root.transform, false);
        bar.anchorMin = new Vector2(.13f,.22f); bar.anchorMax = new Vector2(.87f,.225f); bar.offsetMin = bar.offsetMax = Vector2.zero;
        barObject.GetComponent<Image>().color = new Color(.95f,.65f,.22f); barObject.GetComponent<Image>().raycastTarget = false;
        const float duration=5f;
        for (float t=0; t<duration; t+=Time.unscaledDeltaTime)
        {
            float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.5f));
            group.alpha=Mathf.Min(enter,Mathf.Clamp01((duration-t)/.5f));
            tr.anchoredPosition=Vector2.up*(1-enter)*45;
            tr.localScale=Vector3.one*Mathf.Lerp(1.06f,1,Mathf.Clamp01(t/(duration-.5f)));
            bar.localScale=new Vector3(Mathf.Clamp01(t/(duration-.5f)),1,1);
            yield return null;
        }
        Object.Destroy(root);
    }
}
