using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DispatchTempoInstaller
{
    private static void Float(Object o,string field,float value){var so=new SerializedObject(o);so.FindProperty(field).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    private static void Ref(Object o,string field,Object value){var so=new SerializedObject(o);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    [MenuItem("GameJam/Tempo ve Kaynak Arayüzünü Güncelle")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying){Debug.LogError("Önce Play Mode'dan çıkın.");return;}
        var phone=Object.FindFirstObjectByType<DispatchCallFlow>();if(!phone)return;
        var canvas=phone.GetComponentInParent<Canvas>();
        Float(phone,"firstRingDelay",.55f);Float(phone,"computerFocusDuration",.42f);Float(phone,"conversationFadeDuration",.18f);
        var so=new SerializedObject(phone);var viewport=(RectTransform)so.FindProperty("conversationViewport").objectReferenceValue;
        // Measured against the actual 1672x941 room artwork: exclude monitor sidebar and bezel.
        viewport.anchorMin=new Vector2(.435f,.327f);viewport.anchorMax=new Vector2(.671f,.573f);
        viewport.offsetMin=viewport.offsetMax=Vector2.zero;EditorUtility.SetDirty(viewport);
        var flow=Object.FindFirstObjectByType<OyunAkisiOnur>();Float(flow,"animasyonSuresi",.15f);
        var resources=Object.FindFirstObjectByType<DispatchResources>();
        var root=canvas.transform.Find("Canlı Kaynak Paneli");
        if(!root)
        {
            var hud=new GameObject("Canlı Kaynak Paneli",typeof(RectTransform),typeof(Canvas));root=hud.transform;root.SetParent(canvas.transform,false);
            var overlay=hud.GetComponent<Canvas>();overlay.overrideSorting=true;overlay.sortingOrder=500;
            var component=hud.AddComponent<DispatchResourceHud>();Ref(component,"resources",resources);
            var view=Object.FindFirstObjectByType<OnurConversationView>();var source=new SerializedObject(view).FindProperty("dialogueText").objectReferenceValue as TMPro.TMP_Text;
            Ref(component,"font",source.font);
            // Hide former counter readouts, retaining all serialized references for compatibility.
            var resSo=new SerializedObject(resources);
            foreach(string field in new[]{"ambulancesText","firefightersText","policeText"})
            {
                var label=resSo.FindProperty(field).objectReferenceValue as TMPro.TMP_Text;
                if(label){label.gameObject.SetActive(false);EditorUtility.SetDirty(label.gameObject);}
            }
            foreach(var text in canvas.GetComponentsInChildren<TMPro.TMP_Text>(true))
                if(text.text=="AMBULANS"||text.text=="İTFAİYE"||text.text=="KURTARMA")
                {text.gameObject.SetActive(false);EditorUtility.SetDirty(text.gameObject);}
        }
        var rect=(RectTransform)root;rect.anchorMin=new Vector2(.855f,.36f);rect.anchorMax=new Vector2(.99f,.94f);
        rect.offsetMin=rect.offsetMax=Vector2.zero;EditorUtility.SetDirty(rect);
        var broadcast=Object.FindFirstObjectByType<PresidentialBroadcastView>();
        if(broadcast)
        {
            Float(broadcast,"charactersPerSecond",120);
            var br=(RectTransform)broadcast.transform;br.anchorMin=Vector2.zero;br.anchorMax=new Vector2(.84f,1);br.offsetMin=br.offsetMax=Vector2.zero;
            EditorUtility.SetDirty(br);
        }
        EditorSceneManager.MarkSceneDirty(phone.gameObject.scene);EditorSceneManager.SaveScene(phone.gameObject.scene);
    }
    public static void ApplyMenuPace()
    {
        foreach(var menu in Object.FindObjectsByType<AnaMenü>(FindObjectsSortMode.None))
        {Float(menu,"menuFadeDuration",.3f);Float(menu,"gameplayFadeInDuration",.3f);EditorUtility.SetDirty(menu);}
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
