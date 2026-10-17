using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Taha ve Yasin sahnelerinde tum TMP yazilarini (sahnede veya kodla sonradan olusturulan) okunur boyda tutar.
// Yalnizca "Taha and Yasin" klasorundeki sahnelerde kendiliginden calisir; Onur sahnelerini etkilemez.
public sealed class TextReadabilityBoost : MonoBehaviour
{
    private const string SceneFolder = "Taha and Yasin";
    private const float Factor = 1.25f;
    private const float AutoMinFactor = 1f;
    private const float RefreshInterval = 0.2f;
    private const string NoteTextName = "Görev Notu";
    private const float NoteFactor = 1.45f;
    private const string HealthTextName = "Halk Bilinci";

    private sealed class Entry { public float size, min, max, orig, boosted; public string text; public Vector2 rect; }

    private static bool hooked;
    private readonly Dictionary<TMP_Text, Entry> entries = new Dictionary<TMP_Text, Entry>();
    private readonly List<TMP_Text> dead = new List<TMP_Text>();
    private float nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (!hooked) { hooked = true; SceneManager.sceneLoaded += (scene, mode) => Ensure(scene); }
        Ensure(SceneManager.GetActiveScene());
    }

    private static void Ensure(Scene scene)
    {
        if (!scene.path.Contains(SceneFolder)) return;
        if (FindFirstObjectByType<TextReadabilityBoost>() != null) return;
        new GameObject("TextReadabilityBoost").AddComponent<TextReadabilityBoost>();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + RefreshInterval;
            foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // CallerInfoView owns its measured, compact type scale; do not multiply it twice.
                if(t.GetComponentInParent<CallerInfoView>(true)!=null)continue;
                if (entries.ContainsKey(t)) continue;
                entries[t] = new Entry();
                if (t.name == NoteTextName && t.transform.parent && t.transform.parent.parent)
                    ((RectTransform)t.transform.parent.parent).sizeDelta = new Vector2(700f, 160f);
                if (t.name == HealthTextName)
                {
                    var r = t.rectTransform;
                    r.anchorMin = new Vector2(0.02f, 0.80f);
                    r.anchorMax = new Vector2(0.26f, 0.99f);
                    r.offsetMin = r.offsetMax = Vector2.zero;
                }
            }
        }
        dead.Clear();
        foreach (var pair in entries)
        {
            var t = pair.Key;
            if (t == null) { dead.Add(t); continue; }
            Apply(t, pair.Value);
        }
        foreach (var d in dead) entries.Remove(d);
    }

    private static void Apply(TMP_Text t, Entry e)
    {
        if (t.enableAutoSizing)
        {
            if (!Mathf.Approximately(t.fontSizeMax, e.max) || !Mathf.Approximately(t.fontSizeMin, e.min))
            {
                t.fontSizeMax = Scale(t.fontSizeMax, Factor);
                t.fontSizeMin = Scale(t.fontSizeMin, AutoMinFactor);
                e.max = t.fontSizeMax; e.min = t.fontSizeMin;
            }
        }
        else
        {
            if (!Mathf.Approximately(t.fontSize, e.size))
            {
                e.orig = t.fontSize;
                e.boosted = Scale(e.orig, t.name == NoteTextName ? NoteFactor : Factor);
                t.fontSize = e.boosted;
                e.size = e.boosted;
                e.text = null;
            }
            var rect = t.rectTransform.rect.size;
            if (e.text != t.text || e.rect != rect)
            {
                e.text = t.text; e.rect = rect;
                float size = e.boosted;
                t.fontSize = size;
                while (size > e.orig && !Fits(t)) { size -= 1f; t.fontSize = size; }
                e.size = size;
            }
        }
    }

    private static bool Fits(TMP_Text t)
    {
        var rect = t.rectTransform.rect;
        float w = rect.width - t.margin.x - t.margin.z;
        float h = rect.height - t.margin.y - t.margin.w;
        if (w <= 1f || string.IsNullOrEmpty(t.text)) return true;
        bool wrap = t.textWrappingMode != TextWrappingModes.NoWrap;
        Vector2 pref = wrap ? t.GetPreferredValues(t.text, w, 0f) : t.GetPreferredValues(t.text);
        if (pref.x > w + 0.5f) return false;
        return h <= 1f || pref.y <= h + 0.5f;
    }

    private static float Scale(float size, float factor) => Mathf.Round(size * (size > 40f ? 1.1f : factor));
}
