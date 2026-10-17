using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class PreparationInstaller
{
    private const string WindowPrefab="Assets/Working Folder/Taha and Yasin/Onur/MiniGames/Prefabs/MiniGameWindow.prefab";
    [MenuItem("GameJam/Preparation/Install Preparation Phase")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Önce Play Mode'dan çıkın.");
        var previous=SceneManager.GetActiveScene();
        // Additive creation preserves every open editing scene, including unsaved changes.
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var host=new GameObject("Preparation Phase");var director=host.AddComponent<PreparationDirector>();
            var windowRoot=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WindowPrefab),scene);
            windowRoot.name="Existing Emergency Bag";
            var window=windowRoot.GetComponent<MiniGameWindow>();
            var ws=new SerializedObject(window);ws.FindProperty("preparation").objectReferenceValue=director;ws.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(window);
            var ds=new SerializedObject(director);ds.FindProperty("bagWindow").objectReferenceValue=window;
            ds.FindProperty("font").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Working Folder/Taha and Yasin/Fonts/TY_VCR_OSD_MONO_SDF.asset");ds.ApplyModifiedPropertiesWithoutUndo();
            var camera=new GameObject("Preparation Camera",typeof(Camera));camera.GetComponent<Camera>().backgroundColor=PreparationUI.Navy;camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;camera.tag="MainCamera";
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            EditorSceneManager.SaveScene(scene,PreparationRun.ScenePath);
        }
        finally{EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        DisableOperatorEntrances(PreparationRun.OperatorScene);
        var builds=EditorBuildSettings.scenes.ToList();
        int index=builds.FindIndex(s=>s.path==PreparationRun.ScenePath);
        if(index<0)builds.Insert(1,new EditorBuildSettingsScene(PreparationRun.ScenePath,true));else builds[index].enabled=true;
        EditorBuildSettings.scenes=builds.ToArray();AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Logs/Preparation");File.WriteAllText("Logs/Preparation/install.txt","Preparation scene created; existing bag prefab reused; operator entrances disabled; build scene enabled.");
    }
    private static void DisableOperatorEntrances(string path)
    {
        var already=SceneManager.GetSceneByPath(path);bool opened=!already.IsValid()||!already.isLoaded;
        // Runtime entry guards also cover an open unsaved operator scene. Never save its unrelated edits.
        if(!opened && already.isDirty)return;
        var scene=opened?EditorSceneManager.OpenScene(path,OpenSceneMode.Additive):already;
        try
        {
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var tablet in root.GetComponentsInChildren<DispatchTablet>(true)){tablet.enabled=false;EditorUtility.SetDirty(tablet);}
                foreach(var window in root.GetComponentsInChildren<MiniGameWindow>(true))
                {
                    if(window.BagEntryButton)window.BagEntryButton.gameObject.SetActive(false);
                    if(window.SafetyEntryButton)window.SafetyEntryButton.gameObject.SetActive(false);
                    window.BagPanel.SetActive(false);window.SafetyPanel.SetActive(false);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
