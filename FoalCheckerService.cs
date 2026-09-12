using System;
using System.Collections.Generic;
using System.Linq;

namespace PferdehofGUI;

public class FoalStatComparison
{
    public required string Stat { get; init; }
    public required double FoalValue { get; init; }
    public required double FatherValue { get; init; }
    public required double MotherValue { get; init; }
    public required string VsParents { get; init; }
    public required string Roll { get; init; }
}

public class FoalReport
{
    public required HorseRecord Foal { get; init; }
    public required HorseRecord Father { get; init; }
    public required HorseRecord Mother { get; init; }
    public required List<FoalStatComparison> Stats { get; init; }
}

public static class FoalCheckerService
{
    private static readonly string[] Fields = { "health", "power", "condition", "flexibility", "adrenalin", "weight" };

    public static List<HorseRecord> GetOwnedFoals(List<HorseRecord> allHorses, string selectedPlayerGuid) =>
        allHorses.Where(h => h.GetOwnerCategory(selectedPlayerGuid) == OwnerCategory.Mine
                              && h.Development == "foal").ToList();

    public static FoalReport? BuildReport(HorseRecord foal, List<HorseRecord> allHorses)
    {
        var byGuid = allHorses.ToDictionary(h => h.Guid, h => h);
        if (!byGuid.TryGetValue(foal.FatherGuid, out var father)) return null;
        if (!byGuid.TryGetValue(foal.MotherGuid, out var mother)) return null;

        bool foalIsFemale = foal.Sex == "mare";
        var stats = new List<FoalStatComparison>();

        foreach (var field in Fields)
        {
            if (!foal.MaxValues.TryGetValue(field, out var foalVal)) continue;
            if (!father.MaxValues.TryGetValue(field, out var fatherVal)) continue;
            if (!mother.MaxValues.TryGetValue(field, out var motherVal)) continue;

            var (lo, hi) = RangeForSex(fatherVal, motherVal, foalIsFemale);
            stats.Add(new FoalStatComparison
            {
                Stat = field,
                FoalValue = foalVal,
                FatherValue = fatherVal,
                MotherValue = motherVal,
                VsParents = CompareToParents(foalVal, fatherVal, motherVal),
                Roll = ClassifyRoll(foalVal, lo, hi)
            });
        }

        return new FoalReport { Foal = foal, Father = father, Mother = mother, Stats = stats };
    }

    private static (double Lo, double Hi) RangeForSex(double fatherVal, double motherVal, bool foalIsFemale)
    {
        double wLo = foalIsFemale ? 50.0 : 0.0;
        double wHi = foalIsFemale ? 100.0 : 50.0;
        double avgLo = fatherVal * (100 - wLo) / 100 + motherVal * wLo / 100;
        double avgHi = fatherVal * (100 - wHi) / 100 + motherVal * wHi / 100;
        double lo = Math.Min(avgLo, avgHi) - 1;
        double hi = Math.Min(Math.Max(avgLo, avgHi) + 4, 100);
        return (lo, hi);
    }

    private static string ClassifyRoll(double value, double lo, double hi)
    {
        if (hi <= lo) return "n/a";
        if (value < lo - 0.01) return "below possible range";
        if (value > hi + 0.01) return "above possible range";
        double fraction = (value - lo) / (hi - lo);
        if (fraction < 1.0 / 3) return "low roll";
        if (fraction > 2.0 / 3) return "high roll";
        return "middling roll";
    }

    private static string CompareToParents(double foalVal, double fatherVal, double motherVal)
    {
        double lower = Math.Min(fatherVal, motherVal);
        double upper = Math.Max(fatherVal, motherVal);
        if (foalVal > upper) return $"exceeds both parents ({fatherVal:0.#} / {motherVal:0.#})";
        if (foalVal < lower) return $"below both parents ({fatherVal:0.#} / {motherVal:0.#})";
        return $"between parents ({fatherVal:0.#} / {motherVal:0.#})";
    }
}
