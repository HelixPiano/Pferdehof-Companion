using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PferdehofGUI;

public partial class MainMenuView : UserControl
{
    private readonly Navigator _navigator;
    private readonly string _datFolder;
    private readonly PlayerRecord _player;

    public MainMenuView(Navigator navigator, string datFolder, PlayerRecord player)
    {
        InitializeComponent();
        _navigator = navigator;
        _datFolder = datFolder;
        _player = player;
        PlayerLabel.Text = $"Player: {_player.Name}";
    }

    private void OnEditHorseClick(object? sender, RoutedEventArgs e)
    {
        _navigator.Push(new HorseListView(_navigator, _datFolder, _player), "Edit Horse");
    }

    private void OnBreederSupportClick(object? sender, RoutedEventArgs e)
    {
        _navigator.Push(new BreederSupportView(_datFolder, _player), "Breeder Support");
    }

    private void OnFoalCheckerClick(object? sender, RoutedEventArgs e)
    {
        _navigator.Push(new FoalCheckerView(_datFolder, _player), "Foal Checker");
    }

    private void OnHorseOverviewClick(object? sender, RoutedEventArgs e)
    {
        _navigator.Push(new HorseOverviewView(_datFolder, _player), "My Horses - Overview");
    }

    private void OnSwitchPlayerClick(object? sender, RoutedEventArgs e)
    {
        _navigator.Reset(new StartupView(_navigator), "Setup");
    }

    private void OnRepairClick(object? sender, RoutedEventArgs e)
    {
        _navigator.Push(new RepairView(_datFolder, _player), "Repair Horse List");
    }
}
