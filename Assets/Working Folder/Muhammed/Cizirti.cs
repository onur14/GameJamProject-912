using UnityEngine;
public class Cizirti : MonoBehaviour
{
    public AudioSource CizirtiSesi;
    public AudioSource TiklamaSesi;
    public AudioClip TelsizAcilisSesi;
    public AudioClip TelsizKapanisSesi;
    private float lastClose=-10;
    public void TelefonAc()
    {
        CancelInvoke(nameof(CizirtiSesiCal));
        if(TiklamaSesi && TelsizAcilisSesi)TiklamaSesi.PlayOneShot(TelsizAcilisSesi);
        Invoke(nameof(CizirtiSesiCal),.18f);
    }
    private void CizirtiSesiCal() { if(CizirtiSesi && !CizirtiSesi.isPlaying)CizirtiSesi.Play(); }
    public void TelefonKapat()
    {
        CancelInvoke(nameof(CizirtiSesiCal));
        if(CizirtiSesi)CizirtiSesi.Stop();
        if(Time.unscaledTime-lastClose<.2f)return;
        lastClose=Time.unscaledTime;
        if(TiklamaSesi && TelsizKapanisSesi)TiklamaSesi.PlayOneShot(TelsizKapanisSesi);
    }
}
