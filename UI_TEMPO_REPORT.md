# Oyun temposu, diyalog yerleşimi ve canlı kaynak paneli

17 Ekim 2026 — gerçek çalışma dizini: `C:/Users/MSI/Documents/gamejam/GameJamProject-912`.

## Sonuç

Mevcut vatandaş ve başkan içerikleri korunarak SampleScene ile Onur'un entegre oyun sahnesindeki gösterim düzenlendi. Oyuncu satırları **DEVAM** ile ilerletir; entegre görüşmelerde otomatik okuma beklemesi ve seçim süresi aşımı yoktur. Tam olarak iki vatandaş seçeneği bulunur. Seçenekler gerçek sevk etkisini ve mevcut → sonraki kaynak değerini gösterir. Sağ panelde ambulans, itfaiye ve kurtarma sayıları hem görüşmede hem başkan yayınında görünür.

## 1. İncelenen sistemler ve değişen dosyalar

Mevcut telefon/yakınlaşma kontrolü `Assets/Working Folder/Taha and Yasin/DispatchCallFlow.cs`, vatandaş akışı `Assets/Working Folder/Onur/Scripts/OyunAkisiOnur.cs`, görünüm `OnurConversationView.cs`, tek kaynak yöneticisi ise aynı dizindeki `DispatchResources.cs`. Mevcut başkan sistemi ve görsel referansı korundu.

Bu görevde değiştirilen dosyalar:

- `Assets/Working Folder/Onur/Scripts/OyunAkisiOnur.cs`: entegre satırları oyuncu ilerletir; seçimde zaman aşımı yok; tek maliyet hesabı; kaynak yokken açık kayıtla ilerleme; yeni vaka başlangıcındaki eski seçim bayrağının temizlenmesi.
- `Assets/Working Folder/Onur/Scripts/OnurConversationView.cs`: mevcut TMP ve buton referanslarıyla tek okunabilir terminal satırı, iki seçenek, maliyet/sonuç önizlemesi, tıklama koruması ve gerektiğinde kontrollü kaydırma.
- `Assets/Editor/PresidentialBriefingChecks.cs`: mevcut otomatik kontrol, yeni DEVAM düğmesini kullanacak şekilde güncellendi. Üretim davranışına otomatik ilerleme eklenmedi.
- `Assets/Scenes/SampleScene.unity`.
- `Assets/Working Folder/Onur/Onur Test Place Integrated.unity`.
- `Assets/Working Folder/Taha and Yasin/AnaMenu.unity`: yalnızca mevcut fade süreleri ayarlandı; OYNA/ÇIKIŞ olayları korundu.

Eklenen dosyalar:

- `Assets/Scripts/Presidential/DispatchResourceEffect.cs`: mevcut sevk kuralından türetilen geçici etki değeri; kaynak deposu değildir.
- `Assets/Scripts/Presidential/DispatchResourceHud.cs`: tek kaynak yöneticisinin `Changed` olayını izleyen canlı panel.
- `Assets/Editor/DispatchTempoInstaller.cs`: sahnedeki UI referansları, ölçülmüş anchor'lar ve tempo ayarları.
- `Assets/Editor/DispatchTempoChecks.cs`: gerçek UI tıklamalarıyla kontrol ve yerleşim ölçümleri.
- Unity'nin ilgili `.meta` dosyaları ve bu rapor.

Mevcut prefab varlıkları, vatandaş veri varlıkları, kaynak Markdown/JSON içeriği, başkan veri varlıkları ve teklif değerleri değiştirilmedi. Bu görevde `DispatchResources.cs`, `DispatchCallFlow.cs`, ana menü kodu ve başkan kontrol kodu yeniden yazılmadı. Sahne örneklerinin mevcut serileştirilmiş süre/yerleşim ayarları kullanıldı. Font asset referansları korundu; yeni font veya başkan resmi üretilmedi.

## 2. Tempo ayarları

