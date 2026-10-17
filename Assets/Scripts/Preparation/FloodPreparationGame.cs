using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PreparationUI;

public sealed class FloodPreparationGame : MonoBehaviour
{
    private RectTransform root;
    private Sprite mapSprite;
    private TMP_Text hint;
    private readonly Button[] points=new Button[4];
    private Action mistake,complete;
    private bool resolved;
    private float lastClick=-10;
    public const int HighGroundPoint = 3;
    public bool Succeeded => resolved;
    public System.Collections.Generic.IReadOnlyList<Button> Points => points;
    public void Initialize(Transform parent,TMP_FontAsset font,Action onMistake,Action onComplete)
    {
        mistake=onMistake;complete=onComplete;
        root=Rect("Sel Yüksek Bölge Haritası",parent,Vector2.zero,Vector2.one);
        Label("Yönerge",root,new Vector2(.04f,.89f),new Vector2(.96f,1),"SEL SIRASINDA HANGİ NOKTADA BULUNMALISIN?",font,25);
        var frame=Rect("Harita Alanı",root,new Vector2(.06f,.12f),new Vector2(.94f,.88f));
        var map=Rect("Sel Haritası",frame,Vector2.zero,Vector2.one);
        var fit=map.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1.5f;
        var image=map.gameObject.AddComponent<Image>();image.raycastTarget=false;
        mapSprite=PreparationUI.Sprite(Resources.Load<Texture2D>("Disaster/FloodMap"),new Rect(0,0,1,1));image.sprite=mapSprite;
        Vector2[] sites={new Vector2(.13f,.53f),new Vector2(.46f,.30f),new Vector2(.79f,.25f),new Vector2(.78f,.87f)};
        for(int i=0;i<4;i++)
        {
            int id=i;points[i]=Button("Sel Konumu "+(i+1),map,sites[i],sites[i],"",font);
            var r=(RectTransform)points[i].transform;r.sizeDelta=new Vector2(42,42);
            var outline=points[i].gameObject.AddComponent<Outline>();outline.effectColor=Navy;outline.effectDistance=new Vector2(4,-4);
            Label("Nokta",points[i].transform,Vector2.zero,Vector2.one,"●",font,29).color=White;
            points[i].onClick.AddListener(()=>Choose(id));
        }
        hint=Label("Harita İpucu",root,new Vector2(.04f,.01f),new Vector2(.96f,.11f),"Araziyi incele. Kırmızı noktalardan birini seç.",font,22);
        Hide();
    }
    public void Show()
    {
        StopAllCoroutines();resolved=false;lastClick=-10;root.gameObject.SetActive(true);
        hint.text="Araziyi incele. Kırmızı noktalardan birini seç.";hint.color=White;
        foreach(var point in points){point.interactable=true;point.image.color=Red;}
    }
    public void Choose(int index)
    {
        if(!root.gameObject.activeInHierarchy||resolved||index<0||index>=points.Length||Time.unscaledTime-lastClick<.35f)return;
        lastClick=Time.unscaledTime;
        if(index==HighGroundPoint)
        {
            resolved=true;points[index].image.color=Green;
            foreach(var point in points)point.interactable=false;
            hint.text="YÜKSEK BÖLGE SEÇİLDİ · SU BASKINI OLAN ALANLARDAN UZAKLAŞ.";hint.color=Green;complete?.Invoke();
        }
        else{mistake?.Invoke();hint.text="Burası daha alçakta. Sudan uzak, daha yüksek bir bölge seç.";hint.color=Amber;StartCoroutine(Flash(points[index].image));}
    }
    private IEnumerator Flash(Image image)
    {
        for(float t=0;t<.3f;t+=Time.unscaledDeltaTime){image.color=Color.Lerp(Red,Amber,Mathf.Abs(Mathf.Sin(t*25)));yield return null;}image.color=Red;
    }
    public void Hide(){if(root)root.gameObject.SetActive(false);StopAllCoroutines();}
    private void OnDestroy(){if(mapSprite)Destroy(mapSprite);if(root)Destroy(root.gameObject);}
}
