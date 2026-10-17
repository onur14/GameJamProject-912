using UnityEngine;

// Small cross-scene snapshot. The preparation scene always starts a fresh run.
public static class PreparationRun
{
    public const int ActivityCount = 5;
    public static bool tsunamiComplete { get; private set; }
    public const string ScenePath = "Assets/Scenes/Preparation.unity";
    public const string OperatorScene = "Assets/Working Folder/Taha and Yasin/Onur/Onur Test Place Integrated.unity";
    public static bool bagComplete { get; private set; }
    public static bool dropCoverHoldComplete { get; private set; }
    public static bool floodComplete { get; private set; }
    public static bool smokeDetectorComplete { get; private set; }
    public static bool Visited { get; private set; }
    public static int Health { get; private set; } = 100;
    public static int Mistakes { get; private set; }
    public static int CompletedCount => (bagComplete?1:0)+(dropCoverHoldComplete?1:0)+(floodComplete?1:0)+(smokeDetectorComplete?1:0)+(tsunamiComplete?1:0);
    public static bool preparationComplete => CompletedCount == ActivityCount;
    public static string Destination { get; private set; } = OperatorScene;
    public static int TakeNumber { get; private set; }
    public static bool IsRecording { get; private set; }
    public static double LastTakeSeconds { get; private set; }
    public static double BestTakeSeconds { get; private set; }
    public static bool LastTakeImproved { get; private set; }
    public static bool RewardEarned { get; private set; }
    public static bool Published { get; private set; }
    public static int TeamBonus => Published && RewardEarned ? 1 : 0;
    private static double recordingStarted;
    private static int bestHealth, bestMistakes;
    public static double RecordingSeconds => IsRecording ? System.Math.Max(0, Time.realtimeSinceStartupAsDouble-recordingStarted) : LastTakeSeconds;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        bagComplete=dropCoverHoldComplete=floodComplete=smokeDetectorComplete=tsunamiComplete=false;
        Health=100; Mistakes=0; Visited=false; Destination=OperatorScene;
        TakeNumber=0;IsRecording=false;LastTakeSeconds=BestTakeSeconds=0;
        LastTakeImproved=RewardEarned=Published=false;bestHealth=100;bestMistakes=0;
    }
    public static void Begin(string destination = null)
    {
        Reset(); Visited=true;
        if (!string.IsNullOrEmpty(destination)) Destination=destination;
        ShiftReportFlow.Clear();
    }
    public static bool IsComplete(int index) => index==0?bagComplete:index==1?dropCoverHoldComplete:index==2?floodComplete:index==3?smokeDetectorComplete:index==4&&tsunamiComplete;
    public static bool Complete(int index)
    {
        if(!IsRecording||index<0||index>=ActivityCount||IsComplete(index))return false;
        switch(index){case 0:bagComplete=true;break;case 1:dropCoverHoldComplete=true;break;case 2:floodComplete=true;break;case 3:smokeDetectorComplete=true;break;case 4:tsunamiComplete=true;break;}
        AdjustHealth(5); return true;
    }
    public static bool StartTake()
    {
        if(IsRecording||Published)return false;
        bagComplete=dropCoverHoldComplete=floodComplete=smokeDetectorComplete=tsunamiComplete=false;
        Health=100;Mistakes=0;LastTakeImproved=false;TakeNumber++;
        recordingStarted=Time.realtimeSinceStartupAsDouble;IsRecording=true;return true;
    }
    public static void AbortTake(){if(IsRecording){LastTakeSeconds=RecordingSeconds;IsRecording=false;}}
    public static bool FinishTake()
    {
        if(!IsRecording||!preparationComplete)return false;
        LastTakeSeconds=RecordingSeconds;IsRecording=false;
        LastTakeImproved=BestTakeSeconds>0 && LastTakeSeconds<BestTakeSeconds;
        if(LastTakeImproved)RewardEarned=true;
        if(BestTakeSeconds<=0||LastTakeSeconds<BestTakeSeconds)
        {BestTakeSeconds=LastTakeSeconds;bestHealth=Health;bestMistakes=Mistakes;}
        return true;
    }
    public static bool Publish()
    {
        if(Published||IsRecording||!preparationComplete||BestTakeSeconds<=0)return false;
        Published=true;Health=bestHealth;Mistakes=bestMistakes;return true;
    }
    public static string FormatDuration(double seconds)
    {
        int hundredths=(int)System.Math.Floor(System.Math.Max(0,seconds)*100);
        return $"{hundredths/6000:00}:{hundredths/100%60:00}.{hundredths%100:00}";
    }
    public static void AdjustHealth(int delta)
    {
        if(delta<0)Mistakes++;
        Health=Mathf.Clamp(Health+delta,0,100);
    }
    public static void StartNewGame(string operatorScene = null, float fadeOut = .35f, float fadeIn = .4f)
    {
        if(SceneFadeTransition.IsTransitioning || !Application.CanStreamedLevelBeLoaded(ScenePath))return;
        Begin(operatorScene); Time.timeScale=1;
        SceneFadeTransition.LoadScene(ScenePath,fadeOut,fadeIn);
    }
}
