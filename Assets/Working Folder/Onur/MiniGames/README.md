# Onur — 2D minigame pencereleri

Mevcut sahne: `Assets/Working Folder/Onur/Onur Test Place Integrated.unity`.
İki mevcut giriş düğmesi ve `MiniGameWindow` prefab örneği korunur. Başka sahneye giriş düğmesi veya otomatik görev eklenmedi. Scene örneğindeki root/component fileID'leri korunarak dört mevcut prefab düzenlendi; bağlantılar prefab üzerinden aktarılır.

## Görsel düzen

- `BagMiniGame.prefab`: büyük çanta ortada, altı PNG eşya iki yanında; beyaz kartlar ve eski grid yerleşimleri kaldırıldı/gizlendi. Çanta içinde her eşyanın ayrı `bagPosition` yuvası var. Dışarı bırakılan eşya kendi başlangıç yerine döner.
- `BagDraggableItem.prefab`: gerçek sprite kök Image'da; eski RawImage ve uzun eşya adı görünümü kapalı. Sprite hit test için üretilen kırpımlar readable olarak ayarlı.
- `SafetyMiniGame.prefab`: pano, gaz ve su görselleri üst sırada seçilebilir. Seçilen noktanın sorusu ve iki açık eylem seçeneği altta görünür. İşaret, cevaplanan noktayı gösterir; cevap doğruluğu ancak kontrolde açıklanır.
- `MiniGameWindow.prefab`: aynı karartma ve 501 sıralı modal Canvas; daha küçük kontrol/kapat düğmeleri ve iki panel için ortak pencere sınırları. Yeni EventSystem yok.

Yazılar: küçük başlık, tek satır yönerge, ♥ ♥ ♥, ✓ kontrol, X kapatma ve kısa sonuç.

## Kullanılan PNG'ler

Özgün dosyalar değiştirilmedi:

- `Assets/MiniGame/MiniGame1(Surukleme)/Adsız.png`
- `Assets/MiniGame/MiniGame2(ElektrikGüvenlik)/güvenlik.png`

Mevcut installer'ın sprite kırpma koordinatları ve kenara bağlı arka plan ayıklama yaklaşımı kullanılarak `Cutouts/` altında 14 sprite çıkarıldı:

`bag`, `water`, `flashlight`, `first_aid`, `radio`, `ball`, `controller`, `breaker`, `breaker_a`, `breaker_b`, `gas_valve`, `gas_valve_b`, `water_valve`, `water_valve_b`.

Yeni sanat veya dosya adına dayalı doğruluk sınıflandırması yok. Altı eşyanın `correctArea` referansları ve üç noktanın mevcut `correctOption` değerleri aynen korundu.

## Inspector

- Her oyunda `startingAttempts` (varsayılan 3), `rewardPoints` (10).
- `BagDraggableItem`: `startingArea`, `correctArea`, `startingPosition`, `bagPosition`, `icon`, `game`. `correctArea` boşsa eşya dışarıda kalmalıdır.
- `BagDropZone`: `contentRoot` ve `isBagArea`.
- `SafetyMiniGame.points`: `correctOption` (0/1), `hotspotButton`, `stateImage`, `stateA`, `stateB`, `completionTick`, `question`, `optionA`, `optionB`. Oyuncu bir noktayı seçer ve görünen eylemlerden birine basar; aynı noktaya dönüp cevabını değiştirebilir.
- Güvenlik metinleri deprem sonrası genel tedbir olarak yazıldı; hasarlı tesisata doğrudan müdahale önerilmez. Oyun içeriği için son uzman kontrolü önerilir.

## Korunan kurallar

Hak yalnızca başarısız ✓ kontrolünde azalır. Sürükleme ve durum değişimi hak harcamaz. Başarı sonucu kilitlenmeden önce değil, kilitlendikten sonra mevcut `CityHealth.AddHealth` üzerinden ödül uygulanır; tekrar ✓ veya kapat/aç ödülü tekrarlamaz. Başarısızlık ana oyunu bitirmez. Kapat/aç hak, seçim ve sonucu korur. Şehir sağlığı hesabı ve %100 sınırı değiştirilmedi.

