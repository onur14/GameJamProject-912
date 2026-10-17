using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PreparationUI;

public sealed class SmokeDetectorPreparationGame : MonoBehaviour
{
    private RectTransform root;
    private Button cover,oldBattery,newBattery,slot;
    private Image indicator;
    private TMP_Text hint,stepLabel;
    private readonly Sprite[] art=new Sprite[4];
    private Action complete;
    private bool moving;
    public int Step { get; private set; }
    public Button Cover => cover;
    public Button OldBattery => oldBattery;
    public Button NewBattery => newBattery;
    public Button Slot => slot;
    public bool Succeeded => Step==5;
    public void Initialize(Transform parent,TMP_FontAsset font,Action onComplete)
    {
        complete=onComplete;var sheet=Resources.Load<Texture2D>("Disaster/DetectorSheet");
        // The open cover extends to the right, so retain the whole silhouette.
        Rect[] cells={new Rect(0,.12f,.24f,.78f),new Rect(.25f,.12f,.30f,.78f),new Rect(.56f,.12f,.16f,.78f),new Rect(.77f,.12f,.17f,.78f)};
        for(int i=0;i<4;i++)art[i]=PreparationUI.Sprite(sheet,cells[i]);
        root=Rect("Duman Dedektörü Pil Eğitimi",parent,Vector2.zero,Vector2.one);
        Panel("Tavan Zemini",root,new Vector2(.04f,.16f),new Vector2(.96f,.88f),PanelColor);
        Label("Yönerge",root,new Vector2(.04f,.89f),new Vector2(.96f,1),"DUMAN DEDEKTÖRÜNÜN PİLİNİ DEĞİŞTİR",font,25);
        cover=Button("Dedektör Kapağı",root,new Vector2(.17f,.22f),new Vector2(.62f,.85f),"",font);cover.image.sprite=art[0];cover.image.color=Color.white;cover.image.preserveAspect=true;
        cover.onClick.AddListener(ClickCover);
        slot=Button("Pil Yuvası",root,new Vector2(.31f,.42f),new Vector2(.46f,.60f),"",font);slot.image.color=new Color(.09f,.14f,.17f);slot.onClick.AddListener(InsertBattery);
        oldBattery=Button("Eski Pili Çıkar",root,new Vector2(.33f,.43f),new Vector2(.44f,.59f),"",font);oldBattery.image.sprite=art[2];oldBattery.image.preserveAspect=true;oldBattery.image.color=Color.white;oldBattery.onClick.AddListener(RemoveBattery);
        newBattery=Button("Yeni Pili Seç",root,new Vector2(.72f,.30f),new Vector2(.86f,.68f),"",font);newBattery.image.sprite=art[3];newBattery.image.preserveAspect=true;newBattery.image.color=Color.white;newBattery.onClick.AddListener(SelectBattery);
        foreach(var button in new[]{cover,oldBattery,newBattery,slot})
        {var colors=button.colors;colors.disabledColor=Color.white;button.colors=colors;}
        Label("Yeni Pil Etiketi",root,new Vector2(.66f,.20f),new Vector2(.92f,.30f),"YENİ PİL",font,22);
        indicator=Panel("Dedektör LED",root,new Vector2(.43f,.70f),new Vector2(.447f,.727f),Amber);
        stepLabel=Label("İşlem Adımı",root,new Vector2(.05f,.02f),new Vector2(.95f,.10f),"",font,20);
        hint=Label("İşlem İpucu",root,new Vector2(.05f,.10f),new Vector2(.95f,.19f),"",font,24);hint.color=Amber;
        Hide();
    }
    public void Show(){StopAllCoroutines();Step=0;moving=false;root.gameObject.SetActive(true);Refresh();}
    private bool CanAct(int expected)=>root.gameObject.activeInHierarchy&&!moving&&Step==expected;
    public void ClickCover()
    {
        if(CanAct(0))StartCoroutine(Advance(1));
        else if(CanAct(4))StartCoroutine(Finish());
    }
    public void RemoveBattery(){if(CanAct(1))StartCoroutine(Advance(2));}
    public void SelectBattery(){if(CanAct(2))StartCoroutine(Advance(3));}
    public void InsertBattery(){if(CanAct(3))StartCoroutine(Advance(4));}
    private IEnumerator Advance(int next)
    {
        moving=true;yield return new WaitForSecondsRealtime(.25f);Step=next;Refresh();moving=false;
    }
    private void Refresh()
    {
        cover.image.sprite=art[Step==0||Step==5?0:1];
        slot.gameObject.SetActive(Step>=1&&Step<=4);slot.interactable=Step==3;
        oldBattery.gameObject.SetActive(Step==1||Step==4);oldBattery.image.sprite=Step==4?art[3]:art[2];oldBattery.interactable=Step==1;
        newBattery.gameObject.SetActive(Step<=3);newBattery.interactable=Step==2;newBattery.image.color=Step==3?Amber:Color.white;
        cover.interactable=Step==0||Step==4;
        cover.image.raycastTarget=cover.interactable;
        slot.image.raycastTarget=slot.interactable;
        oldBattery.image.raycastTarget=oldBattery.interactable;
        newBattery.image.raycastTarget=newBattery.interactable;
        indicator.color=Step==5?Green:Amber;
        string[] hints={"Kapağı açmak için dedektöre tıkla.","Eski pili çıkar.","Yeni pili seç.","Pili boş yuvaya yerleştir.","Kapağı kapat.","DEDEKTÖR HAZIR"};
        hint.text=hints[Step];stepLabel.text=Step==5?"PİL DEĞİŞTİRİLDİ":"ADIM "+(Step+1)+" · TOPLAM 5";
    }
    private IEnumerator Finish()
    {
        moving=true;Step=5;Refresh();
        for(float t=0;t<.65f;t+=Time.unscaledDeltaTime){indicator.color=(Mathf.FloorToInt(t*8)%2==0)?Green:White;yield return null;}
        indicator.color=Green;moving=false;complete?.Invoke();
    }
    public void Hide(){if(root)root.gameObject.SetActive(false);StopAllCoroutines();moving=false;}
    private void OnDestroy(){foreach(var sprite in art)if(sprite)Destroy(sprite);if(root)Destroy(root.gameObject);}
}
