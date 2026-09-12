namespace PferdehofGUI;

/// <summary>Flat, display-ready snapshot of one owned horse for the overview grid.</summary>
public class HorseOverviewRow
{
    public required string Name { get; init; }
    public required string Sex { get; init; }
    public required string Development { get; init; }
    public required string Pregnancy { get; init; }
    public string SalesPrice { get; init; } = "";
    public string Health { get; init; } = "";
    public string Power { get; init; } = "";
    public string Condition { get; init; } = "";
    public string Flexibility { get; init; } = "";
    public string Adrenalin { get; init; } = "";
    public string Weight { get; init; } = "";

    public static HorseOverviewRow From(HorseRecord h)
    {
        var mv = h.MaxValues;
        string Fmt(string key) => mv.TryGetValue(key, out var v) ? v.ToString("0.#") : "-";

        return new HorseOverviewRow
        {
            Name = h.Name,
            Sex = h.Sex,
            Development = h.Development,
            Pregnancy = h.PregState,
            SalesPrice = h.SalesPrice?.ToString("0") ?? "-",
            Health = Fmt("health"),
            Power = Fmt("power"),
            Condition = Fmt("condition"),
            Flexibility = Fmt("flexibility"),
            Adrenalin = Fmt("adrenalin"),
            Weight = Fmt("weight"),
        };
    }
}
