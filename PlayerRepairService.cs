using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PferdehofGUI;

public class MissingHorseInfo
{
    public required string Guid { get; init; }
    public required string Name { get; init; }
}

internal class BoxAssignment
{
    public required string BoxId { get; init; } // "Box1".."Box5"
    public string? HorseGuid { get; init; }      // null if VOID
    public int HorseValueStart { get; init; }    // absolute index in the full text
    public int HorseValueLength { get; init; }
}

public static class PlayerRepairService
{
    private static readonly Regex HorsesArrayRegex = new(@"#horses:\s*\[([^\]]*)\]", RegexOptions.Compiled);
    private static readonly Regex GuidEntryRegex = new(@"""\{([^""}]+)\}""", RegexOptions.Compiled);
    private static readonly Regex HorseFieldRegex = new(@"#horse:\s*(<Void>|VOID|""\{[^""}]+\}"")", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly string[] BoxIds = { "Box1", "Box2", "Box3", "Box4", "Box5" };

    // ---------------------------------------------------------------
    // #horses ownership-list check (existing feature)
    // ---------------------------------------------------------------

    /// <summary>Finds horses whose #owner matches the player's GUID but whose GUID is missing
    /// from the player's own #horses array. Read-only - makes no changes.</summary>
    public static List<MissingHorseInfo> FindMissing(string playerFilePath, string playerGuid, List<HorseRecord> allHorses)
    {
        string text = PferdehofFile.DecryptToText(playerFilePath);
        var listedGuids = ExtractListedGuids(text);

        return allHorses
            .Where(h => h.OwnerRaw.Trim('{', '}') == playerGuid)
            .Where(h => !listedGuids.Contains(h.Guid))
            .Select(h => new MissingHorseInfo { Guid = h.Guid, Name = h.Name })
            .ToList();
    }

    private static HashSet<string> ExtractListedGuids(string playerText)
    {
        var match = HorsesArrayRegex.Match(playerText);
        var result = new HashSet<string>();
        if (!match.Success) return result;

        foreach (Match g in GuidEntryRegex.Matches(match.Groups[1].Value))
            result.Add(g.Groups[1].Value);

        return result;
    }

    /// <summary>Appends the given GUIDs to the player's #horses array in place, leaving every
    /// other byte of the file untouched. Caller is responsible for not passing GUIDs already
    /// present (FindMissing already filters these).</summary>
    public static void AddMissingGuids(string playerFilePath, IEnumerable<string> guidsToAdd)
    {
        string text = PferdehofFile.DecryptToText(playerFilePath);
        var match = HorsesArrayRegex.Match(text);
        if (!match.Success)
            throw new InvalidOperationException("Could not find #horses: [...] in the player file - refusing to guess and risk corrupting it.");

        var additions = guidsToAdd.Select(g => $"\"{{{g}}}\"").ToList();
        if (additions.Count == 0) return;

        string innerContent = match.Groups[1].Value.TrimEnd();
        string newInner = string.IsNullOrWhiteSpace(innerContent)
            ? string.Join(", ", additions)
            : innerContent + ", " + string.Join(", ", additions);

        string newText = text.Substring(0, match.Groups[1].Index)
                        + newInner
                        + text.Substring(match.Groups[1].Index + match.Groups[1].Length);

        PferdehofFile.EncryptToFile(newText, playerFilePath);
    }

    // ---------------------------------------------------------------
    // #boxes stall-placement check (new feature)
    //
    // Deliberately only targets #Box1..#Box5, not #auto (the trailer slot) - "#auto" also
    // appears elsewhere in player files as a building ID inside #Buildings, and a plain regex
    // can't safely tell those apart without much more context. Leaving #auto alone avoids any
    // risk of editing the wrong section.
    // ---------------------------------------------------------------

    /// <summary>Finds horses that ARE correctly listed in #horses and owned by this player, but
    /// aren't sitting in any of the 5 stall boxes - which can make them invisible in the stable
    /// view even though ownership is otherwise fully correct.</summary>
    public static List<MissingHorseInfo> FindUnplaced(string playerFilePath, string playerGuid, List<HorseRecord> allHorses)
    {
        string text = PferdehofFile.DecryptToText(playerFilePath);
        var listedGuids = ExtractListedGuids(text);
        var boxedGuids = GetBoxAssignments(text)
            .Where(a => a.HorseGuid is not null)
            .Select(a => a.HorseGuid!)
            .ToHashSet();

        return allHorses
            .Where(h => h.OwnerRaw.Trim('{', '}') == playerGuid)
            .Where(h => listedGuids.Contains(h.Guid))
            .Where(h => !boxedGuids.Contains(h.Guid))
            .Select(h => new MissingHorseInfo { Guid = h.Guid, Name = h.Name })
            .ToList();
    }

    /// <summary>How many of the 5 stall boxes are currently empty (VOID).</summary>
    public static int CountFreeBoxes(string playerFilePath)
    {
        string text = PferdehofFile.DecryptToText(playerFilePath);
        return GetBoxAssignments(text).Count(a => a.HorseGuid is null);
    }

    /// <summary>Every horse GUID currently sitting in one of this player's 5 stall boxes.</summary>
    public static HashSet<string> GetBoxedGuids(string playerFilePath)
    {
        string text = PferdehofFile.DecryptToText(playerFilePath);
        return GetBoxAssignments(text)
            .Where(a => a.HorseGuid is not null)
            .Select(a => a.HorseGuid!)
            .ToHashSet();
    }

    /// <summary>Finds horses physically sitting in one of this player's stall boxes whose own
    /// file says they belong to someone else (or no one) - the mirror image of FindMissing:
    /// here the box assignment is correct but the horse's #owner field itself is wrong.</summary>
    public static List<MissingHorseInfo> FindOwnershipMismatches(string playerFilePath, string playerGuid, List<HorseRecord> allHorses)
    {
        var boxedGuids = GetBoxedGuids(playerFilePath);
        return allHorses
            .Where(h => boxedGuids.Contains(h.Guid))
            .Where(h => h.OwnerRaw.Trim('{', '}') != playerGuid)
            .Select(h => new MissingHorseInfo { Guid = h.Guid, Name = h.Name })
            .ToList();
    }

    /// <summary>Sets #owner on each given horse's own file to the player's GUID, then makes sure
    /// it's also listed in the player's #horses array (it's necessarily already boxed, since
    /// that's how FindOwnershipMismatches found it in the first place).</summary>
    public static void FixOwnership(string playerFilePath, string playerGuid, List<HorseRecord> horsesToFix)
    {
        foreach (var horse in horsesToFix)
        {
            LingoPathHelper.SetString(horse.Root, "owner", "{" + playerGuid + "}");
            PferdehofFile.SerializeAndEncrypt(horse.Root, horse.FilePath);
        }

        string text = PferdehofFile.DecryptToText(playerFilePath);
        var listed = ExtractListedGuids(text);
        var stillMissingFromList = horsesToFix.Where(h => !listed.Contains(h.Guid)).Select(h => h.Guid);
        AddMissingGuids(playerFilePath, stillMissingFromList);
    }

    /// <summary>Places as many of the given GUIDs as there are free boxes for, one per box.
    /// Returns the GUIDs that were actually placed (the rest had no free box available and are
    /// left untouched). Only edits the specific #horse: VOID text for each box slot used -
    /// nothing else in the file is touched.</summary>
    public static List<string> AssignToFreeBoxes(string playerFilePath, IEnumerable<string> guidsNeedingPlacement)
    {
        string text = PferdehofFile.DecryptToText(playerFilePath);
        var placed = new List<string>();

        foreach (var guid in guidsNeedingPlacement)
        {
            var assignments = GetBoxAssignments(text);
            var freeBox = assignments.FirstOrDefault(a => a.HorseGuid is null);
            if (freeBox is null) break; // no more free boxes

            string replacement = $"\"{{{guid}}}\"";
            text = text.Substring(0, freeBox.HorseValueStart)
                 + replacement
                 + text.Substring(freeBox.HorseValueStart + freeBox.HorseValueLength);
            placed.Add(guid);
        }

        if (placed.Count > 0)
            PferdehofFile.EncryptToFile(text, playerFilePath);

        return placed;
    }

    private static List<BoxAssignment> GetBoxAssignments(string text)
    {
        var result = new List<BoxAssignment>();
        if (!TryExtractBracketSection(text, "boxes", out int boxesStart, out int boxesLen))
            return result;

        string boxesContent = text.Substring(boxesStart, boxesLen);
        foreach (var boxId in BoxIds)
        {
            if (!TryExtractBracketSection(boxesContent, boxId, out int relStart, out int relLen))
                continue;

            string boxContent = boxesContent.Substring(relStart, relLen);
            var horseMatch = HorseFieldRegex.Match(boxContent);
            if (!horseMatch.Success) continue;

            int absoluteStart = boxesStart + relStart + horseMatch.Groups[1].Index;
            string val = horseMatch.Groups[1].Value;
            string? guid = val.StartsWith("\"") ? val.Trim('"', '{', '}') : null;

            result.Add(new BoxAssignment
            {
                BoxId = boxId,
                HorseGuid = guid,
                HorseValueStart = absoluteStart,
                HorseValueLength = val.Length
            });
        }
        return result;
    }

    /// <summary>Finds "#key: [ ... ]" and returns the start/length of the content between the
    /// matching outer brackets, correctly handling brackets nested inside (unlike a plain
    /// [^\]]* regex, which breaks on the first inner "]").</summary>
    private static bool TryExtractBracketSection(string text, string key, out int contentStart, out int contentLength)
    {
        contentStart = -1;
        contentLength = 0;

        var keyMatch = Regex.Match(text, $@"#{Regex.Escape(key)}:\s*\[");
        if (!keyMatch.Success) return false;

        int openBracketIndex = keyMatch.Index + keyMatch.Length - 1;
        int depth = 0;
        for (int i = openBracketIndex; i < text.Length; i++)
        {
            if (text[i] == '[') depth++;
            else if (text[i] == ']')
            {
                depth--;
                if (depth == 0)
                {
                    contentStart = openBracketIndex + 1;
                    contentLength = i - contentStart;
                    return true;
                }
            }
        }
        return false;
    }

    // ---------------------------------------------------------------
    // Auto-sync: called from the horse editor on every save so this class of bug
    // (owned horse missing from the list or from a stall box) can't silently reappear.
    // ---------------------------------------------------------------

    /// <summary>Ensures a horse that belongs to the given player is both listed in their
    /// #horses array and sitting in a stall box, fixing whichever part is missing. Mutates
    /// horseRoot's #location fields in memory if it gets placed in a box - the caller is
    /// expected to save/encrypt horseRoot itself right after calling this (the horse file
    /// save and the player file save are two separate files/operations).</summary>
    public static void EnsureRegisteredAndPlaced(string playerFilePath, string playerGuid, string horseGuid, LingoPropertyList horseRoot)
    {
        string text = PferdehofFile.DecryptToText(playerFilePath);

        if (!ExtractListedGuids(text).Contains(horseGuid))
        {
            AddMissingGuids(playerFilePath, new[] { horseGuid });
            text = PferdehofFile.DecryptToText(playerFilePath); // re-read after the edit
        }

        bool alreadyBoxed = GetBoxAssignments(text).Any(a => a.HorseGuid == horseGuid);
        if (!alreadyBoxed)
        {
            var placed = AssignToFreeBoxes(playerFilePath, new[] { horseGuid });
            if (placed.Contains(horseGuid))
            {
                LingoPathHelper.SetSymbol(horseRoot, "location.loc", "box");
                LingoPathHelper.SetNumber(horseRoot, "location.duration", 0);
            }
            // If no free box was available, ownership is still correctly registered above;
            // the horse just can't be physically placed until a box frees up in-game.
        }
    }
}
