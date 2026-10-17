# Afete Hazırlık — uygulama raporu

## Akış

Ana menü → **Bakanlık kamu spotu bilgilendirmesi** → **Afete Hazırlık çekimi** → süre değerlendirmesi / yeniden çekim → kamu yayını montajı → aylar sonra / afet duyurusu → mevcut operatör vardiyası → mevcut vardiya raporu.

OYNA mevcut menü geçiş sürelerini kullanarak `Assets/Scenes/Preparation.unity` sahnesini açar. İlk olarak büyük AFETE HAZIRLIK başlığıyla bakanlığın kamu spotu çekim isteği açıklanır. Dört etkinlik başarıyla tamamlanmadan video yayınlanamaz. Son etkinlikten sonra süre değerlendirmesi açılır; oyuncu yeniden çekebilir veya en kısa tamamlanmış videosunu yayınlayabilir. Yayın montajı ve “Aylar sonra” duyurusundan sonra mevcut `SceneFadeTransition` ile operatör sahnesi açılır. Ayrıntılar `Docs/PublicServiceRecording.md` içindedir.

## Sahneler ve prefablar

- **Yeni:** `Assets/Scenes/Preparation.unity`. Hazırlık yöneticisi, mevcut çanta penceresinin prefab örneği, kamera ve tek EventSystem içerir. Arayüz çalışma sırasında oluşturulur.
- **Değiştirilen:** `Assets/Working Folder/Taha and Yasin/Onur/Onur Test Place Integrated.unity`. Eski tablet bileşeni ve mini oyun girişleri kapatıldı.
- **Build listesi:** Hazırlık sahnesi ana menü ile operatör sahnesi arasına eklendi. Var olan rapor sahnesi etkin bırakıldı.
- Ana menü görseli ve düğme yerleşimi yeniden tasarlanmadı. Menünün mevcut `AnaMenü.OyunBaslat` davranışı güncellendi.
- **Yeni prefab yok.** Mevcut `MiniGameWindow.prefab`, içindeki `BagMiniGame.prefab`, eşya ve çanta görselleri kullanılır. Asıl çanta prefabının kuralları veya eşya listesi değiştirilmedi.

## Etkinlikler

1. **Acil durum çantası:** Yatak odasındaki 1 numaralı nokta. Mevcut `BagMiniGame`, `BagDraggableItem` ve `BagDropZone` kullanılır. Tıklama ve sürükle-bırak, doğru eşyalar, dikkat dağıtıcı eşyalar, üç hak, kontrol düğmesi ve sarsılma korunur. Başarısız olunca haritadan tekrar açılarak üç hakla yeniden denenebilir. Yalnızca bu sahnedeki arka plan rengi yeni palete uyarlandı.
2. **Çök / Kapan / Tutun:** Salondaki masa, 2 numaralı nokta. Üç rastgele ok soldan sağa izlenir. Her doğru basışta aynı karakterin ilgili pozu gösterilir; ok sallanıp küçülerek kaybolur. Son isteğe uygun olarak her adım için dört saniye verilir. Yanlış tuş kırmızı geri bildirim verir; süre dolunca aynı adım yeniden denenir. Tüm etkinlik anında kaybedilmez. Küçülen süre çubuğu, hafif lamba/karakter hareketi ve çatlak görseli vardır.
3. **Sel haritası:** Çalışma odasındaki harita, 3 numaralı nokta. Nehir, alçak su basmış yerleşimler ve yükselen tepe görünür. Dört nokta seçim öncesinde aynı kırmızı durumdadır. Yanlış seçim açıklama ve yeniden deneme verir. Yüksek tepedeki seçim başarılıdır; yalnızca seçimden sonra yeşil doğrulama gösterilir.
4. **Duman dedektörü:** Koridordaki 4 numaralı nokta. Kapak açılır, eski pil çıkarılır, yeni pil seçilir, boş yuvaya tıklanarak takılır ve kapak kapatılır. LED kısa süre yanıp söner. Her aşamada yalnızca gerekli parça etkileşimdedir; çift tıklama birden fazla aşamayı atlatmaz.

## Durum ve sağlık

`PreparationRun` dört basit durumu tutar: `bagComplete`, `dropCoverHoldComplete`, `floodComplete`, `smokeDetectorComplete`. Dördü de doğruysa `preparationComplete` olur. Başarılı görev tekrar sayılmaz; işaretçisi soluk yeşile döner ve yeniden açılmaz. Aynı anda yalnızca bir etkinlik açılabilir.

