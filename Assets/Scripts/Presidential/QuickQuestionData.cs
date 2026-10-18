using System;
using UnityEngine;

[Serializable]
public class QuickQuestionConfig
{
    public string id = "QQ-01";
    public string targetScenarioId = "D-01";
    public string categoryTitle = "SAHA TALEBİ";
    [TextArea(2, 4)]
    public string questionText = "Bölge 3'teki ekip ek ambulans talep ediyor.";

    [Header("Choice A")]
    public string choiceALabel = "GÖNDER (-1)";
    public DispatchResourceEffect choiceAEffect = new DispatchResourceEffect { ambulances = -1 };
    public string choiceAFeedback = "GÖNDERİLDİ";

    [Header("Choice B")]
    public string choiceBLabel = "REDDET";
    public DispatchResourceEffect choiceBEffect = new DispatchResourceEffect();
    public string choiceBFeedback = "TALEP REDDEDİLDİ";
    [Min(0)] public int choiceBHealthDamage;

    [Header("Information verification")]
    public bool isDisinformation;
    [Range(0, 1)] public int correctChoiceIndex = 1;
    [Min(0)] public int wrongAnswerHealthDamage;
    [Min(0)] public int timeoutHealthDamage;

    [Header("Timing")]
    [Tooltip("Response limit in seconds.")]
    [Min(1f)] public float timerDuration = 7f;
    [Tooltip("Minimum delay before appearing after scenario conversation starts.")]
    [Min(0f)] public float appearanceDelayMin = 2.5f;
    [Tooltip("Maximum delay before appearing after scenario conversation starts.")]
    [Min(0f)] public float appearanceDelayMax = 4.0f;

    [Header("Feedback")]
    public string timeoutFeedback = "YANITLANMADI";

    public QuickQuestionConfig Clone()
    {
        return new QuickQuestionConfig
        {
            id = id,
            targetScenarioId = targetScenarioId,
            categoryTitle = categoryTitle,
            questionText = questionText,
            choiceALabel = choiceALabel,
            choiceAEffect = choiceAEffect,
            choiceAFeedback = choiceAFeedback,
            choiceBLabel = choiceBLabel,
            choiceBEffect = choiceBEffect,
            choiceBFeedback = choiceBFeedback,
            choiceBHealthDamage = choiceBHealthDamage,
            timerDuration = timerDuration,
            appearanceDelayMin = appearanceDelayMin,
            appearanceDelayMax = appearanceDelayMax,
            isDisinformation = isDisinformation,
            correctChoiceIndex = correctChoiceIndex,
            wrongAnswerHealthDamage = wrongAnswerHealthDamage,
            timeoutHealthDamage = timeoutHealthDamage,
            timeoutFeedback = timeoutFeedback
        };
    }
}

[CreateAssetMenu(fileName = "QuickQuestion", menuName = "GameJam/Quick Question Data")]
public sealed class QuickQuestionData : ScriptableObject
{
    public QuickQuestionConfig config = new QuickQuestionConfig();
}
