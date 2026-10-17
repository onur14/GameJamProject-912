using UnityEngine;

public class SesYoneticisi : MonoBehaviour
{
   private static SesYoneticisi instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            
        }
        else
        {
            Destroy(gameObject);
        }
    }

}
