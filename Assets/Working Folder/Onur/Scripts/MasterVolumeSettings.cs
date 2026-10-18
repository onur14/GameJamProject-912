using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static PreparationUI;

public sealed class MasterVolumeSettings : MonoBehaviour
{
    private const string PreferenceKey = "912.MasterVolume";
    private const string DisplayKey = "912.Display.";
    [SerializeField] private Button openButton, closeButton;
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text percentageLabel;
    private Button fullscreenButton, resolutionButton, applyButton;
    private TMP_Text fullscreenLabel, resolutionLabel, appliedLabel;
    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private bool pendingFullscreen;
    private int resolutionIndex;
    public Button FullscreenButton => fullscreenButton;
    public Button ResolutionButton => resolutionButton;
    public Button ApplyButton => applyButton;
    public Button OpenButton => openButton;
    public Button BackButton => closeButton;
    public bool IsOpen => panel.activeSelf;
    public bool PendingFullscreen => pendingFullscreen;
    public Vector2Int PendingResolution => resolutions[resolutionIndex];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RestorePreferences()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey, 1f));
        if (PlayerPrefs.HasKey(DisplayKey+"Width"))
            Screen.SetResolution(PlayerPrefs.GetInt(DisplayKey+"Width"), PlayerPrefs.GetInt(DisplayKey+"Height"),
                PlayerPrefs.GetInt(DisplayKey+"Fullscreen",1)==1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    // The current main menu lacks the settings controller found in the report menu.
    public static void EnsureMenu(Canvas canvas)
    {
        if (!canvas || canvas.GetComponentInChildren<MasterVolumeSettings>(true)) return;
        var play = canvas.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name=="Oyna");
        if (!play) return;
        var exit = canvas.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)=="CikisYap"));
        var font = play.GetComponentInChildren<TMP_Text>().font;
        var open = Button("Ayarlar",play.transform.parent,Vector2.zero,Vector2.one,"AYARLAR",font);
        var r=(RectTransform)open.transform;
        var source=(RectTransform)play.transform;
        r.anchorMin=source.anchorMin;r.anchorMax=source.anchorMax;r.pivot=source.pivot;r.sizeDelta=source.sizeDelta;
        source.anchoredPosition=new Vector2(0,-258);r.anchoredPosition=new Vector2(0,-314);
        if(exit)((RectTransform)exit.transform).anchoredPosition=new Vector2(0,-370);
        open.image.color=play.image.color;
        open.colors=play.colors;
        var owner=new GameObject("Menü Ayarları");owner.SetActive(false);owner.transform.SetParent(canvas.transform,false);
        var controller=owner.AddComponent<MasterVolumeSettings>();controller.openButton=open;
        var shade=Panel("Ayarlar Karartması",canvas.transform,Vector2.zero,Vector2.one,new Color(0,0,0,.86f));shade.raycastTarget=true;
        var overlay=shade.gameObject.AddComponent<Canvas>();overlay.overrideSorting=true;overlay.sortingOrder=600;shade.gameObject.AddComponent<GraphicRaycaster>();
        var card=Panel("Ayarlar Paneli",shade.transform,new Vector2(.25f,.17f),new Vector2(.75f,.83f),Navy).rectTransform;
        Label("Başlık",card,new Vector2(.08f,.85f),new Vector2(.92f,.96f),"AYARLAR",font,30);
        Label("Ses Başlığı",card,new Vector2(.08f,.73f),new Vector2(.65f,.83f),"GENEL SES",font,22).alignment=TextAlignmentOptions.Left;
        controller.percentageLabel=Label("Ses Yüzdesi",card,new Vector2(.70f,.73f),new Vector2(.92f,.83f),"",font,22);
        var slider=Rect("Genel Ses Kaydırıcısı",card,new Vector2(.08f,.62f),new Vector2(.92f,.72f)).gameObject.AddComponent<Slider>();
        Panel("Ray",slider.transform,new Vector2(0,.35f),new Vector2(1,.65f),PanelColor).raycastTarget=true;
        slider.fillRect=Panel("Doluluk",slider.transform,new Vector2(0,.35f),new Vector2(1,.65f),new Color(.35f,.82f,.83f)).rectTransform;
        var handle=Panel("Tutamak",slider.transform,new Vector2(0,.08f),new Vector2(.025f,.92f),White);handle.raycastTarget=true;
        slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;controller.volumeSlider=slider;
        controller.closeButton=Button("Geri",card,new Vector2(.08f,.07f),new Vector2(.47f,.18f),"GERİ",font);
        controller.panel=shade.gameObject;owner.SetActive(true);
    }

    private void Awake()
    {
        float volume=Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey,1f));
        AudioListener.volume=volume;volumeSlider.SetValueWithoutNotify(volume);RefreshLabel(volume);
        var card=closeButton.transform.parent;
        // Reuse the existing panel, typography, slider and back control in older menus.
        var cardRect=(RectTransform)card;cardRect.anchorMin=new Vector2(.25f,.17f);cardRect.anchorMax=new Vector2(.75f,.83f);
        foreach(var t in card.GetComponentsInChildren<TMP_Text>(true))
        {
            if(t.name=="Ses Başlığı")Place(t.rectTransform,.08f,.73f,.65f,.83f);
            else if(t==percentageLabel)Place(t.rectTransform,.70f,.73f,.92f,.83f);
            else if(t.name=="Başlık")Place(t.rectTransform,.08f,.85f,.92f,.96f);
        }
        Place((RectTransform)volumeSlider.transform,.08f,.62f,.92f,.72f);
        // Slider collapses the handle anchors to one value: retain a visible hit target.
        if(volumeSlider.handleRect)volumeSlider.handleRect.sizeDelta=new Vector2(20,volumeSlider.handleRect.sizeDelta.y);
        Place((RectTransform)closeButton.transform,.08f,.07f,.47f,.18f);
        var font=percentageLabel.font;
        fullscreenButton=Button("Tam Ekran",card,new Vector2(.08f,.46f),new Vector2(.92f,.57f),"",font);
        fullscreenLabel=Label("Tam Ekran Metni",fullscreenButton.transform,Vector2.zero,Vector2.one,"",font,22);
        resolutionButton=Button("Çözünürlük",card,new Vector2(.08f,.30f),new Vector2(.92f,.41f),"",font);
        resolutionLabel=Label("Çözünürlük Metni",resolutionButton.transform,Vector2.zero,Vector2.one,"",font,22);
        appliedLabel=Label("Ayar Durumu",card,new Vector2(.08f,.19f),new Vector2(.92f,.28f),"Görüntü değişiklikleri UYGULA ile kaydedilir.",font,18);
        applyButton=Button("Uygula",card,new Vector2(.53f,.07f),new Vector2(.92f,.18f),"UYGULA",font);
        foreach(var resolution in Screen.resolutions)
        {
            var size=new Vector2Int(resolution.width,resolution.height);
            if(size.x>=800&&size.y>=600&&!resolutions.Contains(size))resolutions.Add(size);
        }
        if(resolutions.Count==0)resolutions.AddRange(new[]{new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080)});
        var current=new Vector2Int(Screen.width,Screen.height);if(!resolutions.Contains(current))resolutions.Add(current);
        openButton.onClick.AddListener(Open);closeButton.onClick.AddListener(Close);volumeSlider.onValueChanged.AddListener(SetVolume);
        fullscreenButton.onClick.AddListener(()=>{pendingFullscreen=!pendingFullscreen;RefreshDisplay();});
        resolutionButton.onClick.AddListener(()=>{resolutionIndex=(resolutionIndex+1)%resolutions.Count;RefreshDisplay();});
        applyButton.onClick.AddListener(Apply);panel.SetActive(false);
    }
    static void Place(RectTransform r,float x,float y,float xx,float yy){r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(xx,yy);r.offsetMin=r.offsetMax=Vector2.zero;}
    private void OnDestroy()
    {
        openButton.onClick.RemoveListener(Open);closeButton.onClick.RemoveListener(Close);volumeSlider.onValueChanged.RemoveListener(SetVolume);
    }
    private void Open()
    {
        if(SceneFadeTransition.IsTransitioning)return;
        pendingFullscreen=PlayerPrefs.GetInt(DisplayKey+"Fullscreen",Screen.fullScreen?1:0)==1;
        var size=new Vector2Int(PlayerPrefs.GetInt(DisplayKey+"Width",Screen.width),PlayerPrefs.GetInt(DisplayKey+"Height",Screen.height));
        resolutionIndex=resolutions.IndexOf(size);if(resolutionIndex<0)resolutionIndex=resolutions.Count-1;
        RefreshDisplay();panel.SetActive(true);panel.transform.SetAsLastSibling();
    }
    private void Close()=>panel.SetActive(false);
    private void Update(){if(IsOpen&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Close();}
    private void RefreshDisplay()
    {
        fullscreenLabel.text="TAM EKRAN   "+(pendingFullscreen?"AÇIK":"KAPALI");
        resolutionLabel.text="ÇÖZÜNÜRLÜK   "+PendingResolution.x+" × "+PendingResolution.y+"   >";
        appliedLabel.text="Görüntü değişiklikleri UYGULA ile kaydedilir.";appliedLabel.color=White;
    }
    private void Apply()
    {
        var size=PendingResolution;
        Screen.SetResolution(size.x,size.y,pendingFullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
        PlayerPrefs.SetInt(DisplayKey+"Width",size.x);PlayerPrefs.SetInt(DisplayKey+"Height",size.y);
        PlayerPrefs.SetInt(DisplayKey+"Fullscreen",pendingFullscreen?1:0);PlayerPrefs.Save();
        appliedLabel.text="AYARLAR KAYDEDİLDİ";appliedLabel.color=Green;
    }
    private void SetVolume(float value)
    {
        float volume=Mathf.Clamp01(value);AudioListener.volume=volume;PlayerPrefs.SetFloat(PreferenceKey,volume);PlayerPrefs.Save();RefreshLabel(volume);
    }
    private void RefreshLabel(float value)=>percentageLabel.text=$"%{Mathf.RoundToInt(value*100)}";
}
