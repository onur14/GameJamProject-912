using TMPro;
using UnityEngine;

public sealed class DispatchResources : MonoBehaviour
{
    public enum ResourceType
    {
        Firefighters,
        Ambulances,
        Police,
        RescueTeams = Police
    }

    [SerializeField, Min(0)] private int initialFirefighters = 5;
    [SerializeField, Min(0)] private int initialAmbulances = 5;
    [SerializeField, Min(0)] private int initialPolice = 6;
    [SerializeField] private TMP_Text firefightersText;
    [SerializeField] private TMP_Text ambulancesText;
    [SerializeField] private TMP_Text policeText;

    public int Firefighters { get; private set; }
    public int Ambulances { get; private set; }
    public int Police { get; private set; }
    public int RescueTeams => Police;
    public event System.Action Changed;

    // Validate the complete allocation before committing any counter.
    public bool CanApply(int ambulances, int firefighters, int police)
    {
        return (long)Ambulances + ambulances >= 0 && (long)Ambulances + ambulances <= int.MaxValue
            && (long)Firefighters + firefighters >= 0 && (long)Firefighters + firefighters <= int.MaxValue
            && (long)Police + police >= 0 && (long)Police + police <= int.MaxValue;
    }

    public bool TryApply(int ambulances, int firefighters, int police)
    {
        if (!CanApply(ambulances, firefighters, police)) return false;
        Ambulances += ambulances;
        Firefighters += firefighters;
        Police += police;
        Refresh();
        return true;
    }

    public bool TrySpend(ResourceType resource)
    {
        return TryApply(resource == ResourceType.Ambulances ? -1 : 0,
            resource == ResourceType.Firefighters ? -1 : 0, resource == ResourceType.Police ? -1 : 0);
    }

    public void ResetForNewShift()
    {
        Firefighters = Mathf.Max(0, initialFirefighters);
        Ambulances = Mathf.Max(0, initialAmbulances);
        Police = Mathf.Max(0, initialPolice);
        Refresh();
    }

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
        Changed?.Invoke();
    }
}
