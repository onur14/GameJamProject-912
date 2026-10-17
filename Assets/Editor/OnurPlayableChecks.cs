using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class OnurPlayableChecks
{
    private const string MenuPath = "Assets/Working Folder/Onur/OnurAnaMenu.unity";
    private const string GamePath = "Assets/Working Folder/Onur/Onur Test Place Integrated.unity";
    private const string ReportPath = "Logs/OnurPlayableChecks.txt";
    private static string restoreScene;
    private static bool running;

    static OnurPlayableChecks()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static string ValidateScenes()
    {
        string previous = SceneManager.GetActiveScene().path;
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Açık sahneyi önce kaydedin.");
        try
        {
            var build = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            Require(build.Length == 2 && build[0].path == MenuPath && build[1].path == GamePath,
                "Build sırası Onur menü → oyun");
            EditorSceneManager.OpenScene(MenuPath);
            Require(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length == 1,
                "Menüde tek EventSystem");
            Require(UnityEngine.Object.FindAnyObjectByType<MasterVolumeSettings>() != null, "Genel ses ayarı bağlı");
            var menu = UnityEngine.Object.FindAnyObjectByType<AnaMenü>();
            Require(new SerializedObject(menu).FindProperty("gameplayScene").stringValue == "Onur Test Place Integrated",
                "Menü oyun sahnesine yönleniyor");
            EditorSceneManager.OpenScene(GamePath);
            Require(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length == 1,
                "Oyunda tek EventSystem");
            var manager = UnityEngine.Object.FindAnyObjectByType<QuickQuestionManager>();
            var briefing = UnityEngine.Object.FindAnyObjectByType<PresidentialBriefingManager>();
            var order = new SerializedObject(briefing).FindProperty("scenarioOrder");
            for (int index = 0; index < order.arraySize; index++)
            {
                var scenario = order.GetArrayElementAtIndex(index).objectReferenceValue as DispatchScenarioData;
                var first = manager.GetQuestionForScenario(index);
                var second = manager.GetQuestionForScenario(index, true);
                Require(scenario != null && first != null && second != null
                    && first.targetScenarioId == scenario.scenarioId
                    && second.targetScenarioId == scenario.scenarioId && first.id != second.id,
                    "İki farklı hızlı soru: " + index);
            }
            Require(UnityEngine.Object.FindAnyObjectByType<CallerInfoView>(FindObjectsInactive.Include) != null,
                "Arayan bilgisi bağlı");
            Require(UnityEngine.Object.FindAnyObjectByType<RunResultView>() != null, "Sonuç paneli bağlı");
            Require(UnityEngine.Object.FindAnyObjectByType<MiniGameWindow>() != null, "Minigame penceresi bağlı");
            var resources = UnityEngine.Object.FindAnyObjectByType<DispatchResources>();
            var data = new SerializedObject(resources);
            Require(data.FindProperty("initialFirefighters").intValue == 5
                && data.FindProperty("initialAmbulances").intValue == 5
                && data.FindProperty("initialPolice").intValue == 6, "Kaynak başlangıcı 5/5/6");
            var phone = UnityEngine.Object.FindAnyObjectByType<DispatchCallFlow>();
            Require(new SerializedObject(phone).FindProperty("ringClip").objectReferenceValue != null,
                "Telefon sesi bağlı");
            return "Düzenleyici bağlantı kontrolleri geçti.";
        }
        finally
        {
            if (!string.IsNullOrEmpty(previous) && SceneManager.GetActiveScene().path != previous)
                EditorSceneManager.OpenScene(previous);
        }
    }

    public static void StartSmoke()
    {
        if (running || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play Mode zaten açık.");
        restoreScene = SceneManager.GetActiveScene().path;
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Açık sahneyi önce kaydedin.");
        File.WriteAllText(ReportPath, "Play Mode testi başladı.\n");
        running = true;
        EditorSceneManager.OpenScene(MenuPath);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!running) return;
        if (state == PlayModeStateChange.EnteredPlayMode) _ = RunSmoke();
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            running = false;
            if (!string.IsNullOrEmpty(restoreScene)) EditorSceneManager.OpenScene(restoreScene);
        }
    }

    private static async Task RunSmoke()
    {
        try
        {
            await Wait(() => SceneManager.GetActiveScene().name == "OnurAnaMenu");
            var volume = UnityEngine.Object.FindAnyObjectByType<MasterVolumeSettings>();
            await Capture("menu");
            volume.GetComponentsInChildren<Button>(true).First(button => button.name == "Ayarlar").onClick.Invoke();
            await Capture("settings");
            volume.GetComponentsInChildren<Button>(true).First(button => button.name == "Geri").onClick.Invoke();
            var slider = volume.GetComponentInChildren<Slider>(true);
            float saved = AudioListener.volume;
            slider.value = 0;
            Require(AudioListener.volume == 0, "Genel ses %0 tamamen susturuyor");
            slider.value = saved;
            UnityEngine.Object.FindAnyObjectByType<AnaMenü>().OyunBaslat();
            await Wait(() => SceneManager.GetActiveScene().name == "Onur Test Place Integrated"
                && !SceneFadeTransition.IsTransitioning, 30000);
            var phone = UnityEngine.Object.FindAnyObjectByType<DispatchCallFlow>();
            await Wait(() => phone.State == DispatchCallFlow.CallState.IncomingCall);
            phone.AnswerPhone();
            await Wait(() => phone.State == DispatchCallFlow.CallState.Conversation);
            var questions = UnityEngine.Object.FindAnyObjectByType<QuickQuestionManager>();
            var questionView = UnityEngine.Object.FindAnyObjectByType<QuickQuestionView>();
            await Wait(() => questionView.Ready, 30000);
            Require(questions.CurrentQuestion.id == "QQ-01", "İlk soru sıralı başladı");
            questionView.ButtonB.onClick.Invoke();
            await Wait(() => questionView.Ready && questions.CurrentQuestion.id == "QQ-09", 30000);
            Require(questions.Results.Count(result => result.scenarioRevision == 0) == 1,
                "İkinci soru ilk yanıttan sonra açıldı");
            questionView.ButtonB.onClick.Invoke();
            await Wait(() => questions.QuickQuestionsComplete);
            Require(questions.Results.Count(result => result.scenarioRevision == 0) == 2,
                "İki soru birer kez sonuçlandı");
            var conversation = UnityEngine.Object.FindAnyObjectByType<OyunAkisiOnur>();
            var briefing = UnityEngine.Object.FindAnyObjectByType<PresidentialBriefingManager>();
            var scenarios = (DispatchScenarioData[])typeof(PresidentialBriefingManager)
                .GetField("scenarioOrder", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(briefing);
            var completed = typeof(PresidentialBriefingManager)
                .GetField("<CompletedScenarios>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int index = 1; index < scenarios.Length; index++)
            {
                completed.SetValue(briefing, index);
                if (index == scenarios.Length - 1)
                    questions.GetQuestionForScenario(index, true).timerDuration = 1;
                conversation.BeginScenario(scenarios[index]);
                int revision = conversation.ScenarioRevision;
                string firstId = questions.GetQuestionForScenario(index).id;
                string secondId = questions.GetQuestionForScenario(index, true).id;
                await Wait(() => questionView.Ready && questions.CurrentQuestion.id == firstId, 30000);
                questionView.ButtonB.onClick.Invoke();
                await Wait(() => questionView.Ready && questions.CurrentQuestion.id == secondId, 30000);
                if (index != scenarios.Length - 1) questionView.ButtonB.onClick.Invoke();
                await Wait(() => questions.QuickQuestionsComplete, 30000);
                Require(questions.Results.Count(result => result.scenarioRevision == revision) == 2,
                    "Senaryo " + scenarios[index].scenarioId + " iki soru sonuçlandırdı");
            }
            Require(questions.Results.Last().timedOut, "İkinci soru süre dolunca da çözülüyor");
            var caller = UnityEngine.Object.FindAnyObjectByType<CallerInfoView>(FindObjectsInactive.Include);
            Require(caller.gameObject.activeSelf, "Aktif arayan bilgisi görünüyor");
            await Capture("conversation");
            var window = UnityEngine.Object.FindAnyObjectByType<MiniGameWindow>();
            var bag = UnityEngine.Object.FindAnyObjectByType<BagMiniGame>(FindObjectsInactive.Include);
            window.OpenBag();
            await Capture("bag");
            Require(phone.IsPaused && questionView.IsPaused, "Minigame çağrı ve hızlı soruyu duraklatıyor");
            int attempts = bag.AttemptsLeft;
            bag.Check();
            Require(bag.AttemptsLeft == attempts - 1, "Hatalı kontrol tek hak harcıyor");
            window.Close();
            window.OpenBag();
            Require(bag.AttemptsLeft == attempts - 1, "Kapat/aç hakları sıfırlamıyor");
            window.Close();
            var safety = UnityEngine.Object.FindAnyObjectByType<SafetyMiniGame>(FindObjectsInactive.Include);
            window.OpenSafety();
            await Capture("safety");
            safety.SelectPoint(0); safety.SelectOption(1);
            safety.SelectPoint(1); safety.SelectOption(1);
            safety.SelectPoint(2); safety.SelectOption(0);
            safety.Check();
            Require(safety.Succeeded && window.SuccessfulTasks == 1, "Güvenlik kararı bir kez tamamlanıyor");
            window.Close();
            var health = UnityEngine.Object.FindAnyObjectByType<CityHealth>();
            typeof(CityHealth).GetMethod("EndGame", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(health, null);
            var resultView = UnityEngine.Object.FindAnyObjectByType<RunResultView>();
            await Capture("result");
            Require(resultView.GetComponentsInChildren<Button>(true).Any(button => button.name == "Tekrar Oyna")
                && health.IsGameOver, "Kayıp ekranı ve tekrar oynama bağlı");
            resultView.GetComponentsInChildren<Button>(true).First(button => button.name == "Tekrar Oyna").onClick.Invoke();
            await Wait(() => SceneManager.GetActiveScene().name == "Onur Test Place Integrated"
                && UnityEngine.Object.FindAnyObjectByType<CityHealth>() != health);
            Require(UnityEngine.Object.FindAnyObjectByType<CityHealth>().HealthPercent == 100,
                "Tekrar oyna şehir sağlığını sıfırlıyor");
            var freshResources = UnityEngine.Object.FindAnyObjectByType<DispatchResources>();
            Require(freshResources.Firefighters == 5 && freshResources.Ambulances == 5
                && freshResources.Police == 6, "Tekrar oyna ekipleri sıfırlıyor");
            Require(!UnityEngine.Object.FindAnyObjectByType<CallerInfoView>(FindObjectsInactive.Include).gameObject.activeSelf,
                "Yeni çağrı öncesi eski arayan görünmüyor");
            var freshWindow = UnityEngine.Object.FindAnyObjectByType<MiniGameWindow>();
            freshWindow.OpenBag();
            Require(UnityEngine.Object.FindAnyObjectByType<BagMiniGame>(FindObjectsInactive.Include).AttemptsLeft == attempts,
                "Tekrar oyna minigame haklarını sıfırlıyor");
            freshWindow.Close();
            var freshResult = UnityEngine.Object.FindAnyObjectByType<RunResultView>();
            freshResult.ShowSuccess();
            freshResult.GetComponentsInChildren<Button>(true).First(button => button.name == "Ana Menü").onClick.Invoke();
            await Wait(() => SceneManager.GetActiveScene().name == "OnurAnaMenu");
            Require(Mathf.Approximately(AudioListener.volume, saved), "Menüye dönüş ses ayarını koruyor");
            File.AppendAllText(ReportPath, "Play Mode kontrolleri geçti.\n");
        }
        catch (Exception exception)
        {
            File.AppendAllText(ReportPath, exception + "\n");
        }
        finally { EditorApplication.ExitPlaymode(); }
    }

    private static async Task Wait(Func<bool> condition, int timeoutMilliseconds = 20000)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Beklenen oyun durumu oluşmadı.");
            await Task.Delay(50);
        }
    }

    private static async Task Capture(string name)
    {
        ScreenCapture.CaptureScreenshot("Logs/Onur-" + name + ".png");
        await Task.Delay(600);
    }

    private static void Require(bool condition, string text)
    {
        if (!condition) throw new InvalidOperationException(text);
        File.AppendAllText(ReportPath, "OK: " + text + "\n");
    }
}
