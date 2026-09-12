using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PferdehofGUI;

public partial class RepairWindow : Window
{
    private readonly string _datFolder;
    private readonly PlayerRecord _player;
    private List<HorseRecord> _allHorses = new();
    private List<MissingHorseInfo> _missingFromList = new();
    private List<MissingHorseInfo> _unplaced = new();
    private List<MissingHorseInfo> _ownershipMismatches = new();

    /// <summary>Parameterless constructor required by Avalonia's XAML loader/previewer.
    /// Not used by the running app, which always supplies datFolder/player.</summary>
    public RepairWindow() : this(string.Empty, new PlayerRecord { FilePath = string.Empty, Guid = string.Empty, Name = string.Empty }, skipLoad: true) { }

    public RepairWindow(string datFolder, PlayerRecord player) : this(datFolder, player, skipLoad: false) { }

    private RepairWindow(string datFolder, PlayerRecord player, bool skipLoad)
    {
        InitializeComponent();
        _datFolder = datFolder;
        _player = player;
        if (!skipLoad) Rescan();
    }

    private void OnWindowOpened(object? sender, EventArgs e) => WindowSizing.ClampToScreen(this);

    private void OnRescanClick(object? sender, RoutedEventArgs e) => Rescan();

    private void Rescan()
    {
        try
        {
            _allHorses = HorseService.LoadAll(_datFolder);

            _missingFromList = PlayerRepairService.FindMissing(_player.FilePath, _player.Guid, _allHorses);
            MissingListListBox.ItemsSource = _missingFromList.Select(m => $"{m.Name}  ({m.Guid})").ToList();
            FixListButton.IsEnabled = _missingFromList.Count > 0;

            _unplaced = PlayerRepairService.FindUnplaced(_player.FilePath, _player.Guid, _allHorses);
            UnplacedListBox.ItemsSource = _unplaced.Select(m => $"{m.Name}  ({m.Guid})").ToList();
            int freeBoxes = PlayerRepairService.CountFreeBoxes(_player.FilePath);
            FixBoxesButton.IsEnabled = _unplaced.Count > 0 && freeBoxes > 0;

            _ownershipMismatches = PlayerRepairService.FindOwnershipMismatches(_player.FilePath, _player.Guid, _allHorses);
            OwnershipMismatchListBox.ItemsSource = _ownershipMismatches.Select(m => $"{m.Name}  ({m.Guid})").ToList();
            FixOwnershipButton.IsEnabled = _ownershipMismatches.Count > 0;

            StatusText.Text = $"{_missingFromList.Count} missing from list, {_unplaced.Count} not in a stall box "
                             + $"({freeBoxes} free box(es) available), {_ownershipMismatches.Count} boxed but not owned by you.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Scan failed: {ex.Message}";
            FixListButton.IsEnabled = false;
            FixBoxesButton.IsEnabled = false;
            FixOwnershipButton.IsEnabled = false;
        }
    }

    private void OnFixListClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            PlayerRepairService.AddMissingGuids(_player.FilePath, _missingFromList.Select(m => m.Guid));
            StatusText.Text = $"Added {_missingFromList.Count} horse(s) to your ownership list. Rescanning...";
            Rescan();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to update player file: {ex.Message}";
        }
    }

    private void OnFixBoxesClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var placedGuids = PlayerRepairService.AssignToFreeBoxes(_player.FilePath, _unplaced.Select(m => m.Guid));

            // For each horse actually placed, update its own #location fields to match and save it.
            foreach (var guid in placedGuids)
            {
                var horse = _allHorses.FirstOrDefault(h => h.Guid == guid);
                if (horse is null) continue;
                LingoPathHelper.SetSymbol(horse.Root, "location.loc", "box");
                LingoPathHelper.SetNumber(horse.Root, "location.duration", 0);
                PferdehofFile.SerializeAndEncrypt(horse.Root, horse.FilePath);
            }

            int notPlaced = _unplaced.Count - placedGuids.Count;
            StatusText.Text = notPlaced > 0
                ? $"Placed {placedGuids.Count} horse(s). {notPlaced} still need a free box - free one up in-game and rescan."
                : $"Placed {placedGuids.Count} horse(s) into stall boxes. Rescanning...";
            Rescan();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to place horses: {ex.Message}";
        }
    }

    private void OnFixOwnershipClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var horsesToFix = _allHorses.Where(h => _ownershipMismatches.Any(m => m.Guid == h.Guid)).ToList();
            PlayerRepairService.FixOwnership(_player.FilePath, _player.Guid, horsesToFix);
            StatusText.Text = $"Claimed {horsesToFix.Count} horse(s) as yours. Rescanning...";
            Rescan();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to update ownership: {ex.Message}";
        }
    }
}
