using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PresidentialBriefingInstaller
{
    private const string DataFolder = "Assets/PresidentialData";
    public const string IntegratedScene = "Assets/Working Folder/Onur/Onur Test Place Integrated.unity";
    public const string PortraitPath = "Assets/Art/UI/Maın_desk_scene/Gece yayınında sakin ve hazırlıklı başkan.png";

    private static ResourceOffer Offer(string title, string reason, int ambulance, int fire, int rescue)
        => new ResourceOffer { title = title, rationale = reason, ambulances = ambulance, firefighters = fire, thirdTeam = rescue };

    public static PresidentialBriefingData[] CreateData()
    {
        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets", "PresidentialData");
        var result = new PresidentialBriefingData[4];
        string[] headings = { "HAZIRLIK EVDE BAŞLAR", "SARSINTIDAN SONRA İLK MÜDAHALE", "DOĞRU BİLGİ, DOĞRU SEVK", "BİR SONRAKİ VARDİYAYA" };
        string[][] lines = {
            new[] {
                "İki çağrıyı geride bıraktık. Telefonun öbür ucundaki korkuyu siz duydunuz. O korkuyu azaltmanın bir yolu da afet gelmeden hazırlanmaktır.",
                "Evde bir afet çantası hazır bulunsun. Aileler nerede buluşacağını önceden konuşsun, toplanma alanını öğrensin. Sabitlenmiş bir dolap, o gün bir çıkış yolunu açık tutabilir.",
                "Ekrandaki mevcut ekiplerimiz bütün taleplere aynı anda yetişemez. Yeni destek için sahadaki dağılımı değiştirmemiz gerekiyor. Sağlık kapasitesini mi, kurtarma gücünü mü artıracağız?" },
            new[] {
                "Sarsıntı sırasında birkaç saniye uzun gelir. O birkaç saniyede verilen doğru yönlendirme, bir ailenin sonraki çağrıyı yapabilmesini sağlar.",
                "İçerideyseniz güvenli bir noktada Çök-Kapan-Tutun yapın; başınızı ve boynunuzu koruyun. Sarsıntı bitmeden merdivene yönelmeyin, asansör kullanmayın.",
                "Yaralıya ulaşmakla enkazdaki insana ulaşmak aynı hazırlığı istemiyor. Mevcut ekiplere bakarak karar verin: kurtarma hattını mı büyüteceğiz, sağlık ve yangın müdahalesini mi güçlendireceğiz?" },
            new[] {
                "Her ihbar bir ekibin yolunu değiştiriyor. Yanlış bir adres ya da doğrulanmamış bir haber, başka bir evin kapısına ulaşmamızı geciktirebilir.",
                "Acil hatları gerçek acil durumlar için açık tutalım. İhbarda konumu ve gördüğümüz durumu açıkça aktaralım. Doğrulanmamış haberleri paylaşmayalım; resmî açıklamaları izleyelim.",
                "Altı vakanın ardından kalan gücümüz ekranda. Yangınlara ayrılan kapasiteyi artırmak kurtarma yedeğini azaltacak. Saha erişimine ağırlık vermek ise ambulans desteğinden fedakârlık isteyecek." },
            new[] {
                "Bu vardiyada {tamamlanan} vaka tamamladınız, {karar} tahsis kararı verdiniz. Her tercih, sınırlı bir ekibin hangi kapıya önce ulaşacağını belirledi. Şimdi bu sorumluluğu gelecek vardiyaya devrediyoruz.",
                "Dayanışma, tehlikeye hazırlıksız koşmak değildir. Komşumuzun ihtiyacını bilmek, aile planını birlikte yapmak ve doğrulanmış bilgiyi paylaşmak da yardımın bir parçasıdır.",
                "Bu gece yeni çağrı açılmayacak. Yine de son kararınızın karşılığı var: kalan ekipler ve bu tahsis, sonraki vardiyanın hazırlık durumuna yazılacak. Sağlık yedeğini mi, saha dayanıklılığını mı güçlendirelim?" }
        };
        ResourceOffer[] a = {
            Offer("SAĞLIK TAKVİYESİ", "Kurtarma yedeğinin bir bölümü sağlık sevkine aktarılır.", 3,0,-1),
            Offer("ENKAZA ERİŞİM", "Ambulans desteği azaltılarak kurtarma hattı büyütülür.",-2,1,4),
            Offer("YANGIN ÇEMBERİ", "Kurtarma yedeği daralır; yangın müdahalesi güçlenir.",1,4,-3),
            Offer("SAĞLIK NÖBETİ", "İtfaiye yedeğinden sağlık hazırlığına aktarım yapılır.",4,-2,1)
        };
        ResourceOffer[] b = {
            Offer("SAHA DESTEĞİ", "Bir ambulanslık kapasite, iki kurtarma ekibine ayrılır.",-1,1,2),
            Offer("İLK MÜDAHALE AĞI", "Kurtarma yedeği azaltılır; sağlık ve yangın gücü artar.",3,2,-2),
            Offer("GÜVENLİ ERİŞİM", "Sağlık yedeği azalır; kurtarma ve yangın hattı büyür.",-2,2,4),
            Offer("MAHALLE DAYANIKLILIĞI", "Bir ambulanslık destek, saha hazırlığına aktarılır.",-1,3,3)
        };
        string[] closing = {
            "Planı onaylıyorum. Ekipler yeni dağılıma göre göreve hazır. Bir sonraki ihbarda hem doğru yönlendirmeye hem bu kapasiteye ihtiyacımız olacak.",
            "Tahsis yürürlükte. Her ekibi doğru ihtiyaçla buluşturalım. Yeni çağrıda kararımızın sahadaki karşılığını göreceğiz.",
            "Dağılım güncellendi. Kaynağımızı korumak, yardım isteyen bir sonraki kişiye zaman kazandırmaktır. Görevinize devam edin.",
            "Devir planını onaylıyorum. Son dağılım rapora işlendi. Hazırlığımızı yarına taşıyacağız; bu sorumluluk tek bir vardiyayla bitmiyor."
        };
        for (int i=0;i<4;i++)
        {
            string path = $"{DataFolder}/Briefing{i+1}.asset";
            var data = AssetDatabase.LoadAssetAtPath<PresidentialBriefingData>(path);
            if (!data)
            {
                data = ScriptableObject.CreateInstance<PresidentialBriefingData>();
                data.briefingId = "president_" + (i+1); data.afterScenario = (i+1)*2;
                data.heading = headings[i]; data.dialogueLines = lines[i]; data.planA=a[i]; data.planB=b[i];
                data.closing=closing[i]; data.endsShift=i==3;
                AssetDatabase.CreateAsset(data,path);
            }
            result[i]=data;
        }
        AssetDatabase.SaveAssets(); return result;
    }
    private static void Reference(Object target, string field, Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static TMP_Text Label(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform; rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.sizeDelta=size; rect.anchoredPosition=position;
        var text=go.GetComponent<TextMeshProUGUI>(); text.fontSize=28; text.color=Color.white;
        text.raycastTarget=false; return text;
    }
    public static string InstallActiveScene()
    {
        if (EditorApplication.isPlaying) return "Exit Play Mode first.";
        var scene = SceneManager.GetActiveScene();
        var phone = Object.FindFirstObjectByType<DispatchCallFlow>();
        if (!phone) return "No existing DispatchCallFlow in active scene.";
        if (Object.FindFirstObjectByType<PresidentialBriefingManager>()) return "Already installed.";
        var canvas = phone.GetComponentInParent<Canvas>();
        var resources = Object.FindFirstObjectByType<DispatchResources>();
        var flow = Object.FindFirstObjectByType<OyunAkisiOnur>();
        if (!flow)
        {
            // Reuse the authored Onur panel, never rewrite the room or its phone animation.
            var sourceScene = EditorSceneManager.OpenScene(IntegratedScene,OpenSceneMode.Additive);
            var source = sourceScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OyunAkisiOnur>(true)).First();
            var sourceSo = new SerializedObject(source);
            var sourceView=(OnurConversationView)sourceSo.FindProperty("conversationView").objectReferenceValue;
            var phoneSo = new SerializedObject(phone);
            var previous=(RectTransform)phoneSo.FindProperty("conversationPanel").objectReferenceValue;
            var panel = Object.Instantiate(sourceView.gameObject,previous.parent,false);
            panel.name="OnurConversation";
            previous.gameObject.SetActive(false);
            flow = new GameObject("Vardiya Konuşma Akışı").AddComponent<OyunAkisiOnur>();
            SceneManager.MoveGameObjectToScene(flow.gameObject,scene);
            EditorUtility.CopySerialized(source,flow);
            Reference(flow,"conversationView",panel.GetComponent<OnurConversationView>());
            Reference(flow,"dispatchCallFlow",phone);
            var timer=Label(canvas.transform,"Vaka Geri Sayımı",new Vector2(0,-420),new Vector2(850,60));
            var warning=Label(canvas.transform,"Vaka Uyarısı",new Vector2(0,-340),new Vector2(900,100));
            timer.gameObject.SetActive(false); warning.gameObject.SetActive(false);
            Reference(flow,"sahneGeriSayimi",timer); Reference(flow,"sahneUyarisi",warning);
            Reference(phone,"conversationPanel",panel.GetComponent<RectTransform>());
            // Clone-local buttons must target the cloned view, not the source scene.
            foreach(var button in panel.GetComponentsInChildren<Button>(true))
            {
                var buttonSo=new SerializedObject(button);
                var calls=buttonSo.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                for(int i=0;i<calls.arraySize;i++) calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Target").objectReferenceValue=panel.GetComponent<OnurConversationView>();
                buttonSo.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.CloseScene(sourceScene,true);
            SceneManager.SetActiveScene(scene);
        }
        if (!resources)
        {
            resources=canvas.gameObject.AddComponent<DispatchResources>();
            string[] fields={"ambulancesText","firefightersText","policeText"};
            string[] titles={"AMBULANS","İTFAİYE","KURTARMA"};
            for(int i=0;i<3;i++)
            {
                var title=Label(canvas.transform,titles[i],new Vector2(580+i*135,480),new Vector2(140,35));title.text=titles[i];title.fontSize=19;
                var value=Label(canvas.transform,titles[i]+" Sayısı",new Vector2(580+i*135,440),new Vector2(140,40));value.text="10";
                Reference(resources,fields[i],value);
            }
        }
        Reference(flow,"resources",resources);
        foreach(var text in canvas.GetComponentsInChildren<TMP_Text>(true))
            if (!string.IsNullOrEmpty(text.text) && (text.text.ToUpperInvariant().Contains("POLİS") || text.text.ToUpperInvariant()=="POLIS")) { text.text="KURTARMA"; EditorUtility.SetDirty(text); }
        var importer=(TextureImporter)AssetImporter.GetAtPath(PortraitPath);
        if (importer && (importer.filterMode!=FilterMode.Point || importer.textureType!=TextureImporterType.Sprite || importer.textureCompression!=TextureImporterCompression.Uncompressed))
        {
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        var root=new GameObject("Başkan Bilgilendirme",typeof(RectTransform));
        var rt=(RectTransform)root.transform; rt.SetParent(canvas.transform,false);
        rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
        var view=root.AddComponent<PresidentialBroadcastView>();
        view.presidentPortrait=AssetDatabase.LoadAssetAtPath<Sprite>(PortraitPath);
        view.broadcastFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/UI/MainMenu/DispatchMenuPixel SDF.asset");
        var fallback=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if(view.broadcastFont && fallback && !view.broadcastFont.fallbackFontAssetTable.Contains(fallback))
        { view.broadcastFont.fallbackFontAssetTable.Add(fallback);EditorUtility.SetDirty(view.broadcastFont); }
        var manager=root.AddComponent<PresidentialBriefingManager>();
        Reference(manager,"resources",resources);Reference(manager,"conversation",flow);Reference(manager,"phone",phone);Reference(manager,"view",view);
        var so=new SerializedObject(manager);var array=so.FindProperty("briefings");var data=CreateData();
        array.arraySize=4;for(int i=0;i<4;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=data[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Installed in "+scene.path+". Scenario slots 2–8 require existing citizen content.";
    }
    [MenuItem("GameJam/Başkan Sistemini Mevcut Sahneye Entegre Et")]
    public static void InstallMenu() { Debug.Log(InstallActiveScene()); }
}
