using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OnurPlayableInstaller
{
    private const string MenuPath = "Assets/Working Folder/Onur/OnurAnaMenu.unity";
    private const string GamePath = "Assets/Working Folder/Onur/Onur Test Place Integrated.unity";
    private const string FontPath = "Assets/Art/UI/Fonts/VCR_OSD_MONO_1.001 SDF.asset";
    private const string SafetyPrefabPath = "Assets/Working Folder/Onur/MiniGames/Prefabs/SafetyMiniGame.prefab";
    private static TMP_FontAsset font;

    [MenuItem("Tools/Onur/Integrate Playable Flow")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Önce Play Mode'dan çıkın.");
        Scene previous = SceneManager.GetActiveScene();
        string previousPath = previous.path;
        if (previous.isDirty || string.IsNullOrEmpty(previousPath))
            throw new InvalidOperationException("Açık sahneyi kurulumdan önce kaydedin.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        try
        {
            UpdateSafetyPrefab();
            InstallMenu(EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single));
            InstallGame(EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single));
            var existing = EditorBuildSettings.scenes.Where(item => item.path != MenuPath && item.path != GamePath)
                .Select(item => new EditorBuildSettingsScene(item.path, false));
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(GamePath, true)
            }.Concat(existing).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Onur menü → oyun akışı, hızlı sorular, arayan bilgisi, ayarlar ve sonuç ekranı bağlandı.");
        }
        finally
        {
            if (SceneManager.GetActiveScene().path != previousPath)
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
        }
    }

    private static void UpdateSafetyPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SafetyPrefabPath);
        try
        {
            void Position(string name, Vector2 min, Vector2 max)
            {
                Transform item = root.transform.Find(name);
                if (item == null) throw new InvalidOperationException("Güvenlik prefabında eksik nesne: " + name);
                var rect = (RectTransform)item;
                rect.anchorMin = min;
                rect.anchorMax = max;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                item.gameObject.SetActive(true);
            }

            Position("Kontrol Noktası 1", new Vector2(.05f, .40f), new Vector2(.32f, .80f));
            Position("Kontrol Noktası 2", new Vector2(.37f, .46f), new Vector2(.63f, .76f));
            Position("Kontrol Noktası 3", new Vector2(.68f, .46f), new Vector2(.94f, .76f));
            Position("Seçilen Nokta", new Vector2(.04f, .27f), new Vector2(.31f, .35f));
            Position("Açıklama", new Vector2(.32f, .25f), new Vector2(.96f, .35f));
            Position("Seçenek A", new Vector2(.04f, .12f), new Vector2(.49f, .24f));
            Position("Seçenek B", new Vector2(.51f, .12f), new Vector2(.96f, .24f));
            root.transform.Find("Yönerge").GetComponent<TMP_Text>().text =
                "Tesisata dokun, durumu oku ve uygun eylemi seç.";
            var game = root.GetComponent<SafetyMiniGame>();
            var serialized = new SerializedObject(game);
            SerializedProperty points = serialized.FindProperty("points");
            if (points.arraySize != 3) throw new InvalidOperationException("Güvenlik prefabında üç tesisat bekleniyor.");
            string[] names = { "ELEKTRİK PANOSU", "GAZ VANASI", "SU VANASI" };
            string[] situations = {
                "Sarsıntı bitti. Gaz kokusu yok ve panoya güvenle erişebiliyorsun.",
                "Sarsıntı bitti. Gaz vanasına güvenle erişebiliyorsun.",
                "Sarsıntı bitti. Su vanasına güvenle erişebiliyorsun."
            };
            string[] firstActions = { "Elektriği açık bırak", "Gaz vanasını açık bırak", "Su vanasını kapat" };
            string[] secondActions = { "Ana şalteri kapat", "Gaz vanasını kapat", "Su vanasını açık bırak" };
            for (int index = 0; index < points.arraySize; index++)
            {
                SerializedProperty point = points.GetArrayElementAtIndex(index);
                point.FindPropertyRelative("pointName").stringValue = names[index];
                point.FindPropertyRelative("description").stringValue = situations[index];
                point.FindPropertyRelative("optionA").stringValue = firstActions[index];
                point.FindPropertyRelative("optionB").stringValue = secondActions[index];
                point.FindPropertyRelative("correctOption").intValue = index == 2 ? 0 : 1;
                Image marker = root.transform.Find("Seçim Çizgisi " + (index + 1))?.GetComponent<Image>();
                if (marker == null)
                {
                    float left = .05f + .32f * index;
                    marker = Panel("Seçim Çizgisi " + (index + 1), root.transform,
                        new Vector2(left, .37f), new Vector2(left + .25f, .38f),
                        new Color(.21f, .41f, .53f, .9f));
                    marker.raycastTarget = false;
                }
                point.FindPropertyRelative("selectionMarker").objectReferenceValue = marker;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (string name in new[] { "Seçenek A", "Seçenek B" })
            {
                TMP_Text label = root.transform.Find(name).GetComponentInChildren<TMP_Text>(true);
                label.fontSize = 19;
                label.enableAutoSizing = true;
                label.fontSizeMin = 16;
                label.fontSizeMax = 19;
            }
            PrefabUtility.SaveAsPrefabAsset(root, SafetyPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static T Find<T>(Scene scene) where T : Component
    {
        T[] found = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        if (found.Length != 1) throw new InvalidOperationException(typeof(T).Name + " sahnede tekil değil: " + found.Length);
        return found[0];
    }

    private static void Set(UnityEngine.Object component, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null) throw new InvalidOperationException(component.GetType().Name + "." + field + " bulunamadı.");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform Rect(GameObject gameObject, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        Rect(image.gameObject, parent, min, max);
        image.color = color;
        return image;
    }

    private static TMP_Text Label(string name, Transform parent, Vector2 min, Vector2 max,
        string value, int size, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        Rect(label.gameObject, parent, min, max);
        label.font = font != null ? font : TMP_Settings.defaultFontAsset;
        label.fontSize = size;
        label.color = new Color(.88f, .94f, .91f);
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        label.text = value;
        return label;
    }

    private static Button Action(string name, Transform parent, Vector2 min, Vector2 max, string label)
    {
        Image image = Panel(name, parent, min, max, new Color(.11f, .29f, .36f, 1f));
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.2f, 1.25f, 1.25f);
        colors.pressedColor = new Color(.7f, .8f, .8f);
        colors.disabledColor = new Color(.4f, .4f, .4f);
        button.colors = colors;
        Label("Metin", button.transform, new Vector2(.04f, .06f), new Vector2(.96f, .94f),
            label, 20, TextAlignmentOptions.Center);
        return button;
    }

    private static void OverlayCanvas(GameObject target, int order)
    {
        Canvas canvas = target.AddComponent<Canvas>();
        var serialized = new SerializedObject(canvas);
        serialized.FindProperty("m_OverrideSorting").boolValue = true;
        serialized.FindProperty("m_SortingOrder").intValue = order;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        target.AddComponent<GraphicRaycaster>();
    }

    private static void InstallMenu(Scene scene)
    {
        AnaMenü menu = Find<AnaMenü>(scene);
        var menuData = new SerializedObject(menu);
        menuData.FindProperty("gameplayScene").stringValue = "Onur Test Place Integrated";
        menuData.ApplyModifiedPropertiesWithoutUndo();
        Canvas canvas = scene.GetRootGameObjects().First(root => root.name == "Canvas").GetComponent<Canvas>();
        if (canvas.GetComponentInChildren<MasterVolumeSettings>(true) != null) return;
        var play = canvas.GetComponentsInChildren<Button>(true).First(button => button.name == "Oyna");
        var exit = canvas.GetComponentsInChildren<Button>(true).First(button =>
            Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                .Any(index => button.onClick.GetPersistentMethodName(index) == "CikisYap"));
        ((RectTransform)exit.transform).anchoredPosition = new Vector2(0, -412);
        var settingsButton = new GameObject("Ayarlar", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
        var settingsRect = (RectTransform)settingsButton.transform;
        settingsRect.SetParent(play.transform.parent, false);
        settingsRect.anchorMin = settingsRect.anchorMax = new Vector2(.5f, 1f);
        settingsRect.pivot = new Vector2(.5f, 1f);
        settingsRect.anchoredPosition = new Vector2(0, -348);
        settingsRect.sizeDelta = new Vector2(320, 52);
        settingsButton.targetGraphic = settingsButton.GetComponent<Image>();
        settingsButton.targetGraphic.color = new Color(.11f, .29f, .36f, 1f);
        Label("Metin", settingsButton.transform, Vector2.zero, Vector2.one, "AYARLAR", 24, TextAlignmentOptions.Center);

        var controller = canvas.gameObject.AddComponent<MasterVolumeSettings>();
        Image blocker = Panel("Ayarlar Karartması", canvas.transform, Vector2.zero, Vector2.one,
            new Color(0, 0, 0, .82f));
        OverlayCanvas(blocker.gameObject, 600);
        Image card = Panel("Ayarlar Paneli", blocker.transform, new Vector2(.28f, .24f),
            new Vector2(.72f, .76f), new Color(.045f, .10f, .15f, 1f));
        Label("Başlık", card.transform, new Vector2(.08f, .80f), new Vector2(.92f, .95f),
            "AYARLAR", 31, TextAlignmentOptions.Center);
        Label("Ses Başlığı", card.transform, new Vector2(.10f, .59f), new Vector2(.63f, .73f),
            "Genel Ses", 22);
        TMP_Text percentage = Label("Ses Yüzdesi", card.transform, new Vector2(.68f, .59f),
            new Vector2(.90f, .73f), "%100", 22, TextAlignmentOptions.Right);
        var slider = new GameObject("Genel Ses Kaydırıcısı", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>();
        Rect(slider.gameObject, card.transform, new Vector2(.10f, .43f), new Vector2(.90f, .57f));
        Panel("Ray", slider.transform, new Vector2(0, .34f), new Vector2(1, .66f), new Color(.2f, .31f, .35f));
        Image fill = Panel("Doluluk", slider.transform, new Vector2(0, .34f), new Vector2(1, .66f), new Color(.35f, .82f, .83f));
        Image handle = Panel("Tutamak", slider.transform, new Vector2(0, .1f), new Vector2(.045f, .9f), Color.white);
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;
        Button back = Action("Geri", card.transform, new Vector2(.30f, .12f),
            new Vector2(.70f, .28f), "GERİ");
        Set(controller, "openButton", settingsButton);
        Set(controller, "closeButton", back);
        Set(controller, "panel", blocker.gameObject);
        Set(controller, "volumeSlider", slider);
        Set(controller, "percentageLabel", percentage);
        blocker.gameObject.SetActive(false);

        if (scene.GetRootGameObjects().All(root => root.name != "AnaMenuSesi"))
        {
            var music = new GameObject("AnaMenuSesi", typeof(AudioSource)).GetComponent<AudioSource>();
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SoundEffect/AnaMenu1(TELİFLİ).wav");
            music.loop = true;
            music.playOnAwake = true;
            music.volume = .853f;
            SceneManager.MoveGameObjectToScene(music.gameObject, scene);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void InstallGame(Scene scene)
    {
        Canvas canvas = Find<CityHealth>(scene).GetComponent<Canvas>();
        OyunAkisiOnur conversation = Find<OyunAkisiOnur>(scene);
        DispatchCallFlow phone = Find<DispatchCallFlow>(scene);
        PresidentialBriefingManager briefing = Find<PresidentialBriefingManager>(scene);
        DispatchResources resources = Find<DispatchResources>(scene);
        OnurConversationView chat = Find<OnurConversationView>(scene);

        QuickQuestionManager questions = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<QuickQuestionManager>(true)).FirstOrDefault();
        if (questions == null)
        {
            var root = new GameObject("Hızlı Sorular", typeof(RectTransform), typeof(CanvasGroup),
                typeof(QuickQuestionView), typeof(QuickQuestionManager));
            Rect(root, canvas.transform, new Vector2(.02f, .36f), new Vector2(.35f, .88f));
            questions = root.GetComponent<QuickQuestionManager>();
            var view = root.GetComponent<QuickQuestionView>();
            Set(view, "font", font);
            Set(questions, "resources", resources);
            Set(questions, "conversation", conversation);
            Set(questions, "phone", phone);
            Set(questions, "briefingManager", briefing);
            Set(questions, "view", view);
        }
        questions.InitializeDefaultQuestionsIfNeeded();
        EditorUtility.SetDirty(questions);
        Set(conversation, "quickQuestions", questions);
        Set(Find<MiniGameWindow>(scene), "quickQuestionView", questions.GetComponent<QuickQuestionView>());

        CallerInfoView caller = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<CallerInfoView>(true)).FirstOrDefault();
        if (caller == null)
        {
            Image callerPanel = Panel("Arayan Bilgisi", canvas.transform, new Vector2(.14f, .40f),
                new Vector2(.35f, .81f), new Color(.035f, .085f, .11f, .96f));
            callerPanel.raycastTarget = false;
            caller = callerPanel.gameObject.AddComponent<CallerInfoView>();
            Label("Panel Başlığı", caller.transform, new Vector2(.05f, .78f), new Vector2(.95f, .96f),
                "ARAYAN BİLGİSİ", 20);
            TMP_Text name = Label("Arayan", caller.transform, new Vector2(.05f, .61f), new Vector2(.95f, .76f), "", 17);
            TMP_Text location = Label("Konum", caller.transform, new Vector2(.05f, .46f), new Vector2(.95f, .61f), "", 17);
            TMP_Text emergency = Label("Olay", caller.transform, new Vector2(.05f, .31f), new Vector2(.95f, .46f), "", 17);
            TMP_Text summary = Label("Durum", caller.transform, new Vector2(.05f, .04f), new Vector2(.95f, .30f), "", 16);
            Set(caller, "nameLabel", name);
            Set(caller, "locationLabel", location);
            Set(caller, "emergencyLabel", emergency);
            Set(caller, "summaryLabel", summary);
            caller.gameObject.SetActive(false);
        }
        Set(conversation, "callerInfo", caller);

        if (scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RunResultView>(true)).FirstOrDefault() == null)
            InstallResult(canvas, briefing, Find<CityHealth>(scene), Find<MiniGameWindow>(scene));

        var phoneData = new SerializedObject(phone);
        phoneData.FindProperty("ringClip").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SoundEffect/Telefon.wav");
        phoneData.ApplyModifiedPropertiesWithoutUndo();
        var resourceData = new SerializedObject(resources);
        resourceData.FindProperty("initialFirefighters").intValue = 5;
        resourceData.FindProperty("initialAmbulances").intValue = 5;
        resourceData.FindProperty("initialPolice").intValue = 6;
        resourceData.ApplyModifiedPropertiesWithoutUndo();
        var questionData = new SerializedObject(questions);
        questionData.FindProperty("returnStockCeiling").intValue = 6;
        questionData.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void InstallResult(Canvas canvas, PresidentialBriefingManager briefing,
        CityHealth health, MiniGameWindow minigames)
    {
        var root = new GameObject("Vardiya Sonucu", typeof(RectTransform), typeof(RunResultView));
        Rect(root, canvas.transform, Vector2.zero, Vector2.one);
        Image blocker = Panel("Sonuç Karartması", root.transform, Vector2.zero, Vector2.one,
            new Color(.015f, .035f, .055f, .97f));
        OverlayCanvas(blocker.gameObject, 502);
        Image card = Panel("Sonuç Kartı", blocker.transform, new Vector2(.22f, .17f),
            new Vector2(.78f, .83f), new Color(.055f, .12f, .17f, 1f));
        TMP_Text title = Label("Sonuç Başlığı", card.transform, new Vector2(.05f, .81f),
            new Vector2(.95f, .96f), "GÖREV TAMAMLANDI", 31, TextAlignmentOptions.Center);
        TMP_Text reason = Label("Açıklama", card.transform, new Vector2(.07f, .69f),
            new Vector2(.93f, .79f), "", 19, TextAlignmentOptions.Center);
        TMP_Text summary = Label("Vardiya Özeti", card.transform, new Vector2(.16f, .33f),
            new Vector2(.84f, .66f), "", 20);
        Label("Tekrar Sorusu", card.transform, new Vector2(.07f, .23f),
            new Vector2(.93f, .32f), "Tekrar oynamak ister misin?", 19, TextAlignmentOptions.Center);
        Button replay = Action("Tekrar Oyna", card.transform, new Vector2(.07f, .07f),
            new Vector2(.47f, .20f), "TEKRAR OYNA");
        Button menu = Action("Ana Menü", card.transform, new Vector2(.53f, .07f),
            new Vector2(.93f, .20f), "ANA MENÜ");
        var result = root.GetComponent<RunResultView>();
        Set(result, "briefing", briefing);
        Set(result, "health", health);
        Set(result, "minigames", minigames);
        Set(result, "panel", blocker.gameObject);
        Set(result, "titleLabel", title);
        Set(result, "reasonLabel", reason);
        Set(result, "summaryLabel", summary);
        Set(result, "replayButton", replay);
        Set(result, "menuButton", menu);
        blocker.gameObject.SetActive(false);
    }
}
