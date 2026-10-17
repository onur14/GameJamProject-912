using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class AnaMenü : MonoBehaviour
{
    [Header("Entering the shift")]
    [SerializeField] private string gameplayScene = "SampleScene";
    [SerializeField, Min(0f)] private float menuFadeDuration = 0.45f;
    [SerializeField, Min(0f)] private float gameplayFadeInDuration = 0.65f;
    [SerializeField] private TMP_FontAsset openingNoticeFont;
    [SerializeField] private AudioSource menuAudio;
    [SerializeField] private AudioClip buttonSound;
    private bool starting;

    public void OyunBaslat()
    {
        if(starting || SceneFadeTransition.IsTransitioning)return;
        if(menuAudio && buttonSound)menuAudio.PlayOneShot(buttonSound,.6f);
        StartCoroutine(BeginAfterNotice());
    }
    private IEnumerator BeginAfterNotice()
    {
        starting=true;
        var canvas=GetComponentInParent<Canvas>();
        if(canvas)yield return DispatchOpeningNotice.Show(canvas,openingNoticeFont);
        PreparationRun.StartNewGame(gameplayScene, menuFadeDuration, gameplayFadeInDuration);
        starting=false;
    }
    public void CikisYap()
    {
        Application.Quit();
        #if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }

}
