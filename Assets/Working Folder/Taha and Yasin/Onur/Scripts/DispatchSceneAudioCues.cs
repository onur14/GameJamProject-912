using UnityEngine;

// Reuses the authored phone sources; no additional looping notification layer.
public sealed class DispatchSceneAudioCues : MonoBehaviour
{
    [SerializeField] private DispatchCallFlow phone;
    [SerializeField] private OyunAkisiOnur flow;
    [SerializeField] private CityHealth awareness;
    [SerializeField] private Cizirti radio;
    [SerializeField] private AudioSource notices;
    [SerializeField] private AudioClip blink;
    [SerializeField] private AudioClip aftermathMusic;
    [SerializeField] private AudioClip phoneConversation;
    [SerializeField] private AudioClip decisionStress;
    [SerializeField] private AudioClip choiceClick;
    [SerializeField] private AudioClip lostSound;
    [SerializeField] private AudioClip wonSound;
    [SerializeField] private AudioClip ambulanceSiren;
    [SerializeField] private AudioClip fireSiren;
    [SerializeField] private AudioClip policeSiren;
    [SerializeField] private AudioClip presidentMusic;
    [SerializeField] private OnurConversationView conversationView;
    [SerializeField] private PresidentialBriefingManager briefing;
    private AudioSource music,stress,effects;
    private bool ended;
    public bool ConversationActive { get; private set; }
    private float lastOpen = -10;
    private void Awake()
    {
        if(radio && radio.CizirtiSesi)
        {
            radio.CizirtiSesi.Stop();radio.CizirtiSesi.clip=phoneConversation;
            radio.CizirtiSesi.loop=true;radio.CizirtiSesi.volume=.09f;radio.CizirtiSesi.playOnAwake=false;
        }
        music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;music.spatialBlend=0;music.volume=.07f;
        stress=gameObject.AddComponent<AudioSource>();stress.playOnAwake=false;stress.loop=true;stress.spatialBlend=0;stress.volume=.055f;
        effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.spatialBlend=0;effects.volume=.28f;
    }
    private void OnEnable()
    {
        ended=false;
        phone.FocusCompleted += BeginConversation;phone.Answered += Open;flow.ScenarioStarted += ScenarioStarted;flow.ScenarioClosing += Close;awareness.GameEnded += Failed;
        flow.DecisionResolved+=DecisionResolved;flow.DecisionTimedOut+=DecisionTimedOut;flow.UnitDispatched+=UnitDispatched;
        if(conversationView)conversationView.ChoicesShown+=ChoicesShown;
        if(briefing)briefing.RunCompleted+=Won;
        if(aftermathMusic){music.clip=aftermathMusic;music.Play();}
    }
    private void OnDisable()
    {
        phone.FocusCompleted -= BeginConversation;phone.Answered -= Open;flow.ScenarioStarted -= ScenarioStarted;flow.ScenarioClosing -= Close;awareness.GameEnded -= Failed;
        flow.DecisionResolved-=DecisionResolved;flow.DecisionTimedOut-=DecisionTimedOut;flow.UnitDispatched-=UnitDispatched;
        if(conversationView)conversationView.ChoicesShown-=ChoicesShown;
        if(briefing)briefing.RunCompleted-=Won;
        ConversationActive=false;music.Stop();stress.Stop();effects.Stop();
        if(radio)radio.TelefonKapat();
    }
    private void Update()
    {
        if(ended || !briefing || !presidentMusic || !aftermathMusic || !music)return;
        bool presidential=briefing.Phase==PresidentialBriefingManager.ShiftPhase.Dialogue || briefing.Phase==PresidentialBriefingManager.ShiftPhase.Allocation || briefing.Phase==PresidentialBriefingManager.ShiftPhase.Confirmation || briefing.Phase==PresidentialBriefingManager.ShiftPhase.Final;
        var next=presidential?presidentMusic:aftermathMusic;
        if(music.clip==next)return;
        music.clip=next;music.Play();
    }
    private void ChoicesShown(){if(decisionStress){stress.clip=decisionStress;stress.Play();}}
    private void DecisionResolved(bool correct){stress.Stop();GameAudioFeedback.Feedback(correct);}
    private void DecisionTimedOut(){stress.Stop();}
    private void UnitDispatched(DispatchResourceEffect effect)
    {
        var clip=effect.ambulances<0?ambulanceSiren:effect.firefighters<0?fireSiren:effect.rescue<0?policeSiren:null;
        if(clip)effects.PlayOneShot(clip,.45f);
    }
    private void Won(){ConversationActive=false;if(radio&&radio.CizirtiSesi)radio.CizirtiSesi.Stop();ended=true;stress.Stop();music.Stop();if(wonSound)effects.PlayOneShot(wonSound,.75f);}
    private void ScenarioStarted(int revision,string id) { if(phone.State==DispatchCallFlow.CallState.Conversation){Open();BeginConversation();} }
    private void BeginConversation() { if(!ended && phone.State==DispatchCallFlow.CallState.Conversation){ConversationActive=true;if(radio)radio.StartConversation();} }
    private void Open()
    {
        if(Time.unscaledTime-lastOpen<.75f)return;
        lastOpen=Time.unscaledTime;if(radio)radio.TelefonAc(false);
    }
    private void Close(int revision) { ConversationActive=false;if(radio)radio.TelefonKapat(); }
    private void Failed() { ConversationActive=false;ended=true;stress.Stop();music.Stop();if(lostSound)effects.PlayOneShot(lostSound,.75f);if(radio)radio.TelefonKapat(); }
    public void PlayCurse() { if(notices && blink)notices.PlayOneShot(blink,.8f); }
}
