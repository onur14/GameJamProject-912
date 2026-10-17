# Kamu spotu çekimi

## Yeni akış

OYNA → büyük **AFETE HAZIRLIK** başlığı → bakanlık bilgilendirmesi → ÇEKİME BAŞLA → kısa 3–2–1 sayımı → evdeki dört hazırlık görevi → **EN KISA VİDEON BU MU?** → yeniden çekim veya yayın → dört güvenlik mesajından oluşan montaj → **AYLAR SONRA...** → sarsıntılı afet duyurusu → mevcut telefon / operatör akışı.

Bakanlık metni, afet bilincini artıracak kamu spotunu doğru ve mümkün olduğunca hızlı çekme görevini açıklar. Ödül koşulu da çekime başlamadan gösterilir.

## Süre ve yeniden çekim

- Sayaç geri sayımdan sonra başlar, dördüncü görev başarıyla tamamlandığı anda durur. Bilgilendirme, geri sayım, değerlendirme ve yayın süresi çekim süresine dahil değildir.
- Değerlendirmede mevcut çekim ile en kısa tamamlanmış çekimin süreleri dakika:saniye.salise olarak görünür.
- YENİDEN ÇEK bütün görevleri, eşya yerleşimlerini, hakları, işaretçileri, hazırlık sağlığını ve geçici hataları sıfırlar. Önceki en kısa çekim korunur.
- Daha kısa bir yeniden çekim **bir defalık +1 ambulans, +1 itfaiye, +1 kurtarma ekibi** kazandırır. Daha sonraki iyileştirmeler bu ödülü üst üste biriktirmez.
- Daha yavaş bir yeniden çekim, en kısa videoyu veya kazanılan ödülü silmez.
- Oyuncu ilk tamamlanmış çekimi de yeniden denemeden yayınlayabilir; bu durumda süre iyileştirme ödülü kazanılmaz.
- Eksik bir çekim yayınlanamaz. Yayın sırasında yeniden çekim ve ikinci yayın tıklaması kilitlenir.
- Yayın için en kısa tamamlanmış çekimin sağlık/hata sonucu kullanılır.

## Ekip ödülünün aktarılması

Ödül değerlendirmede gösterilir, yayın onaylandıktan sonra operatör sahnesindeki mevcut `DispatchResources` başlangıç stoklarına gerçekten eklenir. Başlangıç ve vardiya sıfırlama işlemleri aynı başlangıç + ödül değerlerini kullanır; aynı ödül ikinci kez stoklara eklenmez. Tam oyunu tekrar başlatmak veya ana menüden yeni oyun açmak süre geçmişini ve ödülü temizler.

## Bilinçlendirme fikri: dört güvenli davranış, tek kamu spotu

Her başarı, klibin kaydedildiği geri bildirimiyle kısa bir güvenlik mesajına dönüşür. Yayın animasyonu dört klibi sırayla vurgular; ilerleme çubuğu ve bağlantı / yayın durumu eşlik eder:

1. Su, fener ve ilk yardım malzemelerini hazır tut.
2. Çök, başını koruyarak kapan, sağlam masaya tutun.
3. Su basan alanlardan uzak, yüksek bir bölgeye geç.
4. Dedektörün pilini kontrol et; çalıştığını test et.

Böylece oyuncu yalnızca görev bitirmiş olmaz; yaptığı güvenli davranışların kısa bir kamu spotuna dönüştüğünü görür ve mesajları yayın sırasında tekrar okur. Bütün görevler doğru tamamlanmadan yayınlanamaz; yalnızca hızlı tıklamak yeterli değildir.

## Yayın ve zaman atlaması

Film şeridi çerçevesi, geri sayım, çekim saati, klip kartları, amber / yeşil vurgu, yayın ilerleme çubuğu ve kısa kararma kullanılır. Yeni raster görsel üretilmedi; mevcut ev, deprem, sel ve dedektör görselleri kullanılır.

