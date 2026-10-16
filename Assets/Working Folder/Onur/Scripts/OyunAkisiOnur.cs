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
    [SerializeField] private float mesajAraligi = 8f;
    [SerializeField] private float altKonum = -55f;

    private readonly Queue<MesajIstegi> bekleyenMesajlar = new Queue<MesajIstegi>();
    private readonly List<RectTransform> mesajlar = new List<RectTransform>();
    private bool animasyonCalisiyor;
    private bool secimBekleniyor;
    private RectTransform mesajAlani;

    private const string OnayMetni = "Evet, bina yıkılmadan hemen merdivenlerden aşağı koşarak binayı terk edin!";
    private const string RedMetni = "HAYIR! Sakın merdivenlere koşmayın! İçeri dönün, sağlam bir eşyanın yanında ÇÖK-KAPAN-TUTUN yapın!";

    private struct MesajIstegi
    {
        public string Metin;
        public bool Sagdan;
    }

    private void Start()
    {
        if (GelenSoru == null || GelenCevap == null || Kabuletti == null || RedYedi == null)
        {
            Debug.LogError("Mesaj şablonları ve seçim butonları atanmalı.", this);
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

        StartCoroutine(GirisDiyalogu());
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
        secimBekleniyor = false;
        ButonlariGizle();
        StartCoroutine(YanlisSecim());
    }

    public void Reddedildi()
    {
        if (!secimBekleniyor) return;
        secimBekleniyor = false;
        ButonlariGizle();
        StartCoroutine(DogruSecim());
    }

    private IEnumerator GirisDiyalogu()
    {
        yield return MesajVeBekle("KAZAZEDE: Alo! ALO, ALO!!!! Kurban olayım sesimi duyun, yardım edin!", false);
        yield return MesajVeBekle("Operatör: 912 Acil Çağrı Merkezi, ben Operatör Melek. Sakin olun, isminizi, nerede olduğunuzu ve durumunuzu söyleyin.", true);
        yield return MesajVeBekle("KAZAZEDE: Ben Z-Zeynep... İzmir Bayraklı'dayız, buralarda yüksek katlı bir sitedeyiz, 7. kattayız! Bina beşik gibi sallanıyor şu an, durmuyor! Duvarlar çatladı, vitrin tuzla buz oldu!", false);
        yield return MesajVeBekle("Operatör: Zeynep Hanım, sakin kalmaya çalışın. Ekipleri bölgeye gönderiyorum. Yanınızda kimse var mı?", true);
        yield return MesajVeBekle("Zeynep: Kızım Elif var kucağımda, iki yaşında daha! Koridordayım şu an, daire kapısını zorla araladım. Karşı komşu fırladı merdivenlere koşuyor! 'Apartman yıkılacak, merdivenlerden aşağı koşun!' diye bağırıyor!", false);

        yield return MesajVeBekle("Zeynep: Operatör Hanım, ben de hemen Elif'i kucağıma sarıp merdivenlerden aşağı koşayım mı?! 7 kattan hemen fırlarız değil mi?!", false);
        yield return MesajlariYukariTasi(105f);
        secimBekleniyor = true;
        Kabuletti.SetActive(true);
        RedYedi.SetActive(true);
    }

    private IEnumerator YanlisSecim()
    {
        yield return MesajVeBekle("Operatör: " + OnayMetni, true);
        yield return MesajVeBekle("Zeynep: Tamam koşuyorum! Allah'ım sen bizi koru! Merdiven boşluğundayız... Elif sıkı tutun annecim... Aaaah! Basamaklar sallanıyor, tutunamıyorum!", false);
    }

    private IEnumerator DogruSecim()
    {
        yield return MesajVeBekle("Operatör: " + RedMetni, true);
        yield return MesajVeBekle("Operatör: Zeynep Hanım beni dinleyin! Daireye geri dönün. Salondaki o ağır bazanın veya çamaşır makinesinin yanına geçin. Yere çökün, Elif'i göğsünüze sarıp başınızı koruyun, sallantı bitene kadar kıpırdamayın!", true);
        yield return MesajVeBekle("Zeynep: Tamam... Tamam döndüm içeri! Yatak odasındaki bazanın yanına çöktük... Elif yanımda... Sallantı yavaşlıyor gibi...", false);
        yield return new WaitForSeconds(10f);
        yield return MesajVeBekle("Zeynep (10 sn sonra): Sarsıntı durdu operatör! Merdiven boşluğundan büyük bir gürültü geldi, galiba basamaklar çökmüş... İyi ki çıkmamışız! Biz iyiyiz, nefes alıyoruz, şimdi ne yapalım?", false);
        yield return MesajVeBekle("Operatör: Sarsıntı bittiğine göre vanaları kapatın, montunuzu alın ve binayı kontrollü tahliye için ekiplerimizi bekleyin.", true);
    }

    private IEnumerator MesajVeBekle(string metin, bool sagdan)
    {
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
            yeniMesaj.fontSize = 12f;
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
        if (Kabuletti != null) Kabuletti.SetActive(false);
        if (RedYedi != null) RedYedi.SetActive(false);
    }
}
