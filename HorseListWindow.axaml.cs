using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace PferdehofGUI;

public partial class HorseListWindow : Window
{
    private readonly string _datFolder;
    private readonly PlayerRecord _player;
    private List<HorseRecord> _allHorses = new();

    /// <summary>Parameterless constructor required by Avalonia's XAML loader/previewer.
    /// Not used by the running app, which always supplies datFolder/player.</summary>
    public HorseListWindow() : this(string.Empty, new PlayerRecord { FilePath = string.Empty, Guid = string.Empty, Name = string.Empty }, skipLoad: true) { }

    public HorseListWindow(string datFolder, PlayerRecord player) : this(datFolder, player, skipLoad: false) { }

    private HorseListWindow(string datFolder, PlayerRecord player, bool skipLoad)
    {
        InitializeComponent();
        _datFolder = datFolder;
        _player = player;
        if (!skipLoad) LoadHorses();
    }

    private void OnWindowOpened(object? sender, EventArgs e) => WindowSizing.ClampToScreen(this);

    private void OnRefreshClick(object? sender, RoutedEventArgs e) => LoadHorses();

    private void LoadHorses()
    {
        _allHorses = HorseService.LoadAll(_datFolder);
        StatusText.Text = _allHorses.Count == 0
            ? "No horses found - check the dat folder contains a 'horses' subfolder."
            : "";
        ApplyFilter();
    }

    private void OnFilterChanged(object? sender, RoutedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<HorseRecord> filtered = _allHorses;

        if (FilterMine.IsChecked == true)
            filtered = _allHorses.Where(h => h.GetOwnerCategory(_player.Guid) == OwnerCategory.Mine);
        else if (FilterBreeder.IsChecked == true)
            filtered = _allHorses.Where(h => h.GetOwnerCategory(_player.Guid) == OwnerCategory.Breeder);
        else if (FilterOffline.IsChecked == true)
            filtered = _allHorses.Where(h => h.GetOwnerCategory(_player.Guid) == OwnerCategory.Offline);

        HorseListBox.ItemsSource = filtered.ToList();
    }

    private void OnHorseDoubleTapped(object? sender, TappedEventArgs e) => OpenEditor();

    private void OnEditClick(object? sender, RoutedEventArgs e) => OpenEditor();

    private void OpenEditor()
    {
        if (HorseListBox.SelectedItem is not HorseRecord horse)
        {
            StatusText.Text = "Select a horse from the list first.";
            return;
        }

        try
        {
            StatusText.Text = "";
            new HorseEditWindow(horse, _player).Show(this);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to open editor: {ex.Message}";
            Debug.WriteLine($"HorseEditWindow error for {horse.FilePath}: {ex}");
        }
    }
}
