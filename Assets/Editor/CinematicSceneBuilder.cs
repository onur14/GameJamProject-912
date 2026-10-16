#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;

public static class CinematicSceneBuilder
{
    [MenuItem("GameJam/Cinematic Sahnesini Oluştur veya Güncelle")]
    public static void BuildScene()
    {
        // 1. Yeni boş sahne oluştur
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Main Camera
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        cam.depth = -1f;
        cameraObj.AddComponent<AudioListener>();
        cameraObj.AddComponent<UniversalAdditionalCameraData>();
        cameraObj.tag = "MainCamera";
        cameraObj.transform.position = new Vector3(0f, 0f, -10f);

        // 3. EventSystem
        GameObject eventObj = new GameObject("EventSystem");
        eventObj.AddComponent<EventSystem>();
        eventObj.AddComponent<InputSystemUIInputModule>();

        // 4. Cinematic Canvas
        GameObject canvasObj = new GameObject("CinematicCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        CinematicDeskIntro intro = canvasObj.AddComponent<CinematicDeskIntro>();

        // 5. Content Root
        GameObject rootObj = new GameObject("ContentRoot", typeof(RectTransform));
        rootObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rootRect = rootObj.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.anchoredPosition = new Vector2(0f, -25f);
        rootRect.sizeDelta = Vector2.zero;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.localScale = Vector3.one * 1.85f;

        // 6. Backdrop
        GameObject bgObj = CreateUIElement("Backdrop", rootObj.transform);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.015f, 0.02f, 0.027f, 1f);

        // Sprite'ları bul
        Sprite spriteLightsOff = FindSpriteByName("Acil merkezde");
        Sprite spriteNightShift = FindSpriteByName("Gece vardiyas");
        Sprite spriteDispatch = FindSpriteByName("Mavi-gri sevk");

        // 7. PhotoLightsOff
        GameObject lightsOffObj = CreateUIElement("PhotoLightsOff", rootObj.transform);
        Image lightsOffImg = lightsOffObj.AddComponent<Image>();
        lightsOffImg.sprite = spriteLightsOff;
        lightsOffImg.preserveAspect = true;
        lightsOffImg.color = new Color(1f, 1f, 1f, 0f);

        // 8. PhotoNightShift
        GameObject nightShiftObj = CreateUIElement("PhotoNightShift", rootObj.transform);
        Image nightShiftImg = nightShiftObj.AddComponent<Image>();
        nightShiftImg.sprite = spriteNightShift;
        nightShiftImg.preserveAspect = true;
        nightShiftImg.color = new Color(1f, 1f, 1f, 0f);

        // 9. PhotoDispatchScreen
        GameObject dispatchObj = CreateUIElement("PhotoDispatchScreen", rootObj.transform);
        Image dispatchImg = dispatchObj.AddComponent<Image>();
        dispatchImg.sprite = spriteDispatch;
        dispatchImg.preserveAspect = true;
        dispatchImg.color = new Color(1f, 1f, 1f, 1f);

        // 10. Blackout Overlay
        GameObject blackoutObj = CreateUIElement("BlackoutOverlay", canvasObj.transform);
        Image blackoutImg = blackoutObj.AddComponent<Image>();
        blackoutImg.color = new Color(0f, 0f, 0f, 0f);

        // 11. Letterbox Bars
        GameObject topBarObj = new GameObject("LetterboxTop", typeof(RectTransform));
        topBarObj.transform.SetParent(canvasObj.transform, false);
        RectTransform topRect = topBarObj.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = Vector2.zero;
        topRect.sizeDelta = new Vector2(0f, 65f);
        Image topImg = topBarObj.AddComponent<Image>();
        topImg.color = Color.black;

        GameObject bottomBarObj = new GameObject("LetterboxBottom", typeof(RectTransform));
        bottomBarObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bottomRect = bottomBarObj.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.anchoredPosition = Vector2.zero;
        bottomRect.sizeDelta = new Vector2(0f, 65f);
        Image bottomImg = bottomBarObj.AddComponent<Image>();
        bottomImg.color = Color.black;

        // 12. Script Referanslarını Ata (SerializedObject ile)
        SerializedObject so = new SerializedObject(intro);
        so.FindProperty("dispatchScreenImage").objectReferenceValue = dispatchImg;
        so.FindProperty("nightShiftDeskImage").objectReferenceValue = nightShiftImg;
        so.FindProperty("lightsOffImage").objectReferenceValue = lightsOffImg;
        so.FindProperty("blackoutOverlay").objectReferenceValue = blackoutImg;
        so.FindProperty("contentRoot").objectReferenceValue = rootRect;
        so.FindProperty("topBar").objectReferenceValue = topRect;
        so.FindProperty("bottomBar").objectReferenceValue = bottomRect;
        so.FindProperty("letterboxHeight").floatValue = 65f;
        so.FindProperty("initialHoldDuration").floatValue = 1.8f;
        so.FindProperty("zoomOutDuration").floatValue = 2.4f;
        so.FindProperty("roomHoldDuration").floatValue = 1.4f;
        so.FindProperty("blackoutFlickerDuration").floatValue = 1.1f;
        so.FindProperty("darkHoldDuration").floatValue = 1.2f;
        so.FindProperty("finalFadeDuration").floatValue = 2.6f;
        so.FindProperty("initialZoomScale").floatValue = 1.85f;
        so.FindProperty("initialOffset").vector2Value = new Vector2(0f, -25f);
        so.FindProperty("handheldDriftAmount").floatValue = 3.5f;
        so.FindProperty("enableProceduralAudio").boolValue = true;
        so.FindProperty("audioVolume").floatValue = 0.55f;
        so.FindProperty("playOnStart").boolValue = true;
        so.FindProperty("allowReplayWithR").boolValue = true;
        so.ApplyModifiedProperties();

        // 13. Sahneyi Kaydet
        string scenePath = "Assets/Scenes/CinematicDeskScene.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=#00FF88>[GameJam] CinematicDeskScene başarıyla oluşturuldu ve kaydedildi: {scenePath}</color>");
    }

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        return obj;
    }

    private static Sprite FindSpriteByName(string partialName)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite " + partialName);
        if (guids == null || guids.Length == 0)
            guids = AssetDatabase.FindAssets("t:Texture2D " + partialName);

        if (guids != null && guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;

            // Texture ise sub-asset'lerde sprite ara
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in allAssets)
            {
                if (asset is Sprite sprite)
                    return sprite;
            }
        }
        return null;
    }
}
#endif
