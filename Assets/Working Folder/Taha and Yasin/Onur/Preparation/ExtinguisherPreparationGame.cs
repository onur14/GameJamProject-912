using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PreparationUI;

// A fictional training rig, not instructions to refill an actual extinguisher.
public sealed class ExtinguisherPreparationGame : MonoBehaviour
{
    private RectTransform root, needle, bottle;
    private CanvasGroup modal;
    private Image lamp, stabilityFill;
    private TMP_Text hint, reading;
    private Action mistake, complete;
    private Sprite art;
    private float adjustment, shownPressure, velocity, stableTime, reaction;
    private bool rejected;
    public float Pressure { get; private set; }
    public bool Succeeded { get; private set; }
    public bool IsOpen => root && root.gameObject.activeInHierarchy;
    public bool CanAct => IsOpen && !Succeeded && (!modal || modal.interactable);
    public Button Increase { get; private set; }
    public Button Decrease { get; private set; }
    public Button CheckButton { get; private set; }
    public const float SafeMin=.46f, SafeMax=.58f;
    private const float ReleaseSpeed=.085f;
    public void Initialize(Transform parent,TMP_FontAsset font,Action onMistake,Action onComplete)
    {
        mistake=onMistake;complete=onComplete;
        root=Rect("Yangın Tüpü Eğitim Düzeneği",parent,Vector2.zero,Vector2.one);modal=root.GetComponentInParent<CanvasGroup>();
        Label("Basınç Yönergesi",root,new Vector2(.02f,.91f),new Vector2(.98f,1),"İBREYİ YEŞİL ARALIĞA GETİR · DENGELENİNCE KONTROL ET",font,25);
        Panel("Tezgâh",root,new Vector2(.03f,.18f),new Vector2(.97f,.89f),new Color(.055f,.085f,.11f));
        for(int i=0;i<5;i++)Panel("Tezgâh Yivi",root,new Vector2(.05f,.2f+i*.13f),new Vector2(.95f,.202f+i*.13f),new Color(.09f,.13f,.15f));
        bottle=Rect("Tüp Görseli",root,new Vector2(.07f,.23f),new Vector2(.35f,.86f));
        art=PreparationUI.Sprite(Resources.Load<Texture2D>("Disaster/ExtinguisherTraining"),new Rect(0,0,1,1));
        var image=bottle.gameObject.AddComponent<Image>();image.sprite=art;image.preserveAspect=true;image.raycastTarget=false;
        var dialFrame=Rect("Manometre Yuvası",root,new Vector2(.43f,.31f),new Vector2(.84f,.87f));
        var dial=Rect("Manometre",dialFrame,Vector2.zero,Vector2.one);
        var fit=dial.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;
        Panel("Manometre Kasası",dial,Vector2.zero,Vector2.one,new Color(.15f,.21f,.24f));
        Panel("Manometre Camı",dial,new Vector2(.025f,.025f),new Vector2(.975f,.975f),Navy);
        for(int i=0;i<=40;i++)
        {
            float p=i/40f,angle=Mathf.Lerp(215,-35,p)*Mathf.Deg2Rad;
            var pos=new Vector2(.5f+Mathf.Cos(angle)*.41f,.47f+Mathf.Sin(angle)*.41f);
            var tick=Panel("Basınç Çizgisi",dial,pos,pos,p>=SafeMin&&p<=SafeMax?Green:Amber).rectTransform;
            tick.sizeDelta=new Vector2(i%5==0?5:3,i%5==0?22:12);tick.localEulerAngles=new Vector3(0,0,angle*Mathf.Rad2Deg-90);
            var outline=tick.gameObject.AddComponent<Outline>();outline.effectColor=Color.black;outline.effectDistance=new Vector2(1,-1);outline.useGraphicAlpha=false;
        }
        needle=Panel("Basınç İbresi",dial,new Vector2(.5f,.47f),new Vector2(.5f,.47f),White).rectTransform;
        needle.pivot=new Vector2(.5f,.05f);needle.sizeDelta=new Vector2(6,125);
        var hub=Panel("İbre Mili",dial,new Vector2(.48f,.45f),new Vector2(.52f,.49f),Amber);
        Label("Aralık",dial,new Vector2(.14f,.10f),new Vector2(.86f,.27f),"YEŞİL = HAZIR",font,20).color=Green;
        reading=Label("Eğitim Basınç Değeri",dial,new Vector2(.2f,.31f),new Vector2(.8f,.43f),"",font,26);
        lamp=Panel("Durum Lambası",root,new Vector2(.87f,.73f),new Vector2(.89f,.77f),Amber);
        var track=Panel("Denge İzleyici",root,new Vector2(.46f,.22f),new Vector2(.82f,.24f),PanelColor);
        stabilityFill=Panel("Denge Dolumu",track.transform,Vector2.zero,Vector2.zero,Green);
        Decrease=Button("Basıncı Azalt",root,new Vector2(.38f,.06f),new Vector2(.55f,.17f),"−  AZALT",font);
        Increase=Button("Basıncı Artır",root,new Vector2(.57f,.06f),new Vector2(.74f,.17f),"+  ARTIR",font);
        BindHold(Decrease,-1);BindHold(Increase,1);
        CheckButton=Button("Basıncı Kontrol Et",root,new Vector2(.77f,.06f),new Vector2(.97f,.17f),"KONTROL ET",font);CheckButton.onClick.AddListener(Check);
        hint=Label("Basınç Geri Bildirimi",root,new Vector2(.02f,.08f),new Vector2(.35f,.18f),"",font,19);
        Label("Eğitim Uyarısı",root,new Vector2(.02f,0),new Vector2(.98f,.055f),"EĞİTİM SİMÜLASYONU · Gerçek tüpün dolum ve bakımı yetkili servise aittir.",font,17);
        ResetForNewTake();Hide();
    }
    private void BindHold(Button button,int direction)
    {
        var trigger=button.gameObject.AddComponent<EventTrigger>();
        foreach(var type in new[]{EventTriggerType.PointerDown,EventTriggerType.PointerUp,EventTriggerType.PointerExit})
        {var kind=type;var e=new EventTrigger.Entry{eventID=kind};e.callback.AddListener(_=>SetAdjustment(kind==EventTriggerType.PointerDown?direction:0));trigger.triggers.Add(e);}
    }
    public void SetAdjustment(int direction){adjustment=CanAct?Mathf.Clamp(direction,-1,1):0;if(adjustment!=0){rejected=false;stableTime=0;}}
    public void ResetForNewTake(){Pressure=shownPressure=.16f;adjustment=velocity=stableTime=reaction=0;Succeeded=rejected=false;ResetControls();Render();}
    public void Show(){root.gameObject.SetActive(true);adjustment=0;Render();}
    public void Hide(){adjustment=0;stableTime=0;if(root)root.gameObject.SetActive(false);}
    private void Update()
    {
        if(!CanAct){adjustment=0;return;}
        float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
        Pressure=Mathf.Clamp01(Pressure+adjustment*dt*.24f);
        if(adjustment==0)Pressure=Mathf.MoveTowards(Pressure,0,ReleaseSpeed*dt);
        shownPressure=Mathf.SmoothDamp(shownPressure,Pressure,ref velocity,.10f,10,dt);
        bool stable=adjustment==0 && Pressure>=SafeMin && Pressure<=SafeMax && Mathf.Abs(shownPressure-Pressure)<.012f;
        stableTime=stable?Mathf.Min(.65f,stableTime+dt):0;
        reaction=Mathf.Max(0,reaction-dt);Render();
    }
    private void Render()
    {
        if(!needle)return;needle.localEulerAngles=new Vector3(0,0,Mathf.Lerp(125,-125,shownPressure));
        bool ready=stableTime>=.65f;lamp.color=Succeeded||ready?Green:reaction>0?Red:Amber;
        reading.text=Mathf.RoundToInt(shownPressure*100)+" / 100";
        stabilityFill.rectTransform.anchorMax=new Vector2(stableTime/.65f,1);
        bottle.anchoredPosition=Vector2.right*(Mathf.Sin(Time.unscaledTime*38)*reaction*3);
        hint.text=Succeeded?"BASINÇ HAZIR":rejected?"Yeşil aralıkta\ndengelenmesini bekle.":adjustment!=0?"AYARLANIYOR":ready?"DENGELENDİ · KONTROL ET":"ARTIR / AZALT\nBasılı tut.";
        hint.color=rejected?Amber:Succeeded?Green:White;
    }
    public void Check()
    {
        if(!CanAct)return;
        if(Pressure<SafeMin||Pressure>SafeMax||stableTime<.65f){rejected=true;reaction=.28f;mistake?.Invoke();Render();return;}
        Succeeded=true;adjustment=0;Decrease.interactable=Increase.interactable=CheckButton.interactable=false;Render();complete?.Invoke();
    }
    public void ResetControls(){Decrease.interactable=Increase.interactable=CheckButton.interactable=true;}
    private void OnDestroy(){if(art)Destroy(art);if(root)Destroy(root.gameObject);}
}
