using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PferdehofGUI;

public partial class MainMenuWindow : Window
{
    private readonly string _datFolder;
    private readonly PlayerRecord _player;

    public MainMenuWindow(string datFolder, PlayerRecord player)
    {
        InitializeComponent();
        _datFolder = datFolder;
        _player = player;
        PlayerLabel.Text = $"Player: {_player.Name}";
    }

    private void OnWindowOpened(object? sender, System.EventArgs e) => WindowSizing.ClampToScreen(this);

    private void OnEditHorseClick(object? sender, RoutedEventArgs e)
    {
        new HorseListWindow(_datFolder, _player).Show(this);
    }

    private void OnBreederSupportClick(object? sender, RoutedEventArgs e)
    {
        new BreederSupportWindow(_datFolder, _player.Guid).Show(this);
    }

    private void OnFoalCheckerClick(object? sender, RoutedEventArgs e)
    {
        new FoalCheckerWindow(_datFolder, _player.Guid).Show(this);
    }

    private void OnHorseOverviewClick(object? sender, RoutedEventArgs e)
    {
        new HorseOverviewWindow(_datFolder, _player.Guid).Show(this);
    }

    private void OnSwitchPlayerClick(object? sender, RoutedEventArgs e)
    {
        var startup = new StartupWindow();
        startup.Show();
        Close();
    }

    private void OnRepairClick(object? sender, RoutedEventArgs e)
    {
        new RepairWindow(_datFolder, _player).Show(this);
    }
}
