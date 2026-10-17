# Başkan bilgilendirme sistemi — entegrasyon raporu

17 Ekim 2026. Çalışılan gerçek proje: `C:/Users/MSI/Documents/gamejam/GameJamProject-912`.

## Sonuç ve oynanış

Sistem `Assets/Scenes/SampleScene.unity` ve `Assets/Working Folder/Onur/Onur Test Place Integrated.unity` sahnelerine bağlandı. Ana menüden OYNA ile SampleScene açılıyor; telefon cevaplanınca mevcut yakınlaşma ve vatandaş konuşması çalışıyor.

Kullanıcının onayladığı ana sıra:

**D-01 → D-02 → Başkan 1 → Y-02 → S-01 → Başkan 2 → T-01 → F-01 → Başkan 3 → K-01 → K-02 → Başkan 4 → Final**

Sıra, sahnedeki **Başkan Bilgilendirme / PresidentialBriefingManager / Scenario Order** alanında değiştirilebilir. İlk incelemede projede yalnızca Onur'un kod içindeki D-01 konuşması bulunuyordu. Çalışma sırasında proje köküne eklenen `afet_senaryolari_kaynak_12.md` dosyası, diğer mevcut vatandaş metinlerinin kaynağı oldu. Sekizli sıra kullanıcı tarafından onaylandı; diğer kayıtlar silinmedi.

## Mevcut kaynak sisteminin analizi

- Yönetici: `Assets/Working Folder/Onur/Scripts/DispatchResources.cs`.
- Gerçek sayaçlar: `Ambulances`, `Firefighters`, `Police`. İlk değerler aynı bileşenin `initialAmbulances`, `initialFirefighters`, `initialPolice` alanlarından geliyor.
- Kullanıcının isteğiyle üçüncü kaynak ekranda **Kurtarma** olarak adlandırıldı. `Police` adı, mevcut serileştirilmiş referansların ve eski kodun uyumluluğu için korundu; `RescueTeams` aynı değeri okur. Bağımsız kurtarma sayacı eklenmedi.
- Eski `Spend` tek kaynaktan bir ekip eksiltiyor ve sıfırda duruyordu. Artırma/tahsis API'si yoktu. `CanApply`, `TryApply`, `TrySpend`, `Changed` ve `ResetForNewShift` eklendi.
- `TryApply`, üç değişikliği ve taşma sınırlarını önce birlikte doğrular. Geçersiz işlem hiçbir sayacı değiştirmez; geçerli işlem bütün sayaçları değiştirip mevcut `Refresh()` üzerinden üç TMP göstergesini günceller ve bir `Changed` olayı üretir.
- Vardiya boyunca sahne değiştirilmediği için aynı `DispatchResources` bileşeni korunur. Yeni vardiya/sahne yüklemesi başlangıç değerlerini kullanır; uygulama kapatıldığında kayıt tutma eklenmedi.
- Konuşma yöneticisi: `Assets/Working Folder/Onur/Scripts/OyunAkisiOnur.cs`. Başlangıçta başarılı konuşma bitiş olayı veya sonraki vaka başlatma sistemi yoktu; yanlış seçim konuşmayı yeniden başlatıyordu.
- Yönetilen vardiyada sonuç konuşmasının tamamlanması `ScenarioCompleted(revision)` olayını üretir. Yanlış seçim ve süre aşımı başarısız vaka olarak ilerler; doğru seçim doğru yönlendirme olarak sayılır. Aynı vaka kimliği tekrar sayılmaz. Vardiya dışındaki eski yanlış seçim tekrar davranışı korunur.

## Kullanılan başkan görseli

`Assets/Art/UI/Maın_desk_scene/Gece yayınında sakin ve hazırlıklı başkan.png`

Yeni görsel üretilmedi. Orijinal piksel oranıyla UI Image içinde gösteriliyor. İçe aktarma Sprite/Single, Point, mipmap kapalı ve sıkıştırmasız olarak düzenlendi. Retro terminal çerçevesi, mevcut piksel fontu, Türkçe karakterler için font fallback'i, sade fade ve tahsis onay geçişi kullanıldı. Yayın mevcut Canvas'ın üzerinde bir resmî yayın penceresi; mevcut oda ve telefon düzeni korunuyor. Yeni EventSystem eklenmedi.

## Başkan konuşmaları ve tahsisler

Miktar sırası: **Ambulans / İtfaiye / Kurtarma**. Her plan mevcut kaynakları, değişikliği ve seçilirse oluşacak sayıları gösterir. Negatif kaynak gerektiren plan pasiftir ve eksik ekip türü belirtilir. İki plan da uygulanamıyorsa yalnızca iki geçerli acil merkez yedeği planı gösterilir: sağlık yedeği veya saha yedeği. Bu alternatifler birbirini dışlar; varsa öncelik verilmeyen ekiplerin fazlası geri aktarılır. Sıfır kaynakta tercih, sınırlı merkez yedeğinin hangi hizmete ayrılacağıdır.

