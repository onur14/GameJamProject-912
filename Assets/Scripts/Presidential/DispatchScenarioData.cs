using System;
using UnityEngine;

// Existing citizen content can be entered without modifying the briefing system.
[CreateAssetMenu(menuName = "GameJam/Vatandaş Senaryosu")]
public sealed class DispatchScenarioData : ScriptableObject
{
    [Serializable] public struct Line { [TextArea(2, 6)] public string text; public bool fromOperator; }
    public string scenarioId;
    public bool readyForPlay = true;
    [TextArea(2, 5)] public string sourceNotes;
    [Header("Caller information")]
    public string callerName;
    public string callerLocation;
    public string emergencyType;
    [TextArea(1, 3)] public string situationSummary;
    public Line[] opening;
    public string choiceA, choiceB;
    public bool correctChoiceIsB = true;
    public DispatchResources.ResourceType resourceA, resourceB;
    public bool dispatchA = true, dispatchB = true;
    public Line[] outcomeA, outcomeB;
}
