using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// File queue retained from the original integration. Play Mode tests survive domain reload.
[InitializeOnLoad]
public static class QuickQuestionBridge
{
    const string Folder="Library/QuickQuestionBridge";
    static double nextPoll;
    [Serializable] class Request { public string id,action; }
    [Serializable] class Response { public string id,status,message,activeScene;public bool isPlaying; }
    static QuickQuestionBridge()
    {
        Directory.CreateDirectory(Folder);EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=state=>{
            if(!SessionState.GetBool("QQRunning",false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)ExecuteTests();
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool("QQRunning",false);
                EditorApplication.delayCall+=()=>{
                    string path=SessionState.GetString("QQRestore","");if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);
                    Write(SessionState.GetString("QQRequest",""),SessionState.GetString("QQStatus","error"),SessionState.GetString("QQReport",""));
                };
            }
        };
    }
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<nextPoll)return;nextPoll=EditorApplication.timeSinceStartup+.25;
        string path=Folder+"/request.json";if(!File.Exists(path))return;
        Request r=null;
        try
        {
            r=JsonUtility.FromJson<Request>(File.ReadAllText(path));File.Delete(path);
            if(r.action=="status")Write(r.id,"ok","");
            else if(r.action=="stop_play"){EditorApplication.ExitPlaymode();Write(r.id,"ok","Stopping Play Mode");}
            else if(r.action=="install"){QuickQuestionInstaller.InstallOrUpdate();Write(r.id,"success","Installed");}
            else if(r.action=="run_tests")StartTests(r.id);
            else throw new InvalidOperationException("Unknown action");
        }
        catch(Exception e){Write(r?.id,"error",e.ToString());}
    }
    public static void StartTests(string id)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("QQRunning",false))throw new InvalidOperationException("Exit Play Mode before running checks.");
        var scene=EditorSceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Save scene edits before running checks.");
        SessionState.SetString("QQRestore",scene.path);SessionState.SetString("QQRequest",id);SessionState.SetBool("QQRunning",true);
        EditorSceneManager.OpenScene("Assets/Working Folder/Taha and Yasin/AnaMenu.unity");EditorApplication.EnterPlaymode();
    }
    static async void ExecuteTests()
    {
        try {SessionState.SetString("QQReport",await QuickQuestionChecks.RunAllTests());SessionState.SetString("QQStatus","success");}
        catch(Exception e){SessionState.SetString("QQReport",e.ToString());SessionState.SetString("QQStatus","error");}
        finally {EditorApplication.ExitPlaymode();}
    }
    static void Write(string id,string status,string message)=>File.WriteAllText(Folder+"/response.json",JsonUtility.ToJson(new Response{id=id,status=status,message=message,isPlaying=EditorApplication.isPlaying,activeScene=EditorSceneManager.GetActiveScene().path},true));
}