Başarı şehir sağlığına +5 uygular; sağlık %100 ile sınırlıdır. Çantadaki eşya yerleşimi mevcut ±2 puan davranışını kullanır. Selde yanlış seçim ve depremde süre aşımı -2 uygular; yanlış ok tek başına görevi bitirmez. Hazırlığın sonunda sağlık mevcut `CityHealth` sistemine aktarılır. Hazırlık sırasında oyun sonu ekranı açılmaz; etkinlikler tekrar denenebilir. Sağlık sıfıra inmişse operatör vardiyasına en az %1 ile girilir.

Operatör raporundaki hazırlık görevleri sayısı bu dört tamamlanma kaydından alınır. Hazırlık hataları rapor verisine taşınır.

## Operatör bölümünden kaldırılan girişler

- Eski çanta ve ev güvenliği giriş düğmeleri pasif.
- `DispatchTablet` operatör sahnesinde pasif; çalışma zamanındaki koruma da tablet arayüzünü oluşturmaz.
- `MiniGameWindow.OpenBag/OpenSafety`, hazırlık yöneticisi atanmamış bir operatör penceresinde hiçbir panel açmaz. Böylece eski bağlı olaylar da mini oyun başlatamaz.
- Hazırlık oyunları senaryo, konuşma veya çağrı tamamlanma olaylarına bağlanmadı.
- Telefon, diyalog, kaynak harcama ve Quick Questions mantığı yeniden yazılmadı. Quick Questions operatör aşamasında kalır.

## Tekrar oynama ve ana menü

Hem vardiya raporundaki hem başarısızlık ekranındaki Tekrar Oyna, hazırlık sahnesinden yeni tam koşu başlatır. Dört görev, geçici sağlık/hata kaydı ve önceki rapor temizlenir. Eski operatör sahnesi kaldırıldığı için kaynaklar, konuşma ve Quick Questions kayıtları bir sonraki operatör sahnesinde kendi mevcut başlangıç değerleriyle oluşur. Ana menüden yeni oyun da hazırlıktan başlar.

## Oluşturulan dosyalar

- `Assets/Scripts/Preparation/PreparationRun.cs`
- `Assets/Scripts/Preparation/PreparationUI.cs`
- `Assets/Scripts/Preparation/PreparationDirector.cs`
- `Assets/Scripts/Preparation/PreparationBroadcast.cs`
- `Assets/Scripts/Preparation/FloodPreparationGame.cs`
- `Assets/Scripts/Preparation/SmokeDetectorPreparationGame.cs`
- `Assets/Scripts/Presidential/EarthquakePreparationGame.cs`
- `Assets/Editor/PreparationInstaller.cs`
- `Assets/Editor/PreparationChecks.cs`
- `Assets/Editor/EarthquakeSheetImporter.cs`

Değiştirilen çalışma zamanı dosyaları: `AnaMenü.cs`, `MiniGameWindow.cs`, `DispatchTablet.cs`, `CityHealth.cs`, `DispatchResources.cs`, `RunResultView.cs`, `ShiftReportView.cs`, `ShiftReportData.cs`.

## Arayüz ve görseller

Çalışma sırasında tek hazırlık Canvas'ı; ev haritası; dört küçük numaralı işaretçi; görev durumları; şehir sağlığı; sakin REC göstergesi ve köşe işaretleri; ortak etkinlik kartı; Eve Dön düğmesi; üç yeni etkinlik görünümü oluşturulur. Ayrı bir EventSystem ekleyen çalışma zamanı kodu yoktur; yeni sahnedeki tek EventSystem kullanılır.

Yeni görseller: `Assets/Resources/Disaster/HouseMap.png`, `FloodMap.png`, `DetectorSheet.png`. Gönderilen karakter ve ok görseli `EarthquakeSheet.png` olarak kopyalanıp sprite bölgeleriyle kullanılır. Mevcut çanta görselleri ve Taha/Yasin pixel fontu yeniden kullanılır. Yeni görseller ImageGen ile üretildi; tam istemler `Docs/PreparationAssets.md` içindedir. Noktasal filtreleme, sıkıştırmasız içe aktarım ve mipmap kapalı ayarları uygulanır.

## Doğrulama

