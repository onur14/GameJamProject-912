using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TahaYasinTabletInstaller
{
    public const string Menu="Assets/Working Folder/Taha and Yasin/AnaMenu.unity";
    [MenuItem("GameJam/Taha Yasin/Tableti Kur ve Menüyü Bağla")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Önce Play Mode'dan çıkın.");
        var active=EditorSceneManager.GetActiveScene();
        if(active.isDirty)throw new InvalidOperationException("Önce açık sahneyi kaydedin.");
        var game=EditorSceneManager.OpenScene(TahaYasinDispatchSetup.Game);
        var w=UnityEngine.Object.FindFirstObjectByType<MiniGameWindow>(FindObjectsInactive.Include);
        if(!w)throw new InvalidOperationException("Mevcut minigame penceresi bulunamadı.");
        var tablet=w.GetComponent<DispatchTablet>();if(!tablet)tablet=w.gameObject.AddComponent<DispatchTablet>();
        var ts=new SerializedObject(tablet);ts.FindProperty("window").objectReferenceValue=w;
        ts.FindProperty("font").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Art/UI/Fonts/VCR_OSD_MONO_1.001 SDF.asset");ts.ApplyModifiedProperties();
        var ws=new SerializedObject(w);ws.FindProperty("tablet").objectReferenceValue=tablet;ws.FindProperty("actionHealthPoints").intValue=2;ws.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(w);
        var result=UnityEngine.Object.FindFirstObjectByType<RunResultView>(FindObjectsInactive.Include);
        if(result){var rs=new SerializedObject(result);rs.FindProperty("menuSceneName").stringValue=Menu;rs.ApplyModifiedProperties();}
        EditorSceneManager.MarkSceneDirty(game);EditorSceneManager.SaveScene(game);
        var menu=EditorSceneManager.OpenScene(Menu);
        var menuController=UnityEngine.Object.FindFirstObjectByType<AnaMenü>(FindObjectsInactive.Include);
        if(!menuController)throw new InvalidOperationException("AnaMenu oyun başlatma bileşeni eksik.");
        var ms=new SerializedObject(menuController);ms.FindProperty("gameplayScene").stringValue=TahaYasinDispatchSetup.Game;ms.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(menu);EditorSceneManager.SaveScene(menu);
        var scenes=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(Menu,true),new EditorBuildSettingsScene(TahaYasinDispatchSetup.Game,true)};
        foreach(var s in EditorBuildSettings.scenes)if(s.path!=Menu && s.path!=TahaYasinDispatchSetup.Game)
            scenes.Add(new EditorBuildSettingsScene(s.path,false));
        EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
    }
}
