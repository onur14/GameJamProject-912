using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shares existing short effects across menus and runtime-created UI.
public sealed class GameAudioFeedback : MonoBehaviour
{
    private static GameAudioFeedback instance;
    private readonly HashSet<Button> buttons=new HashSet<Button>();
    private AudioSource source;
    private DispatchResources resources;
    private Vector3Int previousStock;
    private DispatchSoundLibrary library;
    public int ClickCount { get; private set; }
    public int CorrectCount { get; private set; }
    public int IncorrectCount { get; private set; }
    public int ResourceCount { get; private set; }
    public int TransitionCount { get; private set; }
    public static GameAudioFeedback Instance=>instance;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic()=>instance=null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        var host=new GameObject("Arayüz Ses Geri Bildirimi");instance=host.AddComponent<GameAudioFeedback>();DontDestroyOnLoad(host);
        instance.library=Resources.Load<DispatchSoundLibrary>("DispatchSoundLibrary");
        instance.source=host.AddComponent<AudioSource>();instance.source.playOnAwake=false;instance.source.spatialBlend=0;instance.source.volume=.3f;
        SceneManager.sceneLoaded+=instance.OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        foreach(var button in FindObjectsByType<Button>(FindObjectsInactive.Include))Bind(button);
        StartCoroutine(BindScene());
    }
    IEnumerator BindScene()
    {
        yield return null;yield return null;
        buttons.RemoveWhere(b=>!b);
        foreach(var button in FindObjectsByType<Button>(FindObjectsInactive.Include))Bind(button);
        if(resources)resources.Changed-=ResourceChanged;
        resources=FindAnyObjectByType<DispatchResources>();
        if(resources){previousStock=Stock();resources.Changed+=ResourceChanged;}
    }
    Vector3Int Stock()=>new Vector3Int(resources.Ambulances,resources.Firefighters,resources.RescueTeams);
    void ResourceChanged()
    {
        var stock=Stock();if(stock==previousStock)return;previousStock=stock;
        if(library&&library.resourceChange){source.PlayOneShot(library.resourceChange,.45f);ResourceCount++;}
    }
    public static void Bind(Button button)
    {
        if(!instance||!button||!instance.buttons.Add(button))return;
        button.onClick.AddListener(Click);
        if(button.GetComponentInChildren<TMP_Text>(true) && !button.GetComponent<ButtonHoverFeedback>())
            button.gameObject.AddComponent<ButtonHoverFeedback>();
    }
    static void Click()
    {
        if(instance&&instance.library&&instance.library.buttonClick){instance.source.PlayOneShot(instance.library.buttonClick,.5f);instance.ClickCount++;}
    }
    public static void Feedback(bool correct)
    {
        if(!instance||!instance.library)return;
        var clip=correct?instance.library.correct:instance.library.incorrect;
        if(!clip)return;instance.source.PlayOneShot(clip,.65f);
        if(correct)instance.CorrectCount++;else instance.IncorrectCount++;
    }
    public static void Transition()
    {
        if(instance&&instance.library&&instance.library.transition){instance.source.PlayOneShot(instance.library.transition,.3f);instance.TransitionCount++;}
    }
    void OnDestroy(){SceneManager.sceneLoaded-=OnSceneLoaded;if(resources)resources.Changed-=ResourceChanged;if(instance==this)instance=null;}
}

public sealed class ButtonHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Button button;
    private Outline outline;
    private Vector3 normalScale;
    private bool hovered;

    private void Awake()
    {
        button=GetComponent<Button>();
        normalScale=transform.localScale;
        outline=gameObject.AddComponent<Outline>();
        outline.effectDistance=new Vector2(2,-2);
        outline.useGraphicAlpha=false;
        outline.effectColor=new Color(.75f,.91f,.90f,0);
    }
    public void OnPointerEnter(PointerEventData eventData){hovered=true;}
    public void OnPointerExit(PointerEventData eventData){hovered=false;}
    private void Update()
    {
        float strength=hovered && button && button.interactable ? 1f : 0f;
        float blend=1f-Mathf.Exp(-12f*Time.unscaledDeltaTime);
        transform.localScale=Vector3.Lerp(transform.localScale,normalScale*(1f+.025f*strength),blend);
        var color=outline.effectColor;
        color.a=Mathf.Lerp(color.a,.82f*strength,blend);
        outline.effectColor=color;
    }
    private void OnDisable()
    {
        hovered=false;
        transform.localScale=normalScale;
        if(outline)outline.effectColor=new Color(.75f,.91f,.90f,0);
    }
}
