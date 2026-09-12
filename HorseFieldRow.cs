using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PferdehofGUI;

public class HorseFieldRow : INotifyPropertyChanged
{
    public string Path { get; }
    private readonly LingoValue _target;

    public FieldMeta Meta { get; }
    public bool IsEditable => Meta.Kind != FieldKind.ReadOnly;
    public bool IsDropdown => Meta.Kind == FieldKind.Enum;
    public List<string> Options => Meta.Options;

    public string? Error { get; private set; }

    public HorseFieldRow(string path, LingoValue target)
    {
        Path = path;
        _target = target;
        Meta = HorseFieldMeta.Resolve(path);

        // Anything that isn't a simple scalar (string/symbol/number/color) - e.g. a linear
        // list, a date()/float() call, or a bare keyword - can't be safely round-tripped
        // through a plain text box, so force it to read-only regardless of path-based rules.
        bool isSimpleScalar = target is LingoString or LingoSymbol or LingoNumber or LingoColor;
        if (!isSimpleScalar)
            Meta = new FieldMeta { Kind = FieldKind.ReadOnly };
    }

    public string Value
    {
        get => ToEditString(_target);
        set
        {
            if (!IsEditable) return;

            string candidate = value;
            if (Meta.Kind == FieldKind.Enum && !Meta.Options.Contains(StripSymbol(candidate)))
            {
                Error = $"Must be one of: {string.Join(", ", Meta.Options)}";
                OnPropertyChanged(nameof(Error));
                return; // reject the edit, keep old value
            }

            if (Meta.Kind == FieldKind.Number)
            {
                if (!double.TryParse(candidate, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                {
                    Error = "Must be a number";
                    OnPropertyChanged(nameof(Error));
                    return;
                }
                if (Meta.Min.HasValue && num < Meta.Min.Value) num = Meta.Min.Value;
                if (Meta.Max.HasValue && num > Meta.Max.Value) num = Meta.Max.Value;
                candidate = num.ToString(CultureInfo.InvariantCulture);
            }

            if (Meta.Kind == FieldKind.GenePair)
            {
                char letter = Meta.GeneLetter!.Value;
                if (!IsValidGenePair(candidate, letter))
                {
                    Error = $"Must be two letters: '{char.ToUpper(letter)}' or '{letter}' (e.g. \"{letter}{letter}\", \"{char.ToUpper(letter)}{letter}\")";
                    OnPropertyChanged(nameof(Error));
                    return;
                }
            }

            if (Meta.Kind == FieldKind.Color)
            {
                var m = Regex.Match(candidate, @"rgb\s*\(\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*\)");
                if (!m.Success)
                {
                    Error = "Must be rgb(r, g, b) with each value 0-255";
                    OnPropertyChanged(nameof(Error));
                    return;
                }
                int r = int.Parse(m.Groups[1].Value);
                int g = int.Parse(m.Groups[2].Value);
                int b = int.Parse(m.Groups[3].Value);
                if (r is < 0 or > 255 || g is < 0 or > 255 || b is < 0 or > 255)
                {
                    Error = "Each rgb value must be between 0 and 255";
                    OnPropertyChanged(nameof(Error));
                    return;
                }
            }

            Error = null;
            ApplyEditString(_target, candidate);
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(Error));
        }
    }

    private static bool IsValidGenePair(string text, char letter)
    {
        if (text.Length != 2) return false;
        char up = char.ToUpperInvariant(letter);
        char lo = char.ToLowerInvariant(letter);
        foreach (char c in text)
            if (c != up && c != lo) return false;
        return true;
    }

    private static string StripSymbol(string s) => s.TrimStart('#');

    private static string ToEditString(LingoValue v) => v switch
    {
        LingoString s => s.Value,
        LingoSymbol sym => sym.Name,
        LingoNumber n => n.IsInteger ? ((long)n.Value).ToString() : n.Value.ToString("0.0000", CultureInfo.InvariantCulture),
        LingoColor c => $"rgb({c.R}, {c.G}, {c.B})",
        _ => LingoSerializer.Serialize(v) // LingoLinearList / LingoCall / LingoKeyword: display-only, read-only
    };

    private static void ApplyEditString(LingoValue target, string text)
    {
        switch (target)
        {
            case LingoString s:
                s.Value = text;
                break;
            case LingoSymbol sym:
                sym.Name = StripSymbol(text);
                break;
            case LingoNumber n:
                if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                {
                    n.Value = num;
                    n.IsInteger = !text.Contains('.');
                }
                break;
            case LingoColor c:
                var m = Regex.Match(text, @"rgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)");
                if (m.Success)
                {
                    c.R = int.Parse(m.Groups[1].Value);
                    c.G = int.Parse(m.Groups[2].Value);
                    c.B = int.Parse(m.Groups[3].Value);
                }
                break;
        }
    }

    public static void Flatten(LingoPropertyList list, string prefix, List<HorseFieldRow> rows)
    {
        foreach (var entry in list.Entries)
        {
            string path = string.IsNullOrEmpty(prefix) ? entry.Key : $"{prefix}.{entry.Key}";
            if (entry.Value is LingoPropertyList nested)
                Flatten(nested, path, rows);
            else
                rows.Add(new HorseFieldRow(path, entry.Value));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
