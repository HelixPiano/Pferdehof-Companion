namespace PferdehofGUI;

/// <summary>One of your own horses shown in the Breeder Support overview list,
/// with the sum of whichever stats are currently checked.</summary>
public class OwnHorseScoreRow
{
    public required string Name { get; init; }
    public required string Sex { get; init; }
    public string Health { get; init; } = "";
    public string Power { get; init; } = "";
    public string Condition { get; init; } = "";
    public string Flexibility { get; init; } = "";
    public string Adrenalin { get; init; } = "";
    public string Weight { get; init; } = "";
    public string Score { get; init; } = "";
}
