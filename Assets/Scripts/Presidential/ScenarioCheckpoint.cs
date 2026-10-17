using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PreparationUI;

// A failed attempt rolls back only the current call, never the completed shift.
public sealed class ScenarioCheckpoint : MonoBehaviour
{
    private CityHealth health;
    private OyunAkisiOnur conversation;
    private PresidentialBriefingManager briefing;
    private DispatchCallFlow phone;
    private OnurConversationView chat;
    private DispatchCurseController curses;
    private QuickQuestionManager questions;
    private CanvasGroup overlay;
    private TMP_Text message;
    private int ambulance,fire,rescue,awareness,wrong,timedOut,questionCount,scenarioIndex;
    public bool IsRecovering { get; private set; }
    public int RecoveryCount { get; private set; }
    public int SnapshotAwareness => awareness;
    public Vector3Int SnapshotResources => new Vector3Int(ambulance,fire,rescue);

    public void Initialize(CityHealth status,OyunAkisiOnur flow,PresidentialBriefingManager manager,DispatchCallFlow telephone,OnurConversationView view,DispatchCurseController curseSystem,TMP_FontAsset font)
    {
        health=status;conversation=flow;briefing=manager;phone=telephone;chat=view?view:FindAnyObjectByType<OnurConversationView>();curses=curseSystem;questions=FindAnyObjectByType<QuickQuestionManager>();
        var root=Panel("Senaryo Başlangıcına Dönüş",GetComponentInParent<Canvas>().rootCanvas.transform,Vector2.zero,Vector2.one,new Color(.025f,.035f,.05f,.98f));root.raycastTarget=true;
        var canvas=root.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=1500;root.gameObject.AddComponent<GraphicRaycaster>();overlay=root.gameObject.AddComponent<CanvasGroup>();
        Label("Bilinç Sıfırlandı",root.transform,new Vector2(.15f,.57f),new Vector2(.85f,.75f),"HALK BİLİNCİ TÜKENDİ",font,43).color=Amber;
        message=Label("Senaryo Tekrarı Açıklaması",root.transform,new Vector2(.15f,.25f),new Vector2(.85f,.56f),"",font,27);
        root.gameObject.SetActive(false);Capture(0,"");conversation.ScenarioStarted+=Capture;
    }
    private void Capture(int revision,string id)
    {
        if(IsRecovering||briefing.Phase!=PresidentialBriefingManager.ShiftPhase.Scenario)return;
        var stocks=briefing.Resources;ambulance=stocks.Ambulances;fire=stocks.Firefighters;rescue=stocks.RescueTeams;
        awareness=Mathf.Max(1,health.HealthPercent);wrong=health.WrongDecisionCount;timedOut=health.TimedOutDecisionCount;
        questionCount=questions?questions.Results.Count:0;scenarioIndex=briefing.CompletedScenarios;
    }
    public bool Restart()
    {
        if(IsRecovering||briefing.Phase!=PresidentialBriefingManager.ShiftPhase.Scenario)return false;
        IsRecovering=true;curses.CancelAnnouncement();StartCoroutine(Recover());return true;
    }
    private IEnumerator Recover()
    {
        overlay.gameObject.SetActive(true);overlay.alpha=0;
        message.text=$"{scenarioIndex+1}. SENARYONUN BAŞINDAN DEVAM\n\nEkipler ve halk bilinci, bu senaryonun\nbaşlangıç kaydına geri alınıyor.\nÖnceki senaryolardaki ilerlemen korunuyor.";
        if(chat)chat.SetPaused(true);
        for(float t=0;t<1.6f;t+=Time.unscaledDeltaTime){overlay.alpha=Mathf.Clamp01(t/.2f);yield return null;}
        if(questions)questions.RestoreScenarioResults(questionCount);
        briefing.Resources.RestoreSnapshot(ambulance,fire,rescue);
        health.RestoreScenarioHealth(awareness,wrong,timedOut);
        if(chat)chat.SetPaused(false);phone.SetPaused(false);
        bool restarted=briefing.RetryCurrentScenario();
        RecoveryCount++;IsRecovering=false;overlay.gameObject.SetActive(false);
        if(!restarted)Debug.LogError("Senaryo başlangıç kaydı geri yüklenemedi.");
    }
    private void OnDestroy(){if(conversation)conversation.ScenarioStarted-=Capture;}
}
