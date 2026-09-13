using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PferdehofGUI;

public partial class StartupView : UserControl
{
    private readonly Navigator _navigator;
    private string? _datFolder;
    private System.Collections.Generic.List<PlayerRecord> _players = new();
    private PlayerRecord? _selectedPlayer;

    public StartupView(Navigator navigator)
    {
        InitializeComponent();
        _navigator = navigator;
        LoadFromSavedSettings();
    }

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
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
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
        _selectedPlayer = PlayerList.SelectedItem as PlayerRecord;
    }

    private void OnContinueClick(object? sender, RoutedEventArgs e)
    {
        if (_datFolder is null)
        {
            StatusText.Text = "Please select the 'dat' folder first.";
            return;
        }
        if (_selectedPlayer is null)
        {
            StatusText.Text = "Please select a player.";
            return;
        }

        SettingsService.Save(new AppSettings
        {
            DatFolder = _datFolder,
            PlayerGuid = _selectedPlayer.Guid,
            PlayerName = _selectedPlayer.Name
        });

        _navigator.Reset(new MainMenuView(_navigator, _datFolder, _selectedPlayer), "Main Menu");
    }
}
