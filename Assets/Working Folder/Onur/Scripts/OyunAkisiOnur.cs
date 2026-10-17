using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OyunAkisiOnur : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI GelenSoru;
    [SerializeField] private TextMeshProUGUI GelenCevap;
    [SerializeField] private GameObject Kabuletti;
    [SerializeField] private GameObject RedYedi;
    [SerializeField] private float animasyonSuresi = 0.4f;
    [SerializeField] private float mesajAraligi = 12f;
    [SerializeField] private float altKonum = -55f;
    [SerializeField] private OnurConversationView conversationView;
    [SerializeField] private DispatchCallFlow dispatchCallFlow;
    [SerializeField] private TextMeshProUGUI sahneGeriSayimi;
    [SerializeField] private TextMeshProUGUI sahneUyarisi;
    [SerializeField] private DispatchResources resources;
    [SerializeField] private DispatchResources.ResourceType wrongChoiceResource = DispatchResources.ResourceType.Police;
    [SerializeField] private DispatchResources.ResourceType correctChoiceResource = DispatchResources.ResourceType.Ambulances;
    private const float ConnectionEventChance = 0.3f;

    private readonly Queue<MesajIstegi> bekleyenMesajlar = new Queue<MesajIstegi>();
    private readonly List<RectTransform> mesajlar = new List<RectTransform>();
    private bool animasyonCalisiyor;
    private bool secimBekleniyor;
    private bool senaryoBasladi;
    private RectTransform mesajAlani;
    private Canvas sohbetKanvas;
    private TextMeshProUGUI geriSayimYazisi;
    private TextMeshProUGUI uyariYazisi;
    private Coroutine geriSayimRutini;
    public event System.Action<int> ScenarioCompleted;
    public event System.Action<bool> DecisionResolved;
    public event System.Action<DispatchResourceEffect> UnitDispatched;
    public int ScenarioRevision { get; private set; }
    public bool LastDecisionCorrect { get; private set; }
    public bool ManagedShift { get; set; }
    public bool WaitingForChoice => secimBekleniyor;
    public DispatchResources Resources => resources;
    private DispatchScenarioData currentScenario;
    private bool completionSent;
    public void ConfigureInitialScenario(DispatchScenarioData data) { currentScenario = data; }

    public void BeginScenario(DispatchScenarioData data)
    {
        StopAllCoroutines();
        animasyonCalisiyor = false;
        secimBekleniyor = false;
        geriSayimRutini = null;
        bekleyenMesajlar.Clear();
        completionSent = false;
        ScenarioRevision++;
        LastDecisionCorrect = false;
        currentScenario = data;
        senaryoBasladi = false;
        if (conversationView != null) conversationView.Clear();
        if (uyariYazisi != null) uyariYazisi.gameObject.SetActive(false);
        SenaryoyuBaslat();
    }

    private void CompleteScenario()
    {
        if (completionSent) return;
        completionSent = true;
        ButonlariGizle();
        if (conversationView != null && (ConnectionEventChance >= 1f || Random.value < ConnectionEventChance))
            StartCoroutine(CompleteAfterConnectionRecovery());
        else
            ScenarioCompleted?.Invoke(ScenarioRevision);
    }

    private IEnumerator CompleteAfterConnectionRecovery()
    {
        yield return conversationView.ShowCallEnded();
        yield return conversationView.RecoverConnection();
        ScenarioCompleted?.Invoke(ScenarioRevision);
    }

    private IEnumerator DataConversation()
    {
        foreach (var line in currentScenario.opening)
            yield return MesajVeBekle(line.text, line.fromOperator);
        secimBekleniyor = true;
        conversationView.SetChoices(currentScenario.choiceA, currentScenario.choiceB);
        geriSayimRutini = StartCoroutine(SecimGeriSayimi());
    }

    private IEnumerator DataOutcome(bool choiceB)
    {
        yield return MesajVeBekle("Operatör: " + (choiceB ? currentScenario.choiceB : currentScenario.choiceA), true);
        foreach (var line in choiceB ? currentScenario.outcomeB : currentScenario.outcomeA)
            yield return MesajVeBekle(line.text, line.fromOperator);
        CompleteScenario();
    }

    public DispatchResourceEffect GetChoiceEffect(bool choiceB)
    {
        if (currentScenario != null && !(choiceB ? currentScenario.dispatchB : currentScenario.dispatchA)) return default;
        var type = currentScenario != null ? (choiceB ? currentScenario.resourceB : currentScenario.resourceA)
            : (choiceB ? correctChoiceResource : wrongChoiceResource);
        return DispatchResourceEffect.ForTeam(type);
    }

    public void ResolveWithoutDispatch()
    {
        if (!secimBekleniyor || resources == null || GetChoiceEffect(false).CanApply(resources) || GetChoiceEffect(true).CanApply(resources)) return;
        secimBekleniyor = false; LastDecisionCorrect = false;
        GeriSayimiDurdur(); CompleteScenario();
    }

    private bool SpendForChoice(bool choiceB)
    {
        if (resources == null) return true;
        var effect = GetChoiceEffect(choiceB);
        if (effect.Apply(resources))
        {
            if (!effect.IsZero) UnitDispatched?.Invoke(effect);
            return true;
        }
        uyariYazisi.text = "Bu ekip şu anda müsait değil. Diğer yönlendirmeyi değerlendirin.";
        uyariYazisi.gameObject.SetActive(true);
        return false;
    }

    private const string OnayMetni = "Evet, bina yıkılmadan hemen merdivenlerden aşağı koşarak binayı terk edin!";
    private const string RedMetni = "HAYIR! Sakın merdivenlere koşmayın! İçeri dönün, sağlam bir eşyanın yanında ÇÖK-KAPAN-TUTUN yapın!";

    private struct MesajIstegi
    {
        public string Metin;
        public bool Sagdan;
    }

    private void Start()
    {
        if (conversationView != null)
        {
            if (dispatchCallFlow == null || sahneGeriSayimi == null || sahneUyarisi == null)
            {
                Debug.LogError("Entegre sahnenin telefon akışı, sayaç ve uyarı arayüzü atanmalı.", this);
                enabled = false;
                return;
            }

            sohbetKanvas = conversationView.GetComponentInParent<Canvas>(true);
            if (sohbetKanvas == null)
            {
                Debug.LogError("Konuşma kanvası bulunamadı.", this);
                enabled = false;
                return;
            }

            conversationView.Bind(this);
            geriSayimYazisi = sahneGeriSayimi;
            uyariYazisi = sahneUyarisi;
            geriSayimYazisi.gameObject.SetActive(false);
            uyariYazisi.gameObject.SetActive(false);
            StartCoroutine(DispatchBaslangici());
            return;
        }

        if (GelenSoru == null || GelenCevap == null || Kabuletti == null || RedYedi == null)
        {
            Debug.LogError("Mesaj şablonları ve seçim butonları atanmalı.", this);
            enabled = false;
            return;
        }

        sohbetKanvas = GelenSoru.GetComponentInParent<Canvas>(true);
        if (sohbetKanvas == null)
        {
            Debug.LogError("Konuşma kanvası bulunamadı.", this);
            enabled = false;
            return;
        }

        GelenSoru.gameObject.SetActive(false);
        GelenCevap.gameObject.SetActive(false);
        ButonlariGizle();

        GameObject alan = new GameObject("Mesaj Alanı", typeof(RectTransform), typeof(RectMask2D));
        mesajAlani = alan.GetComponent<RectTransform>();
        mesajAlani.SetParent(GelenSoru.transform.parent, false);
        mesajAlani.anchorMin = Vector2.zero;
        mesajAlani.anchorMax = Vector2.one;
        mesajAlani.offsetMin = Vector2.zero;
        mesajAlani.offsetMax = Vector2.zero;

        KanvasiSolUsteYerlestir();
        sohbetKanvas.gameObject.SetActive(false);
        BaslaButonunuOlustur();
    }

    private void KanvasiSolUsteYerlestir()
    {
        RectTransform rect = sohbetKanvas.GetComponent<RectTransform>();
        rect.localScale *= 0.5f;

        Camera kamera = sohbetKanvas.worldCamera != null ? sohbetKanvas.worldCamera : Camera.main;
        if (kamera == null) return;

        float uzaklik = Vector3.Dot(rect.position - kamera.transform.position, kamera.transform.forward);
        Vector3 ustSol = kamera.ViewportToWorldPoint(new Vector3(0f, 1f, uzaklik));
        float kenarBosluk = kamera.orthographicSize * 0.04f;
        rect.position = ustSol
            + kamera.transform.right * (rect.rect.width * rect.lossyScale.x / 2f + kenarBosluk)
            - kamera.transform.up * (rect.rect.height * rect.lossyScale.y / 2f + kenarBosluk);
    }

    private IEnumerator DispatchBaslangici()
    {
        yield return new WaitUntil(() => dispatchCallFlow.State == DispatchCallFlow.CallState.Conversation);
        SenaryoyuBaslat();
    }

    private void BaslaButonunuOlustur()
    {
        GameObject arayuz = new GameObject("Başlat Arayüzü", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas arayuzKanvas = arayuz.GetComponent<Canvas>();
        arayuzKanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        arayuzKanvas.sortingOrder = 100;

        GameObject butonNesnesi = new GameObject("Başla", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform butonRect = butonNesnesi.GetComponent<RectTransform>();
        butonRect.SetParent(arayuz.transform, false);
        butonRect.anchorMin = butonRect.anchorMax = Vector2.zero;
        butonRect.pivot = Vector2.zero;
        butonRect.anchoredPosition = new Vector2(16f, 16f);
        butonRect.sizeDelta = new Vector2(140f, 48f);
        butonNesnesi.GetComponent<Image>().color = new Color(0.13f, 0.36f, 0.64f);
        butonNesnesi.GetComponent<Button>().onClick.AddListener(() =>
        {
            butonNesnesi.SetActive(false);
            SenaryoyuBaslat();
        });

        GameObject yaziNesnesi = new GameObject("Başla Yazısı", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform yaziRect = yaziNesnesi.GetComponent<RectTransform>();
        yaziRect.SetParent(butonRect, false);
        yaziRect.anchorMin = Vector2.zero;
        yaziRect.anchorMax = Vector2.one;
        yaziRect.offsetMin = Vector2.zero;
        yaziRect.offsetMax = Vector2.zero;
        TextMeshProUGUI yazi = yaziNesnesi.GetComponent<TextMeshProUGUI>();
        yazi.font = GelenSoru.font;
        yazi.text = "Başla";
        yazi.fontSize = 22f;
        yazi.color = Color.white;
        yazi.alignment = TextAlignmentOptions.Center;
        yazi.raycastTarget = false;

        geriSayimYazisi = EkranYazisiOlustur("Geri Sayım", arayuz.transform, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(300f, 48f), 26f, Color.yellow);
        uyariYazisi = EkranYazisiOlustur("Uyarı", arayuz.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 100f), 48f, Color.red);
        uyariYazisi.text = "Çok yavaşsın!";
    }

    private TextMeshProUGUI EkranYazisiOlustur(string ad, Transform ebeveyn, Vector2 cipa, Vector2 konum, Vector2 boyut, float fontBoyutu, Color renk)
    {
        GameObject nesne = new GameObject(ad, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = nesne.GetComponent<RectTransform>();
        rect.SetParent(ebeveyn, false);
        rect.anchorMin = rect.anchorMax = cipa;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = konum;
        rect.sizeDelta = boyut;

        TextMeshProUGUI yazi = nesne.GetComponent<TextMeshProUGUI>();
        yazi.font = GelenSoru.font;
        yazi.fontSize = fontBoyutu;
        yazi.color = renk;
        yazi.alignment = TextAlignmentOptions.Center;
        yazi.raycastTarget = false;
        nesne.SetActive(false);
        return yazi;
    }

    public void SenaryoyuBaslat()
    {
        if (senaryoBasladi) return;
        senaryoBasladi = true;
        if (conversationView != null) conversationView.gameObject.SetActive(true);
        else sohbetKanvas.gameObject.SetActive(true);
        StartCoroutine(currentScenario != null ? DataConversation() : GirisDiyalogu());
    }

    public void SagdanMesajGoster(string metin)
    {
        MesajEkle(metin, true);
    }

    public void SoldanMesajGoster(string metin)
    {
        MesajEkle(metin, false);
    }

    public void KabulEdildi()
    {
        if (!secimBekleniyor) return;
        if (!SpendForChoice(false)) return;
        LastDecisionCorrect = currentScenario != null && !currentScenario.correctChoiceIsB;
        secimBekleniyor = false;
        DecisionResolved?.Invoke(LastDecisionCorrect);
        GeriSayimiDurdur();
        ButonlariGizle();
        StartCoroutine(currentScenario != null ? DataOutcome(false) : YanlisSecim());
    }

    public void Reddedildi()
    {
        if (!secimBekleniyor) return;
        if (!SpendForChoice(true)) return;
        LastDecisionCorrect = currentScenario == null || currentScenario.correctChoiceIsB;
        secimBekleniyor = false;
        DecisionResolved?.Invoke(LastDecisionCorrect);
        GeriSayimiDurdur();
        ButonlariGizle();
        StartCoroutine(currentScenario != null ? DataOutcome(true) : DogruSecim());
    }

    private IEnumerator GirisDiyalogu()
    {
        yield return MesajVeBekle("KAZAZEDE: Alo! ALO, ALO!!!! Kurban olayım sesimi duyun, yardım edin!", false);
        yield return MesajVeBekle("Operatör: 912 Acil Çağrı Merkezi, ben Operatör Melek. Sakin olun, isminizi, nerede olduğunuzu ve durumunuzu söyleyin.", true);
        yield return MesajVeBekle("KAZAZEDE: Ben Z-Zeynep... İzmir Bayraklı'dayız, buralarda yüksek katlı bir sitedeyiz, 7. kattayız! Bina beşik gibi sallanıyor şu an, durmuyor! Duvarlar çatladı, vitrin tuzla buz oldu!", false);
        yield return MesajVeBekle("Operatör: Zeynep Hanım, sakin kalmaya çalışın. Ekipleri bölgeye gönderiyorum. Yanınızda kimse var mı?", true);
        yield return MesajVeBekle("Zeynep: Kızım Elif var kucağımda, iki yaşında daha! Koridordayım şu an, daire kapısını zorla araladım. Karşı komşu fırladı merdivenlere koşuyor! 'Apartman yıkılacak, merdivenlerden aşağı koşun!' diye bağırıyor!", false);

        yield return MesajVeBekle("Zeynep: Operatör Hanım, ben de hemen Elif'i kucağıma sarıp merdivenlerden aşağı koşayım mı?! 7 kattan hemen fırlarız değil mi?!", false);
        if (conversationView == null) yield return MesajlariYukariTasi(105f);
        secimBekleniyor = true;
        if (conversationView != null) conversationView.SetChoices(OnayMetni, RedMetni);
        else
        {
            Kabuletti.SetActive(true);
            RedYedi.SetActive(true);
        }
        geriSayimRutini = StartCoroutine(SecimGeriSayimi());
    }

    private IEnumerator SecimGeriSayimi()
    {
        // Integrated dialogue choices wait for an explicit decision, never a timeout.
        if (conversationView != null) { geriSayimYazisi.gameObject.SetActive(false); yield break; }
        float kalanSure = 10f;
        geriSayimYazisi.gameObject.SetActive(true);

        while (kalanSure > 0f && secimBekleniyor)
        {
            geriSayimYazisi.text = "Kalan süre: " + Mathf.CeilToInt(kalanSure);
            yield return null;
            kalanSure -= Time.deltaTime;
        }

        geriSayimYazisi.gameObject.SetActive(false);
        geriSayimRutini = null;
        if (!secimBekleniyor) yield break;

        secimBekleniyor = false;
        ButonlariGizle();
        if (ManagedShift) { CompleteScenario(); yield break; }
        if (conversationView != null) conversationView.gameObject.SetActive(false);
        else sohbetKanvas.gameObject.SetActive(false);
        uyariYazisi.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        uyariYazisi.gameObject.SetActive(false);
        if (conversationView != null) conversationView.gameObject.SetActive(true);
        else sohbetKanvas.gameObject.SetActive(true);
        KonusmayiYenidenBaslat();
    }

    private void GeriSayimiDurdur()
    {
        if (geriSayimRutini != null)
        {
            StopCoroutine(geriSayimRutini);
            geriSayimRutini = null;
        }

        geriSayimYazisi.gameObject.SetActive(false);
    }

    private IEnumerator YanlisSecim()
    {
        yield return MesajVeBekle("Operatör: " + OnayMetni, true);
        yield return MesajVeBekle("Zeynep: Tamam koşuyorum! Allah'ım sen bizi koru! Merdiven boşluğundayız... Elif sıkı tutun annecim... Aaaah! Basamaklar sallanıyor, tutunamıyorum!", false);
        if (ManagedShift) { CompleteScenario(); yield break; }
        KonusmayiYenidenBaslat();
    }

    private void KonusmayiYenidenBaslat()
    {
        ButonlariGizle();
        secimBekleniyor = false;
        bekleyenMesajlar.Clear();

        foreach (RectTransform mesaj in mesajlar)
        {
            mesaj.gameObject.SetActive(false);
            Destroy(mesaj.gameObject);
        }

        mesajlar.Clear();
        if (conversationView != null) conversationView.Clear();
        StartCoroutine(GirisDiyalogu());
    }

    private IEnumerator DogruSecim()
    {
        yield return MesajVeBekle("Operatör: " + RedMetni, true);
        yield return MesajVeBekle("Operatör: Zeynep Hanım beni dinleyin! Daireye geri dönün. Salondaki o ağır bazanın veya çamaşır makinesinin yanına geçin. Yere çökün, Elif'i göğsünüze sarıp başınızı koruyun, sallantı bitene kadar kıpırdamayın!", true);
        yield return MesajVeBekle("Zeynep: Tamam... Tamam döndüm içeri! Yatak odasındaki bazanın yanına çöktük... Elif yanımda... Sallantı yavaşlıyor gibi...", false);
        yield return new WaitForSeconds(10f);
        yield return MesajVeBekle("Zeynep (10 sn sonra): Sarsıntı durdu operatör! Merdiven boşluğundan büyük bir gürültü geldi, galiba basamaklar çökmüş... İyi ki çıkmamışız! Biz iyiyiz, nefes alıyoruz, şimdi ne yapalım?", false);
        yield return MesajVeBekle("Operatör: Sarsıntı bittiğine göre vanaları kapatın, montunuzu alın ve binayı kontrollü tahliye için ekiplerimizi bekleyin.", true);
        CompleteScenario();
    }

    private IEnumerator MesajVeBekle(string metin, bool sagdan)
    {
        if (conversationView != null)
        {
            yield return conversationView.ShowMessage(metin, sagdan, animasyonSuresi);
            yield return conversationView.WaitForAdvance();
            yield break;
        }
        MesajEkle(metin, sagdan);
        yield return new WaitUntil(() => !animasyonCalisiyor && bekleyenMesajlar.Count == 0);
        yield return new WaitForSeconds(Mathf.Max(2f, metin.Length * 0.035f));
    }

    private IEnumerator MesajlariYukariTasi(float mesafe)
    {
        Vector2[] baslangiclar = new Vector2[mesajlar.Count];
        for (int index = 0; index < mesajlar.Count; index++)
            baslangiclar[index] = mesajlar[index].anchoredPosition;

        float gecenSure = 0f;
        float sure = Mathf.Max(0.01f, animasyonSuresi);
        while (gecenSure < sure)
        {
            gecenSure += Time.deltaTime;
            float oran = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(gecenSure / sure));
            for (int index = 0; index < mesajlar.Count; index++)
                mesajlar[index].anchoredPosition = baslangiclar[index] + Vector2.up * (mesafe * oran);
            yield return null;
        }
    }

    private void MesajEkle(string metin, bool sagdan)
    {
        if (string.IsNullOrEmpty(metin)) return;

        bekleyenMesajlar.Enqueue(new MesajIstegi { Metin = metin, Sagdan = sagdan });
        if (!animasyonCalisiyor && isActiveAndEnabled)
            StartCoroutine(MesajlariGoster());
    }

    private IEnumerator MesajlariGoster()
    {
        animasyonCalisiyor = true;

        while (bekleyenMesajlar.Count > 0)
        {
            MesajIstegi istek = bekleyenMesajlar.Dequeue();
            if (conversationView != null)
            {
                yield return conversationView.ShowMessage(istek.Metin, istek.Sagdan, animasyonSuresi);
                continue;
            }

            TextMeshProUGUI sablon = istek.Sagdan ? GelenCevap : GelenSoru;
            TextMeshProUGUI yeniMesaj = Instantiate(sablon, mesajAlani);
            yeniMesaj.gameObject.name = istek.Sagdan ? "Sağ Mesaj" : "Sol Mesaj";
            yeniMesaj.text = istek.Metin;
            yeniMesaj.raycastTarget = false;
            yeniMesaj.gameObject.SetActive(true);

            RectTransform rect = yeniMesaj.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300f, rect.sizeDelta.y);
            yeniMesaj.fontSize = 11f;
            yeniMesaj.margin = Vector4.zero;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Max(35f, yeniMesaj.GetPreferredValues(istek.Metin, rect.rect.width, 0).y));

            float hedefX = istek.Sagdan ? 25f : -25f;
            float girisX = (istek.Sagdan ? 1f : -1f) * ((RectTransform)rect.parent).rect.width;
            float yukselme = mesajlar.Count == 0 ? 0f : (mesajlar[mesajlar.Count - 1].rect.height + rect.rect.height) / 2f + mesajAraligi;
            Vector2[] baslangiclar = new Vector2[mesajlar.Count];
            for (int index = 0; index < mesajlar.Count; index++)
                baslangiclar[index] = mesajlar[index].anchoredPosition;

            rect.anchoredPosition = new Vector2(girisX, altKonum);
            float sure = Mathf.Max(0.01f, animasyonSuresi);
            float gecenSure = 0f;
            while (gecenSure < sure)
            {
                gecenSure += Time.deltaTime;
                float oran = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(gecenSure / sure));
                rect.anchoredPosition = Vector2.Lerp(new Vector2(girisX, altKonum), new Vector2(hedefX, altKonum), oran);
                for (int index = 0; index < mesajlar.Count; index++)
                    mesajlar[index].anchoredPosition = baslangiclar[index] + Vector2.up * (yukselme * oran);
                yield return null;
            }

            rect.anchoredPosition = new Vector2(hedefX, altKonum);
            for (int index = 0; index < mesajlar.Count; index++)
                mesajlar[index].anchoredPosition = baslangiclar[index] + Vector2.up * yukselme;
            mesajlar.Add(rect);

            float ustSinir = mesajAlani.rect.height / 2f;
            while (mesajlar.Count > 0 && mesajlar[0].anchoredPosition.y - mesajlar[0].rect.height / 2f > ustSinir)
            {
                Destroy(mesajlar[0].gameObject);
                mesajlar.RemoveAt(0);
            }
        }

        animasyonCalisiyor = false;
    }

    private void ButonlariGizle()
    {
        if (conversationView != null)
        {
            conversationView.SetChoicesVisible(false);
            return;
        }

        if (Kabuletti != null) Kabuletti.SetActive(false);
        if (RedYedi != null) RedYedi.SetActive(false);
    }
}
