using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Local file queue: Library/CodexBridge/request.json -> response.json.
// Runs on the editor thread and only edits explicitly addressed scene buttons.
[InitializeOnLoad]
public static class CodexButtonBridge
{
    [Serializable] public class Request
    {
        public string id, action, path, text;
        public bool setText, setPosition, setSize, setColor;
        public Vector2 position, size;
        public Color color = Color.white;
    }
    [Serializable] public class ButtonInfo
    {
        public string path, text;
        public Vector2 position, size;
        public Color color;
        public bool active, interactable;
        public string[] callbacks;
    }
    [Serializable] public class Response
    {
        public string id, error, scene, text;
        public ButtonInfo[] buttons;
    }
    static readonly string DirectoryPath = Path.GetFullPath("Library/CodexBridge");
    static double nextPoll;

    static CodexButtonBridge()
    {
        Directory.CreateDirectory(DirectoryPath);
        EditorApplication.update += Poll;
    }

    static string ObjectPath(Transform t) => t.parent == null ? t.name : ObjectPath(t.parent) + "/" + t.name;
    static Button[] Buttons() => Resources.FindObjectsOfTypeAll<Button>()
        .Where(b => b.gameObject.scene.IsValid() && b.gameObject.scene.isLoaded && !EditorUtility.IsPersistent(b)).ToArray();

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 0.25;
        string input = Path.Combine(DirectoryPath, "request.json");
        if (!File.Exists(input)) return;
        var response = new Response();
        try
        {
            var request = JsonUtility.FromJson<Request>(File.ReadAllText(input));
            File.Delete(input);
            response.id = request.id;
            if (request.action == "edit")
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before editing.");
                var matches = Buttons().Where(b => ObjectPath(b.transform) == request.path).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one button at path: " + request.path);
                var button = matches[0];
                var rect = button.GetComponent<RectTransform>();
                var label = button.GetComponentInChildren<TMP_Text>(true);
                var legacy = button.GetComponentInChildren<Text>(true);
                if (request.setText && label == null && legacy == null) throw new InvalidOperationException("Button has no text component.");
                if (request.setColor && button.targetGraphic == null) throw new InvalidOperationException("Button has no target graphic.");
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Codex: Edit button");
                if (request.setText)
                {
                    UnityEngine.Object target = label != null ? (UnityEngine.Object)label : legacy;
                    Undo.RecordObject(target, "Button text");
                    if (label != null) label.text = request.text; else legacy.text = request.text;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                }
                Undo.RecordObject(rect, "Button layout");
                if (request.setPosition) rect.anchoredPosition = request.position;
                if (request.setSize) rect.sizeDelta = request.size;
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                if (request.setColor)
                {
                    Undo.RecordObject(button.targetGraphic, "Button color");
                    button.targetGraphic.color = request.color;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(button.targetGraphic);
                }
                EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
            }
            else if (request.action == "refresh")
            {
                AssetDatabase.Refresh();
                response.text = "Refreshed AssetDatabase";
            }
            else if (request.action == "open_sample_scene")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
                response.text = "Opened SampleScene";
            }
            else if (request.action == "install_qq")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
                QuickQuestionInstaller.InstallOrUpdate();
                response.text = "Installed Quick Question System";
            }
            else if (request.action == "install_onur")
            {
                OnurPlayableInstaller.Install();
                response.text = "Integrated Onur playable flow";
            }
            else if (request.action == "polish_typography")
            {
                OnurTypographyPolish.Apply();
                response.text = "Polished Onur typography";
            }
            else if (request.action == "verify_typography")
            {
                response.text = OnurTypographyPolish.VerifyTurkishGlyphs();
            }
            else if (request.action == "save_active")
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                response.text = "Saved active scene";
            }
            else if (request.action == "validate_onur")
            {
                response.text = OnurPlayableChecks.ValidateScenes();
            }
            else if (request.action == "smoke_onur")
            {
                OnurPlayableChecks.StartSmoke();
                response.text = "Started Onur Play Mode smoke test";
            }
            else if (request.action == "run_qq_tests")
            {
                ExecuteQuickQuestionTests(response);
                return;
            }
            else if (request.action != "inspect") throw new InvalidOperationException("Supported actions: inspect, edit, refresh, open_sample_scene, install_qq, install_onur, polish_typography, verify_typography, save_active, validate_onur, smoke_onur, run_qq_tests.");
            response.scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            response.buttons = Buttons().Select(b => new ButtonInfo
            {
                path = ObjectPath(b.transform),
                text = b.GetComponentInChildren<TMP_Text>(true)?.text ?? b.GetComponentInChildren<Text>(true)?.text,
                position = b.GetComponent<RectTransform>().anchoredPosition,
                size = b.GetComponent<RectTransform>().sizeDelta,
                color = b.targetGraphic != null ? b.targetGraphic.color : Color.white,
                active = b.gameObject.activeInHierarchy,
                interactable = b.interactable,
                callbacks = Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Select(i => b.onClick.GetPersistentMethodName(i)).ToArray()
            }).ToArray();
        }
        catch (Exception e) { response.error = e.Message; }
        string output = Path.Combine(DirectoryPath, "response.json");
        File.WriteAllText(output, JsonUtility.ToJson(response, true));
    }

    private static void ExecuteQuickQuestionTests(Response response)
    {
        QuickQuestionBridge.StartTests(response.id);
    }
}
