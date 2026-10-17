using UnityEngine;
using UnityEngine.SceneManagement;
public class AnaMenü : MonoBehaviour
{
    [Header("Entering the shift")]
    [SerializeField] private string gameplayScene = "SampleScene";
    [SerializeField, Min(0f)] private float menuFadeDuration = 0.45f;
    [SerializeField, Min(0f)] private float gameplayFadeInDuration = 0.65f;

    public void OyunBaslat()
    {
        SceneFadeTransition.LoadScene(gameplayScene, menuFadeDuration, gameplayFadeInDuration);
    }
    public void CikisYap()
    {
        Application.Quit();
        #if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }

}
