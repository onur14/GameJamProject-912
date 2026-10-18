using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PreparationUI;

// Storytelling, recording review and the educational public-service montage.
// The preparation activities remain owned by PreparationDirector.
public sealed class PreparationBroadcast : MonoBehaviour
{
    public enum BroadcastPhase { Briefing, Countdown, Recording, Review, Publishing, MonthsLater, Disaster, LoadingOperator }
    public static readonly string[] SafetyMessages={
        "Su, fener ve ilk yardım malzemelerini hazır tut.",
        "Çök, başını koruyarak kapan, sağlam masaya tutun.",
        "Su basan alanlardan uzak, yüksek bir bölgeye geç.",
        "Dedektörün pilini kontrol et; çalıştığını test et.",
        "Tüp göstergesini kontrol et; bakım için yetkili servise başvur.",
        "Doğalgaz hattını ve elektrik ana şalterini önceden tanı."
    };
    private PreparationDirector director;
    private TMP_FontAsset font;
    private RectTransform overlay, briefing, review, broadcast, story, storyCard, purposeOverlay, purposeCard;
    private CanvasGroup overlayGroup, purposeGroup;
    private Button startButton,retakeButton,publishButton,purposeButton;
    private TMP_Text reviewTimes,reviewReward,reviewHint,countdown,signal,playbackLabel,storyHeading,storyBody;
    private Image transmissionBar,storyFlash;
    private readonly List<Sprite> sprites=new List<Sprite>();
    private PreparationSpriteCutout earthquakeCutouts;
    private readonly Image[] clipFrames=new Image[PreparationRun.ActivityCount];
    private readonly TMP_Text[] clipMessages=new TMP_Text[PreparationRun.ActivityCount];
    private bool inputLocked=true;
    private bool purposeAcknowledged;
    public BroadcastPhase Phase { get; private set; } = BroadcastPhase.Briefing;
    public Button StartButton => startButton;
    public Button PurposeButton => purposeButton;
    public Button RetakeButton => retakeButton;
    public Button PublishButton => publishButton;
    public bool DidShake { get; private set; }
    public int ClipsShown { get; private set; }
    public Transform VisibleOverlay => overlay;

