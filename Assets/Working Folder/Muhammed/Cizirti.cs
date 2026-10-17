using UnityEngine;

public class Cizirti : MonoBehaviour
{
    public AudioSource CizirtiSesi;
    public AudioSource TiklamaSesi;
    public AudioClip TelsizAcilisSesi;
    public AudioClip TelsizKapanisSesi;
       public void TelefonAc()
    {
       TiklamaSesi.PlayOneShot(TelsizAcilisSesi);
       CizirtiSesi.Play();
    }

    // Update is called once per frame
    public void TelefonKapat()
    {
        CizirtiSesi.Stop();
        TiklamaSesi.PlayOneShot(TelsizKapanisSesi);
    }
}
