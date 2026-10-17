using UnityEngine;

// Short one-shots use independent channels; speech never stacks multi-second clips.
public sealed class DispatchAudioCues : MonoBehaviour
{
    private OyunAkisiOnur conversation;
    private AudioSource calls,alerts;
    private int lastOpened=-1,lastClosed=-1;
    public DispatchSoundLibrary Library { get; private set; }
    public int OpenCount { get; private set; }
    public int CloseCount { get; private set; }
    public int CurseCount { get; private set; }
    public void Initialize(OyunAkisiOnur flow,DispatchCallFlow phone)
    {
        conversation=flow;Library=Resources.Load<DispatchSoundLibrary>("DispatchSoundLibrary");
        if(!Library){Debug.LogError("DispatchSoundLibrary eksik; GameJam/Operator/Install Curse Audio Assets menüsünü çalıştırın.");return;}
        calls=gameObject.AddComponent<AudioSource>();calls.playOnAwake=false;calls.spatialBlend=0;calls.volume=.32f;
        alerts=gameObject.AddComponent<AudioSource>();alerts.playOnAwake=false;alerts.spatialBlend=0;alerts.volume=.32f;
        phone.ConfigureRing(Library.phoneRing);
        var chat=FindAnyObjectByType<OnurConversationView>();if(chat)chat.ConfigureTypingSound(Library.callerGrain);
        var broadcast=FindAnyObjectByType<PresidentialBroadcastView>();if(broadcast)broadcast.ConfigureTypingSound(Library.presidentGrain);
        var questions=FindAnyObjectByType<QuickQuestionView>();if(questions)questions.ConfigureAudio(alerts,Library.notification,Library.warningTick);
        conversation.ScenarioStarted+=OnStarted;conversation.ScenarioClosing+=OnClosing;
    }
    private void OnStarted(int revision,string id){PlayOpen();}
    private void OnClosing(int revision){PlayClose();}
    public void PlayOpen()
    {
        if(!Library||!calls||lastOpened==conversation.ScenarioRevision)return;
        lastOpened=conversation.ScenarioRevision;calls.Stop();calls.PlayOneShot(Library.phoneOpen);OpenCount++;
    }
    public void PlayClose()
    {
        if(!Library||!calls||lastClosed==conversation.ScenarioRevision)return;
        lastClosed=conversation.ScenarioRevision;calls.Stop();calls.PlayOneShot(Library.phoneClose,.7f);CloseCount++;
    }
    public void PlayCurse(){if(Library&&alerts){alerts.PlayOneShot(Library.curseImpact,.8f);CurseCount++;}}
    private void OnDestroy(){if(conversation){conversation.ScenarioStarted-=OnStarted;conversation.ScenarioClosing-=OnClosing;}}
}
