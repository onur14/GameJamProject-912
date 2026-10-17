using System;
using UnityEngine;

[Serializable]
public class QuickQuestionResult
{
    public string questionId;
    public string targetScenarioId;
    public int scenarioIndex;
    public int scenarioRevision;
    public bool cancelled;
    public bool answered;
    public bool timedOut;
    [Tooltip("0 for Choice A, 1 for Choice B, -1 for timeout")]
    public int selectedChoice = -1;
    public string selectedChoiceLabel;
    public float responseTime;
    public float totalDuration;
    public DispatchResourceEffect appliedEffect;

    public override string ToString()
    {
        if (timedOut) return $"[{questionId}] ({targetScenarioId}) ZAMAN AŞIMI (yanıt {responseTime:F1}s, süre {totalDuration:F1}s)";
        return $"[{questionId}] ({targetScenarioId}) {selectedChoiceLabel} ({responseTime:F1}s)";
    }
}