| İşlem | Önce | Şimdi |
| --- | --- | --- |
| Ana menü fade-out | 0,45 sn | 0,30 sn |
| Oyun fade-in | 0,65 sn | 0,30 sn |
| İlk çağrı gecikmesi | 1,50 sn | 0,55 sn |
| Telefon sonrası yakınlaşma | 0,75 sn | 0,42 sn |
| Konuşma paneli fade | 0,25 sn | 0,18 sn |
| Entegre vatandaş satırı beklemesi | En az 2 sn; uzunluğa göre daha fazla | Zorunlu bekleme yok; DEVAM ile ilerleme |
| Vatandaş seçim süresi | 10 sn; sonunda vaka sonucu | Süresiz, açık oyuncu kararı |
| Vatandaş metni | Balon kaydırma + otomatik bekleme | Varsayılan anında görünür; isteğe bağlı hızlı yazı efekti |
| Başkan yazı efekti | 38 karakter/sn | 120 karakter/sn; tıklama önce satırı tamamlar |
| Sağ panel değişim vurgusu | Yok | 0,35 sn |

Eski balon animasyon parametresi sahnelerde 0,40 yerine 0,15 sn olarak kaydedildi; yeni terminal satır görünümü balon kaydırma animasyonunu bekletmez. Başkanın kısa mevcut fade/onay animasyonları korunur. Entegre olmayan eski test görünümünün otomatik bekleme kodu geri uyumluluk için durur; ana sekizli oyun akışı bu yolu kullanmaz.

Telefon kendiliğinden cevaplanmaz: mevcut telefona tıklama davranışı korunmuştur. Satır okunduktan sonra DEVAM'a tıklanır. Yazı efekti etkinleştirilirse ilk tıklama satırı tamamlar, sonraki tıklama ilerler. 0,22 sn ortak tıklama koruması aynı hareketin sonraki satıra/seçime taşmasını engeller.

## 3. Diyalog konumu ve okunabilirlik

Gerçek Unity RectTransform hiyerarşisi ve orijinal 1672×941 oda görseli incelendi. Ortadaki monitördeki sol menü ve çerçeve hariç tutularak boş sağ bölüm kullanıldı.

`RoomArtwork/ConversationViewport` anchor'ları:

- Min: `(0.435, 0.327)`
- Max: `(0.671, 0.573)`
- Offset'ler: sıfır.

Bunlar ekran piksel tahmini değildir; görselin ölçülen normalize edilmiş alanına bağlıdır. Mevcut `DispatchCallFlow.FitConversation()` tasarım panelini bu alana sığdırır; oda yakınlaşması ve ekran oranı değişiminde aynı artwork koordinatlarını izler. Canvas Scaler düzeni korunmuştur.

Son satır oyuncu ilerletene kadar kalır; önceki konuşma yeni bir balonun zamanlayıcısıyla kaybolmaz. Konuşma için mevcut TMP fontu kullanılır. Eski otomatik 12–20 punto sınırı kaldırılmıştır. Normal satır 34, seçeneklerin üstünde kalan son soru 28 tasarım birimindedir; seçenek etiketleri mevcut fontlarıyla 23–26 arasında sığdırılır. Fiziksel görünüm mevcut panel ölçeğine bağlıdır.

Çok uzun satırlar maskeli ScrollRect içinde kalır. Seçenek ekranında kaydırma gerekirse fare tekerleği ipucu görünür. D-01'in son sorusu, iki seçeneği ve maliyetleri üç test oranında kaydırmadan birlikte gösterildi. Her senaryonun bütün satırlarının her oranda kaydırmasız olması iddia edilmez.

Ölçülen son ekran sınırları (Unity ekran koordinatında alt-sol başlangıç):

| Ekran | Diyalog x/y/genişlik/yükseklik | Sağ panel x/y/genişlik/yükseklik |
| --- | --- | --- |
| 1600×900, 16:9 | 653,24 / 228,46 / 632,53 / 425,09 | 1368 / 324 / 216 / 522 |
| 1200×900, 4:3 | 489,87 / 283,75 / 474,65 / 318,99 | 1026 / 324 / 162 / 522 |
| 2100×900, 21:9 | 903,24 / 228,46 / 632,53 / 425,09 | 1795,50 / 324 / 283,50 / 522 |

Üçünde de diyalog ekran içinde, panellerin kesişimi yok ve ilk karar etiketlerinde TMP metin taşması yok.

## 4. Gerçek seçenek maliyetleri

Kaynak etkisi **mevcut** `DispatchScenarioData.dispatchA/dispatchB` ve `resourceA/resourceB` alanlarından okunur. Sevk bayrağı kapalıysa etki sıfırdır. Açık olduğunda mevcut davranıştaki ilgili tek ekibin `-1` sevki kullanılır. Eski veri dışı görünüm için mevcut `wrongChoiceResource/correctChoiceResource` alanları kullanılır.

