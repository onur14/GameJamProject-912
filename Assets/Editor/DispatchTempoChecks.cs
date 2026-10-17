using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public static class DispatchTempoChecks
{
    private static void Check(bool valid,string message){if(!valid)throw new InvalidOperationException("FAIL: "+message);}
    private static async Task WaitFor(Func<bool> condition,bool advance=true)
    {
        var until=DateTime.UtcNow.AddSeconds(15);
        while(!condition())
        {
            if(DateTime.UtcNow>until)throw new TimeoutException("Tempo check timed out.");
            var v=UnityEngine.Object.FindFirstObjectByType<OnurConversationView>();
            if(advance&&v&&v.WaitingForAdvance)v.AdvanceButton.onClick.Invoke();
            await Task.Delay(20);
        }
    }
    public static async void StartRun()
    {
        System.IO.Directory.CreateDirectory("Logs/TempoChecks");
        try {System.IO.File.WriteAllText("Logs/TempoChecks/result.txt",await Run());}
        catch(Exception e){System.IO.File.WriteAllText("Logs/TempoChecks/result.txt",e.ToString());Debug.LogException(e);}
    }
    public static async void StartAll()
    {
        System.IO.Directory.CreateDirectory("Logs/TempoChecks");
        try
        {
            string tempo=await Run();
            string regression=await PresidentialBriefingChecks.Run();
            System.IO.File.WriteAllText("Logs/TempoChecks/all-results.txt",tempo+"\n\nBRIEFING REGRESSION\n"+regression);
        }
        catch(Exception e){System.IO.File.WriteAllText("Logs/TempoChecks/all-results.txt",e.ToString());Debug.LogException(e);}
    }
    private static async Task<string> Run()
    {
        Check(Application.isPlaying,"Play Mode required");Application.runInBackground=true;
        var manager=UnityEngine.Object.FindFirstObjectByType<PresidentialBriefingManager>();
        var flow=UnityEngine.Object.FindFirstObjectByType<OyunAkisiOnur>();
        var view=UnityEngine.Object.FindFirstObjectByType<OnurConversationView>();
        var r=UnityEngine.Object.FindFirstObjectByType<DispatchResources>();
        var hud=UnityEngine.Object.FindFirstObjectByType<DispatchResourceHud>();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var order=(DispatchScenarioData[])typeof(PresidentialBriefingManager).GetField("scenarioOrder",flags).GetValue(manager);
        var speed=typeof(OnurConversationView).GetField("charactersPerSecond",flags);
        float savedSpeed=(float)speed.GetValue(view),savedScale=Time.timeScale;
        var result=new System.Collections.Generic.List<string>();
        try
        {
            speed.SetValue(view,0f);manager.ResetNewShift();
            string first=view.MessageText.text;await Task.Delay(650);
            Check(view.MessageText.text==first&&view.WaitingForAdvance,"unread text remains until click");
            view.AdvanceButton.onClick.Invoke();await Task.Delay(80);string next=view.MessageText.text;view.AdvanceButton.onClick.Invoke();await Task.Delay(30);
            Check(next!=first&&view.MessageText.text==next,"double click advances only one citizen line");
            speed.SetValue(view,1f);flow.BeginScenario(order[0]);await Task.Delay(250);
            first=view.MessageText.text;Check(view.IsTyping,"optional typewriter active");
            view.AdvanceButton.onClick.Invoke();Check(!view.IsTyping&&view.MessageText.text==first,"first typewriter click only reveals current line");
            await Task.Delay(250);view.AdvanceButton.onClick.Invoke();await Task.Delay(80);Check(view.MessageText.text!=first,"next click moves to next line");
            speed.SetValue(view,0f);manager.ResetNewShift();
            float start=Time.realtimeSinceStartup;await WaitFor(()=>flow.WaitingForChoice);
            float decisionTime=Time.realtimeSinceStartup-start;
            Check(decisionTime<30,"first decision has no imposed long wait");
            Check(view.EffectA.text.Contains("Kurtarma -1")&&view.EffectB.text.Contains("Ambulans -1"),"actual choice effects displayed");
            Check(view.EffectB.text.Contains("10 → 9"),"predicted resource amount");
            int before=r.Ambulances;await Task.Delay(250);
            view.ChoiceBButton.onClick.Invoke();view.ChoiceBButton.onClick.Invoke();
            Check(r.Ambulances==before-1,"actual citizen UI spends exactly once");
            Check(hud.Numbers[0].text==r.Ambulances.ToString(),"HUD updates on citizen dispatch");
            r.TryApply(2,1,1);
            Check(hud.Numbers[0].text==r.Ambulances.ToString()&&hud.Numbers[1].text==r.Firefighters.ToString()&&hud.Numbers[2].text==r.Police.ToString(),"HUD updates on allocation API");
            result.Add("Player-controlled text, actual citizen double click, optional typewriter, source-derived effects, one dispatch and live HUD: PASS");
            result.Add("First-decision UI latency with fast test clicks: "+decisionTime.ToString("F2")+"s (not a human reading-time claim).");
            manager.ResetNewShift();r.TryApply(-r.Ambulances,-r.Firefighters,-r.Police);
            await WaitFor(()=>flow.WaitingForChoice);
            Check(!view.ChoiceAButton.interactable&&!view.ChoiceBButton.interactable,"both unavailable options disabled");
            Check(view.AdvanceButton.gameObject.activeSelf,"explicit no-dispatch continuation visible");
            Time.timeScale=100;await Task.Delay(250);Time.timeScale=savedScale;
            Check(manager.CompletedScenarios==0&&flow.WaitingForChoice,"no automatic selection or timeout");
            Check(hud.GetComponentsInChildren<TMPro.TMP_Text>().Count(t=>t.text=="EKİP KALMADI")==3,"zero resource warnings");
            view.AdvanceButton.onClick.Invoke();view.AdvanceButton.onClick.Invoke();
            Check(manager.CompletedScenarios==1&&r.Ambulances==0&&r.Firefighters==0&&r.Police==0,"no-stock continuation completes once, no hidden grants");
            var information=order.First(s=>s.scenarioId=="K-01");flow.BeginScenario(information);
            await WaitFor(()=>flow.WaitingForChoice);await Task.Delay(250);
            Check(view.ChoiceBButton.interactable&&view.EffectB.text.Contains("Kaynak etkisi: 0"),"information option free and available at zero stock");
            view.ChoiceBButton.onClick.Invoke();Check(r.Ambulances==0&&r.Firefighters==0&&r.Police==0,"information action consumes no teams");
            result.Add("No deadline, insufficient stock, explicit no-dispatch continuation, zero warnings and free information option: PASS");
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length==1,"single EventSystem");
            return string.Join("\n",result);
        }
        finally {speed.SetValue(view,savedSpeed);Time.timeScale=savedScale;manager.ResetNewShift();}
    }
    public static string LayoutStatus()
    {
        var view=UnityEngine.Object.FindFirstObjectByType<OnurConversationView>();
        var hud=UnityEngine.Object.FindFirstObjectByType<DispatchResourceHud>();
        Func<RectTransform,Rect> screenRect=rt=>{var c=new Vector3[4];rt.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y);};
        Rect dialogue=screenRect((RectTransform)view.transform),resources=screenRect((RectTransform)hud.transform);
        var options=view.GetComponentsInChildren<TMPro.TMP_Text>(false);
        var overflow=options.Where(t=>t.gameObject.name!="Satır İçeriği"&&t.isTextOverflowing).Select(t=>t.name).ToArray();
        Check(!dialogue.Overlaps(resources),"dialogue and resource panel do not overlap");
        Check(dialogue.xMin>=0&&dialogue.yMin>=0&&dialogue.xMax<=Screen.width&&dialogue.yMax<=Screen.height,"dialogue remains in screen");
        return $"{Screen.width}x{Screen.height}; dialogue {dialogue}; HUD {resources}; overlap false; scroll needed: {view.HasScrollableText}; overflowing text: {string.Join(", ",overflow)}";
    }
}
