# Yeni hazırlık minigameleri

Taha ve Yasin akışının kullandığı mevcut Preparation sahnesine bağlıdır. Yeni oyun kodları bu klasördedir; ortak görev sırası PreparationDirector, PreparationRun ve PreparationBroadcast içinde güncellendi. Yeni framework veya ayrı kayıt sistemi eklenmedi.

- 5: Yangın tüpü eğitim düzeneği. ARTIR/AZALT basılı tutulur; 42–62 normalize yeşil bantta 0,65 saniye dengelenen ibre kontrol edilir. Gerçek tüp dolumu veya basınç değeri talimatı değildir.
- 6: Beş noktadan doğalgaz hattı ve elektrik ana şalteri işaretlenir. Üçü boş hattır. İşaretleme doğruluğu KONTROL ET anında değerlendirilir. Elektrik görseli vanayı değil ana şalteri temsil eder.
- Mevcut tamamlanma ödülü ve hata sistemi korunur. Tek çekim bütçesi 45 yerine 75 saniyedir. Yeni çekim her iki oyunu sıfırlar; aynı çekimde kapat/aç seçimleri korur. Yayın montajı altı kliptir.

## AI görsel üretimi

OpenAI built-in imagegen aracı, transparent_background=true kullanıldı; API anahtarı/CLI kullanılmadı. Seçilen PNG'ler projeye kopyalandı, Unity'de point filtering ve şeffaflıkla içe aktarıldı. Kaynak görseller generated_images klasöründe korundu.

Kaydedilen görseller:
- Assets/Resources/Disaster/ExtinguisherTraining.png
- Assets/Resources/Disaster/UtilityTrainingSheet.png

Prompt seti:
1. Front-facing red fire extinguisher, black hose, brass valve housing, small unmarked gauge; crisp chunky pixel art, charcoal outlines, red/brass/cool blue palette, centered full silhouette, genuine transparent background, no letters/digits/logo/UI or hands. The interactive enlarged gauge is created separately in Unity UI.
2. Five equal-width horizontal transparent sprite cells: unused capped grey pipe and handwheel, yellow natural-gas pipe with yellow handwheel and flame pictogram, unused grey pipe, blue-grey electrical main disconnect cabinet with lever and lightning symbol, unused grey pipe. Front-facing polished chunky pixel art, separated complete silhouettes, no text/digits/background/shared pipes. Electrical cabinet is a switch, not a pipe valve.

Play Mode test kaydı: Logs/SixPreparationChecks/result.txt. Ekran görüntüleri aynı klasörde. Tam altı oyun turu, işaretleme ve basılı tutma etkileşimi, yanlış kontrol, tek ödül, yeniden açma, süre aşımı, yeni çekim sıfırlaması ve altı kliplik yayın kontrol edildi.