    public void Initialize(RectTransform screen,TMP_FontAsset typeface,PreparationDirector owner)
    {
        font=typeface;director=owner;
        overlay=Panel("Kamu Spotu Hikâye Katmanı",screen,Vector2.zero,Vector2.one,Navy).rectTransform;
        overlay.GetComponent<Image>().raycastTarget=true;overlayGroup=overlay.gameObject.AddComponent<CanvasGroup>();
        // Stepped film-strip decorations keep the composition in the project's pixel palette.
        for(int side=0;side<2;side++)for(int y=0;y<14;y++)
            Panel("Film Şeridi",overlay,new Vector2(side==0?.027f:.958f,.04f+y*.067f),new Vector2(side==0?.042f:.973f,.066f+y*.067f),PanelColor);
        briefing=Rect("Bakanlık Çekim Bilgilendirmesi",overlay,new Vector2(.11f,.10f),new Vector2(.89f,.91f));
        Label("Büyük Afete Hazırlık Başlığı",briefing,new Vector2(.03f,.79f),new Vector2(.97f,.99f),"AFETE HAZIRLIK",font,64);
        var stamp=Label("Bakanlık Görev Başlığı",briefing,new Vector2(.08f,.68f),new Vector2(.92f,.79f),"BAKANLIKTAN KAMU SPOTU ÇEKİM GÖREVİ",font,25);stamp.color=Amber;
        Panel("Başlık Çizgisi",briefing,new Vector2(.08f,.66f),new Vector2(.92f,.665f),Amber);
        Label("Çekim Görev Metni",briefing,new Vector2(.09f,.31f),new Vector2(.91f,.63f),
            "Bakanlık, afet bilincini artırmak için bir kamu spotu çekmeni istiyor.\n\nAltı hazırlık görevini toplam 90 saniye içinde doğru tamamla. Çektiğin video kamuoyuna yayınlanacak.",font,28);
        var reward=Label("Yeniden Çekim Ödülü",briefing,new Vector2(.10f,.17f),new Vector2(.90f,.31f),
            "Daha kısa bir yeniden çekim yap:\n+1 AMBULANS   +1 İTFAİYE   +1 KURTARMA EKİBİ\nBu ödül her koşuda bir kez verilir.",font,23);reward.color=Amber;
        startButton=Button("Çekime Başla",briefing,new Vector2(.31f,.025f),new Vector2(.82f,.115f),"ÇEKİME BAŞLA",font);startButton.onClick.AddListener(StartRecording);startButton.interactable=false;
        var menu=Button("Çekimden Ana Menü",briefing,new Vector2(.08f,.025f),new Vector2(.28f,.115f),"ANA MENÜ",font);
        menu.onClick.AddListener(()=>{if(inputLocked||Phase!=BroadcastPhase.Briefing||SceneFadeTransition.IsTransitioning)return;PreparationRun.Reset();SceneFadeTransition.LoadScene("Assets/Working Folder/Taha and Yasin/AnaMenu.unity",.3f,.4f);});
        countdown=Label("Çekim Geri Sayımı",overlay,new Vector2(.15f,.22f),new Vector2(.85f,.78f),"",font,100);countdown.gameObject.SetActive(false);
        BuildReview();BuildPublication();BuildStory();BuildPurposeNotice();
        review.gameObject.SetActive(false);broadcast.gameObject.SetActive(false);story.gameObject.SetActive(false);
    }
    private void BuildReview()
    {
        review=Rect("Çekim Değerlendirmesi",overlay,new Vector2(.10f,.10f),new Vector2(.90f,.91f));
        Label("Çekim Bitti Başlığı",review,new Vector2(.03f,.84f),new Vector2(.97f,.98f),"KAMU SPOTUN HAZIR",font,44);
        Label("En Kısa Video Sorusu",review,new Vector2(.06f,.72f),new Vector2(.94f,.84f),"EN KISA VİDEON BU MU?",font,34).color=Amber;
        reviewTimes=Label("Çekim Süreleri",review,new Vector2(.12f,.51f),new Vector2(.88f,.71f),"",font,29);
        reviewReward=Label("Kazanılan Ekip Ödülü",review,new Vector2(.10f,.30f),new Vector2(.90f,.51f),"",font,25);reviewReward.color=Amber;
        reviewHint=Label("Yayın Seçimi İpucu",review,new Vector2(.08f,.17f),new Vector2(.92f,.30f),"",font,22);
        retakeButton=Button("Yeniden Çek",review,new Vector2(.065f,.025f),new Vector2(.43f,.13f),"YENİDEN ÇEK",font);retakeButton.onClick.AddListener(Retake);
        publishButton=Button("Videoyu Kamuoyuyla Yayınla",review,new Vector2(.46f,.025f),new Vector2(.935f,.13f),"VİDEOYU KAMUOYUYLA YAYINLA",font);publishButton.onClick.AddListener(Publish);
    }
    private Sprite Art(string resource,Rect crop)
    {var s=PreparationUI.Sprite(Resources.Load<Texture2D>("Disaster/"+resource),crop);sprites.Add(s);return s;}
    private void BuildPublication()
    {
        broadcast=Rect("Kamu Yayını Montajı",overlay,new Vector2(.07f,.08f),new Vector2(.93f,.93f));
        Label("Yayın Başlığı",broadcast,new Vector2(.04f,.85f),new Vector2(.96f,.98f),"BİR VİDEO. 6 ÖNEMLİ AFET HAZIRLIĞI.",font,34);
        signal=Label("Yayın Bağlantı Durumu",broadcast,new Vector2(.10f,.75f),new Vector2(.90f,.85f),"YAYIN AĞINA BAĞLANILIYOR",font,24);signal.color=Amber;
        string[] titles={"HAZIR TUT","KENDİNİ KORU","YÜKSEĞE ÇIK","ERKEN UYARI","BASINCI KONTROL ET","HATLARI TANI"};
        earthquakeCutouts=new PreparationSpriteCutout(Resources.Load<Texture2D>("Disaster/EarthquakeSheet"));
        Sprite[] thumbs={Art("HouseMap",new Rect(.05f,.53f,.41f,.42f)),earthquakeCutouts.Cut(817,72,119,91,keepLargest:true),Art("FloodMap",new Rect(.56f,.60f,.42f,.39f)),Art("DetectorSheet",new Rect(0,.12f,.24f,.78f)),Art("ExtinguisherTraining",new Rect(0,0,1,1)),Art("UtilityTrainingSheet",new Rect(.6f,0,.2f,1))};
        for(int i=0;i<PreparationRun.ActivityCount;i++)
        {
            float x=.015f+(i%3)*.325f; float y=i<3?.47f:.205f;
            clipFrames[i]=Panel("Güvenli Davranış Klibi "+(i+1),broadcast,new Vector2(x,y),new Vector2(x+.315f,y+.255f),new Color(.18f,.26f,.30f));
            var card=Panel("Klip Kartı",clipFrames[i].transform,new Vector2(.014f,.012f),new Vector2(.986f,.988f),PanelColor);
            Label("Klip Adı",card.transform,new Vector2(.03f,.81f),new Vector2(.97f,.98f),titles[i],font,22);
            var art=Panel("Klip Önizlemesi",card.transform,new Vector2(.05f,.37f),new Vector2(.95f,.80f),Color.white);art.sprite=thumbs[i];art.preserveAspect=true;
            clipMessages[i]=Label("Güvenlik Mesajı",card.transform,new Vector2(.07f,.035f),new Vector2(.93f,.34f),SafetyMessages[i],font,21);
        }
        var track=Panel("Yayın İlerleme Zemini",broadcast,new Vector2(.04f,.155f),new Vector2(.96f,.18f),PanelColor);
        transmissionBar=Panel("Yayın İlerleme Çubuğu",track.transform,Vector2.zero,new Vector2(0,1),Amber);
        playbackLabel=Label("Yayın Alt Bilgisi",broadcast,new Vector2(.05f,.035f),new Vector2(.95f,.14f),"",font,24);
    }
    private void BuildStory()
    {
        story=Rect("Aylar Sonra Geçişi",overlay,Vector2.zero,Vector2.one);
        storyCard=Rect("Sarsılan Haber Kartı",story,new Vector2(.10f,.19f),new Vector2(.90f,.83f));
        storyHeading=Label("Zaman ve Afet Başlığı",storyCard,new Vector2(.02f,.53f),new Vector2(.98f,.94f),"AYLAR SONRA...",font,57);
        storyBody=Label("Operatör Görevine Geçiş Metni",storyCard,new Vector2(.08f,.10f),new Vector2(.92f,.50f),"",font,30);
        Panel("Haber Alt Çizgisi",storyCard,new Vector2(.18f,.04f),new Vector2(.82f,.047f),Amber);
        storyFlash=Panel("Sarsıntı Işık Etkisi",story,Vector2.zero,Vector2.one,new Color(.72f,.19f,.13f,0));
    }
    private void BuildPurposeNotice()
    {
        purposeOverlay=Panel("Çekim Öncesi Uyarı",overlay,Vector2.zero,Vector2.one,new Color(.01f,.025f,.04f,.96f)).rectTransform;
        purposeOverlay.GetComponent<Image>().raycastTarget=true;
        purposeGroup=purposeOverlay.gameObject.AddComponent<CanvasGroup>();
        purposeCard=Panel("Afete Hazırlık Bilgi Kutusu",purposeOverlay,new Vector2(.14f,.20f),new Vector2(.86f,.80f),PanelColor).rectTransform;
        Panel("Uyarı Üst Çizgisi",purposeCard,new Vector2(.02f,.96f),new Vector2(.98f,.97f),Amber);
        Label("Uyarı Başlığı",purposeCard,new Vector2(.06f,.72f),new Vector2(.94f,.94f),"ÖNCE HAZIRLAN, SONRA ANLAT",font,42).color=Amber;
        Label("Uyarı Açıklaması",purposeCard,new Vector2(.08f,.28f),new Vector2(.92f,.72f),
            "Afetlere hazırlık yaparak insanların güvenli davranışları öğrenmesini sağlıyoruz. Altı hazırlık uygulamasını deneyip bir kamu spotu çekiyoruz; öğrendiklerimizi kamuoyuna anlatarak afet bilincini artırıyoruz.",font,29);
        purposeButton=Button("Uyarıyı Onayla",purposeCard,new Vector2(.36f,.07f),new Vector2(.64f,.22f),"ANLADIM",font);
        purposeButton.onClick.AddListener(()=>purposeAcknowledged=true);
        purposeOverlay.gameObject.SetActive(false);
    }
    public IEnumerator ShowBriefing()
    {
        Phase=BroadcastPhase.Briefing;inputLocked=true;
        var group=briefing.gameObject.AddComponent<CanvasGroup>();
        for(float t=0;t<.45f;t+=Time.unscaledDeltaTime)
        {float p=Mathf.Clamp01(t/.45f);group.alpha=p;briefing.localScale=Vector3.one*(1+.025f*(1-p));yield return null;}
        group.alpha=1;briefing.localScale=Vector3.one;inputLocked=false;startButton.interactable=true;
    }
    public void StartRecording()
    {
        if(inputLocked||Phase!=BroadcastPhase.Briefing)return;
        inputLocked=true;startButton.interactable=false;StartCoroutine(PurposeThenCountdown());
    }
    public void Retake()
    {
        if(inputLocked||Phase!=BroadcastPhase.Review||PreparationRun.Published)return;
        inputLocked=true;retakeButton.interactable=publishButton.interactable=false;StartCoroutine(Countdown());
    }
    private IEnumerator PurposeThenCountdown()
    {
        Phase=BroadcastPhase.Countdown;briefing.gameObject.SetActive(false);
        purposeAcknowledged=false;purposeOverlay.gameObject.SetActive(true);purposeButton.interactable=false;
        float width=Mathf.Max(overlay.rect.width,1920);
        for(float elapsed=0;elapsed<.45f;elapsed+=Time.unscaledDeltaTime)
        {
            float progress=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.45f));
            purposeCard.anchoredPosition=Vector2.left*width*(1-progress);
            purposeGroup.alpha=progress;
            yield return null;
        }
        purposeCard.anchoredPosition=Vector2.zero;purposeGroup.alpha=1;purposeButton.interactable=true;
        while(!purposeAcknowledged)yield return null;
        purposeButton.interactable=false;
        for(float elapsed=0;elapsed<.3f;elapsed+=Time.unscaledDeltaTime)
        {
            float progress=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.3f));
            purposeCard.anchoredPosition=Vector2.right*width*progress;
            purposeGroup.alpha=1-progress;
            yield return null;
        }
        purposeOverlay.gameObject.SetActive(false);purposeCard.anchoredPosition=Vector2.zero;purposeGroup.alpha=1;
        yield return Countdown();
    }
    private IEnumerator Countdown()
    {
        Phase=BroadcastPhase.Countdown;briefing.gameObject.SetActive(false);review.gameObject.SetActive(false);countdown.gameObject.SetActive(true);
        for(int n=3;n>0;n--)
        {
            countdown.text="ÇEKİM "+(PreparationRun.TakeNumber+1).ToString("00")+"\n"+n;
            for(float t=0;t<.65f;t+=Time.unscaledDeltaTime)
            {float p=t/.65f;countdown.rectTransform.localScale=Vector3.one*(1+.10f*(1-p));countdown.color=Color.Lerp(Amber,White,p);yield return null;}
        }
        countdown.gameObject.SetActive(false);countdown.rectTransform.localScale=Vector3.one;
        Phase=BroadcastPhase.Recording;
        for(float t=0;t<.25f;t+=Time.unscaledDeltaTime){overlayGroup.alpha=1-t/.25f;yield return null;}
        overlayGroup.alpha=1;overlay.gameObject.SetActive(false);inputLocked=false;director.BeginRecording();
    }
    public void ShowReview()
    {
        Phase=BroadcastPhase.Review;inputLocked=false;overlay.gameObject.SetActive(true);overlayGroup.alpha=1;
        briefing.gameObject.SetActive(false);review.gameObject.SetActive(true);
        reviewTimes.text="BU ÇEKİM    "+PreparationRun.FormatDuration(PreparationRun.LastTakeSeconds)+"\nEN KISA ÇEKİM    "+PreparationRun.FormatDuration(PreparationRun.BestTakeSeconds);
        reviewReward.text=PreparationRun.RewardEarned?
            (PreparationRun.LastTakeImproved?"DAHA KISA ÇEKTİN! EKİP ÖDÜLÜ KAZANDIN.":"EKİP ÖDÜLÜN KORUNUYOR.")+"\n+1 AMBULANS  ·  +1 İTFAİYE  ·  +1 KURTARMA":
            "DAHA KISA BİR YENİDEN ÇEKİM YAPARSAN\n+1 AMBULANS  ·  +1 İTFAİYE  ·  +1 KURTARMA KAZANIRSIN.";
        reviewReward.color=PreparationRun.RewardEarned?Green:Amber;
        reviewHint.text="En kısa tamamlanmış çekimin yayınlanacak.\nÖdül bir defalıktır; daha yavaş bir deneme en iyi videonu silmez.";
        retakeButton.interactable=publishButton.interactable=true;
    }
    public void Publish()
    {
        if(inputLocked||Phase!=BroadcastPhase.Review||!PreparationRun.Publish())return;
        inputLocked=true;retakeButton.interactable=publishButton.interactable=false;
        StartCoroutine(PublishAndAdvance());
    }
    private IEnumerator PublishAndAdvance()
    {
        const float videoTimeScale=.42f;
        const float connectionDuration=1.1f*videoTimeScale;
        const float clipDuration=1.6f*videoTimeScale;
        const float fadeDuration=.5f*videoTimeScale;
        Phase=BroadcastPhase.Publishing;review.gameObject.SetActive(false);broadcast.gameObject.SetActive(true);ClipsShown=0;
        foreach(var frame in clipFrames)frame.color=new Color(.18f,.26f,.30f);
        for(float t=0;t<connectionDuration;t+=Time.unscaledDeltaTime)
        {signal.text="YAYIN AĞINA BAĞLANILIYOR"+new string('.',1+Mathf.FloorToInt(t/connectionDuration*4)%3);transmissionBar.rectTransform.anchorMax=new Vector2(t/connectionDuration*.14f,1);yield return null;}
        signal.text="KAMU SPOTU YAYINDA";signal.color=Green;
        for(int clip=0;clip<PreparationRun.ActivityCount;clip++)
        {
            clipFrames[clip].color=Amber;ClipsShown=clip+1;
            playbackLabel.text="KLİP "+(clip+1)+" · TOPLAM 6 · "+SafetyMessages[clip];
            for(float t=0;t<clipDuration;t+=Time.unscaledDeltaTime)
            {
                float p=t/clipDuration;transmissionBar.rectTransform.anchorMax=new Vector2(.14f+.86f*(clip+p)/PreparationRun.ActivityCount,1);
                clipFrames[clip].rectTransform.localScale=Vector3.one*(1+.018f*Mathf.Sin(p*Mathf.PI));yield return null;
            }
            clipFrames[clip].rectTransform.localScale=Vector3.one;clipFrames[clip].color=Green;
        }
        transmissionBar.rectTransform.anchorMax=Vector2.one;playbackLabel.text="KAMU SPOTU YAYINLANDI · HAZIRLIK BİLGİSİ PAYLAŞILDI";
        yield return new WaitForSecondsRealtime(connectionDuration);
        var videoFade=broadcast.gameObject.AddComponent<CanvasGroup>();
        for(float t=0;t<fadeDuration;t+=Time.unscaledDeltaTime){videoFade.alpha=1-t/fadeDuration;yield return null;}
        broadcast.gameObject.SetActive(false);story.gameObject.SetActive(true);Phase=BroadcastPhase.MonthsLater;
        storyHeading.text="AYLAR SONRA...";storyHeading.color=White;storyBody.text="";overlayGroup.alpha=1;
        for(float t=0;t<2f;t+=Time.unscaledDeltaTime){storyHeading.alpha=Mathf.Clamp01(t/.45f);yield return null;}
        Phase=BroadcastPhase.Disaster;storyHeading.text="ÜLKEDE BİRDEN FAZLA\nAFET YAŞANDI";storyHeading.color=Amber;storyHeading.alpha=1;
        storyBody.text="Şimdi toplumu bilinçlendirme\nve destek olma zamanı.\n\nAcil çağrı operatörü olarak göreve başlıyorsun.";
        DidShake=true;
        for(float t=0;t<1.25f;t+=Time.unscaledDeltaTime)
        {
            float decay=1-t/1.25f;
            storyCard.anchoredPosition=new Vector2(Mathf.Round(Mathf.Sin(t*53)*8*decay),Mathf.Round(Mathf.Sin(t*39)*5*decay));
            storyFlash.color=new Color(.72f,.19f,.13f,.055f*decay*Mathf.Abs(Mathf.Sin(t*15)));yield return null;
        }
        storyCard.anchoredPosition=Vector2.zero;storyFlash.color=Color.clear;
        yield return new WaitForSecondsRealtime(3.3f);
        Phase=BroadcastPhase.LoadingOperator;
        SceneFadeTransition.LoadScene(PreparationRun.Destination,.5f,.6f);
    }
    private void OnDestroy(){earthquakeCutouts?.Dispose();foreach(var sprite in sprites)if(sprite)Destroy(sprite);}
}
