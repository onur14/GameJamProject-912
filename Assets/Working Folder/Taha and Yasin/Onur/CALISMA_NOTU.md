# Taha ve Yasin çalışma kopyası

Oyun: `Onur Test Place Integrated.unity`
Menü: `OnurAnaMenu.unity`

Bu sahneler ve kopyalanmış prefablar/PNG'ler bu klasörde düzenlenir. Build Settings başlangıcı buradaki menü ve oyundur. Menü, tekrar oyna ve ana menü dönüşü bu kopyayı kullanır.

C# sınıfları Unity projesi genelinde ortaktır. Çift sınıf/GUID hatası oluşturmamak için Onur/Scripts ve Assets/Scripts/Presidential altındaki derlenen kaynaklar paylaşılır; kaynak klasör taşınmamıştır. Asıl Onur sahne ve prefabları kopyalama sırasında korunmuştur.

GameJam > Taha Yasin menüsü çalışma sahnesini açar ve gerektiğinde bu kopyanın bağlantılarını kaydeder. Bağlantı aracı yalnızca bu sahnede çalışır.

İkinci teyit soruları: yanlış veya süre aşımı -10 sağlık; doğru cevap kaynak tüketmez. İlk soru iki, ikinci soru en az üç ana konuşma satırından sonra zamanlanır. Metin 38 yerine 40 karakter/sn; satır beklemesi 0,8–1,8 sn sınırında kalır.

Başkan öncesi rapor otomatik 5 sn görünür. Kişi sayısı oyun içi tahmini erişimdir (doğru ana karar başına varsayılan 1250); Inspector üzerinden ayarlanabilir.

## Ceza ve ödüller

Ana karar 15 saniye yanıtsız kalırsa yalnızca bir kez -5 sağlık; oyuncunun seçim hakkı devam eder ve minigame duraklamasında sayaç durur. Yan talebe zamanında yanıt +2 sağlık. Teyit sorusunda doğru yanıt +5 sağlık ve o soruya atanmış +1 ekip; yanlış/süre aşımı -10 sağlık. Ödüller tek seferliktir, sağlık %100 ve ekip rezervi 6 sınırını aşmaz; rezerv doluyken doğru yanıt engellenmez.
