# Atmosfer ve konuşma sunumu — uygulama raporu

17 Ekim 2026. Çalışılan proje: `C:\Users\MSI\Documents\gamejam\GameJamProject-912`.
Mevcut SampleScene ve mevcut oyun sistemleri kullanıldı. Oda/başkan görselleri, senaryo metinleri, seçenekler, maliyetler, tahsis verileri ve menü tasarımı değiştirilmedi.

## 1–3. Değişen dosyalar ve scriptler

| Dosya | Değişiklik |
| --- | --- |
| `Assets/Scenes/SampleScene.unity` | İki diyalog görünümünde 38 karakter/sn; yeni sunum parametreleri kaydedildi. Unity mevcut bileşenlerin varsayılanlarını da serileştirdi. |
| `Assets/Scripts/Presidential/BroadcastMouthGraphic.cs` | Eksik CanvasRenderer gereksinimi; özgün yüzün dudaklarına hizalanan kademeli piksel ağız şekilleri. |
| `Assets/Scripts/Presidential/PresidentialBroadcastView.cs` | CanvasRenderer oluşturma, 3 ağız durumu, mevcut yazım durumuyla eşleşme ve isteğe bağlı ses desteği. |
| `Assets/Working Folder/Onur/Scripts/OnurConversationView.cs` | Mevcut baloncuklarda yazım, tek satırı açma girişi, kısa çağrı sonu bildirimi. Sağ/sol kayma ve otomatik ilerleme korundu. |
| `Assets/Working Folder/Onur/Scripts/OyunAkisiOnur.cs` | Başarılı gerçek sevkte sunum olayı; mevcut bağlantı kurtarmasından önce çağrı sonu bildirimi. Kaynak hesabı değiştirilmedi. |
| `Assets/Scripts/Presidential/DispatchResourceHud.cs` | Birim adına göre kısa sevk bildirimi ve sayıdan bağımsız ikon tepkisi. |
| `Assets/Scripts/Presidential/DispatchVisualFeedback.cs` | Mevcut efekt bileşenine çok hafif monitör parlaklığı eklendi. |
| **Yeni:** `Assets/Scripts/Presidential/DialogueTypingAudio.cs` | İki mevcut görünümün kullandığı küçük, serileştirilebilir, isteğe bağlı yazım sesi ayarları/yardımcısı. |
| **Yeni:** `Assets/Editor/AtmospherePolishChecks.cs` | Açıkça çalıştırılan Play Mode doğrulamaları. Oyuna kurulmaz; senaryo verilerini değiştirmez. |

Yeni iki script için Unity `.meta` dosyaları da oluştu. Yeni efekt yöneticisi, Animator, shader veya EventSystem eklenmedi. Testte değişen dinamik font atlasının içeriği geri alındı; font dosyasında içerik farkı yok.

## 4. GameObject ve bileşenler

Mevcut çalışma zamanı UI oluşturma mantığı genişletildi:

- `Başkan Konuşma`: eksik CanvasRenderer eklendi; mevcut BroadcastMouthGraphic kullanılıyor.
- `Sevk Bildirimi`: kaynak paneli içinde küçük TMP metni, raycast kapalı.
- Mevcut üç birim ikonunun kendi RectTransform'u kısa süre ölçekleniyor; sayı transformuyla yarışmıyor.
- `Çağrı Durumu`: sohbet alanının altında kısa TMP durum metni, raycast kapalı.
- `MonitorLuminance`: konuşma viewport'u içinde, sohbetin arkasında düşük alfa Image; raycast kapalı.
- AudioSource yalnızca isteğe bağlı bir clip atanır ve kullanılırsa oluşturulur. Varsayılan sahnede oluşturulmuyor.

## 5–7. Başkan teşhisi, düzeltme ve eşleşme

Asıl hata ağız nesnesinin CanvasRenderer bileşeni olmamasıydı. Özel Graphic oluşturulmuş ve konuşma durumu ilerliyor olsa da çizim ekrana ulaşmıyordu. Gerçek Play Mode nesnesinde render derinliğine erişmek MissingComponentException ile bunu doğruladı. Yalnızca hız/ölçek değerleri değiştirilerek çözülmedi.

CanvasRenderer hem oluşturma noktasında hem RequireComponent ile güvenceye alındı. Kapalı durumda ek geometri çizilmiyor; onaylı görselin özgün ağzı görülüyor. Yarı açık ve açık durumlarda düz bir yüz yaması yerine dudak çevresine oturan kademeli piksel şekilleri çiziliyor. Portre preserveAspect oranı ve boşlukları hesaplamaya dahil.