`OyunAkisiOnur.GetChoiceEffect()` hem gösterime hem gerçek uygulamaya hizmet eder. `SpendForChoice()` aynı etkinin `Apply()` metoduyla mevcut `DispatchResources.TryApply()` API'sini bir kez çağırır. Gösterim için ikinci bir azaltma veya ayrı bir kaynak sayacı eklenmedi. Etki yapısı üç kaynağı da ifade edebilir; mevcut senaryolara yeni/maliyetli veri atanmadı.

Örnek gerçek D-01 görünümü:

- A: `Kurtarma -1 (10 → 9)`.
- B: `Ambulans -1 (10 → 9)`.
- Bilgi/hazırlık yönlendirmesinde mevcut sevk bayrağı kapalıysa: `Ekip sevki yok · Kaynak etkisi: 0`.

Seçilemeyen düğme devre dışıdır ve yetersizlik gerekçesi görünür. Her iki düğme de kullanılamıyorsa aynı iki özgün seçenek ekranda kalır; alt durum eylemi **“EKİP YOK · Sevk yapılamadı — vakayı kaydet ve ilerle”** olur. Oyuncu bunu onaylayana kadar beklenir. Kayıt başarısız yönlendirme olarak ilerler; ekip tüketilmez/eklenmez ve özgün karar metinleri değiştirilmez. Bu eylem üçüncü bir vatandaş diyaloğu değildir.

## 5. Canlı sağ panel ve başkan uyumu

Sağ panel doğrudan aynı bileşenin `Ambulances`, `Firefighters`, `Police` değerlerini okur. Üçüncü başlık oyuncuya **KURTARMA EKİBİ** olarak gösterilir. `Police` ve serileştirilmiş eski kaynak alanları yeniden adlandırılmadı.

`DispatchResources.Changed` olayı; sevk, tahsis, mevcut artırma API'si ve yeni vardiya sıfırlamasında paneli günceller. Paneldeki önceki değerler yalnızca değişim animasyonu için görünüm anlık görüntüsüdür; oyun kaynağı olarak kullanılmaz. Artış yeşil, azalış kırmızı ve kısa `+/-` etiketiyle gösterilir; sıfırda **EKİP KALMADI** uyarısı kalır.

Eski sayaç metinleri silinmedi; serileştirilmiş bağlantıları korunarak sahnede gizlendi. Yeni panel aynı değerleri olay üzerinden gösterir. Yakınlaşma kökünün dışında, mevcut Canvas altında sıralaması yükseltilmiş bir UI katmanıdır. Yeni EventSystem yoktur. Başkan penceresine sağda alan bırakılır; konuşma ve tahsis sırasında panel görünür kalır. Başkan tahsisleri hâlâ mevcut kaynak API'sini kullanır ve aynı paneli günceller. Konuşma/tahsis içeriği değişmedi.

## 6. İçeriğin değişmediğinin doğrulanması

Başlangıçta 12 vatandaş varlığı, 4 başkan varlığı ve CitizenSource.json için SHA-256 özetleri kaydedildi. Son karşılaştırmada **17 dosyanın tamamı aynı**, değişen içerik dosyası sayısı **0**.

Ana sıra değişmedi: `D-01, D-02, Y-02, S-01, T-01, F-01, K-01, K-02`. Yeni vaka eklenmedi; alternatif kayıtlar silinmedi. Başkan konuşmaları ve sekiz teklifin sayıları aynı kaldı. Kaynak Markdown dosyasına dokunulmadı.

## 7. Gerçekten yapılan testler

Unity 6000.6.5f1, gerçek Editor/Play Mode:

