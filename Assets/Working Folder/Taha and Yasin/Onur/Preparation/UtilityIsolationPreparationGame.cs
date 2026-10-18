using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PreparationUI;

// Identify isolation points on a schematic. No real gas/electrical operation is simulated.
public sealed class UtilityIsolationPreparationGame : MonoBehaviour
{
    private RectTransform root;
    private CanvasGroup modal;
    private readonly Button[] points=new Button[5];
    private readonly Image[] borders=new Image[5];
    private readonly TMP_Text[] tags=new TMP_Text[5];
    private readonly Sprite[] art=new Sprite[5];
    private readonly bool[] selected=new bool[5];
    private readonly int[] order={0,1,2,3,4};
    private TMP_Text hint;
    private Image resultLight;
    private Action mistake,complete;
    private Coroutine feedback;
    public bool Succeeded { get; private set; }
    public bool IsOpen => root && root.gameObject.activeInHierarchy;
    public bool CanAct => IsOpen && !Succeeded && (!modal || modal.interactable);
    public Button[] Points => points;
    public Button CheckButton { get; private set; }
    public bool IsSelected(int index)=>index>=0 && index<5 && selected[index];
    public bool IsTarget(int index)=>index>=0 && index<5 && (order[index]==1||order[index]==3);
    public void Initialize(Transform parent,TMP_FontAsset font,Action onMistake,Action onComplete)
    {
        mistake=onMistake;complete=onComplete;root=Rect("Hat İzolasyon İşaretleme",parent,Vector2.zero,Vector2.one);
        modal=root.GetComponentInParent<CanvasGroup>();
        Label("İzolasyon Yönergesi",root,new Vector2(.02f,.90f),new Vector2(.98f,1),"ALEV: DOĞALGAZ · YILDIRIM: ELEKTRİK · İKİ HATTI BUL",font,24);
        Panel("Tesisat Panosu",root,new Vector2(.02f,.22f),new Vector2(.98f,.88f),new Color(.055f,.085f,.11f));
        var sheet=Resources.Load<Texture2D>("Disaster/UtilityTrainingSheet");
        string[] labels={"HAT A","HAT B","HAT C","HAT D","HAT E"};
        for(int i=0;i<5;i++)
        {
            int id=i;float x=.035f+i*.188f;
            borders[i]=Panel("Seçim Çerçevesi "+i,root,new Vector2(x,.25f),new Vector2(x+.178f,.85f),new Color(.16f,.23f,.27f));
            var face=Panel("Hat Kartı",borders[i].transform,new Vector2(.015f,.01f),new Vector2(.985f,.99f),PanelColor);
            art[i]=PreparationUI.Sprite(sheet,new Rect(i*.2f,0,.2f,1));
            points[i]=Button("İzolasyon Noktası "+i,face.transform,new Vector2(.025f,.18f),new Vector2(.975f,.98f),"",font);
            points[i].image.sprite=art[i];points[i].image.preserveAspect=true;points[i].image.color=Color.white;
            points[i].onClick.AddListener(()=>Toggle(id));
            Label("Hat Etiketi",face.transform,new Vector2(.02f,.075f),new Vector2(.98f,.20f),labels[i],font,20);
            tags[i]=Label("İşaret Durumu",face.transform,new Vector2(.02f,.01f),new Vector2(.98f,.08f),"",font,16);
            // Full-card target: label and empty padding are clickable too.
            var target=face.gameObject.AddComponent<Button>();face.raycastTarget=true;target.targetGraphic=face;target.onClick.AddListener(()=>Toggle(id));
            var colors=target.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.8f,.9f,.95f);target.colors=colors;
        }
        hint=Label("İzolasyon Geri Bildirimi",root,new Vector2(.03f,.08f),new Vector2(.72f,.20f),"",font,22);
        CheckButton=Button("İşaretleri Kontrol Et",root,new Vector2(.76f,.08f),new Vector2(.97f,.19f),"KONTROL ET",font);CheckButton.onClick.AddListener(Check);
        resultLight=Panel("Pano Durum Işığı",root,new Vector2(.025f,.875f),new Vector2(.975f,.88f),new Color(.16f,.23f,.27f));
        Label("İzolasyon Eğitim Notu",root,new Vector2(.02f,.005f),new Vector2(.98f,.065f),"ŞEMA EĞİTİMİ · Gaz kokusunda elektrik düğmelerine dokunma.",font,17);
        ResetForNewTake();Hide();
    }
    public void ResetForNewTake()
    {
        if(feedback!=null)StopCoroutine(feedback);feedback=null;Succeeded=false;
        for(int i=order.Length-1;i>0;i--){int j=UnityEngine.Random.Range(0,i+1);int old=order[i];order[i]=order[j];order[j]=old;}
        for(int i=0;i<5;i++){selected[i]=false;points[i].interactable=true;points[i].image.sprite=art[order[i]];}
        CheckButton.interactable=true;Refresh();
    }
    public void Show(){root.gameObject.SetActive(true);Refresh();}
    public void Hide(){if(feedback!=null)StopCoroutine(feedback);feedback=null;if(root)root.gameObject.SetActive(false);}
    public void Toggle(int index)
    {
        if(!CanAct||index<0||index>=5)return;selected[index]=!selected[index];Refresh();
        if(feedback!=null)StopCoroutine(feedback);feedback=StartCoroutine(SeatMarker(index));
    }
    private IEnumerator SeatMarker(int index)
    {
        var r=borders[index].rectTransform;
        for(float t=0;t<.14f;t+=Time.unscaledDeltaTime){r.localScale=Vector3.one*(1+.018f*Mathf.Sin(t/.14f*Mathf.PI));yield return null;}
        r.localScale=Vector3.one;feedback=null;
    }
    private void Refresh()
    {
        int count=0;
        for(int i=0;i<5;i++){if(selected[i])count++;borders[i].color=selected[i]?Amber:new Color(.16f,.23f,.27f);tags[i].text=selected[i]?"İŞARETLİ":"";borders[i].transform.localScale=Vector3.one;}
        hint.text=Succeeded?"İKİ NOKTA DOĞRU İŞARETLENDİ":count+" / 2 NOKTA İŞARETLİ";
        hint.color=Succeeded?Green:White;resultLight.color=Succeeded?Green:new Color(.16f,.23f,.27f);
    }
    public void Check()
    {
        if(!CanAct)return;
        bool correct=true;for(int i=0;i<5;i++)if(selected[i]!=IsTarget(i))correct=false;
        if(!correct){mistake?.Invoke();hint.text="İki aktif hattı yeniden kontrol et.";hint.color=Amber;resultLight.color=Amber;return;}
        Succeeded=true;foreach(var point in points)point.interactable=false;CheckButton.interactable=false;Refresh();complete?.Invoke();
    }
    private void OnDestroy(){foreach(var sprite in art)if(sprite)Destroy(sprite);if(root)Destroy(root.gameObject);}
}
