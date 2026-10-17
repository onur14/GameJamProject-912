using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TahaYasinDispatchSetup
{
    public const string Root = "Assets/Working Folder/Taha and Yasin/Onur";
    public const string Game = Root + "/Onur Test Place Integrated.unity";
    public const string Menu = "Assets/Working Folder/Taha and Yasin/AnaMenu.unity";

    [MenuItem("GameJam/Taha Yasin/Çalışma Sahnesini Aç")]
    public static void OpenGame()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Önce Play Mode'dan çıkın.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(Game);
    }

    [MenuItem("GameJam/Taha Yasin/Mevcut Sahne Bağlantılarını Kaydet")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().path != Game)
            throw new InvalidOperationException("Taha Yasin oyun sahnesini düzenleme modunda açın.");
        var manager = UnityEngine.Object.FindFirstObjectByType<QuickQuestionManager>(FindObjectsInactive.Include);
        if (!manager) throw new InvalidOperationException("Hızlı soru sistemi sahnede eksik.");
        manager.InitializeDefaultQuestionsIfNeeded();
        manager.ConfigureVerificationRewards();
        var ms = new SerializedObject(manager);
        ms.FindProperty("cityHealth").objectReferenceValue = UnityEngine.Object.FindFirstObjectByType<CityHealth>(FindObjectsInactive.Include);
        ms.FindProperty("visualFeedback").objectReferenceValue = UnityEngine.Object.FindFirstObjectByType<DispatchVisualFeedback>(FindObjectsInactive.Include);
        ms.ApplyModifiedProperties(); EditorUtility.SetDirty(manager);
        var view = UnityEngine.Object.FindFirstObjectByType<OnurConversationView>(FindObjectsInactive.Include);
        var vs = new SerializedObject(view); vs.FindProperty("charactersPerSecond").floatValue = 40;
        vs.ApplyModifiedProperties(); EditorUtility.SetDirty(view);
        var result = UnityEngine.Object.FindFirstObjectByType<RunResultView>(FindObjectsInactive.Include);
        var rs = new SerializedObject(result); rs.FindProperty("menuSceneName").stringValue = Menu; rs.ApplyModifiedProperties();

        var caller = UnityEngine.Object.FindFirstObjectByType<CallerInfoView>(FindObjectsInactive.Include);
        var phone = UnityEngine.Object.FindFirstObjectByType<DispatchCallFlow>(FindObjectsInactive.Include);
        var rect = (RectTransform)caller.transform;
        rect.SetParent(phone.GetComponentInParent<Canvas>().rootCanvas.transform, false);
        rect.anchorMin = new Vector2(.02f,.07f); rect.anchorMax = new Vector2(.35f,.335f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        if (!caller.GetComponent<CanvasGroup>()) caller.gameObject.AddComponent<CanvasGroup>();
        var image = caller.GetComponent<Image>();
        if (image) { image.color = new Color(.04f,.09f,.12f,.97f); image.raycastTarget = false; }
        var cs = new SerializedObject(caller);
        string[] fields = { "nameLabel", "locationLabel", "emergencyLabel", "summaryLabel" };
        float[] mins = { .66f,.48f,.31f,.035f }, maxs = { .85f,.66f,.48f,.31f };
        var labels = new HashSet<TMP_Text>();
        for (int i=0;i<4;i++)
        {
            var text = (TMP_Text)cs.FindProperty(fields[i]).objectReferenceValue; labels.Add(text);
            Place(text, mins[i], maxs[i]); text.fontSize = i==0 ? 21 : 16;
            text.enableAutoSizing = true; text.fontSizeMin = i==0 ? 18 : 13; text.fontSizeMax = i==0 ? 21 : 16;
            text.color = i==0 ? new Color(.4f,.96f,.82f) : new Color(.83f,.9f,.87f);
            EditorUtility.SetDirty(text);
        }
        foreach (var text in caller.GetComponentsInChildren<TMP_Text>(true))
            if (!labels.Contains(text)) { Place(text,.88f,.985f); text.text="AKTİF ÇAĞRI"; text.fontSize=12; text.color=new Color(.49f,.69f,.72f); EditorUtility.SetDirty(text); }
        EditorUtility.SetDirty(rect); EditorUtility.SetDirty(caller);
        EditorSceneManager.MarkSceneDirty(caller.gameObject.scene);
        EditorSceneManager.SaveScene(caller.gameObject.scene);
        var builds = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(Menu,true), new EditorBuildSettingsScene(Game,true) };
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.path != Menu && scene.path != Game)
                builds.Add(new EditorBuildSettingsScene(scene.path, scene.enabled && !scene.path.StartsWith("Assets/Working Folder/Onur/",StringComparison.Ordinal)));
        EditorBuildSettings.scenes = builds.ToArray();
        foreach (var root in caller.gameObject.scene.GetRootGameObjects())
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.fontStyle |= FontStyles.Bold; text.fontWeight = FontWeight.Heavy;
                EditorUtility.SetDirty(text);
                if (text.font && text.font.material && text.font.material.HasProperty("_WeightBold"))
                {
                    text.font.boldStyle = 1f; text.font.material.SetFloat("_WeightBold", 1f);
                    EditorUtility.SetDirty(text.font); EditorUtility.SetDirty(text.font.material);
                }
            }
        EditorSceneManager.MarkSceneDirty(caller.gameObject.scene); EditorSceneManager.SaveScene(caller.gameObject.scene);
        AssetDatabase.SaveAssets();
    }
    private static void Place(TMP_Text text,float min,float max)
    {
        text.rectTransform.anchorMin = new Vector2(.055f,min); text.rectTransform.anchorMax=new Vector2(.945f,max);
        text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
        text.alignment=TextAlignmentOptions.TopLeft; text.overflowMode=TextOverflowModes.Ellipsis; text.raycastTarget=false;
        EditorUtility.SetDirty(text.rectTransform);
    }
}
