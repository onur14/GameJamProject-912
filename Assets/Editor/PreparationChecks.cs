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

// Explicit opt-in checks run through Library/Preparation/request.txt.
// No fixture values or Play Mode scene objects are saved to project assets.
[InitializeOnLoad]
public static class PreparationChecks
{
    const string Queue="Library/Preparation",Logs="Logs/Preparation";
    const string Menu="Assets/Working Folder/Taha and Yasin/AnaMenu.unity";
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> errors=new List<string>();
    static double nextPoll;
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Flags).GetValue(target);
    static PreparationChecks()
    {
        Directory.CreateDirectory(Queue);EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=s=>{
            if(!SessionState.GetBool("PreparationChecks",false))return;
            if(s==PlayModeStateChange.EnteredPlayMode)Run();
            if(s==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool("PreparationChecks",false);
                EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("PreparationPreviousStart",""));
                File.WriteAllText(Queue+"/status.txt",SessionState.GetString("PreparationResult","No result"));
            }
        };
    }
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<nextPoll)return;nextPoll=EditorApplication.timeSinceStartup+.3;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        string path=Queue+"/request.txt";if(!File.Exists(path))return;
        string command=File.ReadAllText(path).Trim();File.Delete(path);
        try
        {
            if(command=="install"){PreparationInstaller.Install();File.WriteAllText(Queue+"/status.txt","INSTALLED");}
            else if(command=="test")Start();
            else if(command=="test-visual")StartVisual();
            else if(command=="status")File.WriteAllText(Queue+"/status.txt",SceneManager.GetActiveScene().path+" play="+EditorApplication.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty);
            else throw new Exception("Unknown preparation request");
        }
        catch(Exception e){File.WriteAllText(Queue+"/status.txt",e.ToString());}
    }
    [MenuItem("GameJam/Preparation/Test Full Flow (Play Mode)")]
    public static void Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Play Mode already active");
        SessionState.SetString("PreparationPreviousStart",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool("PreparationChecks",true);SessionState.SetString("PreparationResult","RUNNING");
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(Menu);
        File.WriteAllText(Queue+"/status.txt","RUNNING");EditorApplication.EnterPlaymode();
    }
    [MenuItem("GameJam/Preparation/Test Visual Flow (Play Mode)")]
    public static void StartVisual()
    {
        SessionState.SetBool("PreparationVisualOnly",true);
        Start();
    }
    static void Check(bool ok,string description)
    {if(!ok)throw new Exception(description);File.AppendAllText(Logs+"/checks.txt","PASS: "+description+"\n");}
    static async Task Wait(Func<bool> predicate,int seconds=30)
    {
        var until=DateTime.UtcNow.AddSeconds(seconds);
        while(!predicate()){if(DateTime.UtcNow>until)throw new TimeoutException("Timed out waiting for preparation state");await Task.Delay(30);}
    }
    static void Click(Button button)
    {
        Check(button&&button.gameObject.activeInHierarchy&&button.IsInteractable(),"Clickable: "+(button?button.name:"missing"));
        Canvas.ForceUpdateCanvases();var r=(RectTransform)button.transform;var canvas=button.GetComponentInParent<Canvas>();
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,r.TransformPoint(r.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,"Raycast reaches: "+button.name);
        ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
    static void OneEventSystem()=>Check(UnityEngine.Object.FindObjectsByType<EventSystem>().Count(e=>e.isActiveAndEnabled)==1,"Exactly one active EventSystem");
    static async Task Capture(string file)
    {
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Repaint();
        await Task.Delay(150);ScreenCapture.CaptureScreenshot(Logs+"/"+file+".png");await Task.Delay(250);
    }
    static void Fits(GameObject root)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var text in root.GetComponentsInChildren<TMP_Text>())
        {
            if(string.IsNullOrWhiteSpace(text.text))continue;text.ForceMeshUpdate();
            var b=text.textBounds;var r=text.rectTransform.rect;
            Check(!text.isTextTruncated&&b.min.x>=r.xMin-2&&b.max.x<=r.xMax+2&&b.min.y>=r.yMin-2&&b.max.y<=r.yMax+2,"Readable bounds: "+text.name);
        }
    }
    static async Task KeyPress(int direction)
    {
        if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();
        Key[] keys={Key.UpArrow,Key.DownArrow,Key.LeftArrow,Key.RightArrow};
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys[direction]));await Task.Delay(70);
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());await Task.Delay(70);
    }
    static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    static async Task Open(PreparationDirector d,int index)
    {
        Click(d.Markers[index]);await Wait(()=>d.ActiveActivity==index&&!d.IsBusy);
        Check(d.ActiveActivity==index,"One active activity: "+index);OneEventSystem();
    }
    static async Task Home(PreparationDirector d,int expected)
    {
        await Wait(()=>d.ActiveActivity<0&&!d.IsBusy);
        Check(PreparationRun.CompletedCount==expected,"Completion count retained: "+expected);
        Check(!Find<DispatchCallFlow>()&&SceneManager.GetActiveScene().path==PreparationRun.ScenePath,"Operator not started with fewer than four tasks");
        Fits(d.gameObject);
    }
    static async void Run()
    {
        Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/checks.txt","");errors.Clear();Application.logMessageReceived+=Log;Application.runInBackground=true;
        try
        {
            await Wait(()=>!SceneFadeTransition.IsTransitioning&&Find<AnaMenü>());OneEventSystem();
            var play=UnityEngine.Object.FindObjectsByType<Button>().First(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)=="OyunBaslat"));
            Click(play);
            await Wait(()=>GameObject.Find("Açılış Bilgi Notu"));
            Check(GameObject.Find("Açılış Bilgi Notu"),"Heinrich/BM notice appears after OYNA");
            await Wait(()=>Find<PreparationDirector>()&&!SceneFadeTransition.IsTransitioning);
            var d=Find<PreparationDirector>();
            await Wait(()=>d.Broadcast.StartButton.IsInteractable());
            Check(d.Broadcast.Phase==PreparationBroadcast.BroadcastPhase.Briefing&&!PreparationRun.IsRecording,"OYNA first shows ministry public-service briefing without starting timer");
            d.OpenActivity(0);Check(d.ActiveActivity<0,"Activities cannot start before recording briefing is accepted");
            Fits(d.gameObject);await Capture("00-ministry-briefing");
            Click(d.Broadcast.StartButton);
            await Wait(()=>d.Broadcast.PurposeButton.IsInteractable());
            Check(GameObject.Find("Çekim Öncesi Uyarı"),"Preparation purpose appears just before recording");
            Click(d.Broadcast.PurposeButton);
            await Wait(()=>d.Broadcast.Phase==PreparationBroadcast.BroadcastPhase.Recording&&!d.IsBusy&&!d.Broadcast.VisibleOverlay.gameObject.activeSelf);
            Check(PreparationRun.TakeNumber==1&&PreparationRun.IsRecording,"Countdown starts exactly one timed recording");
            Check(PreparationRun.CompletedCount==0&&d.Markers.Count==4,"OYNA enters fresh house preparation with four markers");
            Check(!Find<DispatchCallFlow>()&&!Find<QuickQuestionManager>(),"No operator systems exist in preparation scene");
            Check(d.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="Kayıt Göstergesi"&&t.text.Contains("REC")),"Subtle REC overlay exists");
            Fits(d.gameObject);OneEventSystem();await Capture("01-house");

            await Open(d,0);Check(d.BagWindow.BagGame.AttemptsLeft==3,"Existing bag starts with three attempts");
            d.OpenActivity(1);Check(d.ActiveActivity==0,"Second activity cannot start while bag is active");
            var bag=d.BagWindow.BagGame;var items=Field<BagDraggableItem[]>(bag,"items");
            int distractors=items.Count(i=>Field<BagDropZone>(i,"correctArea")==null);
            Check(distractors>0&&items.Length>distractors,"Existing bag preserves useful items and distractors");
            bag.Check();bag.Check();Check(bag.AttemptsLeft==2,"Double check consumes only one attempt");
            var draggable=items.First(i=>Field<BagDropZone>(i,"correctArea")!=null);
            var dragPointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,draggable.transform.position)};
            draggable.OnPointerDown(dragPointer);draggable.OnBeginDrag(dragPointer);draggable.OnDrag(dragPointer);draggable.DropInto(bag.BagArea);draggable.OnEndDrag(dragPointer);
            Check(draggable.IsCorrectlyPlaced,"Existing drag and drop works inside the preparation modal");
            foreach(var item in items)
                if(Field<BagDropZone>(item,"correctArea")!=null&&!item.IsCorrectlyPlaced)
                {
                    var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                    item.OnPointerDown(pointer);item.OnPointerClick(pointer);
                }
            Check(items.All(i=>i.IsCorrectlyPlaced),"Real bag item placement recognizes correct contents");
            await Task.Delay(350);await Capture("02-bag");bag.Check();
            await Home(d,1);Check(!d.Markers[0].interactable,"Completed bag marker is disabled");
            d.OpenActivity(0);Check(d.ActiveActivity<0&&PreparationRun.CompletedCount==1,"Completed bag cannot be counted twice");

            await Open(d,1);await Wait(()=>d.Earthquake.Running);
            int beforeTimeout=PreparationRun.Health;await Task.Delay(4150);
            Check(d.Earthquake.Step==0&&d.Earthquake.Running&&PreparationRun.Health<beforeTimeout,"Expired arrow step resets its timer and allows retry");
            int required=d.Earthquake.RequiredDirection;await KeyPress((required+1)%4);
            Check(d.Earthquake.Step==0&&d.Earthquake.Running,"Wrong arrow does not advance or fail training");
            await Capture("03-earthquake");
            for(int step=0;step<3;step++)
            {
                await KeyPress(d.Earthquake.RequiredDirection);
                await Wait(()=>d.Earthquake.Step==step+1);
                Check(!d.Earthquake.IsTransitioning&&d.Earthquake.CurrentPose==new[]{2,4,5}[step],"Arrow movement finishes before next input: "+step);
                await Capture("03-earthquake-stage-"+step);
            }
            await Home(d,2);

            await Open(d,2);Click(d.Flood.Points[0]);
            Check(!d.Flood.Succeeded&&PreparationRun.CompletedCount==2,"Wrong flood point allows retry");
            await Capture("04-flood");await Task.Delay(400);Click(d.Flood.Points[FloodPreparationGame.HighGroundPoint]);await Home(d,3);
            Check(!PreparationRun.preparationComplete,"Three completions do not release operator");

            await Open(d,3);var smoke=d.Detector;await Capture("05-detector-closed");
            Click(smoke.Cover);smoke.ClickCover();await Wait(()=>smoke.Step==1);await Task.Delay(80);
            Check(smoke.Step==1,"Double cover click only opens once");
            Click(smoke.OldBattery);await Wait(()=>smoke.Step==2);await Task.Delay(50);
            Click(smoke.NewBattery);await Wait(()=>smoke.Step==3);await Task.Delay(50);
            await Capture("06-detector-open");Click(smoke.Slot);await Wait(()=>smoke.Step==4);await Task.Delay(50);
            Click(smoke.Cover);await Wait(()=>PreparationRun.preparationComplete);
            Check(PreparationRun.CompletedCount==4,"All four completion states tracked");
            await Wait(()=>d.Broadcast.Phase==PreparationBroadcast.BroadcastPhase.Review);
            Check(!PreparationRun.IsRecording&&PreparationRun.LastTakeSeconds>0&&!PreparationRun.RewardEarned,"First complete take stops clock and establishes baseline without reward");
            Check(!Find<DispatchCallFlow>(),"Recording review waits for explicit publish action");
            double stoppedTime=PreparationRun.LastTakeSeconds;await Task.Delay(250);
            Check(PreparationRun.RecordingSeconds==stoppedTime,"Review time is excluded from recording duration");
            Fits(d.gameObject);await Capture("09-first-take-review");
            Click(d.Broadcast.RetakeButton);d.Broadcast.Retake();
            await Wait(()=>PreparationRun.TakeNumber==2&&PreparationRun.IsRecording&&!d.Broadcast.VisibleOverlay.gameObject.activeSelf);
            Check(PreparationRun.CompletedCount==0&&d.Markers.All(b=>b.interactable),"Retake resets all tasks and markers exactly once");
            Check(!PreparationRun.Publish(),"Incomplete retake cannot be published");
            await CompleteQuickTake(d);
            Check(PreparationRun.LastTakeImproved&&PreparationRun.RewardEarned&&PreparationRun.BestTakeSeconds<stoppedTime,"Faster second take earns team reward");
            double best=PreparationRun.BestTakeSeconds;
            Check(PreparationRun.TeamBonus==0,"Team award waits until the video is published");
            Fits(d.gameObject);await Capture("10-faster-take-reward");
            Click(d.Broadcast.RetakeButton);
            await Wait(()=>PreparationRun.TakeNumber==3&&PreparationRun.IsRecording&&!d.Broadcast.VisibleOverlay.gameObject.activeSelf);
            await Task.Delay((int)((best+1)*1000));await CompleteQuickTake(d);
            Check(!PreparationRun.LastTakeImproved&&PreparationRun.BestTakeSeconds==best&&PreparationRun.RewardEarned,"Slower retake preserves best video and one-time reward");
            Fits(d.gameObject);await Task.Delay(450);Click(d.Broadcast.PublishButton);d.Broadcast.Publish();
            Check(PreparationRun.Published&&PreparationRun.TeamBonus==1,"Publish is locked once and grants exactly one team of each type");
            await Wait(()=>d.Broadcast.Phase==PreparationBroadcast.BroadcastPhase.Publishing);await Task.Delay(1800);Fits(d.gameObject);await Capture("11-public-service-montage");
            await Wait(()=>d.Broadcast.Phase==PreparationBroadcast.BroadcastPhase.MonthsLater,20);
            Check(d.Broadcast.ClipsShown==4,"Publication montage presents all four safety messages");await Capture("12-months-later");
            await Wait(()=>d.Broadcast.Phase==PreparationBroadcast.BroadcastPhase.Disaster);
            Check(d.Broadcast.DidShake,"Multiple-disaster announcement triggers screen tremor");Fits(d.gameObject);await Capture("13-disaster-announcement");
            await Wait(()=>SceneManager.GetActiveScene().path==PreparationRun.OperatorScene&&!SceneFadeTransition.IsTransitioning,40);
            Check(!Find<PreparationDirector>(),"Published video and months-later story transition to existing operator scene");OneEventSystem();
            var awardedResources=Find<DispatchResources>();
            Check(awardedResources.Ambulances==Field<int>(awardedResources,"initialAmbulances")+1&&awardedResources.Firefighters==Field<int>(awardedResources,"initialFirefighters")+1&&awardedResources.RescueTeams==Field<int>(awardedResources,"initialPolice")+1,"Operator starts with one real bonus ambulance, fire and rescue team");
            int awardedAmbulances=awardedResources.Ambulances;awardedResources.ResetForNewShift();
            Check(awardedResources.Ambulances==awardedAmbulances,"Resource initialization cannot stack the recording reward");
            var minigames=Find<MiniGameWindow>();
            Check(!minigames.BagEntryButton.gameObject.activeInHierarchy&&!minigames.SafetyEntryButton.gameObject.activeInHierarchy,"Operator has no mini-game entry buttons");
            minigames.OpenBag();minigames.OpenSafety();
            Check(!minigames.BagPanel.activeInHierarchy&&!minigames.SafetyPanel.activeInHierarchy,"Legacy operator callbacks cannot open mini-games");
            Check(!Find<DispatchTablet>()||!Find<DispatchTablet>().enabled,"Operator tablet trigger disabled");
            var phone=Find<DispatchCallFlow>();await Wait(()=>phone.State==DispatchCallFlow.CallState.IncomingCall);
            phone.AnswerPhone();await Wait(()=>phone.State==DispatchCallFlow.CallState.Conversation);
            Check(Find<CallerInfoView>()&&Find<DispatchResources>()&&Find<OnurConversationView>(),"Operator caller, resource and dialogue systems intact");
            if(SessionState.GetBool("PreparationVisualOnly",false))
            {
                var health=Find<CityHealth>();var fill=Field<RectTransform>(health,"healthFill");
                int originalHealth=health.HealthPercent;
                health.ApplyDamage(5);await Task.Delay(500);
                Check(Mathf.Abs(fill.anchorMax.x-health.HealthPercent/100f)<.02f,"Public awareness bar tracks percentage damage");
                health.RestoreScenarioHealth(originalHealth,health.WrongDecisionCount,health.TimedOutDecisionCount);
                await Wait(()=>Find<QuickQuestionView>()&&Find<QuickQuestionView>().Ready,30);
                await Capture("07-operator-quick-question");
                Check(errors.Count==0,"No new Play Mode errors: "+string.Join("; ",errors));
                File.AppendAllText(Logs+"/checks.txt","VISUAL COMPLETE\n");
                SessionState.SetString("PreparationResult","PASS — visual preparation and operator entry");
                return;
            }
            await RunOperatorShift();
            await Wait(()=>Find<ShiftReportView>()&&Find<ShiftReportView>().IsVisible&&!SceneFadeTransition.IsTransitioning,40);
            var report=Find<ShiftReportView>();var snapshot=report.Data;
            Check(snapshot.Completed==8&&snapshot.SuccessfulTasks==4,"Ending retains eight calls and four preparation completions");
            Check(snapshot.Answered+snapshot.Missed==16,"All sixteen Quick Questions remain in operator flow");
            OneEventSystem();await Capture("08-ending");Click(report.ReplayButton);
            await Wait(()=>Find<PreparationDirector>()&&!SceneFadeTransition.IsTransitioning);
            Check(PreparationRun.CompletedCount==0&&PreparationRun.Health==100&&ShiftReportFlow.Pending==null,"Retry restarts full preparation with cleared state");
            Check(!Find<QuickQuestionManager>()&&!Find<DispatchResources>(),"Retry disposes old operator resources and Quick Question data");OneEventSystem();
            Check(!PreparationRun.RewardEarned&&!PreparationRun.Published&&PreparationRun.TakeNumber==0,"Full-run retry clears recording history and bonus");
            d=Find<PreparationDirector>();await Wait(()=>d.Broadcast.StartButton.IsInteractable());Click(d.Broadcast.StartButton);
            await Wait(()=>d.Broadcast.PurposeButton.IsInteractable());Click(d.Broadcast.PurposeButton);
            await Wait(()=>!d.IsBusy&&!d.Broadcast.VisibleOverlay.gameObject.activeSelf);
            // Use the normal return-to-menu button, then OYNA again.
            var menu=UnityEngine.Object.FindObjectsByType<Button>().First(b=>b.name=="Ana Menü");Click(menu);
            await Wait(()=>SceneManager.GetActiveScene().path==Menu&&!SceneFadeTransition.IsTransitioning);
            play=UnityEngine.Object.FindObjectsByType<Button>().First(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)=="OyunBaslat"));Click(play);
            await Wait(()=>Find<PreparationDirector>()&&!SceneFadeTransition.IsTransitioning);
            Check(PreparationRun.CompletedCount==0&&!Find<DispatchCallFlow>(),"Main menu new game begins in fresh preparation again");OneEventSystem();
            Check(errors.Count==0,"No new Play Mode errors: "+string.Join("; ",errors));
            File.AppendAllText(Logs+"/checks.txt","COMPLETE\n");SessionState.SetString("PreparationResult","PASS — Logs/Preparation/checks.txt");
        }
        catch(Exception e){File.AppendAllText(Logs+"/checks.txt","FAIL: "+e+"\n");SessionState.SetString("PreparationResult",e.ToString());}
        finally{SessionState.SetBool("PreparationVisualOnly",false);if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());Application.logMessageReceived-=Log;EditorApplication.ExitPlaymode();}
    }
    static async Task CompleteQuickTake(PreparationDirector d)
    {
        await Open(d,0);var bag=d.BagWindow.BagGame;
        foreach(var item in Field<BagDraggableItem[]>(bag,"items"))
            if(Field<BagDropZone>(item,"correctArea")!=null)
            {var p=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};item.OnPointerDown(p);item.OnPointerClick(p);}
        bag.Check();await Home(d,1);
        await Open(d,1);await Wait(()=>d.Earthquake.Running);
        for(int i=0;i<3;i++){await KeyPress(d.Earthquake.RequiredDirection);int next=i+1;await Wait(()=>d.Earthquake.Step==next);}await Home(d,2);
        await Open(d,2);Click(d.Flood.Points[FloodPreparationGame.HighGroundPoint]);await Home(d,3);
        await Open(d,3);var smoke=d.Detector;
        Click(smoke.Cover);await Wait(()=>smoke.Step==1);await Task.Delay(50);
        Click(smoke.OldBattery);await Wait(()=>smoke.Step==2);await Task.Delay(50);
        Click(smoke.NewBattery);await Wait(()=>smoke.Step==3);await Task.Delay(50);
        Click(smoke.Slot);await Wait(()=>smoke.Step==4);await Task.Delay(50);Click(smoke.Cover);
        await Wait(()=>d.Broadcast.Phase==PreparationBroadcast.BroadcastPhase.Review);
    }
    static async Task RunOperatorShift()
    {
        var briefing=Find<PresidentialBriefingManager>();var conversation=Find<OyunAkisiOnur>();var chat=Find<OnurConversationView>();
        var questions=Find<QuickQuestionManager>();var view=Find<QuickQuestionView>();var resources=Find<DispatchResources>();var broadcast=Find<PresidentialBroadcastView>();
        var health=Find<CityHealth>();var minigames=Find<MiniGameWindow>();
        broadcast.charactersPerSecond=0;
        var scenarios=Field<DispatchScenarioData[]>(briefing,"scenarioOrder");var answered=new HashSet<int>();
        var deadline=DateTime.UtcNow.AddMinutes(6);bool keyDown=false;int last=-1;bool firstRequest=false;
        while(SceneManager.GetActiveScene().path==PreparationRun.OperatorScene)
        {
            if(DateTime.UtcNow>deadline)throw new TimeoutException("Full operator shift");
            if(health.IsGameOver)throw new Exception("Operator died during full-flow test");
            if(briefing.CompletedScenarios!=last){last=briefing.CompletedScenarios;File.AppendAllText(Logs+"/checks.txt","PROGRESS: calls "+last+"/8\n");}
            CheckNoMinigame(minigames);
            chat.RevealCurrentLine();
            if(view.Ready)
            {
                if(!firstRequest){firstRequest=true;await Capture("07-operator-quick-question");}
                int choice=questions.CurrentQuestion.isDisinformation?questions.CurrentQuestion.correctChoiceIndex:1;
                var button=choice==0?view.ButtonA:view.ButtonB;
                if(button.interactable)button.onClick.Invoke();
            }
            if(conversation.WaitingForChoice&&!answered.Contains(conversation.ScenarioRevision))
            {
                var scenario=scenarios[Mathf.Min(briefing.CompletedScenarios,7)];var button=scenario.correctChoiceIsB?chat.ChoiceBButton:chat.ChoiceAButton;
                if(button.interactable){answered.Add(conversation.ScenarioRevision);button.onClick.Invoke();}
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
            {var button=Field<Button>(broadcast,"advance");if(button.gameObject.activeInHierarchy&&button.interactable)button.onClick.Invoke();}
            var connection=Field<GameObject>(chat,"connectionOverlay");
            if(connection&&connection.activeSelf&&Keyboard.current!=null){keyDown=!keyDown;InputSystem.QueueStateEvent(Keyboard.current,keyDown?new KeyboardState(Key.Space):new KeyboardState());}
            await Task.Delay(100);
        }
    }
    static void CheckNoMinigame(MiniGameWindow window)
    {
        if(window.BagPanel.activeInHierarchy||window.SafetyPanel.activeInHierarchy||Find<PreparationDirector>()||Find<PreparationBroadcast>()||Find<EarthquakePreparationGame>()||Find<FloodPreparationGame>()||Find<SmokeDetectorPreparationGame>())
            throw new Exception("Preparation mini-game appeared in operator phase");
    }
}
