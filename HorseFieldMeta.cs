using System.Collections.Generic;
using System.Linq;

namespace PferdehofGUI;

public enum FieldKind
{
    Text,       // free text, no validation
    ReadOnly,   // e.g. GUID
    Enum,       // dropdown of symbols
    Number,     // numeric with optional min/max
    GenePair,   // 2-char dominant/recessive pair, e.g. "Aa"
    Color       // rgb(r, g, b) with each channel validated 0-255
}

public class FieldMeta
{
    public FieldKind Kind = FieldKind.Text;
    public List<string> Options = new();   // for Enum
    public double? Min;                    // for Number
    public double? Max;                    // for Number
    public char? GeneLetter;               // for GenePair (lowercase form, e.g. 'a')
}

public static class HorseFieldMeta
{
    // Equip slots: #none plus known purchasable item ids (from Items2Buy tables in breeder.dir/dealer.dir)
    private static readonly List<string> SaddleOptions = new() { "none", "saddle1", "saddle2", "saddle3", "saddle4" };
    private static readonly List<string> HalterOptions = new() { "none", "halter1", "halter2", "halter3" };
    private static readonly List<string> BlanketOptions = new() { "none", "blanket1", "blanket2", "blanket3" };
    private static readonly List<string> PadOptions = new() { "none", "pad1", "pad2", "pad3" };
    private static readonly List<string> SingleItemOptions = new() { "none", "item" }; // boots/harness/whip: only one known variant

    private static readonly List<string> SexOptions = new() { "stallion", "mare", "gelding" };
    private static readonly List<string> DevelopmentOptions = new() { "foal", "youth", "adult" };
    private static readonly List<string> PregnancyOptions = new() { "unpregnant", "pregnant" };
    private static readonly List<string> RosseStateOptions = new() { "none" }; // only #none confirmed in scripts
    private static readonly List<string> ColorOptions = new()
    {
        "Dunkelfuchs", "Fuchs", "Brauner", "Schwarzbrauner", "Schimmel", "Rappe", "Falbe", "palomino"
    };

    // Stats that appear both as a top-level current value (0-100) and under maxValues.* (also 0-100) -
    // confirmed via clamp checks in the decompiled scripts (e.g. "if ...supply.food > 100").
    private static readonly HashSet<string> PercentStatFields = new()
        { "health", "trust", "power", "condition", "flexibility", "adrenalin" };

    /// <summary>
    /// Looks up validation rules for a dotted field path, e.g. "genetics.basicColor", "supply.food".
    /// Falls back to free-text Text if nothing specific is known about the field.
    /// </summary>
    public static FieldMeta Resolve(string path)
    {
        // GUID is system-managed, never user-editable
        if (path == "GUID") return new FieldMeta { Kind = FieldKind.ReadOnly };

        switch (path)
        {
            case "sexual.sex":
                return Enum(SexOptions);
            case "sexual.state":
                return Enum(PregnancyOptions);
            case "sexual.rosse.state":
                return Enum(RosseStateOptions);
            case "development":
                return Enum(DevelopmentOptions);
            case "genetics.color":
                return Enum(ColorOptions);

            case "genetics.basicColor": return Gene('a');
            case "genetics.blackFactor": return Gene('b');
            case "genetics.blackDistribution": return Gene('c');
            case "genetics.whiteColor": return Gene('d');
            case "genetics.pinto": return Gene('e');
            case "genetics.tiger": return Gene('f');

            // rgb(r, g, b) fields - each channel must be a valid 8-bit color component (0-255).
            case "genetics.skincolor":
            case "genetics.haircolor":
                return new FieldMeta { Kind = FieldKind.Color };

            case "equip.saddle": return Enum(SaddleOptions);
            case "equip.halter": return Enum(HalterOptions);
            case "equip.blanket": return Enum(BlanketOptions);
            case "equip.pad": return Enum(PadOptions);
            case "equip.boots":
            case "equip.harness":
            case "equip.whip":
                return Enum(SingleItemOptions);

            // Money can't go negative; no evidence of an upper cap.
            case "salesPrice":
                return Number(0, null);

            // Day-counter fields - always >= 0 in every real file seen (never negative like #birthday can be).
            case "lifeTime":
            case "Flags.sexDone":
                return Number(0, null);

            // Observed as small non-negative integers (always 0 in every sample) with no known upper
            // bound - likely indices into a small option list, but that list isn't confirmed anywhere.
            case "genetics.eyes":
            case "genetics.ears":
                return Number(0, null);

            // Observed as 1.2000 in every real file seen so far - consistent enough to know it's a
            // plain non-negative number, but not enough variation to infer a real upper bound.
            case "genetics.PatternFactor":
                return Number(0, null);

            // Birthday is a signed day-offset (negative = born that many days ago) - no valid bound
            // to enforce beyond "must be a number".
            case "birthday":
                return Number(null, null);

            // Weight varies wildly in scale between real files (0.3 vs 800), so no usable numeric
            // range can be inferred - just enforce it can't be negative.
            case "weight":
            case "maxValues.weight":
                return Number(0, null);
        }

        // Supply stats: 0-100 (confirmed via clamp checks like "if ...supply.food > 100")
        if (path.StartsWith("supply."))
        {
            return path switch
            {
                "supply.food" or "supply.water" or "supply.skin" or "supply.mane" or
                "supply.hoove" or "supply.move" or "supply.worms" or "supply.rabies" or
                "supply.tetanus" or "supply.cough" or "supply.influenza" => Number(0, 100),
                _ => Number(null, null)
            };
        }

        // Sickness state/probability: 0-100. Delta can swing either way, left unbounded.
        if (path.StartsWith("sickness.") && (path.EndsWith(".state") || path.EndsWith(".probability")))
            return Number(0, 100);

        // Core stats: 0-100, both as the current top-level value and under maxValues.* (the
        // genetic ceiling for the same stat) - same clamp logic applies to both in the scripts.
        string leafName = path.Contains('.') ? path[(path.LastIndexOf('.') + 1)..] : path;
        bool isMaxValuesField = path.StartsWith("maxValues.");
        if ((path == leafName || isMaxValuesField) && PercentStatFields.Contains(leafName))
            return Number(0, 100);

        return new FieldMeta { Kind = FieldKind.Text };
    }

    private static FieldMeta Enum(List<string> options) =>
        new() { Kind = FieldKind.Enum, Options = options.ToList() };

    private static FieldMeta Number(double? min, double? max) =>
        new() { Kind = FieldKind.Number, Min = min, Max = max };

    private static FieldMeta Gene(char letter) =>
        new() { Kind = FieldKind.GenePair, GeneLetter = letter };
}
