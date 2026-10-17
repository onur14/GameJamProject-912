using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static PreparationUI;

public sealed class EarthquakePreparationGame : MonoBehaviour
{
    [SerializeField, Range(.35f, .55f)] private float crouchDuration = .46f;
    [SerializeField, Range(.4f, .7f)] private float coverDuration = .58f;
    [SerializeField, Range(.35f, .6f)] private float holdDuration = .45f;
    [SerializeField, Range(1.8f, 2.5f)] private float pixelScale = 2.2f;
    private RectTransform root, bar, lamp, characterRect;
    private Image character;
    private TMP_Text prompt;
    private readonly Image[] arrows=new Image[3];
    private readonly int[] sequence=new int[3];
    private readonly Sprite[] poses=new Sprite[6], keys=new Sprite[4];
    private PreparationSpriteCutout cutouts;
    private Action mistake;
    public int Step { get; private set; }
    public int CurrentPose { get; private set; }
    public bool IsTransitioning { get; private set; }
    public int RequiredDirection => Step<3?sequence[Step]:-1;
    public bool Running { get; private set; }
    public bool Succeeded => Step==3;
    public RectTransform View => root;
    public IReadOnlyList<Image> Arrows => arrows;
    public void Initialize(Transform parent,TMP_FontAsset font,Action onMistake)
    {
        mistake=onMistake;
        var sheet=Resources.Load<Texture2D>("Disaster/EarthquakeSheet");
        if(!sheet)throw new InvalidOperationException("Deprem görseli bulunamadı.");
        cutouts=new PreparationSpriteCutout(sheet);
        poses[0]=cutouts.Cut(40,4,104,160,keepLargest:true);
        poses[1]=cutouts.Cut(155,20,108,140,keepLargest:true);
        poses[2]=cutouts.Cut(286,38,113,126,keepLargest:true);
        poses[3]=cutouts.Cut(419,42,131,121,keepLargest:true);
        poses[4]=cutouts.Cut(609,73,116,90,keepLargest:true);
        poses[5]=cutouts.Cut(817,72,119,91,keepLargest:true);
        for(int i=0;i<4;i++)keys[i]=cutouts.Cut(40+i*99,471,83,72);
        root=Rect("Çök Kapan Tutun Eğitimi",parent,Vector2.zero,Vector2.one);
        Panel("Eğitim Zemini",root,Vector2.zero,Vector2.one,PanelColor);
        Label("Kısa Yönerge",root,new Vector2(.1f,.88f),new Vector2(.9f,.98f),"OKLARA SOLDAN SAĞA SIRAYLA BAS · HER ADIM 4 SANİYE",font,23);
        var room=Rect("Korunaklı Oda",root,new Vector2(.06f,.32f),new Vector2(.94f,.85f));
        Panel("Duvar",room,Vector2.zero,Vector2.one,new Color(.13f,.20f,.25f));
        Panel("Zemin",room,Vector2.zero,new Vector2(1,.23f),new Color(.075f,.12f,.16f));
        Panel("Zemin Çizgisi",room,new Vector2(0,.23f),new Vector2(1,.238f),new Color(.31f,.39f,.40f));
        var desk=Panel("Sağlam Masa",room,new Vector2(.62f,.13f),new Vector2(.62f,.13f),Color.white);
        desk.sprite=cutouts.Cut(576,43,180,119,true);desk.rectTransform.pivot=new Vector2(.5f,0);
        desk.rectTransform.sizeDelta=desk.sprite.rect.size*pixelScale;
        character=Panel("Aynı Karakter",room,new Vector2(.33f,.11f),new Vector2(.33f,.11f),Color.white);
        characterRect=character.rectTransform;characterRect.pivot=new Vector2(.5f,0);SetPose(0);
        var crack=Panel("Küçük Duvar Çatlağı",room,new Vector2(.09f,.45f),new Vector2(.20f,.75f),Color.white);
        crack.sprite=cutouts.Cut(804,191,194,139,removeBright:true);crack.preserveAspect=true;
        crack.transform.SetSiblingIndex(desk.transform.GetSiblingIndex());
        var light=Panel("Sallanan Lamba",room,new Vector2(.81f,.97f),new Vector2(.81f,.97f),Color.white);
        light.sprite=cutouts.Cut(314,330,105,126,keepLargest:true,removeBright:true);lamp=light.rectTransform;
        lamp.pivot=new Vector2(.5f,1);lamp.sizeDelta=light.sprite.rect.size*1.4f;
        prompt=Label("Hareket Uyarısı",root,new Vector2(.13f,.23f),new Vector2(.87f,.34f),"! ÇÖK !",font,34);prompt.color=Amber;
        for(int i=0;i<3;i++)
        {
            arrows[i]=Panel("Sıralı Ok "+i,root,new Vector2(.35f+i*.105f,.055f),new Vector2(.44f+i*.105f,.22f),Color.white);
            arrows[i].preserveAspect=true;
        }
        var track=Panel("Süre Zemini",root,new Vector2(.08f,.015f),new Vector2(.92f,.04f),new Color(.18f,.24f,.28f));
        bar=Panel("Küçülen Adım Süresi",track.transform,Vector2.zero,Vector2.one,Amber).rectTransform;
        Hide();
    }
    private void SetPose(int index)
    {
        CurrentPose=index;character.sprite=poses[index];characterRect.sizeDelta=poses[index].rect.size*pixelScale;
    }
    public void Show(){root.gameObject.SetActive(true);}
    public void Hide()
    {
        Running=false;IsTransitioning=false;StopAllCoroutines();
        if(root)root.gameObject.SetActive(false);
        if(characterRect)characterRect.localRotation=Quaternion.identity;
        if(lamp)lamp.localRotation=Quaternion.identity;
    }
    public IEnumerator Play()
    {
        Step=0;Running=true;IsTransitioning=false;float remaining=4,flash=0;
        characterRect.anchorMin=characterRect.anchorMax=new Vector2(.33f,.11f);
        SetPose(0);prompt.text="! ÇÖK !";
        for(int i=0;i<3;i++)
        {
            do{sequence[i]=UnityEngine.Random.Range(0,4);}while(i>0&&sequence[i]==sequence[i-1]);
            arrows[i].sprite=keys[sequence[i]];arrows[i].color=Color.white;arrows[i].gameObject.SetActive(true);
            arrows[i].rectTransform.localScale=Vector3.one;arrows[i].rectTransform.localRotation=Quaternion.identity;
        }
        bar.anchorMax=Vector2.one;
        while(Running&&Step<3)
        {
            if(!IsTransitioning)remaining-=Time.unscaledDeltaTime;
            flash=Mathf.Max(0,flash-Time.unscaledDeltaTime);
            if(remaining<=0){mistake?.Invoke();remaining=4;flash=.3f;prompt.text="TEKRAR DENE · "+new[]{"ÇÖK","KAPAN","TUTUN"}[Step];}
            bar.anchorMax=new Vector2(Mathf.Clamp01(remaining/4),1);
            for(int i=Step;i<3;i++)arrows[i].color=flash>0?Red:i==Step?new Color(1,.81f,.42f):Color.white;
            lamp.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.unscaledTime*8)*2.5f);
            characterRect.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.unscaledTime*11)*.6f);
            prompt.color=Color.Lerp(Amber,White,.18f+.18f*Mathf.Sin(Time.unscaledTime*9));
            var kb=Keyboard.current;
            if(kb!=null&&!IsTransitioning)
            {
                int pressed=-1,count=0;
                if(kb.upArrowKey.wasPressedThisFrame){pressed=0;count++;}
                if(kb.downArrowKey.wasPressedThisFrame){pressed=1;count++;}
                if(kb.leftArrowKey.wasPressedThisFrame){pressed=2;count++;}
                if(kb.rightArrowKey.wasPressedThisFrame){pressed=3;count++;}
                if(count>0)
                {
                    if(count==1&&pressed==sequence[Step])
                    {
                        StartCoroutine(Consume(arrows[Step].rectTransform));
                        yield return AnimateStep(Step);
                        if(!Running)yield break;
                        Step++;remaining=4;
                        prompt.text=Step==1?"ÇÖK TAMAM  ·  ŞİMDİ KAPAN!":Step==2?"KAPAN TAMAM  ·  ŞİMDİ TUTUN!":"TUTUN!  BAŞINI KORU, MASAYA TUTUN.";
                    }
                    else{flash=.25f;}
                }
            }
            yield return null;
        }
        Running=false;characterRect.localRotation=Quaternion.identity;lamp.localRotation=Quaternion.identity;
        yield return new WaitForSecondsRealtime(.3f);
    }
    private IEnumerator AnimateStep(int stage)
    {
        IsTransitioning=true;
        float duration=stage==0?crouchDuration:stage==1?coverDuration:holdDuration;
        Vector2 start=characterRect.anchorMin;
        Vector2 target=stage==0?new Vector2(.38f,.11f):stage==1?new Vector2(.61f,.11f):new Vector2(.63f,.11f);
        int middle=stage==0?1:stage==1?3:5;
        int finish=stage==0?2:stage==1?4:5;
        bool changed=false;
        for(float elapsed=0;elapsed<duration&&Running;elapsed+=Time.unscaledDeltaTime)
        {
            float progress=Mathf.Clamp01(elapsed/duration);
            float eased=progress*progress*(3-2*progress);
            Vector2 position=Vector2.Lerp(start,target,eased);
            characterRect.anchorMin=characterRect.anchorMax=position;
            if(!changed&&progress>=.38f){SetPose(middle);changed=true;}
            yield return null;
        }
        if(!Running)yield break;
        characterRect.anchorMin=characterRect.anchorMax=target;
        SetPose(finish);
        IsTransitioning=false;
    }
    private IEnumerator Consume(RectTransform arrow)
    {
        for(float t=0;t<.23f;t+=Time.unscaledDeltaTime){float p=t/.23f;arrow.localScale=Vector3.one*(1-p);arrow.localRotation=Quaternion.Euler(0,0,Mathf.Sin(p*32)*22*(1-p));yield return null;}
        arrow.gameObject.SetActive(false);
    }
    private void OnDestroy(){cutouts?.Dispose();if(root)Destroy(root.gameObject);}
}
