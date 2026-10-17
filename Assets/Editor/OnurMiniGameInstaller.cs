using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OnurMiniGameInstaller
{
    private const string ScenePath = "Assets/Working Folder/Onur/Onur Test Place Integrated.unity";
    private const string PrefabFolder = "Assets/Working Folder/Onur/MiniGames/Prefabs";
    private const string CutoutFolder = "Assets/Working Folder/Onur/MiniGames/Cutouts";
    private const string BagImagePath = "Assets/MiniGame/MiniGame1(Surukleme)/Adsız.png";
    private const string SafetyImagePath = "Assets/MiniGame/MiniGame2(ElektrikGüvenlik)/güvenlik.png";
    private const string FontGuid = "8f586378b4e144a9851e7b34d9b748ee";

    private static Scene previewScene;
    private static TMP_FontAsset font;

    [MenuItem("Tools/Onur/Install Minigames")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Minigame kurulumu için Play Mode'dan çıkın.");
            return;
        }

        Scene previousScene = SceneManager.GetActiveScene();
        string previousPath = previousScene.path;
        if (previousScene.isDirty || string.IsNullOrEmpty(previousScene.path))
        {
            Debug.LogError("Minigame kurulumundan önce açık sahneyi kaydedin.");
            return;
        }

        try
        {
            EnsureFolder("Assets/Working Folder/Onur", "MiniGames");
            EnsureFolder("Assets/Working Folder/Onur/MiniGames", "Prefabs");
            EnsureFolder("Assets/Working Folder/Onur/MiniGames", "Cutouts");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(FontGuid));
            Sprite[] bagArt = {
                Cutout("bag", BagImagePath, new RectInt(0, 190, 326, 390), true),
                Cutout("water", BagImagePath, new RectInt(330, 380, 82, 160), true),
                Cutout("flashlight", BagImagePath, new RectInt(418, 380, 112, 150), true),
                Cutout("first_aid", BagImagePath, new RectInt(536, 378, 145, 145), true),
                Cutout("radio", BagImagePath, new RectInt(496, 225, 110, 130), true),
                Cutout("ball", BagImagePath, new RectInt(10, 52, 124, 138), true),
                Cutout("controller", BagImagePath, new RectInt(130, 50, 145, 135), true)
            };
            Sprite[] safetyArt = {
                Cutout("breaker", SafetyImagePath, new RectInt(0, 205, 470, 399), false),
                Cutout("gas_valve", SafetyImagePath, new RectInt(474, 465, 220, 120), false),
                Cutout("water_valve", SafetyImagePath, new RectInt(475, 318, 220, 145), false)
            };
            previewScene = EditorSceneManager.NewPreviewScene();

            GameObject itemPrefab = BuildItemPrefab();
            GameObject bagPrefab = BuildBagPrefab(itemPrefab, bagArt);
            GameObject safetyPrefab = BuildSafetyPrefab(safetyArt);
            GameObject windowPrefab = BuildWindowPrefab(bagPrefab, safetyPrefab);

            EditorSceneManager.ClosePreviewScene(previewScene);
            previewScene = default;
            ConnectScene(windowPrefab);
            AssetDatabase.SaveAssets();
            Debug.Log("Onur minigame prefabları ve oyun sahnesi bağlantıları kuruldu.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
            if (!string.IsNullOrEmpty(previousPath) && SceneManager.GetActiveScene().path != previousPath)
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
        }
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }

    private static Sprite Cutout(string name, string sheetPath, RectInt crop, bool whiteBackground)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string sourcePath = Path.Combine(projectRoot, sheetPath);
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!source.LoadImage(File.ReadAllBytes(sourcePath)))
            throw new InvalidOperationException("Görsel okunamadı: " + sheetPath);
        Color32[] sourcePixels = source.GetPixels32();
        var pixels = new Color32[crop.width * crop.height];
        for (int row = 0; row < crop.height; row++)
            Array.Copy(sourcePixels, (crop.y + row) * source.width + crop.x,
                pixels, row * crop.width, crop.width);
        UnityEngine.Object.DestroyImmediate(source);

        var exterior = new bool[pixels.Length];
        var queue = new Queue<int>();
        void Enqueue(int index)
        {
            if (exterior[index] || !IsBackground(pixels[index], whiteBackground)) return;
            exterior[index] = true;
            queue.Enqueue(index);
        }
        for (int column = 0; column < crop.width; column++)
        {
            Enqueue(column);
            Enqueue((crop.height - 1) * crop.width + column);
        }
        for (int row = 0; row < crop.height; row++)
        {
            Enqueue(row * crop.width);
            Enqueue(row * crop.width + crop.width - 1);
        }
        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int column = index % crop.width;
            if (column > 0) Enqueue(index - 1);
            if (column + 1 < crop.width) Enqueue(index + 1);
            if (index >= crop.width) Enqueue(index - crop.width);
            if (index + crop.width < pixels.Length) Enqueue(index + crop.width);
        }
        for (int index = 0; index < pixels.Length; index++)
            if (exterior[index]) pixels[index].a = 0;

        var output = new Texture2D(crop.width, crop.height, TextureFormat.RGBA32, false);
        output.SetPixels32(pixels);
        output.Apply();
        string assetPath = CutoutFolder + "/" + name + ".png";
        string absolutePath = Path.Combine(projectRoot, assetPath);
        byte[] imageBytes = output.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(output);
        if (!File.Exists(absolutePath) || !File.ReadAllBytes(absolutePath).SequenceEqual(imageBytes))
            File.WriteAllBytes(absolutePath, imageBytes);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer.textureType != TextureImporterType.Sprite || !importer.isReadable ||
            importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static bool IsBackground(Color32 pixel, bool whiteBackground)
    {
        if (whiteBackground) return pixel.r >= 225 && pixel.g >= 225 && pixel.b >= 225;
        int highest = Mathf.Max(pixel.r, pixel.g, pixel.b);
        int lowest = Mathf.Min(pixel.r, pixel.g, pixel.b);
        return highest <= 72 && highest - lowest <= 8;
    }

    private static GameObject Object(string name, Transform parent, params Type[] components)
    {
        Type[] types = new[] { typeof(RectTransform) }.Concat(components).ToArray();
        var result = new GameObject(name, types);
        SceneManager.MoveGameObjectToScene(result, previewScene);
        if (parent != null) result.transform.SetParent(parent, false);
        return result;
    }

    private static RectTransform Rect(GameObject target, Vector2 min, Vector2 max)
    {
        var rect = (RectTransform)target.transform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var image = Object(name, parent, typeof(Image)).GetComponent<Image>();
        Rect(image.gameObject, min, max);
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, Vector2 min, Vector2 max,
        string value, float size, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var label = Object(name, parent, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        Rect(label.gameObject, min, max);
        label.font = font != null ? font : TMP_Settings.defaultFontAsset;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        label.text = value;
        return label;
    }

    private static Button Button(string name, Transform parent, Vector2 min, Vector2 max, string label)
    {
        Image image = Panel(name, parent, min, max, new Color(.13f, .29f, .38f));
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f);
        colors.pressedColor = new Color(.7f, .8f, .85f);
        button.colors = colors;
        Text("Metin", button.transform, new Vector2(.04f, .04f), new Vector2(.96f, .96f),
            label, 21, TextAlignmentOptions.Center);
        return button;
    }

    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(UnityEngine.Object target, string field, string value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRect(UnityEngine.Object target, string field, Rect value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).rectValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector(UnityEngine.Object target, string field, Vector2 value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).vector2Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject Save(GameObject root, string name)
    {
        string path = PrefabFolder + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        if (prefab == null) throw new InvalidOperationException("Prefab kaydedilemedi: " + path);
        return prefab;
    }

    private static GameObject BuildItemPrefab()
    {
        var root = Object("Sürüklenebilir Eşya", null, typeof(Image), typeof(CanvasGroup), typeof(BagDraggableItem));
        var rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(120, 120);
        var icon = root.GetComponent<Image>();
        icon.preserveAspect = true;
        Set(root.GetComponent<BagDraggableItem>(), "icon", icon);
        return Save(root, "BagDraggableItem");
    }

    private static BagDropZone Zone(string name, Transform parent, Vector2 min, Vector2 max, bool isBagArea)
    {
        var image = Panel(name, parent, min, max, new Color(.34f, .49f, .47f, .06f));
        BagDropZone zone = image.gameObject.AddComponent<BagDropZone>();
        var serialized = new SerializedObject(zone);
        serialized.FindProperty("isBagArea").boolValue = isBagArea;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var content = Object("Eşyalar", image.transform);
        Rect(content, Vector2.zero, Vector2.one);
        Set(zone, "contentRoot", content.GetComponent<RectTransform>());
        return zone;
    }

    private static GameObject BuildBagPrefab(GameObject itemPrefab, Sprite[] art)
    {
        var root = Object("Afet Çantası Oyun Alanı", null, typeof(Image), typeof(BagMiniGame));
        Rect(root, new Vector2(.08f, .10f), new Vector2(.92f, .90f));
        root.GetComponent<Image>().color = new Color(.13f, .17f, .17f, .97f);
        Panel("Masa Işığı", root.transform, new Vector2(.02f, .18f), new Vector2(.58f, .82f),
            new Color(.38f, .48f, .42f, .08f)).raycastTarget = false;
        Text("Başlık", root.transform, new Vector2(.04f, .91f), new Vector2(.72f, .98f),
            "AFET ÇANTASINI HAZIRLA", 31);
        Text("Yönerge", root.transform, new Vector2(.04f, .83f), new Vector2(.86f, .90f),
            "Acil durumda işe yarayacak eşyaları çantaya sürükle; diğerlerini masada bırak.", 19);
        Text("Masa Yazısı", root.transform, new Vector2(.06f, .76f), new Vector2(.28f, .81f),
            "MASADAKİ EŞYALAR", 17);
        Text("Çanta Yazısı", root.transform, new Vector2(.66f, .76f), new Vector2(.87f, .81f),
            "ÇANTAYA BIRAK", 17, TextAlignmentOptions.Center);

        BagDropZone available = Zone("Masa Alanı", root.transform,
            new Vector2(.03f, .20f), new Vector2(.58f, .78f), false);
        BagDropZone bag = Zone("Çanta Bırakma Alanı", root.transform,
            new Vector2(.61f, .19f), new Vector2(.94f, .78f), true);
        var bagPicture = bag.GetComponent<Image>();
        bagPicture.sprite = art[0];
        bagPicture.color = Color.white;
        bagPicture.preserveAspect = true;
        bagPicture.alphaHitTestMinimumThreshold = .1f;

        var attempts = Text("Kalan Hak", root.transform, new Vector2(.04f, .09f),
            new Vector2(.25f, .16f), "Kalan hak: 3", 20);
        var result = Text("Sonuç", root.transform, new Vector2(.27f, .08f),
            new Vector2(.74f, .16f), string.Empty, 18);
        Button check = Button("Kontrol Et", root.transform, new Vector2(.76f, .07f),
            new Vector2(.94f, .17f), "ÇANTAYI KONTROL ET");
        Button close = Button("Kapat", root.transform, new Vector2(.92f, .91f),
            new Vector2(.97f, .98f), "×");
        var dragLayer = Object("Sürükleme Katmanı", root.transform);
        Rect(dragLayer, Vector2.zero, Vector2.one);

        string[] names = { "Su", "Fener", "İlk yardım", "Radyo", "Top", "Oyun kolu" };
        Vector2[] sizes = {
            new Vector2(80, 155), new Vector2(110, 145), new Vector2(150, 140),
            new Vector2(115, 135), new Vector2(135, 130), new Vector2(150, 115)
        };
        Vector2[] tablePositions = {
            new Vector2(-315, 110), new Vector2(-95, 115), new Vector2(175, 110),
            new Vector2(-275, -120), new Vector2(-35, -120), new Vector2(205, -125)
        };
        Vector2[] bagPositions = {
            new Vector2(-80, 10), new Vector2(-5, 28), new Vector2(78, 12),
            new Vector2(-8, -72), new Vector2(72, -72), new Vector2(-75, -72)
        };
        var items = new BagDraggableItem[names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            var itemObject = (GameObject)PrefabUtility.InstantiatePrefab(itemPrefab, previewScene);
            itemObject.name = names[index];
            itemObject.transform.SetParent(available.ContentRoot, false);
            var itemRect = (RectTransform)itemObject.transform;
            itemRect.sizeDelta = sizes[index];
            itemRect.anchoredPosition = tablePositions[index];
            itemObject.GetComponent<Image>().sprite = art[index + 1];
            var item = itemObject.GetComponent<BagDraggableItem>();
            SetString(item, "itemName", names[index]);
            SetVector(item, "startingPosition", tablePositions[index]);
            SetVector(item, "bagPosition", bagPositions[index]);
            Set(item, "startingArea", available);
            Set(item, "correctArea", index < 4 ? bag : null);
            Set(item, "game", root.GetComponent<BagMiniGame>());
            items[index] = item;
        }

        var game = root.GetComponent<BagMiniGame>();
        Set(game, "dragLayer", dragLayer.GetComponent<RectTransform>());
        Set(game, "attemptsLabel", attempts);
        Set(game, "resultLabel", result);
        Set(game, "checkButton", check);
        Set(game, "closeButton", close);
        var gameData = new SerializedObject(game);
        var itemArray = gameData.FindProperty("items");
        itemArray.arraySize = items.Length;
        for (int index = 0; index < items.Length; index++) itemArray.GetArrayElementAtIndex(index).objectReferenceValue = items[index];
        gameData.ApplyModifiedPropertiesWithoutUndo();
        return Save(root, "BagMiniGame");
    }

    private static GameObject BuildSafetyPrefab(Sprite[] art)
    {
        var root = Object("Tesisat Kontrol Oyun Alanı", null, typeof(Image), typeof(SafetyMiniGame));
        Rect(root, new Vector2(.08f, .10f), new Vector2(.92f, .90f));
        root.GetComponent<Image>().color = new Color(.13f, .17f, .17f, .97f);
        Text("Başlık", root.transform, new Vector2(.04f, .91f), new Vector2(.79f, .98f),
            "SARSINTI SONRASI TESİSAT", 31);
        Text("Yönerge", root.transform, new Vector2(.04f, .84f), new Vector2(.90f, .91f),
            "Üç tesisata sırayla dokun ve her durum için ne yapacağını seç.", 19);

        Vector2[] lower = {
            new Vector2(.05f, .39f), new Vector2(.38f, .46f), new Vector2(.68f, .46f)
        };
        Vector2[] upper = {
            new Vector2(.34f, .79f), new Vector2(.64f, .73f), new Vector2(.94f, .73f)
        };
        string[] names = { "1  ELEKTRİK PANOSU", "2  GAZ VANASI", "3  SU VANASI" };
        var hotspots = new Button[names.Length];
        var markers = new Image[names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            markers[index] = Panel("Seçim İşareti " + (index + 1), root.transform,
                new Vector2(lower[index].x, .375f), new Vector2(upper[index].x, .385f),
                new Color(.21f, .41f, .53f, .9f));
            markers[index].raycastTarget = false;
            var picture = Panel(names[index], root.transform, lower[index], upper[index], Color.white);
            picture.sprite = art[index];
            picture.preserveAspect = true;
            picture.alphaHitTestMinimumThreshold = .1f;
            hotspots[index] = picture.gameObject.AddComponent<Button>();
            hotspots[index].targetGraphic = picture;
            Text("Tesisat Adı " + (index + 1), root.transform,
                new Vector2(lower[index].x, .32f), new Vector2(upper[index].x, .37f),
                names[index], 18, TextAlignmentOptions.Center);
        }

        var pointName = Text("Seçilen Tesisat", root.transform, new Vector2(.04f, .25f),
            new Vector2(.32f, .31f), "ELEKTRİK PANOSU", 22);
        var description = Text("Durum", root.transform, new Vector2(.34f, .24f),
            new Vector2(.96f, .31f), string.Empty, 19);
        Button optionA = Button("Birinci Eylem", root.transform, new Vector2(.04f, .11f),
            new Vector2(.49f, .23f), string.Empty);
        Button optionB = Button("İkinci Eylem", root.transform, new Vector2(.51f, .11f),
            new Vector2(.96f, .23f), string.Empty);
        var attempts = Text("Kalan Hak", root.transform, new Vector2(.67f, .92f),
            new Vector2(.89f, .98f), "Kalan hak: 3", 19, TextAlignmentOptions.Right);
        var result = Text("Sonuç", root.transform, new Vector2(.04f, .02f),
            new Vector2(.76f, .10f), string.Empty, 17);
        Button check = Button("Kontrol Et", root.transform, new Vector2(.77f, .02f),
            new Vector2(.96f, .10f), "KARARLARI KONTROL ET");
        Button close = Button("Kapat", root.transform, new Vector2(.92f, .92f),
            new Vector2(.97f, .99f), "×");

        var game = root.GetComponent<SafetyMiniGame>();
        Set(game, "pointNameLabel", pointName);
        Set(game, "descriptionLabel", description);
        Set(game, "optionALabel", optionA.GetComponentInChildren<TMP_Text>());
        Set(game, "optionBLabel", optionB.GetComponentInChildren<TMP_Text>());
        Set(game, "attemptsLabel", attempts);
        Set(game, "resultLabel", result);
        Set(game, "optionAButton", optionA);
        Set(game, "optionBButton", optionB);
        Set(game, "checkButton", check);
        Set(game, "closeButton", close);

        var data = new SerializedObject(game);
        SerializedProperty points = data.FindProperty("points");
        points.arraySize = hotspots.Length;
        for (int index = 0; index < hotspots.Length; index++)
        {
            SerializedProperty point = points.GetArrayElementAtIndex(index);
            point.FindPropertyRelative("pointName").stringValue = names[index];
            point.FindPropertyRelative("description").stringValue = new[] {
                "Sarsıntı bitti, gaz kokusu yok. Tahliyeden önce elektrik için ne yaparsın?",
                "Sarsıntı bitti. Gaz vanasına güvenle erişebiliyorsun. Ne yaparsın?",
                "Sarsıntı bitti. Su vanasına güvenle erişebiliyorsun. Ne yaparsın?"
            }[index];
            point.FindPropertyRelative("optionA").stringValue = new[] {
                "Ana şalteri kapat", "Gaz vanasını kapat", "Su vanasını açık bırak"
            }[index];
            point.FindPropertyRelative("optionB").stringValue = new[] {
                "Elektriği açık bırak", "Gaz vanasını açık bırak", "Su vanasını kapat"
            }[index];
            point.FindPropertyRelative("correctOption").intValue = index == 2 ? 1 : 0;
            point.FindPropertyRelative("hotspotButton").objectReferenceValue = hotspots[index];
            point.FindPropertyRelative("selectionMarker").objectReferenceValue = markers[index];
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        return Save(root, "SafetyMiniGame");
    }

    private static GameObject BuildWindowPrefab(GameObject bagPrefab, GameObject safetyPrefab)
    {
        var root = Object("Onur Minigame Penceresi", null, typeof(MiniGameWindow));
        Rect(root, Vector2.zero, Vector2.one);
        Button bagEntry = Button("Afet Çantası Aç", root.transform, new Vector2(.35f, .93f),
            new Vector2(.48f, .99f), "AFET ÇANTASI");
        Button safetyEntry = Button("Ev Güvenlik Kontrolü Aç", root.transform, new Vector2(.49f, .93f),
            new Vector2(.68f, .99f), "EV GÜVENLİK KONTROLÜ");
        Image blocker = Panel("Minigame Karartması", root.transform, Vector2.zero, Vector2.one,
            new Color(0, 0, 0, .78f));
        Canvas overlayCanvas = blocker.gameObject.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 501;
        var canvasData = new SerializedObject(overlayCanvas);
        canvasData.FindProperty("m_OverrideSorting").boolValue = true;
        canvasData.ApplyModifiedPropertiesWithoutUndo();
        blocker.gameObject.AddComponent<GraphicRaycaster>();
        CanvasGroup overlay = blocker.gameObject.AddComponent<CanvasGroup>();
        var bagObject = (GameObject)PrefabUtility.InstantiatePrefab(bagPrefab, previewScene);
        bagObject.transform.SetParent(blocker.transform, false);
        var safetyObject = (GameObject)PrefabUtility.InstantiatePrefab(safetyPrefab, previewScene);
        safetyObject.transform.SetParent(blocker.transform, false);
        var window = root.GetComponent<MiniGameWindow>();
        Set(window, "bagEntryButton", bagEntry);
        Set(window, "safetyEntryButton", safetyEntry);
        Set(window, "overlay", overlay);
        Set(window, "bagGame", bagObject.GetComponent<BagMiniGame>());
        Set(window, "safetyGame", safetyObject.GetComponent<SafetyMiniGame>());
        Set(bagObject.GetComponent<BagMiniGame>(), "window", window);
        Set(safetyObject.GetComponent<SafetyMiniGame>(), "window", window);
        bagObject.SetActive(false);
        safetyObject.SetActive(false);
        blocker.gameObject.SetActive(false);
        return Save(root, "MiniGameWindow");
    }

    private static T InScene<T>(Scene scene) where T : Component
    {
        T[] matches = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException(typeof(T).Name + " sahnede tekil değil: " + matches.Length);
        return matches[0];
    }

    private static void ConnectScene(GameObject windowPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CityHealth health = InScene<CityHealth>(scene);
        DispatchCallFlow phone = InScene<DispatchCallFlow>(scene);
        OnurConversationView conversation = InScene<OnurConversationView>(scene);
        Canvas canvas = health.GetComponent<Canvas>();
        if (canvas == null) throw new InvalidOperationException("Onur Canvas bulunamadı.");
        MiniGameWindow window = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MiniGameWindow>(true)).FirstOrDefault();
        if (window == null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(windowPrefab, scene);
            instance.transform.SetParent(canvas.transform, false);
            window = instance.GetComponent<MiniGameWindow>();
        }
        Set(window, "cityHealth", health);
        Set(window, "phone", phone);
        Set(window, "conversationView", conversation);
        PrefabUtility.RecordPrefabInstancePropertyModifications(window);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
