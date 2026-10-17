using UnityEngine;
using System.Collections;
public class Cizirti : MonoBehaviour
{
    public AudioSource CizirtiSesi;
    public AudioSource TiklamaSesi;
    public AudioClip TelsizAcilisSesi;
    public AudioClip TelsizKapanisSesi;
       public void TelefonAc()
    {
       TiklamaSesi.PlayOneShot(TelsizAcilisSesi);
       
       Invoke("CizirtiSesiCal", 1.0f); 
    }

    void CizirtiSesiCal()
    {
        CizirtiSesi.Play();
        
    }
    public void TelefonKapat()
    {
        CancelInvoke("CizirtiSesiCal");


        CizirtiSesi.Stop();
        TiklamaSesi.PlayOneShot(TelsizKapanisSesi);
    }
}
