# Onur minigame prototipi

Oyun sahnesi: `Assets/Working Folder/Onur/Onur Test Place Integrated.unity`. Ana menüye veya diğer ekiplerin sahnelerine minigame eklenmedi. `DispatchRoomCanvas` altındaki `MiniGameWindow` prefab örneği iki giriş düğmesini ve tek bir karartmalı pencereyi içerir. Sahnedeki `EventSystem` kullanılır; yeni bir EventSystem ve süre sınırı yoktur.

## Prefablar ve görseller

- `Prefabs/MiniGameWindow.prefab`: giriş düğmeleri, raycast engelleyen üst Canvas, panel seçimi ve şehir sağlığı bağlantısı.
- `Prefabs/BagMiniGame.prefab`: çanta, başlangıç alanı, eşya kartları, sonuç ve kontrol düğmeleri.
- `Prefabs/BagDraggableItem.prefab`: yeniden kullanılabilir eşya kartı.
- `Prefabs/SafetyMiniGame.prefab`: görsel üzerindeki üç **TEST** noktası, iki karar seçeneği, sonuç ve kontrol düğmeleri.
- Çanta görseli ve eşya kırpımları: `Assets/MiniGame/MiniGame1(Surukleme)/Adsız.png`. Güvenlik görseli: `Assets/MiniGame/MiniGame2(ElektrikGüvenlik)/güvenlik.png`. PNG dosyaları ve importer ayarları değiştirilmedi.

Görseller gerçek doğru cevapları doğrulamıyor. `TEST:` adları, çantaya konacak eşya örnekleri ve güvenlik seçenekleri yalnızca Inspector'da değiştirilecek prototip verisidir; gerçek güvenlik talimatı sayılmamalıdır. `Tools > Onur > Install Minigames` kurulumu yeniden çalıştırılırsa örnek prefablar **yeniden üretilir ve düzenlenen test verileri üzerine yazılır**.

## Inspector alanları

| Bileşen | Alan | Görev |
| --- | --- | --- |
| `MiniGameWindow` | `bagEntryButton`, `safetyEntryButton` | Prototip giriş düğmeleri. |
| `MiniGameWindow` | `overlay`, `bagGame`, `safetyGame` | Karartma ve iki ayrı panel. |
| `MiniGameWindow` | `cityHealth`, `phone`, `conversationView` | Mevcut sağlık ödülü ve panel açıkken duraklatılacak akışlar. Sahne örneğinde bağlıdır. |
| `BagMiniGame`, `SafetyMiniGame` | `startingAttempts`, `rewardPoints` | Her oyun için ayrı hak ve yüzde puanı ödülü; varsayılan 3 ve 10. |
| `BagMiniGame` | `window`, `dragLayer`, `items` | Ortak pencere, sürükleme katmanı ve kontrol edilen eşyalar. |
| `BagMiniGame` | `attemptsLabel`, `resultLabel`, `checkButton`, `closeButton` | Durum yazıları ve düğmeler. |
| `BagDraggableItem` | `itemName`, `sourceSprite`, `cropPixels`, `icon`, `nameLabel` | Eşya adı ve mevcut sprite sayfasından kart görseli. `cropPixels` piksel kırpımıdır. |
| `BagDraggableItem` | `startingArea`, `correctArea`, `game` | Başlangıç yeri, doğru bırakma alanı ve oyun bağlantısı. `correctArea` boşsa eşya çanta dışında kalmalıdır. |
| `BagDropZone` | `isBagArea`, `contentRoot` | Çanta/dış alan ayrımı ve eşyaların yerleştiği UI kökü. |
| `SafetyMiniGame` | `points` | Her noktanın adı, açıklaması, A/B metinleri, doğru seçeneği (0=A, 1=B), görsel butonu ve seçim işareti. |
| `SafetyMiniGame` | `pointNameLabel`, `descriptionLabel`, `optionALabel`, `optionBLabel`, `attemptsLabel`, `resultLabel`, `optionAButton`, `optionBButton`, `checkButton`, `closeButton`, `window` | Seçilen nokta bilgisi, hak/sonuç yazıları, düğmeler ve ortak pencere bağlantısı. |

Çalışma anındaki alanlar Inspector verisi değildir:

| Bileşen | Alan | Görev |
| --- | --- | --- |
| `MiniGameWindow` | `wasPhonePaused`, `wasConversationPaused`, `isOpen` | Açılış öncesi duraklama durumlarını ve tek pencere kuralını korur. |
| `BagMiniGame` | `attemptsLeft`, `resolved`, `lastCheckTime` | Kalan hak, kesinleşmiş sonuç ve hızlı çift kontrol engeli. |
| `BagDraggableItem` | `rect`, `group`, `currentArea`, `previousArea`, `dragLayer`, `pointerOffset`, `previousSibling`, `dragging`, `dropped` | Kartın UI dönüşümü, raycast durumu, mevcut/önceki alanı ve sıra/konumunu sürükleme boyunca tutar. |
| `SafetyMiniGame` | `ControlPoint.selectedOption`, `attemptsLeft`, `selectedPoint`, `resolved`, `lastCheckTime` | Her noktanın seçimi, kalan hak, açık nokta, sonuç ve çift kontrol engeli. |
| `DispatchCallFlow`, `OnurConversationView` | `IsPaused` | Pencere açıkken sadece ilgili çağrı/konuşma akışını durdurur. |

## Davranış

- Çantada kartlar `EventSystem` sürükleme olaylarıyla alanlar arasında taşınır. Geçersiz bırakma önceki yere döner; yerleştirme hak harcamaz. `Kontrol Et`, bütün gerekli eşyaların doğru alanlarda ve gereksizlerin dışarıda olup olmadığını karşılaştırır.
- Güvenlik oyununda nokta seçimi ve A/B kararı ayrı saklanır; kontrol öncesi değiştirilebilir. `Kontrol Et`, bütün noktaların cevaplanmasını ve `correctOption` eşleşmesini ister. Boş liste başarı değildir.
- Başarısız kontrol bir hak eksiltir; son hakta yalnız o panel kilitlenir. Başarıda mevcut `CityHealth.AddHealth` en fazla %100'e kadar tek sefer uygulanır. Kapat/aç hakları ve sonucu korur. `ResetForNewSession()` dışarıdan çağrılmadıkça oturum sıfırlanmaz.
- Pencere açılırken telefon ve konuşmanın önceki duraklama değerleri saklanır, kapanınca aynen geri yüklenir. `Time.timeScale` değiştirilmez.

Unity Play Mode'da iki giriş, panel kapatma, eksik kontrolün tek hak azaltması, kapat/aç koruması, güvenlik seçimi ve başarılı güvenlik sonucu gözlendi. Otomasyon aracının fare sürüklemesi Unity `OnBeginDrag` olayını tetiklemediğinden çanta sürüklemesi gerçek fareyle ayrıca sınanmalıdır.