Kamu spotunun ardından **AYLAR SONRA...** başlığı gösterilir. Sonra “Ülkede birden fazla afet yaşandı. Şimdi toplumu bilinçlendirme ve destek olma zamanı.” bilgisi gelir. Afet duyurusunda kısa, azalan piksel sarsıntısı ve çok hafif sıcak renkli ışık etkisi uygulanır. Ardından mevcut sahne geçişi ve normal telefon akışı devam eder.

## Dosyalar

- Yeni: `Assets/Scripts/Preparation/PreparationBroadcast.cs` — bilgilendirme, değerlendirme, yayın ve zaman atlaması.
- Güncellenen: `PreparationRun.cs` — süre, en iyi çekim, yeniden çekim ve tek seferlik ödül durumu.
- Güncellenen: `PreparationDirector.cs` — görevleri kayıt akışına bağlama ve otomatik operatör geçişini kaldırma.
- Güncellenen: `DispatchResources.cs` — gerçek başlangıç ekip ödülü.
- Güncellenen: `Assets/Editor/PreparationChecks.cs` — yeni akışın Play Mode kontrolleri.
- Sahne, prefab ve özgün görsel dosyalarının yeniden kurulması gerekmez; mevcut hazırlık yöneticisi yeni arayüzü çalışma sırasında oluşturur.

## Sınırlar

Video, oyun içindeki klip montajı ve gerçek görev tamamlama süresiyle temsil edilir. Harici bir video dosyası kaydı, dışa aktarım veya gerçek bir sosyal ağda yayın yapılmaz. Çekim geçmişi yalnızca mevcut oyun koşusunda tutulur; kalıcı kayıt eklenmedi.

## Testler

Gerçek Unity Play Mode çalıştırmasında **456 doğrulama geçti**. `Logs/Preparation/checks.txt` dosyası `COMPLETE`, `Library/Preparation/status.txt` dosyası `PASS` ile sonuçlandı. Yeni Play Mode hata kaydı: **0**.

Fiilen sınanan yeni davranışlar:

- OYNA önce bakanlık bilgilendirmesini gösterir; onaylanmadan görev veya sayaç başlamaz. Geri sayımın çift tetiklenmesi yalnızca tek çekim başlatır.
- Birinci çekim dört gerçek mini oyunla **18,33 sn** tamamlandı; değerlendirmede zamanın durduğu ve ödül verilmediği doğrulandı.
- İkinci çekimde bütün görevler yeniden oynandı: **10,82 sn** ile daha kısa çekim ödülü kazanıldı.
- Üçüncü çekim bilerek daha yavaş yapıldı; önceki en kısa süre ve kazanılmış ödül korundu.
- Eksik çekim yayınlanamadı. Çift yayın çağrısı ikinci bir yayın veya ödül oluşturmadı.
- Yayın montajındaki dört güvenlik mesajı, zaman atlaması ve afet duyurusundaki sarsıntı çalıştı.
- Operatör sahnesindeki gerçek ambulans, itfaiye ve kurtarma stokları kendi başlangıç değerlerinden tam birer fazla başladı. Kaynak sıfırlama işlemi ödülü biriktirmedi.
- Yayından sonra sekiz operatör vakası, 16 Quick Question ve mevcut vardiya raporu tamamlandı. Operatör aşamasında hazırlık mini oyunu açılmadı.
- Tekrar Oyna çekim geçmişini, görevleri ve ekip ödülünü temizledi; ana menüden yeni oyun da yeni bilgilendirmeden başladı.
- Geçişlerde tek aktif EventSystem ve görünür metinlerin alanlarına sığması kontrol edildi.

Bilgilendirme, ilk çekim değerlendirmesi, daha hızlı çekim ödülü, kamu spotu montajı, Aylar Sonra ve afet duyurusu ekran görüntüleri ayrıca görsel olarak incelendi. Önceki hazırlık/operatör testleri de aynı çalıştırmaya dahildir. Ayrı bir dağıtım build'i veya gerçek video dışa aktarımı test edilmedi; video dışa aktarımı bu özellik kapsamında değildir. İlk çekimi yeniden denemeden yayınlama dalı bu otomatik çalıştırmada ayrıca oynanmadı; test edilen yayın üçüncü çekim sonrasında yapıldı.
