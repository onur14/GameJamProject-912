using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OnurTypographyPolish
{
    private const string MenuPath = "Assets/Working Folder/Onur/OnurAnaMenu.unity";
    private const string GamePath = "Assets/Working Folder/Onur/Onur Test Place Integrated.unity";
    private const string ConversationPath = "Assets/Working Folder/Onur/OnurConversation.prefab";
    private const string MiniGamePath = "Assets/Working Folder/Onur/MiniGames/Prefabs/";
    private const string PixelFontPath = "Assets/Art/UI/Fonts/VCR_OSD_MONO_1.001 SDF.asset";
    private const string TurkishFallbackPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";

    private static readonly Color Primary = new Color(.91f, .96f, .93f);
    private static readonly Color Secondary = new Color(.84f, .92f, .91f);
    private static readonly Color Amber = new Color(.98f, .78f, .47f);

    public static string VerifyTurkishGlyphs()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);
        if (font == null) throw new InvalidOperationException("Mevcut piksel fontu bulunamadı.");
        EnsureTurkishFallback(font);
        const string characters = "ÇçĞğİıÖöŞşÜü";
        string missing = string.Concat(characters.Where(character => !font.HasCharacter(character, true, false)));
        AssetDatabase.SaveAssets();
        return string.IsNullOrEmpty(missing) ? "Türkçe karakterler piksel fontu ve mevcut yedek fontla görüntüleniyor."
            : "Font zincirinde eksik Türkçe karakterler: " + missing;
    }

    [MenuItem("Tools/Onur/Polish Text Readability")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Önce Play Mode'dan çıkın.");
        Scene current = SceneManager.GetActiveScene();
        if (current.isDirty || string.IsNullOrEmpty(current.path))
            throw new InvalidOperationException("Açık sahneyi önce kaydedin.");

        TMP_FontAsset pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);
        if (pixelFont == null) throw new InvalidOperationException("Mevcut piksel fontu bulunamadı.");
        EnsureTurkishFallback(pixelFont);
        string previousPath = current.path;
        try
        {
            PolishPrefab(ConversationPath, root =>
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                    Polish(text, null, Primary, true, false);
            });
            foreach (string name in new[] { "BagMiniGame", "SafetyMiniGame", "MiniGameWindow", "BagDraggableItem" })
            {
                string path = MiniGamePath + name + ".prefab";
                PolishPrefab(path, root =>
                {
                    foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        bool title = text.name.Contains("Başlık") || text.name == "Seçilen Nokta";
                        bool status = text.name == "Sonuç" || text.name == "Kalan Hak";
                        bool instruction = text.name == "Yönerge" || text.name == "Açıklama";
                        Color color = title ? Amber : status ? Secondary : instruction ? Secondary : Primary;
                        Polish(text, pixelFont, color, true, instruction || text.GetComponentInParent<Button>() != null);
                        if (text.name == "Kalan Hak") text.color = new Color(.98f, .60f, .55f);
                    }
                });
            }

            Scene menu = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            Button settingsButton = menu.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .FirstOrDefault(button => button.name == "Ayarlar");
            if (settingsButton == null) throw new InvalidOperationException("Ayarlar düğmesi bulunamadı.");
            settingsButton.GetComponent<Image>().color = new Color(.11f, .29f, .36f, 1f);
            foreach (TMP_Text text in menu.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true)))
            {
                bool important = text.GetComponentInParent<Button>() != null
                    || text.name == "Başlık" || text.name == "Ses Başlığı" || text.name == "Ses Yüzdesi";
                if (important) Polish(text, null, text.color, true, false);
                else if (text.fontSize < 20) text.color = new Color(.69f, .79f, .84f);
            }
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);

            Scene game = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
            CallerInfoView caller = game.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<CallerInfoView>(true)).FirstOrDefault();
            if (caller == null) throw new InvalidOperationException("Arayan bilgisi paneli bulunamadı.");
            var callerRect = (RectTransform)caller.transform;
            callerRect.anchorMin = new Vector2(.14f, .40f);
            callerRect.anchorMax = new Vector2(.35f, .81f);
            callerRect.offsetMin = callerRect.offsetMax = Vector2.zero;
            foreach (TMP_Text text in caller.GetComponentsInChildren<TMP_Text>(true))
                Polish(text, null, text.name == "Panel Başlığı" ? Amber : Primary, true, text.name != "Panel Başlığı");
            RunResultView result = game.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<RunResultView>(true)).FirstOrDefault();
            if (result == null) throw new InvalidOperationException("Sonuç paneli bulunamadı.");
            foreach (TMP_Text text in result.GetComponentsInChildren<TMP_Text>(true))
                Polish(text, null, text.name == "Sonuç Başlığı" ? Amber : Primary, true, true);
            foreach (TMP_Text text in game.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true)))
                if (text.name.EndsWith(" Sayisi", StringComparison.Ordinal))
                    Polish(text, null, text.color, true, false);
            EditorSceneManager.MarkSceneDirty(game);
            EditorSceneManager.SaveScene(game);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (SceneManager.GetActiveScene().path != previousPath)
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
        }
    }

    private static void PolishPrefab(string path, Action<GameObject> edit)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void EnsureTurkishFallback(TMP_FontAsset pixelFont)
    {
        TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TurkishFallbackPath);
        if (fallback == null) throw new InvalidOperationException("Türkçe font yedeği bulunamadı.");
        if (pixelFont.fallbackFontAssetTable.Contains(fallback)) return;
        pixelFont.fallbackFontAssetTable.Add(fallback);
        EditorUtility.SetDirty(pixelFont);
    }

    private static void Polish(TMP_Text text, TMP_FontAsset font, Color color, bool strong, bool fit)
    {
        if (font != null) text.font = font;
        text.color = color;
        if (strong) text.fontStyle |= FontStyles.Bold;
        text.characterSpacing = .15f;
        text.lineSpacing = 2f;
        text.textWrappingMode = TextWrappingModes.Normal;
        if (!fit) return;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(13f, text.fontSize - 3f);
        text.fontSizeMax = text.fontSize;
    }
}
