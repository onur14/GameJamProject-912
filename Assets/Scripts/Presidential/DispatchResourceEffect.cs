using System;

// A transient view of the existing dispatch rule, not another resource store.
[Serializable]
public struct DispatchResourceEffect
{
    public int ambulances, firefighters, rescue;
    public bool IsZero => ambulances == 0 && firefighters == 0 && rescue == 0;
    public bool CanApply(DispatchResources r) => r != null && r.CanApply(ambulances, firefighters, rescue);
    public bool Apply(DispatchResources r) => r != null && r.TryApply(ambulances, firefighters, rescue);
    public static DispatchResourceEffect ForTeam(DispatchResources.ResourceType team)
    {
        return new DispatchResourceEffect {
            ambulances = team == DispatchResources.ResourceType.Ambulances ? -1 : 0,
            firefighters = team == DispatchResources.ResourceType.Firefighters ? -1 : 0,
            rescue = team == DispatchResources.ResourceType.Police ? -1 : 0
        };
    }
    private static string Signed(int n) => n > 0 ? "+" + n : n.ToString();
    public string Describe(DispatchResources r)
    {
        if (IsZero) return "Ekip sevki yok · Kaynak etkisi: 0";
        var lines = new System.Collections.Generic.List<string>();
        if (ambulances != 0) lines.Add($"Ambulans {Signed(ambulances)}  ({r.Ambulances} → {r.Ambulances + ambulances})");
        if (firefighters != 0) lines.Add($"İtfaiye {Signed(firefighters)}  ({r.Firefighters} → {r.Firefighters + firefighters})");
        if (rescue != 0) lines.Add($"Kurtarma {Signed(rescue)}  ({r.Police} → {r.Police + rescue})");
        if (!CanApply(r)) lines.Add("SEÇİLEMEZ: Yeterli ekip yok.");
        return string.Join("\n", lines);
    }
}
