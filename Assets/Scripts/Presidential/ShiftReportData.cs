using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class ShiftScenarioOutcome
{
    public string scenarioId;
    public string callerName;
    public bool correct;
}

// A snapshot of the existing run, not another resource/scoring system during gameplay.
public sealed class ShiftReportData
{
    public int Completed { get; private set; }
    public int Correct { get; private set; }
    public int Missed { get; private set; }
    public int Answered { get; private set; }
    public float AverageSeconds { get; private set; }
    public bool HasResponseTimes => Answered > 0;
    public int Ambulances { get; private set; }
    public int Firefighters { get; private set; }
    public int RescueTeams { get; private set; }
    public int SuccessfulTasks { get; private set; }
    public int FailedTasks { get; private set; }
    public int UnfinishedTasks { get; private set; }
    public int Health { get; private set; }
    public int Score { get; private set; }
    public string Grade => GradeFor(Score);
    public string OperationStatus => Ambulances == 0 || Firefighters == 0 || RescueTeams == 0 || Health < 35
        ? "KRİTİK" : Health < 70 || Score < 85 ? "STABİL" : "KONTROL ALTINDA";
    public string GameplayScene { get; private set; }
    public string[] EventSummary { get; private set; }
    public bool SlowResponses { get; private set; }
    public int IncorrectDecisions { get; private set; }
    public int TimedOutDecisions { get; private set; }
    public int VerificationMistakes { get; private set; }
    public int IncorrectMiniActions { get; private set; }

    public static string GradeFor(int score) => score >= 85 ? "A" : score >= 70 ? "B" : score >= 50 ? "C" : "D";
    public static int CalculateScore(int incorrect, int missed, int failedTasks, int emptyServices, bool slow)
        => Mathf.Clamp(100 - incorrect * 10 - missed * 5 - failedTasks * 8 - emptyServices * 3 - (slow ? 5 : 0), 0, 100);

    public static ShiftReportData Capture(PresidentialBriefingManager briefing, QuickQuestionManager questions,
        MiniGameWindow minigames, CityHealth health, string gameplayScene)
    {
        if (!briefing || !briefing.Resources) throw new ArgumentException("The report requires the actual shift resources.");
        var responses = questions != null ? questions.Results.Where(r => r.answered && !r.timedOut && !r.cancelled).ToArray() : Array.Empty<QuickQuestionResult>();
        var resources = briefing.Resources;
        var data = new ShiftReportData {
            Completed = briefing.CompletedScenarios, Correct = briefing.CorrectDecisions,
            Missed = questions != null ? questions.Results.Count(r => r.timedOut && !r.cancelled) : 0,
            Answered = responses.Length,
            AverageSeconds = responses.Length > 0 ? responses.Average(r => r.responseTime) : 0,
            Ambulances = resources.Ambulances, Firefighters = resources.Firefighters, RescueTeams = resources.RescueTeams,
            SuccessfulTasks = minigames ? minigames.SuccessfulTasks : 0,
            FailedTasks = minigames ? minigames.FailedTasks : 0,
            UnfinishedTasks = minigames ? minigames.AvailableTasks - minigames.SuccessfulTasks - minigames.FailedTasks : 0,
            Health = health ? health.HealthPercent : 100, GameplayScene = gameplayScene,
            IncorrectDecisions = health ? health.WrongDecisionCount : briefing.CompletedScenarios - briefing.CorrectDecisions,
            TimedOutDecisions = health ? health.TimedOutDecisionCount : 0,
            IncorrectMiniActions = PreparationRun.Visited ? PreparationRun.Mistakes : minigames ? minigames.IncorrectActions : 0,
            SlowResponses = responses.Length > 0 && responses.Average(r => r.responseTime / Mathf.Max(.01f, r.totalDuration)) > .8f
        };
        if (questions != null)
            foreach (var response in responses)
                for (int index=0;index<8;index++) for(int question=0;question<3;question++)
                {
                    var config=questions.GetQuestionForScenario(index,question==1,question==2);
                    if(config!=null && config.id==response.questionId && config.isDisinformation && response.selectedChoice!=config.correctChoiceIndex)
                        data.VerificationMistakes++;
                }
        int empty = (data.Ambulances == 0 ? 1 : 0) + (data.Firefighters == 0 ? 1 : 0) + (data.RescueTeams == 0 ? 1 : 0);
        data.Score = CalculateScore(data.Completed - data.Correct, data.Missed, data.FailedTasks, empty, data.SlowResponses);
        var summary = new List<string>();
        var last = briefing.ScenarioOutcomes.LastOrDefault();
        if (last != null) summary.Add(last.scenarioId + " — " + (last.correct ? "Doğru yönlendirme yapıldı." : "Yönlendirme yeniden değerlendirilmeli."));
        summary.Add($"İkincil talepler: {data.Answered} yanıtlandı, {data.Missed} yanıtsız kaldı.");
        summary.Add($"Mini görevler: {data.SuccessfulTasks} tamamlandı, {data.FailedTasks} başarısız, {data.UnfinishedTasks} sonuçlanmadı.");
        data.EventSummary = summary.ToArray();
        return data;
    }

