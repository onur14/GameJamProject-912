using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShiftReportFlow
{
    public const string MenuScene = "Assets/Working Folder/Onur/OnurAnaMenu.unity";
    public static ShiftReportData Pending { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear() { Pending = null; }

    public static bool Complete(PresidentialBriefingManager briefing, CityHealth health, MiniGameWindow minigames)
    {
        if (!briefing || briefing.CompletedScenarios != briefing.TotalScenarios || SceneFadeTransition.IsTransitioning || Pending != null
            || !Application.CanStreamedLevelBeLoaded(MenuScene)) return false;
        Pending = ShiftReportData.Capture(briefing, Object.FindAnyObjectByType<QuickQuestionManager>(), minigames, health, SceneManager.GetActiveScene().path);
        if (minigames) minigames.Close();
        Time.timeScale = 1;
        SceneFadeTransition.LoadScene(MenuScene, .45f, .65f);
        return true;
    }
}
