using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One reusable blackout overlay across a scene load. No extra EventSystem.</summary>
[DisallowMultipleComponent]
public sealed class SceneFadeTransition : MonoBehaviour
{
    private static SceneFadeTransition instance;
    private readonly List<EventSystem> lockedEventSystems = new List<EventSystem>();
    private CanvasGroup curtain;
    public static bool IsTransitioning => instance != null && instance.curtain != null && instance.curtain.gameObject.activeSelf;

    public static void LoadScene(string sceneName, float fadeOutDuration, float fadeInDuration)
    {
        if (IsTransitioning) return;
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Cannot enter shift: scene is not available in the active Build Profile: " + sceneName);
            return;
        }

        if (instance == null)
        {
            var host = new GameObject("SceneFadeTransition");
            instance = host.AddComponent<SceneFadeTransition>();
            DontDestroyOnLoad(host);
            instance.CreateCurtain();
        }
        instance.curtain.gameObject.SetActive(true);
        instance.StartCoroutine(instance.LoadRoutine(sceneName, fadeOutDuration, fadeInDuration));
    }

    private void CreateCurtain()
    {
        var layer = new GameObject("SceneFadeCurtain", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        layer.transform.SetParent(transform, false);
        var canvas = layer.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        curtain = layer.GetComponent<CanvasGroup>();
        curtain.alpha = 0;
        curtain.blocksRaycasts = true;
        curtain.interactable = false;

        var black = new GameObject("Black", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        black.transform.SetParent(layer.transform, false);
        var rect = black.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        black.GetComponent<Image>().color = Color.black;
        layer.SetActive(false);
    }

    private IEnumerator LoadRoutine(string sceneName, float fadeOutDuration, float fadeInDuration)
    {
        LockInput();
        SceneManager.sceneLoaded += OnSceneLoaded;
        yield return Fade(0, 1, fadeOutDuration);

        // Availability was checked before fading. An interrupted load restores the menu.
        AsyncOperation load = null;
        try { load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); }
        catch (System.Exception exception) { Debug.LogException(exception); }
        if (load != null) yield return load;
        yield return null; // Let the destination's Awake/Start finish behind full black.

        yield return Fade(1, 0, fadeInDuration);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        RestoreInput();
        curtain.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        duration = Mathf.Max(0, duration);
        float elapsed = 0;
        curtain.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            curtain.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        curtain.alpha = to;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => LockInput();

    private void LockInput()
    {
        foreach (var system in FindObjectsByType<EventSystem>())
        {
            if (!system.enabled) continue;
            system.SetSelectedGameObject(null);
            system.enabled = false;
            lockedEventSystems.Add(system);
        }
    }

    private void RestoreInput()
    {
        foreach (var system in lockedEventSystems)
            if (system != null) system.enabled = true;
        lockedEventSystems.Clear();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        RestoreInput();
        if (instance == this) instance = null;
    }
}
