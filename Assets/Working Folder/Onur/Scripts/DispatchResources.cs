using TMPro;
using UnityEngine;

public sealed class DispatchResources : MonoBehaviour
{
    public enum ResourceType
    {
        Firefighters,
        Ambulances,
        Police
    }

    [SerializeField, Min(0)] private int initialFirefighters = 10;
    [SerializeField, Min(0)] private int initialAmbulances = 10;
    [SerializeField, Min(0)] private int initialPolice = 10;
    [SerializeField] private TMP_Text firefightersText;
    [SerializeField] private TMP_Text ambulancesText;
    [SerializeField] private TMP_Text policeText;

    public int Firefighters { get; private set; }
    public int Ambulances { get; private set; }
    public int Police { get; private set; }

    private void Awake()
    {
        Firefighters = initialFirefighters;
        Ambulances = initialAmbulances;
        Police = initialPolice;
        Refresh();
    }

    public void Spend(ResourceType resource)
    {
        switch (resource)
        {
            case ResourceType.Firefighters:
                Firefighters = Mathf.Max(0, Firefighters - 1);
                break;
            case ResourceType.Ambulances:
                Ambulances = Mathf.Max(0, Ambulances - 1);
                break;
            case ResourceType.Police:
                Police = Mathf.Max(0, Police - 1);
                break;
        }

        Refresh();
    }

    private void Refresh()
    {
        if (firefightersText != null) firefightersText.text = Firefighters.ToString();
        if (ambulancesText != null) ambulancesText.text = Ambulances.ToString();
        if (policeText != null) policeText.text = Police.ToString();
    }
}
