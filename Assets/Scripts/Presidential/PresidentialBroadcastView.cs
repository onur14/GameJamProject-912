using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PresidentialBroadcastView : MonoBehaviour
{
    public Sprite presidentPortrait;
    public TMP_FontAsset broadcastFont;
    [Min(0)] public float charactersPerSecond = 38;
    [SerializeField, Range(5,8)] private float speakingFramesPerSecond = 7;
    [SerializeField] private DialogueTypingAudio typingAudio = new DialogueTypingAudio();
    private BroadcastMouthGraphic mouth;
    public bool IsSpeaking { get; private set; }
    public void ConfigureTypingSound(AudioClip clip){typingAudio.clip=clip;typingAudio.volume=.045f;typingAudio.characterInterval=3;}
    private static readonly int[] MouthFrames = {0,1,2,1,0};
    public string thirdResourceLabel = "Kurtarma";
    public Action AdvanceRequested;
    public Action<int> OfferSelected;
    private RectTransform board;
    private CanvasGroup group;
    private TMP_Text header, body, stock, footer;
    private Button advance;
    private readonly Button[] offers = new Button[2];
    private readonly TMP_Text[] offerTexts = new TMP_Text[2];
    private Coroutine typing;
    private float lastClick = -10;
    private bool showingOffers;
    private string completeLine;
    private CanvasGroup shiftSummary;
    public bool IsShiftSummaryVisible => shiftSummary != null && shiftSummary.gameObject.activeSelf;
    private static readonly Color Ink = new Color(.035f, .055f, .075f);
    private static readonly Color Paper = new Color(.83f, .88f, .84f);
    private static readonly Color Amber = new Color(.96f, .69f, .30f);

    private void Awake() { Build(); Hide(); }
    private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        result.SetParent(parent, false);
        result.anchorMin = min; result.anchorMax = max;
        result.offsetMin = result.offsetMax = Vector2.zero;
        return result;
    }
    private Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var image = Rect(name, parent, min, max).gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }
    private TMP_Text Text(string name, Transform parent, Vector2 min, Vector2 max, float size, Color color)
    {
        var text = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = broadcastFont != null ? broadcastFont : TMP_Settings.defaultFontAsset;
        text.fontStyle |= FontStyles.Bold; text.fontWeight = FontWeight.Heavy;
        text.fontSize = size; text.color = color; text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }
    private Button Button(string name, Transform parent, Vector2 min, Vector2 max, out TMP_Text label)
    {
        var panel = Panel(name, parent, min, max, new Color(.10f,.16f,.19f));
        var button = panel.gameObject.AddComponent<Button>();
        button.targetGraphic = panel;
        var colors = button.colors;
        colors.normalColor = Color.white; colors.highlightedColor = new Color(1.25f,1.25f,1.15f);
        colors.pressedColor = new Color(.65f,.8f,.7f); colors.disabledColor = new Color(.45f,.45f,.45f);
        button.colors = colors;
        var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
        label = Text("Metin", panel.transform, new Vector2(.045f,.045f), new Vector2(.955f,.955f), 21, Paper);
        return button;
    }
    private void Build()
    {
        if (board != null) return;
        var blocker = Panel("Yayın Etkileşim Kilidi", transform, Vector2.zero, Vector2.one, new Color(0,0,0,.82f));
        group = blocker.gameObject.AddComponent<CanvasGroup>();
        board = Rect("Resmî Yayın Terminali", blocker.transform, new Vector2(.5f,.5f), new Vector2(.5f,.5f));
        board.sizeDelta = new Vector2(1140,760);
        Panel("Çerçeve", board, Vector2.zero, Vector2.one, new Color(.28f,.34f,.34f));
        Panel("Ekran", board, new Vector2(.006f,.009f), new Vector2(.994f,.991f), Ink);
        Text("Kurum", board, new Vector2(.035f,.925f), new Vector2(.97f,.98f), 23, Paper).text = "ULUSAL AFET KOORDİNASYON MERKEZİ";
        Panel("Yayın Çizgisi", board, new Vector2(.035f,.907f), new Vector2(.965f,.911f), Amber);
        header = Text("Başlık", board, new Vector2(.035f,.84f), new Vector2(.965f,.9f), 25, Amber);
        header.fontStyle = FontStyles.Bold;
        var portrait = Panel("Başkan Yayını", board, new Vector2(.035f,.465f), new Vector2(.49f,.825f), Color.white);
        portrait.sprite = presidentPortrait; portrait.preserveAspect = true; portrait.raycastTarget = false;
        var mouthObject=Rect("Başkan Konuşma",portrait.transform,Vector2.zero,Vector2.one).gameObject;
        mouthObject.AddComponent<CanvasRenderer>();
        mouth=mouthObject.AddComponent<BroadcastMouthGraphic>();
        mouth.Portrait=portrait;mouth.raycastTarget=false;
        if (!presidentPortrait)
        {
            portrait.color = Ink;
            Text("Eksik Görsel", portrait.transform, Vector2.zero, Vector2.one, 22, Paper).text = "Başkan yayını\nGörsel referansı bekleniyor";
        }
        body = Text("Konuşma", board, new Vector2(.52f,.46f), new Vector2(.965f,.825f), 25, Paper);
        body.fontStyle = FontStyles.Bold;
        body.lineSpacing = 2f;
        body.enableAutoSizing = true;
        body.fontSizeMin = 21;
        body.fontSizeMax = 25;
        stock = Text("Mevcut Kaynaklar", board, new Vector2(.035f,.365f), new Vector2(.965f,.44f), 23, Amber);
        offers[0] = Button("Plan A", board, new Vector2(.035f,.10f), new Vector2(.49f,.35f), out offerTexts[0]);
        offers[1] = Button("Plan B", board, new Vector2(.51f,.10f), new Vector2(.965f,.35f), out offerTexts[1]);
        offers[0].onClick.AddListener(() => Choose(0)); offers[1].onClick.AddListener(() => Choose(1));
        advance = Button("Devam", board, new Vector2(.735f,.025f), new Vector2(.965f,.085f), out var nextText);
        nextText.text = "DEVAM  >";
        nextText.alignment = TextAlignmentOptions.Center;
        advance.onClick.AddListener(Next);
        footer = Text("Durum", board, new Vector2(.035f,.025f), new Vector2(.71f,.085f), 19, Paper);
    }
    private void LateUpdate()
    {
        if (board == null) return;
        var area = ((RectTransform)transform).rect.size;
        board.localScale = Vector3.one * Mathf.Max(.01f, Mathf.Min(area.x / 1200, area.y / 800));
    }
    public IEnumerator ShowShiftSummary(int completed, int correct, int estimatedHelped, int health, bool finalShift = false)
    {
        Build(); Hide(); transform.SetAsLastSibling();
        if (shiftSummary != null) Destroy(shiftSummary.gameObject);
        var shade = Panel("Vardiya Özeti", transform, Vector2.zero, Vector2.one, new Color(.015f,.03f,.05f,.96f));
        shiftSummary = shade.gameObject.AddComponent<CanvasGroup>();
        var card = Rect("Özet Terminali", shade.transform, new Vector2(.08f,.17f), new Vector2(.92f,.83f));
        bool critical = health <= 40;
        Color accent = critical ? new Color(1,.34f,.25f) : health <= 65 ? Amber : new Color(.38f,.96f,.8f);
        var line = Panel("Sinyal Çizgisi", card, new Vector2(0,.91f), new Vector2(1,.923f), accent);
        var heading = Text("Vardiya Başlığı", card, new Vector2(0,.76f), new Vector2(1,.89f), 32, accent);
        heading.text = finalShift ? "GÜNLÜK VARDİYA SONU" : "VARDİYA ARA RAPORU";
        var count = Text("Yardım Sayacı", card, new Vector2(0,.49f), new Vector2(1,.74f), 60, Paper);
        var message = Text("Vardiya Değerlendirmesi", card, new Vector2(.02f,.15f), new Vector2(.98f,.46f), 25, Paper);
        message.alignment = count.alignment = heading.alignment = TextAlignmentOptions.Center;
        message.overflowMode = TextOverflowModes.Ellipsis;
        message.text = critical ? "Halk bilinci kritik! Son yönlendirmelerin yeterli değil. Bilgi kirliliğini durdur, kaynaklarını dikkatli yönet."
            : health <= 65 ? "Halk bilinci düşüyor. Teyitsiz bilgiye müdahale et ve yönlendirmelerini dikkatle seç."
            : "Sen ve ekibin doğru yönlendirmelerinizle şehre yardımcı oldunuz. Hazırlığı ve kaynak dengesini koru!";
        Text("Özet Alt Bilgi", card, new Vector2(.02f,0), new Vector2(.98f,.12f), 18, accent).text =
            $"HALK BİLİNCİ %{health} · {completed} VAKADA {correct} DOĞRU KARAR · BAŞKAN YAYINI BAĞLANIYOR";
        shiftSummary.blocksRaycasts = true;
        for (float t = 0; t < 5f; t += Time.unscaledDeltaTime)
        {
            float enter = Mathf.Clamp01(t / .3f);
            shiftSummary.alpha = Mathf.Min(enter, Mathf.Clamp01((5f - t) / .3f));
            card.anchoredPosition = Vector2.right * Mathf.Round(34 * Mathf.Pow(1 - enter, 3));
            card.localScale = Vector3.one * (1 + .025f * (1 - enter));
            line.color = Color.Lerp(accent, Color.white, Mathf.Max(0, Mathf.Sin(t * 18)) * Mathf.Clamp01(1 - t / .7f));
            int shown = Mathf.RoundToInt(estimatedHelped * Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .8f)));
            count.text = $"YAKLAŞIK {shown:N0} KİŞİ";
            yield return null;
        }
        shiftSummary.gameObject.SetActive(false);
    }

    public void Show(string title)
    {
        Build(); group.gameObject.SetActive(true); transform.SetAsLastSibling();
        header.text = title; group.alpha = 0; group.interactable = group.blocksRaycasts = true;
        StartCoroutine(Fade());
    }
    private IEnumerator Fade()
    {
        for (float t = 0; t < .2f; t += Time.unscaledDeltaTime) { group.alpha = t / .2f; yield return null; }
        group.alpha = 1;
    }
    public void Hide()
    {
        if (shiftSummary != null) shiftSummary.gameObject.SetActive(false);
        if (typing != null) StopCoroutine(typing);
        typing = null;
        StopSpeaking();
        if (group != null) group.gameObject.SetActive(false);
    }
    public void Line(string line, string resources)
    {
        body.rectTransform.anchorMin = new Vector2(.52f,.46f); body.rectTransform.anchorMax = new Vector2(.965f,.825f);
        body.fontSize = 25;
        stock.rectTransform.anchorMin = new Vector2(.035f,.365f); stock.rectTransform.anchorMax = new Vector2(.965f,.44f);
        showingOffers = false;
        foreach (var button in offers) button.gameObject.SetActive(false);
        advance.gameObject.SetActive(true); stock.text = resources;
        footer.text = "RESMÎ BİLGİLENDİRME · Tıklayarak ilerleyin";
        if (typing != null) StopCoroutine(typing);
        StopSpeaking();
        completeLine = line; body.text = line; body.maxVisibleCharacters = 0;
        typingAudio.BeginLine();
        typing = StartCoroutine(Type());
    }
    private IEnumerator Type()
    {
        body.ForceMeshUpdate(); int count = body.textInfo.characterCount;
        if (charactersPerSecond <= 0) { body.maxVisibleCharacters = int.MaxValue; typing = null; yield break; }
        float visible = 0, speakingTime = 0;
        IsSpeaking = count > 0;
        while (visible < count)
        {
            if(mouth)mouth.Frame=MouthFrames[(int)(speakingTime*speakingFramesPerSecond)%MouthFrames.Length];
            speakingTime+=Time.unscaledDeltaTime;
            visible += Time.unscaledDeltaTime * charactersPerSecond;
            body.maxVisibleCharacters = (int)visible;
            typingAudio.Reveal(body, body.maxVisibleCharacters, gameObject);
            yield return null;
        }
        body.maxVisibleCharacters = int.MaxValue; typing = null;
        StopSpeaking();
    }
    private void StopSpeaking(){IsSpeaking=false;if(mouth)mouth.Frame=0;typingAudio.Stop();}
    private void OnDisable(){if(typing!=null)StopCoroutine(typing);typing=null;StopSpeaking();}
    private bool AcceptClick()
    {
        if (Time.unscaledTime - lastClick < .3f) return false;
        lastClick = Time.unscaledTime; return true;
    }
    private void Next()
    {
        if (!AcceptClick()) return;
        if (typing != null)
        {
            StopCoroutine(typing); typing = null; body.text = completeLine;
            StopSpeaking();
            body.maxVisibleCharacters = int.MaxValue; return;
        }
        AdvanceRequested?.Invoke();
    }
    private void Choose(int index)
    {
        if (!showingOffers || !offers[index].interactable || !AcceptClick()) return;
        showingOffers = false;
        foreach (var button in offers) button.interactable = false;
        OfferSelected?.Invoke(index);
    }
    public string Stock(DispatchResources r) => $"MEVCUT  |  Ambulans {r.Ambulances}    İtfaiye {r.Firefighters}    {thirdResourceLabel} {r.Police}";
    private string Delta(int value) => value >= 0 ? "+" + value : value.ToString();
    public void Offers(ResourceOffer a, ResourceOffer b, DispatchResources r)
    {
        if (typing != null) StopCoroutine(typing);
        StopSpeaking();
        typing = null; showingOffers = true; body.maxVisibleCharacters = int.MaxValue;
        body.text = "KAYNAK TAHSİS KARARI\n\nİki plandan yalnızca biri uygulanabilir. Kararınız sonraki müdahalelerin kapasitesini belirleyecek.";
        stock.text = Stock(r); advance.gameObject.SetActive(false); footer.text = "Bir plan seçin. Tahsis geri alınamaz.";
        ResourceOffer[] plans = { a, b };
        for (int i = 0; i < 2; i++)
        {
            var p = plans[i]; bool valid = p.IsValid(r);
            offers[i].gameObject.SetActive(true); offers[i].interactable = valid;
            offerTexts[i].text = $"{(i == 0 ? "A" : "B")} · {p.title}\n{p.rationale}\nAmb {Delta(p.ambulances)} · İtf {Delta(p.firefighters)} · {thirdResourceLabel} {Delta(p.thirdTeam)}\nSONUÇ: Amb {r.Ambulances + p.ambulances} · İtf {r.Firefighters + p.firefighters} · {thirdResourceLabel} {r.Police + p.thirdTeam}";
            if (!valid)
            {
                string missing = r.Ambulances + (long)p.ambulances < 0 ? "Ambulans" : r.Firefighters + (long)p.firefighters < 0 ? "İtfaiye" : thirdResourceLabel;
                offerTexts[i].text += "\nSeçilemez: " + missing + " ekibi yetersiz.";
            }
        }
    }
    public string AllocationSummary(ResourceOffer p) => $"Ambulans {Delta(p.ambulances)} · İtfaiye {Delta(p.firefighters)} · {thirdResourceLabel} {Delta(p.thirdTeam)}";
    public void ConfirmAllocation() { StartCoroutine(ConfirmationPulse()); }
    private IEnumerator ConfirmationPulse()
    {
        for(float t=0;t<.22f;t+=Time.unscaledDeltaTime) { group.alpha=Mathf.Lerp(.65f,1,t/.22f);yield return null; }
        group.alpha=1;
    }
    public void Final(string report, string resources)
    {
        Show("VARDİYA SONU · GELECEK VARDİYAYA DEVİR");
        Line(report, resources);
        if (typing != null) StopCoroutine(typing);
        typing = null; body.maxVisibleCharacters = int.MaxValue;
        StopSpeaking();
        body.rectTransform.anchorMin = new Vector2(.52f,.12f); body.rectTransform.anchorMax = new Vector2(.965f,.825f);
        body.fontSize = 23;
        stock.rectTransform.anchorMin = new Vector2(.035f,.10f); stock.rectTransform.anchorMax = new Vector2(.49f,.43f);
        stock.text = resources.Replace("    ","\n");
        advance.gameObject.SetActive(false); footer.text = "VARDİYA TAMAMLANDI";
    }
}