### Başkan 1 — Hazırlık evde başlar

“İki çağrıyı geride bıraktık. Telefonun öbür ucundaki korkuyu siz duydunuz. O korkuyu azaltmanın bir yolu da afet gelmeden hazırlanmaktır.”

“Evde bir afet çantası hazır bulunsun. Aileler nerede buluşacağını önceden konuşsun, toplanma alanını öğrensin. Sabitlenmiş bir dolap, o gün bir çıkış yolunu açık tutabilir.”

“Ekrandaki mevcut ekiplerimiz bütün taleplere aynı anda yetişemez. Yeni destek için sahadaki dağılımı değiştirmemiz gerekiyor. Sağlık kapasitesini mi, kurtarma gücünü mü artıracağız?”

| Plan | Ambulans | İtfaiye | Kurtarma |
| --- | ---: | ---: | ---: |
| Sağlık Takviyesi | +3 | 0 | -1 |
| Saha Desteği | -1 | +1 | +2 |

Kapanış: “Planı onaylıyorum. Ekipler yeni dağılıma göre göreve hazır. Bir sonraki ihbarda hem doğru yönlendirmeye hem bu kapasiteye ihtiyacımız olacak.”

### Başkan 2 — Sarsıntıdan sonra ilk müdahale

“Sarsıntı sırasında birkaç saniye uzun gelir. O birkaç saniyede verilen doğru yönlendirme, bir ailenin sonraki çağrıyı yapabilmesini sağlar.”

“İçerideyseniz güvenli bir noktada Çök-Kapan-Tutun yapın; başınızı ve boynunuzu koruyun. Sarsıntı bitmeden merdivene yönelmeyin, asansör kullanmayın.”

“Yaralıya ulaşmakla enkazdaki insana ulaşmak aynı hazırlığı istemiyor. Mevcut ekiplere bakarak karar verin: kurtarma hattını mı büyüteceğiz, sağlık ve yangın müdahalesini mi güçlendireceğiz?”

| Plan | Ambulans | İtfaiye | Kurtarma |
| --- | ---: | ---: | ---: |
| Enkaza Erişim | -2 | +1 | +4 |
| İlk Müdahale Ağı | +3 | +2 | -2 |

Kapanış: “Tahsis yürürlükte. Her ekibi doğru ihtiyaçla buluşturalım. Yeni çağrıda kararımızın sahadaki karşılığını göreceğiz.”

### Başkan 3 — Doğru bilgi, doğru sevk

“Her ihbar bir ekibin yolunu değiştiriyor. Yanlış bir adres ya da doğrulanmamış bir haber, başka bir evin kapısına ulaşmamızı geciktirebilir.”

“Acil hatları gerçek acil durumlar için açık tutalım. İhbarda konumu ve gördüğümüz durumu açıkça aktaralım. Doğrulanmamış haberleri paylaşmayalım; resmî açıklamaları izleyelim.”

“Altı vakanın ardından kalan gücümüz ekranda. Yangınlara ayrılan kapasiteyi artırmak kurtarma yedeğini azaltacak. Saha erişimine ağırlık vermek ise ambulans desteğinden fedakârlık isteyecek.”

| Plan | Ambulans | İtfaiye | Kurtarma |
| --- | ---: | ---: | ---: |
| Yangın Çemberi | +1 | +4 | -3 |
| Güvenli Erişim | -2 | +2 | +4 |

Kapanış: “Dağılım güncellendi. Kaynağımızı korumak, yardım isteyen bir sonraki kişiye zaman kazandırmaktır. Görevinize devam edin.”

### Başkan 4 — Bir sonraki vardiyaya

“Bu vardiyada {tamamlanan} vaka tamamladınız, {karar} tahsis kararı verdiniz. Her tercih, sınırlı bir ekibin hangi kapıya önce ulaşacağını belirledi. Şimdi bu sorumluluğu gelecek vardiyaya devrediyoruz.”

“Dayanışma, tehlikeye hazırlıksız koşmak değildir. Komşumuzun ihtiyacını bilmek, aile planını birlikte yapmak ve doğrulanmış bilgiyi paylaşmak da yardımın bir parçasıdır.”

“Bu gece yeni çağrı açılmayacak. Yine de son kararınızın karşılığı var: kalan ekipler ve bu tahsis, sonraki vardiyanın hazırlık durumuna yazılacak. Sağlık yedeğini mi, saha dayanıklılığını mı güçlendirelim?”

| Plan | Ambulans | İtfaiye | Kurtarma |
| --- | ---: | ---: | ---: |
| Sağlık Nöbeti | +4 | -2 | +1 |
| Mahalle Dayanıklılığı | -1 | +3 | +3 |

