using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PferdehofGUI;

public partial class StartupWindow : Window
{
    private string? _datFolder;
    private System.Collections.Generic.List<PlayerRecord> _players = new();
    public PlayerRecord? SelectedPlayer { get; private set; }
    public string? DatFolder => _datFolder;

    public StartupWindow()
    {
        InitializeComponent();
        LoadFromSavedSettings();
    }

    private void OnWindowOpened(object? sender, EventArgs e) => WindowSizing.ClampToScreen(this);

    private void LoadFromSavedSettings()
    {
        var settings = SettingsService.Load();
        if (!string.IsNullOrEmpty(settings.DatFolder) && System.IO.Directory.Exists(settings.DatFolder))
        {
            _datFolder = settings.DatFolder;
            FolderText.Text = _datFolder;
            LoadPlayers();

            if (!string.IsNullOrEmpty(settings.PlayerGuid))
            {
                var match = _players.FirstOrDefault(p => p.Guid == settings.PlayerGuid);
                if (match is not null)
                {
                    PlayerList.SelectedItem = match;
                }
            }
        }
    }

    private async void OnChooseFolderClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = "Select the 'dat' folder"
        });

        var folder = folders.FirstOrDefault();
        if (folder is null) return;

        string path = folder.Path.LocalPath;
        string folderName = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));

        if (!string.Equals(folderName, "dat", StringComparison.OrdinalIgnoreCase))
        {
            StatusText.Text = $"That folder is named '{folderName}', not 'dat'. Please select the folder literally named 'dat'.";
            return;
        }

        _datFolder = path;
        FolderText.Text = _datFolder;
        StatusText.Text = "";
        LoadPlayers();
    }

    private void LoadPlayers()
    {
        if (_datFolder is null) return;
        _players = PlayerService.LoadAll(_datFolder);
        PlayerList.ItemsSource = _players;

        StatusText.Text = _players.Count == 0
            ? "No player files found in dat/players."
            : $"Found {_players.Count} player(s).";
    }

    private void OnPlayerSelected(object? sender, SelectionChangedEventArgs e)
    {
        SelectedPlayer = PlayerList.SelectedItem as PlayerRecord;
    }

    private void OnContinueClick(object? sender, RoutedEventArgs e)
    {
        if (_datFolder is null)
        {
            StatusText.Text = "Please select the 'dat' folder first.";
            return;
        }
        if (SelectedPlayer is null)
        {
            StatusText.Text = "Please select a player.";
            return;
        }

        SettingsService.Save(new AppSettings
        {
            DatFolder = _datFolder,
            PlayerGuid = SelectedPlayer.Guid,
            PlayerName = SelectedPlayer.Name
        });

        var menu = new MainMenuWindow(_datFolder, SelectedPlayer);
        menu.Show();
        Close();
    }
}
