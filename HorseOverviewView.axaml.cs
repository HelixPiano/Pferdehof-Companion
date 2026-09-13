using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PferdehofGUI;

public partial class HorseOverviewView : UserControl
{
    private readonly string _datFolder;
    private readonly PlayerRecord _player;

    public HorseOverviewView(string datFolder, PlayerRecord player)
    {
        InitializeComponent();
        _datFolder = datFolder;
        _player = player;
        Load();
    }

    private void OnRefreshClick(object? sender, RoutedEventArgs e) => Load();

    private void Load()
    {
        var mine = HorseService.LoadAll(_datFolder)
            .Where(h => h.GetOwnerCategory(_player.Guid) == OwnerCategory.Mine)
            .Select(HorseOverviewRow.From)
            .OrderBy(r => r.Name)
            .ToList();

        OverviewGrid.ItemsSource = mine;
        StatusText.Text = $"{mine.Count} horse(s) owned by you";
    }
}
