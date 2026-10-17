using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PreparationUI;

public sealed class DispatchCurseController : MonoBehaviour
{
    public enum CurseKind { None, Slowness, InformationFog }
    private OyunAkisiOnur conversation;
    private PresidentialBriefingManager briefing;
    private OnurConversationView chat;
    private DispatchCallFlow phone;
    private QuickQuestionView questions;
    private RectTransform badge,notice,card;
    private Image badgeIcon,largeIcon,flash;
    private TMP_Text badgeText,title,description;
    private CanvasGroup noticeGroup;
    private Sprite slowIcon,fogIcon;
    private Coroutine announcement;
    private bool previousPhonePause,previousChatPause,previousQuestionPause;
    private readonly HashSet<int> triggered=new HashSet<int>();
    private int expiresAfter;
    public CurseKind ActiveCurse { get; private set; }
    public bool IsAnnouncing => announcement!=null;
    public int RemainingScenarios => ActiveCurse==CurseKind.None?0:Mathf.Max(0,expiresAfter-briefing.CompletedScenarios);
    public float QuestionTimeMultiplier => 1;
    public bool PresidentPenaltyPending { get; private set; }
    public void ConsumePresidentPenalty() { PresidentPenaltyPending=false; }
    public RectTransform Badge => badge;

    public void Initialize(OyunAkisiOnur flow,PresidentialBriefingManager manager,OnurConversationView view,DispatchCallFlow telephone,TMP_FontAsset font)
    {
        conversation=flow;briefing=manager;chat=view?view:FindAnyObjectByType<OnurConversationView>();phone=telephone;questions=FindAnyObjectByType<QuickQuestionView>();
        var root=Rect("Ceza Etkileri",GetComponentInParent<Canvas>().rootCanvas.transform,Vector2.zero,Vector2.one);
        var canvas=root.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=850;root.gameObject.AddComponent<GraphicRaycaster>();
        var sheet=Resources.Load<Texture2D>("Disaster/CurseEmblems");
        if(sheet){slowIcon=PreparationUI.Sprite(sheet,new Rect(0,0,.5f,1));fogIcon=PreparationUI.Sprite(sheet,new Rect(.5f,0,.5f,1));}
        badge=Panel("Aktif Ceza Amblemi",root,new Vector2(.017f,.012f),new Vector2(.37f,.135f),new Color(.12f,.055f,.065f,.98f)).rectTransform;
        badgeIcon=Panel("Ceza Amblemi",badge,new Vector2(.01f,.05f),new Vector2(.25f,.95f),Color.white);badgeIcon.preserveAspect=true;
        badgeText=Label("Ceza Kalan Senaryo",badge,new Vector2(.27f,.04f),new Vector2(.98f,.96f),"",font,19);badgeText.alignment=TextAlignmentOptions.Left;badgeText.color=Amber;
        notice=Panel("Ceza Geliş Örtüsü",root,Vector2.zero,Vector2.one,new Color(.05f,.005f,.015f,.65f)).rectTransform;notice.GetComponent<Image>().raycastTarget=true;
        noticeGroup=notice.gameObject.AddComponent<CanvasGroup>();
        flash=Panel("Ceza Kenar Parlaması",notice,Vector2.zero,Vector2.one,new Color(.7f,.14f,.07f,.08f));
        for(int i=0;i<2;i++)Panel("Ceza Alarm Şeridi",notice,new Vector2(0,i==0?.02f:.965f),new Vector2(1,i==0?.035f:.98f),Red);
        card=Panel("Ceza Uyarı Kartı",notice,new Vector2(.18f,.30f),new Vector2(.82f,.73f),new Color(.07f,.035f,.055f)).rectTransform;
        Label("Ceza Başlığı",card,new Vector2(.06f,.76f),new Vector2(.94f,.98f),"CEZA DEVREDE!",font,40).color=Red;
        largeIcon=Panel("Büyük Ceza Amblemi",card,new Vector2(.035f,.12f),new Vector2(.255f,.75f),Color.white);largeIcon.preserveAspect=true;
        title=Label("Ceza Adı",card,new Vector2(.27f,.48f),new Vector2(.97f,.75f),"",font,32);title.color=Amber;
        description=Label("Ceza Açıklaması",card,new Vector2(.27f,.08f),new Vector2(.97f,.48f),"",font,23);
        badge.gameObject.SetActive(false);notice.gameObject.SetActive(false);
        conversation.ScenarioStarted+=OnScenarioStarted;briefing.ShiftReset+=ResetCurses;
    }
    private void OnScenarioStarted(int revision,string id)
    {
        OnScenarioCompleted(briefing.CompletedScenarios);
        int number=briefing.CompletedScenarios+1;
        var health=FindAnyObjectByType<CityHealth>();
        var quick=FindAnyObjectByType<QuickQuestionManager>();
        int wrong=(health?health.WrongDecisionCount:0)+(quick?quick.IncorrectAnswerCount:0);
        if((number==2||number==5&&wrong>=2)&&triggered.Add(number))
        {
            ActiveCurse=number==2?CurseKind.Slowness:CurseKind.InformationFog;if(number==5)PresidentPenaltyPending=true;expiresAfter=briefing.CompletedScenarios+2;
            if(ActiveCurse==CurseKind.Slowness)
            {
                var stock=briefing.Resources;
                stock.TryApply(-Mathf.Min(2,stock.Ambulances),-Mathf.Min(2,stock.Firefighters),-Mathf.Min(2,stock.RescueTeams));
            }
            RefreshBadge();announcement=StartCoroutine(Announce());
        }
    }
    public void OnScenarioCompleted(int count)
    {
        if(ActiveCurse!=CurseKind.None&&count>=expiresAfter)ActiveCurse=CurseKind.None;
        RefreshBadge();
    }
    private void RefreshBadge()
    {
        if(!badge)return;bool active=ActiveCurse!=CurseKind.None;badge.gameObject.SetActive(active);
        if(!active)return;badgeIcon.sprite=ActiveCurse==CurseKind.Slowness?slowIcon:fogIcon;
        badgeText.text=(ActiveCurse==CurseKind.Slowness?"YAVAŞLIK CEZASI":"BİLGİ SİSİ")+"  ·  "+RemainingScenarios+" SENARYO";
    }
    private IEnumerator Announce()
    {
        previousPhonePause=phone.IsPaused;previousChatPause=chat&&chat.IsPaused;previousQuestionPause=questions&&questions.IsPaused;
        phone.SetPaused(true);if(chat)chat.SetPaused(true);if(questions)FindAnyObjectByType<QuickQuestionManager>()?.SetPaused(true);
        notice.gameObject.SetActive(true);largeIcon.sprite=badgeIcon.sprite;
        title.text=ActiveCurse==CurseKind.Slowness?"YAVAŞLIK CEZASI":"BİLGİ SİSİ";
        description.text=ActiveCurse==CurseKind.Slowness?"Her ekipten −2 kaynak.\nAmblem iki senaryo sonra kaybolur.":"Başkanın kaynak bonusları %50 azalır.\nKaynaklarını dikkatli yönet.\nEtki iki senaryo sürer.";
        var sound=FindAnyObjectByType<DispatchAudioCues>();if(sound)sound.PlayCurse();
        for(float t=0;t<2.35f;t+=Time.unscaledDeltaTime)
        {
            float enter=Mathf.Clamp01(t/.18f),exit=Mathf.Clamp01((2.35f-t)/.3f);noticeGroup.alpha=Mathf.Min(enter,exit);
            float impact=Mathf.Clamp01(1-t/.7f);card.localScale=Vector3.one*(1+.11f*impact);
            card.anchoredPosition=Vector2.right*Mathf.Round(Mathf.Sin(t*48)*14*impact);
            flash.color=new Color(.7f,.14f,.07f,.07f+.08f*impact);yield return null;
        }
        FinishAnnouncement();announcement=null;
    }
    private void FinishAnnouncement()
    {
        if(notice)notice.gameObject.SetActive(false);if(card){card.localScale=Vector3.one;card.anchoredPosition=Vector2.zero;}
        if(phone)phone.SetPaused(previousPhonePause);if(chat)chat.SetPaused(previousChatPause);if(questions)FindAnyObjectByType<QuickQuestionManager>()?.SetPaused(previousQuestionPause);
    }
    public void CancelAnnouncement()
    {
        if(announcement==null)return;StopCoroutine(announcement);announcement=null;FinishAnnouncement();
    }
    private void ResetCurses(){CancelAnnouncement();triggered.Clear();PresidentPenaltyPending=false;ActiveCurse=CurseKind.None;RefreshBadge();}
    private void OnDestroy()
    {
        if(conversation)conversation.ScenarioStarted-=OnScenarioStarted;if(briefing)briefing.ShiftReset-=ResetCurses;
        if(slowIcon)Destroy(slowIcon);if(fogIcon)Destroy(fogIcon);
    }
}
