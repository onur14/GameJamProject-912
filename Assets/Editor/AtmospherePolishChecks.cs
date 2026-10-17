using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Explicit in-memory Play Mode checks; never installs or replaces gameplay content.
public static class AtmospherePolishChecks
{
    const string Folder="Logs/AtmosphereChecks/";
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static readonly List<string> Results=new List<string>();
    static void Check(bool value,string message)
    {
        if(!value)throw new InvalidOperationException(message);
        Results.Add("PASS: "+message);File.WriteAllLines(Folder+"progress.txt",Results);
    }
    static async Task Wait(Func<bool> condition,int seconds=20)
    {
        var end=DateTime.UtcNow.AddSeconds(seconds);
        while(!condition()){if(DateTime.UtcNow>end)throw new TimeoutException("Condition timed out");await Task.Delay(16);}
    }
    static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Private).GetValue(owner);
    static void Invoke(object owner,string name)=>owner.GetType().GetMethod(name,Private).Invoke(owner,null);
    static async Task Shot(string name)
    {
        ScreenCapture.CaptureScreenshot(Folder+name+".png");await Task.Delay(100);
    }
    public static async void President()
    {
        Directory.CreateDirectory(Folder);Results.Clear();Application.runInBackground=true;
        File.WriteAllText(Folder+"president-result.txt","RUNNING");
        try
        {
            var v=Object.FindFirstObjectByType<PresidentialBroadcastView>();
            var phone=Object.FindFirstObjectByType<DispatchCallFlow>();phone.SetInteractionLocked(true);
            var data=AssetDatabase.LoadAssetAtPath<PresidentialBriefingData>("Assets/PresidentialData/Briefing1.asset");
            v.Show(data.heading);await Task.Delay(260);
            var mouth=v.GetComponentInChildren<BroadcastMouthGraphic>();
            Check(mouth && mouth.GetComponent<CanvasRenderer>(),"President mouth owns CanvasRenderer");
            Check(!v.IsSpeaking && mouth.Frame==0,"President idle before first line");
            await Shot("president-idle");
            v.Line(data.dialogueLines[0],v.Stock(Object.FindFirstObjectByType<DispatchResources>()));
            var body=Field<TMP_Text>(v,"body");
            await Wait(()=>v.IsSpeaking && body.maxVisibleCharacters>0 && mouth.Frame==2);
            Check(body.maxVisibleCharacters<body.textInfo.characterCount,"President text reveals progressively");
            Check(mouth.depth>mouth.Portrait.depth && !mouth.canvasRenderer.cull,"Mouth renders above portrait without clipping");
            await Shot("president-speaking-open");
            await Wait(()=>mouth.Frame==1);await Shot("president-speaking-half");
            await Wait(()=>!v.IsSpeaking,15);
            Check(mouth.Frame==0 && body.maxVisibleCharacters==int.MaxValue,"Line completion returns president to original idle");
            await Shot("president-line-complete");
            v.Line(data.dialogueLines[1],v.Stock(Object.FindFirstObjectByType<DispatchResources>()));
            await Wait(()=>v.IsSpeaking && body.maxVisibleCharacters>2);
            string line=body.text;Invoke(v,"Next");
            Check(!v.IsSpeaking && mouth.Frame==0 && body.maxVisibleCharacters==int.MaxValue && body.text==line,"First president skip reveals same line and stops mouth");
            await Task.Delay(350);
            v.Line(data.dialogueLines[2],v.Stock(Object.FindFirstObjectByType<DispatchResources>()));
            await Wait(()=>v.IsSpeaking && mouth.Frame==2);
            Check(v.IsSpeaking,"Next president line resumes speaking");
            v.Hide();Check(!v.IsSpeaking && mouth.Frame==0,"Broadcast hide restores idle");
            phone.SetInteractionLocked(false);
            File.WriteAllLines(Folder+"president-result.txt",Results);
        }
        catch(Exception e){File.WriteAllText(Folder+"president-result.txt",string.Join("\n",Results)+"\nFAIL: "+e);Debug.LogException(e);}
    }
    static bool RaycastReaches(GameObject target,Vector2 position)
    {
        var data=new PointerEventData(EventSystem.current){position=position};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        return hits.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==target;
    }
    static void Click(GameObject target)
    {
        var rect=(RectTransform)target.transform;
        var canvas=target.GetComponentInParent<Canvas>();
        var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
        Check(RaycastReaches(target,point),"Raycast reaches "+target.name);
        var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
        ExecuteEvents.Execute(target,data,ExecuteEvents.pointerClickHandler);
    }
    static async Task Pump(OnurConversationView view,Func<bool> done,int seconds=50)
    {
        var end=DateTime.UtcNow.AddSeconds(seconds);
        while(!done())
        {
            if(DateTime.UtcNow>end)throw new TimeoutException("Conversation transition timed out");
            if(view.IsTyping)view.RevealCurrentLine();
            await Task.Delay(20);
        }
    }
    static async Task Recover(OnurConversationView view)
    {
        await Wait(()=>view.transform.Find("Bağlantı Kurtarma") && view.transform.Find("Bağlantı Kurtarma").gameObject.activeSelf);
        var keyboard=UnityEngine.InputSystem.Keyboard.current;
        Check(keyboard!=null,"Connection recovery keyboard exists");
        for(int i=0;i<16 && view.transform.Find("Bağlantı Kurtarma").gameObject.activeSelf;i++)
        {
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Space));
            int pressedFrame=Time.frameCount;await Wait(()=>Time.frameCount>=pressedFrame+2);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            int releasedFrame=Time.frameCount;await Wait(()=>Time.frameCount>=releasedFrame+2);
        }
        await Wait(()=>!view.transform.Find("Bağlantı Kurtarma").gameObject.activeSelf);
    }
    public static async void Gameplay()
    {
        Directory.CreateDirectory(Folder);Results.Clear();Application.runInBackground=true;
        File.WriteAllText(Folder+"gameplay-result.txt","RUNNING");
        try
        {
            var menu=Object.FindFirstObjectByType<AnaMenü>();Check(menu,"AnaMenu loaded");
            var play=menu.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)=="OyunBaslat"));
            if(!play)play=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)=="OyunBaslat"));
            Click(play.gameObject);
            await Wait(()=>SceneFadeTransition.IsTransitioning);Check(true,"Menu fade begins");
            await Wait(()=>Object.FindFirstObjectByType<DispatchCallFlow>() && !SceneFadeTransition.IsTransitioning);
            var phone=Object.FindFirstObjectByType<DispatchCallFlow>();
            var flow=Object.FindFirstObjectByType<OyunAkisiOnur>();
            var view=Object.FindFirstObjectByType<OnurConversationView>();
            var resources=Object.FindFirstObjectByType<DispatchResources>();
            var hud=Object.FindFirstObjectByType<DispatchResourceHud>();
            var manager=Object.FindFirstObjectByType<PresidentialBriefingManager>();
            var broadcast=Object.FindFirstObjectByType<PresidentialBroadcastView>();
            var feedback=Object.FindFirstObjectByType<DispatchVisualFeedback>();
            Check(resources.Ambulances==5 && resources.Firefighters==5 && resources.Police==5,"All starting resources remain 5");
            Check(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"Exactly one EventSystem in gameplay");
            await Wait(()=>phone.State==DispatchCallFlow.CallState.IncomingCall);
            var pointer=new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(phone.gameObject,pointer,ExecuteEvents.pointerEnterHandler);await Task.Delay(180);
            Check(phone.GetComponentInChildren<PhoneHoverGraphic>().color.a>.5f,"Existing phone hover highlights");
            var ring=Field<CanvasGroup>(phone,"ringIndicator");float first=ring.alpha;bool ringChanges=false;
            for(int i=0;i<12;i++){await Task.Delay(80);if(Mathf.Abs(ring.alpha-first)>.1f || Quaternion.Angle(ring.transform.localRotation,Quaternion.identity)>.1f)ringChanges=true;}
            Check(ringChanges,"Existing ring marks animate");await Shot("phone-ring-hover");
            Click(phone.gameObject);await Wait(()=>phone.State==DispatchCallFlow.CallState.Conversation);
            Check(!phone.GetComponent<Image>().raycastTarget && ring.alpha==0,"Phone answer stops ring and focuses workstation");
            await Wait(()=>view.IsTyping && view.MessageText.maxVisibleCharacters>4);
            Check(view.MessageText.maxVisibleCharacters<view.MessageText.textInfo.characterCount,"Citizen typewriter reveals progressively");
            await Shot("dialogue-typewriter");
            var currentText=view.MessageText;var scroll=view.GetComponentInChildren<ScrollRect>();int rowCount=scroll.content.childCount;
            // Use the actual bubble hit target; no full-screen click catcher is installed.
            var point=RectTransformUtility.WorldToScreenPoint(null,currentText.rectTransform.TransformPoint(currentText.rectTransform.rect.center));
            Check(RaycastReaches(view.gameObject,point),"Bubble click reaches existing conversation handler");
            ExecuteEvents.Execute(view.gameObject,new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            Check(!view.IsTyping && currentText.maxVisibleCharacters==int.MaxValue,"First bubble click instantly reveals current line");
            await Task.Delay(160);Check(scroll.content.childCount==rowCount && view.MessageText==currentText,"Reveal input does not skip a second line");
            var order=Field<DispatchScenarioData[]>(manager,"scenarioOrder");
            for(int i=0;i<8;i++)
            {
                await Pump(view,()=>flow.WaitingForChoice);
                await Task.Delay(100); // Let newly enabled Graphics register before the pointer probe.
                Check(!view.IsTyping && view.ChoiceAButton.gameObject.activeSelf && view.ChoiceBButton.gameObject.activeSelf,"Two choices activate after text: "+order[i].scenarioId);
                if(i==0){await Task.Delay(2100);Check(flow.WaitingForChoice,"No automatic choice after waiting");await Shot("dialogue-choices");}
                bool choiceB=i!=1; // Second call exercises wrong feedback; third call dispatches fire.
                var button=choiceB?view.ChoiceBButton:view.ChoiceAButton;
                if(!button.interactable){choiceB=!choiceB;button=choiceB?view.ChoiceBButton:view.ChoiceAButton;}
                var effect=flow.GetChoiceEffect(choiceB);int a=resources.Ambulances,f=resources.Firefighters,r=resources.Police;
                int pulses=feedback.DecisionPulseCount;
                Click(button.gameObject);
                Check(resources.Ambulances==a+effect.ambulances && resources.Firefighters==f+effect.firefighters && resources.Police==r+effect.rescue,"One original resource cost: "+order[i].scenarioId);
                Check(feedback.DecisionPulseCount==pulses+1,"Existing correct/wrong pulse triggered: "+order[i].scenarioId);
                if(i<3)
                {
                    await Task.Delay(100);
                    int unit=effect.ambulances<0?0:effect.firefighters<0?1:2;
                    Check(hud.DispatchNotice.Contains("SEVK EDİLDİ"),"Dispatch status appears for unit "+unit);
                    Check(hud.Numbers[unit].rectTransform.localScale.x>1,"Existing resource number pulses for unit "+unit);
                    var icon=hud.GetComponentsInChildren<DispatchUnitIcon>().First(x=>x.UnitIndex==unit);
                    Check(icon.transform.localScale.x>1,"Separate unit icon pulse for unit "+unit);
                    await Shot("dispatch-unit-"+unit);
                    await Task.Delay(1200);Check(string.IsNullOrEmpty(hud.DispatchNotice),"Dispatch notice disappears for unit "+unit);
                }
                await Pump(view,()=>view.IsCallEndedVisible);
                Check(!view.ChoiceAButton.gameObject.activeSelf && !view.ChoiceBButton.gameObject.activeSelf,"Call end disables decision controls: "+order[i].scenarioId);
                if(i==0){await Task.Delay(200);await Shot("call-ended");}
                await Recover(view);await Wait(()=>manager.CompletedScenarios==i+1);
                Check(!view.IsCallEndedVisible,"Call end fades and existing recovery completes: "+order[i].scenarioId);
                if((i+1)%2==0)
                {
                    Check(manager.Phase==PresidentialBriefingManager.ShiftPhase.Dialogue,"President occurs at intended call "+(i+1));
                    if(i==1){await Wait(()=>broadcast.IsSpeaking && broadcast.GetComponentInChildren<BroadcastMouthGraphic>().Frame==2);await Shot("president-in-shift");}
                    var next=Field<Button>(broadcast,"advance");
                    while(manager.Phase==PresidentialBriefingManager.ShiftPhase.Dialogue)
                    {Click(next.gameObject);await Task.Delay(340);}
                    Check(manager.Phase==PresidentialBriefingManager.ShiftPhase.Allocation,"President advances to exactly two plans");
                    var plans=Field<Button[]>(broadcast,"offers");Click((plans[0].interactable?plans[0]:plans[1]).gameObject);await Task.Delay(340);
                    while(manager.Phase==PresidentialBriefingManager.ShiftPhase.Confirmation)
                    {Click(next.gameObject);await Task.Delay(340);}
                }
            }
            Check(manager.Phase==PresidentialBriefingManager.ShiftPhase.Final && manager.CompletedScenarios==8,"Full eight-call flow reaches final after fourth broadcast");
            resources.ResetForNewShift();
            for(int i=4;i>=0;i--)
            {
                resources.TrySpend(DispatchResources.ResourceType.Ambulances);await Task.Delay(90);
                if(i<=2)
                {
                    var labels=Field<TMP_Text[]>(hud,"warnings");
                    Check(labels[0].text==(i==0?"! EKİP KALMADI":"! AZ EKİP"),"Existing low/zero warning at stock "+i);
                    if(i==0)await Shot("resource-zero");
                }
            }
            Check(!resources.TrySpend(DispatchResources.ResourceType.Ambulances) && resources.Ambulances==0,"Zero resource never becomes negative");
            Check(string.IsNullOrEmpty(hud.DispatchNotice),"Reserve changes do not create false dispatch notifications");
            var glow=Object.FindObjectsByType<Image>(FindObjectsSortMode.None).First(x=>x.name=="MonitorLuminance");
            float glowAlpha=glow.color.a;await Task.Delay(500);
            Check(!glow.raycastTarget && glow.color.a<=.025f && Mathf.Abs(glow.color.a-glowAlpha)>.00001f,"Monitor ambience is subtle, active and never intercepts input");
            Check(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"Exactly one EventSystem after full flow, including inactive objects");
            File.WriteAllLines(Folder+"gameplay-result.txt",Results);
        }
        catch(Exception e){File.WriteAllText(Folder+"gameplay-result.txt",string.Join("\n",Results)+"\nFAIL: "+e);Debug.LogException(e);}
    }

}
