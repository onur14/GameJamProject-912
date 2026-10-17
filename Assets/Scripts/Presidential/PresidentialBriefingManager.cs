using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public sealed class PresidentialBriefingManager : MonoBehaviour
{
    public enum ShiftPhase { Scenario, Dialogue, Allocation, Confirmation, Final, AwaitingScenarioData, ShiftSummary }
    [SerializeField] private DispatchResources resources;
    [SerializeField] private OyunAkisiOnur conversation;
    [SerializeField] private DispatchCallFlow phone;
    [SerializeField] private PresidentialBroadcastView view;
    [SerializeField] private CityHealth cityHealth;
    [Tooltip("Exactly eight slots. Empty first slot uses the existing Onur earthquake conversation. Other empty slots wait for authored content.")]
    [SerializeField] private DispatchScenarioData[] scenarioOrder = new DispatchScenarioData[8];
    [SerializeField] private PresidentialBriefingData[] briefings = new PresidentialBriefingData[4];
    public UnityEvent<int> ScenarioRequested;
    public event Action ShiftReset;
    public event Action RunCompleted;
    public int CompletedScenarios { get; private set; }
    public ShiftPhase Phase { get; private set; }
    public IReadOnlyList<string> Decisions => decisions;
    private readonly List<string> decisions = new List<string>();
    private readonly HashSet<int> shown = new HashSet<int>();
    private readonly HashSet<int> completedRevisions = new HashSet<int>();
    public int CorrectDecisions { get; private set; }
    public DispatchResources Resources => resources;
    private readonly List<ShiftScenarioOutcome> scenarioOutcomes = new List<ShiftScenarioOutcome>();
    public IReadOnlyList<ShiftScenarioOutcome> ScenarioOutcomes => scenarioOutcomes;
    private PresidentialBriefingData current;
    private ResourceOffer offerA, offerB;
    private int line;
    private bool acceptingCompletion, applied;
    [SerializeField, Min(1)] private int estimatedPeoplePerCorrectDecision = 1250;

    private void Awake()
    {
        if (!resources || !conversation || !phone || !view)
        { Debug.LogError("Başkan sistemi: sahne referansları eksik.", this); enabled = false; return; }
        conversation.ManagedShift = true;
        if (scenarioOrder.Length > 0) conversation.ConfigureInitialScenario(scenarioOrder[0]);
        conversation.ScenarioCompleted += OnScenarioCompleted;
        view.AdvanceRequested += Advance;
        view.OfferSelected += SelectOffer;
        resources.Changed += OnResourcesChanged;
        CompletedScenarios = 0; shown.Clear(); decisions.Clear(); scenarioOutcomes.Clear();
        Phase = ShiftPhase.Scenario; acceptingCompletion = true;
    }
    private void OnDestroy()
    {
        if (conversation) conversation.ScenarioCompleted -= OnScenarioCompleted;
        if (view) { view.AdvanceRequested -= Advance; view.OfferSelected -= SelectOffer; }
        if (resources) resources.Changed -= OnResourcesChanged;
    }
    public void ResetNewShift()
    {
        ShiftReset?.Invoke();
        scenarioOutcomes.Clear();
        StopAllCoroutines(); CompletedScenarios = 0; CorrectDecisions = 0; completedRevisions.Clear(); shown.Clear(); decisions.Clear(); current = null;
        conversation.ResetGameOver();
        if (cityHealth) cityHealth.ResetForNewShift();
        resources.ResetForNewShift(); view.Hide(); phone.SetInteractionLocked(false);
        Phase = ShiftPhase.Scenario; acceptingCompletion = true;
        conversation.BeginScenario(scenarioOrder.Length > 0 ? scenarioOrder[0] : null);
    }
    public void RestoreScenarioCheckpoint(int completed, int correct, int allocationCount, int outcomeCount, DispatchScenarioData scenario)
    {
        StopAllCoroutines(); view.Hide();
        CompletedScenarios = completed; CorrectDecisions = correct;
        if (decisions.Count > allocationCount) decisions.RemoveRange(allocationCount, decisions.Count - allocationCount);
        if (scenarioOutcomes.Count > outcomeCount) scenarioOutcomes.RemoveRange(outcomeCount, scenarioOutcomes.Count - outcomeCount);
        shown.RemoveWhere(n => n > completed);
        current = null; line = 0; applied = false;
        Phase = ShiftPhase.Scenario; acceptingCompletion = true;
        conversation.ResetGameOver(); phone.SetPaused(false); phone.SetInteractionLocked(false);
        conversation.BeginScenario(scenario);
    }

    public void OnScenarioCompleted(int revision)
    {
        if (!acceptingCompletion || Phase != ShiftPhase.Scenario || CompletedScenarios >= 8
            || revision != conversation.ScenarioRevision || !completedRevisions.Add(revision)) return;
        if (conversation.LastDecisionCorrect) CorrectDecisions++;
        var scenario = CompletedScenarios < scenarioOrder.Length ? scenarioOrder[CompletedScenarios] : null;
        scenarioOutcomes.Add(new ShiftScenarioOutcome { scenarioId = scenario ? scenario.scenarioId : "D-01",
            callerName = scenario ? scenario.callerName : string.Empty, correct = conversation.LastDecisionCorrect });
        acceptingCompletion = false; CompletedScenarios++;
        var curses=FindAnyObjectByType<DispatchCurseController>();
        if(curses)curses.OnScenarioCompleted(CompletedScenarios);
        if (CompletedScenarios % 2 != 0) { RequestNextScenario(); return; }
        if (!shown.Add(CompletedScenarios)) return;
        int index = CompletedScenarios / 2 - 1;
        if (index >= briefings.Length || !briefings[index] || briefings[index].afterScenario != CompletedScenarios)
        {
            Phase = ShiftPhase.AwaitingScenarioData; phone.SetInteractionLocked(true);
            view.Show("YAYIN VERİSİ EKSİK"); view.Line("Bu aşamanın başkan bilgilendirmesi Inspector üzerinden atanmalı.", view.Stock(resources));
            return;
        }
        current = briefings[index]; line = 0; applied = false;
        phone.SetInteractionLocked(true);
        Phase = ShiftPhase.ShiftSummary;
        StartCoroutine(SummaryBeforeBroadcast(index));
    }
    private IEnumerator SummaryBeforeBroadcast(int index)
    {
        yield return view.ShowShiftSummary(CompletedScenarios, CorrectDecisions,
            CorrectDecisions * estimatedPeoplePerCorrectDecision, cityHealth ? cityHealth.HealthPercent : 100);
        if (cityHealth && cityHealth.IsGameOver) yield break;
        view.Show($"RESMÎ BİLGİLENDİRME {index + 1}/4  /  {current.heading}");
        Phase = ShiftPhase.Dialogue; ShowLine();
    }
    public bool RetryCurrentScenario()
    {
        if(Phase!=ShiftPhase.Scenario||CompletedScenarios>=8)return false;
        StopAllCoroutines();view.Hide();acceptingCompletion=true;applied=false;
        conversation.ResetGameOver();phone.SetPaused(false);phone.SetInteractionLocked(false);
        conversation.BeginScenario(CompletedScenarios<scenarioOrder.Length?scenarioOrder[CompletedScenarios]:null);
        return true;
    }

    private void ShowLine()
    {
        if (current.dialogueLines != null && line < current.dialogueLines.Length)
        {
            string text = current.dialogueLines[line].Replace("{tamamlanan}", CompletedScenarios.ToString())
                .Replace("{karar}", decisions.Count.ToString());
            view.Line(text, view.Stock(resources));
        }
        else ShowOffers();
    }
    private void Advance()
    {
        if (Phase == ShiftPhase.Dialogue) { line++; ShowLine(); }
        else if (Phase == ShiftPhase.Confirmation && applied)
        {
            if (CompletedScenarios == 8) ShowFinal();
            else { view.Hide(); RequestNextScenario(); }
        }
    }
    public static ResourceOffer EmergencyOffer(DispatchResources r, bool medical)
    {
        // Finite reserve: retain at most one of each unprioritized team in exchange for specialization.
        // At zero stock the opportunity cost is the other mutually exclusive reserve package.
        return new ResourceOffer {
            title = medical ? "ACİL SAĞLIK YEDEĞİ" : "ACİL SAHA YEDEĞİ",
            rationale = medical ? "Sınırlı merkez yedeği sağlığa ayrılır; saha desteğinden vazgeçilir." : "Sınırlı merkez yedeği sahaya ayrılır; sağlık desteğinden vazgeçilir.",
            ambulances = medical ? 2 : -Mathf.Max(0, r.Ambulances - 1),
            firefighters = medical ? -Mathf.Max(0, r.Firefighters - 1) : 1,
            thirdTeam = medical ? -Mathf.Max(0, r.Police - 1) : 1
        };
    }
    private void ShowOffers()
    {
        Phase = ShiftPhase.Allocation; offerA = current.planA; offerB = current.planB;
        if (offerA == null || offerB == null) { Debug.LogError("İki tahsis planı atanmalı."); return; }
        if (!offerA.IsValid(resources) && !offerB.IsValid(resources))
        { offerA = EmergencyOffer(resources, true); offerB = EmergencyOffer(resources, false); }
        offerA = AdjustCurseOffer(offerA); offerB = AdjustCurseOffer(offerB);
        view.Offers(offerA, offerB, resources);
    }
    public ResourceOffer AdjustCurseOffer(ResourceOffer source)
    {
        if (source == null) return null;
        var director = cityHealth ? cityHealth.CurseDirector : null;
        var legacy = director ? null : FindAnyObjectByType<DispatchCurseController>();
        bool fog = director ? director.PresidentPenaltyPending : legacy && legacy.PresidentPenaltyPending;
        if (!fog) return source;
        return new ResourceOffer { title = source.title, rationale = source.rationale + "\nBİLGİ SİSİ: pozitif bonuslar yarıya indi.",
            ambulances = source.ambulances > 0 ? source.ambulances / 2 : source.ambulances,
            firefighters = source.firefighters > 0 ? source.firefighters / 2 : source.firefighters,
            thirdTeam = source.thirdTeam > 0 ? source.thirdTeam / 2 : source.thirdTeam };
    }
    private void OnResourcesChanged()
    {
        if (Phase == ShiftPhase.Allocation && !applied) ShowOffers();
    }
    public void SelectOffer(int index)
    {
        if (Phase != ShiftPhase.Allocation || applied || (index != 0 && index != 1)) return;
        var plan = index == 0 ? offerA : offerB;
        // Lock before the resource event can re-enter the offer controller.
        applied = true; Phase = ShiftPhase.Confirmation;
        if (plan == null || !plan.Apply(resources)) { applied = false; ShowOffers(); return; }
        var director = cityHealth ? cityHealth.CurseDirector : null;
        if(director)director.ConsumePresidentPenalty();
        else { var curse=FindAnyObjectByType<DispatchCurseController>();if(curse)curse.ConsumePresidentPenalty(); }
        decisions.Add(plan.title);
        view.Line($"TAHSİS ONAYLANDI: {plan.title}\n{view.AllocationSummary(plan)}\n\n{current.closing}", view.Stock(resources));
        view.ConfirmAllocation();
    }
    private void RequestNextScenario()
    {
        if (CompletedScenarios >= 8) { ShowFinal(); return; }
        ScenarioRequested?.Invoke(CompletedScenarios + 1);
        var data = CompletedScenarios < scenarioOrder.Length ? scenarioOrder[CompletedScenarios] : null;
        if (data == null || !data.readyForPlay)
        {
            Phase = ShiftPhase.AwaitingScenarioData; phone.SetInteractionLocked(true);
            view.Show("SONRAKİ VAKA BEKLENİYOR");
            view.Line($"{CompletedScenarios}. vaka tamamlandı. {CompletedScenarios + 1}. senaryonun mevcut vatandaş metinleri henüz atanmamış.\n\nVardiya sırası Inspector'daki Scenario Order alanından düzenlenir. Kaynaklarınız korunuyor.", view.Stock(resources));
            return;
        }
        Phase = ShiftPhase.Scenario; acceptingCompletion = true;
        phone.SetInteractionLocked(false); view.Hide(); conversation.BeginScenario(data);
    }
    private void ShowFinal()
    {
        if (Phase == ShiftPhase.Final) return;
        Phase = ShiftPhase.Final; acceptingCompletion = false; phone.SetInteractionLocked(true);
        int weakest = Mathf.Min(resources.Ambulances, Mathf.Min(resources.Firefighters, resources.Police));
        string readiness = weakest >= 3 ? "Dengeli hazırlık: tüm hizmetlerde en az üç ekip hazır."
            : weakest > 0 ? "Sınırlı hazırlık: tüm hizmetler açık; yedek kapasite düşük."
            : "Kritik hazırlık: en az bir hizmette ekip kalmadı. İlk destek bu alana ayrılmalı.";
        view.Final($"8 vaka tamamlandı. {CorrectDecisions}/8 doğru yönlendirme; {decisions.Count} stratejik tahsis.\n\n{readiness}\n\nSon tahsis: {(decisions.Count > 0 ? decisions[decisions.Count - 1] : "Yok")}\n\nBu ekipler gelecek vardiyaya devredilir. Hazırlık, doğru bilgi ve dayanışma birlikte hayat kurtarır.", view.Stock(resources));
        RunCompleted?.Invoke();
    }
}
