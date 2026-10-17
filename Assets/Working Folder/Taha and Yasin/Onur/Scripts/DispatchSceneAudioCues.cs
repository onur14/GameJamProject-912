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
    private float lastOpen = -10;
    private void OnEnable()
    {
        phone.Answered += Open;flow.ScenarioStarted += ScenarioStarted;flow.ScenarioClosing += Close;awareness.GameEnded += Failed;
    }
    private void OnDisable()
    {
        phone.Answered -= Open;flow.ScenarioStarted -= ScenarioStarted;flow.ScenarioClosing -= Close;awareness.GameEnded -= Failed;
        if(radio)radio.TelefonKapat();
    }
    private void ScenarioStarted(int revision,string id) { if(phone.State==DispatchCallFlow.CallState.Conversation)Open(); }
    private void Open()
    {
        if(Time.unscaledTime-lastOpen<.75f)return;
        lastOpen=Time.unscaledTime;if(radio)radio.TelefonAc();
    }
    private void Close(int revision) { if(radio)radio.TelefonKapat(); }
    private void Failed() { if(radio)radio.TelefonKapat(); }
    public void PlayCurse() { if(notices && blink)notices.PlayOneShot(blink,.8f); }
}
