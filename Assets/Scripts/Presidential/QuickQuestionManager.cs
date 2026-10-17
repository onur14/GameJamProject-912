using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class QuickQuestionManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private DispatchResources resources;
    [SerializeField] private OyunAkisiOnur conversation;
    [SerializeField] private DispatchCallFlow phone;
    [SerializeField] private PresidentialBriefingManager briefingManager;
    [SerializeField] private QuickQuestionView view;
    [SerializeField] private CityHealth cityHealth;
    [SerializeField] private DispatchVisualFeedback visualFeedback;

    [Header("Scenario Questions (8 Slots)")]
    [Tooltip("One Quick Question configuration per scenario.")]
    [SerializeField] private QuickQuestionConfig[] scenarioQuestions = new QuickQuestionConfig[8];
    [Tooltip("Second, sequential information-verification question for each scenario.")]
    [SerializeField] private QuickQuestionConfig[] secondQuestions = new QuickQuestionConfig[8];

    [Header("Optional ScriptableObject Overrides")]
    [SerializeField] private QuickQuestionData[] questionDataAssets;

    public event Action<QuickQuestionConfig> QuestionStarted;
    public event Action<QuickQuestionResult> QuestionResolved;

    private QuickQuestionView[] views;
    private readonly QuickQuestionConfig[] activeConfigs=new QuickQuestionConfig[2];
    private readonly bool[] activeSlots=new bool[2];
    [SerializeField] private QuickQuestionConfig[] thirdQuestions=new QuickQuestionConfig[8];
    public IReadOnlyList<QuickQuestionView> Views => views;
    public bool IsQuestionActive => activeSlots[0]||activeSlots[1];
    public QuickQuestionConfig CurrentQuestion => activeSlots[0]?activeConfigs[0]:activeSlots[1]?activeConfigs[1]:activeConfigs[0];
    public QuickQuestionConfig GetQuestionForView(QuickQuestionView target)=>activeConfigs[target==view?0:1];
    public IReadOnlyList<QuickQuestionResult> Results => results;

    private readonly List<QuickQuestionResult> results = new List<QuickQuestionResult>();
    private Coroutine triggerRoutine;
    private int currentScenarioRevision = -1;
    private int activeScenarioIndex = -1;
    private bool scenarioFinished;
    private int resolvedInScenario;

    public bool QuickQuestionsComplete => currentScenarioRevision >= 0 && resolvedInScenario >= 3
        && !AnyViewVisible;

    public int AnsweredCount => results.FindAll(r => r.answered && !r.timedOut).Count;
    public int MissedCount => results.FindAll(r => r.timedOut).Count;
    public float AverageResponseTime
    {
        get
        {
            var answered = results.FindAll(r => r.answered && !r.timedOut);
            if (answered.Count == 0) return 0f;
            float sum = 0f;
            foreach (var r in answered) sum += r.responseTime;
            return sum / answered.Count;
        }
    }

    private void Awake()
    {
        InitializeDefaultQuestionsIfNeeded();
        ConfigureVerificationRewards();
        if(view){view.SetSide(false);views=new[]{view,view.CreateCompanion()};}
    }

    [SerializeField, Min(0)] private int returnStockCeiling = 6;
    [SerializeField, Min(0)] private int timelyResponseHealthReward = 2;
    [SerializeField, Min(0)] private int verifiedAnswerHealthReward = 5;
    private string activeScenarioId;
    private readonly HashSet<int> scheduledRevisions = new HashSet<int>();

    private void OnEnable()
    {
        if (conversation) { conversation.ScenarioStarted += OnScenarioStarted; conversation.ScenarioClosing += OnScenarioClosing; }
        if (briefingManager) briefingManager.ShiftReset += ResetForNewShift;
    }
    private void OnDisable()
    {
        if (conversation) { conversation.ScenarioStarted -= OnScenarioStarted; conversation.ScenarioClosing -= OnScenarioClosing; }
        if (briefingManager) briefingManager.ShiftReset -= ResetForNewShift;
        CancelActiveQuestion();
    }
    private void OnScenarioStarted(int revision, string scenarioId)
    {
        if (!scheduledRevisions.Add(revision)) return;
        CancelActiveQuestion();
        currentScenarioRevision = revision; activeScenarioId = scenarioId; scenarioFinished = false; resolvedInScenario = 0;
        activeScenarioIndex = briefingManager ? briefingManager.CompletedScenarios : 0;
        QuickQuestionConfig config = GetQuestionForScenario(activeScenarioIndex);
        if (config != null && config.targetScenarioId != scenarioId) config = null;
        if(config!=null)triggerRoutine=StartCoroutine(WaitAndTriggerQuestion(config,revision));
    }
    private void OnScenarioClosing(int revision)
    {
        if(revision!=currentScenarioRevision)return;
        scenarioFinished=true;CancelActiveQuestion();
    }
    public void InitializeDefaultQuestionsIfNeeded()
    {
        var defaults=GetDefault8Questions();
        if(scenarioQuestions==null)scenarioQuestions=new QuickQuestionConfig[8];
        if(scenarioQuestions.Length!=8)Array.Resize(ref scenarioQuestions,8);
        var ids = new HashSet<string>();
        for (int i = 0; i < 8; i++)
        {
            // Repair the unfinished installer's duplicated placeholder slots, preserving unique edits.
            if (scenarioQuestions[i] == null || string.IsNullOrEmpty(scenarioQuestions[i].id) || !ids.Add(scenarioQuestions[i].id))
            {
                scenarioQuestions[i] = defaults[i];
                ids.Add(scenarioQuestions[i].id);
            }
        }
        if (secondQuestions == null) secondQuestions = new QuickQuestionConfig[8];
        if (secondQuestions.Length != 8) Array.Resize(ref secondQuestions, 8);
        for (int i = 0; i < 8; i++)
            if (secondQuestions[i] == null || string.IsNullOrEmpty(secondQuestions[i].id)
                || secondQuestions[i].questionText == "Bu vakanın ikinci hızlı soru metni henüz yazılmadı."
                || secondQuestions[i].targetScenarioId != scenarioQuestions[i].targetScenarioId
                || !ids.Add(secondQuestions[i].id))
            {
                secondQuestions[i] = GetDefaultSecondQuestions()[i];
                ids.Add(secondQuestions[i].id);
            }
        if(thirdQuestions==null)thirdQuestions=new QuickQuestionConfig[8];
        if(thirdQuestions.Length!=8)Array.Resize(ref thirdQuestions,8);
        var thirds=GetDefaultThirdQuestions();
        for(int i=0;i<8;i++)
            if(thirdQuestions[i]==null||string.IsNullOrEmpty(thirdQuestions[i].id)||thirdQuestions[i].targetScenarioId!=scenarioQuestions[i].targetScenarioId||!ids.Add(thirdQuestions[i].id))
            {thirdQuestions[i]=thirds[i];ids.Add(thirdQuestions[i].id);}
    }

    public void ConfigureVerificationRewards()
    {
        if (secondQuestions == null) return;
        for (int i = 0; i < secondQuestions.Length; i++)
        {
            var q = secondQuestions[i]; if (q == null || !q.isDisinformation) continue;
            var bonus = new DispatchResourceEffect { ambulances = i % 3 == 0 ? 1 : 0, firefighters = i % 3 == 1 ? 1 : 0, rescue = i % 3 == 2 ? 1 : 0 };
            if (q.correctChoiceIndex == 0) q.choiceAEffect = bonus; else q.choiceBEffect = bonus;
        }
    }

    public static QuickQuestionConfig[] GetDefaultSecondQuestions()
    {
        string[] ids = { "D-01", "D-02", "Y-02", "S-01", "T-01", "F-01", "K-01", "K-02" };
        string[] questions = {
            "Bir hesap 'Gece 03.15’te kesin deprem var' diyor. Paylaşalım mı?",
            "Kaynağı belirsiz ses kaydı tahliye emri veriyor. Yayılsın mı?",
            "Eski yangın videosu 'şimdi burada' diye yayılıyor. Düzeltelim mi?",
            "Teyitsiz mesaj 'Tüm yollar açık' diyor. Resmî bilgi diye yayalım mı?",
            "Sahte hesap 'Tsunami uyarısı kalktı' diyor. Resmî teyit isteyelim mi?",
            "Tarihsiz fırtına haritası yeniden yayılıyor. Güncel diye sunalım mı?",
            "Uzman adı olmayan deprem tahmini gündemde. Kaynağı doğrulayalım mı?",
            "Kişisel IBAN isteyen yardım mesajını resmî duyuru diye paylaşalım mı?"
        };
        int[] correct = { 1, 1, 0, 1, 0, 1, 0, 1 };
        var result = new QuickQuestionConfig[8];
        for (int i = 0; i < result.Length; i++)
        {
            string good = "TEYİTSİZ BİLGİ ENGELLENDİ";
            string bad = "YANLIŞ BİLGİ YAYILDI · SAĞLIK -10";
            result[i] = new QuickQuestionConfig {
                id = "QQ-" + (i + 9).ToString("00"), targetScenarioId = ids[i],
                categoryTitle = "BİLGİ KİRLİLİĞİ VE TEYİT", questionText = questions[i],
                choiceALabel = "EVET", choiceBLabel = "HAYIR",
                choiceAEffect = new DispatchResourceEffect(), choiceBEffect = new DispatchResourceEffect(),
                choiceAFeedback = correct[i] == 0 ? good : bad, choiceBFeedback = correct[i] == 1 ? good : bad,
                isDisinformation = true, correctChoiceIndex = correct[i],
                wrongAnswerHealthDamage = 10, timeoutHealthDamage = 10,
                timeoutFeedback = "MÜDAHALE EDİLMEDİ · SAĞLIK -10",
                timerDuration = 7f, appearanceDelayMin = 1.4f, appearanceDelayMax = 2.2f
            };
        }
        return result;
    }

    public static QuickQuestionConfig[] GetDefault8Questions()
    {
        return new QuickQuestionConfig[8]
        {
            // Scenario 1: D-01 (Ambulance request)
            new QuickQuestionConfig
            {
                id = "QQ-01",
                targetScenarioId = "D-01",
                categoryTitle = "SAHA TALEBİ",
                questionText = "Bölge 3'teki ekip ek ambulans talep ediyor.",
                choiceALabel = "GÖNDER (-1)",
                choiceAEffect = new DispatchResourceEffect { ambulances = -1 },
                choiceAFeedback = "AMBULANS GÖNDERİLDİ",
                choiceBLabel = "REDDET",
                choiceBEffect = new DispatchResourceEffect(),
                choiceBFeedback = "TALEP REDDEDİLDİ",
                timerDuration = 7f,
                appearanceDelayMin = 2.5f,
                appearanceDelayMax = 3.5f
            },

            // Scenario 2: D-02 (Fire support request)
            new QuickQuestionConfig
            {
                id = "QQ-02",
                targetScenarioId = "D-02",
                categoryTitle = "YANGIN DESTEĞİ",
                questionText = "Yangının yayılma riski arttı. Ek itfaiye desteği isteniyor.",
                choiceALabel = "GÖNDER (-1)",
                choiceAEffect = new DispatchResourceEffect { firefighters = -1 },
                choiceAFeedback = "İTFAİYE GÖNDERİLDİ",
                choiceBLabel = "REDDET",
                choiceBEffect = new DispatchResourceEffect(),
                choiceBFeedback = "TALEP REDDEDİLDİ",
                timerDuration = 7f,
                appearanceDelayMin = 2.5f,
                appearanceDelayMax = 3.5f
            },

            // Scenario 3: Y-02 (Rescue support request)
            new QuickQuestionConfig
            {
                id = "QQ-03",
                targetScenarioId = "Y-02",
                categoryTitle = "KURTARMA TALEBİ",
                questionText = "Enkaz bölgesindeki ekip ek kurtarma desteği istiyor.",
                choiceALabel = "GÖNDER (-1)",
                choiceAEffect = new DispatchResourceEffect { rescue = -1 },
                choiceAFeedback = "KURTARMA EKİBİ GÖNDERİLDİ",
                choiceBLabel = "REDDET",
                choiceBEffect = new DispatchResourceEffect(),
                choiceBFeedback = "TALEP REDDEDİLDİ",
                timerDuration = 7f,
                appearanceDelayMin = 2.0f,
                appearanceDelayMax = 3.0f
            },

            // Scenario 4: S-01 (Ambulance redirection)
            new QuickQuestionConfig
            {
                id = "QQ-04",
                targetScenarioId = "S-01",
                categoryTitle = "EKİP YÖNLENDİRME",
                questionText = "Yakındaki ambulans yeni bir vakaya yönlendirilsin mi?",
                choiceALabel = "YÖNLENDİR",
                choiceAEffect = new DispatchResourceEffect(), // Decision only
                choiceAFeedback = "AMBULANS YÖNLENDİRİLDİ",
                choiceBLabel = "MEVCUT GÖREVDE TUT",
                choiceBEffect = new DispatchResourceEffect(),
                choiceBFeedback = "MEVCUT GÖREVDE TUTULDU",
                timerDuration = 7f,
                appearanceDelayMin = 2.0f,
                appearanceDelayMax = 3.0f
            },

            // Scenario 5: T-01 (Unit return request - restore +1)
            new QuickQuestionConfig
            {
                id = "QQ-05",
                targetScenarioId = "T-01",
                categoryTitle = "EKİP DÖNÜŞÜ",
                questionText = "Bir saha ekibi görevini tamamladı. Merkeze geri çağrılsın mı?",
                choiceALabel = "GERİ ÇAĞIR (+1)",
                choiceAEffect = new DispatchResourceEffect { ambulances = 1 },
                choiceAFeedback = "AMBULANS MERKEZE DÖNDÜ",
                choiceBLabel = "BÖLGEDE TUT",
                choiceBEffect = new DispatchResourceEffect(),
                choiceBFeedback = "EKİP BÖLGEDE BIRAKILDI",
                timerDuration = 7f,
                appearanceDelayMin = 1.8f,
                appearanceDelayMax = 2.8f
            },

            // Scenario 6: F-01 (Two competing requests - priority choice)
            new QuickQuestionConfig
            {
                id = "QQ-06",
                targetScenarioId = "F-01",
                categoryTitle = "ÖNCELİK SEÇİMİ",
                questionText = "Tek kurtarma ekibi müsait. Hangi bölgeye yönlendirilsin?",
                choiceALabel = "BÖLGE A (-1)",
                choiceAEffect = new DispatchResourceEffect { rescue = -1 },
                choiceAFeedback = "BÖLGE A'YA SEVK EDİLDİ",
                choiceBLabel = "BÖLGE B (-1)",
                choiceBEffect = new DispatchResourceEffect { rescue = -1 },
                choiceBFeedback = "BÖLGE B'YE SEVK EDİLDİ",
                timerDuration = 7f,
                appearanceDelayMin = 1.8f,
                appearanceDelayMax = 2.5f
            },

            // Scenario 7: K-01 (Hospital capacity / route decision)
            new QuickQuestionConfig
            {
                id = "QQ-07",
                targetScenarioId = "K-01",
                categoryTitle = "ROTA DEĞERLENDİRMESİ",
                questionText = "Yakındaki hastanede yoğunluk bildirildi. Ambulans rotası değiştirilsin mi?",
                choiceALabel = "ROTAYI DEĞİŞTİR",
                choiceAEffect = new DispatchResourceEffect(),
                choiceAFeedback = "ALTERNATİF ROTA SEÇİLDİ",
                choiceBLabel = "MEVCUT ROTAYI KORU",
                choiceBEffect = new DispatchResourceEffect(),
                choiceBFeedback = "MEVCUT ROTA KORUNDU",
                timerDuration = 7f,
                appearanceDelayMin = 1.5f,
                appearanceDelayMax = 2.2f
            },

            // Scenario 8: K-02 (Reserve unit request)
            new QuickQuestionConfig
            {
                id = "QQ-08",
                targetScenarioId = "K-02",
                categoryTitle = "REZERV TALEBİ",
                questionText = "Büyük olay bölgesi rezerv ekip desteği talep ediyor.",
                choiceALabel = "REZERV GÖNDER (-1)",
                choiceAEffect = new DispatchResourceEffect { rescue = -1 },
                choiceAFeedback = "REZERV EKİP SEVK EDİLDİ",
                choiceBLabel = "REZERVİ KORU",
                choiceBEffect = new DispatchResourceEffect(),
                choiceBFeedback = "REZERV KORUNDU",
                timerDuration = 7f,
                appearanceDelayMin = 1.2f,
                appearanceDelayMax = 2.0f
            }
        };
    }

    private bool AnyViewVisible => views!=null && System.Array.Exists(views,v=>v&&v.IsActive);
    public void SetPaused(bool paused){if(views!=null)foreach(var card in views)card.SetPaused(paused);}
    public static QuickQuestionConfig[] GetDefaultThirdQuestions()
    {
        string[] text={"Deprem sonrası hasarlı binaya geri dönmek güvenli mi?","Gaz kokusunda elektrik düğmesine basalım mı?","Yoğun dumanda alçaktan ilerleyelim mi?","Sel suyuna araçla girelim mi?","Tsunami uyarısında kıyıdan uzaklaşıp yükseğe çıkalım mı?","Fırtınada devrilebilecek ağaçlardan uzaklaşalım mı?","Yardım bilgisini resmî kaynaklardan doğrulayalım mı?","Acil yardım araçları için yolları açık tutalım mı?"};
        int[] correct={1,1,0,1,0,0,0,0};
        var result=GetDefaultSecondQuestions();
        for(int i=0;i<8;i++)
        {
            var q=result[i];q.id="QQ-"+(i+17).ToString("00");q.categoryTitle="GÜVENLİ DAVRANIŞ";q.questionText=text[i];q.correctChoiceIndex=correct[i];
            q.choiceAEffect=q.choiceBEffect=new DispatchResourceEffect();
            q.choiceAFeedback=correct[i]==0?"DOĞRU YÖNLENDİRME":"GÜVENSİZ YÖNLENDİRME";
            q.choiceBFeedback=correct[i]==1?"DOĞRU YÖNLENDİRME":"GÜVENSİZ YÖNLENDİRME";q.timerDuration=7;
        }
        return result;
    }
    public int IncorrectAnswerCount
    {
        get
        {
            int count=0;
            foreach(var result in results)
            {
                if(!result.answered || result.cancelled)continue;
                for(int i=0;i<3;i++)
                {
                    var config=GetQuestionForScenario(result.scenarioIndex,i==1,i==2);
                    if(config!=null&&config.id==result.questionId&&config.isDisinformation&&result.selectedChoice!=config.correctChoiceIndex)count++;
                }
            }
            return count;
        }
    }
    private IEnumerator WaitAndTriggerQuestion(QuickQuestionConfig config,int revision)
    {
        while(phone && phone.State!=DispatchCallFlow.CallState.Conversation)
        {if(scenarioFinished || conversation.GameOver)yield break;yield return null;}
        int requiredLines = resolvedInScenario == 0 ? 2 : 3;
        while (conversation && conversation.CompletedDialogueLines < requiredLines)
        {
            if (scenarioFinished || currentScenarioRevision != revision || conversation.GameOver) yield break;
            yield return null;
        }
        float min=Mathf.Max(0,config.appearanceDelayMin);
        float delay=UnityEngine.Random.Range(min,Mathf.Max(min,config.appearanceDelayMax));
        for(float elapsed=0;elapsed<delay;)
        {
            if(scenarioFinished || currentScenarioRevision!=revision || conversation.GameOver)yield break;
            if (!phone || !phone.IsPaused) elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if(scenarioFinished||currentScenarioRevision!=revision)yield break;
        int primary=activeScenarioIndex%2==0?0:1;
        ShowSlot(config,primary);
        ShowSlot(GetQuestionForScenario(activeScenarioIndex,true),1-primary);
        while(views[primary].IsActive)
        {
            if(scenarioFinished||currentScenarioRevision!=revision||conversation.GameOver)yield break;
            yield return null;
        }
        if(!scenarioFinished&&currentScenarioRevision==revision)ShowSlot(GetQuestionForScenario(activeScenarioIndex,false,true),primary);
        triggerRoutine=null;
    }

    public QuickQuestionConfig GetQuestionForScenario(int index, bool second = false, bool third = false)
    {
        if(index<0)return null;
        if(third)return thirdQuestions!=null&&index<thirdQuestions.Length?thirdQuestions[index]:null;
        if (second) return secondQuestions != null && index < secondQuestions.Length ? secondQuestions[index] : null;
        if(questionDataAssets!=null && index<questionDataAssets.Length && questionDataAssets[index])return questionDataAssets[index].config;
        return scenarioQuestions!=null && index<scenarioQuestions.Length ? scenarioQuestions[index] : null;
    }

    public void TriggerQuestion(QuickQuestionConfig config)
    {
        if(IsQuestionActive||AnyViewVisible)return;
        ShowSlot(config,0);
    }
    private void ShowSlot(QuickQuestionConfig config,int slot)
    {
        if(config==null||views==null||!resources||activeSlots[slot]||views[slot].IsActive||!isActiveAndEnabled||(conversation&&conversation.GameOver))return;
        var question=config.Clone();question.timerDuration=7f;activeConfigs[slot]=question;activeSlots[slot]=true;
        views[slot].Show(question,resources,choice=>OnChoiceSelected(slot,choice),()=>OnTimeout(slot),choice=>CanChoose(slot,choice));
        if(visualFeedback)visualFeedback.SignalAlert(false);QuestionStarted?.Invoke(question);
    }

    private DispatchResourceEffect EffectiveCost(int slot,int choice)
    {
        var effect=choice==0?activeConfigs[slot].choiceAEffect:activeConfigs[slot].choiceBEffect;
        if(effect.ambulances>0)effect.ambulances=Mathf.Min(effect.ambulances,Mathf.Max(0,returnStockCeiling-resources.Ambulances));
        if(effect.firefighters>0)effect.firefighters=Mathf.Min(effect.firefighters,Mathf.Max(0,returnStockCeiling-resources.Firefighters));
        if(effect.rescue>0)effect.rescue=Mathf.Min(effect.rescue,Mathf.Max(0,returnStockCeiling-resources.Police));
        return effect;
    }
    private bool CanChoose(int slot,int choice)
    {
        if(!activeSlots[slot] || choice<0 || choice>1 || !resources)return false;
        var raw=choice==0?activeConfigs[slot].choiceAEffect:activeConfigs[slot].choiceBEffect;
        var effect=EffectiveCost(slot,choice);
        // Do not claim a return succeeded when already at its configured ceiling.
        if(!raw.IsZero && effect.IsZero && !activeConfigs[slot].isDisinformation)return false;
        return effect.CanApply(resources);
    }
    private bool OnChoiceSelected(int slot,int choice)
    {
        if(!CanChoose(slot,choice))return false;
        var config=activeConfigs[slot];var effect=EffectiveCost(slot,choice);
        activeSlots[slot]=false; // Lock before Changed listeners run.
        if(!effect.IsZero && !effect.Apply(resources)){activeSlots[slot]=true;return false;}
        var result=CreateResult(config,slot);result.answered=true;result.selectedChoice=choice;
        result.selectedChoiceLabel=choice==0?config.choiceALabel:config.choiceBLabel;
        result.appliedEffect=effect;results.Add(result);
        if(conversation && (effect.ambulances<0 || effect.firefighters<0 || effect.rescue<0))conversation.NotifyUnitDispatched(effect);
        if (config.isDisinformation)
        {
            bool wrong = choice != config.correctChoiceIndex;
            if (wrong && cityHealth) cityHealth.ApplyDamage(config.wrongAnswerHealthDamage);
            if (visualFeedback) visualFeedback.SignalAlert(wrong);
        }
        bool earnsReward = !config.isDisinformation || choice == config.correctChoiceIndex;
        if (earnsReward && cityHealth)
        {
            int before = cityHealth.HealthPercent;
            cityHealth.AddHealth(config.isDisinformation ? verifiedAnswerHealthReward : timelyResponseHealthReward);
            int gained = cityHealth.HealthPercent - before;
            string bonus = gained > 0 ? "HALK BİLİNCİ +" + gained : "HALK BİLİNCİ TAM";
            if (effect.ambulances > 0) bonus += " · AMBULANS +" + effect.ambulances;
            if (effect.firefighters > 0) bonus += " · İTFAİYE +" + effect.firefighters;
            if (effect.rescue > 0) bonus += " · KURTARMA +" + effect.rescue;
            if (config.isDisinformation && effect.IsZero) bonus += " · EKİP YEDEĞİ DOLU";
            views[slot].ShowRewardFeedback(bonus);
        }
        GameAudioFeedback.Feedback(earnsReward);resolvedInScenario++;QuestionResolved?.Invoke(result);return true;
    }
    private QuickQuestionResult CreateResult(QuickQuestionConfig config,int slot)
    {
        return new QuickQuestionResult {questionId=config.id,targetScenarioId=activeScenarioId??config.targetScenarioId,
            scenarioIndex=activeScenarioIndex,scenarioRevision=currentScenarioRevision,
            responseTime=views[slot].ElapsedResponseTime,totalDuration=Mathf.Max(1,config.timerDuration),selectedChoice=-1};
    }
    private void OnTimeout(int slot)
    {
        if(!activeSlots[slot])return;
        var config=activeConfigs[slot];activeSlots[slot]=false;var result=CreateResult(config,slot);
        result.timedOut=true;result.selectedChoiceLabel="YANITLANMADI";results.Add(result);resolvedInScenario++;
        if(config.isDisinformation){if(cityHealth)cityHealth.ApplyDamage(config.timeoutHealthDamage);if(visualFeedback)visualFeedback.SignalAlert(true);}
        GameAudioFeedback.Feedback(false);QuestionResolved?.Invoke(result);
    }
    public void CancelActiveQuestion()
    {
        if(triggerRoutine!=null){StopCoroutine(triggerRoutine);triggerRoutine=null;}
        for(int slot=0;slot<2;slot++)
        {
            if(activeSlots[slot])
            {
                activeSlots[slot]=false;var result=CreateResult(activeConfigs[slot],slot);result.cancelled=true;
                result.selectedChoiceLabel="ANA ÇAĞRI KAPANDI";results.Add(result);QuestionResolved?.Invoke(result);
            }
            if(views!=null&&views[slot])views[slot].HideImmediate();
        }
    }
    public void RestoreScenarioCheckpoint(int resultCount)
    {
        CancelActiveQuestion();
        if (results.Count > resultCount) results.RemoveRange(resultCount, results.Count - resultCount);
        currentScenarioRevision = -1; activeScenarioIndex = -1; activeScenarioId = null;
        scenarioFinished = false; resolvedInScenario = 0;
    }

    public void ResetForNewShift()
    {
        CancelActiveQuestion();results.Clear();scheduledRevisions.Clear();
        currentScenarioRevision=-1;activeScenarioIndex=-1;activeScenarioId=null;scenarioFinished=false;resolvedInScenario=0;
    }
    public void RestoreScenarioResults(int previousCount)
    {
        CancelActiveQuestion();
        previousCount=Mathf.Clamp(previousCount,0,results.Count);
        if(results.Count>previousCount)results.RemoveRange(previousCount,results.Count-previousCount);
        resolvedInScenario=0;scenarioFinished=false;
    }
}