Kapanış: “Devir planını onaylıyorum. Son dağılım rapora işlendi. Hazırlığımızı yarına taşıyacağız; bu sorumluluk tek bir vardiyayla bitmiyor.”

Yer tutucular gerçek tamamlanan vaka ve önceki tahsis sayısıyla değiştirilir. Final, 8 vakadaki doğru yönlendirme sayısını, dört kararın sonuncusunu, kalan gerçek kaynakları ve en düşük ekip kapasitesine göre dengeli/sınırlı/kritik gelecek vardiya hazırlığını gösterir. Dokuzuncu vaka başlatılmaz.

Başkan için yazılan hazırlık ve deprem bilgileri [AFAD'ın hazırlık ve deprem davranışı açıklamaları](https://afad.gov.tr/deprem-oncesi-ani-ve-sonrasi-onlemini-al-guvende-kal) ile kontrol edildi. Mevcut vatandaş metinlerinin tıbbi/afet güvenliği açısından tamamının yeniden doğrulanması veya yeniden yazılması bu entegrasyona dahil edilmedi.

## Oluşturulan dosyalar

- `Assets/Scripts/Presidential/PresidentialBriefingData.cs`: konuşma ve plan verisi.
- `Assets/Scripts/Presidential/DispatchScenarioData.cs`: mevcut vatandaş metinleri için veri modeli.
- `Assets/Scripts/Presidential/PresidentialBriefingManager.cs`: vaka kimlikleri, yayınlar, tahsis kilidi, final.
- `Assets/Scripts/Presidential/PresidentialBroadcastView.cs`: retro yayın, iki plan, metin efekti, sonuç UI.
- `Assets/Editor/PresidentialBriefingInstaller.cs`: var olan sahneye referansları bağlama ve ilk veri varlıklarını oluşturma.
- `Assets/Editor/PresidentialScenarioImporter.cs`: mevcut vatandaş verilerinden varlıkları oluşturma ve onaylı sırayı bağlama; tekrar çalıştırmada elle düzenlenmiş senaryo varlıklarının içeriğini ezmez.
- `Assets/Editor/PresidentialBriefingChecks.cs`: açıkça çalıştırılan Play Mode doğrulamaları.
- `Assets/PresidentialData/Briefing1.asset` … `Briefing4.asset`.
- `Assets/PresidentialData/CitizenSource.json`.
- `Assets/PresidentialData/Scenarios/`: D-01, D-02, Y-01, Y-02, S-01, S-02, T-01, T-02, F-01, F-02, K-01, K-02 varlıkları.
- Unity'nin oluşturduğu karşılık gelen `.meta` ve klasör `.meta` dosyaları.
- Bu rapor. Yerel kontrol çıktıları `Logs/PresidentialChecks/` içindedir; Logs sürüm kontrolüne dahil değildir.

Önceki bağlantı kurulumundan gelen `Assets/Editor/CodexUnityConnection.cs` ve paket listelerindeki MCP eklemesi de çalışma ağacındadır.

## Değiştirilen mevcut dosyalar

- `Assets/Working Folder/Onur/Scripts/DispatchResources.cs`.
- `Assets/Working Folder/Onur/Scripts/OyunAkisiOnur.cs`.
- `Assets/Working Folder/Taha and Yasin/DispatchCallFlow.cs`: yayın boyunca etkileşim kilidi.
- `Assets/Scenes/SampleScene.unity`: mevcut Onur konuşma paneli ve kaynak sistemiyle entegrasyon. Önceki panel silinmedi; devre dışı bırakıldı.
- `Assets/Working Folder/Onur/Onur Test Place Integrated.unity`: yayın referansları ve onaylı sıra.
- Başkan PNG'sinin `.meta` import ayarları.
- `Assets/Art/UI/MainMenu/DispatchMenuPixel SDF.asset`: font fallback ve yeni Türkçe glifler.
- `Assets/Art/UI/Fonts/VCR_OSD_MONO_1.001 SDF.asset` ve `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`: mevcut konuşmalar çalıştırılırken Unity/TMP'nin oluşturduğu glif/atlas güncellemeleri.

AnaMenü kodu, menü sahnesi, fade kodu, mevcut prefablar ve kaynak Markdown metni değiştirilmedi. EditorBuildSettings için Git içerik farkı yoktur; Unity serileştirmesi satır sonu durumunu değiştirebilir.

## Yapılan doğrulamalar

Unity 6000.6.5f1, Play Mode:

- Tam sekizli akış hem geçici izole test senaryolarıyla hem onaylanmış gerçek vatandaş içerikleriyle çalıştırıldı. Gerçek konuşma korutinleri hızlandırılmış oyun zamanı kullanılarak tamamlandı; bunlar üretim verilerine test metni olarak kaydedilmedi.
- 2/4/6/8 bitişlerinde yayın ve dördüncü seçimden sonra final.
- Gerçek vatandaş kararlarının aynı kaynak yöneticisini tüketmesi; bilgi/hazırlık onaylarının gereksiz ekip tüketmemesi.
- Atomik tahsis: yetersiz kaynakta kısmi değişiklik ve olay yok; geçerli tahsiste bir olay.
- Mevcut üç HUD metninin sayılarla eşleşmesi.
- Aynı vaka bitişinin tekrar gönderilmesi, teklif seçimine erken erişim, aynı/farklı teklifin tekrar seçilmesi.
- Kaynakların sonraki vakaya taşınması; son tahsisin final kaynaklarına dahil olması.
- Sıfır kaynakta iki geçerli acil alternatif ve tek uygulama.
- Tek bir geçersiz teklifin devre dışı kalması.
- Gerçek UI düğmesinin iki hızlı tıklamada tek satır ilerletmesi.
- Yazı efektinde ilk tıklamanın yalnızca mevcut satırı tamamlaması, sonraki tıklamanın ilerletmesi.
- Yeni vardiyada karar/vaka/doğru yönlendirme sayıları ve başlangıç kaynaklarının sıfırlanması.
- Tek EventSystem ve mevcut başkan Sprite referansı.
- Teklif ekranı Game View'da 1600×900 (16:9), 1200×900 (4:3), 2100×900 (21:9) olarak görüntülendi ve incelendi. Türkçe glifler, görsel oranı ve iki planın yerleşimi kontrol edildi.
- Final ekranı gerçek sekizli testin sonunda yakalandı. Dosya fade'in başlangıcında yakalandığından görüntü kısmi saydamdır; geçiş tamamlandığında panel opak olur.
- Mevcut telefonda IncomingCall → Focusing → Conversation.
- Ana menünün gerçek OYNA düğmesi tetiklendi; fade başladı, SampleScene açıldı, fade tamamlandı, telefon IncomingCall durumuna geldi. OYNA ve ÇIKIŞ düğmelerinin kayıtlı olayları doğrulandı.
- Son Console sorgusunda kodla ilgili hata yoktu. Unity'nin hesap servisi için ağ/odak kaynaklı bir uyarısı vardı.

Yerel sonuç: `Logs/PresidentialChecks/test-result.txt`. Ekran görüntüleri aynı klasördedir.

## Açık sınırlar

- Y-01'in özgün kaynak bölümünde mutfak yangını ile orman yangını içeriği birleşmiştir. Kayıt korundu, ana sıraya alınmadı ve `Ready For Play` kapalı bırakıldı. Yazar tarafından ayrıştırılmadan oynatılmaz.
- Diğer bütün alternatiflerin her dalı uzun, gerçek zamanlı bir insan oynanışıyla tek tek oynanmadı. Ana sekizlinin doğru dalları ve kaynak/akış uç durumları otomatik Play Mode kontrollerinden geçti.
- Bağımsız Windows build alınmadı; işletim sistemi düzeyinde gerçek ÇIKIŞ işlemi çalıştırılmadı. ÇIKIŞ olay bağlantısı ve değişmeyen metodu kontrol edildi.
- Normal hızda tam vardiya sesli oynanışı, her plan kombinasyonunun deneyimsel dengesi, eksik Sprite referansının canlı fixture testi ve farklı monitör DPI'ları ayrı test edilmedi. Görsel referansı yokken UI kodu korumalı bir metin yer tutucusu kullanır.
- Başlangıç kaynakları 10/10/10 olduğundan tek ekiplik vakalarda bütün doğru karar yolları kaynak kıtlığına yol açmayabilir. Tahsislerin fırsat maliyetleri, yanlış yönlendirmeler ve final hazırlığı gerçektir; daha sert denge için mevcut başlangıç sayıları ve veri planları Inspector'dan ayarlanabilir.

## Düzenleme ve yeniden test

Konuşma metinleri ve iki plan, dört Briefing varlığında düzenlenir. Vatandaş metinleri Scenarios altındaki ayrı varlıklardadır. Sahnedeki Scenario Order sırası kullanıcı onaylı sekiz varlığı tutar. Başkan yayınında üçüncü seçenek yoktur; konuşma ve kapanıştaki DEVAM yalnızca metin ilerletir.

Yeni sahneye bağlamak için mevcut editör menüsü: **GameJam → Başkan Sistemini Mevcut Sahneye Entegre Et**. Play Mode'da `PresidentialBriefingChecks.StartRun()` kontrol çalışmasını başlatır ve sonucu yerel Logs klasörüne yazar. Kontroller zaman ölçeğini geçici artırır ve tamamlandığında geri yükler.
