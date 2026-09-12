// PlayerService.cs
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PferdehofGUI;

public static class PlayerService
{
    private static readonly Regex GuidRegex = new(@"#GUID:\s*""\{([^""]+)\}""", RegexOptions.Compiled);
    private static readonly Regex NameRegex = new(@"#identity:\s*\[#name:\s*""([^""]*)""", RegexOptions.Compiled);

    public static List<PlayerRecord> LoadAll(string datFolder)
    {
        var result = new List<PlayerRecord>();
        string playersFolder = Path.Combine(datFolder, "players");
        if (!Directory.Exists(playersFolder)) return result;

        foreach (var file in Directory.GetFiles(playersFolder, "*.dat"))
        {
            string text;
            try
            {
                text = PferdehofFile.DecryptToText(file);
            }
            catch
            {
                continue;
            }

            var guidMatch = GuidRegex.Match(text);
            var nameMatch = NameRegex.Match(text);
            if (!guidMatch.Success || !nameMatch.Success) continue;

            result.Add(new PlayerRecord
            {
                FilePath = file,
                Guid = guidMatch.Groups[1].Value,
                Name = nameMatch.Groups[1].Value.Trim()
            });
        }
        return result;
    }
}