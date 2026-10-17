using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DispatchCurseInstaller
{
    private const string Root="Assets/Working Folder/Taha and Yasin/Onur";
    [MenuItem("GameJam/Taha Yasin/Ceza ve Senaryo Kaydını Bağla")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Önce Play Mode'dan çıkın.");
        var scene=EditorSceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Önce açık sahneyi kaydedin.");
        EditorSceneManager.OpenScene(TahaYasinDispatchSetup.Game);
        var health=UnityEngine.Object.FindFirstObjectByType<CityHealth>(FindObjectsInactive.Include);
        var flow=UnityEngine.Object.FindFirstObjectByType<OyunAkisiOnur>(FindObjectsInactive.Include);
        var briefing=UnityEngine.Object.FindFirstObjectByType<PresidentialBriefingManager>(FindObjectsInactive.Include);
        var phone=UnityEngine.Object.FindFirstObjectByType<DispatchCallFlow>(FindObjectsInactive.Include);
        var questions=UnityEngine.Object.FindFirstObjectByType<QuickQuestionManager>(FindObjectsInactive.Include);
        var director=health.GetComponent<DispatchCurseDirector>();if(!director)director=health.gameObject.AddComponent<DispatchCurseDirector>();
        var ds=new SerializedObject(director);
        Set(ds,"flow",flow);Set(ds,"briefing",briefing);Set(ds,"resources",briefing.Resources?briefing.Resources:UnityEngine.Object.FindFirstObjectByType<DispatchResources>(FindObjectsInactive.Include));
        Set(ds,"awareness",health);Set(ds,"questions",questions);Set(ds,"phone",phone);
        var font=new SerializedObject(health).FindProperty("healthFont").objectReferenceValue as TMP_FontAsset;
        if(!font)font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Working Folder/Taha and Yasin/Fonts/TY_VCR_OSD_MONO_SDF.asset");
        if(!font)throw new InvalidOperationException("Sahnenin pixel fontu eksik.");
        Set(ds,"font",font);
        Set(ds,"slownessEmblem",ImportEmblem(Root+"/Curses/SlownessPenalty.png"));Set(ds,"fogEmblem",ImportEmblem(Root+"/Curses/InformationFog.png"));
        ds.FindProperty("slownessAtScenario").intValue=2;ds.FindProperty("fogAtScenario").intValue=5;ds.ApplyModifiedProperties();
        var radio=UnityEngine.Object.FindFirstObjectByType<Cizirti>(FindObjectsInactive.Include);if(!radio)radio=health.gameObject.AddComponent<Cizirti>();
        if(!radio.CizirtiSesi)radio.CizirtiSesi=Source(health.transform,"Telsiz Ortamı");
        if(!radio.TiklamaSesi || radio.TiklamaSesi==radio.CizirtiSesi)radio.TiklamaSesi=Source(health.transform,"Telefon Aç Kapat");
        radio.CizirtiSesi.clip=Clip("Assets/SoundEffect/telefondayken.wav");radio.CizirtiSesi.loop=true;radio.CizirtiSesi.volume=.08f;radio.CizirtiSesi.playOnAwake=false;radio.CizirtiSesi.spatialBlend=0;
        radio.TiklamaSesi.playOnAwake=false;radio.TiklamaSesi.loop=false;radio.TiklamaSesi.volume=.85f;radio.TiklamaSesi.spatialBlend=0;
        radio.TelsizAcilisSesi=Clip("Assets/SoundEffect/TelefonAcma.wav");radio.TelsizKapanisSesi=Clip("Assets/SoundEffect/TelefonuKapama.wav");EditorUtility.SetDirty(radio);
        var ps=new SerializedObject(phone);var ring=ps.FindProperty("ringAudioSource").objectReferenceValue as AudioSource;
        if(!ring)ring=Source(phone.transform,"Telefon Zili");ring.playOnAwake=false;ring.spatialBlend=0;ring.volume=.23f;
        Set(ps,"ringAudioSource",ring);Set(ps,"ringClip",Clip("Assets/SoundEffect/Telefon.wav"));ps.FindProperty("ringVolume").floatValue=.23f;ps.ApplyModifiedProperties();
        var source=Source(health.transform,"Bildirim Tıkları");source.playOnAwake=false;source.loop=false;source.volume=.45f;source.spatialBlend=0;
        var cues=health.GetComponent<DispatchSceneAudioCues>();if(!cues)cues=health.gameObject.AddComponent<DispatchSceneAudioCues>();var cs=new SerializedObject(cues);
        Set(cs,"phoneConversation",Clip("Assets/SoundEffect/telefondayken.wav"));Set(cs,"phone",phone);Set(cs,"flow",flow);Set(cs,"awareness",health);Set(cs,"radio",radio);Set(cs,"notices",source);Set(cs,"blink",Clip(Root+"/Audio/NotificationBlink.wav"));cs.ApplyModifiedProperties();
        var questionView=UnityEngine.Object.FindFirstObjectByType<QuickQuestionView>(FindObjectsInactive.Include);var qs=new SerializedObject(questionView);
        Set(qs,"audioSource",source);Set(qs,"arrivalClip",Clip(Root+"/Audio/NotificationBlink.wav"));Set(qs,"warningTickClip",Clip(Root+"/Audio/NotificationBlink.wav"));qs.ApplyModifiedProperties();
        var main=UnityEngine.Object.FindFirstObjectByType<OnurConversationView>(FindObjectsInactive.Include);Typing(main,Root+"/Audio/OperatorTick.wav",.065f);
        Typing(UnityEngine.Object.FindFirstObjectByType<PresidentialBroadcastView>(FindObjectsInactive.Include),Root+"/Audio/PresidentTick.wav",.055f);
        var result=UnityEngine.Object.FindFirstObjectByType<RunResultView>(FindObjectsInactive.Include);if(result){var rs=new SerializedObject(result);Set(rs,"cizirti",radio);rs.ApplyModifiedProperties();}
        foreach(var go in health.gameObject.scene.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<TMP_Text>(true))
        {
            t.text=t.text.Replace("ŞEHİR SAĞLIĞI","HALK BİLİNCİ").Replace("Şehir sağlığı","Halk bilinci").Replace("şehir sağlığı","halk bilinci");EditorUtility.SetDirty(t);
        }
        EditorUtility.SetDirty(radio.CizirtiSesi);EditorUtility.SetDirty(radio.TiklamaSesi);EditorUtility.SetDirty(ring);EditorUtility.SetDirty(source);
        EditorUtility.SetDirty(director);EditorUtility.SetDirty(cues);
        EditorSceneManager.MarkSceneDirty(health.gameObject.scene);EditorSceneManager.SaveScene(health.gameObject.scene);AssetDatabase.SaveAssets();
    }
    private static void Set(SerializedObject o,string n,UnityEngine.Object v){o.FindProperty(n).objectReferenceValue=v;}
    private static AudioClip Clip(string p){var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(p);if(!clip)throw new InvalidOperationException("Ses eksik: "+p);return clip;}
    private static AudioSource Source(Transform parent,string name)
    {
        var child=parent.Find(name);if(child)return child.GetComponent<AudioSource>();
        var go=new GameObject(name,typeof(AudioSource));go.transform.SetParent(parent,false);return go.GetComponent<AudioSource>();
    }
    private static Sprite ImportEmblem(string path)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Point;importer.maxTextureSize=256;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static void Typing(UnityEngine.Object owner,string path,float volume)
    {
        var s=new SerializedObject(owner);s.FindProperty("typingAudio.clip").objectReferenceValue=Clip(path);
        s.FindProperty("typingAudio.volume").floatValue=volume;s.FindProperty("typingAudio.characterInterval").intValue=4;s.ApplyModifiedProperties();
    }
}
