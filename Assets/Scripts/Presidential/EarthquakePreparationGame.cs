using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static PreparationUI;

// Four seconds per input; wrong keys flash but never consume the step.
public sealed class EarthquakePreparationGame : MonoBehaviour
{
    private RectTransform root, bar, lamp;
    private Image character;
    private TMP_Text prompt;
    private readonly Image[] arrows=new Image[3];
    private readonly int[] sequence=new int[3];
    private readonly Sprite[] poses=new Sprite[4], keys=new Sprite[4];
    private readonly List<Sprite> owned=new List<Sprite>();
    private Action mistake;
    public int Step { get; private set; }
    public int RequiredDirection => Step<3?sequence[Step]:-1;
    public bool Running { get; private set; }
    public bool Succeeded => Step==3;
    public RectTransform View => root;
    public IReadOnlyList<Image> Arrows => arrows;
    private Sprite Cut(Texture2D sheet,int x,int y,int width,int height)
    {
        var sprite=PreparationUI.Sprite(sheet,new Rect(x/1097f,1-(y+height)/671f,width/1097f,height/671f));owned.Add(sprite);return sprite;
    }
    public void Initialize(Transform parent,TMP_FontAsset font,Action onMistake)
    {
        mistake=onMistake;
        var sheet=Resources.Load<Texture2D>("Disaster/EarthquakeSheet");
        if(!sheet)throw new InvalidOperationException("Deprem görseli bulunamadı.");
        poses[0]=Cut(sheet,40,4,104,160);poses[1]=Cut(sheet,155,20,108,140);
        poses[2]=Cut(sheet,576,43,180,119);poses[3]=Cut(sheet,793,43,180,119);
        for(int i=0;i<4;i++)keys[i]=Cut(sheet,40+i*99,471,83,78);
        root=Rect("Çök Kapan Tutun Eğitimi",parent,Vector2.zero,Vector2.one);
        Panel("Eğitim Zemini",root,Vector2.zero,Vector2.one,PanelColor);
        Label("Kısa Yönerge",root,new Vector2(.1f,.88f),new Vector2(.9f,.98f),"OKLARA SOLDAN SAĞA SIRAYLA BAS · HER ADIM 4 SANİYE",font,23);
        // The supplied character sheet is opaque. A single pale training card makes the crop edges intentional.
        Panel("Karakter Kartı",root,new Vector2(.06f,.32f),new Vector2(.94f,.85f),new Color(.96f,.97f,.98f));
        character=Panel("Aynı Karakter",root,new Vector2(.34f,.34f),new Vector2(.66f,.82f),Color.white);character.preserveAspect=true;
        var crack=Panel("Küçük Duvar Çatlağı",root,new Vector2(.08f,.43f),new Vector2(.22f,.76f),Color.white);crack.sprite=Cut(sheet,804,191,194,139);crack.preserveAspect=true;
        var light=Panel("Sallanan Lamba",root,new Vector2(.78f,.44f),new Vector2(.92f,.79f),Color.white);light.sprite=Cut(sheet,314,330,105,126);light.preserveAspect=true;lamp=light.rectTransform;
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
    public void Show(){root.gameObject.SetActive(true);}
    public void Hide(){Running=false;if(root)root.gameObject.SetActive(false);StopAllCoroutines();}
    public IEnumerator Play()
    {
        Step=0;Running=true;float remaining=4,flash=0;
        character.sprite=poses[0];prompt.text="! ÇÖK !";
        for(int i=0;i<3;i++)
        {
            do{sequence[i]=UnityEngine.Random.Range(0,4);}while(i>0&&sequence[i]==sequence[i-1]);
            arrows[i].sprite=keys[sequence[i]];arrows[i].color=Color.white;arrows[i].gameObject.SetActive(true);
            arrows[i].rectTransform.localScale=Vector3.one;arrows[i].rectTransform.localRotation=Quaternion.identity;
        }
        bar.anchorMax=Vector2.one;
        while(Running&&Step<3)
        {
            remaining-=Time.unscaledDeltaTime;flash=Mathf.Max(0,flash-Time.unscaledDeltaTime);
            if(remaining<=0){mistake?.Invoke();remaining=4;flash=.3f;prompt.text="TEKRAR DENE · "+new[]{"ÇÖK","KAPAN","TUTUN"}[Step];}
            bar.anchorMax=new Vector2(Mathf.Clamp01(remaining/4),1);
            for(int i=Step;i<3;i++)arrows[i].color=flash>0?Red:i==Step?new Color(1,.81f,.42f):Color.white;
            lamp.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.unscaledTime*8)*2.5f);
            character.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.unscaledTime*11)*.6f);
            prompt.color=Color.Lerp(Amber,White,.18f+.18f*Mathf.Sin(Time.unscaledTime*9));
            var kb=Keyboard.current;
            if(kb!=null)
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
                        StartCoroutine(Consume(arrows[Step].rectTransform));Step++;remaining=4;
                        character.sprite=poses[Step];
                        prompt.text=Step==1?"ÇÖK ✓  ·  ŞİMDİ KAPAN!":Step==2?"KAPAN ✓  ·  ŞİMDİ TUTUN!":"TUTUN!  BAŞINI KORU, MASAYA TUTUN.";
                    }
                    else{flash=.25f;}
                }
            }
            yield return null;
        }
        Running=false;character.rectTransform.localRotation=Quaternion.identity;lamp.localRotation=Quaternion.identity;
        yield return new WaitForSecondsRealtime(.3f);
    }
    private IEnumerator Consume(RectTransform arrow)
    {
        for(float t=0;t<.23f;t+=Time.unscaledDeltaTime){float p=t/.23f;arrow.localScale=Vector3.one*(1-p);arrow.localRotation=Quaternion.Euler(0,0,Mathf.Sin(p*32)*22*(1-p));yield return null;}
        arrow.gameObject.SetActive(false);
    }
    private void OnDestroy(){foreach(var sprite in owned)if(sprite)Destroy(sprite);if(root)Destroy(root.gameObject);}
}
