using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class OperatorCurseChecks
{
    const string Queue="Library/OperatorCurse",Logs="Logs/OperatorCurse";
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> errors=new List<string>();
    static double nextPoll;
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Flags).GetValue(target);
    static OperatorCurseChecks()
    {
        Directory.CreateDirectory(Queue);EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=s=>{
            if(!SessionState.GetBool("OperatorCurseChecks",false))return;
            if(s==PlayModeStateChange.EnteredPlayMode)Run();
            if(s==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool("OperatorCurseChecks",false);
                EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("OperatorCursePreviousStart",""));
                File.WriteAllText(Queue+"/status.txt",SessionState.GetString("OperatorCurseResult","No result"));
            }
        };
    }
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<nextPoll||EditorApplication.isCompiling||EditorApplication.isUpdating)return;nextPoll=EditorApplication.timeSinceStartup+.3;
        string path=Queue+"/request.txt";if(!File.Exists(path))return;string command=File.ReadAllText(path).Trim();File.Delete(path);
        try
        {
            if(command=="install"){OperatorCurseInstaller.Install();File.WriteAllText(Queue+"/status.txt","INSTALLED");}
            else if(command=="test")Start();
            else if(command=="status")File.WriteAllText(Queue+"/status.txt",SceneManager.GetActiveScene().path+" play="+EditorApplication.isPlaying);
            else throw new Exception("Unknown operator request");
        }
        catch(Exception e){File.WriteAllText(Queue+"/status.txt",e.ToString());}
    }
    [MenuItem("GameJam/Operator/Test Curses Checkpoint Audio")]
    public static void Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Play Mode already active");
        SessionState.SetString("OperatorCursePreviousStart",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));SessionState.SetBool("OperatorCurseChecks",true);
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(PreparationRun.OperatorScene);
        File.WriteAllText(Queue+"/status.txt","RUNNING");EditorApplication.EnterPlaymode();
    }
    static void Check(bool ok,string description)
    {if(!ok)throw new Exception(description);File.AppendAllText(Logs+"/checks.txt","PASS: "+description+"\n");}
    static async Task Wait(Func<bool> predicate,int seconds=30)
    {var until=DateTime.UtcNow.AddSeconds(seconds);while(!predicate()){if(DateTime.UtcNow>until)throw new TimeoutException("Operator state");await Task.Delay(30);}}
    static async Task Capture(string name)
    {
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Repaint();
        await Task.Delay(130);ScreenCapture.CaptureScreenshot(Logs+"/"+name+".png");await Task.Delay(220);
    }
    static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    static async void Run()
    {
        Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/checks.txt","");errors.Clear();Application.logMessageReceived+=Log;Application.runInBackground=true;
        try
        {
            await Wait(()=>Find<ScenarioCheckpoint>()&&Find<DispatchAudioCues>());
            var health=Find<CityHealth>();var checkpoint=Find<ScenarioCheckpoint>();var curses=Find<DispatchCurseController>();var audio=Find<DispatchAudioCues>();
            var phone=Find<DispatchCallFlow>();var briefing=Find<PresidentialBriefingManager>();var resources=Find<DispatchResources>();var conversation=Find<OyunAkisiOnur>();var chat=Find<OnurConversationView>();
            var questions=Find<QuickQuestionManager>();var qview=Find<QuickQuestionView>();var broadcast=Find<PresidentialBroadcastView>();broadcast.charactersPerSecond=0;
            Check(audio.Library&&audio.Library.phoneRing&&audio.Library.phoneOpen&&audio.Library.phoneClose,"Existing telephone files are wired through the sound library");
            Check(audio.Library.callerGrain.length<.08f&&audio.Library.presidentGrain.length<.08f,"Long speech files are reduced to non-overlapping typing grains");
            Check(audio.Library.notification.length<.2f&&audio.Library.warningTick.length<.1f,"Notification and warning effects are short");
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>().Count(s=>s.isActiveAndEnabled)==1,"One active EventSystem");
            Check(health.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("HALK BİLİNCİ")),"HUD uses Halk Bilinci name");
            await Wait(()=>phone.State==DispatchCallFlow.CallState.IncomingCall);Check(Field<AudioSource>(phone,"ringAudioSource").isPlaying,"Telephone file plays while incoming call rings");
            phone.AnswerPhone();await Wait(()=>phone.State==DispatchCallFlow.CallState.Conversation);
            Check(!Field<AudioSource>(phone,"ringAudioSource").isPlaying&&audio.OpenCount==1,"Answer stops ringing and plays opening cue exactly once");
            bool recovered=false,expiredSlow=false,fogSeen=false,fogExpired=false;var answered=new HashSet<int>();
            var scenarios=Field<DispatchScenarioData[]>(briefing,"scenarioOrder");int last=-1;bool keyDown=false;var deadline=DateTime.UtcNow.AddMinutes(7);
            Vector3Int beforeStock=new Vector3Int(resources.Ambulances,resources.Firefighters,resources.RescueTeams),lastDelta=Vector3Int.zero;
            resources.Changed+=()=>{var now=new Vector3Int(resources.Ambulances,resources.Firefighters,resources.RescueTeams);lastDelta=now-beforeStock;beforeStock=now;};
            questions.QuestionStarted+=q=>{
                if(briefing.CompletedScenarios>=4&&briefing.CompletedScenarios<=5)
                {
                    var original=new[]{questions.GetQuestionForScenario(briefing.CompletedScenarios),questions.GetQuestionForScenario(briefing.CompletedScenarios,true)}.FirstOrDefault(c=>c.id==q.id);
                    if(original!=null){Check(Mathf.Abs(q.timerDuration-original.timerDuration*.75f)<.001f,"Bilgi Sisi reduces cloned question duration by 25 percent");fogSeen=true;}
                }
            };
            while(SceneManager.GetActiveScene().path==PreparationRun.OperatorScene)
            {
                if(DateTime.UtcNow>deadline)throw new TimeoutException("Eight operator scenarios");
                if(briefing.CompletedScenarios!=last){last=briefing.CompletedScenarios;File.AppendAllText(Logs+"/checks.txt","PROGRESS: "+last+"/8\n");}
                if(briefing.CompletedScenarios==1&&!recovered)
                {
                    recovered=true;Check(curses.ActiveCurse==DispatchCurseController.CurseKind.Slowness&&curses.RemainingScenarios==2,"Slowness arrives independently of player response speed");
                    Check(lastDelta==new Vector3Int(-3,-3,-3)&&resources.Ambulances>=0&&resources.Firefighters>=0&&resources.RescueTeams>=0,"Curse immediately removes exactly three ambulances, fire and rescue teams");
                    Check(curses.Badge.gameObject.activeInHierarchy,"Curse emblem appears in lower-left HUD");await Capture("01-slowness-impact");
                    await Wait(()=>!curses.IsAnnouncing);await Capture("02-slowness-badge");
                    int revision=conversation.ScenarioRevision,results=questions.Results.Count,awareness=checkpoint.SnapshotAwareness;
                    var stock=checkpoint.SnapshotResources;int priorWrong=health.WrongDecisionCount;
                    var fixture=questions.GetQuestionForScenario(1).Clone();fixture.id="AUDIO-CHECKPOINT-FIXTURE";fixture.isDisinformation=false;fixture.timerDuration=1;
                    int warnings=qview.WarningSoundCount;questions.TriggerQuestion(fixture);await Wait(()=>qview.Ready);
                    Check(qview.ArrivalSoundCount>0,"Arrival blink plays when the question becomes actionable");
                    await Wait(()=>qview.WarningSoundCount>warnings,4);Check(qview.WarningSoundCount==warnings+1,"Critical timer plays one warning tick without spamming");
                    if(resources.Ambulances>0)resources.TrySpend(DispatchResources.ResourceType.Ambulances);
                    health.ApplyDamage(1000);Check(checkpoint.IsRecovering,"Zero awareness opens current-scenario recovery instead of full-run failure");await Capture("03-awareness-checkpoint");
                    await Wait(()=>!checkpoint.IsRecovering&&conversation.ScenarioRevision>revision);
                    Check(briefing.CompletedScenarios==1&&SceneManager.GetActiveScene().path==PreparationRun.OperatorScene,"Retry stays in the same scene and second scenario");
                    Check(health.HealthPercent==awareness&&health.WrongDecisionCount==priorWrong,"Retry restores exact scenario-start awareness and counters");
                    Check(new Vector3Int(resources.Ambulances,resources.Firefighters,resources.RescueTeams)==stock,"Retry restores exact scenario-start resource stocks without a second curse debit");
                    Check(questions.Results.Count==results,"Failed attempt question results are rolled back while prior results survive");
                    Check(curses.RemainingScenarios==2,"Failed attempt does not consume curse duration");await Capture("04-scenario-restored");
                }
                if(briefing.CompletedScenarios==3&&!expiredSlow)
                {expiredSlow=true;Check(curses.ActiveCurse==DispatchCurseController.CurseKind.None&&!curses.Badge.gameObject.activeInHierarchy,"Slowness emblem disappears after exactly two completed scenarios");}
                if(briefing.CompletedScenarios==4&&curses.IsAnnouncing)await Capture("05-information-fog");
                if(briefing.CompletedScenarios==6&&!fogExpired)
                {fogExpired=true;Check(curses.QuestionTimeMultiplier==1&&!curses.Badge.gameObject.activeInHierarchy,"Information Fog and its badge expire after two scenarios");}
                if(health.IsGameOver&&!checkpoint.IsRecovering)throw new Exception("Unexpected full-run loss");
                chat.RevealCurrentLine();
                if(qview.Ready&&!qview.IsPaused)
                {
                    int choice=questions.CurrentQuestion.isDisinformation?questions.CurrentQuestion.correctChoiceIndex:1;
                    var button=choice==0?qview.ButtonA:qview.ButtonB;if(button.interactable)button.onClick.Invoke();
                }
                if(conversation.WaitingForChoice&&!chat.IsPaused&&!answered.Contains(conversation.ScenarioRevision))
                {
                    var scenario=scenarios[Mathf.Min(briefing.CompletedScenarios,7)];var b=scenario.correctChoiceIsB?chat.ChoiceBButton:chat.ChoiceAButton;
                    if(b.interactable){answered.Add(conversation.ScenarioRevision);b.onClick.Invoke();}
                    else if(chat.AdvanceButton.gameObject.activeInHierarchy){answered.Add(conversation.ScenarioRevision);chat.AdvanceButton.onClick.Invoke();}
                }
                if(briefing.Phase==PresidentialBriefingManager.ShiftPhase.Allocation)
                {
                    var offers=Field<Button[]>(broadcast,"offers");int index=offers[0].interactable?0:1;
                    if(offers[0].interactable&&offers[1].interactable)
                    {
                        var a=Field<ResourceOffer>(briefing,"offerA");var b=Field<ResourceOffer>(briefing,"offerB");
                        int ra=Mathf.Min(resources.Ambulances+a.ambulances,Mathf.Min(resources.Firefighters+a.firefighters,resources.RescueTeams+a.thirdTeam));
                        int rb=Mathf.Min(resources.Ambulances+b.ambulances,Mathf.Min(resources.Firefighters+b.firefighters,resources.RescueTeams+b.thirdTeam));if(rb>ra)index=1;
                    }
                    if(offers[index].interactable)offers[index].onClick.Invoke();
                }
                else if(briefing.Phase==PresidentialBriefingManager.ShiftPhase.Dialogue||briefing.Phase==PresidentialBriefingManager.ShiftPhase.Confirmation)
                {var b=Field<Button>(broadcast,"advance");if(b.gameObject.activeInHierarchy&&b.interactable)b.onClick.Invoke();}
                var connection=Field<GameObject>(chat,"connectionOverlay");
                if(connection&&connection.activeSelf&&Keyboard.current!=null){keyDown=!keyDown;InputSystem.QueueStateEvent(Keyboard.current,keyDown?new KeyboardState(Key.Space):new KeyboardState());}
                await Task.Delay(100);
            }
            await Wait(()=>Find<ShiftReportView>()&&Find<ShiftReportView>().IsVisible&&!SceneFadeTransition.IsTransitioning);
            Check(recovered&&expiredSlow&&fogSeen&&fogExpired,"Checkpoint and both two-scenario curse lifecycles were exercised");
            var report=Find<ShiftReportView>();Check(report.Data.Completed==8&&report.Data.Answered+report.Data.Missed==16,"Full eight-scenario report retains exactly sixteen questions after rollback");
            await Capture("06-shift-report");Check(errors.Count==0,"No new Play Mode errors: "+string.Join("; ",errors));
            File.AppendAllText(Logs+"/checks.txt","COMPLETE\n");SessionState.SetString("OperatorCurseResult","PASS — Logs/OperatorCurse/checks.txt");
        }
        catch(Exception e){File.AppendAllText(Logs+"/checks.txt","FAIL: "+e+"\n");SessionState.SetString("OperatorCurseResult",e.ToString());}
        finally{if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());Application.logMessageReceived-=Log;EditorApplication.ExitPlaymode();}
    }
}