- Okunmayan satırın oyuncu tıklamadan değişmemesi.
- Gerçek DEVAM düğmesinde hızlı çift tıklamayla yalnızca tek satır ilerlemesi.
- İsteğe bağlı typewriter'da ilk tıklamanın yalnızca satırı tamamlaması.
- Gerçek vatandaş seçenek düğmesinde iki tıklamaya karşı tek kaynak tüketimi.
- Maliyet/önizleme metninin gerçek D-01 kuralıyla eşleşmesi.
- Sevkte ve tahsis API'sinde yeni panelin üç gerçek değerle aynı anda eşleşmesi.
- İki kaynak-yetersiz seçeneğin kapanması, yüksek oyun zaman ölçeğinde bile otomatik karar/zaman aşımı olmaması.
- Ekip yokken açık onayla tek vaka ilerlemesi ve gizli kaynak artışı olmaması.
- Sıfır kaynakta K-01'in gerçek ücretsiz bilgi seçeneğinin kullanılabilmesi ve ekip tüketmemesi.
- Tek EventSystem.
- Mevcut sekizli başkan kontrolü yeni DEVAM düğmesini kullanacak şekilde yeniden çalıştırıldı: sekiz gerçek senaryo, 2/4/6/8 yayınları, final, tek tahsis, atomiklik, kaynak sürekliliği ve yeni vardiya sıfırlaması geçti.
- 16:9, 4:3, 21:9 gerçek Game View görüntüleri alınıp incelendi. Panellerin fiziksel sınırları da ölçüldü.
- Başkan yayını + canlı sağ panel birlikte görüntülendi.
- Yeni 0,30/0,30 menü süreleri Inspector'dan okundu. Gerçek OYNA düğmesi tetiklendi; fade başladı, SampleScene açıldı, fade tamamlandı ve telefon `IncomingCall` durumuna geldi. ÇIKIŞ olay bağlantısı korundu.
- Son kontrol aralığında Console'da üretim koduna ait derleme/çalışma zamanı hatası görülmedi. C# farklarının boşluk kontrolü temiz.

Yerel kanıtlar:

- `Logs/TempoChecks/all-results.txt`: tempo kontrolleri ve sekizli başkan regresyonu.
- `Logs/ui-tempo-content-hashes.json`: içerik özetleri.
- `Logs/TempoChecks/final-16x9.png`.
- `Logs/TempoChecks/final-4x3-1.png`.
- `Logs/TempoChecks/final-21x9.png`.
- `Logs/TempoChecks/president-live-hud-16x9.png`.

Son küçük görsel düzenleme, seçenek üstündeki soru boyutunun 28 olarak sabitlenmesi ve kaydırma ipucudur. Bunun ardından derleme ve üç oranlı görsel/sınır kontrolü tekrarlandı; daha önce geçen kaynak ve başkan mantığı değiştirilmedi.

## 8. Süre hedefi ve sınırlar

Hızlı kontrol tıklamalarıyla ilk karar arayüzüne erişim yaklaşık **1,34 sn** ölçüldü. Bu, sıfırlanan ilk konuşmanın UI gecikmesidir; menü, telefon ve insan okuma süresini kapsamaz. İnsan oynanış süresi ölçümü değildir.

İlk açılışta 110 kelime vardır. Ana vatandaş akışı iki seçeneğin de metni dahil yaklaşık 1.303, başkan satırları/kapanışları/plan açıklamaları yaklaşık 391 kelimedir. Seçilen operatör yanıtının tekrar gösterimi ve karar verme süresi ayrıca eklenir. Yaklaşık 1.700+ kelimeyi tümüyle okumak 200–250 kelime/dakika hızında tek başına 7–9 dakika isteyebilir. 5–6 dakikalık jüri deneyimi daha hızlı okuma veya incelemeyle mümkündür; metinlere dokunmadan kesin süre garantisi verilmez.

Bağımsız Windows build, her senaryonun her dalında her ekran oranı, farklı DPI/çok küçük ekranlar ve 5–6 dakikalık insan jüri oturumu ayrıca test edilmedi. Genel uzun metin taşması için kontrollü kaydırma vardır; her satırın her ekranda kaydırmasız olması iddia edilmez. Gerçek işletim sistemi ÇIKIŞ işlemi uygulanmadı, bağlantısı kontrol edildi.

Bir son Play Mode geçişinde Unity geçici yanıt gecikmesi yaşadı; editör kendiliğinden yanıt verdi. Zorla kapatma yapılmadı. Normal Stop/Play sonrası kalan görüntüler ve menü geçişi tamamlandı; bu durum içerik değişikliğine yol açmadı.

## 9. Deneme

`SampleScene` açıkken Play → telefona tıkla → **DEVAM** ile satırları oku → kaynak etkisine bakıp iki seçenekten birini seç. Artık seçim süresi dolmaz. İki vakadan sonra aynı başkan yayını açılır; sağdaki ekipler görünür kalır. Test bitince Play'i kapat.

Gerekirse editör menüsü: **GameJam → Tempo ve Kaynak Arayüzünü Güncelle**. Bu menü sahne örneklerinin yerleşim/sürelerini ayarlar; veri varlıklarını yeniden yazmaz.
