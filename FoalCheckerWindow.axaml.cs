using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PferdehofGUI;

public partial class FoalCheckerWindow : Window
{
    private readonly string _datFolder;
    private readonly string _playerGuid;
    private List<HorseRecord> _allHorses = new();

    /// <summary>Parameterless constructor required by Avalonia's XAML loader/previewer.
    /// Not used by the running app, which always supplies datFolder/playerGuid.</summary>
    public FoalCheckerWindow() : this(string.Empty, string.Empty, skipLoad: true) { }

    public FoalCheckerWindow(string datFolder, string playerGuid) : this(datFolder, playerGuid, skipLoad: false) { }

    private FoalCheckerWindow(string datFolder, string playerGuid, bool skipLoad)
    {
        InitializeComponent();
        _datFolder = datFolder;
        _playerGuid = playerGuid;
        if (!skipLoad) Load(null);
    }

    private void OnWindowOpened(object? sender, EventArgs e) => WindowSizing.ClampToScreen(this);

    private void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        // Try to keep the same foal selected across the refresh, by GUID.
        string? previousGuid = (FoalListBox.SelectedItem as HorseRecord)?.Guid;
        Load(previousGuid);
    }

    private void Load(string? reselectGuid)
    {
        _allHorses = HorseService.LoadAll(_datFolder);
        var foals = FoalCheckerService.GetOwnedFoals(_allHorses, _playerGuid);
        FoalListBox.ItemsSource = foals;

        if (reselectGuid is not null)
        {
            var match = foals.FirstOrDefault(f => f.Guid == reselectGuid);
            if (match is not null)
            {
                FoalListBox.SelectedItem = match;
                return;
            }
        }
        ReportText.Text = "";
    }

    private void OnFoalSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (FoalListBox.SelectedItem is not HorseRecord foal)
        {
            ReportText.Text = "";
            return;
        }

        var report = FoalCheckerService.BuildReport(foal, _allHorses);
        if (report is null)
        {
            ReportText.Text = $"{foal.Name}: parent records not found on disk, cannot compare.";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{report.Foal.Name}");
        sb.AppendLine($"  Father: {report.Father.Name}");
        sb.AppendLine($"  Mother: {report.Mother.Name}");
        sb.AppendLine();

        foreach (var stat in report.Stats)
        {
            sb.AppendLine($"{stat.Stat,-12} foal={stat.FoalValue,-6:0.#}  {stat.VsParents}");
            sb.AppendLine($"             ({stat.Roll})");
        }

        ReportText.Text = sb.ToString();
    }
}
