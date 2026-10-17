using UnityEngine;
public class Cizirti : MonoBehaviour
{
    public AudioSource CizirtiSesi;
    public AudioSource TiklamaSesi;
    public AudioClip TelsizAcilisSesi;
    public AudioClip TelsizKapanisSesi;
    private float lastClose=-10;
    public void TelefonAc() => TelefonAc(true);
    public void TelefonAc(bool playConversation)
    {
        CancelInvoke(nameof(CizirtiSesiCal));
        if(TiklamaSesi && TiklamaSesi.isActiveAndEnabled && TelsizAcilisSesi)TiklamaSesi.PlayOneShot(TelsizAcilisSesi);
        if(playConversation)Invoke(nameof(CizirtiSesiCal),.18f);
    }
    public void StartConversation() { CancelInvoke(nameof(CizirtiSesiCal)); CizirtiSesiCal(); }
    private void CizirtiSesiCal() { if(CizirtiSesi && CizirtiSesi.isActiveAndEnabled && CizirtiSesi.clip && !CizirtiSesi.isPlaying)CizirtiSesi.Play(); }
    public void TelefonKapat()
    {
        CancelInvoke(nameof(CizirtiSesiCal));
        if(CizirtiSesi)CizirtiSesi.Stop();
        if(Time.unscaledTime-lastClose<.2f)return;
        lastClose=Time.unscaledTime;
        if(TiklamaSesi && TiklamaSesi.isActiveAndEnabled && TelsizKapanisSesi)TiklamaSesi.PlayOneShot(TelsizKapanisSesi);
    }
}
