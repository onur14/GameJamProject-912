using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
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

// Explicit Play Mode checks: never saves gameplay scenes or runtime fixture values.
[InitializeOnLoad]
public static class ShiftReportChecks
{
    const string Queue="Library/EndingReport",Logs="Logs/ShiftReportChecks";
    static readonly BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> errors=new List<string>();
    static double nextPoll;
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Fields).GetValue(target);
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
    static ShiftReportChecks()
    {
        Directory.CreateDirectory(Queue);EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=s=>{
            if(!SessionState.GetBool("ShiftReportChecks",false))return;
            if(s==PlayModeStateChange.EnteredPlayMode)Run();
            if(s==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool("ShiftReportChecks",false);
                EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("ShiftReportPreviousStart",""));
                File.WriteAllText(Queue+"/status.txt",SessionState.GetString("ShiftReportCheckResult","No test result"));
            }
        };
    }
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<nextPoll)return;nextPoll=EditorApplication.timeSinceStartup+.25;
        string path=Queue+"/request.txt";if(!File.Exists(path))return;string action=File.ReadAllText(path).Trim();File.Delete(path);
        try
        {
            if(action=="install"){ShiftReportInstaller.Install();File.WriteAllText(Queue+"/status.txt","Installed in Onur AnaMenu only");}
            else if(action=="check")Start();
            else if(action=="status")File.WriteAllText(Queue+"/status.txt",SceneManager.GetActiveScene().path+" play="+EditorApplication.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty);
            else throw new Exception("Unknown ending command");
        }
        catch(Exception e){File.WriteAllText(Queue+"/status.txt",e.ToString());}
    }
    [MenuItem("Tools/Onur/Test Shift Report (Play Mode)")]
    public static void Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Play Mode is already running");
        if(!EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==ShiftReportFlow.MenuScene))throw new Exception("Onur report menu must be enabled in the build scene list");
        string game=EditorBuildSettings.scenes.First(s=>s.enabled&&s.path.EndsWith("/Onur Test Place Integrated.unity")).path;
        SessionState.SetString("ShiftReportPreviousStart",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetString("ShiftReportGame",game);SessionState.SetBool("ShiftReportChecks",true);
        // Keep the user's open/unsaved editing scenes intact; start Play Mode in the existing build scene.
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(game);EditorApplication.EnterPlaymode();
    }
    static void Check(bool ok,string message)
    {if(!ok)throw new Exception(message);File.AppendAllText(Logs+"/checks.txt","PASS: "+message+"\n");}
    static async Task Wait(Func<bool> test,int seconds=30)
    {var end=DateTime.UtcNow.AddSeconds(seconds);while(!test()){if(DateTime.UtcNow>end)throw new Exception("Timed out waiting for ending test state");await Task.Delay(40);}}
    static void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;var canvas=button.GetComponentInParent<Canvas>();
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,"Actual raycast reaches "+button.name);
        ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
    static async Task Capture(string name)
    {
        var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));gameView.Show();gameView.Repaint();
        await Task.Delay(200);ScreenCapture.CaptureScreenshot(Logs+"/"+name+".png");await Task.Delay(350);
    }
    static void Log(string message,string trace,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    static void Fits(GameObject root)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var text in root.GetComponentsInChildren<TMP_Text>())
        {
            if(string.IsNullOrWhiteSpace(text.text))continue;text.ForceMeshUpdate();
            Check(!text.isTextTruncated,"No truncated text: "+text.name);
            var bounds=text.textBounds;var rect=text.rectTransform.rect;
            Check(bounds.min.x>=rect.xMin-1&&bounds.max.x<=rect.xMax+1&&bounds.min.y>=rect.yMin-1&&bounds.max.y<=rect.yMax+1,"Rendered text stays in its field: "+text.name);
        }
    }
    static async Task GlyphSheet()
    {
        var root=new GameObject("Turkish glyph QA (test only)",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(Image));
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=2000;
        root.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1920,1080);root.GetComponent<Image>().color=new Color(.035f,.065f,.085f);
        string[] names={"GLOBAL PIXEL","GLOBAL MENU","TAHA / YASIN PIXEL","TAHA / YASIN MENU"};
        for(int i=0;i<TurkishFontRepair.FontPaths.Length;i++)
        {
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TurkishFontRepair.FontPaths[i]);
            foreach(char c in TurkishFontRepair.Characters)Check(font.HasCharacter(c,false,false),names[i]+" has native "+c);
            var label=new GameObject(names[i],typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();label.transform.SetParent(root.transform,false);
            label.rectTransform.anchorMin=new Vector2(.045f,.75f-i*.23f);label.rectTransform.anchorMax=new Vector2(.955f,.97f-i*.23f);label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            label.font=font;label.fontSize=44;label.color=new Color(.9f,.95f,.92f);label.raycastTarget=false;
            label.text="<size=25>"+names[i]+"</size>\n9 C Ç ç   Ğ ğ   İ ı   Ö ö   Ş ş   Ü ü\n<b>ÇIKIŞ  /  ACİL ÇAĞRI MERKEZİ</b>";
            label.ForceMeshUpdate();
            foreach(var character in label.textInfo.characterInfo.Take(label.textInfo.characterCount))
                if(TurkishFontRepair.Characters.Contains(character.character))
                    Check(character.fontAsset==font&&character.textElement.unicode==character.character,"Native Turkish glyph rendered: "+character.character);
        }
        await Capture("00-turkish-glyphs");UnityEngine.Object.Destroy(root);await Task.Delay(100);
    }
    static async void Run()
    {
        Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/checks.txt","");errors.Clear();Application.logMessageReceived+=Log;
        try
        {
            Application.runInBackground=true;string gamePath=SessionState.GetString("ShiftReportGame","");
            await GlyphSheet();
            var briefing=Find<PresidentialBriefingManager>();var phone=Find<DispatchCallFlow>();var conversation=Find<OyunAkisiOnur>();var chat=Find<OnurConversationView>();
            var questions=Find<QuickQuestionManager>();var questionView=Find<QuickQuestionView>();var resources=Find<DispatchResources>();var minigames=Find<MiniGameWindow>();var health=Find<CityHealth>();
            int[] initial={resources.Ambulances,resources.Firefighters,resources.RescueTeams};
            for(int i=0;i<8;i++)foreach(bool second in new[]{false,true})
            {var q=questions.GetQuestionForScenario(i,second);q.appearanceDelayMin=q.appearanceDelayMax=.05f;q.timerDuration=1.2f;}
            await Wait(()=>phone.State==DispatchCallFlow.CallState.IncomingCall);phone.AnswerPhone();await Wait(()=>phone.State==DispatchCallFlow.CallState.Conversation);
            minigames.OpenBag();var bag=UnityEngine.Object.FindAnyObjectByType<BagMiniGame>(FindObjectsInactive.Include);
            for(int i=0;i<3;i++){bag.Check();await Task.Delay(350);}Check(bag.IsResolved&&!bag.Succeeded&&minigames.FailedTasks==1,"Failed minigame is measured from its real resolved state");minigames.Close();
            minigames.OpenSafety();var safety=UnityEngine.Object.FindAnyObjectByType<SafetyMiniGame>(FindObjectsInactive.Include);var points=Field<SafetyMiniGame.ControlPoint[]>(safety,"points");
            for(int i=0;i<points.Length;i++){safety.SelectPoint(i);safety.SelectOption(points[i].correctOption);}safety.Check();
            Check(safety.Succeeded&&minigames.SuccessfulTasks==1,"Successful minigame remains independent from failed task");minigames.Close();
            var broadcast=Find<PresidentialBroadcastView>();broadcast.charactersPerSecond=0;
            int[] expected=null;float expectedAverage=0;bool expectedSlow=false;
            briefing.RunCompleted+=()=>{
                expected=new[]{briefing.CompletedScenarios,briefing.CorrectDecisions,resources.Ambulances,resources.Firefighters,resources.RescueTeams,questions.MissedCount,minigames.SuccessfulTasks,minigames.FailedTasks,health.HealthPercent};
                expectedAverage=questions.AverageResponseTime;
                var answered=questions.Results.Where(r=>r.answered&&!r.timedOut&&!r.cancelled).ToArray();expectedSlow=answered.Length>0&&answered.Average(r=>r.responseTime/Mathf.Max(.01f,r.totalDuration))>.8f;
                Check(briefing.ScenarioOutcomes.Count==8&&briefing.ScenarioOutcomes.Count(r=>r.correct)==briefing.CorrectDecisions,"Eight actual scenario completion events retain their outcomes");
                Check(questions.Results.Count==16&&questions.MissedCount==2,"Two deliberately missed requests among sixteen real request results");
            };
            var answeredRevisions=new HashSet<int>();var scenarios=Field<DispatchScenarioData[]>(briefing,"scenarioOrder");
            var deadline=DateTime.UtcNow.AddMinutes(5);bool keyDown=false;int lastCompleted=-1;
            while(SceneManager.GetActiveScene().path!=ShiftReportFlow.MenuScene)
            {
                if(DateTime.UtcNow>deadline)throw new Exception("Full shift did not reach ending");
                if(health&&health.IsGameOver)throw new Exception("Shift ended early from city health");
                if(briefing.CompletedScenarios!=lastCompleted){lastCompleted=briefing.CompletedScenarios;File.AppendAllText(Logs+"/checks.txt","PROGRESS: completed scenarios "+lastCompleted+"\n");}
                chat.RevealCurrentLine();
                if(questionView.Ready&&questions.Results.Count>=2)
                {
                    int choice=questions.CurrentQuestion.isDisinformation?questions.CurrentQuestion.correctChoiceIndex:1;
                    var button=choice==0?questionView.ButtonA:questionView.ButtonB;
                    if(button.interactable)button.onClick.Invoke();
                    else if((choice==0?questionView.ButtonB:questionView.ButtonA).interactable)(choice==0?questionView.ButtonB:questionView.ButtonA).onClick.Invoke();
                }
                if(conversation.WaitingForChoice&&!answeredRevisions.Contains(conversation.ScenarioRevision))
                {
                    var scenario=scenarios[Mathf.Min(briefing.CompletedScenarios,7)];var button=scenario.correctChoiceIsB?chat.ChoiceBButton:chat.ChoiceAButton;
                    if(!button.interactable)button=scenario.correctChoiceIsB?chat.ChoiceAButton:chat.ChoiceBButton;
                    if(button.interactable){answeredRevisions.Add(conversation.ScenarioRevision);button.onClick.Invoke();}
                    else if(chat.AdvanceButton.gameObject.activeSelf){answeredRevisions.Add(conversation.ScenarioRevision);chat.AdvanceButton.onClick.Invoke();}
                }
                if(briefing.Phase==PresidentialBriefingManager.ShiftPhase.Allocation)
                {
                    var offers=Field<Button[]>(broadcast,"offers");var a=Field<ResourceOffer>(briefing,"offerA");var b=Field<ResourceOffer>(briefing,"offerB");
                    int index=offers[0].interactable?0:1;
                    if(offers[0].interactable&&offers[1].interactable)
                    {
                        int rankA=Mathf.Min(resources.Ambulances+a.ambulances,Mathf.Min(resources.Firefighters+a.firefighters,resources.RescueTeams+a.thirdTeam));
                        int rankB=Mathf.Min(resources.Ambulances+b.ambulances,Mathf.Min(resources.Firefighters+b.firefighters,resources.RescueTeams+b.thirdTeam));
                        if(rankB>rankA)index=1;
                    }
                    if(offers[index].interactable)offers[index].onClick.Invoke();
                }
                else if(briefing.Phase==PresidentialBriefingManager.ShiftPhase.Dialogue||briefing.Phase==PresidentialBriefingManager.ShiftPhase.Confirmation)
                {var advance=Field<Button>(broadcast,"advance");if(advance.gameObject.activeInHierarchy&&advance.interactable)advance.onClick.Invoke();}
                var connection=Field<GameObject>(chat,"connectionOverlay");
                if(connection&&connection.activeSelf&&Keyboard.current!=null){keyDown=!keyDown;InputSystem.QueueStateEvent(Keyboard.current,keyDown?new KeyboardState(Key.Space):new KeyboardState());}
                await Task.Delay(110);
            }
            if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            await Wait(()=>!SceneFadeTransition.IsTransitioning&&Find<ShiftReportView>()&&Find<ShiftReportView>().IsVisible);
            var report=Find<ShiftReportView>();var data=report.Data;
            Check(expected!=null&&data.Completed==expected[0]&&data.Correct==expected[1],"Final sequence automatically loads the report in Onur AnaMenu");
            Check(data.Ambulances==expected[2]&&data.Firefighters==expected[3]&&data.RescueTeams==expected[4],"Report retains exact final shared resource values");
            Check(data.Missed==expected[5]&&Mathf.Abs(data.AverageSeconds-expectedAverage)<.0001f,"Missed count and answered-only average survive scene load");
            Check(data.SuccessfulTasks==expected[6]&&data.FailedTasks==expected[7]&&data.Health==expected[8],"Actual minigame and health data retained");
            int empty=(data.Ambulances==0?1:0)+(data.Firefighters==0?1:0)+(data.RescueTeams==0?1:0);
            int score=Mathf.Clamp(100-(8-data.Correct)*10-data.Missed*5-data.FailedTasks*8-empty*3-(expectedSlow?5:0),0,100);
            Check(data.Score==score&&data.Grade==ShiftReportData.GradeFor(score),"Score agrees with independently computed penalties");
            foreach(int value in new[]{100,85,84,70,69,50,49,0})Check(ShiftReportData.GradeFor(value)==(value>=85?"A":value>=70?"B":value>=50?"C":"D"),"Grade boundary "+value);
            foreach(var text in report.GetComponentsInChildren<TMP_Text>())
            {text.ForceMeshUpdate();Check(!text.isTextTruncated,"No clipped report text: "+text.name);}
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1,"Exactly one EventSystem in report scene");
            File.WriteAllText(Logs+"/values.txt",$"Correct={data.Correct}/{data.Completed}\nMissed={data.Missed}\nAverage={data.AverageSeconds}\nResources={data.Ambulances}/{data.Firefighters}/{data.RescueTeams}\nMinigames={data.SuccessfulTasks} successful/{data.FailedTasks} failed\nScore={data.Score} Grade={data.Grade}\nStatus={data.OperationStatus}");
            Check(report.GetComponentsInChildren<DispatchInsetFrame>(true).Length==1,"One subtle inset frame in ending report");
            var reportEvents=Field<TMP_Text>(report,"events");reportEvents.ForceMeshUpdate();
            Check(reportEvents.textInfo.lineCount<=4&&data.ExplainOutcome(true).Length<=4,"Win explanation remains within four readable lines");
            foreach(string name in new[]{"Rapor Başlığı","Vardiya","Performans Başlığı","Kaynak Başlığı","Değerlendirme Başlığı","Vardiya Notu","Operasyon Durumu","Olay Başlığı"})
                Check(report.GetComponentsInChildren<TMP_Text>().Single(t=>t.name==name).alignment==TextAlignmentOptions.Top,"Ending alignment centered: "+name);
            await Capture("01-shift-report");Click(report.MenuButton);
            Check(!report.IsVisible&&ShiftReportFlow.Pending==null&&GameObject.Find("Canvas")!=null,"Main Menu hides report and restores existing normal menu");await Capture("02-normal-menu");
            // Re-present the same real snapshot to exercise Retry as well, without replaying/fabricating run data.
            report.Show(data);Click(report.ReplayButton);
            await Wait(()=>SceneManager.GetActiveScene().path==gamePath&&!SceneFadeTransition.IsTransitioning);
            resources=Find<DispatchResources>();briefing=Find<PresidentialBriefingManager>();questions=Find<QuickQuestionManager>();minigames=Find<MiniGameWindow>();
            Check(resources.Ambulances==initial[0]&&resources.Firefighters==initial[1]&&resources.RescueTeams==initial[2],"Retry reloads intended initial stocks");
            Check(briefing.CompletedScenarios==0&&briefing.CorrectDecisions==0&&briefing.ScenarioOutcomes.Count==0&&questions.Results.Count==0,"Retry clears scenario and Quick Question results");
            Check(minigames.SuccessfulTasks==0&&minigames.FailedTasks==0&&ShiftReportFlow.Pending==null,"Retry clears mini-game results and report snapshot");
            var fresh=ShiftReportData.Capture(briefing,questions,minigames,Find<CityHealth>(),gamePath);Check(!fresh.HasResponseTimes&&fresh.AverageSeconds==0,"No unanswered request is treated as a zero-second answer");
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1,"Retry keeps one EventSystem");await Capture("03-retry");
            // New visual checks use the ordinary fresh call, preserving authored gameplay scene data.
            phone=Find<DispatchCallFlow>();conversation=Find<OyunAkisiOnur>();chat=Find<OnurConversationView>();health=Find<CityHealth>();
            await Wait(()=>phone.State==DispatchCallFlow.CallState.IncomingCall);phone.AnswerPhone();await Wait(()=>phone.State==DispatchCallFlow.CallState.Conversation);await Task.Delay(950);
            var caller=Find<CallerInfoView>();await Capture("04-caller-information");Fits(caller.gameObject);
            foreach(var scenario in scenarios){caller.Show(scenario);await Task.Delay(50);Fits(caller.gameObject);}
            caller.Show(scenarios[0]);
            minigames.OpenBag();await Capture("07-bag-turkish");Fits(minigames.BagPanel);minigames.Close();
            minigames.OpenSafety();await Capture("08-safety-turkish");Fits(minigames.SafetyPanel);minigames.Close();
            questions.CancelActiveQuestion();
            for(int attempt=0;attempt<3;attempt++)
            {
                if(attempt>0)conversation.BeginScenario(scenarios[0]);questions.CancelActiveQuestion();
                var decisionDeadline=DateTime.UtcNow.AddSeconds(40);
                while(!conversation.WaitingForChoice){if(DateTime.UtcNow>decisionDeadline)throw new Exception("Wrong-choice test timed out");chat.RevealCurrentLine();await Task.Delay(100);}
                var wrong=scenarios[0].correctChoiceIsB?chat.ChoiceAButton:chat.ChoiceBButton;wrong.onClick.Invoke();await Task.Delay(300);
            }
            await Wait(()=>health.IsGameOver);await Task.Delay(600);
            var failure=Find<RunResultView>();var failurePanel=Field<GameObject>(failure,"panel");var failureSummary=Field<TMP_Text>(failure,"summaryLabel");
            Check(health.WrongDecisionCount==3&&failureSummary.text.Contains("3 hatalı ana yönlendirme"),"Loss summary includes the actual fatal third wrong decision");
            Check(failureSummary.text.Split('\n').Length>=2&&failureSummary.text.Split('\n').Length<=4,"Loss explanation has two to four concise lines");
            Fits(failurePanel);Check(failurePanel.GetComponentsInChildren<DispatchInsetFrame>(true).Length==1,"One inset frame in loss panel");await Capture("05-loss-summary");
            Click(Field<Button>(failure,"menuButton"));await Wait(()=>SceneManager.GetActiveScene().path.EndsWith("/AnaMenu.unity"));await Task.Delay(200);await Capture("06-exit-menu-turkish");
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1,"Failure menu keeps a single EventSystem");
            Check(errors.Count==0,"No new Play Mode errors: "+string.Join("; ",errors));File.AppendAllText(Logs+"/checks.txt","COMPLETE\n");SessionState.SetString("ShiftReportCheckResult","PASS - Logs/ShiftReportChecks");
        }
        catch(Exception e){File.AppendAllText(Logs+"/checks.txt","FAIL: "+e+"\n");SessionState.SetString("ShiftReportCheckResult",e.ToString());}
        finally{Application.logMessageReceived-=Log;EditorApplication.ExitPlaymode();}
    }
}