Mevcut Type coroutine'i gerçek yazım süresince IsSpeaking durumunu yönetiyor. Kapalı → yarı açık → açık → yarı açık → kapalı döngüsü kullanılıyor; geometri yalnızca ağız durumu değiştiğinde yenileniyor. Etkin sahne hızı 8 durum adımı/sn. Satır bitimi, anında açma, teklif ekranı, Hide/OnDisable ve vardiya sonu ağzı kapatıyor. Gövde/kamera sallama veya abartılı hareket eklenmedi.

1600×900 gerçek Game görüntüsünde açık, yarı açık ve idle durumlar görsel olarak incelendi. Ağız yüz üzerinde, portreden önde ve görünür; maske kesilmesi, yüzen ağız veya dikdörtgen yüz bindirmesi görülmedi. İlk yayın, gerçek akışta ikinci vaka sonunda da görüntülendi.

## 8–9. Yazım ve atlama

TMP maxVisibleCharacters kullanılıyor; mevcut konuşma mimarisi ve metinler korunuyor. Sahnedeki hız 38 karakter/sn. Baloncuğun sağdan/soldan kayma animasyonu korunuyor; tamamı bir an görünüp tekrar gizlenen metin parlaması önlendi.

Kazazede görüşmesinde baloncuğa/sohbet alanına sol tıklama, Space veya Enter yalnızca yazılan satırı tamamen açar. Aynı giriş sonraki satırı atlatmaz. Tam satır gösteriminden sonra mevcut 0,8–1,8 sn otomatik okuma aralığı sürer. Seçenekler geldiğinde akış bekler ve seçim otomatik yapılmaz. Kazazede sohbetine DEVAM düğmesi eklenmedi.

Başkan yayını mevcut DEVAM etkileşimini korur: yazım sürerken ilk giriş o satırı açıp ağzı durdurur; sonraki ayrı giriş normal ilerlemeyi yapar. Sonraki satırda konuşma tekrar başlar.

## 10. Sevk geri bildirimi

UnitDispatched olayı yalnızca başarılı seçim maliyeti uygulandıktan sonra ve gerçekten bir birim sevki varsa yayınlanır. Başkan tahsisindeki azalışlar veya sevksiz tercihler yanlış sevk bildirimi oluşturmaz.

HUD bu olayı dinler: AMBULANS / İTFAİYE / KURTARMA EKİBİ SEVK EDİLDİ metni 1,15 sn içinde belirip kaybolur. İlgili ikon 0,30 sn içinde 100% → en fazla 108% → 100% olur. Mevcut sayı tepkisi, düşük ekip ve sıfır ekip uyarıları korunur. Bildirim alanı düşük kaynak uyarısının altında kalır; raycast almaz.

## 11–13. Monitör, çağrı sonu ve isteğe bağlı çevre

Mevcut DispatchVisualFeedback içinde monitör alanına yavaş bir parlaklık değişimi eklendi: azami alfa 0,012, yaklaşık 7,5 sn döngü; 0,1 sn aralıkla örneklenir. Kalın tarama çizgisi, glitch, shader veya yazının üzerine güçlü katman yok. Önceki doğru/yanlış ve monitör aktivasyon efektleri korunur.

Çağrı tamamlandığında seçenekler gizlenir, ÇAĞRI SONLANDIRILDI metni 1,05 sn içinde belirip söner. Ardından mevcut Space ile bağlantı kurtarma aşaması ve sonraki oyun durumu aynen devam eder.

Ek arka plan karakter hareketi veya oda ışığı animasyonu eklenmedi. Onaylı arka plan tek bir UI görseli olarak korunuyor; Canvas üzerindeki görsel için sahne ışıklarını oynatmak yararlı bir çözüm olmazdı. İsteğe bağlı bu iki öğe uğruna arka plan resmi değiştirilmedi.

## 14. Inspector ayarları

| Bileşen | Ayar |
| --- | --- |
| OnurConversationView / PresidentialBroadcastView | Characters Per Second: sahnede 38. |
| İki diyalog görünümü / Typing Audio | Clip: boş; Character Interval: 3 (2–4); Volume: 0,045 (0–0,2). En kısa ses aralığı ayrıca 65 ms ile sınırlı. |
| PresidentialBroadcastView | Speaking Frames Per Second: sahnede 8, aralık 5–8. |
| OnurConversationView | Call Ended Duration: 1,05 sn (0,8–1,5). |
| DispatchResourceHud | Dispatch Notice Duration: 1,15 sn (0,8–1,4); Icon Pulse Scale: 1,08. Mevcut sayı/uyarı ayarları korunur. |
| DispatchVisualFeedback | Monitor Luminance: 0,012 (0–0,025); Monitor Cycle: 7,5 sn (4–12). Önceki karar efektleri ayarları korunur. |

