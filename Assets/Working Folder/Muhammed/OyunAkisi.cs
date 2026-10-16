using UnityEngine;
using TMPro;
public class OyunAkisi : MonoBehaviour
{
    public TextMeshProUGUI GelenCevap;
    public GameObject Kabuletti;
    public GameObject RedYedi;
    public void KabulEdildi()
    {
        GelenCevap.text = "Arayan: Teşekkürler, yardımınız için minnettarım!";
    }

    // 
    public void Reddedildi()
    {
        GelenCevap.text = "Arayan: Anladım, belki başka zaman yardım edebilirsin.";
    }
    void ButonlariGizle()
    {
        if (Kabuletti != null) 
        {
            Kabuletti.SetActive(false);
        }
        if (RedYedi != null) 
        {
            RedYedi.SetActive(false);
        }
    }
}
