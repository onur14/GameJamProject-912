using TMPro;
using UnityEngine;
using UnityEngine.UI;

// This component only observes DispatchResources; previous values are animation snapshots.
public sealed class DispatchResourceHud : MonoBehaviour
{
    [SerializeField] private DispatchResources resources;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField, Range(.15f,.25f)] private float feedbackDuration = .22f;
    [SerializeField, Min(1)] private int lowResourceThreshold = 3;
    [SerializeField, Range(.7f,1.1f)] private float warningCycle = .9f;
    [SerializeField, Range(1.08f,1.15f)] private float numberPulseScale = 1.09f;
    [SerializeField, Range(.8f,1.4f)] private float dispatchNoticeDuration = 1.15f;
    [SerializeField, Range(1,1.08f)] private float iconPulseScale = 1.08f;
    private OyunAkisiOnur conversation;
    private TMP_Text dispatchNotice;
    private Coroutine noticeRoutine;
    private readonly RectTransform[] icons=new RectTransform[3];
    private readonly float[] iconUntil=new float[3];
    public string DispatchNotice => dispatchNotice ? dispatchNotice.text : string.Empty;
    private readonly TMP_Text[] numbers=new TMP_Text[3], deltas=new TMP_Text[3], warnings=new TMP_Text[3];
    private readonly int[] previous=new int[3];
    private readonly float[] feedbackUntil=new float[3];
    private readonly Color[] emphasis=new Color[3];
    private readonly Color amber=new Color(.83f,.63f,.32f), critical=new Color(.77f,.35f,.30f), cool=new Color(.77f,.9f,.96f);
    private Coroutine animation;
    public TMP_Text[] Numbers => numbers;
    private readonly Color normal=new Color(.84f,.9f,.86f);
    private void Awake(){Build();}
    private void OnEnable()
    {
        if(resources)resources.Changed+=Changed;
        conversation=FindFirstObjectByType<OyunAkisiOnur>();
        if(conversation)conversation.UnitDispatched+=Dispatched;
        Refresh(false);
    }
    private void OnDisable()
    {
        if(resources)resources.Changed-=Changed;
        if(conversation)conversation.UnitDispatched-=Dispatched;
        if(noticeRoutine!=null)StopCoroutine(noticeRoutine);noticeRoutine=null;
        if(dispatchNotice)dispatchNotice.alpha=0;
        if(animation!=null)StopCoroutine(animation);animation=null;
        for(int i=0;i<3;i++)if(numbers[i]){numbers[i].rectTransform.localScale=Vector3.one;numbers[i].color=normal;deltas[i].text=string.Empty;warnings[i].alpha=1;}
        for(int i=0;i<3;i++)if(icons[i])icons[i].localScale=Vector3.one;
    }
    private TMP_Text Text(string name,Transform parent,Vector2 min,Vector2 max,float size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);
        rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var t=go.GetComponent<TMP_Text>();t.font=font?font:TMP_Settings.defaultFontAsset;t.fontSize=size;t.color=color;t.raycastTarget=false;
        t.alignment=TextAlignmentOptions.MidlineLeft;t.textWrappingMode=TextWrappingModes.Normal;
        return t;
    }
    private void Build()
    {
        if(numbers[0])return;
        var bg=gameObject.GetComponent<Image>()??gameObject.AddComponent<Image>();bg.color=new Color(.035f,.06f,.08f,.98f);bg.raycastTarget=false;
        var heading=Text("Kaynak Başlığı",transform,new Vector2(.07f,.89f),new Vector2(.94f,.98f),18,new Color(.98f,.76f,.43f));
        heading.fontStyle=FontStyles.Bold;heading.text="SAHA EKİPLERİ";
        string[] labels={"AMBULANS","İTFAİYE","KURTARMA EKİBİ"};
        for(int i=0;i<3;i++)
        {
            float top=.875f-i*.28f;
            var label=Text(labels[i],transform,new Vector2(.07f,top-.08f),new Vector2(.94f,top),17,normal);
            label.fontStyle=FontStyles.Bold;label.text=labels[i];
            var iconObject=new GameObject(labels[i]+" İkonu",typeof(RectTransform),typeof(CanvasRenderer),typeof(DispatchUnitIcon));
            iconObject.transform.SetParent(transform,false);
            var icon=iconObject.GetComponent<DispatchUnitIcon>();icon.UnitIndex=i;icon.raycastTarget=false;icon.color=new Color(.69f,.81f,.86f);
            var iconRect=icon.rectTransform;iconRect.anchorMin=new Vector2(.07f,top-.21f);iconRect.anchorMax=new Vector2(.33f,top-.08f);
            iconRect.offsetMin=iconRect.offsetMax=Vector2.zero;
            icons[i]=iconRect;
            numbers[i]=Text(labels[i]+" Sayısı",transform,new Vector2(.40f,top-.21f),new Vector2(.74f,top-.08f),38,normal);
            numbers[i].fontStyle=FontStyles.Bold;
            numbers[i].enableAutoSizing=true;numbers[i].fontSizeMin=22;numbers[i].fontSizeMax=38;
            deltas[i]=Text("Değişim",transform,new Vector2(.76f,top-.19f),new Vector2(.98f,top-.09f),18,normal);
            warnings[i]=Text("Yetersizlik",transform,new Vector2(.07f,top-.27f),new Vector2(.96f,top-.21f),14,new Color(1,.46f,.35f));
        }
        dispatchNotice=Text("Sevk Bildirimi",transform,new Vector2(.06f,.005f),new Vector2(.96f,.04f),12,cool);
        dispatchNotice.enableAutoSizing=true;dispatchNotice.fontSizeMin=9;dispatchNotice.fontSizeMax=12;
        dispatchNotice.alignment=TextAlignmentOptions.Center;dispatchNotice.alpha=0;
    }
    private void Dispatched(DispatchResourceEffect effect)
    {
        int index=effect.ambulances<0?0:effect.firefighters<0?1:2;
        dispatchNotice.text=index==0?"AMBULANS SEVK EDİLDİ":index==1?"İTFAİYE SEVK EDİLDİ":"KURTARMA EKİBİ SEVK EDİLDİ";
        iconUntil[index]=Time.unscaledTime+.30f;
        if(animation==null)animation=StartCoroutine(Animate());
        if(noticeRoutine!=null)StopCoroutine(noticeRoutine);
        noticeRoutine=StartCoroutine(Notice());
    }
    private System.Collections.IEnumerator Notice()
    {
        for(float t=0;t<dispatchNoticeDuration;t+=Time.unscaledDeltaTime)
        {
            float p=t/dispatchNoticeDuration;
            dispatchNotice.alpha=Mathf.Min(Mathf.Clamp01(p/.12f),Mathf.Clamp01((1-p)/.25f));
            yield return null;
        }
        dispatchNotice.alpha=0;dispatchNotice.text=string.Empty;noticeRoutine=null;
    }
    private void Changed(){Refresh(true);}
    private void Refresh(bool animate)
    {
        if(!resources)return;Build();
        for(int i=0;i<3;i++)
        {
            int value=i==0?resources.Ambulances:i==1?resources.Firefighters:resources.Police;
            int delta=value-previous[i];numbers[i].text=value.ToString();
            warnings[i].text=value==0?"! EKİP KALMADI":value<lowResourceThreshold?"! AZ EKİP":string.Empty;
            warnings[i].color=value==0?critical:amber;
            if(animate&&delta!=0)
            {
                feedbackUntil[i]=Time.unscaledTime+feedbackDuration;
                emphasis[i]=deltas[i].color=delta>0?cool:amber;
                deltas[i].text=delta>0?"+"+delta:delta.ToString();
            }
            previous[i]=value;
        }
        if(animation==null&&isActiveAndEnabled)animation=StartCoroutine(Animate());
    }
    private System.Collections.IEnumerator Animate()
    {
        // Runs only while a count is changing or a stock warning is present.
        bool active;
        do
        {
            active=false;
            for(int i=0;i<3;i++)
            {
                float remaining=Mathf.Clamp01((feedbackUntil[i]-Time.unscaledTime)/feedbackDuration);
                float pulse=Mathf.Sin((1-remaining)*Mathf.PI);
                numbers[i].rectTransform.localScale=Vector3.one*(1+(numberPulseScale-1)*pulse);
                numbers[i].color=Color.Lerp(previous[i]==0?critical:normal,emphasis[i],pulse);
                if(remaining<=0)deltas[i].text=string.Empty;else active=true;
                float iconRemaining=Mathf.Clamp01((iconUntil[i]-Time.unscaledTime)/.30f);
                icons[i].localScale=Vector3.one*(1+(iconPulseScale-1)*Mathf.Sin((1-iconRemaining)*Mathf.PI));
                if(iconRemaining>0)active=true;
                if(previous[i]<lowResourceThreshold)
                {
                    float cycle=warningCycle*(previous[i]==0?1.15f:1);
                    warnings[i].alpha=.45f+.55f*(.5f-.5f*Mathf.Cos(Time.unscaledTime/cycle*Mathf.PI*2));
                    active=true;
                }
            }
            yield return null;
        }while(active);
        animation=null;
    }
}
