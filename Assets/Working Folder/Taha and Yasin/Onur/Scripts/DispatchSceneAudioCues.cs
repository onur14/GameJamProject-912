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
    private AudioSource music,presidentVoice,stress,effects;
    private PresidentialBroadcastView presidentView;
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
        music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;music.spatialBlend=0;music.volume=.035f;
        presidentVoice=gameObject.AddComponent<AudioSource>();presidentVoice.playOnAwake=false;presidentVoice.loop=true;presidentVoice.spatialBlend=0;presidentVoice.volume=.08f;
        stress=gameObject.AddComponent<AudioSource>();stress.playOnAwake=false;stress.loop=true;stress.spatialBlend=0;stress.volume=.035f;
        effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.spatialBlend=0;effects.volume=.28f;
    }
    private void OnEnable()
    {
        ended=false;
        if(!presidentView)presidentView=FindAnyObjectByType<PresidentialBroadcastView>();
        phone.FocusCompleted += BeginConversation;phone.Answered += Open;flow.ScenarioStarted += ScenarioStarted;flow.ScenarioClosing += Close;awareness.GameEnded += Failed;
        flow.DecisionResolved+=DecisionResolved;flow.DecisionTimedOut+=DecisionTimedOut;flow.UnitDispatched+=UnitDispatched;
        if(conversationView)conversationView.ChoicesShown+=ChoicesShown;
        if(briefing)briefing.RunCompleted+=Won;
        if(aftermathMusic){music.clip=aftermathMusic;music.Play();}
        presidentVoice.clip=presidentMusic;
    }
    private void OnDisable()
    {
        phone.FocusCompleted -= BeginConversation;phone.Answered -= Open;flow.ScenarioStarted -= ScenarioStarted;flow.ScenarioClosing -= Close;awareness.GameEnded -= Failed;
        flow.DecisionResolved-=DecisionResolved;flow.DecisionTimedOut-=DecisionTimedOut;flow.UnitDispatched-=UnitDispatched;
        if(conversationView)conversationView.ChoicesShown-=ChoicesShown;
        if(briefing)briefing.RunCompleted-=Won;
        ConversationActive=false;music.Stop();presidentVoice.Stop();stress.Stop();effects.Stop();
        if(radio)radio.TelefonKapat();
    }
    private void Update()
    {
        if(ended)return;
        if(!presidentView)presidentView=FindAnyObjectByType<PresidentialBroadcastView>();
        music.volume=Mathf.MoveTowards(music.volume,flow.WaitingForChoice?.02f:.035f,Time.unscaledDeltaTime*.12f);
        bool speaking=presidentMusic && presidentView && presidentView.IsSpeaking;
        if(speaking && !presidentVoice.isPlaying)presidentVoice.Play();
        else if(!speaking && presidentVoice.isPlaying)presidentVoice.Stop();
    }
    private void ChoicesShown(){if(decisionStress){stress.clip=decisionStress;stress.Play();}}
    private void DecisionResolved(bool correct){stress.Stop();GameAudioFeedback.Feedback(correct);}
    private void DecisionTimedOut(){stress.Stop();}
    private void UnitDispatched(DispatchResourceEffect effect)
    {
        var clip=effect.ambulances<0?ambulanceSiren:effect.firefighters<0?fireSiren:effect.rescue<0?policeSiren:null;
        if(clip)effects.PlayOneShot(clip,.45f);
    }
    private void Won(){ConversationActive=false;if(radio&&radio.CizirtiSesi)radio.CizirtiSesi.Stop();ended=true;stress.Stop();music.Stop();presidentVoice.Stop();if(wonSound)effects.PlayOneShot(wonSound,.75f);}
    private void ScenarioStarted(int revision,string id) { if(phone.State==DispatchCallFlow.CallState.Conversation){Open();BeginConversation();} }
    private void BeginConversation() { if(!ended && phone.State==DispatchCallFlow.CallState.Conversation){ConversationActive=true;if(radio)radio.StartConversation();} }
    private void Open()
    {
        if(Time.unscaledTime-lastOpen<.75f)return;
        lastOpen=Time.unscaledTime;if(radio)radio.TelefonAc(false);
    }
    private void Close(int revision) { ConversationActive=false;if(radio)radio.TelefonKapat(); }
    private void Failed() { ConversationActive=false;ended=true;stress.Stop();music.Stop();presidentVoice.Stop();if(lostSound)effects.PlayOneShot(lostSound,.75f);if(radio)radio.TelefonKapat(); }
    public void PlayCurse() { if(notices && blink)notices.PlayOneShot(blink,.8f); }
}
