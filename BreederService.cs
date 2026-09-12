using System;
using System.Collections.Generic;
using System.Linq;

namespace PferdehofGUI;

public enum BreedingMode
{
    /// <summary>Any horse that's either mine or the breeder's - no other restriction
    /// beyond "not offline" and the usual eligibility checks.</summary>
    AnyEligible,
    OwnMareBreederStallion,
    OwnOnly
}

public class BreedingCombination
{
    public required HorseRecord Stallion { get; init; }
    public required HorseRecord Mare { get; init; }
    public required double TotalScore { get; init; }
    public required Dictionary<string, (double Lo, double Hi)> ExpectedFoalRanges { get; init; }
}

public static class BreederService
{
    /// <summary>All stat fields available for display in the expected-foal-range output,
    /// regardless of which ones the user chose to score on.</summary>
    public static readonly string[] AllStatFields = { "health", "power", "condition", "flexibility", "adrenalin", "weight" };

    public static BreedingCombination? FindBest(
        List<HorseRecord> allHorses,
        string selectedPlayerGuid,
        BreedingMode mode,
        IReadOnlyCollection<string> scoreFields,
        bool ignoreEligibilityChecks = false)
    {
        if (scoreFields.Count == 0) return null; // caller must guarantee at least one is selected

        // "Discard offline" applies to every mode
        var candidates = allHorses.Where(h => h.GetOwnerCategory(selectedPlayerGuid) != OwnerCategory.Offline).ToList();

        List<HorseRecord> stallionPool, marePool;

        switch (mode)
        {
            case BreedingMode.AnyEligible:
                stallionPool = candidates.Where(h =>
                    (h.OwnerRaw == "Breeder" || h.GetOwnerCategory(selectedPlayerGuid) == OwnerCategory.Mine)
                    && h.Sex == "stallion").ToList();
                marePool = candidates.Where(h =>
                    (h.OwnerRaw == "Breeder" || h.GetOwnerCategory(selectedPlayerGuid) == OwnerCategory.Mine)
                    && h.Sex == "mare").ToList();
                break;
            case BreedingMode.OwnMareBreederStallion:
                stallionPool = candidates.Where(h => h.OwnerRaw == "Breeder" && h.Sex == "stallion").ToList();
                marePool = candidates.Where(h => h.GetOwnerCategory(selectedPlayerGuid) == OwnerCategory.Mine && h.Sex == "mare").ToList();
                break;
            case BreedingMode.OwnOnly:
                stallionPool = candidates.Where(h => h.GetOwnerCategory(selectedPlayerGuid) == OwnerCategory.Mine && h.Sex == "stallion").ToList();
                marePool = candidates.Where(h => h.GetOwnerCategory(selectedPlayerGuid) == OwnerCategory.Mine && h.Sex == "mare").ToList();
                break;
            default:
                return null;
        }

        // Gelding is excluded either way - it's not a biological possibility, not a game-state check.
        // The other checks (adult, unpregnant, off cooldown) are skipped entirely when the caller
        // asks to ignore them, rather than loosened individually.
        if (ignoreEligibilityChecks)
        {
            stallionPool = stallionPool.Where(h => h.Sex != "gelding").ToList();
            marePool = marePool.Where(h => h.Sex != "gelding").ToList();
        }
        else
        {
            stallionPool = stallionPool.Where(h => h.IsBreedingEligible).ToList();
            marePool = marePool.Where(h => h.IsBreedingEligible).ToList();
        }

        if (stallionPool.Count == 0 || marePool.Count == 0) return null;

        // Tie-break: prefer a horse I own over a breeder's horse when scores are equal.
        var bestStallion = stallionPool
            .OrderByDescending(h => Score(h, scoreFields))
            .ThenByDescending(h => OwnershipPriority(h, selectedPlayerGuid))
            .First();
        var bestMare = marePool
            .OrderByDescending(h => Score(h, scoreFields))
            .ThenByDescending(h => OwnershipPriority(h, selectedPlayerGuid))
            .First();

        var ranges = new Dictionary<string, (double, double)>();
        foreach (var field in AllStatFields)
        {
            if (bestStallion.MaxValues.TryGetValue(field, out var fVal) &&
                bestMare.MaxValues.TryGetValue(field, out var mVal))
            {
                ranges[field] = ExpectedRange(fVal, mVal);
            }
        }

        return new BreedingCombination
        {
            Stallion = bestStallion,
            Mare = bestMare,
            TotalScore = Score(bestStallion, scoreFields) + Score(bestMare, scoreFields),
            ExpectedFoalRanges = ranges
        };
    }

    private static double Score(HorseRecord h, IReadOnlyCollection<string> fields) =>
        fields.Sum(f => h.MaxValues.TryGetValue(f, out var v) ? v : 0);

    /// <summary>Higher = preferred as a tie-break. Horses I own outrank the breeder's.</summary>
    private static int OwnershipPriority(HorseRecord h, string selectedPlayerGuid) =>
        h.GetOwnerCategory(selectedPlayerGuid) == OwnerCategory.Mine ? 1 : 0;

    /// <summary>Combined range across both possible foal sexes (colt leans sire, filly leans dam),
    /// same weighting/formula as give_breed_result() reproduced in the Python script.</summary>
    private static (double Lo, double Hi) ExpectedRange(double fatherVal, double motherVal)
    {
        var colt = RangeForSex(fatherVal, motherVal, foalIsFemale: false);
        var filly = RangeForSex(fatherVal, motherVal, foalIsFemale: true);
        return (Math.Min(colt.Lo, filly.Lo), Math.Max(colt.Hi, filly.Hi));
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
}