using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class OnurConversationView : MonoBehaviour, IPointerClickHandler
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
    [SerializeField] private Color operatorColor = new Color(.82f,.91f,1);
    [Header("Automatic conversation pace")]
    [SerializeField, Min(0)] private float charactersPerSecond = 38;
    [SerializeField] private DialogueTypingAudio typingAudio = new DialogueTypingAudio();
    [SerializeField, Range(.8f,1.5f)] private float callEndedDuration = 1.05f;
    [SerializeField, Range(.15f,.4f)] private float clickDebounce = .22f;
    private const float ConnectionGainPerPress = .12f;
    private const float ConnectionDecayPerSecond = .07f;
    private OyunAkisiOnur hikaye;
    private RectTransform panel, textViewport, textContent;
    private ScrollRect scroll;
    private Button next;
    private TMP_Text nextLabel, effectA, effectB, latestBody;
    private TMP_Text callEndedLabel, newCallLabel;
    private GameObject connectionOverlay;
    private RectTransform connectionFill;
    private readonly List<RectTransform> messages = new List<RectTransform>();
    private bool choicesVisible, awaitingAdvance, typing, bothUnavailable;
    private float lastClick = -10;
    public event System.Action ChoicesShown;
    public bool IsPaused { get; private set; }
    public void SetPaused(bool paused) { IsPaused = paused; }
    public bool WaitingForAdvance => awaitingAdvance;
    public bool IsTyping => typing;
    public bool IsCallEndedVisible => callEndedLabel && callEndedLabel.gameObject.activeSelf;
    public Button AdvanceButton => next;
    public Button ChoiceAButton => approveButton;
    public Button ChoiceBButton => rejectButton;
    public TMP_Text MessageText => latestBody ? latestBody : dialogueText;
    public TMP_Text EffectA => effectA;
    public TMP_Text EffectB => effectB;
    public bool HasScrollableText => textViewport && textContent.rect.height > textViewport.rect.height + 1;

    public void Bind(OyunAkisiOnur akis)
    {
        if(hikaye != null && hikaye.Resources != null) hikaye.Resources.Changed -= UpdateEffects;
        hikaye = akis; panel = (RectTransform)transform;
        if(next == null) BuildChat();
        if(hikaye.Resources != null) hikaye.Resources.Changed += UpdateEffects;
        Clear();
    }
    private void OnDestroy()
    {
        if(hikaye != null && hikaye.Resources != null) hikaye.Resources.Changed -= UpdateEffects;
    }
    private void OnDisable(){typing=false;typingAudio.Stop();}
    private static void Anchor(RectTransform r, Vector2 min, Vector2 max)
    {
        r.anchorMin=min;r.anchorMax=max;r.pivot=new Vector2(.5f,.5f);r.offsetMin=r.offsetMax=Vector2.zero;
        r.localScale=Vector3.one;
    }
    private TMP_Text Text(string name, Transform parent, float size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var t=go.GetComponent<TMP_Text>();t.font=dialogueText.font;t.fontSize=size;t.raycastTarget=false;
        t.color=new Color(.80f,.87f,.84f);t.textWrappingMode=TextWrappingModes.Normal;
        return t;
    }
    private void BuildChat()
    {
        var background=GetComponent<Image>();if(background)background.color=new Color(.24f,.32f,.37f,.92f);
        messageTemplate.gameObject.SetActive(false);
        var viewport=new GameObject("Sohbet Alanı",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));
        textViewport=(RectTransform)viewport.transform;textViewport.SetParent(panel,false);
        viewport.GetComponent<Image>().color=new Color(0,0,0,.01f);
        scroll=viewport.GetComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;
        scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=26;
        var content=new GameObject("Mesaj Geçmişi",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));
        textContent=(RectTransform)content.transform;textContent.SetParent(textViewport,false);
        textContent.anchorMin=new Vector2(0,1);textContent.anchorMax=Vector2.one;textContent.pivot=new Vector2(.5f,1);
        textContent.sizeDelta=Vector2.zero;
        var layout=content.GetComponent<VerticalLayoutGroup>();layout.spacing=messageGap;
        layout.padding=new RectOffset(0,0,6,Mathf.RoundToInt(bottomPadding));
        layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport=textViewport;scroll.content=textContent;
        // Preserve the existing no-team resolution action; ordinary messages have no advance button.
        var go=new GameObject("Sevk Yapılamadı",typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(panel,false);
        Anchor((RectTransform)go.transform,new Vector2(.035f,.015f),new Vector2(.965f,.095f));
        go.GetComponent<Image>().color=new Color(.10f,.17f,.20f);next=go.GetComponent<Button>();next.targetGraphic=go.GetComponent<Image>();
        nextLabel=Text("Sevk Durumu",go.transform,19);Anchor(nextLabel.rectTransform,Vector2.zero,Vector2.one);nextLabel.alignment=TextAlignmentOptions.Center;
        next.onClick.AddListener(Advance);next.gameObject.SetActive(false);
        effectA=Text("Gerçek Kaynak Etkisi A",approveButton.transform,21);
        effectB=Text("Gerçek Kaynak Etkisi B",rejectButton.transform,21);
        ConfigureChoice(approveButton,approveLabel,effectA,false);ConfigureChoice(rejectButton,rejectLabel,effectB,true);
        Layout();
    }
    private void ConfigureChoice(Button button,TMP_Text label,TMP_Text effect,bool right)
    {
        button.transform.SetParent(panel,false);
        Anchor((RectTransform)button.transform,new Vector2(right ? .51f : .035f,.115f),new Vector2(right ? .965f : .49f,.60f));
        Anchor(label.rectTransform,new Vector2(.055f,.27f),new Vector2(.945f,.95f));
        label.enableAutoSizing=true;label.fontSizeMin=23;label.fontSizeMax=26;label.fontSize=26;
        label.alignment=TextAlignmentOptions.TopLeft;label.margin=Vector4.zero;label.raycastTarget=false;
        label.textWrappingMode=TextWrappingModes.Normal;label.overflowMode=TextOverflowModes.Overflow;
        label.color=new Color(.88f,.92f,.87f);
        Anchor(effect.rectTransform,new Vector2(.055f,.025f),new Vector2(.945f,.25f));
        effect.enableAutoSizing=true;effect.fontSizeMin=18;effect.fontSizeMax=21;effect.color=new Color(.96f,.73f,.37f);
        if(button.targetGraphic)button.targetGraphic.color=new Color(.11f,.18f,.21f);
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.22f,1.26f,1.28f);
        colors.selectedColor=colors.highlightedColor;colors.fadeDuration=.12f;
        colors.pressedColor=new Color(.72f,.77f,.80f);colors.disabledColor=new Color(.45f,.45f,.45f);button.colors=colors;
        var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
    }
    private bool AcceptClick()
    {
        if(Time.unscaledTime-lastClick<clickDebounce)return false;
        lastClick=Time.unscaledTime;return true;
    }
    private void Advance()
    {
        if(!AcceptClick())return;
        if(choicesVisible)
        {
            if(bothUnavailable)hikaye.ResolveWithoutDispatch();
            return;
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button==PointerEventData.InputButton.Left && typing && AcceptClick())RevealCurrentLine();
    }
    public void RevealCurrentLine()
    {
        if(!typing)return;
        typing=false;MessageText.maxVisibleCharacters=int.MaxValue;typingAudio.Stop();
    }
    public IEnumerator ShowMessage(string message,bool fromRight,float duration)
    {
        SetChoicesVisible(false);awaitingAdvance=true;
        var row=new GameObject(fromRight?"Operatör Mesajı":"Kazazede Mesajı",typeof(RectTransform),typeof(LayoutElement));
        var rowRect=(RectTransform)row.transform;rowRect.SetParent(textContent,false);messages.Add(rowRect);
        var bubble=Instantiate(messageTemplate,rowRect,false);bubble.gameObject.SetActive(true);
        var nameText=bubble.Find(callerName.transform.name).GetComponent<TMP_Text>();
        var body=nameText.transform.Find(dialogueText.transform.name).GetComponent<TMP_Text>();
        latestBody=body;
        int separator=message.IndexOf(':');
        nameText.text=separator<0?string.Empty:message.Substring(0,separator).ToUpperInvariant();
        body.text=separator<0?message:message.Substring(separator+1).TrimStart();
        var image=bubble.GetComponent<Image>();if(image)image.color=fromRight?operatorColor:callerColor;
        nameText.enableAutoSizing=false;nameText.fontSize=18;nameText.color=new Color(.12f,.20f,.26f);
        body.enableAutoSizing=false;body.fontSize=26;body.color=new Color(.09f,.15f,.20f);
        body.textWrappingMode=TextWrappingModes.Normal;body.overflowMode=TextOverflowModes.Overflow;
        nameText.raycastTarget=false;body.raycastTarget=false;
        float width=Mathf.Min(textViewport.rect.width*.86f,620f);
        body.margin=Vector4.zero;
        float bodyHeight=Mathf.Max(32,body.GetPreferredValues(body.text,width-32,0).y+4);
        float height=bodyHeight+42;
        bubble.anchorMin=bubble.anchorMax=new Vector2(fromRight?1:0,1);bubble.pivot=new Vector2(fromRight?1:0,1);
        bubble.sizeDelta=new Vector2(width,height);bubble.anchoredPosition=new Vector2(fromRight?-sidePadding:sidePadding,0);
        nameText.rectTransform.anchorMin=new Vector2(0,1);nameText.rectTransform.anchorMax=Vector2.one;
        nameText.rectTransform.pivot=new Vector2(.5f,1);nameText.rectTransform.sizeDelta=new Vector2(-32,24);
        nameText.rectTransform.anchoredPosition=new Vector2(0,-8);
        // The authored prefab nests the message under its speaker label.
        body.rectTransform.anchorMin=new Vector2(0,1);body.rectTransform.anchorMax=Vector2.one;
        body.rectTransform.pivot=new Vector2(.5f,1);body.rectTransform.sizeDelta=new Vector2(0,bodyHeight);
        body.rectTransform.anchoredPosition=new Vector2(0,-27);
        row.GetComponent<LayoutElement>().preferredHeight=height;
        typing=charactersPerSecond>0;
        body.maxVisibleCharacters=typing?0:int.MaxValue;
        typingAudio.BeginLine();
        FitText();
        Vector2 destination=bubble.anchoredPosition;
        Vector2 start=destination+Vector2.right*(fromRight?1:-1)*(width+sidePadding*2);
        float slideTime=Mathf.Clamp(duration,.12f,.25f);
        bubble.anchoredPosition=start;
        for(float elapsed=0;elapsed<slideTime;)
        {
            if(IsPaused){yield return null;continue;}
            float progress=Mathf.Clamp01(elapsed/slideTime);
            bubble.anchoredPosition=Vector2.LerpUnclamped(start,destination,1-Mathf.Pow(1-progress,3));
            elapsed+=Time.unscaledDeltaTime;
            yield return null;
        }
        bubble.anchoredPosition=destination;
        if(typing)
        {
            body.ForceMeshUpdate();int count=body.textInfo.characterCount;float visible=0;
            while(typing && visible<count)
            {
                if(IsPaused){yield return null;continue;}
                var keyboard=Keyboard.current;
                if(keyboard!=null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                {RevealCurrentLine();break;}
                visible+=Time.unscaledDeltaTime*charactersPerSecond;body.maxVisibleCharacters=(int)visible;
                typingAudio.Reveal(body,body.maxVisibleCharacters,gameObject);
                yield return null;
            }
        }
        typing=false;body.maxVisibleCharacters=int.MaxValue;typingAudio.Stop();
    }
    public IEnumerator WaitForAdvance()
    {
        float displayTime = Mathf.Clamp(MessageText.text.Length * .012f, .8f, 1.8f);
        while(displayTime>0f || IsPaused)
        {
            if(!IsPaused)displayTime-=Time.unscaledDeltaTime;
            yield return null;
        }
        awaitingAdvance = false;
    }
    public IEnumerator ShowCallEnded()
    {
        SetChoicesVisible(false);awaitingAdvance=false;RevealCurrentLine();
        if(!callEndedLabel)
        {
            callEndedLabel=Text("Çağrı Durumu",panel,22);
            Anchor(callEndedLabel.rectTransform,new Vector2(.10f,.02f),new Vector2(.90f,.10f));
            callEndedLabel.alignment=TextAlignmentOptions.Center;
            callEndedLabel.color=new Color(.72f,.90f,.89f);
        }
        Anchor(textViewport,new Vector2(.015f,.125f),new Vector2(.985f,.98f));FitText();
        callEndedLabel.text="— ÇAĞRI SONLANDIRILDI —";
        callEndedLabel.gameObject.SetActive(true);callEndedLabel.transform.SetAsLastSibling();
        for(float t=0;t<callEndedDuration;)
        {
            if(IsPaused){yield return null;continue;}
            float p=t/callEndedDuration;
            callEndedLabel.alpha=Mathf.Min(Mathf.Clamp01(p/.12f),Mathf.Clamp01((1-p)/.25f));
            t+=Time.unscaledDeltaTime;
            yield return null;
        }
        callEndedLabel.gameObject.SetActive(false);
    }
    public IEnumerator ShowNewCall()
    {
        SetChoicesVisible(false);
        if (newCallLabel == null)
        {
            newCallLabel = Text("Yeni Arama Başlığı", panel, 64);
            Anchor(newCallLabel.rectTransform, new Vector2(.03f, .28f), new Vector2(.97f, .72f));
            newCallLabel.alignment = TextAlignmentOptions.Center;
            newCallLabel.fontStyle = FontStyles.Bold;
            newCallLabel.color = new Color(.45f, 1f, .85f);
            newCallLabel.raycastTarget = false;
            newCallLabel.text = "YENİ ARAMA";
        }
        newCallLabel.gameObject.SetActive(true);
        newCallLabel.transform.SetAsLastSibling();
        for (float t = 0; t < .9f;)
        {
            if (IsPaused) { yield return null; continue; }
            float p = t / .9f;
            newCallLabel.alpha = Mathf.Min(Mathf.Clamp01(p / .14f), Mathf.Clamp01((1 - p) / .24f));
            newCallLabel.rectTransform.localScale = Vector3.one * (1 + .12f * (1 - Mathf.Clamp01(p / .25f)));
            newCallLabel.rectTransform.anchoredPosition = new Vector2(30 * Mathf.Pow(1 - Mathf.Clamp01(p / .25f), 3), 0);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        newCallLabel.gameObject.SetActive(false);
    }

    public IEnumerator RecoverConnection(bool clearHistory = true)
    {
        if (clearHistory) Clear();
        if(connectionOverlay==null)BuildConnectionOverlay();
        connectionFill.anchorMax=new Vector2(0f,1f);
        connectionOverlay.SetActive(true);
        connectionOverlay.transform.SetAsLastSibling();
        float progress=0;
        while(progress<1f)
        {
            if(IsPaused){yield return null;continue;}
            if(Keyboard.current!=null && Keyboard.current.spaceKey.wasPressedThisFrame)
                progress=Mathf.Min(1f,progress+ConnectionGainPerPress);
            else
                progress=Mathf.Max(0f,progress-ConnectionDecayPerSecond*Time.unscaledDeltaTime);
            connectionFill.anchorMax=new Vector2(progress,1f);
            yield return null;
        }
        connectionOverlay.SetActive(false);
    }
    private void BuildConnectionOverlay()
    {
        connectionOverlay=new GameObject("Bağlantı Kurtarma",typeof(RectTransform),typeof(Image));
        var overlayRect=(RectTransform)connectionOverlay.transform;overlayRect.SetParent(panel,false);
        Anchor(overlayRect,Vector2.zero,Vector2.one);
        connectionOverlay.GetComponent<Image>().color=new Color(.04f,.09f,.12f,.97f);

        var title=Text("Bağlantı Uyarısı",overlayRect,34);
        Anchor(title.rectTransform,new Vector2(.08f,.54f),new Vector2(.92f,.72f));
        title.text="Bağlantı kesildi";title.alignment=TextAlignmentOptions.Center;
        title.color=new Color(1f,.65f,.4f);

        var instruction=Text("Kurtarma Talimatı",overlayRect,19);
        Anchor(instruction.rectTransform,new Vector2(.08f,.42f),new Vector2(.92f,.52f));
        instruction.text="SPACE tuşuna art arda bas";instruction.alignment=TextAlignmentOptions.Center;

        var bar=new GameObject("Bağlantı Çubuğu",typeof(RectTransform),typeof(Image));
        var barRect=(RectTransform)bar.transform;barRect.SetParent(overlayRect,false);
        Anchor(barRect,new Vector2(.12f,.30f),new Vector2(.88f,.37f));
        bar.GetComponent<Image>().color=new Color(.12f,.19f,.22f);
        bar.GetComponent<Image>().raycastTarget=false;

        var fill=new GameObject("İlerleme",typeof(RectTransform),typeof(Image));
        connectionFill=(RectTransform)fill.transform;connectionFill.SetParent(barRect,false);
        Anchor(connectionFill,Vector2.zero,new Vector2(0f,1f));
        fill.GetComponent<Image>().color=new Color(.42f,.87f,.69f);
        fill.GetComponent<Image>().raycastTarget=false;
    }
    private void FitText()
    {
        if(textViewport==null)return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(textContent);
        scroll.StopMovement();scroll.verticalNormalizedPosition=0;
    }
    private void Layout()
    {
        if(textViewport==null)return;
        Anchor(textViewport,new Vector2(.015f,choicesVisible ? .625f : .02f),new Vector2(.985f,.98f));
        FitText();
    }
    public void SetChoices(string approve,string reject)
    {
        approveLabel.text=approve;rejectLabel.text=reject;awaitingAdvance=false;
        SetChoicesVisible(true);UpdateEffects();
        ChoicesShown?.Invoke();
    }
    private void UpdateEffects()
    {
        if(!choicesVisible || hikaye==null || hikaye.Resources==null)return;
        var r=hikaye.Resources;var a=hikaye.GetChoiceEffect(false);var b=hikaye.GetChoiceEffect(true);
        effectA.text=a.Describe(r);effectB.text=b.Describe(r);
        approveButton.interactable=a.CanApply(r);rejectButton.interactable=b.CanApply(r);
        bothUnavailable=!approveButton.interactable&&!rejectButton.interactable;
        next.gameObject.SetActive(bothUnavailable);
        nextLabel.text="EKİP YOK · Sevk yapılamadı — vakayı kaydet ve ilerle";
    }
    public void SetChoicesVisible(bool visible)
    {
        choicesVisible=visible;approveButton.gameObject.SetActive(visible);rejectButton.gameObject.SetActive(visible);
        if(next!=null)next.gameObject.SetActive(false);
        Layout();
    }
    public void Clear()
    {
        awaitingAdvance=false;typing=false;bothUnavailable=false;
        typingAudio.Stop();if(callEndedLabel)callEndedLabel.gameObject.SetActive(false);
        if(newCallLabel)newCallLabel.gameObject.SetActive(false);
        if(connectionOverlay!=null)connectionOverlay.SetActive(false);
        foreach(var row in messages)if(row){row.gameObject.SetActive(false);Destroy(row.gameObject);}
        messages.Clear();latestBody=null;SetChoicesVisible(false);
    }
    public void KabulEdildi(){if(hikaye!=null && approveButton.interactable && AcceptClick())hikaye.KabulEdildi();}
    public void Reddedildi(){if(hikaye!=null && rejectButton.interactable && AcceptClick())hikaye.Reddedildi();}
}