## 15. Gerçekte yapılan testler

Başkan testi önce çalıştırıldı: renderer varlığı, başlangıç idle, doğal ilerleyen yazım, açık/yarı açık Game görüntüsü, portre önünde çizim, satır sonunda idle, ilk atlama girişinde yalnızca satırın açılması ve ağzın kapanması, sonraki satırda yeniden konuşma, Hide ile idle. Sonuç: 8 doğrulama geçti.

Sonra gerçek menü düğmesi ve gerçek sahne verileri kullanıldı:

AnaMenu → OYNA → fade → SampleScene → telefon hover/zil işaretleri → telefonu yanıtlama → workstation focus → yazılan baloncuk → ilk tıklamayla satırı açma → iki seçenek → doğru/yanlış efekt → kaynak/ikon/sevk bildirimi → çağrı sonu → mevcut Space kurtarma → sonraki vaka.

D-01 → D-02 → Y-02 → S-01 → T-01 → F-01 → K-01 → K-02 sırası, 2/4/6/8 sonrası dört yayın ve vardiya sonu tamamlandı. İlk doğal yazım ve başkan satırı ayrıca gözlendi; uzun regresyonda diğer satırlar mevcut anında açma desteğiyle hızlandırıldı. Senaryolar test fixtürüyle değiştirilmedi.

Üç birimin her biri kendi ilk sevkinde 5→4 olarak doğrulandı. Bildirim görünmesi/sönmesi, sayı ve ayrı ikon tepkisi kontrol edildi. Sonrasında mevcut kaynak API'siyle 3→2→1→0 sınırları ve sıfırın altına inmeme kontrol edildi. Bunlar Play Mode içindeki geçici test değerleriydi; kaydedilen başlangıç değerleri 5/5/5.

Telefon ve tüm karar/tahsis düğmelerine raycast ulaşması doğrulandı. Yeni dekoratif katmanların raycast almadığı kontrol edildi. İnaktif nesneler dahil tam bir EventSystem var. Boş ses ayarı iki diyalog görünümünde AudioSource oluşturmuyor.

Tam akışın sonundaki test kodu önce DispatchVisualFeedback'in üst nesnesi olduğunu varsaydı; bu bileşen kök nesnede olduğu için yalnızca son monitör aramasında test hatası oluştu. Global nesne araması düzeltildi. Tamamlanmış aynı Play Mode vardiyasında son monitör, EventSystem ve boş ses kontrolleri MCP ile tekrar çalıştırılarak geçti. Bu yalnızca Editor test araması düzeltmesinden sonra sekiz vaka tekrar oynatılmış gibi raporlanmıyor.

Kayıtlar:

- `Logs/AtmosphereChecks/president-result.txt`: 8 PASS.
- `Logs/AtmosphereChecks/gameplay-result.txt`: 135 PASS; tam sekiz vaka ve son kontroller dahil.
- `Logs/AtmosphereChecks/president-speaking-open.png`, `president-speaking-half.png`, `president-line-complete.png`, `president-in-shift.png`: gerçek Game görüntüleri.
- `Logs/AtmosphereChecks/dialogue-typewriter.png`, `dispatch-unit-0.png`, `dispatch-unit-1.png`, `dispatch-unit-2.png`, `call-ended.png`, `phone-ring-hover.png`, `resource-zero.png`: görsel kontrol kayıtları.

Son C# derleme hatası sorgusu: 0. Senaryo/başkan veri dosyalarının önceden kaydedilmiş 17 içerik hash'inde değişiklik: 0. `git diff --check`: temiz.

## 16. Kalan durum

İsteğe bağlı ses clip'i atanmadı; projedeki ses klasöründe tamamlanmamış .crdownload dosyaları vardı. Haricî ses indirilmedi. Clip olmadan davranış test edildi; atanmış gerçek bir sesin işitsel değerlendirmesi yapılmadı.

Bu geçişte doğrulanan oyun akışında açık bir hata kalmadı. Testler 1600×900 Game görüntüsünde yapıldı; tüm çözünürlükler bu geçişte yeniden sınanmadı. Ek arka plan/oda ışığı animasyonu kasıtlı olarak eklenmedi.

Başkan konuşması görünür ve hizalı; idle'a dönüyor. Yazım ve tek satır açma çalışıyor. Telefon, doğru/yanlış efektleri, 5/5/5 başlangıç kaynakları, birim ikonları, düşük/sıfır uyarıları, sevk ve çağrı sonu bildirimi, tıklanabilir diyalog düğmeleri, menüden oyuna geçiş ve tek EventSystem kontrolleri geçti.
