using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

// Opt-in Play Mode checks. Never auto-runs or changes saved scene/asset data.
public static class OnurMiniGameChecks
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Get<T>(object o, string name) => (T)o.GetType().GetField(name, Fields).GetValue(o);
    private static void Assert(bool ok, string message)
    { if (!ok) throw new InvalidOperationException(message); Debug.Log("MINIGAME PASS: " + message); }
    private static void Health(CityHealth health, int value)
    {
        typeof(CityHealth).GetProperty("HealthPercent").GetSetMethod(true).Invoke(health,new object[]{value});
        typeof(CityHealth).GetMethod("Refresh",Fields).Invoke(health,null);
    }
    private static void Drag(BagDraggableItem item, BagDropZone zone)
    {
        var rect=(RectTransform)item.transform;
        var canvas=item.GetComponentInParent<Canvas>();
        var camera=canvas.rootCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.rootCanvas.worldCamera;
        var pointer=new PointerEventData(EventSystem.current) {
            pointerDrag=item.gameObject,
            position=RectTransformUtility.WorldToScreenPoint(camera,rect.position)
        };
        ExecuteEvents.Execute(item.gameObject,pointer,ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(item.gameObject,pointer,ExecuteEvents.beginDragHandler);
        pointer.position=RectTransformUtility.WorldToScreenPoint(camera,zone.ContentRoot.position);
        ExecuteEvents.Execute(item.gameObject,pointer,ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(zone.gameObject,pointer,ExecuteEvents.dropHandler);
        ExecuteEvents.Execute(item.gameObject,pointer,ExecuteEvents.endDragHandler);
        // Some UI modules also deliver click after release; a completed drag must not toggle back.
        ExecuteEvents.Execute(item.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        Assert(item.CurrentArea==zone,"drag/drop reaches requested area");
    }
    [MenuItem("Tools/Onur/Test Minigames (Play Mode)")]
    public static async void Run()
    {
        if(!Application.isPlaying){Debug.LogWarning("Önce minigame sahnesinde Play Mode'u açın. Bu kontrol mevcut Play Mode minigame ilerlemesini sıfırlar.");return;}
        var window=Object.FindFirstObjectByType<MiniGameWindow>();
        if(!window){Debug.LogWarning("Mevcut minigame sahnesini açın: Onur Test Place Integrated.");return;}
        var health=Get<CityHealth>(window,"cityHealth");
        int originalHealth=health.HealthPercent;
        try
        {
            Assert(!health.IsGameOver,"main game is running");
            window.ResetForNewSession();Health(health,80);
            window.OpenBag();await Task.Delay(100);
            var bag=Get<BagMiniGame>(window,"bagGame");
            var safety=Get<SafetyMiniGame>(window,"safetyGame");
            window.OpenSafety();Assert(bag.gameObject.activeInHierarchy&&!safety.gameObject.activeInHierarchy,"only one game can open");
            var phone=Get<DispatchCallFlow>(window,"phone");var view=Get<OnurConversationView>(window,"conversationView");
            Assert(phone.IsPaused&&view.IsPaused,"background phone/conversation paused");
            Assert(bag.AttemptsLeft==3,"bag starts with three attempts");
            bag.Check();Assert(bag.AttemptsLeft==2,"only failed check consumes one attempt");
            window.Close();window.OpenBag();Assert(bag.AttemptsLeft==2,"close/reopen preserves attempts");
            var items=Get<BagDraggableItem[]>(bag,"items");
            var needed=items.First(x=>Get<BagDropZone>(x,"correctArea")!=null);
            var outside=Get<BagDropZone>(needed,"startingArea");var inside=Get<BagDropZone>(needed,"correctArea");
            Drag(needed,inside);Drag(needed,outside);Assert(bag.AttemptsLeft==2,"dragging in and out does not consume attempts");
            foreach(var item in items)Drag(item,Get<BagDropZone>(item,"correctArea")??Get<BagDropZone>(item,"startingArea"));
            Assert(items.All(x=>x.IsCorrectlyPlaced),"authored bag data drives correctness");
            await Task.Delay(350);bag.Check();Assert(bag.IsResolved&&health.HealthPercent==90,"bag grants ten percentage points once");
            bag.Check();window.Close();window.OpenBag();bag.Check();Assert(health.HealthPercent==90&&bag.IsResolved,"bag reward/result survive reopen without duplicate reward");
            bag.ResetForNewSession();
            for(int i=0;i<3;i++){await Task.Delay(350);bag.Check();}
            Assert(bag.IsResolved&&bag.AttemptsLeft==0&&!health.IsGameOver&&health.HealthPercent==90,"bag failure is local and grants no reward");
            window.Close();window.OpenBag();Assert(bag.IsResolved&&bag.AttemptsLeft==0,"failure survives reopen");
            window.Close();window.OpenSafety();await Task.Delay(100);
            Assert(safety.AttemptsLeft==3,"safety starts with three attempts");
            safety.Check();Assert(safety.AttemptsLeft==2,"incomplete safety check costs one attempt");
            var points=Get<SafetyMiniGame.ControlPoint[]>(safety,"points");
            Assert(points.All(x=>x.hotspotButton.gameObject.activeInHierarchy),"all safety hotspots are actually visible in hierarchy");
            for(int i=0;i<points.Length;i++)
            {
                safety.SelectPoint(i);var first=points[i].stateImage.sprite;
                safety.SelectPoint(i);Assert(points[i].stateImage.sprite!=first,"point toggles between authored PNG states");
                Assert(points[i].completionTick.text=="✓"&&safety.AttemptsLeft==2,"tick records interaction, not correctness or attempt loss");
                var mark=points[i].completionTick.GetComponentInChildren<MiniGameCheckGraphic>();
                Assert(mark!=null&&mark.isActiveAndEnabled&&mark.canvasRenderer!=null,"completion check renders without a font glyph");
                while(points[i].selectedOption!=points[i].correctOption)safety.SelectPoint(i);
            }
            int[] selected=points.Select(x=>x.selectedOption).ToArray();
            window.Close();window.OpenSafety();Assert(points.Select(x=>x.selectedOption).SequenceEqual(selected)&&safety.AttemptsLeft==2,"safety choices and attempts survive reopen");
            await Task.Delay(350);safety.Check();Assert(safety.IsResolved&&health.HealthPercent==100,"safety grants ten points and health caps at 100");
            safety.Check();window.Close();window.OpenSafety();safety.Check();Assert(health.HealthPercent==100&&safety.IsResolved,"safety reward/result cannot repeat");
            safety.ResetForNewSession();
            for(int i=0;i<3;i++){await Task.Delay(350);safety.Check();}
            Assert(safety.AttemptsLeft==0&&safety.IsResolved&&!health.IsGameOver&&health.HealthPercent==100,"safety failure is local with no reward");
            Debug.Log("MINIGAME CHECKS COMPLETE. Pointer events were dispatched directly; separately verify physical mouse dragging and Game view appearance.");
        }
        catch(Exception e){Debug.LogException(e);}
        finally {window.ResetForNewSession();Health(health,originalHealth);}
    }
}
