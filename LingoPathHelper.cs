using System;
using System.Linq;

namespace PferdehofGUI;

public static class LingoPathHelper
{
    public static LingoValue? Get(LingoPropertyList root, string dottedPath)
    {
        LingoValue current = root;
        foreach (var part in dottedPath.Split('.'))
        {
            if (current is not LingoPropertyList list) return null;
            var entry = list.Entries.FirstOrDefault(e => e.Key == part);
            if (entry is null) return null;
            current = entry.Value;
        }
        return current;
    }

    public static string? GetString(LingoPropertyList root, string path) =>
        Get(root, path) is LingoString s ? s.Value : null;

    public static string? GetSymbolName(LingoPropertyList root, string path) =>
        Get(root, path) is LingoSymbol s ? s.Name : null;

    public static double? GetNumber(LingoPropertyList root, string path) =>
        Get(root, path) is LingoNumber n ? n.Value : null;

    public static void SetString(LingoPropertyList root, string path, string value)
    {
        if (Get(root, path) is LingoString s) s.Value = value;
    }

    public static void SetSymbol(LingoPropertyList root, string path, string symbolName)
    {
        if (Get(root, path) is LingoSymbol s) s.Name = symbolName;
    }

    public static void SetNumber(LingoPropertyList root, string path, double value)
    {
        if (Get(root, path) is LingoNumber n)
        {
            n.Value = value;
            n.IsInteger = value == Math.Floor(value);
        }
    }
}
