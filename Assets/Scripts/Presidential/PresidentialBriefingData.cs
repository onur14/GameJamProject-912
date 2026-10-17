using System;
using UnityEngine;

[Serializable]
public sealed class ResourceOffer
{
    public string title;
    [TextArea(2, 4)] public string rationale;
    public int ambulances, firefighters, thirdTeam;
    public bool IsValid(DispatchResources resources) => resources.CanApply(ambulances, firefighters, thirdTeam);
    public bool Apply(DispatchResources resources) => resources.TryApply(ambulances, firefighters, thirdTeam);
}

[CreateAssetMenu(menuName = "GameJam/Başkan Bilgilendirmesi")]
public sealed class PresidentialBriefingData : ScriptableObject
{
    public string briefingId;
    [Range(2, 8)] public int afterScenario;
    public string heading;
    [TextArea(3, 6)] public string[] dialogueLines;
    public ResourceOffer planA, planB;
    [TextArea(2, 4)] public string closing;
    public bool endsShift;
}
