// PlayerRecord.cs
namespace PferdehofGUI;

public class PlayerRecord
{
    public required string FilePath { get; init; }
    public required string Guid { get; init; }
    public required string Name { get; init; }

    public override string ToString() => Name;
}