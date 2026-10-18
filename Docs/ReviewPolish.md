# Oynanabilirlik ve eğitim metni düzenlemesi — 18 Ekim 2026

İlk ana karar 18 saniye, sonraki kararlar 12 saniye. İlk yanlış ana cevap −10, diğerleri −20; cevapsız karar cezası hâlâ bir kez −5. İlk çağrıda ana kararın ardından yalnızca bir destek talebi gelir. Sonraki çağrılarda iki yan panelin girişleri arasında en az 2,5 saniye vardır; yan soru süreleri ilk çağrıda 12, sonraki çağrılarda 9 saniyedir. İlk çağrıda bağlantı onarma olayı yoktur.

Yavaşlık Cezası görünür adı Lojistik Aksama oldu. Her ekipten 2 kesilmesi korunur; metin takviye sevkiyatının aksadığını açıklar. Vana/şalter eğitiminde cevap etiketleri kaldırıldı. Yerler yeni çekimde karıştırılır; alev ve yıldırım simgeleri eşleştirilir. Kapatıp açma aynı çekimin düzenini değiştirmez.

Görsel geri bildirimde sarsıntı azaltıldı. Karar anında müzik kısılır; başkan/operatör müziği arasında kısa ses geçişi vardır. Ses seviyesi değişiklikleri işitsel kullanıcı testi yerine kod ve playback durumuyla kontrol edilmiştir.

## Kaynak kontrolü

Açılıştaki Heinrich'e atfedilen “afetlerin %88'i” ifadesi doğrulanmış bir afet istatistiği olarak kullanılamadığı için kaldırıldı. Yerine aşağıdaki UNDRR kaynağının risk yaklaşımından uyarlanmış, doğrudan alıntı olarak sunulmayan Türkçe metin kondu.

- UNDRR: https://www.undrr.org/our-impact/campaigns/no-natural-disasters
- AFAD deprem öncesi/anı/sonrası: https://afad.gov.tr/deprem-oncesi-ani-ve-sonrasi-onlemini-al-guvende-kal
- AFAD temel önlemler: https://www.afad.gov.tr/deprem-oncesi-ani-ve-sonrasi-alabileceginiz-onlemleri-biliyor-musunuz?v=1.0.12
- AFAD yangın, bakım ve dedektör: https://afad.gov.tr/afadem/yangin
- AFAD sel: https://bursa.afad.gov.tr/sel
- American Red Cross, gösterge kontrolü ve eğitim: https://www.redcross.org/get-help/how-to-prepare-for-emergencies/types-of-emergencies/fire/fire-safety-equipment.html
- MGM, ağaçlardan uzaklaşma: https://mgm.gov.tr/kurumsal/haberler.aspx?f=yildirim&y=2012
- NOAA/NWS, tsunami sırasında kıyıdan yükseğe/iç kesime geçiş: https://www.weather.gov/safety/tsunami-during

İlk deprem vakasının doğru cevabındaki “içeri dön” ve bazaya yönlendirme metni, bulunulan yerde korunma ve yakındaki sağlam masa altında baş/boyun koruyarak tutunma olarak düzeltildi. Diğer senaryoların tamamı için bağımsız uzman onayı alınmış değildir. Oyun gerçek acil yardım hizmeti veya bakım eğitimi yerine geçmez.

## Doğrulama

Test kayıtları: Logs/ReviewPolish/checks.txt ve Logs/SixPreparationChecks/result.txt. Bu kayıtlar otomatik Play Mode kontrolleridir; projeyi bilmeyen bir kişinin kullanıcı testi veya başka bilgisayar testi olarak sunulmamalıdır.

## Windows build doğrulaması

Builds/ReviewWindows/GameJam912.exe, Windows x64 Development build: başarılı, 0 hata; raporda 519 uyarı var. Uyarılar arasında Unity paketlerinin ConvGeneric compute shader varyantları bulunuyor. Bu test build'inde kamera post-processing'i kapalıyken URP'nin kullanımdan çıkarılan post-process shader'larını başlatmasına ilişkin uyarılar da görüldü; menü ve hazırlık görüntüsü çalıştı. Bunlar “uyarısız build” olarak sunulmamalıdır.

Aynı bilgisayarda bağımsız çalıştırılabilir dosyanın ana menüsü, kaynaklı açılış metni ve Oyna → Çekime Başla → Anladım geçişi gerçek pencere tıklamalarıyla kontrol edildi. Başka bilgisayarda veya bağımsız insan katılımcıyla test yapılmadı. Geliştirme build'inin BackUpThisFolder_ButDontShipItWithYourGame klasörü dağıtılacak oyun dosyalarına eklenmemelidir.

Otomatik hazırlık turunun ilk iki denemesinde editör odakta değilken klavye enjeksiyonu alınamadı. Unity öne alınarak tekrarlanan altı görev turu tamamlandı. Ham test kayıtları zaman aşımı denemelerini ve başarılı son turu birlikte içerir.
