using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PferdehofGUI;

public partial class HorseOverviewWindow : Window
{
    private readonly string _datFolder;
    private readonly string _playerGuid;

    public HorseOverviewWindow(string datFolder, string playerGuid)
    {
        InitializeComponent();
        _datFolder = datFolder;
        _playerGuid = playerGuid;
        Load();
    }

    private void OnWindowOpened(object? sender, EventArgs e) => WindowSizing.ClampToScreen(this);

    private void OnRefreshClick(object? sender, RoutedEventArgs e) => Load();

    private void Load()
    {
        var mine = HorseService.LoadAll(_datFolder)
            .Where(h => h.GetOwnerCategory(_playerGuid) == OwnerCategory.Mine)
            .Select(HorseOverviewRow.From)
            .OrderBy(r => r.Name)
            .ToList();

        OverviewGrid.ItemsSource = mine;
        StatusText.Text = $"{mine.Count} horse(s) owned by you";
    }
}
