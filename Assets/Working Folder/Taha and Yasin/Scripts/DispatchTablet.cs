using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Presentation wrapper for the existing two minigames; it does not pause the dispatch flow.
public sealed class DispatchTablet : MonoBehaviour
{
    [SerializeField] private MiniGameWindow window;
    [SerializeField] private TMP_FontAsset font;
    private CanvasGroup layer;
    private RectTransform body, content;
    private TMP_Text recording, menuHint;
    private Button arrow;
    private Coroutine motion;
    private bool visible;
    public bool IsVisible => visible;
    public RectTransform Content => content;
    public Button Arrow => arrow;

    private void Start()
    {
        Build();
        window.Health.GameEnded += HideInstant;
    }
    private void OnDestroy()
    {
        if (window && window.Health) window.Health.GameEnded -= HideInstant;
    }
    private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.anchorMin = min; r.anchorMax = max;
        r.offsetMin = r.offsetMax = Vector2.zero; return r;
    }
    private Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var p = Rect(name,parent,min,max).gameObject.AddComponent<Image>(); p.color=color; return p;
    }
    private TMP_Text Text(string name, Transform parent, Vector2 min, Vector2 max, string value, float size)
    {
        var t=Rect(name,parent,min,max).gameObject.AddComponent<TextMeshProUGUI>();
        t.font=font ? font : TMP_Settings.defaultFontAsset; t.text=value; t.fontSize=size;
        t.fontStyle=FontStyles.Bold; t.color=new Color(.84f,.94f,.92f); t.raycastTarget=false;
        t.alignment=TextAlignmentOptions.Center; t.enableAutoSizing=true; t.fontSizeMin=12; t.fontSizeMax=size;
        return t;
    }
    private Button Button(string name, Transform parent, Vector2 min, Vector2 max, string label)
    {
        var image=Panel(name,parent,min,max,new Color(.09f,.24f,.29f));
        var b=image.gameObject.AddComponent<Button>(); b.targetGraphic=image;
        var nav=b.navigation; nav.mode=Navigation.Mode.None; b.navigation=nav;
        Text("Yazı",image.transform,Vector2.one*.06f,Vector2.one*.94f,label,21); return b;
    }
    private void Build()
    {
        var parent=GetComponentInParent<Canvas>().rootCanvas.transform;
        arrow=Button("Tableti Aç",parent,new Vector2(.797f,.225f),new Vector2(.843f,.292f),"^");
        arrow.onClick.AddListener(Show);
        var root=Rect("Kamu Yayını Tableti",parent,Vector2.zero,Vector2.one);
        var canvas=root.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=600;
        root.gameObject.AddComponent<GraphicRaycaster>();layer=root.gameObject.AddComponent<CanvasGroup>();
        var shade=Panel("Arka Plan",root,Vector2.zero,Vector2.one,new Color(0,.025f,.04f,.42f));
        body=Panel("Tablet Gövdesi",root,new Vector2(.055f,.03f),new Vector2(.945f,.97f),new Color(.025f,.04f,.055f)).rectTransform;
        Panel("İç Çerçeve",body,new Vector2(.009f,.012f),new Vector2(.991f,.988f),new Color(.24f,.39f,.42f)).raycastTarget=false;
        Panel("Tablet Ekranı",body,new Vector2(.015f,.021f),new Vector2(.985f,.979f),new Color(.045f,.095f,.12f)).raycastTarget=false;
        Text("Tablet Başlığı",body,new Vector2(.08f,.918f),new Vector2(.90f,.975f),"Kamu Duyuru Yayını Yapma Menüsü",26);
        var close=Button("Tableti Kapat",body,new Vector2(.92f,.92f),new Vector2(.975f,.975f),"X");close.onClick.AddListener(Hide);
        content=Rect("Yayın İçeriği",body,new Vector2(.027f,.16f),new Vector2(.973f,.875f));
        Panel("Yayın Zemini",content,Vector2.zero,Vector2.one,new Color(.065f,.125f,.15f)).raycastTarget=false;
        menuHint=Text("Menü İpucu",content,new Vector2(.10f,.40f),new Vector2(.90f,.60f),"Kamu yayınını hazırlamak için aşağıdan bir görev seç.",24);
        recording=Text("Kayıt Göstergesi",body,new Vector2(.04f,.878f),new Vector2(.32f,.915f),"REC  /  KAMU YAYINI",13);
        recording.color=new Color(1,.38f,.32f);recording.gameObject.SetActive(false);
        Mount(window.BagPanel); Mount(window.SafetyPanel);
        MountEntry(window.BagEntryButton,new Vector2(.05f,.035f),new Vector2(.485f,.13f));
        MountEntry(window.SafetyEntryButton,new Vector2(.515f,.035f),new Vector2(.95f,.13f));
        // Pixel corner brackets remain above the game graphics and never intercept clicks.
        for(int i=0;i<4;i++)
        {
            float x=i%2==0?0:1;float y=i<2?0:1;
            var h=Panel("Kayıt Köşesi",content,new Vector2(x==0?0:.96f,y==0?0:.993f),new Vector2(x==0?.04f:1,y==0?.007f:1),new Color(.4f,.91f,.83f));h.raycastTarget=false;
            var v=Panel("Kayıt Köşesi",content,new Vector2(x==0?0:.996f,y==0?0:.94f),new Vector2(x==0?.004f:1,y==0?.06f:1),h.color);v.raycastTarget=false;
        }
        layer.gameObject.SetActive(false);
    }
    private void Mount(GameObject game)
    {
        var r=(RectTransform)game.transform;r.SetParent(content,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        foreach(var t in game.GetComponentsInChildren<TMP_Text>(true)){t.enableAutoSizing=true;t.fontSizeMax=Mathf.Min(26,t.fontSize);t.fontSizeMin=Mathf.Min(13,t.fontSizeMax);}
        foreach(var button in game.GetComponentsInChildren<Button>(true))
            if(button.name=="Kontrol Et")
            {
                var check=(RectTransform)button.transform;check.anchorMin=new Vector2(.88f,.025f);check.anchorMax=new Vector2(.975f,.12f);check.offsetMin=check.offsetMax=Vector2.zero;
            }
        game.SetActive(false);
    }
    private void MountEntry(Button button, Vector2 min, Vector2 max)
    {
        var r=(RectTransform)button.transform;r.SetParent(body,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;button.gameObject.SetActive(true);
    }
    public void Show()
    {
        if (!layer || window.Health.IsGameOver) return;
        visible=true;layer.gameObject.SetActive(true);arrow.gameObject.SetActive(false);
        if(motion!=null)StopCoroutine(motion);motion=StartCoroutine(Slide(true));
    }
    public void ShowGame(GameObject panel)
    {
        if(!visible)Show();
        recording.gameObject.SetActive(true);
        menuHint.gameObject.SetActive(false);
    }
    public void ShowMenu() { if(recording)recording.gameObject.SetActive(false); if(menuHint)menuHint.gameObject.SetActive(true); }
    public void Hide()
    {
        if(!visible)return;
        window.Close();visible=false;
        if(motion!=null)StopCoroutine(motion);motion=StartCoroutine(Slide(false));
    }
    private IEnumerator Slide(bool opening)
    {
        Vector2 from=body.anchoredPosition;Vector2 to=opening?Vector2.zero:new Vector2(0,-((RectTransform)layer.transform).rect.height);
        if(opening){from=new Vector2(0,-((RectTransform)layer.transform).rect.height);body.anchoredPosition=from;}
        for(float t=0;t<.32f;t+=Time.unscaledDeltaTime){float p=1-Mathf.Pow(1-Mathf.Clamp01(t/.32f),3);body.anchoredPosition=Vector2.Lerp(from,to,p);layer.alpha=opening?p:1-p;yield return null;}
        body.anchoredPosition=to;layer.alpha=opening?1:0;
        if(!opening){layer.gameObject.SetActive(false);arrow.gameObject.SetActive(!window.Health.IsGameOver);}motion=null;
    }
    private void HideInstant()
    {
        window.Close();visible=false;if(motion!=null)StopCoroutine(motion);motion=null;
        if(layer)layer.gameObject.SetActive(false);if(arrow)arrow.gameObject.SetActive(false);
    }
    private void Update()
    {
        if(recording && recording.gameObject.activeInHierarchy)recording.alpha=.65f+.35f*Mathf.Abs(Mathf.Sin(Time.unscaledTime*3));
    }
    public void Feedback(int actualDelta, bool correct)
    {
        if(!layer)return;
        var text=Text("Anlık Sağlık Etkisi",layer.transform,Vector2.one*.5f,Vector2.one*.5f,
            actualDelta==0 && correct ? "ŞEHİR SAĞLIĞI TAM" : (actualDelta>0?"+":"")+actualDelta+" ŞEHİR SAĞLIĞI",21);
        text.enableAutoSizing=false;text.rectTransform.sizeDelta=new Vector2(340,45);
        text.color=correct?new Color(.4f,1,.68f):new Color(1,.4f,.32f);
        var screen=Mouse.current!=null?Mouse.current.position.ReadValue():new Vector2(Screen.width/2f,Screen.height/2f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)layer.transform,screen,null,out Vector2 point);
        var bounds=((RectTransform)layer.transform).rect;
        point.x=Mathf.Clamp(point.x,bounds.xMin+180,bounds.xMax-180);
        point.y=Mathf.Clamp(point.y,bounds.yMin+30,bounds.yMax-105);
        text.rectTransform.anchoredPosition=point+Vector2.up*32;
        StartCoroutine(Float(text));
    }
    private IEnumerator Float(TMP_Text text)
    {
        var r=text.rectTransform;var start=r.anchoredPosition;
        for(float t=0;t<.85f;t+=Time.unscaledDeltaTime){if(!text)yield break;float p=t/.85f;r.anchoredPosition=start+new Vector2(Mathf.Sin(p*Mathf.PI*2)*7,p*50);r.localRotation=Quaternion.Euler(0,0,Mathf.Sin(p*Mathf.PI*4)*10*(1-p));text.alpha=1-Mathf.Pow(p,2);yield return null;}
        if(text)Destroy(text.gameObject);
    }
}
