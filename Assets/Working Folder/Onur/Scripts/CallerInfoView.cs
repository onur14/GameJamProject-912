using TMPro;
using UnityEngine;

public sealed class CallerInfoView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text locationLabel;
    [SerializeField] private TMP_Text emergencyLabel;
    [SerializeField] private TMP_Text summaryLabel;

    public void Show(DispatchScenarioData scenario)
    {
        if (scenario == null) { Clear(); return; }
        Set(nameLabel, scenario.callerName, "Arayan: ");
        Set(locationLabel, scenario.callerLocation, "Konum: ");
        Set(emergencyLabel, scenario.emergencyType, "Olay: ");
        Set(summaryLabel, scenario.situationSummary, "Durum: ");
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        gameObject.SetActive(false);
    }

    private static void Set(TMP_Text label, string value, string prefix)
    {
        if (label == null) return;
        label.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
        label.text = string.IsNullOrWhiteSpace(value) ? string.Empty : prefix + value;
    }
}
