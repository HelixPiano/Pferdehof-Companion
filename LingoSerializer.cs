using System.Globalization;
using System.Text;

namespace PferdehofGUI;

/// <summary>Writes a compact single line, matching the exact format every real save file uses
/// (no newlines, no indentation) - "#key: value, #key2: value2" with a comma+space separator,
/// nested lists inline the same way. Real Pferdehof .dat files are never pretty-printed, so this
/// deliberately never introduces line breaks or indentation of its own.</summary>
public static class LingoSerializer
{
    public static string Serialize(LingoValue value)
    {
        var sb = new StringBuilder();
        Write(value, sb);
        return sb.ToString();
    }

    private static void Write(LingoValue value, StringBuilder sb)
    {
        switch (value)
        {
            case LingoPropertyList list: WriteList(list, sb); break;
            case LingoLinearList linear: WriteLinearList(linear, sb); break;
            case LingoString s: sb.Append('"').Append(s.Value).Append('"'); break;
            case LingoSymbol sym: sb.Append('#').Append(sym.Name); break;
            case LingoNumber num: sb.Append(FormatNumber(num)); break;
            case LingoColor col: sb.Append($"rgb( {col.R}, {col.G}, {col.B} )"); break;
            case LingoKeyword kw: sb.Append(kw.IsAngleBracketed ? $"<{kw.Text}>" : kw.Text); break;
            case LingoCall call: WriteCall(call, sb); break;
        }
    }

    private static string FormatNumber(LingoNumber num) =>
        num.IsInteger
            ? ((long)num.Value).ToString(CultureInfo.InvariantCulture)
            : num.Value.ToString("0.0000", CultureInfo.InvariantCulture);

    private static void WriteList(LingoPropertyList list, StringBuilder sb)
    {
        if (list.Entries.Count == 0) { sb.Append("[]"); return; }
        sb.Append('[');
        for (int i = 0; i < list.Entries.Count; i++)
        {
            var entry = list.Entries[i];
            sb.Append('#').Append(entry.Key).Append(": ");
            Write(entry.Value, sb);
            if (i < list.Entries.Count - 1) sb.Append(", ");
        }
        sb.Append(']');
    }

    private static void WriteLinearList(LingoLinearList list, StringBuilder sb)
    {
        sb.Append('[');
        for (int i = 0; i < list.Items.Count; i++)
        {
            Write(list.Items[i], sb);
            if (i < list.Items.Count - 1) sb.Append(", ");
        }
        sb.Append(']');
    }

    private static void WriteCall(LingoCall call, StringBuilder sb)
    {
        sb.Append(call.FunctionName).Append('(');
        for (int i = 0; i < call.Args.Count; i++)
        {
            Write(call.Args[i], sb);
            if (i < call.Args.Count - 1) sb.Append(", ");
        }
        sb.Append(')');
    }
}
