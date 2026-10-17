using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class QuickQuestionInstaller
{
    [MenuItem("GameJam/Hızlı Soru Sistemini Kur veya Güncelle")]
    public static void InstallOrUpdate()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Lütfen önce Play Mode'dan çıkın.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/SampleScene.unity")throw new System.InvalidOperationException("Quick Questions are installed only in SampleScene.");
        var flow=Object.FindAnyObjectByType<DispatchCallFlow>();
        var canvas=flow ? flow.GetComponentInParent<Canvas>() : null;
        if (!canvas)
        {
            Debug.LogError("Sahnede Canvas bulunamadı.");
            return;
        }

        var resources = Object.FindAnyObjectByType<DispatchResources>();
        var conversation = Object.FindAnyObjectByType<OyunAkisiOnur>();
        var phone = Object.FindAnyObjectByType<DispatchCallFlow>();
        var briefingManager = Object.FindAnyObjectByType<PresidentialBriefingManager>();

        // Find or load pixel font
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/UI/Fonts/VCR_OSD_MONO_1.001 SDF.asset");
        if (!font)
        {
            var hud = Object.FindAnyObjectByType<DispatchResourceHud>();
            if (hud)
            {
                var soHud = new SerializedObject(hud);
                font = soHud.FindProperty("font").objectReferenceValue as TMP_FontAsset;
            }
        }

        // Find or create Hızlı Soru Paneli under Canvas
        var panelTransform = canvas.transform.Find("Hızlı Soru Paneli");
        GameObject panelGo;
        if (!panelTransform)
        {
            panelGo = new GameObject("Hızlı Soru Paneli", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(QuickQuestionView), typeof(QuickQuestionManager));
            panelGo.transform.SetParent(canvas.transform, false);
        }
        else
        {
            panelGo = panelTransform.gameObject;
            if (!panelGo.GetComponent<CanvasGroup>()) panelGo.AddComponent<CanvasGroup>();
            if (!panelGo.GetComponent<QuickQuestionView>()) panelGo.AddComponent<QuickQuestionView>();
            if (!panelGo.GetComponent<QuickQuestionManager>()) panelGo.AddComponent<QuickQuestionManager>();
        }

        // Setup Canvas override sorting
        var panelCanvas = panelGo.GetComponent<Canvas>();
        panelCanvas.overrideSorting = false;
        panelCanvas.sortingOrder = 0;

        // Position on left monitor
        var rect = (RectTransform)panelGo.transform;
        rect.anchorMin = new Vector2(0.02f, 0.36f);
        rect.anchorMax = new Vector2(0.35f, 0.88f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Configure QuickQuestionView
        var view = panelGo.GetComponent<QuickQuestionView>();
        var soView = new SerializedObject(view);
        if (font) soView.FindProperty("font").objectReferenceValue = font;
        soView.ApplyModifiedPropertiesWithoutUndo();

        // Configure QuickQuestionManager
        var manager = panelGo.GetComponent<QuickQuestionManager>();
        var soManager = new SerializedObject(manager);
        soManager.FindProperty("resources").objectReferenceValue = resources;
        soManager.FindProperty("conversation").objectReferenceValue = conversation;
        soManager.FindProperty("phone").objectReferenceValue = phone;
        soManager.FindProperty("briefingManager").objectReferenceValue = briefingManager;
        soManager.FindProperty("view").objectReferenceValue = view;

        // Populate 8 questions
        soManager.ApplyModifiedPropertiesWithoutUndo();
        manager.InitializeDefaultQuestionsIfNeeded();
        soManager.Update();
        EditorUtility.SetDirty(manager);

        EditorUtility.SetDirty(panelGo);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("Hızlı Soru Sistemi başarıyla kuruldu ve sahneye kaydedildi.");
    }
}
