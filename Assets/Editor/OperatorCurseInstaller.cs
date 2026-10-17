using UnityEditor;
using UnityEngine;

public static class OperatorCurseInstaller
{
    [MenuItem("GameJam/Operator/Install Curse Audio Assets")]
    public static void Install()
    {
        const string path="Assets/Resources/DispatchSoundLibrary.asset";
        var library=AssetDatabase.LoadAssetAtPath<DispatchSoundLibrary>(path);
        if(!library){library=ScriptableObject.CreateInstance<DispatchSoundLibrary>();AssetDatabase.CreateAsset(library,path);}
        AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SoundEffect/"+name);
        library.phoneRing=Clip("Telefon.wav");library.phoneOpen=Clip("TelefonAcma.wav");library.phoneClose=Clip("TelefonuKapama.wav");
        library.callerGrain=Clip("Generated/CallerGrain.wav");library.presidentGrain=Clip("Generated/PresidentGrain.wav");
        library.notification=Clip("Generated/NotificationBlink.wav");library.warningTick=Clip("Generated/WarningTick.wav");library.curseImpact=Clip("Generated/CurseImpact.wav");
        if(!library.phoneRing||!library.phoneOpen||!library.phoneClose||!library.callerGrain||!library.presidentGrain||!library.notification||!library.warningTick||!library.curseImpact)
            throw new System.InvalidOperationException("Lanet ses kitaplığındaki kaynaklardan biri eksik.");
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
    }
}
