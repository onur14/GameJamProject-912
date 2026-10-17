using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PreparationUI;

// The only owner of preparation activity entry, completion, and scene hand-off.
public sealed class PreparationDirector : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private MiniGameWindow bagWindow;
    [SerializeField] private AudioClip activitySuccess;
    [SerializeField] private AudioClip activityFailure;
    private AudioSource activityAudio;
    private RectTransform house, activityHost;
    private CanvasGroup mapGroup, modal;
    private TMP_Text status, activityTitle, feedback;
    private CanvasGroup feedbackGroup;
    private Coroutine feedbackAnimation;
    private Image feedbackFlash;
    private TMP_Text recordingClock;
    private PreparationBroadcast broadcast;
    private Button back;
    public const float ActivityTimeLimit=45f;
    private float remainingSeconds;
    private bool timerRunning;
    private Image timerFill;
    private GameObject timerStrip;
    private TMP_Text timerLabel;
    private GameObject timeoutPanel;
    private Button retryButton, timeoutReturnButton;
    public float RemainingSeconds => remainingSeconds;
    public bool TimerRunning => timerRunning;
    public bool TimedOut => timeoutPanel && timeoutPanel.activeSelf;
    public Button RetryButton => retryButton;
    public Button TimeoutReturnButton => timeoutReturnButton;
    private readonly Button[] markers=new Button[PreparationRun.ActivityCount];
    private readonly TMP_Text[] taskLabels=new TMP_Text[PreparationRun.ActivityCount];
    private readonly List<Sprite> sprites=new List<Sprite>();
    private readonly string[] names={"ACİL DURUM ÇANTASI","ÇÖK · KAPAN · TUTUN","SELDE GÜVENLİ NOKTA","DEDEKTÖRÜ HAZIRLA","TSUNAMİDEN KAÇIŞ"};
    private readonly Vector2[] locations={new Vector2(.24f,.71f),new Vector2(.41f,.41f),new Vector2(.79f,.49f),new Vector2(.525f,.89f),new Vector2(.84f,.15f)};
    private EarthquakePreparationGame earthquake;
    private FloodPreparationGame flood;
    private SmokeDetectorPreparationGame detector;
    private TsunamiPreparationGame tsunami;
    public TsunamiPreparationGame Tsunami => tsunami;
    private Coroutine activity;
    private bool busy, finishing;
    public int ActiveActivity { get; private set; } = -1;
    public bool IsBusy => busy;
    public IReadOnlyList<Button> Markers => markers;
    public MiniGameWindow BagWindow => bagWindow;
    public EarthquakePreparationGame Earthquake => earthquake;
    public FloodPreparationGame Flood => flood;
    public SmokeDetectorPreparationGame Detector => detector;
    public Button BackButton => back;
    public PreparationBroadcast Broadcast => broadcast;

    private void Awake()
    {
        if(!FindAnyObjectByType<AudioListener>()&&Camera.main)Camera.main.gameObject.AddComponent<AudioListener>();
        activityAudio=gameObject.AddComponent<AudioSource>();activityAudio.playOnAwake=false;activityAudio.spatialBlend=0;activityAudio.volume=.28f;
        string destination=PreparationRun.Destination;
        PreparationRun.Begin(destination);
        Build(); RefreshStatus();
        busy=true;mapGroup.interactable=false;
    }
    private IEnumerator Start()
    {
        mapGroup.interactable=false;
        while(SceneFadeTransition.IsTransitioning)yield return null;
        yield return broadcast.ShowBriefing();
    }
    public void BeginRecording()
    {
        if(!PreparationRun.StartTake())return;
        bagWindow.Close();bagWindow.ResetForNewSession();
        earthquake.Hide();flood.Hide();detector.Hide();tsunami.Hide();modal.gameObject.SetActive(false);
        remainingSeconds=ActivityTimeLimit;timerRunning=true;RefreshTimer();timeoutPanel.SetActive(false);
        house.localScale=Vector3.one;mapGroup.alpha=1;back.interactable=true;
        ActiveActivity=-1;finishing=false;busy=false;mapGroup.interactable=true;
        status.color=White;RefreshStatus();
    }
    private Sprite Load(string name)
    {
        var texture=Resources.Load<Texture2D>("Disaster/"+name);
        if(!texture)throw new System.InvalidOperationException("Hazırlık görseli bulunamadı: "+name);
        var sprite=PreparationUI.Sprite(texture,new Rect(0,0,1,1));sprites.Add(sprite);return sprite;
    }
    private void Build()
    {
        var canvasObject=new GameObject("Afete Hazırlık Arayüzü",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform,false);
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var screen=(RectTransform)canvasObject.transform;
        Panel("Zemin",screen,Vector2.zero,Vector2.one,Navy);
        Label("Başlık",screen,new Vector2(.045f,.90f),new Vector2(.56f,.94f),"AFETE HAZIRLIK",font,38).alignment=TextAlignmentOptions.Left;
        Label("Alt Başlık",screen,new Vector2(.045f,.865f),new Vector2(.58f,.91f),"BAKANLIK KAMU SPOTU · BEŞ GÖREV, TEK ÇEKİM",font,19).alignment=TextAlignmentOptions.Left;
        var rec=Label("Kayıt Göstergesi",screen,new Vector2(.78f,.91f),new Vector2(.96f,.94f),"REC  ●  EĞİTİM KAYDI",font,18);rec.color=Red;
        recordingClock=Label("Çekim Sayacı",screen,new Vector2(.78f,.855f),new Vector2(.97f,.92f),"KAYIT BEKLİYOR",font,24);
        var map=Rect("Ev Haritası",screen,new Vector2(.035f,.11f),new Vector2(.775f,.855f));mapGroup=map.gameObject.AddComponent<CanvasGroup>();
        var viewport=Rect("Harita Görünümü",map,Vector2.zero,Vector2.one);viewport.gameObject.AddComponent<RectMask2D>();
        house=Rect("Tepeden Ev",viewport,Vector2.zero,Vector2.one);
        var fit=house.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1.5f;
        var art=house.gameObject.AddComponent<Image>();art.sprite=Load("HouseMap");art.raycastTarget=false;
        for(int i=0;i<PreparationRun.ActivityCount;i++)
        {
            int id=i;var p=locations[i];
            markers[i]=Button("Hazırlık Noktası "+(i+1),house,p,p,(i+1).ToString(),font);
            markers[i].image.color=Color.white;
            var r=(RectTransform)markers[i].transform;r.sizeDelta=new Vector2(48,48);
            markers[i].onClick.AddListener(()=>OpenActivity(id));
            var outline=markers[i].gameObject.AddComponent<Outline>();outline.effectColor=new Color(.015f,.035f,.05f);outline.effectDistance=new Vector2(3,-3);
        }
        for(int i=0;i<PreparationRun.ActivityCount;i++)
        {
            float y=.75f-i*.105f;
            taskLabels[i]=Label("Görev Durumu "+i,screen,new Vector2(.80f,y),new Vector2(.965f,y+.09f),(i+1)+"  "+names[i]+"\nHAZIRLANACAK",font,20);
            taskLabels[i].alignment=TextAlignmentOptions.Left;
        }
        status=Label("Hazırlık Durumu",screen,new Vector2(.79f,.15f),new Vector2(.97f,.28f),"",font,23);
        Label("Harita İpucu",screen,new Vector2(.04f,.025f),new Vector2(.78f,.09f),"5 ÖNEMLİ AFET HAZIRLIĞI · Beş görevi toplam 45 saniyede tamamla.",font,22);
        var menu=Button("Ana Menü",screen,new Vector2(.81f,.035f),new Vector2(.96f,.09f),"ANA MENÜ",font);
        menu.onClick.AddListener(()=>{if(busy||SceneFadeTransition.IsTransitioning)return;PreparationRun.Reset();ShiftReportFlow.Clear();SceneFadeTransition.LoadScene("Assets/Working Folder/Taha and Yasin/AnaMenu.unity",.3f,.4f);});
        // Quiet recording brackets, outside the game and pointer paths.
        for(int i=0;i<4;i++)
        {
            float x=i%2==0?.015f:.975f,y=i<2?.10f:.84f;
            Panel("Kayıt Çerçevesi",screen,new Vector2(x,y),new Vector2(x+.01f,y+.003f),new Color(.32f,.45f,.48f));
        }
        var shade=Panel("Etkinlik Örtüsü",screen,Vector2.zero,Vector2.one,new Color(.01f,.025f,.04f,.94f));shade.raycastTarget=true;
        modal=shade.gameObject.AddComponent<CanvasGroup>();
        Panel("Etkinlik Çerçevesi",shade.transform,new Vector2(.065f,.075f),new Vector2(.935f,.925f),new Color(.27f,.37f,.4f));
        var card=Panel("Etkinlik Kartı",shade.transform,new Vector2(.068f,.08f),new Vector2(.932f,.92f),Navy).rectTransform;
        activityTitle=Label("Etkinlik Başlığı",card,new Vector2(.04f,.90f),new Vector2(.79f,.99f),"",font,28);activityTitle.alignment=TextAlignmentOptions.Left;
        back=Button("Eve Dön",card,new Vector2(.80f,.91f),new Vector2(.97f,.98f),"EVE DÖN",font);back.onClick.AddListener(ReturnToHouse);
        activityHost=Rect("Etkinlik İçeriği",card,new Vector2(.025f,.10f),new Vector2(.975f,.84f));
        feedbackFlash=Panel("Sonuç Işığı",card,new Vector2(.025f,.10f),new Vector2(.975f,.84f),Color.clear);
        var feedbackBackground=Panel("Geri Bildirim Zemini",card,new Vector2(.04f,.012f),new Vector2(.96f,.095f),new Color(.1f,.2f,.23f,.9f));
        feedbackGroup=feedbackBackground.gameObject.AddComponent<CanvasGroup>();feedbackGroup.alpha=0;
        feedback=Label("Kısa Geri Bildirim",feedbackBackground.transform,new Vector2(.02f,0),new Vector2(.98f,1),"",font,22);
        PreparationUI.Fit((RectTransform)bagWindow.transform,screen);
        bagWindow.transform.SetAsFirstSibling();
        PreparationUI.Fit((RectTransform)bagWindow.BagPanel.transform,activityHost);
        var bagBackground=bagWindow.BagPanel.GetComponent<Image>();
        if(bagBackground)bagBackground.color=PanelColor;
        bagWindow.BagPanel.SetActive(false);
        earthquake=gameObject.AddComponent<EarthquakePreparationGame>();earthquake.Initialize(activityHost,font,OnTrainingMistake);
        flood=gameObject.AddComponent<FloodPreparationGame>();flood.Initialize(activityHost,font,OnTrainingMistake,()=>CompleteActivity(2));
        detector=gameObject.AddComponent<SmokeDetectorPreparationGame>();detector.Initialize(activityHost,font,()=>CompleteActivity(3));
        tsunami=gameObject.AddComponent<TsunamiPreparationGame>();tsunami.Initialize(activityHost,font,()=>{OnTrainingMistake();activityAudio.Stop();if(activityFailure)activityAudio.PlayOneShot(activityFailure);},()=>CompleteActivity(4));
        var shadeTimeout=Panel("Süre Doldu Örtüsü",card,new Vector2(.025f,.10f),new Vector2(.975f,.84f),new Color(.015f,.035f,.055f,.97f));
        shadeTimeout.raycastTarget=true;timeoutPanel=shadeTimeout.gameObject;
        Label("Süre Doldu",shadeTimeout.transform,new Vector2(.15f,.60f),new Vector2(.85f,.76f),"SÜRE DOLDU",font,36).color=Amber;
        Label("Yeniden Dene Açıklaması",shadeTimeout.transform,new Vector2(.12f,.43f),new Vector2(.88f,.59f),"Çekim süresi bitti. Beş hazırlık için toplam 45 saniyen var.\nÇekimi baştan deneyebilir veya menüye dönebilirsin.",font,23);
        retryButton=Button("Görevi Yeniden Dene",shadeTimeout.transform,new Vector2(.12f,.22f),new Vector2(.49f,.36f),"ÇEKİMİ YENİLE",font);
        timeoutReturnButton=Button("Süreden Ana Menü",shadeTimeout.transform,new Vector2(.51f,.22f),new Vector2(.88f,.36f),"ANA MENÜ",font);
        retryButton.onClick.AddListener(RetryActivity);timeoutReturnButton.onClick.AddListener(ReturnAfterTimeout);
        timeoutPanel.SetActive(false);
        modal.gameObject.SetActive(false);
        broadcast=gameObject.AddComponent<PreparationBroadcast>();
        broadcast.Initialize(screen,font,this);
        var track=Panel("Toplam Hazırlık Süresi",screen,new Vector2(.035f,.945f),new Vector2(.965f,.975f),new Color(.14f,.21f,.26f));
        timerStrip=track.gameObject;timerStrip.SetActive(false);
        timerFill=Panel("Kalan Hazırlık Süresi",track.transform,Vector2.zero,Vector2.one,new Color(.28f,.67f,.84f));
        timerLabel=Label("Toplam Süre",track.transform,new Vector2(.02f,-.1f),new Vector2(.98f,1.1f),"TOPLAM 45 SN",font,18);
    }
    public void OpenActivity(int index)
    {
        if(!PreparationRun.IsRecording||index<0||index>=PreparationRun.ActivityCount||busy||ActiveActivity>=0||PreparationRun.IsComplete(index)||SceneFadeTransition.IsTransitioning)return;
        activityAudio.Stop();
        busy=true;ActiveActivity=index;finishing=false;activity=StartCoroutine(EnterActivity(index));
    }
    private IEnumerator EnterActivity(int index)
    {
        mapGroup.interactable=false;feedback.text="";feedbackGroup.alpha=0;activityTitle.text=names[index];
        for(float t=0;t<.3f;t+=Time.unscaledDeltaTime){float p=Mathf.SmoothStep(0,1,t/.3f);house.localScale=Vector3.one*(1+p*.035f);mapGroup.alpha=1-p*.35f;yield return null;}
        modal.gameObject.SetActive(true);modal.alpha=0;modal.interactable=false;
        bagWindow.BagPanel.SetActive(false);earthquake.Hide();flood.Hide();detector.Hide();tsunami.Hide();
        if(index==0){bagWindow.BagGame.ResetForNewSession();bagWindow.OpenBag();}
        else if(index==1)earthquake.Show();else if(index==2)flood.Show();else if(index==3)detector.Show();else tsunami.Show();
        for(float t=0;t<.25f;t+=Time.unscaledDeltaTime){modal.alpha=Mathf.Clamp01(t/.25f);yield return null;}
        modal.alpha=1;modal.interactable=true;busy=false;
        if(index==0)
        {
            while(ActiveActivity==0&&!bagWindow.BagGame.IsResolved)yield return null;
            if(ActiveActivity!=0)yield break;
            if(bagWindow.BagGame.Succeeded)CompleteActivity(0);
            else{OnTrainingMistake();activityAudio.Stop();if(activityFailure)activityAudio.PlayOneShot(activityFailure);ShowFeedback(false,"Haklar bitti. Eşyaları gözden geçirip yeniden deneyebilirsin.");yield return new WaitForSecondsRealtime(1.6f);ReturnToHouse();}
        }
        else if(index==1){yield return earthquake.Play();if(ActiveActivity==1)CompleteActivity(1);}
    }
    public void EvaluateBagAction(bool correct,int points)
    {
        if(ActiveActivity!=0||busy||finishing)return;
        GameAudioFeedback.Feedback(correct);int before=PreparationRun.Health;PreparationRun.AdjustHealth(correct?points:-points);RefreshStatus();
        ShowFeedback(correct,correct?"Uygun eşya yerleştirildi.":"Bu eşya çantaya uygun değil. Yerine geri koyabilirsin.");
    }
    private void OnTrainingMistake()
    {
        if(finishing||TimedOut)return;GameAudioFeedback.Feedback(false);PreparationRun.AdjustHealth(-2);RefreshStatus();
        ShowFeedback(false,ActiveActivity==1?"YANLIŞ TUŞ · TEKRAR DENE":ActiveActivity==4?"DALGA YETİŞTİ · YENİDEN DENE":"GÜVENLİ NOKTA DEĞİL · TEKRAR DENE");
    }
    public void CompleteActivity(int index)
    {
        if(ActiveActivity!=index||busy||finishing||!PreparationRun.Complete(index))return;
        finishing=true;busy=true;modal.interactable=false;RefreshStatus();
        activityAudio.Stop();if(activitySuccess)activityAudio.PlayOneShot(activitySuccess);
        ShowFeedback(true,"KLİP KAYDEDİLDİ · "+PreparationBroadcast.SafetyMessages[index]);
        if(PreparationRun.preparationComplete){timerRunning=false;PreparationRun.FinishTake();}
        StartCoroutine(CompleteAndReturn());
    }
    private IEnumerator CompleteAndReturn(){yield return new WaitForSecondsRealtime(1.6f);yield return LeaveActivity();}
    private void ShowFeedback(bool success,string message)
    {
        feedback.text=message;feedback.color=success?Green:Amber;
        if(feedbackAnimation!=null)StopCoroutine(feedbackAnimation);
        feedbackAnimation=StartCoroutine(AnimateFeedback(success));
    }
    private IEnumerator AnimateFeedback(bool success)
    {
        var panel=feedbackGroup.GetComponent<Image>();
        panel.color=success?new Color(.13f,.28f,.25f,.91f):new Color(.30f,.16f,.13f,.91f);
        feedbackFlash.color=Color.clear;
        for(float elapsed=0;elapsed<.18f;elapsed+=Time.unscaledDeltaTime)
        {
            float progress=Mathf.Clamp01(elapsed/.18f);
            feedbackGroup.alpha=progress;
            feedbackGroup.transform.localScale=Vector3.one*(.97f+.03f*progress);
            var tint=success?Green:Red;tint.a=.09f*Mathf.Sin(progress*Mathf.PI);
            feedbackFlash.color=tint;
            yield return null;
        }
        feedbackGroup.alpha=1;feedbackGroup.transform.localScale=Vector3.one;feedbackFlash.color=Color.clear;
        yield return new WaitForSecondsRealtime(.8f);
        if(success)yield break;
        for(float elapsed=0;elapsed<.2f;elapsed+=Time.unscaledDeltaTime)
        {feedbackGroup.alpha=1-Mathf.Clamp01(elapsed/.2f);yield return null;}
        feedbackGroup.alpha=0;
    }
    public void BagClosed(){if(ActiveActivity==0&&!busy&&!finishing)ReturnToHouse();}
    public void ReturnToHouse()
    {
        if(busy||finishing||ActiveActivity<0)return;
        busy=true;if(activity!=null){StopCoroutine(activity);activity=null;}StartCoroutine(LeaveActivity());
    }
    private IEnumerator LeaveActivity()
    {
        timeoutPanel.SetActive(false);busy=true;modal.interactable=false;
        for(float t=0;t<.3f;t+=Time.unscaledDeltaTime){float p=Mathf.Clamp01(t/.3f);modal.alpha=1-p;mapGroup.alpha=.65f+p*.35f;house.localScale=Vector3.one*(1+(1-p)*.035f);yield return null;}
        ActiveActivity=-1;bagWindow.Close();earthquake.Hide();flood.Hide();detector.Hide();tsunami.Hide();
        modal.gameObject.SetActive(false);house.localScale=Vector3.one;mapGroup.alpha=1;busy=false;finishing=false;mapGroup.interactable=true;
        RefreshStatus();
        if(PreparationRun.preparationComplete)
        {
            busy=true;mapGroup.interactable=false;
            status.text="ÇEKİM TAMAMLANDI";status.color=Green;
            broadcast.ShowReview();
        }
    }
    public void RefreshStatus()
    {
        if(!status)return;
        status.text=$"HAZIRLIK {PreparationRun.CompletedCount} GÖREV TAMAMLANDI\nHALK BİLİNCİ %{PreparationRun.Health}";
        for(int i=0;i<PreparationRun.ActivityCount;i++)
        {
            bool done=PreparationRun.IsComplete(i);markers[i].interactable=!done;
            var colors=markers[i].colors;colors.disabledColor=Green;colors.normalColor=Amber;colors.highlightedColor=White;colors.pressedColor=Green;markers[i].colors=colors;
            taskLabels[i].text=(i+1)+"  "+names[i]+"\n"+(done?"HAZIR":"HAZIRLANACAK");taskLabels[i].color=done?Green:White;
        }
    }
    private void RefreshTimer()
    {
        timerFill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(remainingSeconds/ActivityTimeLimit),1);
        Color tint=remainingSeconds>15?new Color(.28f,.67f,.84f):remainingSeconds>5?Amber:Red;
        if(remainingSeconds>0&&remainingSeconds<=5)tint=Color.Lerp(tint,White,.12f*(.5f+.5f*Mathf.Sin(Time.unscaledTime*5)));
        timerFill.color=tint;timerLabel.text=Mathf.CeilToInt(remainingSeconds)+" SN";
    }
    private void ExpireActivity()
    {
        timerRunning=false;remainingSeconds=0;RefreshTimer();busy=true;finishing=false;
        StopAllCoroutines();activity=null;feedbackAnimation=null;
        PreparationRun.AbortTake();mapGroup.interactable=false;
        bagWindow.Close();bagWindow.BagGame.CancelDrags();earthquake.Hide();flood.Hide();detector.Hide();tsunami.Hide();
        feedbackGroup.alpha=0;feedbackFlash.color=Color.clear;
        activityTitle.text="HAZIRLIK SÜRESİ";modal.gameObject.SetActive(true);modal.alpha=1;modal.interactable=true;
        timeoutPanel.SetActive(true);back.interactable=false;
        activityAudio.Stop();if(activityFailure)activityAudio.PlayOneShot(activityFailure);
    }
    private void RetryActivity()
    {
        if(!TimedOut)return;
        BeginRecording();
    }
    private void ReturnAfterTimeout()
    {
        if(!TimedOut||SceneFadeTransition.IsTransitioning)return;
        PreparationRun.Reset();ShiftReportFlow.Clear();
        SceneFadeTransition.LoadScene("Assets/Working Folder/Taha and Yasin/AnaMenu.unity",.3f,.4f);
    }
    private void Update()
    {
        timerStrip.SetActive(PreparationRun.IsRecording||TimedOut);
        if(timerRunning)
        {
            remainingSeconds=Mathf.Max(0,remainingSeconds-Time.unscaledDeltaTime);RefreshTimer();
            if(remainingSeconds<=0)ExpireActivity();
        }
        if(recordingClock)recordingClock.text=PreparationRun.TakeNumber==0?"KAYIT BEKLİYOR":"ÇEKİM "+PreparationRun.TakeNumber.ToString("00")+"  "+PreparationRun.FormatDuration(PreparationRun.RecordingSeconds);
        for(int i=0;i<PreparationRun.ActivityCount;i++)if(markers[i]&&!PreparationRun.IsComplete(i))markers[i].transform.localScale=Vector3.one*(1+.035f*Mathf.Sin(Time.unscaledTime*3+i));
    }
    private void OnDestroy(){foreach(var sprite in sprites)if(sprite)Destroy(sprite);}
}
