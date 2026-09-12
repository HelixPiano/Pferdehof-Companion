using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PferdehofGUI;

public static class PferdehofFile
{
    private const byte XorKey = 22; // derived from "yxcvbnm"
    private static readonly Encoding Latin1 = Encoding.Latin1;

    public static string DecryptToText(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        byte[] decrypted = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
            decrypted[i] = (byte)(data[i] ^ XorKey);
        return Latin1.GetString(decrypted);
    }

    public static void EncryptToFile(string text, string outputPath)
    {
        byte[] raw = Latin1.GetBytes(text);
        byte[] encrypted = new byte[raw.Length];
        for (int i = 0; i < raw.Length; i++)
            encrypted[i] = (byte)(raw[i] ^ XorKey);
        File.WriteAllBytes(outputPath, encrypted);
    }

    /// <summary>Decrypts and parses a .dat file into a LingoPropertyList. Returns null on any failure
    /// (corrupt file, unexpected format) so callers can skip bad entries when scanning a folder.</summary>
    public static LingoPropertyList? TryDecryptAndParse(string path)
    {
        try
        {
            string text = DecryptToText(path);
            return LingoParser.Parse(text) as LingoPropertyList;
        }
        catch (Exception ex)
        {
            string text = DecryptToText(path);
            int errorPos = ExtractPosition(ex.Message);
            int start = Math.Max(0, errorPos - 60);
            int len = Math.Min(150, text.Length - start);

            Debug.WriteLine($"Failed to parse {path}: {ex.Message}");
            Debug.WriteLine($"Context: ...{text.Substring(start, len)}...");
            return null;
        }
    }

    private static int ExtractPosition(string message)
    {
        var match = System.Text.RegularExpressions.Regex.Match(message, @"pos (\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    public static void SerializeAndEncrypt(LingoPropertyList root, string outputPath)
    {
        string text = LingoSerializer.Serialize(root);
        EncryptToFile(text, outputPath);
    }
}