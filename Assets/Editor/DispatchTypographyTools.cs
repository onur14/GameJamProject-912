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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Opt-in typography installation and Game View checks. No gameplay data is saved by checks.
[InitializeOnLoad]
public static class DispatchTypographyTools
{
    const string Game="Assets/Working Folder/Onur/Onur Test Place Integrated.unity";
    const string Prefabs="Assets/Working Folder/Onur/MiniGames/Prefabs/";
    const string Materials="Assets/Art/UI/Typography";
    const string Queue="Library/TypographyPolish";
    const string Logs="Logs/TypographyPolish";
    static double nextPoll;
    static readonly BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> errors=new List<string>();
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Fields).GetValue(o);
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
    static DispatchTypographyTools()
    {
        Directory.CreateDirectory(Queue);EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=s=>{
            if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("TypographyChecks",false))RunChecks();
            if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("TypographyChecks",false))
            {SessionState.SetBool("TypographyChecks",false);File.WriteAllText(Queue+"/status.txt",SessionState.GetString("TypographyResult","No result"));}
        };
    }
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<nextPoll)return;nextPoll=EditorApplication.timeSinceStartup+.25;
        string file=Queue+"/request.txt";if(!File.Exists(file))return;
        string action=File.ReadAllText(file).Trim();File.Delete(file);
        try
        {
            if(action=="install")Install();
            else if(action=="check")StartChecks();
            else if(action=="status")File.WriteAllText(Queue+"/status.txt",SceneManager.GetActiveScene().path+" play="+EditorApplication.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty);
            else throw new Exception("Unknown typography command");
        }
        catch(Exception e){File.WriteAllText(Queue+"/status.txt",e.ToString());}
    }
    static bool Heading(TMP_Text t)=>t.name.Contains("Başlı")||t.name=="Sonuç"||t.name=="Seçilen Tesisat";
    static Material Preset(TMP_FontAsset font,bool heading)
    {
        string path=Materials+"/"+font.name.Replace(" ","_")+(heading?"_Heading.mat":"_Body.mat");
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=DispatchTextStyle.CreateMaterial(font,heading);AssetDatabase.CreateAsset(material,path);}
        material.SetFloat("_FaceDilate", DispatchTextStyle.FaceExpansion(font,heading));EditorUtility.SetDirty(material);
        return material;
    }
    static void Style(TMP_Text text,bool mini)
    {
        bool heading=Heading(text);text.fontSharedMaterial=Preset(text.font,heading);text.extraPadding=true;
        text.textWrappingMode=TextWrappingModes.Normal;
        if(mini)
        {
            text.color=heading?new Color(.98f,.99f,.97f):new Color(.94f,.96f,.94f);
            if(text.name=="Sonuç")text.color=new Color(.99f,.88f,.65f);
            text.lineSpacing=heading?1:2;
            if(text.GetComponentInParent<Button>())
            {text.alignment=TextAlignmentOptions.Midline;text.margin=new Vector4(6,3,6,3);}
            float size=text.enableAutoSizing?text.fontSizeMax:text.fontSize;
            text.enableAutoSizing=true;text.fontSizeMax=size;text.fontSizeMin=size*.92f;
            text.overflowMode=TextOverflowModes.Truncate;
        }
        else if(text.GetComponentInParent<CallerInfoView>(true))
        {
            text.color=heading?new Color(.86f,.91f,.91f):new Color(.86f,.90f,.87f);
            // Give longer event names two lines within the same sidebar, keeping the outer layout.
            if(text.name=="Olay")text.rectTransform.anchorMin=new Vector2(text.rectTransform.anchorMin.x,.415f);
            if(text.name=="Durum")text.rectTransform.anchorMax=new Vector2(text.rectTransform.anchorMax.x,.39f);
            EditorUtility.SetDirty(text.rectTransform);
        }
        EditorUtility.SetDirty(text);
    }
    [MenuItem("Tools/Onur/Typography/Apply Text Polish")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
        Directory.CreateDirectory(Materials);AssetDatabase.Refresh();
        foreach(string name in new[]{"BagMiniGame","SafetyMiniGame","MiniGameWindow"})
        {
            string path=Prefabs+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    // Child minigame prefabs own their typography, without window-level overrides.
                    if(name=="MiniGameWindow"&&(t.GetComponentInParent<BagMiniGame>(true)||t.GetComponentInParent<SafetyMiniGame>(true)))continue;
                    if(t.name=="Tik"||t.name=="Kalan Hak")continue; // Existing symbol rendering / hearts.
                    Style(t,true);
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        if(SceneManager.GetActiveScene().path!=Game)EditorSceneManager.OpenScene(Game);
        var caller=Find<CallerInfoView>();foreach(var t in caller.GetComponentsInChildren<TMP_Text>(true))Style(t,false);
        EditorSceneManager.MarkSceneDirty(caller.gameObject.scene);EditorSceneManager.SaveScene(caller.gameObject.scene);
        AssetDatabase.SaveAssets();File.WriteAllText(Queue+"/status.txt","Installed typography presets and TMP properties only");
    }
    [MenuItem("Tools/Onur/Typography/Check Game View")]
    public static void StartChecks()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
        if(SceneManager.GetActiveScene().path!=Game)EditorSceneManager.OpenScene(Game);
        SessionState.SetBool("TypographyChecks",true);EditorApplication.EnterPlaymode();
    }
    static async Task Wait(Func<bool> test,int seconds=30)
    {var end=DateTime.UtcNow.AddSeconds(seconds);while(!test()){if(DateTime.UtcNow>end)throw new Exception("Check timeout");await Task.Delay(30);}}
    static void Check(bool ok,string message)
    {if(!ok)throw new Exception(message);File.AppendAllText(Logs+"/checks.txt","PASS: "+message+"\n");}
    static async Task Capture(string name)
    {
        var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));gameView.Show();gameView.Repaint();
        await Task.Delay(200);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Logs+"/"+name+".png");await Task.Delay(250);
    }
    static void TextFits(GameObject root)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var t in root.GetComponentsInChildren<TMP_Text>())
        {
            if(string.IsNullOrWhiteSpace(t.text)||t.maxVisibleCharacters==0)continue;
            t.ForceMeshUpdate();string path=t.transform.parent.name+"/"+t.name;
            Check(!t.isTextTruncated,"No truncated text: "+path);
            // Preferred values use the base size; inspect the mesh actually rendered after auto sizing.
            var bounds=t.textBounds;var rect=t.rectTransform.rect;
            Check(bounds.min.y>=rect.yMin-1 && bounds.max.y<=rect.yMax+1,"Rendered text fits height: "+path+" text="+t.text);
            Check(bounds.min.x>=rect.xMin-1 && bounds.max.x<=rect.xMax+1,"Rendered text fits width: "+path);
        }
    }
    static void Log(string message,string trace,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    static void TurkishGlyphs(TMP_Text text)
    {
        text.ForceMeshUpdate();
        foreach(var c in text.textInfo.characterInfo.Take(text.textInfo.characterCount))
            if(!char.IsWhiteSpace(c.character))
                Check(c.textElement!=null && c.textElement.unicode==c.character,"Turkish glyph "+c.character+" in "+(c.fontAsset?c.fontAsset.name:"missing font"));
    }
    static async void RunChecks()
    {
        Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/checks.txt","");errors.Clear();Application.logMessageReceived+=Log;
        try
        {
            Application.runInBackground=true;var phone=Find<DispatchCallFlow>();
            await Wait(()=>phone.State==DispatchCallFlow.CallState.IncomingCall);phone.AnswerPhone();
            await Wait(()=>phone.State==DispatchCallFlow.CallState.Conversation);
            var view=Find<OnurConversationView>();var qq=Find<QuickQuestionView>();var manager=Find<QuickQuestionManager>();var resources=Find<DispatchResources>();
            await Wait(()=>qq.Ready);await Capture("01-dialogue-quick-question");TextFits(qq.gameObject);TextFits(Find<CallerInfoView>().gameObject);TextFits(Find<DispatchResourceHud>().gameObject);
            int count=resources.Ambulances;qq.ButtonA.onClick.Invoke();Check(resources.Ambulances==count-1,"Quick Question still spends one real unit");
            await Capture("02-resource-feedback");await Wait(()=>!qq.IsActive);
            var window=Find<MiniGameWindow>();window.OpenBag();await Task.Delay(150);var bag=Find<BagMiniGame>();var safety=Find<SafetyMiniGame>();
            TextFits(bag.gameObject);await Capture("03-bag");
            int attempts=bag.AttemptsLeft;bag.Check();Check(bag.AttemptsLeft==attempts-1,"Bag check preserves attempt logic");await Task.Delay(250);TextFits(bag.gameObject);await Capture("04-bag-retry");
            foreach(var item in bag.GetComponentsInChildren<BagDraggableItem>())if(Field<BagDropZone>(item,"correctArea")!=null)ExecuteEvents.Execute(item.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            await Task.Delay(350);bag.Check();Check(bag.Succeeded,"Bag completion still works");TextFits(bag.gameObject);await Capture("05-bag-complete");
            window.Close();window.OpenSafety();await Task.Delay(150);TextFits(safety.gameObject);await Capture("06-safety");
            var points=Field<SafetyMiniGame.ControlPoint[]>(safety,"points");
            for(int i=0;i<points.Length;i++){safety.SelectPoint(i);safety.SelectOption(1-points[i].correctOption);}safety.Check();
            Check(!safety.Succeeded&&safety.AttemptsLeft==2,"Safety feedback preserves attempt logic");await Task.Delay(250);TextFits(safety.gameObject);await Capture("07-safety-retry");
            for(int i=0;i<points.Length;i++){safety.SelectPoint(i);safety.SelectOption(points[i].correctOption);TextFits(safety.gameObject);await Capture("safety-point-"+i);}
            await Task.Delay(350);safety.Check();Check(safety.Succeeded,"Safety completion still works");TextFits(safety.gameObject);await Capture("08-safety-complete");
            // Render the Turkish specimen through each of the actual UI font assets.
            string specimen="Ç ç  Ğ ğ  İ ı  Ö ö  Ş ş  Ü ü";var instruction=safety.GetComponentsInChildren<TMP_Text>().First(t=>t.name=="Yönerge");string original=instruction.text;
            instruction.text=specimen;await Capture("09-turkish-minigame");TextFits(instruction.gameObject);TurkishGlyphs(instruction);instruction.text=original;
            window.Close();await Wait(()=>!qq.IsActive,30);var main=Find<OyunAkisiOnur>();
            var deadline=DateTime.UtcNow.AddSeconds(40);while(!main.WaitingForChoice){if(DateTime.UtcNow>deadline)throw new Exception("Main choices timeout");view.RevealCurrentLine();await Task.Delay(100);}
            // Let the existing first-decision note and second request finish before checking the sidebar.
            var note=Find<CityHealth>().transform.Find("İlk Karar Bilgi Notu");
            await Wait(()=>!qq.IsActive && (!note || !note.gameObject.activeSelf),40);
            await Capture("10-dialogue-choices");TextFits(view.ChoiceAButton.gameObject);TextFits(view.ChoiceBButton.gameObject);
            var scenarios=Field<DispatchScenarioData[]>(Find<PresidentialBriefingManager>(),"scenarioOrder");
            foreach(var scenario in scenarios){view.SetChoices(scenario.choiceA,scenario.choiceB);TextFits(view.ChoiceAButton.gameObject);TextFits(view.ChoiceBButton.gameObject);Find<CallerInfoView>().Show(scenario);TextFits(Find<CallerInfoView>().gameObject);}
            var caller=Find<CallerInfoView>();caller.Show(scenarios[0]);view.SetChoices(scenarios[0].choiceA,scenarios[0].choiceB);
            var summary=Field<TMP_Text>(caller,"summaryLabel");string saved=summary.text;summary.text=specimen;await Capture("11-turkish-pixel");TurkishGlyphs(summary);summary.text=saved;
            resources.TryApply(2-resources.Ambulances,0,0);await Task.Delay(100);
            Check(Find<DispatchResourceHud>().GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("AZ EKİP")),"Existing low-resource warning remains active");await Capture("12-resource-warning");
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1,"One EventSystem retained");Check(errors.Count==0,"No new runtime errors: "+string.Join("; ",errors));
            File.AppendAllText(Logs+"/checks.txt","COMPLETE\n");SessionState.SetString("TypographyResult","PASS - Logs/TypographyPolish");
        }
        catch(Exception e){File.AppendAllText(Logs+"/checks.txt","FAIL: "+e+"\n");SessionState.SetString("TypographyResult",e.ToString());}
        finally{Application.logMessageReceived-=Log;EditorApplication.ExitPlaymode();}
    }
}