Pencere, mevcut telefon/konuşma ve hızlı soru `SetPaused` alanlarını kullanır ve önceki değerlerini geri yükler. Pencere kapanırken devam eden eşya sürüklemesi önce son geçerli yerine döner. `Time.timeScale` değişmez. Aynı pencere açıkken diğer oyun açılamaz; modal Image arka UI raycast'lerini engeller.

`Tools > Onur > Install Minigames` artık kurulmuş prefab varsa veriyi ve tasarımı yeniden üretmek yerine mevcut pencereyi sahneye bağlar; düzenlenen cevapları üzerine yazmaz.

## Son onarım ve yapılan Play Mode kontrolleri

Türkçe başlık ve yönergelerdeki bozuk UTF-8/Windows kodlaması düzeltildi. Canlar iki oyunda kırmızı. Her başarısız kontrolde yalnızca minigame penceresi 0,2 saniye, en fazla 2 ekran pikseli yatay titrer; kamera ve ana sahne hareket ettirilmez. Efekt kapanışta/bitimde konumunu geri yükler.

Güvenlik noktaları gizli eski atlas görselinin altından görünür pencere köküne taşındı. Fontta bulunmayan ✓ için `MiniGameCheckGraphic` kullanılıyor; kontrol düğmeleri ve tamamlanan noktalardaki tikler fonttan bağımsız çiziliyor. Doğruluk yalnızca kontrolde değerlendirilir.

Çantada rahat tutulabilen görünmez hit alanı, sürükleme sırasında büyüme ve çanta hedef vurgusu eklendi. Sürükle-bırak korunur; tek tıklama da eşyayı çantaya/dışarı taşır. Sürüklenen bir basışın sonundaki click olayı engellenir; eşya bırakılınca yanlışlıkla tekrar dışarı taşınmaz. Altı ayrı yuva çanta PNG'sinin açık iç bölümüne göre hizalandı. Eşya doğruluk verisi değiştirilmedi.

Unity MCP üzerinden mevcut Onur sahnesinde Play Mode çalıştırıldı:

- `OnurMiniGameChecks`: 36 doğrulama geçti. Çanta içine/dışına taşıma, sürükleme sonunda click gelmesi, hakların yalnızca yanlış kontrolde azalması, tek pencere, arka plan duraklaması, görünür üç güvenlik noktası, iki oyunda başarı/başarısızlık, +10 puanın tek sefer verilmesi, %100 sınırı ve kapat/aç koruması kontrol edildi.
- Ayrıca Input System'e fare basma/taşıma/bırakma durumları verilerek gerçek UI giriş hattında 14 kontrol geçti. Bu test doğrudan OnBeginDrag çağırmakla sınırlı değildi: gerçek kontrol, kapatma ve giriş düğmeleri kullanıldı; çanta tamamlandı, gerçek sağlık değeri 70→80 oldu, tekrar ödül verilmedi. Bunlar geçici test değerleridir; sağlık hesabı veya kayıtlı başlangıç değeri değiştirilmedi.
- Yanlış kontrolde tek hak kaybı, titremenin gerçekten hareket etmesi, iki pikseli aşmaması ve tam yerine dönmesi ölçüldü.
- 1600×900 gerçek Game görüntülerinde Türkçe başlıklar/yönergeler, kırmızı canlar, fonttan bağımsız tikler, görünür güvenlik noktaları ve çantanın tamamlanmış hali incelendi.
- Son Unity hata sorgusu: 0 kayıt.

Kayıtlar: `Logs/MiniGameRepair/regression-result.json`, `Logs/MiniGameRepair/ui-pointer-result.txt`, `Logs/MiniGameRepair/bag-fixed.png`, `Logs/MiniGameRepair/safety-fixed.png`.

Fare testleri Input System üzerinden otomatik giriş kullandı; fiziksel masaüstü faresiyle elle oynanmış gibi raporlanmıyor. Hedef 30–45 saniyelik süre kullanıcıyla zaman tutularak ölçülmedi.

`Tools > Onur > Test Minigames (Play Mode)` regresyonu tekrar çalıştırabilir. Bu test Play Mode minigame ilerlemesini sıfırlar ve geçici sağlık değerleri kullanır; kaydedilmiş sahne/asset verisine yazmaz.
