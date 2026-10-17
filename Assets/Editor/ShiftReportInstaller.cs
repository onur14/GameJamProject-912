using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ShiftReportInstaller
{
    static TMP_FontAsset font;
    static readonly Color Paper=new Color(.88f,.93f,.90f), Secondary=new Color(.66f,.76f,.81f), Amber=new Color(.91f,.72f,.40f);
    static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
    {
        var t=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();t.SetParent(parent,false);
        t.anchorMin=min;t.anchorMax=max;t.offsetMin=t.offsetMax=Vector2.zero;return t;
    }
    static Image Panel(string name,Transform parent,Vector2 min,Vector2 max,Color color)
    {var image=Rect(name,parent,min,max).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
    static TMP_Text Label(string name,Transform parent,Vector2 min,Vector2 max,string value,float size,bool heading=false)
    {
        var text=Rect(name,parent,min,max).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;
        text.text=value;text.fontSize=size;text.fontStyle=heading?FontStyles.Bold:FontStyles.Normal;
        text.color=heading?Amber:Paper;text.alignment=TextAlignmentOptions.TopLeft;text.raycastTarget=false;
        text.textWrappingMode=TextWrappingModes.Normal;text.overflowMode=TextOverflowModes.Truncate;
        text.enableAutoSizing=true;text.fontSizeMax=size;text.fontSizeMin=size*.9f;text.lineSpacing=6;text.extraPadding=true;
        return text;
    }
    static Button Button(string name,Transform parent,Vector2 min,Vector2 max,string value)
    {
        var image=Panel(name,parent,min,max,new Color(.12f,.20f,.25f));image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.18f,1.22f,1.24f);
        colors.selectedColor=new Color(1.10f,1.14f,1.17f);colors.pressedColor=new Color(.72f,.77f,.81f);colors.fadeDuration=.1f;button.colors=colors;
        var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
        var text=Label("Metin",button.transform,new Vector2(.035f,.08f),new Vector2(.965f,.92f),value,26);
        text.alignment=TextAlignmentOptions.Center;text.lineSpacing=0;return button;
    }
    static void Set(UnityEngine.Object target,string field,UnityEngine.Object value)
    {var data=new SerializedObject(target);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();}
    static void Rule(Transform parent,float y)
    {Panel("Bölüm Çizgisi",parent,new Vector2(.05f,y),new Vector2(.95f,y+.002f),new Color(.21f,.30f,.35f));}

    [MenuItem("Tools/Onur/Install Shift Report in AnaMenu")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        // Replace the scene entry itself so the serialized build list records the enabled flag.
        var build=EditorBuildSettings.scenes.ToList();int entryIndex=build.FindIndex(s=>s.path==ShiftReportFlow.MenuScene);
        var enabledEntry=new EditorBuildSettingsScene(ShiftReportFlow.MenuScene,true);
        if(entryIndex>=0)build[entryIndex]=enabledEntry;else build.Add(enabledEntry);
        EditorBuildSettings.scenes=build.ToArray();AssetDatabase.SaveAssets();
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/UI/Fonts/VCR_OSD_MONO_1.001 SDF.asset");
        if(!font)throw new InvalidOperationException("Existing pixel font missing");
        var menu=SceneManager.GetSceneByPath(ShiftReportFlow.MenuScene);bool opened=!menu.isLoaded;
        if(opened)menu=EditorSceneManager.OpenScene(ShiftReportFlow.MenuScene,OpenSceneMode.Additive);
        try
        {
            if(menu.GetRootGameObjects().Any(go=>go.GetComponent<ShiftReportView>()))return;
            var normalCanvas=menu.GetRootGameObjects().Single(go=>go.name=="Canvas");
            var root=new GameObject("Vardiya Raporu",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(ShiftReportView));
            SceneManager.MoveGameObjectToScene(root,menu);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=500;canvas.pixelPerfect=true;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var view=root.GetComponent<ShiftReportView>();
            var background=Panel("Rapor Ekranı",root.transform,Vector2.zero,Vector2.one,new Color(.025f,.045f,.065f,1));background.raycastTarget=true;
            var frame=Panel("Rapor Çerçevesi",background.transform,new Vector2(.12f,.065f),new Vector2(.88f,.935f),new Color(.18f,.26f,.31f));
            var card=Panel("Rapor İçeriği",frame.transform,new Vector2(.003f,.004f),new Vector2(.997f,.996f),new Color(.045f,.075f,.10f)).transform;
            var brand=Label("Kurum",card,new Vector2(.05f,.934f),new Vector2(.95f,.973f),"912 // ACİL ÇAĞRI MERKEZİ",20);brand.color=Secondary;
            Label("Rapor Başlığı",card,new Vector2(.05f,.854f),new Vector2(.95f,.918f),"VARDİYA TAMAMLANDI",38,true).color=Paper;
            Label("Vardiya",card,new Vector2(.05f,.802f),new Vector2(.95f,.845f),"Gece Vardiyası Sonuçları",22).color=Secondary;
            Rule(card,.775f);
            Label("Performans Başlığı",card,new Vector2(.05f,.716f),new Vector2(.60f,.756f),"PERFORMANS",20,true);
            var performance=Label("Performans Değerleri",card,new Vector2(.05f,.548f),new Vector2(.62f,.704f),"",24);
            Label("Kaynak Başlığı",card,new Vector2(.66f,.716f),new Vector2(.95f,.756f),"KALAN KAYNAKLAR",20,true);
            var resources=Label("Kaynak Değerleri",card,new Vector2(.66f,.548f),new Vector2(.95f,.704f),"",24);
            Rule(card,.526f);
            Label("Değerlendirme Başlığı",card,new Vector2(.05f,.470f),new Vector2(.95f,.510f),"VARDİYA DEĞERLENDİRMESİ",20,true);
            var grade=Label("Vardiya Notu",card,new Vector2(.05f,.395f),new Vector2(.49f,.461f),"",32);
            var operation=Label("Operasyon Durumu",card,new Vector2(.55f,.382f),new Vector2(.95f,.461f),"",22);
            Rule(card,.361f);
            Label("Olay Başlığı",card,new Vector2(.05f,.305f),new Vector2(.95f,.345f),"OLAY ÖZETİ",20,true);
            var events=Label("Olay Özeti",card,new Vector2(.05f,.155f),new Vector2(.95f,.293f),"",22);
            var replay=Button("Tekrar Oyna",card,new Vector2(.05f,.052f),new Vector2(.36f,.126f),"TEKRAR OYNA");
            var mainMenu=Button("Ana Menü",card,new Vector2(.39f,.052f),new Vector2(.70f,.126f),"ANA MENÜ");
            var score=Label("Puan",card,new Vector2(.74f,.058f),new Vector2(.95f,.116f),"",20);score.color=Secondary;score.alignment=TextAlignmentOptions.MidlineRight;
            Set(view,"reportPanel",background.gameObject);Set(view,"normalMenu",normalCanvas);Set(view,"performance",performance);
            Set(view,"resources",resources);Set(view,"grade",grade);Set(view,"operation",operation);Set(view,"events",events);Set(view,"score",score);
            Set(view,"replayButton",replay);Set(view,"menuButton",mainMenu);background.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(menu);EditorSceneManager.SaveScene(menu);
        }
        finally{if(opened)EditorSceneManager.CloseScene(menu,true);}
    }
}
