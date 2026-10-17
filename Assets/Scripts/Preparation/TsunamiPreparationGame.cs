using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static PreparationUI;

// A short route in the existing preparation modal; the director owns the total timer.
public sealed class TsunamiPreparationGame : MonoBehaviour
{
    private RectTransform root,stage;
    private Image player,water;
    private readonly Image[] waveFront=new Image[3];
    private TMP_Text hint;
    private PreparationSpriteCutout cutouts;
    private Sprite idle,runA,runB;
    private Action mistake,complete;
    private CanvasGroup modal;
    private Vector2 position;
    private float waveX,slowUntil,lastCollision,escapeElapsed;
    private readonly Rect[] barriers={new Rect(270,0,42,248),new Rect(490,177,42,243),new Rect(710,0,42,248)};
    public bool Running { get; private set; }
    public bool Succeeded { get; private set; }
    public bool Failed { get; private set; }
    public Vector2 Position => position;
    public float WavePosition => waveX;
    public int CollisionCount { get; private set; }
    public Button RetryButton { get; private set; }
    public void Initialize(Transform parent,TMP_FontAsset font,Action onMistake,Action onComplete)
    {
        mistake=onMistake;complete=onComplete;
        root=Rect("Tsunami Bariyer Kaçışı",parent,Vector2.zero,Vector2.one);
        modal=root.GetComponentInParent<CanvasGroup>();
        Label("Tsunami Yönergesi",root,new Vector2(.02f,.90f),new Vector2(.98f,1),"OK TUŞLARI / WASD · ENGELLERİ DOLAŞ, YÜKSEK BÖLGEYE ULAŞ",font,25);
        var frame=Rect("Tsunami Oyun Alanı",root,new Vector2(.02f,.16f),new Vector2(.98f,.89f));
        stage=Rect("Kıyıdan İç Kesime Rota",frame,Vector2.zero,Vector2.one);
        var fit=stage.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1000f/420;
        stage.gameObject.AddComponent<RectMask2D>();
        cutouts=new PreparationSpriteCutout(Resources.Load<Texture2D>("Disaster/TsunamiSheet"));
        var ground=cutouts.Cut(363,410,64,64);
        var wornGround=cutouts.Cut(455,410,64,64);
        for(int y=0;y<4;y++)for(int x=0;x<10;x++)Art("Zemin",(x+y)%3==0?wornGround:ground,new Rect(x*100,y*105,100,105));
        var obstacle=cutouts.Cut(626,412,65,62,keepLargest:true);
        for(int i=0;i<barriers.Length;i++)
        {
            var wall=barriers[i];
            var bg=Panel("Geçilmez Bariyer "+(i+1),stage,Vector2.zero,Vector2.one,new Color(.13f,.18f,.20f));Place(bg,wall);
            for(float y=wall.y;y<wall.yMax;y+=60)Art("Bariyer Kayası",obstacle,new Rect(wall.x-8,y,58,Mathf.Min(65,wall.yMax-y)));
        }
        var safe=Panel("Güvenli Bölge Zemini",stage,Vector2.zero,Vector2.one,new Color(.12f,.32f,.24f,.8f));Place(safe,new Rect(860,0,140,420));
        Art("Yüksek Güvenli Bölge",cutouts.Cut(748,246,234,141,keepLargest:true,removeBright:true),new Rect(850,200,150,125));
        Label("Güvenli Bölge Yazısı",stage,new Vector2(.86f,.02f),new Vector2(.995f,.19f),"YÜKSEK\nBÖLGE",font,20).color=White;
        water=Panel("İlerleyen Su",stage,Vector2.zero,Vector2.one,new Color(.08f,.36f,.61f,.88f));
        var waveSprite=cutouts.Cut(421,4,202,151,keepLargest:true);
        for(int i=0;i<waveFront.Length;i++)
        {
            waveFront[i]=Art("Tsunami Dalga Cephesi",waveSprite,new Rect(-240,20+i*140,140,100));
            waveFront[i].rectTransform.localEulerAngles=new Vector3(0,0,-90);
        }
        idle=cutouts.Cut(72,64,41,79,keepLargest:true);runA=cutouts.Cut(300,66,48,79,keepLargest:true);runB=cutouts.Cut(348,60,77,85,keepLargest:true);
        player=Art("Kaçan Oyuncu",idle,new Rect(80,290,40,56));
        hint=Label("Tsunami İpucu",root,new Vector2(.02f,.01f),new Vector2(.77f,.14f),"",font,21);
        RetryButton=Button("Tsunamiyi Yeniden Dene",root,new Vector2(.78f,.015f),new Vector2(.98f,.13f),"TEKRAR DENE",font);RetryButton.onClick.AddListener(()=>{if(Failed)Show();});
        Hide();
    }
    private Image Art(string name,Sprite sprite,Rect rect)
    {var image=Panel(name,stage,Vector2.zero,Vector2.one,Color.white);image.sprite=sprite;Place(image,rect);return image;}
    private void Place(Image image,Rect rect)
    {var r=image.rectTransform;r.anchorMin=new Vector2(rect.xMin/1000,rect.yMin/420);r.anchorMax=new Vector2(rect.xMax/1000,rect.yMax/420);r.offsetMin=r.offsetMax=Vector2.zero;}
    public void Show()
    {
        if(root==null)return;
        root.gameObject.SetActive(true);Running=true;Succeeded=Failed=false;position=new Vector2(85,320);waveX=-120;
        CollisionCount=0;escapeElapsed=0;slowUntil=lastCollision=-10;RetryButton.gameObject.SetActive(false);
        hint.text="Kıyıda bekleme. Engellerin açık taraflarından iç kesime ve yükseğe kaç.";hint.color=White;Render(Vector2.zero);
    }
    public void Hide(){Running=false;if(root)root.gameObject.SetActive(false);}
    private bool Blocked(Vector2 next)
    {
        var body=new Rect(next.x-15,next.y-17,30,34);
        foreach(var barrier in barriers)if(body.Overlaps(barrier))return true;
        return false;
    }
    private void Update()
    {
        if(!Running||!root.gameObject.activeInHierarchy||modal&&!modal.interactable)return;
        var keyboard=Keyboard.current;Vector2 input=Vector2.zero;
        if(keyboard!=null)
        {
            if(keyboard.rightArrowKey.isPressed||keyboard.dKey.isPressed)input.x++;
            if(keyboard.leftArrowKey.isPressed||keyboard.aKey.isPressed)input.x--;
            if(keyboard.upArrowKey.isPressed||keyboard.wKey.isPressed)input.y++;
            if(keyboard.downArrowKey.isPressed||keyboard.sKey.isPressed)input.y--;
        }
        input=Vector2.ClampMagnitude(input,1);float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
        Vector2 delta=input*(Time.unscaledTime<slowUntil?175:285)*dt;
        bool hit=false;var next=position;next.x=Mathf.Clamp(next.x+delta.x,18,982);
        if(!Blocked(next))position=next;else hit=true;
        next=position;next.y=Mathf.Clamp(next.y+delta.y,20,400);
        if(!Blocked(next))position=next;else hit=true;
        if(hit&&Time.unscaledTime-lastCollision>.7f)
        {lastCollision=Time.unscaledTime;slowUntil=Time.unscaledTime+.28f;CollisionCount++;hint.text="BARİYER · Açık taraftan dolaş!";hint.color=Amber;GameAudioFeedback.Feedback(false);}
        escapeElapsed+=dt;if(escapeElapsed>1)waveX+=76*dt;Render(input);
        if(position.x>=880)
        {Running=false;Succeeded=true;hint.text="GÜVENLİ BÖLGE · Kıyıdan uzaklaş, yüksek araziye çık.";hint.color=Green;complete?.Invoke();}
        else if(position.x-15<=waveX)
        {Running=false;Failed=true;hint.text="DALGA YETİŞTİ · Rotayı yeniden dene. Toplam süre devam ediyor.";hint.color=Amber;RetryButton.gameObject.SetActive(true);mistake?.Invoke();}
    }
    private void Render(Vector2 input)
    {
        Place(player,new Rect(position.x-20,position.y-25,40,56));
        player.sprite=input.sqrMagnitude>.01f?(Mathf.FloorToInt(Time.unscaledTime*9)%2==0?runA:runB):idle;
        Place(water,new Rect(-150,0,Mathf.Max(0,waveX+150),420));for(int i=0;i<waveFront.Length;i++)Place(waveFront[i],new Rect(waveX-120,20+i*140,140,100));
    }
    private void OnDestroy(){cutouts?.Dispose();if(root)Destroy(root.gameObject);}
}
