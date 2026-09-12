using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PferdehofGUI;

public partial class BreederSupportWindow : Window
{
    private readonly string _datFolder;
    private readonly string _playerGuid;
    private List<HorseRecord> _allHorses = new();
    private CheckBox[] _statCheckBoxes = Array.Empty<CheckBox>();

    public BreederSupportWindow(string datFolder, string playerGuid)
    {
        InitializeComponent();
        _datFolder = datFolder;
        _playerGuid = playerGuid;

        _statCheckBoxes = new[] { StatHealth, StatPower, StatCondition, StatFlexibility, StatAdrenalin, StatWeight };

        LoadHorses();
    }

    private void OnWindowOpened(object? sender, EventArgs e) => WindowSizing.ClampToScreen(this);

    private void OnRefreshClick(object? sender, RoutedEventArgs e) => LoadHorses();

    private void LoadHorses()
    {
        _allHorses = HorseService.LoadAll(_datFolder);
        Calculate();
    }

    private void OnCalculateClick(object? sender, RoutedEventArgs e) => Calculate();

    private void OnStatCheckChanged(object? sender, RoutedEventArgs e)
    {
        // Enforce "at least one stat selected" - if the user just unchecked the last one, revert it.
        if (sender is CheckBox cb && cb.IsChecked != true && _statCheckBoxes.All(c => c.IsChecked != true))
        {
            cb.IsChecked = true;
            return; // Click will fire again from the revert; avoid double-calculating
        }
        Calculate();
    }

    private List<string> GetSelectedStatFields()
    {
        var map = new (CheckBox Box, string Field)[]
        {
            (StatHealth, "health"),
            (StatPower, "power"),
            (StatCondition, "condition"),
            (StatFlexibility, "flexibility"),
            (StatAdrenalin, "adrenalin"),
            (StatWeight, "weight"),
        };
        return map.Where(m => m.Box.IsChecked == true).Select(m => m.Field).ToList();
    }

    private void Calculate()
    {
        var mode = ModeAnyEligible.IsChecked == true ? BreedingMode.AnyEligible
                 : ModeOwnMareBreederStallion.IsChecked == true ? BreedingMode.OwnMareBreederStallion
                 : BreedingMode.OwnOnly;

        var scoreFields = GetSelectedStatFields();
        if (scoreFields.Count == 0) return; // guarded by OnStatCheckChanged, but be safe

        var result = BreederService.FindBest(_allHorses, _playerGuid, mode, scoreFields, IgnoreEligibilityCheckBox.IsChecked == true);

        if (result is null)
        {
            string reason = IgnoreEligibilityCheckBox.IsChecked == true
                ? "No stallion/mare pair found (excluding geldings) for this mode."
                : "No eligible pairing found for this mode.\n\n" +
                  "Eligible means: adult, not pregnant, not on breeding cooldown, and not a gelding.";
            ResultText.Text = reason;
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Best stallion: {result.Stallion.Name} ({result.Stallion.OwnerLabel(_playerGuid)})");
            sb.AppendLine($"Best mare:     {result.Mare.Name} ({result.Mare.OwnerLabel(_playerGuid)})");
            sb.AppendLine();
            sb.AppendLine($"Scored on: {string.Join(", ", scoreFields)}");
            sb.AppendLine($"Combined score (stallion + mare): {result.TotalScore:0.#}");
            sb.AppendLine();
            sb.AppendLine("Expected foal stat ranges (across either foal sex):");
            foreach (var kv in result.ExpectedFoalRanges)
            {
                sb.AppendLine($"  {kv.Key,-12} {kv.Value.Lo,6:0.0} - {kv.Value.Hi,6:0.0}");
            }
            ResultText.Text = sb.ToString();
        }

        UpdateOwnHorsesOverview(scoreFields);
    }

    private void UpdateOwnHorsesOverview(List<string> scoreFields)
    {
        var rows = _allHorses
            .Where(h => h.GetOwnerCategory(_playerGuid) == OwnerCategory.Mine)
            .Select(h =>
            {
                var mv = h.MaxValues;
                string Fmt(string key) => mv.TryGetValue(key, out var v) ? v.ToString("0.#") : "-";
                double score = scoreFields.Sum(f => mv.TryGetValue(f, out var v) ? v : 0);

                return (Score: score, Row: new OwnHorseScoreRow
                {
                    Name = h.Name,
                    Sex = h.Sex,
                    Health = Fmt("health"),
                    Power = Fmt("power"),
                    Condition = Fmt("condition"),
                    Flexibility = Fmt("flexibility"),
                    Adrenalin = Fmt("adrenalin"),
                    Weight = Fmt("weight"),
                    Score = score.ToString("0.#")
                });
            })
            .OrderByDescending(x => x.Score)
            .Select(x => x.Row)
            .ToList();

        OwnHorsesGrid.ItemsSource = rows;
    }
}
