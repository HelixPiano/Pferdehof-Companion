using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace PferdehofGUI;

public partial class HorseListView : UserControl
{
    private readonly Navigator _navigator;
    private readonly string _datFolder;
    private readonly PlayerRecord _player;
    private List<HorseRecord> _allHorses = new();

    public HorseListView(Navigator navigator, string datFolder, PlayerRecord player)
    {
        InitializeComponent();
        _navigator = navigator;
        _datFolder = datFolder;
        _player = player;
        LoadHorses();
    }

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
            _navigator.Push(new HorseEditView(horse, _player), $"Edit Horse - {horse.Name}");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to open editor: {ex.Message}";
            Debug.WriteLine($"HorseEditView error for {horse.FilePath}: {ex}");
        }
    }
}