    public string[] ExplainOutcome(bool won)
    {
        var lines=new List<string>();
        if(won)
        {
            lines.Add(Completed>0 && Correct*4>=Completed*3 ? "Doğru müdahale kararları vardiyayı destekledi." : "Vardiya tamamlandı; bazı yönlendirmeler hatalıydı.");
            lines.Add(Missed>0 ? $"{Missed} destek talebi yanıtsız kaldı." : HasResponseTimes ? SlowResponses ? "Destek taleplerine yanıtlar gecikti." : "Destek taleplerine zamanında yanıt verildi." : "İkincil taleplere ait yanıt kaydı bulunmuyor.");
            int minimum=Mathf.Min(Ambulances,Mathf.Min(Firefighters,RescueTeams));
            lines.Add(minimum==0 ? "Bazı hizmetlerde ekip kapasitesi tükendi." : minimum<3 ? "Bazı hizmetlerde yedek ekip sayısı azaldı." : "Kaynak yönetimi genel olarak dengeliydi.");
            if(FailedTasks>0)lines.Add($"{FailedTasks} hazırlık görevi başarısız sonuçlandı.");
            else if(SuccessfulTasks>0)lines.Add($"{SuccessfulTasks} hazırlık görevi başarıyla tamamlandı.");
        }
        else
        {
            if(IncorrectDecisions>0)lines.Add($"{IncorrectDecisions} hatalı ana yönlendirme yapıldı.");
            if(VerificationMistakes>0)lines.Add("Bilgi teyidinde hatalı kararlar verildi.");
            if(Missed>0)lines.Add($"{Missed} destek talebi yanıtsız kaldı.");
            if(TimedOutDecisions>0)lines.Add($"{TimedOutDecisions} ana çağrıda karar süresi aşıldı.");
            if(IncorrectMiniActions>0)lines.Add("Mini görevlerde hatalı işlemler yapıldı.");
            else if(FailedTasks>0)lines.Add($"{FailedTasks} hazırlık görevi başarısız sonuçlandı.");
            if(Ambulances==0||Firefighters==0||RescueTeams==0)lines.Add("Bazı hizmetlerde ekip kapasitesi tükendi.");
            if(SlowResponses)lines.Add("Destek taleplerine yanıtlar gecikti.");
            if(lines.Count<2)lines.Add(Health==0 ? "Halk bilinci %0'a düştü; son vakanın başlangıç kaydı korunuyor." : "Vardiya başarı koşulları sağlanamadı.");
            if(lines.Count<2)lines.Add($"Tamamlanan çağrı: {Completed}; doğru karar: {Correct}.");
        }
        return lines.Take(4).ToArray();
    }
}
