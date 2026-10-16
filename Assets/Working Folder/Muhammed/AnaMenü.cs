using UnityEngine;
using UnityEngine.SceneManagement;
public class AnaMenü : MonoBehaviour
{
    public void OyunBaslat()
    {
        SceneManager.LoadScene("Muhammed Test Place");
    }
    public void CikisYap()
    {
        Application.Quit();
        #if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }

}
