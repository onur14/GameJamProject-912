using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PresidentialScenarioImporter
{
    [Serializable] private class Source { public Scenario[] scenarios; }
    [Serializable] private class Scenario
    {
        public string scenarioId,sourceNotes,choiceA,choiceB;
        public bool readyForPlay,correctChoiceIsB,dispatchA,dispatchB;
        public int resourceA,resourceB;
        public DispatchScenarioData.Line[] opening,outcomeA,outcomeB;
    }
    public static string ImportAndAssign()
    {
        if(Application.isPlaying)return "Exit Play Mode first.";
        var source=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/PresidentialData/CitizenSource.json");
        if(!source)return "Citizen source JSON not imported yet.";
        var data=JsonUtility.FromJson<Source>(source.text);
        const string folder="Assets/PresidentialData/Scenarios";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/PresidentialData","Scenarios");
        foreach(var s in data.scenarios)
        {
            string path=folder+"/"+s.scenarioId+".asset";
            var asset=AssetDatabase.LoadAssetAtPath<DispatchScenarioData>(path);
            if(asset)continue; // Preserve hand edits on subsequent runs.
            asset=ScriptableObject.CreateInstance<DispatchScenarioData>();
            asset.scenarioId=s.scenarioId;asset.sourceNotes=s.sourceNotes;asset.readyForPlay=s.readyForPlay;
            asset.opening=s.opening;asset.outcomeA=s.outcomeA;asset.outcomeB=s.outcomeB;
            asset.choiceA=s.choiceA;asset.choiceB=s.choiceB;asset.correctChoiceIsB=s.correctChoiceIsB;
            asset.resourceA=(DispatchResources.ResourceType)s.resourceA;asset.resourceB=(DispatchResources.ResourceType)s.resourceB;
            asset.dispatchA=s.dispatchA;asset.dispatchB=s.dispatchB;
            AssetDatabase.CreateAsset(asset,path);
        }
        string[] approved={"D-01","D-02","Y-02","S-01","T-01","F-01","K-01","K-02"};
        var manager=UnityEngine.Object.FindFirstObjectByType<PresidentialBriefingManager>();
        if(!manager)return "No briefing manager in current scene.";
        var so=new SerializedObject(manager);var order=so.FindProperty("scenarioOrder");order.arraySize=8;
        for(int i=0;i<8;i++)order.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<DispatchScenarioData>(folder+"/"+approved[i]+".asset");
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);EditorSceneManager.SaveScene(manager.gameObject.scene);
        AssetDatabase.SaveAssets();
        return "Imported 12 source records; assigned approved eight-scenario order. Y-01 retained as not ready for play.";
    }
}