Çalışma zamanı ve editör kodları derleme kontrolünden geçti. Gerçek Play Mode senaryosu `PreparationChecks` ile mevcut Unity oturumunda çalıştırıldı. Kamu spotu çekim akışı da eklenmiş güncel çalıştırmada **456 doğrulama geçti**. Sonuç: `Logs/Preparation/checks.txt` → `COMPLETE`. Ekran görüntüleri aynı klasördedir; yeni çekim / yayın testlerinin ayrıntıları `Docs/PublicServiceRecording.md` içindedir.

Gerçekten çalıştırılan kontroller:

- Mevcut OYNA düğmesine gerçek arayüz ışın testiyle tıklama; hazırlık sahnesinin açılması.
- Tepeden ev haritası, REC göstergesi, dört işaretçi ve görünür metinlerin alanlarına sığması.
- Mevcut çantanın üç hakkı; art arda kontrolün tek hak tüketmesi; doğru/yanlış eşya listesinin korunması; mevcut sürükle-bırak ve tıklamayla yerleştirme; başarı sonrası haritaya dönüş.
- Aynı anda ikinci etkinliğin açılamaması; tamamlanan etkinliğin tekrar sayılamaması.
- Gerçek klavye durum olaylarıyla depremde süre aşımı ve yeniden deneme, yanlış ok, üç doğru ok ve karakterin üç aşaması.
- Selde yanlış nokta, yeniden deneme ve yüksek noktanın doğru tanınması.
- Dedektörde açma, çift tıklama koruması, eski pili çıkarma, yeni pili seçme, yuvaya takma, kapağı kapatma ve tamamlama.
- Bir, iki veya üç tamamlanmada operatörün başlamaması; dört tamamlanmada çekim değerlendirmesi; açık yayın onayından sonra geçiş.
- Operatörde çanta/ev güvenliği düğmelerinin görünmemesi ve eski açma çağrılarının etkisiz olması. Sekiz vaka boyunca hazırlık mini oyunu açılmadığının sürekli kontrolü.
- Mevcut telefon, arayan bilgisi, diyalog ve kaynak sistemleriyle **sekiz gerçek operatör vakasının tamamlanması**; **16 Quick Question** sonucunun rapora ulaşması.
- Mevcut vardiya raporunun açılması ve dört hazırlık görevinin rapora taşınması.
- Raporun Tekrar Oyna düğmesinin dört hazırlığı sıfırlaması; önceki operatör kaynak/Quick Question nesnelerinin kaldırılması; ana menüye dönüş ve yeni OYNA'nın yeniden hazırlıktan başlaması.
- Hazırlık, operatör, rapor, tekrar oynama ve yeni oyunda tek aktif EventSystem.
- Testin kaydettiği yeni Play Mode hata sayısı: **0**.

Harita, çanta, deprem, sel, açık/kapalı dedektör, operatör ve vardiya raporu ekran görüntüleri ayrıca görsel olarak incelendi. İlk denemede dedektörün pasif pil yuvasının kapak tıklamasını engellediği bulundu; etkileşim alanı düzeltildikten sonra yukarıdaki tam akış başarıyla yeniden çalıştırıldı.

Ayrı bir dağıtım build'i, farklı ekran oranları, çantada üç hakkın tamamını tüketerek tekrar açma ve operatör başarısızlık ekranından tekrar oynama bu testte çalıştırılmadı. Başarısızlık ekranının tekrar düğmesi aynı yeni koşu girişine yönlendirilmiştir, ancak bu dal için Play Mode testi yapıldığı iddia edilmez.

## Sınırlar

- Ev haritası tek bir sabit pixel-art zeminidir; odalarda yürüyen karakter veya ayrı ayrı taşınabilen mobilyalar yoktur. Etkinlik noktalarına tıklanır.
- Pil etkileşimi tıklamayla yapılır; ayrı bir pil sürükleme sistemi eklenmedi.
- Oyunlar öğrenme hızında tamamlanır; 10–25 saniyeyi zorunlu tutan bekleme süresi yoktur.
- Gönderilen deprem görselinin açık renkli özgün zemini korundu; bu nedenle karakterler açık renkli bir eğitim kartında gösterilir.
- Kayıt hissi REC ve köşe işaretleriyle sağlanır; ağır efekt veya bozulma yoktur.
- Günler arası yeni senaryo sistemi eklenmedi. Hazırlık bir koşunun başında oynanır, operatör çağrıları arasında tekrar tetiklenmez.
