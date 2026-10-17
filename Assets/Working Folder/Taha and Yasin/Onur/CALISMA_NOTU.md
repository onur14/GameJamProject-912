# Taha ve Yasin çalışma kopyası

Oyun: `Onur Test Place Integrated.unity`
Menü: `../AnaMenu.unity` (Taha and Yasin/AnaMenu)

`OnurAnaMenu.unity`, `Assets/Working Folder/Onur/OnurAnaMenu.unity` sahnesinin güncel bağımsız kopyasıdır. OYNA bağlantısı bu klasördeki `Onur Test Place Integrated.unity` sahnesine yönelir. Operatör sahnesindeki Taha ve Yasin'e özgü yeni işlevler korunur; C# ve diğer ortak bağımlılıklar proje genelinden paylaşılır.

Bu sahneler ve kopyalanmış prefablar/PNG'ler bu klasörde düzenlenir. Build Settings başlangıcı buradaki menü ve oyundur. Menü, tekrar oyna ve ana menü dönüşü bu kopyayı kullanır.

C# sınıfları Unity projesi genelinde ortaktır. Çift sınıf/GUID hatası oluşturmamak için Onur/Scripts ve Assets/Scripts/Presidential altındaki derlenen kaynaklar paylaşılır; kaynak klasör taşınmamıştır. Asıl Onur sahne ve prefabları kopyalama sırasında korunmuştur.

GameJam > Taha Yasin menüsü çalışma sahnesini açar ve gerektiğinde bu kopyanın bağlantılarını kaydeder. Bağlantı aracı yalnızca bu sahnede çalışır.

İkinci teyit soruları: yanlış veya süre aşımı -10 sağlık; doğru cevap kaynak tüketmez. İlk soru iki, ikinci soru en az üç ana konuşma satırından sonra zamanlanır. Metin 38 yerine 40 karakter/sn; satır beklemesi 0,8–1,8 sn sınırında kalır.

Başkan öncesi rapor otomatik 5 sn görünür. Kişi sayısı oyun içi tahmini erişimdir (doğru ana karar başına varsayılan 1250); Inspector üzerinden ayarlanabilir.

## Ceza ve ödüller

Ana karar 15 saniye yanıtsız kalırsa yalnızca bir kez -5 sağlık; oyuncunun seçim hakkı devam eder ve minigame duraklamasında sayaç durur. Yan talebe zamanında yanıt +2 sağlık. Teyit sorusunda doğru yanıt +5 sağlık ve o soruya atanmış +1 ekip; yanlış/süre aşımı -10 sağlık. Ödüller tek seferliktir, sağlık %100 ve ekip rezervi 6 sınırını aşmaz; rezerv doluyken doğru yanıt engellenmez.

## Kamu duyuru tableti

Taha and Yasin/AnaMenu, bu klasördeki mevcut oyun sahnesine girer. Monitörün sağ altındaki ok tableti alttan açar. Afet Çantası ve Ev Güvenlik Kontrolü tablet içindedir; köşe çerçevesi ve REC göstergesi kullanılır.

Tabletli kullanımda telefon, diyalog ve yan soru sayaçları duraklatılmaz. Gerçek eşya yerleştirme veya yeni tesisat seçimi anında +2/-2 sağlık uygular (%100 sınırı korunur). Aynı konuma bırakma / aynı seçeneği yeniden tıklama ödül vermez; doğru-yanlış arasında gidip gelmek toplam ödülü artırmaz. Eski bitişte +10 ödülü tablet kullanımında verilmez. Kapatıp açmak görev sonucunu veya hakları sıfırlamaz.

Sağlık etkisi fare üstünde kısa süre yükselip salınan bir yazıdır. GameJam > Taha Yasin > Tableti Kur ve Menüyü Bağla bağlantıları yeniden kurar.

Play Mode kontrolleri: Logs/TabletChecks/result.txt. Arka çağrının ilerlemesi, UI raycast, sürükleme/bırakma, anlık sağlık, tekrar ödül engeli, tablet kapatma ve kayan yazının yok olması doğrulandı.
