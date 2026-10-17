using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Two authored curse events and an in-session checkpoint at the beginning of each call.
public sealed class DispatchCurseDirector : MonoBehaviour
{
    [SerializeField] private OyunAkisiOnur flow;
    [SerializeField] private PresidentialBriefingManager briefing;
    [SerializeField] private DispatchResources resources;
    [SerializeField] private CityHealth awareness;
    [SerializeField] private QuickQuestionManager questions;
    [SerializeField] private DispatchCallFlow phone;
    [SerializeField] private Sprite slownessEmblem, fogEmblem;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField, Min(1)] private int slownessAtScenario = 2;
    [SerializeField, Min(1)] private int fogAtScenario = 5;
    private bool slownessApplied, fogApplied, retrying;
    private int slownessUntil = -1, fogUntil = -1;
    private RectTransform slowBadge, fogBadge, banner;
    private TMP_Text slowCount, fogCount, heading, detail;
    private Image flash, emblem;
    private CanvasGroup notice;
    private Coroutine intro;
    private int lastCompleted = -1;
    private Snapshot checkpoint;
    private bool captured;
    private struct Snapshot
    {
        public int ambulances, firefighters, rescue, health, wrong, timeout;
        public int completed, correct, allocations, outcomes, questions;
        public DispatchScenarioData scenario;
    }
    public bool CanRetry => captured;
    public bool IsFogActive => fogApplied && briefing.CompletedScenarios < fogUntil;
    public int SlownessRemaining => Mathf.Max(0, slownessUntil - briefing.CompletedScenarios);
    public int FogRemaining => Mathf.Max(0, fogUntil - briefing.CompletedScenarios);
    public bool SlownessVisible => slowBadge && slowBadge.gameObject.activeSelf;
    public bool FogVisible => fogBadge && fogBadge.gameObject.activeSelf;
    public int AdjustAwarenessGain(int points) => IsFogActive ? Mathf.CeilToInt(points * .5f) : points;

    private void Awake() { Build(); awareness.CurseDirector = this; }
    private void OnEnable()
    {
        flow.ScenarioStarted += Started; briefing.ShiftReset += ResetRun; awareness.GameEnded += HideNotice;
    }
    private void OnDisable()
    {
        flow.ScenarioStarted -= Started; briefing.ShiftReset -= ResetRun; awareness.GameEnded -= HideNotice;
    }
    private void OnDestroy() { if(awareness && awareness.CurseDirector==this) awareness.CurseDirector=null; }
    private void ResetRun()
    {
        slownessApplied=fogApplied=retrying=captured=false;slownessUntil=fogUntil=-1;HideNotice();RefreshBadges();
    }
    private void Started(int revision, string scenario)
    {
        if(retrying){retrying=false;RefreshBadges();return;}
        int number=briefing.CompletedScenarios+1;
        if(number==slownessAtScenario)ApplySlowness();
        if(number==fogAtScenario)ApplyFog();
        CaptureCheckpoint();RefreshBadges();
    }
    public void ApplySlowness()
    {
        if(slownessApplied)return;
        slownessApplied=true;slownessUntil=briefing.CompletedScenarios+2;
        int a=Mathf.Min(3,resources.Ambulances), f=Mathf.Min(3,resources.Firefighters), r=Mathf.Min(3,resources.Police);
        resources.TryApply(-a,-f,-r);
        Announce("YAVAŞLIK CEZASI",$"Ambulans -{a}   /   İtfaiye -{f}   /   Kurtarma -{r}\nHızından bağımsız lanet · 2 vaka",slownessEmblem,new Color(1,.30f,.20f));
        RefreshBadges();
    }
    public void ApplyFog()
    {
        if(fogApplied)return;
        fogApplied=true;fogUntil=briefing.CompletedScenarios+2;
        Announce("BİLGİ SİSİ","Halk bilinci kazanımları %50 azaldı\n2 vaka boyunca",fogEmblem,new Color(.78f,.48f,1));
        RefreshBadges();
    }
    public void CaptureCheckpoint()
    {
        if(awareness.IsGameOver)return;
        checkpoint=new Snapshot {ambulances=resources.Ambulances,firefighters=resources.Firefighters,rescue=resources.Police,
            health=awareness.HealthPercent,wrong=awareness.WrongDecisionCount,timeout=awareness.TimedOutDecisionCount,
            completed=briefing.CompletedScenarios,correct=briefing.CorrectDecisions,allocations=briefing.Decisions.Count,
            outcomes=briefing.ScenarioOutcomes.Count,questions=questions?questions.Results.Count:0,scenario=flow.CurrentScenario};
        captured=true;
    }
    public void RetryCurrentScenario()
    {
        if(!captured)return;
        HideNotice();
        if(questions)questions.RestoreScenarioCheckpoint(checkpoint.questions);
        awareness.RestoreScenarioCheckpoint(checkpoint.health,checkpoint.wrong,checkpoint.timeout);
        resources.TryApply(checkpoint.ambulances-resources.Ambulances,checkpoint.firefighters-resources.Firefighters,checkpoint.rescue-resources.Police);
        retrying=true;
        briefing.RestoreScenarioCheckpoint(checkpoint.completed,checkpoint.correct,checkpoint.allocations,checkpoint.outcomes,checkpoint.scenario);
        phone.SetInteractionLocked(false);RefreshBadges();
    }
    private void LateUpdate()
    {
        if(lastCompleted!=briefing.CompletedScenarios)RefreshBadges();
    }
    private void RefreshBadges()
    {
        lastCompleted=briefing.CompletedScenarios;
        if(slowBadge){slowBadge.gameObject.SetActive(SlownessRemaining>0);slowCount.text=SlownessRemaining.ToString();}
        if(fogBadge){fogBadge.gameObject.SetActive(FogRemaining>0);fogCount.text=FogRemaining.ToString();}
    }
    private RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;
    }
    private TMP_Text Text(string name,Transform parent,Vector2 min,Vector2 max,float size)
    {
        var t=Rect(name,parent,min,max).gameObject.AddComponent<TextMeshProUGUI>();t.font=font?font:TMP_Settings.defaultFontAsset;
        t.fontSize=size;t.fontStyle=FontStyles.Bold;t.color=Color.white;t.raycastTarget=false;t.alignment=TextAlignmentOptions.Center;
        t.enableAutoSizing=true;t.fontSizeMin=12;t.fontSizeMax=size;return t;
    }
    private RectTransform Badge(string name,Transform parent,float x,Sprite sprite,out TMP_Text count)
    {
        var r=Rect(name,parent,new Vector2(x,.005f),new Vector2(x+.065f,.070f));
        var bg=r.gameObject.AddComponent<Image>();bg.color=new Color(.04f,.035f,.065f,.94f);bg.raycastTarget=false;
        var image=Rect("Amblem",r,new Vector2(.02f,.02f),new Vector2(.78f,.98f)).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
        count=Text("Kalan Vaka",r,new Vector2(.73f,.05f),new Vector2(.99f,.5f),18);r.gameObject.SetActive(false);return r;
    }
    private void Build()
    {
        var root=Rect("Lanet Göstergeleri",GetComponentInParent<Canvas>().rootCanvas.transform,Vector2.zero,Vector2.one);
        var c=root.gameObject.AddComponent<Canvas>();c.overrideSorting=true;c.sortingOrder=650;
        slowBadge=Badge("Yavaşlık Cezası",root,.02f,slownessEmblem,out slowCount);
        fogBadge=Badge("Bilgi Sisi",root,.095f,fogEmblem,out fogCount);
        flash=Rect("Lanet Işığı",root,Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();flash.color=Color.clear;flash.raycastTarget=false;
        banner=Rect("Lanet Duyurusu",root,new Vector2(.28f,.52f),new Vector2(.82f,.86f));
        var bg=banner.gameObject.AddComponent<Image>();bg.color=new Color(.035f,.02f,.06f,.97f);bg.raycastTarget=false;
        notice=banner.gameObject.AddComponent<CanvasGroup>();notice.blocksRaycasts=false;notice.interactable=false;
        emblem=Rect("Büyük Amblem",banner,new Vector2(.01f,.10f),new Vector2(.23f,.90f)).gameObject.AddComponent<Image>();emblem.preserveAspect=true;emblem.raycastTarget=false;
        heading=Text("Lanet Adı",banner,new Vector2(.24f,.56f),new Vector2(.98f,.94f),34);
        detail=Text("Lanet Etkisi",banner,new Vector2(.25f,.08f),new Vector2(.97f,.55f),20);
        notice.gameObject.SetActive(false);
    }
    private void Announce(string title,string explanation,Sprite sprite,Color color)
    {
        if(intro!=null)StopCoroutine(intro);
        heading.text="LANET: "+title;heading.color=color;detail.text=explanation;emblem.sprite=sprite;
        notice.gameObject.SetActive(true);intro=StartCoroutine(AnimateNotice(color));
        var sounds=GetComponent<DispatchAudioCues>();if(sounds)sounds.PlayCurse();
    }
    private IEnumerator AnimateNotice(Color color)
    {
        for(float t=0;t<3.5f;t+=Time.unscaledDeltaTime)
        {
            float enter=Mathf.Clamp01(t/.22f);notice.alpha=Mathf.Min(enter,Mathf.Clamp01((3.5f-t)/.4f));
            banner.localScale=Vector3.one*(1+.14f*(1-enter));
            banner.anchoredPosition=Vector2.right*Mathf.Round(Mathf.Sin(t*42)*7*Mathf.Clamp01(1-t/.55f));
            float pulse=t<.6f?Mathf.Max(0,Mathf.Sin(t/.6f*Mathf.PI*4))*.20f:0;
            flash.color=new Color(color.r,color.g,color.b,pulse);yield return null;
        }
        HideNotice();
    }
    private void HideNotice()
    {
        if(intro!=null)StopCoroutine(intro);intro=null;
        if(notice)notice.gameObject.SetActive(false);if(flash)flash.color=Color.clear;
    }
}
