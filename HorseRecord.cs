using System.Collections.Generic;

namespace PferdehofGUI;

public enum OwnerCategory { Offline, Breeder, Mine, Other }

public class HorseRecord
{
    public required string FilePath { get; init; }
    public required LingoPropertyList Root { get; init; }

    public string Guid => LingoPathHelper.GetString(Root, "GUID")?.Trim('{', '}') ?? "";
    public string Name => LingoPathHelper.GetString(Root, "name") ?? "(unnamed)";
    public string Sex => LingoPathHelper.GetSymbolName(Root, "sexual.sex") ?? "";
    public string Development => LingoPathHelper.GetSymbolName(Root, "development") ?? "";
    public string PregState => LingoPathHelper.GetSymbolName(Root, "sexual.state") ?? "";
    public string OwnerRaw => LingoPathHelper.GetString(Root, "owner") ?? "";
    public int SexDone => (int)(LingoPathHelper.GetNumber(Root, "Flags.sexDone") ?? 0);

    /// <summary>Raw in-game day-counter value from the file. Sign/offset semantics aren't fully
    /// confirmed (negative appears to mean "born this many days ago"), so this is exposed as-is
    /// rather than converted to a calendar age.</summary>
    public double? Birthday => LingoPathHelper.GetNumber(Root, "birthday");

    /// <summary>In-game money value (sale/worth), from the top-level #salesPrice field.</summary>
    public double? SalesPrice => LingoPathHelper.GetNumber(Root, "salesPrice");

    public string FatherGuid => LingoPathHelper.GetString(Root, "parents.fatherGuid")?.Trim('{', '}') ?? "";
    public string MotherGuid => LingoPathHelper.GetString(Root, "parents.motherGuid")?.Trim('{', '}') ?? "";

    private static readonly string[] MaxValueFields =
        { "health", "power", "condition", "flexibility", "adrenalin", "weight", "trust" };

    public Dictionary<string, double> MaxValues
    {
        get
        {
            var result = new Dictionary<string, double>();
            foreach (var fieldName in MaxValueFields)
            {
                var v = LingoPathHelper.GetNumber(Root, $"maxValues.{fieldName}");
                if (v.HasValue) result[fieldName] = v.Value;
            }
            return result;
        }
    }

    /// <summary>Categorizes ownership relative to the currently selected player's GUID.</summary>
    public OwnerCategory GetOwnerCategory(string selectedPlayerGuid)
    {
        if (OwnerRaw == "offline") return OwnerCategory.Offline;
        if (OwnerRaw == "Breeder") return OwnerCategory.Breeder;
        if (OwnerRaw.Trim('{', '}') == selectedPlayerGuid) return OwnerCategory.Mine;
        return OwnerCategory.Other;
    }

    public bool IsBreedingEligible =>
        Development == "adult" && PregState != "pregnant" && SexDone == 0 && Sex != "gelding";

    /// <summary>"You" if this horse's owner matches the selected player, "Breeder" if breeder-owned,
    /// otherwise the raw owner string (offline / another player's GUID).</summary>
    public string OwnerLabel(string selectedPlayerGuid) => GetOwnerCategory(selectedPlayerGuid) switch
    {
        OwnerCategory.Mine => "You",
        OwnerCategory.Breeder => "Breeder",
        OwnerCategory.Offline => "Offline",
        _ => OwnerRaw
    };

    public override string ToString() => $"{Name} ({Sex}, {Development}) - {OwnerRaw}";
}
