using TMPro;
using UnityEngine;
using UnityEngine.UI;

// This component only observes DispatchResources; previous values are animation snapshots.
public sealed class DispatchResourceHud : MonoBehaviour
{
    [SerializeField] private DispatchResources resources;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField, Range(.2f,.5f)] private float feedbackDuration = .35f;
    private readonly TMP_Text[] numbers=new TMP_Text[3], deltas=new TMP_Text[3], warnings=new TMP_Text[3];
    private readonly int[] previous=new int[3];
    private readonly float[] feedbackUntil=new float[3];
    public TMP_Text[] Numbers => numbers;
    private readonly Color normal=new Color(.84f,.9f,.86f);
    private void Awake(){Build();}
    private void OnEnable(){if(resources)resources.Changed+=Changed;Refresh(false);}
    private void OnDisable(){if(resources)resources.Changed-=Changed;}
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
        Text("Kaynak Başlığı",transform,new Vector2(.07f,.89f),new Vector2(.94f,.98f),18,new Color(.95f,.70f,.35f)).text="SAHA EKİPLERİ";
        string[] labels={"AMBULANS","İTFAİYE","KURTARMA EKİBİ"};
        for(int i=0;i<3;i++)
        {
            float top=.875f-i*.28f;
            Text(labels[i],transform,new Vector2(.07f,top-.08f),new Vector2(.94f,top),17,normal).text=labels[i];
            numbers[i]=Text(labels[i]+" Sayısı",transform,new Vector2(.08f,top-.21f),new Vector2(.53f,top-.08f),38,normal);
            deltas[i]=Text("Değişim",transform,new Vector2(.55f,top-.19f),new Vector2(.96f,top-.09f),24,normal);
            warnings[i]=Text("Yetersizlik",transform,new Vector2(.07f,top-.27f),new Vector2(.96f,top-.21f),14,new Color(1,.46f,.35f));
        }
    }
    private void Changed(){Refresh(true);}
    private void Refresh(bool animate)
    {
        if(!resources)return;Build();
        int[] values={resources.Ambulances,resources.Firefighters,resources.Police};
        for(int i=0;i<3;i++)
        {
            int delta=values[i]-previous[i];numbers[i].text=values[i].ToString();
            warnings[i].text=values[i]==0?"EKİP KALMADI":string.Empty;
            if(animate&&delta!=0)
            {
                feedbackUntil[i]=Time.unscaledTime+feedbackDuration;
                numbers[i].color=deltas[i].color=delta>0?new Color(.45f,1,.63f):new Color(1,.42f,.32f);
                deltas[i].text=delta>0?"+"+delta:delta.ToString();
            }
            previous[i]=values[i];
        }
    }
    private void Update()
    {
        for(int i=0;i<3;i++)if(numbers[i]&&Time.unscaledTime>=feedbackUntil[i])
        {numbers[i].color=previous[i]==0?new Color(1,.46f,.35f):normal;deltas[i].text=string.Empty;}
    }
}
