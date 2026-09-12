using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PferdehofGUI;

public class AppSettings
{
    public string? DatFolder { get; set; }
    public string? PlayerGuid { get; set; }
    public string? PlayerName { get; set; }
}

/// <summary>Source-generated (trim- and AOT-safe) serialization context for AppSettings.
/// Avoids reflection-based JsonSerializer.Serialize/Deserialize&lt;T&gt; overloads, which
/// break under aggressive IL trimming (TrimMode=full).</summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PferdehofGUI", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize(json, AppJsonContext.Default.AppSettings) ?? new AppSettings();
            }
        }
        catch { /* fall through to defaults */ }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        string json = JsonSerializer.Serialize(settings, AppJsonContext.Default.AppSettings);
        File.WriteAllText(SettingsPath, json);
    }
}
