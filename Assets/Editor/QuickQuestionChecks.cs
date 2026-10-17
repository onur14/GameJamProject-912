using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

// Checks fail closed. Runtime fixtures never save scene/config changes.
public static class QuickQuestionChecks
{
    const string Folder="Logs/QuickQuestionChecks";
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Flags).GetValue(target);
    static readonly List<string> errors=new List<string>();
    static void Check(bool value,string text){if(!value)throw new Exception("FAIL: "+text);File.AppendAllText(Folder+"/result.txt","PASS: "+text+"\n");}
    static async Task Wait(Func<bool> condition,float seconds=20)
    {var end=DateTime.UtcNow.AddSeconds(seconds);while(!condition()){if(DateTime.UtcNow>end)throw new TimeoutException("Condition timed out");await Task.Delay(20);}}
    static void Click(RectTransform rect)
    {
        Canvas.ForceUpdateCanvases();var canvas=rect.GetComponentInParent<Canvas>();
        var p=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);
        Check(hits.Count>0,"raycast hits "+rect.name);
        var target=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        Check(target==rect.gameObject,"topmost interactive target is "+rect.name);
        ExecuteEvents.Execute(target,p,ExecuteEvents.pointerClickHandler);
    }
    static async Task Capture(string name)
    {
        var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));gameView.Show();gameView.Repaint();
        ScreenCapture.CaptureScreenshot(Folder+"/"+name+".png");await Task.Delay(350);
    }
    static void Log(string m,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)errors.Add(m);}
    public static async Task<string> RunAllTests()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","");errors.Clear();Application.logMessageReceived+=Log;
        Application.runInBackground=true;
        try
        {
            await Task.Delay(300);Check(Application.isPlaying,"Play Mode active");
            var play=UnityEngine.Object.FindObjectsByType<Button>().First(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)=="OyunBaslat"));
            Click((RectTransform)play.transform);await Wait(()=>SceneManager.GetActiveScene().name=="SampleScene"&&!SceneFadeTransition.IsTransitioning);
            var phone=Find<DispatchCallFlow>();await Wait(()=>phone.State==DispatchCallFlow.CallState.IncomingCall);
            Click((RectTransform)phone.transform);await Wait(()=>phone.State==DispatchCallFlow.CallState.Conversation);
            var manager=Find<QuickQuestionManager>();var view=Find<QuickQuestionView>();var main=Find<OnurConversationView>();
            var conversation=Find<OyunAkisiOnur>();var resources=Find<DispatchResources>();var hud=Find<DispatchResourceHud>();var briefing=Find<PresidentialBriefingManager>();
            Check(manager&&view&&manager.isActiveAndEnabled,"hidden view retains enabled scenario manager");
            Check(resources.Ambulances==5&&resources.Firefighters==5&&resources.Police==5,"real starting stocks are five");
            await Wait(()=>view.Ready);Check(manager.CurrentQuestion.id=="QQ-01","first scenario automatically triggers its request");
            float timer=view.RemainingFraction;int chars=main.MessageText.maxVisibleCharacters;string text=main.MessageText.text;
            await Task.Delay(500);Check(view.RemainingFraction<timer,"visible timer counts down");
            Check(main.MessageText.text!=text || main.MessageText.maxVisibleCharacters!=chars,"main dialogue progresses alongside timer");
            await Capture("01-concurrent");
            timer=view.RemainingFraction;Click((RectTransform)main.transform);await Task.Delay(100);
            Check(view.IsActive&&view.RemainingFraction<timer&&!main.IsPaused,"main dialogue accepts input while timer continues");
            int count=manager.Results.Count;Click((RectTransform)view.ButtonA.transform);view.ButtonA.onClick.Invoke();view.ButtonA.onClick.Invoke();
            Check(resources.Ambulances==4&&hud.Numbers[0].text=="4","send updates shared stock and right HUD exactly once");
            Check(manager.Results.Count==count+1&&manager.Results.Last().appliedEffect.ambulances==-1,"one result stores actual debit");
            Check(manager.Results.Last().responseTime>0&&manager.Results.Last().responseTime<=10,"response time recorded from actionable timer");
            await Capture("02-sent");await Wait(()=>!view.IsActive);
            var q=manager.GetQuestionForScenario(1).Clone();manager.TriggerQuestion(q);await Wait(()=>view.Ready);
            int fire=resources.Firefighters;Click((RectTransform)view.ButtonB.transform);Check(resources.Firefighters==fire,"reject costs no resource");await Wait(()=>!view.IsActive);
            q=manager.GetQuestionForScenario(0).Clone();q.id="TIMEOUT-FIXTURE";q.timerDuration=1;manager.TriggerQuestion(q);await Wait(()=>view.Ready);
            int before=resources.Ambulances;await Wait(()=>!manager.IsQuestionActive,4);
            Check(manager.Results.Last().timedOut&&resources.Ambulances==before,"timeout is missed with no resource cost");await Capture("03-timeout");await Wait(()=>!view.IsActive);
            resources.TryApply(-resources.Ambulances,0,0);manager.TriggerQuestion(q);await Wait(()=>view.Ready);
            count=manager.Results.Count;Check(!view.ButtonA.interactable,"zero-stock send is disabled");view.ButtonA.onClick.Invoke();
            Check(resources.Ambulances==0&&manager.Results.Count==count&&manager.IsQuestionActive,"forced disabled callback cannot record success or negative stock");
            Click((RectTransform)view.ButtonB.transform);await Wait(()=>!view.IsActive);
            resources.ResetForNewShift();resources.TryApply(-2,0,0);q.timerDuration=10;manager.TriggerQuestion(q);await Wait(()=>view.Ready);Click((RectTransform)view.ButtonA.transform);
            Check(resources.Ambulances==2&&hud.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("AZ EKİP")),"secondary dispatch triggers existing low-stock warning");
            await Wait(()=>!view.IsActive);resources.ResetForNewShift();
            var order=Field<DispatchScenarioData[]>(briefing,"scenarioOrder");
            for(int i=0;i<8;i++)
            {
                var config=manager.GetQuestionForScenario(i);Check(config.targetScenarioId==order[i].scenarioId,"scenario ID mapping "+i);
                config.appearanceDelayMin=config.appearanceDelayMax=.1f;
                conversation.BeginScenario(order[i]);await Wait(()=>view.Ready);
                Check(manager.CurrentQuestion.id==config.id,"automatic trigger for "+config.id);
                var original=manager.CurrentQuestion;manager.TriggerQuestion(manager.GetQuestionForScenario((i+1)%8));Check(manager.CurrentQuestion==original,"second trigger cannot replace active question");
                await Capture("scenario-"+(i+1));Click((RectTransform)view.ButtonB.transform);await Wait(()=>!view.IsActive);
            }
            // Exercise both main choice and secondary choice concurrently at a real decision point.
            conversation.BeginScenario(order[0]);await Wait(()=>view.Ready);manager.CancelActiveQuestion();
            var limit=DateTime.UtcNow.AddSeconds(30);
            while(!conversation.WaitingForChoice){if(DateTime.UtcNow>limit)throw new TimeoutException("main choices");if(main.IsTyping)main.RevealCurrentLine();await Task.Delay(200);}
            manager.TriggerQuestion(manager.GetQuestionForScenario(0));await Wait(()=>view.Ready);
            timer=view.RemainingFraction;int stock=resources.Ambulances;
            Click((RectTransform)main.ChoiceBButton.transform);await Task.Delay(100);
            Check(resources.Ambulances==stock-1&&view.Ready&&view.RemainingFraction<timer,"main decision spends once while secondary remains interactive");
            Click((RectTransform)view.ButtonB.transform);await Wait(()=>!view.IsActive);
            // Multi-unit return clamps actual applied quantity, including partial headroom.
            q=manager.GetQuestionForScenario(4).Clone();q.choiceAEffect=new DispatchResourceEffect{ambulances=10};resources.ResetForNewShift();resources.TryApply(-1,0,0);
            manager.TriggerQuestion(q);await Wait(()=>view.Ready);Click((RectTransform)view.ButtonA.transform);
            Check(resources.Ambulances==5&&manager.Results.Last().appliedEffect.ambulances==1,"return respects ceiling and records actual +1");await Wait(()=>!view.IsActive);
            manager.TriggerQuestion(q);await Wait(()=>view.Ready);Check(!view.ButtonA.interactable,"return at ceiling disabled");manager.CancelActiveQuestion();
            briefing.ResetNewShift();Check(manager.Results.Count==0,"new shift resets results and trigger history");await Wait(()=>view.Ready);
            conversation.EndGame();Check(!view.IsActive&&!manager.IsQuestionActive&&manager.Results.Last().cancelled,"game over cancels outstanding request without debit");
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1,"exactly one EventSystem");
            Check(UnityEngine.Object.FindObjectsByType<AudioListener>().Length==1,"exactly one AudioListener");
            Check(errors.Count==0,"no new Play Mode errors: "+string.Join(";",errors));File.AppendAllText(Folder+"/result.txt","COMPLETE\n");
            return File.ReadAllText(Folder+"/result.txt");
        }
        catch(Exception e){File.AppendAllText(Folder+"/result.txt",e+"\n");throw;}
        finally{Application.logMessageReceived-=Log;}
    }
}
