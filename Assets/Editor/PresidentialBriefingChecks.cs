using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Explicit Play Mode integration checks. Fixtures exist in memory only and never become game content.
public static class PresidentialBriefingChecks
{
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool valid, string description)
    { if (!valid) throw new InvalidOperationException("FAIL: " + description); }
    private static void Advance(PresidentialBriefingManager manager)
    { typeof(PresidentialBriefingManager).GetMethod("Advance",Private).Invoke(manager,null); }
    private static async Task WaitFor(Func<bool> condition)
    {
        var deadline=DateTime.UtcNow.AddSeconds(12);
        while(!condition())
        {
            if(DateTime.UtcNow>deadline)throw new TimeoutException("Play Mode check timed out.");
            // Exercise the player-controlled presentation rather than restoring auto-advance.
            var dialogue=UnityEngine.Object.FindFirstObjectByType<OnurConversationView>();
            if(dialogue && dialogue.WaitingForAdvance)dialogue.AdvanceButton.onClick.Invoke();
            await Task.Delay(10);
        }
    }
    public static async void StartRun()
    {
        System.IO.Directory.CreateDirectory("Logs/PresidentialChecks");
        try { System.IO.File.WriteAllText("Logs/PresidentialChecks/test-result.txt",await Run()); }
        catch(Exception e) { System.IO.File.WriteAllText("Logs/PresidentialChecks/test-result.txt",e.ToString());Debug.LogException(e); }
    }
    public static async Task<string> Run()
    {
        if (!Application.isPlaying) return "Enter Play Mode first.";
        var manager=UnityEngine.Object.FindFirstObjectByType<PresidentialBriefingManager>();
        var resources=UnityEngine.Object.FindFirstObjectByType<DispatchResources>();
        var flow=UnityEngine.Object.FindFirstObjectByType<OyunAkisiOnur>();
        var view=UnityEngine.Object.FindFirstObjectByType<PresidentialBroadcastView>();
        Check(manager && resources && flow && view,"scene references");
        Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"one EventSystem");
        Check(view.presidentPortrait,"existing president sprite");
        var orderField=typeof(PresidentialBriefingManager).GetField("scenarioOrder",Private);
        var savedOrder=(DispatchScenarioData[])orderField.GetValue(manager);
        var fixtures=new DispatchScenarioData[8];
        var result=new System.Collections.Generic.List<string>();
        float originalTimeScale=Time.timeScale;Time.timeScale=100;Application.runInBackground=true;
        try
        {
            for(int i=0;i<8;i++)
            {
                var data=ScriptableObject.CreateInstance<DispatchScenarioData>();
                data.scenarioId="TEST_ONLY_"+i;data.opening=new DispatchScenarioData.Line[0];
                data.outcomeA=data.outcomeB=new DispatchScenarioData.Line[0];
                data.choiceA="TEST A";data.choiceB="TEST B";data.resourceB=DispatchResources.ResourceType.Ambulances;
                fixtures[i]=data;
            }
            orderField.SetValue(manager,fixtures);
            manager.ResetNewShift();
            int a=resources.Ambulances,f=resources.Firefighters,r=resources.Police;
            int changes=0; Action listener=()=>changes++;resources.Changed+=listener;
            Check(!resources.TryApply(-a-1,5,5),"reject unaffordable atomic allocation");
            Check(resources.Ambulances==a && resources.Firefighters==f && resources.Police==r && changes==0,"no partial commit or event");
            Check(resources.TryApply(1,-1,2),"valid allocation");
            Check(changes==1 && resources.Ambulances==a+1 && resources.Firefighters==f-1 && resources.Police==r+2,"single resource event");
            foreach(string field in new[]{"ambulancesText","firefightersText","policeText"})
            {
                var label=(TMP_Text)typeof(DispatchResources).GetField(field,Private).GetValue(resources);
                int expected=field=="ambulancesText"?resources.Ambulances:field=="firefightersText"?resources.Firefighters:resources.Police;
                Check(label && label.text==expected.ToString(),"existing resource HUD refresh");
            }
            resources.Changed-=listener; resources.ResetForNewShift();
            result.Add("Atomic allocation, insufficient stock, one change notification: PASS");
            var definitions=(PresidentialBriefingData[])typeof(PresidentialBriefingManager).GetField("briefings",Private).GetValue(manager);
            for(int i=0;i<8;i++)
            {
                int revision=flow.ScenarioRevision;
                int before=resources.Ambulances;
                flow.Reddedildi();
                await WaitFor(()=>manager.CompletedScenarios==i+1);
                Check(manager.CompletedScenarios==i+1,"scenario completion "+(i+1));
                Check(resources.Ambulances==before-1,"citizen call uses same stock");
                manager.OnScenarioCompleted(revision);
                Check(manager.CompletedScenarios==i+1,"duplicate completion ignored");
                if((i+1)%2!=0) continue;
                Check(manager.Phase==PresidentialBriefingManager.ShiftPhase.Dialogue,"briefing at "+(i+1));
                manager.SelectOffer(0);
                Check(manager.Phase==PresidentialBriefingManager.ShiftPhase.Dialogue,"early offer blocked");
                for(int j=0;j<definitions[i/2].dialogueLines.Length;j++)Advance(manager);
                Check(manager.Phase==PresidentialBriefingManager.ShiftPhase.Allocation,"two-offer phase");
                var buttons=view.GetComponentsInChildren<Button>(false).Where(b=>b.name=="Plan A"||b.name=="Plan B").ToArray();
                Check(buttons.Length==2,"exactly two visible offers");
                var plan=definitions[i/2].planA;
                int expectedA=resources.Ambulances+plan.ambulances,expectedF=resources.Firefighters+plan.firefighters,expectedR=resources.Police+plan.thirdTeam;
                manager.SelectOffer(0);manager.SelectOffer(0);manager.SelectOffer(1);
                Check(resources.Ambulances==expectedA && resources.Firefighters==expectedF && resources.Police==expectedR,"allocation once, correct delta");
                Check(manager.Decisions.Count==(i+1)/2,"one decision per briefing");
                Advance(manager);
            }
            Check(manager.Phase==PresidentialBriefingManager.ShiftPhase.Final && manager.CompletedScenarios==8,"final after fourth allocation, no ninth scenario");
            Check(manager.CorrectDecisions==8,"citizen outcome assessment");
            Check(resources.Ambulances==8 && resources.Firefighters==13 && resources.Police==11,"final reserve includes fourth allocation");
            result.Add("1 > 2 > B1 > 3 > 4 > B2 > 5 > 6 > B3 > 7 > 8 > B4 > Final: PASS");
            result.Add("Duplicate completion, early selection, repeated selection, resource persistence, final reserve: PASS");
            resources.TryApply(-resources.Ambulances,-resources.Firefighters,-resources.Police);
            Check(PresidentialBriefingManager.EmergencyOffer(resources,true).IsValid(resources),"zero-stock emergency A");
            Check(PresidentialBriefingManager.EmergencyOffer(resources,false).IsValid(resources),"zero-stock emergency B");
            manager.ResetNewShift();
            flow.Reddedildi();await WaitFor(()=>manager.CompletedScenarios==1);
            flow.Reddedildi();await WaitFor(()=>manager.CompletedScenarios==2);
            resources.TryApply(-resources.Ambulances,-resources.Firefighters,-resources.Police);
            for(int j=0;j<definitions[0].dialogueLines.Length;j++)Advance(manager);
            Check(view.GetComponentsInChildren<Button>(false).Count(b=>(b.name=="Plan A"||b.name=="Plan B")&&b.interactable)==2,"both invalid plans replaced with two usable alternatives");
            manager.SelectOffer(0);manager.SelectOffer(0);
            Check(resources.Ambulances==2 && resources.Firefighters==0 && resources.Police==0,"emergency choice committed once");
            manager.ResetNewShift();
            Check(manager.CompletedScenarios==0 && manager.Decisions.Count==0 && manager.CorrectDecisions==0,"new game resets progress");
            Check(resources.Ambulances==10 && resources.Firefighters==10 && resources.Police==10,"new game resets stock");
            result.Add("Zero-stock fallback, emergency double-click guard, new shift reset: PASS");
            orderField.SetValue(manager,savedOrder);manager.ResetNewShift();
            var waiting=typeof(OyunAkisiOnur).GetField("secimBekleniyor",Private);
            for(int i=0;i<8;i++)
            {
                Check(savedOrder[i] && savedOrder[i].readyForPlay,"authored scenario assigned");
                await WaitFor(()=>(bool)waiting.GetValue(flow));
                if(savedOrder[i].correctChoiceIsB)flow.Reddedildi();else flow.KabulEdildi();
                await WaitFor(()=>manager.CompletedScenarios==i+1);
                if((i+1)%2==0)
                {
                    for(int j=0;j<definitions[i/2].dialogueLines.Length;j++)Advance(manager);
                    int choice=definitions[i/2].planA.IsValid(resources)?0:1;
                    manager.SelectOffer(choice);Advance(manager);
                }
            }
            Check(manager.CompletedScenarios==8 && manager.CorrectDecisions==8 && manager.Phase==PresidentialBriefingManager.ShiftPhase.Final,"authored eight-scenario playthrough");
            result.Add("Approved real citizen data: D-01 > D-02 > Y-02 > S-01 > T-01 > F-01 > K-01 > K-02, eight correct outcomes and final: PASS");
            await Task.Delay(100);ScreenCapture.CaptureScreenshot("Logs/PresidentialChecks/final.png");await Task.Delay(100);
            int advances=0;Action listenerAdvance=()=>advances++;view.AdvanceRequested+=listenerAdvance;
            view.charactersPerSecond=0;view.Line("Türkçe: çğıöşü ÇĞİÖŞÜ",view.Stock(resources));
            var next=view.GetComponentsInChildren<Button>(false).First(b=>b.name=="Devam");
            await Task.Delay(350);next.onClick.Invoke();next.onClick.Invoke();
            Check(advances==1,"double click advances one line");
            view.charactersPerSecond=1;view.Line("Uzun bilgilendirme satırı",view.Stock(resources));
            await Task.Delay(350);next.onClick.Invoke();Check(advances==1,"first typewriter click only completes line");
            await Task.Delay(350);next.onClick.Invoke();Check(advances==2,"next click advances completed line");
            view.AdvanceRequested-=listenerAdvance;
            resources.TryApply(1-resources.Ambulances,1-resources.Firefighters,-resources.Police);
            view.Offers(definitions[0].planA,definitions[0].planB,resources);
            Check(view.GetComponentsInChildren<Button>(false).Count(b=>(b.name=="Plan A"||b.name=="Plan B")&&b.interactable)==1,"one unaffordable offer disabled");
            result.Add("HUD synchronization, actual UI double-click, typewriter completion, one disabled offer: PASS");
            return string.Join("\n",result);
        }
        finally
        {
            orderField.SetValue(manager,savedOrder);manager.ResetNewShift();
            Time.timeScale=originalTimeScale;
            foreach(var data in fixtures)if(data)UnityEngine.Object.Destroy(data);
        }
    }
    public static string Preview(int briefingNumber, bool offers)
    {
        if(!Application.isPlaying)return "Enter Play Mode first.";
        var manager=UnityEngine.Object.FindFirstObjectByType<PresidentialBriefingManager>();
        var flow=UnityEngine.Object.FindFirstObjectByType<OyunAkisiOnur>();flow.StopAllCoroutines();
        var resources=UnityEngine.Object.FindFirstObjectByType<DispatchResources>();
        var view=UnityEngine.Object.FindFirstObjectByType<PresidentialBroadcastView>();
        var phone=UnityEngine.Object.FindFirstObjectByType<DispatchCallFlow>();phone.SetInteractionLocked(true);
        var data=PresidentialBriefingInstaller.CreateData()[briefingNumber-1];
        view.charactersPerSecond=0;view.Show(data.heading);
        if(offers)view.Offers(data.planA,data.planB,resources);
        else view.Line(data.dialogueLines[1],view.Stock(resources));
        return "Preview only; no allocation applied.";
    }
}
