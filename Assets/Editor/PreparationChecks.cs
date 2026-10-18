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
            else if(command=="reimport-bag")
            {
                foreach(var imagePath in Directory.GetFiles("Assets/Working Folder","*.png",SearchOption.AllDirectories))
                    if(imagePath.Replace('\\','/').Contains("/MiniGames/Cutouts/"))AssetDatabase.ImportAsset(imagePath,ImportAssetOptions.ForceUpdate);
                File.WriteAllText(Queue+"/status.txt","BAG REIMPORTED");
            }
            else if(command=="test-sequential"){SessionState.SetBool("SkipVerifiedSharedClock",true);Start();}
            else if(command=="test"||command=="test-focused")Start();
            else if(command=="test-visual")StartVisual();
            else if(command=="test-polish-flow"){SessionState.SetBool("PreparationFastTimeouts",true);Start();}
            else if(command=="test-polish-edges"){SessionState.SetBool("PreparationEdges",true);Start();}
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
    static void DragBattery(Button battery,Button destination)
    {
        Canvas.ForceUpdateCanvases();
        var canvas=battery.GetComponentInParent<Canvas>();
        var source=(RectTransform)battery.transform;
        var target=(RectTransform)destination.transform;
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
            position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,source.TransformPoint(source.rect.center))};
        ExecuteEvents.Execute(battery.gameObject,pointer,ExecuteEvents.beginDragHandler);
        pointer.position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,target.TransformPoint(target.rect.center));
        ExecuteEvents.Execute(battery.gameObject,pointer,ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(battery.gameObject,pointer,ExecuteEvents.endDragHandler);
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
        var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));gameView.Focus();
        await Task.Delay(100);
        int frame=Time.frameCount;
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys[direction]));
        await Wait(()=>Time.frameCount>=frame+3,5);
        frame=Time.frameCount;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        await Wait(()=>Time.frameCount>=frame+2,5);
    }
    static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    static async Task Open(PreparationDirector d,int index)
    {
        float before=d.RemainingSeconds;Click(d.Markers[index]);Check(d.TimerRunning&&d.RemainingSeconds<=before&&d.RemainingSeconds>0,"Shared timer continues on entry: "+index);await Wait(()=>d.ActiveActivity==index&&!d.IsBusy);
        Check(d.ActiveActivity==index,"One active activity: "+index);OneEventSystem();
    }
    static async Task Home(PreparationDirector d,int expected)
    {
        await Wait(()=>d.ActiveActivity<0&&!d.IsBusy);
        Check(PreparationRun.CompletedCount==expected,"Completion count retained: "+expected);
        Check(!Find<DispatchCallFlow>()&&SceneManager.GetActiveScene().path==PreparationRun.ScenePath,"Operator not started with fewer than six tasks");
        Fits(d.gameObject);
    }
    static async void Run()
    {
        Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/checks.txt","");errors.Clear();Application.logMessageReceived+=Log;Application.runInBackground=true;
        var inputSettings=InputSystem.settings;var previousInputBehavior=inputSettings.editorInputBehaviorInPlayMode;var previousBackgroundBehavior=inputSettings.backgroundBehavior;
        inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        EditorWindow testGameView=null;PropertyInfo sizeProperty=null;bool previousMax=false;int previousSize=0;
        try
        {
            testGameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            previousMax=testGameView.maximized;sizeProperty=testGameView.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            previousSize=(int)sizeProperty.GetValue(testGameView);testGameView.maximized=true;
            var sizesType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
            var singletonType=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes=singletonType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
            var groupMethod=sizesType.GetMethod("GetGroup");
            var group=groupMethod.Invoke(sizes,new[]{Enum.Parse(groupMethod.GetParameters()[0].ParameterType,"Standalone")});
            var names=(string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group,null);
            int widescreen=Array.FindIndex(names,n=>n.Replace(" ","").Contains("16:9")||n.Replace(" ","").Contains("1920x1080"));
            Check(widescreen>=0,"Editor has a standard widescreen test size");sizeProperty.SetValue(testGameView,widescreen);testGameView.Focus();
            int frame=Time.frameCount;await Wait(()=>Time.frameCount>=frame+3);
            Check(Mathf.Abs(Screen.width/(float)Screen.height-16f/9)<.02f,"UI testing uses a 16:9 viewport");
            await Wait(()=>!SceneFadeTransition.IsTransitioning&&Find<AnaMenü>());OneEventSystem();await TestSettings();
            Check(!Directory.GetFiles("Assets","*.unity",SearchOption.AllDirectories).Any(path=>File.ReadAllText(path).Contains("331573029c7e0cd48b178c2731e7abfd")),"AnaMenu1 has no scene references");
            var sounds=Resources.Load<DispatchSoundLibrary>("DispatchSoundLibrary");
            Check(sounds&&sounds.buttonClick&&sounds.correct&&sounds.incorrect&&sounds.resourceChange&&sounds.transition&&sounds.phoneRing&&sounds.phoneOpen&&sounds.phoneClose,"Existing project clips are connected");
            var play=UnityEngine.Object.FindObjectsByType<Button>().First(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)=="OyunBaslat"));
            int clicks=GameAudioFeedback.Instance.ClickCount;Click(play);
            Check(GameAudioFeedback.Instance.ClickCount==clicks+1&&GameAudioFeedback.Instance.GetComponent<AudioSource>().isPlaying,"Button click plays once through active audio source");
            await Wait(()=>Find<PreparationDirector>()&&!SceneFadeTransition.IsTransitioning);
            var d=Find<PreparationDirector>();await Wait(()=>d.Broadcast.StartButton.IsInteractable());
            Check(!d.TimerRunning&&!PreparationRun.IsRecording,"Preparation clock does not run during briefing");
            Click(d.Broadcast.StartButton);await Wait(()=>d.Broadcast.PurposeButton.IsInteractable());Click(d.Broadcast.PurposeButton);
            await Wait(()=>PreparationRun.IsRecording&&!d.IsBusy&&!d.Broadcast.VisibleOverlay.gameObject.activeSelf);
            Check(d.TimerRunning&&d.RemainingSeconds>74,"One 75-second clock starts when preparation becomes playable");
            Check(d.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("6 ÖNEMLİ AFET HAZIRLIĞI")),"Requested preparation text is visible");
            TestBagTransparency();bool skipClock=SessionState.GetBool("SkipVerifiedSharedClock",false);SessionState.SetBool("SkipVerifiedSharedClock",false);
            if(!skipClock)await TestSharedPreparationClock(d);
            // The first completed recording deliberately includes a little map time.
            await Task.Delay(6000);await CompleteQuickTake(d);
            Check(!d.TimerRunning&&PreparationRun.CompletedCount==6,"Completing all six tasks stops the shared timer");
            float stopped=d.RemainingSeconds;await Task.Delay(250);Check(d.RemainingSeconds==stopped,"Review does not consume preparation time");
            Check(PreparationRun.LastTakeSeconds<=75,"All six games completed within one total 75-second take");
            await Capture("focused-review");Click(d.Broadcast.RetakeButton);
            await Wait(()=>PreparationRun.IsRecording&&!d.Broadcast.VisibleOverlay.gameObject.activeSelf&&!d.IsBusy);
            Check(d.RemainingSeconds>74&&PreparationRun.CompletedCount==0,"Retake starts a fresh total budget and resets six tasks");
            await CompleteQuickTake(d);Check(PreparationRun.RewardEarned,"Existing faster-retake reward remains functional");
            Click(d.Broadcast.PublishButton);await Wait(()=>d.Broadcast.Phase==PreparationBroadcast.BroadcastPhase.Publishing);
            await Task.Delay(1500);Fits(d.gameObject);await Capture("focused-publication");
            Check(d.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("6 ÖNEMLİ AFET HAZIRLIĞI")),"Publication uses requested heading");
            await Wait(()=>SceneManager.GetActiveScene().path==PreparationRun.OperatorScene&&!SceneFadeTransition.IsTransitioning,40);
            Check(!Find<PreparationDirector>(),"Shared preparation timer does not exist in operator phase");
            var manager=Find<QuickQuestionManager>();var flow=Find<OyunAkisiOnur>();var chat=Find<OnurConversationView>();var phone=Find<DispatchCallFlow>();
            var starts=new List<string>();var sides=new List<int>();
            int dualStarts=0;
            manager.QuestionStarted+=q=>{
                int visible=manager.Views.Count(v=>v.IsActive);Check(visible<=3,"Never more than three active question cards: "+q.id);
                if(visible==2)dualStarts++;
            };
            manager.QuestionStarted+=q=>{starts.Add(q.targetScenarioId);sides.Add(manager.Views[0].Config==q?0:1);};
            double decisionStart=0,decisionElapsed=0;int decisionTimeouts=0,phoneCloses=0;
            chat.ChoicesShown+=()=>decisionStart=Time.realtimeSinceStartupAsDouble;
            flow.DecisionTimedOut+=()=>{decisionTimeouts++;decisionElapsed=Time.realtimeSinceStartupAsDouble-decisionStart;};
            var radio=Find<Cizirti>();
            if(!radio.CizirtiSesi.clip)
            {
                // Temporary silent fixture: validates lifecycle only, never saved to the scene.
                radio.CizirtiSesi.clip=AudioClip.Create("TEST ONLY - missing telefondayken",44100*2,1,44100,false);
                File.AppendAllText(Logs+"/checks.txt","NOTE: telefondayken is absent; phone loop lifecycle uses a temporary silent fixture.\n");
            }
            flow.ScenarioClosing+=(revision)=>{if(radio.TiklamaSesi.isPlaying)phoneCloses++;Check(!radio.CizirtiSesi.isPlaying,"Conversation loop stops at call closing");};
            await Wait(()=>phone.State==DispatchCallFlow.CallState.IncomingCall);
            var ring=Field<AudioSource>(phone,"ringAudioSource");Check(ring.isPlaying&&ring.clip==sounds.phoneRing,"Existing phone ring is playing");
            bool openPlayed=false;phone.Answered+=()=>openPlayed=radio.TiklamaSesi.isPlaying;
            phone.AnswerPhone();Check(openPlayed&&!ring.isPlaying,"Phone opening plays and ringing stops");
            Check(!radio.CizirtiSesi.isPlaying,"Conversation loop stays off during phone focus transition");
            await Wait(()=>phone.State==DispatchCallFlow.CallState.Conversation);
            Check(radio.CizirtiSesi.isPlaying&&radio.CizirtiSesi.loop,"Conversation loop begins when the conversation is playable");
            await Task.Delay(130);int samples=radio.CizirtiSesi.timeSamples;radio.StartConversation();radio.StartConversation();
            Check(radio.CizirtiSesi.timeSamples>=samples&&UnityEngine.Object.FindObjectsByType<AudioSource>().Count(a=>a.isPlaying&&a.clip==radio.CizirtiSesi.clip)==1,"Repeated conversation start neither restarts nor stacks the loop");
            await TestDualQuestions(manager,chat);
            while(!flow.WaitingForChoice){chat.RevealCurrentLine();await Task.Delay(80);}
            Check(Mathf.Approximately(Field<float>(flow,"unansweredDecisionSeconds"),12),"Main two-option decision base limit is twelve seconds");
            await Wait(()=>decisionTimeouts>0,12);
            Check(decisionElapsed>=11.8&&decisionElapsed<12.5,"Actual later decision timeout occurs at twelve seconds: "+decisionElapsed.ToString("F2"));
            int timeoutCount=decisionTimeouts;await Task.Delay(500);Check(decisionTimeouts==timeoutCount&&flow.WaitingForChoice,"Decision timeout notifies once without duplicate action");
            await RunOperatorShift();
            await Wait(()=>Find<ShiftReportView>()&&Find<ShiftReportView>().IsVisible&&!SceneFadeTransition.IsTransitioning,40);
            var report=Find<ShiftReportView>();Check(report.Data.Completed==7&&report.Data.SuccessfulTasks==6,"Ending retains seven scenarios and six preparation tasks");
            Check(report.Data.Answered+report.Data.Missed==19,"Ending records one introductory and 18 later Quick Questions");
            Check(report.Data.VerificationMistakes==2,"Report includes both actual incorrect verification answers");
            Check(starts.Count==19&&starts.GroupBy(id=>id).Count()==7&&starts.GroupBy(id=>id).Count(g=>g.Count()==1)==1&&starts.GroupBy(id=>id).Count(g=>g.Count()==3)==6,"First scenario starts one question; later scenarios start three");
            for(int i=0;i<7;i++)
            {
                int left=sides.Skip(i*3).Take(3).Count(side=>side==0);
                Check(left==(i%2==0?2:1),"Scenario side pattern is 2+1 or 1+2: "+i);
            }
            Check(dualStarts>=6,"Later scenarios support the three-panel layout");
            Check(phoneCloses==7,"Phone close sound plays at every call ending");
            Check(GameAudioFeedback.Instance.ResourceCount>0&&GameAudioFeedback.Instance.TransitionCount>0&&GameAudioFeedback.Instance.CorrectCount>0&&GameAudioFeedback.Instance.IncorrectCount>0,"Resource, transition, correct and incorrect sound routes actually fired");
            await Capture("focused-ending");Click(report.ReplayButton);
            await Wait(()=>Find<PreparationDirector>()&&!SceneFadeTransition.IsTransitioning);
            Check(!Find<QuickQuestionManager>()&&PreparationRun.CompletedCount==0&&!PreparationRun.IsRecording,"Replay removes both question cards and resets preparation");
            d=Find<PreparationDirector>();await Wait(()=>d.Broadcast.StartButton.IsInteractable());Click(d.Broadcast.StartButton);
            await Wait(()=>d.Broadcast.PurposeButton.IsInteractable());Click(d.Broadcast.PurposeButton);
            await Wait(()=>d.TimerRunning&&!d.IsBusy&&!d.Broadcast.VisibleOverlay.gameObject.activeSelf);
            await Open(d,3);Click(d.Detector.Cover);typeof(PreparationDirector).GetField("remainingSeconds",Flags).SetValue(d,.01f);
            await Wait(()=>d.TimedOut);await Task.Delay(150);Click(d.TimeoutReturnButton);
            await Wait(()=>SceneManager.GetActiveScene().path==Menu&&!SceneFadeTransition.IsTransitioning);
            await Task.Delay(350);Check(!Find<PreparationDirector>(),"Timeout during an animation can return safely to the main menu");
            Check(!UnityEngine.Object.FindObjectsByType<TMP_Text>().Any(t=>t.text.ToUpperInvariant().Contains("LANET")),"Visible text contains no LANET wording");
            Check(errors.Count==0,"No new Play Mode errors: "+string.Join("; ",errors));
            File.AppendAllText(Logs+"/checks.txt","COMPLETE FOCUSED FLOW\n");SessionState.SetString("PreparationResult","PASS — six preparation games, shared 75 seconds, dual questions and full shift");
        }
        catch(Exception e){File.AppendAllText(Logs+"/checks.txt","FAIL: "+e+"\n");SessionState.SetString("PreparationResult",e.ToString());}
        finally
        {
            inputSettings.editorInputBehaviorInPlayMode=previousInputBehavior;inputSettings.backgroundBehavior=previousBackgroundBehavior;
            if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            Application.logMessageReceived-=Log;
            try{if(testGameView){if(sizeProperty!=null)sizeProperty.SetValue(testGameView,previousSize);testGameView.maximized=previousMax;}}
            finally{EditorApplication.ExitPlaymode();}
        }
    }
    static async Task TestSharedPreparationClock(PreparationDirector d)
    {
        double started=Time.realtimeSinceStartupAsDouble;
        float last=d.RemainingSeconds;
        for(int i=0;i<PreparationRun.ActivityCount;i++)
        {
            await Open(d,i);Check(d.RemainingSeconds<last,"Same preparation budget enters game: "+i);
            if(i==3){Click(d.Detector.Cover);await Wait(()=>d.Detector.Step==1);}
            await Task.Delay(200);last=d.RemainingSeconds;Click(d.BackButton);await Home(d,0);
            Check(d.TimerRunning&&d.RemainingSeconds<last,"Shared clock continues on the house map: "+i);last=d.RemainingSeconds;
        }
        await TestPressureEdges(d);
        await Open(d,0);await Capture("focused-shared-blue");
        await Wait(()=>d.RemainingSeconds<14,75);Check(Field<Image>(d,"timerFill").color.r>.8f,"Shared low-time bar becomes amber");await Capture("focused-shared-amber");
        await Wait(()=>d.RemainingSeconds<4,15);await Capture("focused-shared-red");
        var item=Field<BagDraggableItem[]>(d.BagWindow.BagGame,"items")[0];
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,item.transform.position)};
        item.OnPointerDown(pointer);item.OnBeginDrag(pointer);
        await Wait(()=>d.TimedOut,8);await Task.Delay(120);
        double elapsed=Time.realtimeSinceStartupAsDouble-started;
        Check(elapsed>=74&&elapsed<76,"Actual whole-phase timeout is 75 seconds: "+elapsed.ToString("F2"));
        Check(!d.TimerRunning&&!PreparationRun.IsRecording&&item.GetComponent<CanvasGroup>().blocksRaycasts,"Timeout cancels drag and stops preparation safely");
        Check(Field<AudioSource>(d,"activityAudio").isPlaying,"Existing mini-game failure clip plays on shared timeout");
        d.OpenActivity(1);Check(d.TimedOut,"No activity can reopen after the total budget expires");
        await Capture("focused-shared-timeout");int take=PreparationRun.TakeNumber;Click(d.RetryButton);d.RetryButton.onClick.Invoke();
        Check(PreparationRun.TakeNumber==take+1&&d.TimerRunning&&d.RemainingSeconds>74&&d.ActiveActivity<0&&PreparationRun.CompletedCount==0,"Timeout retry starts one fresh whole-phase take");
        await Open(d,3);await Task.Delay(400);Check(d.Detector.Step==0,"Previous detector interaction cannot affect retry");Click(d.BackButton);await Home(d,0);
    }
    static Rect WorldRect(RectTransform r)
    {
        var corners=new Vector3[4];r.GetWorldCorners(corners);return new Rect(corners[0],corners[2]-corners[0]);
    }
    static void TestBagTransparency()
    {
        string[] names={"bag","ball","controller","first_aid","flashlight","radio","water"};
        foreach(string root in new[]{"Assets/Working Folder/Onur","Assets/Working Folder/Taha and Yasin/Onur"})
            foreach(string name in names)
            {
                string path=root+"/MiniGames/Cutouts/"+name+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                Check(importer.alphaIsTransparency&&importer.filterMode==FilterMode.Point&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed,"Pixel transparency import: "+path);
                var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(path));
                var original=source.GetPixels32();var imported=AssetDatabase.LoadAssetAtPath<Texture2D>(path).GetPixels32();
                int removed=0,whiteKept=0;
                for(int i=0;i<original.Length;i++)
                {
                    var c=original[i];int min=Mathf.Min(c.r,Mathf.Min(c.g,c.b)),max=Mathf.Max(c.r,Mathf.Max(c.g,c.b));
                    if(c.a>0&&imported[i].a==0){removed++;if(min<185||max-min>40)throw new Exception("Colored item pixel removed: "+path);}
                    if(min>150&&max-min<50&&imported[i].a>0)whiteKept++;
                }
                Check(removed>20,"White exterior matte removed without changing colored pixels: "+name+" ("+removed+")");
                if(name=="first_aid")Check(whiteKept>0,"Interior white first-aid detail is preserved");
                UnityEngine.Object.Destroy(source);
            }
    }
    static async Task TestDualQuestions(QuickQuestionManager manager,OnurConversationView chat)
    {
        var fog=Find<DispatchCurseDirector>();fog.ApplyFog();Check(!fog.IsFogActive,"Zero wrong answers do not trigger fog");
        var until=DateTime.UtcNow.AddSeconds(35);
        while(!manager.Views.All(v=>v.Ready)){if(DateTime.UtcNow>until)throw new TimeoutException("Dual question arrival");chat.RevealCurrentLine();await Task.Delay(70);}
        var left=manager.Views[0];var right=manager.Views[1];
        Check(left.Ready&&right.Ready,"Two questions appear together on opposite sides");
        Check(!WorldRect((RectTransform)left.transform).Overlaps(WorldRect((RectTransform)right.transform)),"Left and right question panels never overlap");
        foreach(var card in manager.Views){Fits(card.gameObject);Check(!WorldRect((RectTransform)card.transform).Overlaps(WorldRect((RectTransform)chat.transform)),"Dual questions preserve the main dialogue");}
        await Capture("five-dual-questions");float otherTime=right.ElapsedResponseTime;string first=left.Config.id;int before=manager.Results.Count;
        int resourceSounds=GameAudioFeedback.Instance.ResourceCount;Click(left.ButtonA);left.ButtonA.onClick.Invoke();
        Check(manager.Results.Count==before+1&&GameAudioFeedback.Instance.ResourceCount==resourceSounds+1,"Double click applies one result and one resource effect");
        await Wait(()=>left.Ready&&left.Config.id!=first);
        Check(right.Ready&&right.ElapsedResponseTime>otherTime,"Other card remains clickable with its independently advancing timer");
        Click(left.Config.correctChoiceIndex==0?left.ButtonB:left.ButtonA);Check(fog.WrongAnswerCount==1,"First actual wrong answer counted");fog.ApplyFog();Check(!fog.IsFogActive,"One wrong answer does not trigger fog");
        Click(right.Config.correctChoiceIndex==0?right.ButtonB:right.ButtonA);
        Check(fog.WrongAnswerCount==2,"Two actual wrong answers reach the fog threshold");
        await Wait(()=>manager.Results.Count==3&&!manager.Views.Any(v=>v.IsActive));
        Check(manager.Results.Select(r=>r.questionId).Distinct().Count()==3,"Three distinct question results with no duplicate timers/actions");
    }
    static async Task TestSettings()
    {
        await Wait(()=>Find<MasterVolumeSettings>());
        var settings=Find<MasterVolumeSettings>();
        string[] keys={"912.MasterVolume","912.Display.Width","912.Display.Height","912.Display.Fullscreen"};
        bool[] existed=keys.Select(PlayerPrefs.HasKey).ToArray();
        float volume=PlayerPrefs.GetFloat(keys[0],1);int[] values=keys.Skip(1).Select(k=>PlayerPrefs.GetInt(k)).ToArray();
        int width=Screen.width,height=Screen.height;var mode=Screen.fullScreenMode;
        try
        {
            Click(settings.OpenButton);await Task.Delay(80);Check(settings.IsOpen,"Settings opens in canonical main menu");
            var slider=Field<Slider>(settings,"volumeSlider");
            var rect=(RectTransform)slider.transform;
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector2(rect.rect.xMin+rect.rect.width*.75f,rect.rect.center.y)))};
            slider.OnPointerDown(pointer);
            Check(Mathf.Abs(slider.value-.75f)<.04f&&slider.handleRect.rect.width>=18,"Volume track clicks work and handle stays visible");
            pointer.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector2(rect.rect.xMin+rect.rect.width*.25f,rect.rect.center.y)));
            slider.OnDrag(pointer);slider.OnPointerUp(pointer);
            Check(Mathf.Abs(slider.value-.25f)<.04f,"Volume slider drag follows pointer");
            slider.value=.37f;
            Check(Mathf.Abs(AudioListener.volume-.37f)<.001f&&Mathf.Abs(PlayerPrefs.GetFloat(keys[0])-.37f)<.001f,"Volume preview and persistence work");
            Fits(Field<GameObject>(settings,"panel"));await Capture("polish-settings");
            bool fullscreen=settings.PendingFullscreen;var resolution=settings.PendingResolution;
            Click(settings.FullscreenButton);Click(settings.ResolutionButton);
            Check(settings.PendingFullscreen!=fullscreen&&settings.PendingResolution!=resolution,"Fullscreen and resolution selectors change pending settings");
            Click(settings.BackButton);Check(!settings.IsOpen,"Back closes settings safely");
            Click(settings.OpenButton);await Task.Delay(80);Check(settings.PendingFullscreen==fullscreen&&settings.PendingResolution==resolution,"Back discards unapplied display choices");
            Click(settings.FullscreenButton);Click(settings.ResolutionButton);
            var applied=settings.PendingResolution;bool appliedFullscreen=settings.PendingFullscreen;
            Click(settings.ApplyButton);await Task.Delay(200);
            Check(PlayerPrefs.GetInt(keys[1])==applied.x&&PlayerPrefs.GetInt(keys[2])==applied.y&&PlayerPrefs.GetInt(keys[3])==(appliedFullscreen?1:0),"Apply saves selected display settings and calls Screen.SetResolution");
            Click(settings.BackButton);Click(settings.OpenButton);await Task.Delay(80);
            Check(settings.PendingResolution==applied&&settings.PendingFullscreen==appliedFullscreen,"Applied settings survive reopening");
            Click(settings.BackButton);
        }
        finally
        {
            for(int i=0;i<keys.Length;i++)
                if(!existed[i])PlayerPrefs.DeleteKey(keys[i]);else if(i==0)PlayerPrefs.SetFloat(keys[i],volume);else PlayerPrefs.SetInt(keys[i],values[i-1]);
            PlayerPrefs.Save();AudioListener.volume=volume;Screen.SetResolution(width,height,mode);
        }
    }
    static async Task MoveUntil(Key key,Func<bool> reached)
    {
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Focus();
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));
        try{await Wait(reached,8);}finally{int frame=Time.frameCount;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());await Wait(()=>Time.frameCount>=frame+2,5);}
    }
    static async Task AdjustExtinguisher(ExtinguisherPreparationGame game)
    {
        game.SetAdjustment(1);await Wait(()=>game.Pressure>=.50f,5);game.SetAdjustment(0);
        await Task.Delay(850);Click(game.CheckButton);Check(game.Succeeded,"Pressure stabilizes in green range and confirms");
    }
    static async Task TestPressureEdges(PreparationDirector d)
    {
        await Open(d,4);int before=PreparationRun.Mistakes;Click(d.Extinguisher.CheckButton);
        Check(!d.Extinguisher.Succeeded&&PreparationRun.Mistakes==before+1,"Unsafe pressure check fails once");
        d.Extinguisher.SetAdjustment(1);await Task.Delay(250);Click(d.BackButton);await Home(d,0);
        float pressure=d.Extinguisher.Pressure;await Task.Delay(150);Check(d.Extinguisher.Pressure==pressure,"Hidden pressure rig stops adjusting");
    }
    static async Task CompleteQuickTake(PreparationDirector d)
    {
        await Open(d,0);await Capture("bag-cleaned");var bag=d.BagWindow.BagGame;
        foreach(var item in Field<BagDraggableItem[]>(bag,"items"))
            if(Field<BagDropZone>(item,"correctArea")!=null)
            {var p=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};item.OnPointerDown(p);item.OnPointerClick(p);}
        bag.Check();await Wait(()=>d.IsBusy);Check(Field<AudioSource>(d,"activityAudio").isPlaying,"Existing preparation success clip is playing");await Home(d,1);
        await Open(d,1);await Wait(()=>d.Earthquake.Running);
        for(int i=0;i<3;i++){await KeyPress(d.Earthquake.RequiredDirection);int next=i+1;await Wait(()=>d.Earthquake.Step==next);}await Home(d,2);
        await Open(d,2);Click(d.Flood.Points[FloodPreparationGame.HighGroundPoint]);await Home(d,3);
        await Open(d,3);var smoke=d.Detector;
        Click(smoke.Cover);await Wait(()=>smoke.Step==1);await Task.Delay(50);
        DragBattery(smoke.OldBattery,smoke.Cover);Check(smoke.Step==1,"Old battery stays until dropped outside the detector");
        DragBattery(smoke.OldBattery,smoke.NewBattery);await Wait(()=>smoke.Step==2);await Task.Delay(50);
        DragBattery(smoke.NewBattery,smoke.NewBattery);Check(smoke.Step==2,"New battery stays until dropped on the detector");
        DragBattery(smoke.NewBattery,smoke.Cover);await Wait(()=>smoke.Step==3);await Task.Delay(50);
        Click(smoke.Cover);await Home(d,4);
        await Open(d,4);await AdjustExtinguisher(d.Extinguisher);await Home(d,5);
        await Open(d,5);for(int i=0;i<5;i++)if(d.Isolation.IsTarget(i))Click(d.Isolation.Points[i]);Click(d.Isolation.CheckButton);
        await Wait(()=>d.Broadcast.Phase==PreparationBroadcast.BroadcastPhase.Review);
    }
    static async Task RunOperatorShift()
    {
        var briefing=Find<PresidentialBriefingManager>();var conversation=Find<OyunAkisiOnur>();var chat=Find<OnurConversationView>();
        var questions=Find<QuickQuestionManager>();var view=Find<QuickQuestionView>();var resources=Find<DispatchResources>();var broadcast=Find<PresidentialBroadcastView>();
        var health=Find<CityHealth>();var minigames=Find<MiniGameWindow>();
        broadcast.charactersPerSecond=0;
        var scenarios=Field<DispatchScenarioData[]>(briefing,"scenarioOrder");var answered=new HashSet<int>();
        var deadline=DateTime.UtcNow.AddMinutes(6);bool keyDown=false;int last=-1;bool firstRequest=false;var capturedPenalties=new HashSet<int>();
        while(SceneManager.GetActiveScene().path==PreparationRun.OperatorScene)
        {
            if(DateTime.UtcNow>deadline)throw new TimeoutException("Full operator shift");
            if(health.IsGameOver)throw new Exception("Operator died during full-flow test");
            if(briefing.CompletedScenarios!=last){last=briefing.CompletedScenarios;File.AppendAllText(Logs+"/checks.txt","PROGRESS: calls "+last+"/7\n");}
            CheckNoMinigame(minigames);
            var radio=Find<Cizirti>();var cues=Find<DispatchSceneAudioCues>();
            if(UnityEngine.Object.FindObjectsByType<AudioSource>().Any(a=>a.isPlaying&&a.clip&&a.clip.name=="Konuşurken"))throw new Exception("Scream audio is active");
            if(cues.ConversationActive)
                Check(radio.CizirtiSesi.isPlaying,"Single conversation loop is playing during active call");
            else Check(!radio.CizirtiSesi.isPlaying,"Conversation loop is off between calls");
            Check(Field<AudioSource>(cues,"music").volume<=.07f&&Field<AudioSource>(cues,"stress").volume<=.055f,"Remaining operator ambience is quiet");
            var curse=Find<DispatchCurseDirector>();
            if(briefing.Phase==PresidentialBriefingManager.ShiftPhase.Scenario&&briefing.CompletedScenarios==1)
                Check(curse.SlownessRemaining==2&&curse.SlownessVisible,"Original second-scenario slowness penalty remains active");
            if(briefing.Phase==PresidentialBriefingManager.ShiftPhase.Scenario&&briefing.CompletedScenarios==4)
                Check(curse.IsFogActive&&curse.FogVisible,"Two wrong answers trigger Bilgi Sisi at its existing scenario event");
            if(briefing.CompletedScenarios>=6)Check(!curse.FogVisible,"Fog badge expires after two scenarios");
            var penaltyTitle=UnityEngine.Object.FindObjectsByType<TMP_Text>().FirstOrDefault(t=>t&&t.text!=null&&t.text.StartsWith("CEZA:"));
            if(penaltyTitle&&capturedPenalties.Add(briefing.CompletedScenarios))
            {Check(!penaltyTitle.text.Contains("LANET"),"Penalty announcement says CEZA");await Capture("focused-penalty-"+briefing.CompletedScenarios);}
            chat.RevealCurrentLine();
            if(questions.Views.Count(v=>v.IsActive)>3)throw new Exception("More than three question cards");
            foreach(var card in questions.Views)
            {
                if(!card.Ready)continue;
                if(briefing.CompletedScenarios==1&&!card.Config.isDisinformation)continue; // exercise one genuine seven-second QQ timeout
                Fits(card.gameObject);
                Check(!WorldRect((RectTransform)card.transform).Overlaps(WorldRect((RectTransform)chat.transform))&&!WorldRect((RectTransform)card.transform).Overlaps(WorldRect((RectTransform)Find<DispatchResourceHud>().transform)),"Question preserves dialogue and resources: "+card.Config.id);
                if(!firstRequest){firstRequest=true;await Capture("07-operator-quick-question");}
                int choice=card.Config.isDisinformation?card.Config.correctChoiceIndex:1;
                var button=choice==0?card.ButtonA:card.ButtonB;
                if(button.interactable)Click(button);
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
            if(connection&&connection.activeSelf&&Keyboard.current!=null)
            {
                EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
                int frame=Time.frameCount;keyDown=!keyDown;
                InputSystem.QueueStateEvent(Keyboard.current,keyDown?new KeyboardState(Key.Space):new KeyboardState());
                await Wait(()=>Time.frameCount>=frame+2,5);
            }
            await Task.Delay(100);
        }
    }
    static void CheckNoMinigame(MiniGameWindow window)
    {
        if(window.BagPanel.activeInHierarchy||window.SafetyPanel.activeInHierarchy||Find<PreparationDirector>()||Find<PreparationBroadcast>()||Find<EarthquakePreparationGame>()||Find<FloodPreparationGame>()||Find<SmokeDetectorPreparationGame>()||Find<ExtinguisherPreparationGame>()||Find<UtilityIsolationPreparationGame>())
            throw new Exception("Preparation mini-game appeared in operator phase");
    }
}
