using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PreparationUI;

public sealed class SmokeDetectorPreparationGame : MonoBehaviour
{
    private RectTransform root;
    private Button cover,oldBattery,newBattery,slot;
    private Image indicator;
    private Image draggedBattery;
    private TMP_Text hint,stepLabel;
    private readonly Sprite[] art=new Sprite[4];
    private Action complete;
    private bool moving;
    public int Step { get; private set; }
    public Button Cover => cover;
    public Button OldBattery => oldBattery;
    public Button NewBattery => newBattery;
    public Button Slot => slot;
    public bool Succeeded => Step==4;
    public void Initialize(Transform parent,TMP_FontAsset font,Action onComplete)
    {
        complete=onComplete;var sheet=Resources.Load<Texture2D>("Disaster/DetectorSheet");
        Rect[] cells={new Rect(0,.08f,.24f,.84f),new Rect(.25f,.08f,.30f,.84f),new Rect(.565f,.09f,.17f,.82f),new Rect(.77f,.09f,.19f,.82f)};
        for(int i=0;i<4;i++)art[i]=PreparationUI.Sprite(sheet,cells[i]);
        root=Rect("Duman Dedektörü Pil Eğitimi",parent,Vector2.zero,Vector2.one);
        Panel("Tavan Zemini",root,new Vector2(.04f,.16f),new Vector2(.96f,.88f),PanelColor);
        Label("Yönerge",root,new Vector2(.04f,.89f),new Vector2(.96f,1),"DUMAN DEDEKTÖRÜNÜN PİLİNİ DEĞİŞTİR",font,25);
        cover=Button("Dedektör Kapağı",root,new Vector2(.17f,.22f),new Vector2(.62f,.85f),"",font);cover.image.sprite=art[0];cover.image.color=Color.white;cover.image.preserveAspect=true;
        cover.onClick.AddListener(ClickCover);
        slot=Button("Pil Yuvası",root,new Vector2(.30f,.39f),new Vector2(.47f,.63f),"",font);slot.image.color=Color.clear;
        oldBattery=Button("Eski Pili Çıkar",root,new Vector2(.32f,.40f),new Vector2(.45f,.62f),"",font);oldBattery.image.sprite=art[2];oldBattery.image.preserveAspect=true;oldBattery.image.color=Color.white;oldBattery.transform.localRotation=Quaternion.Euler(0,0,-90);
        newBattery=Button("Yeni Pili Yerleştir",root,new Vector2(.69f,.28f),new Vector2(.90f,.73f),"",font);newBattery.image.sprite=art[3];newBattery.image.preserveAspect=true;newBattery.image.color=Color.white;newBattery.transform.localRotation=Quaternion.Euler(0,0,-90);
        AddDrag(oldBattery,true);AddDrag(newBattery,false);
        foreach(var button in new[]{cover,oldBattery,newBattery,slot})
        {var colors=button.colors;colors.disabledColor=Color.white;button.colors=colors;}
        Label("Yeni Pil Etiketi",root,new Vector2(.66f,.20f),new Vector2(.92f,.30f),"YENİ PİL",font,22);
        indicator=Panel("Dedektör LED",root,new Vector2(.43f,.70f),new Vector2(.447f,.727f),Amber);
        stepLabel=Label("İşlem Adımı",root,new Vector2(.05f,.02f),new Vector2(.95f,.10f),"",font,20);
        Panel("İşlem İpucu Zemini",root,new Vector2(.05f,.10f),new Vector2(.95f,.19f),new Color(.015f,.035f,.055f,1f));
        hint=Label("İşlem İpucu",root,new Vector2(.07f,.10f),new Vector2(.93f,.19f),"",font,28);hint.color=Amber;
        Hide();
    }
    public void Show(){StopAllCoroutines();ClearDrag();Step=0;moving=false;root.gameObject.SetActive(true);Refresh();}
    private bool CanAct(int expected)=>root.gameObject.activeInHierarchy&&!moving&&Step==expected;
    public void ClickCover()
    {
        if(CanAct(0))StartCoroutine(Advance(1));
        else if(CanAct(3))StartCoroutine(Finish());
    }
    private void AddDrag(Button battery,bool old)
    {
        var trigger=battery.gameObject.AddComponent<EventTrigger>();
        AddDragEvent(trigger,EventTriggerType.BeginDrag,data=>BeginDrag(old,(PointerEventData)data));
        AddDragEvent(trigger,EventTriggerType.Drag,data=>Drag((PointerEventData)data));
        AddDragEvent(trigger,EventTriggerType.EndDrag,data=>EndDrag(old,(PointerEventData)data));
    }
    private static void AddDragEvent(EventTrigger trigger,EventTriggerType type,UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        var entry=new EventTrigger.Entry{eventID=type};entry.callback.AddListener(callback);trigger.triggers.Add(entry);
    }
    private void BeginDrag(bool old,PointerEventData eventData)
    {
        if(eventData.button!=PointerEventData.InputButton.Left||!CanAct(old?1:2))return;
        var source=old?oldBattery:newBattery;
        draggedBattery=Panel("Sürüklenen Pil",root,Vector2.one*.5f,Vector2.one*.5f,Color.white);
        draggedBattery.sprite=source.image.sprite;draggedBattery.preserveAspect=true;
        draggedBattery.rectTransform.sizeDelta=source.GetComponent<RectTransform>().rect.size;
        draggedBattery.rectTransform.localRotation=source.transform.localRotation;
        source.image.color=new Color(1,1,1,.35f);
        Drag(eventData);
    }
    private void Drag(PointerEventData eventData)
    {
        if(!draggedBattery)return;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(root,eventData.position,eventData.pressEventCamera,out var position))
            draggedBattery.rectTransform.anchoredPosition=position;
    }
    private void EndDrag(bool old,PointerEventData eventData)
    {
        if(!draggedBattery)return;
        ClearDrag();
        bool overDetector=RectTransformUtility.RectangleContainsScreenPoint(cover.GetComponent<RectTransform>(),eventData.position,eventData.pressEventCamera);
        if(CanAct(old?1:2)&&(old?!overDetector:overDetector))StartCoroutine(Advance(old?2:3));
    }
    private void ClearDrag()
    {
        if(draggedBattery)Destroy(draggedBattery.gameObject);
        draggedBattery=null;
        if(oldBattery)oldBattery.image.color=Color.white;
        if(newBattery)newBattery.image.color=Color.white;
    }
    private IEnumerator Advance(int next)
    {
        moving=true;yield return new WaitForSecondsRealtime(.25f);Step=next;Refresh();moving=false;
    }
    private void Refresh()
    {
        cover.image.sprite=art[Step==0||Step==4?0:1];
        slot.gameObject.SetActive(Step>=1&&Step<=3);slot.interactable=false;
        oldBattery.gameObject.SetActive(Step==1||Step==3);oldBattery.image.sprite=Step==3?art[3]:art[2];oldBattery.interactable=Step==1;
        newBattery.gameObject.SetActive(Step<=2);newBattery.interactable=Step==2;
        cover.interactable=Step==0||Step==3;
        cover.image.raycastTarget=cover.interactable;
        slot.image.raycastTarget=slot.interactable;
        oldBattery.image.raycastTarget=oldBattery.interactable;
        newBattery.image.raycastTarget=newBattery.interactable;
        indicator.color=Step==4?Green:Amber;
        string[] hints={"Kapağı açmak için dedektöre tıkla.","Eski pili dedektörün dışına sürükleyip bırak.","Yeni pili dedektörün üstüne sürükleyip bırak.","Kapağı kapat.","PİL DEĞİŞTİ · CİHAZI ÜRETİCİ TALİMATINA GÖRE TEST ET"};
        hint.text=hints[Step];stepLabel.text=Step==4?"PİL DEĞİŞTİRİLDİ":"ADIM "+(Step+1)+" · TOPLAM 4";
    }
    private IEnumerator Finish()
    {
        moving=true;Step=4;Refresh();
        for(float t=0;t<.65f;t+=Time.unscaledDeltaTime){indicator.color=(Mathf.FloorToInt(t*8)%2==0)?Green:White;yield return null;}
        indicator.color=Green;moving=false;complete?.Invoke();
    }
    public void Hide(){ClearDrag();if(root)root.gameObject.SetActive(false);StopAllCoroutines();moving=false;}
    private void OnDestroy(){foreach(var sprite in art)if(sprite)Destroy(sprite);if(root)Destroy(root.gameObject);}
}
