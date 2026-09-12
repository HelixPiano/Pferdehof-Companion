using System.Collections.Generic;
using System.IO;

namespace PferdehofGUI;

public static class HorseService
{
    /// <summary>Horses live directly at &lt;datFolder&gt;/horses (matches .../data/dat/horses,
    /// where the folder the user selects is that "dat" folder itself).</summary>
    public static List<HorseRecord> LoadAll(string datFolder)
    {
        var result = new List<HorseRecord>();
        string horsesFolder = Path.Combine(datFolder, "horses");
        if (!Directory.Exists(horsesFolder)) return result;

        foreach (var file in Directory.GetFiles(horsesFolder, "*.dat"))
        {
            var root = PferdehofFile.TryDecryptAndParse(file);
            if (root is null) continue;
            result.Add(new HorseRecord { FilePath = file, Root = root });
        }
        return result;
    }
}
