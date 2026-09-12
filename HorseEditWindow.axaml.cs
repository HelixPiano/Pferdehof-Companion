using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PferdehofGUI;

public partial class HorseEditWindow : Window
{
    private readonly LingoPropertyList _root;
    private string _filePath;
    private readonly PlayerRecord? _currentPlayer;
    private readonly ObservableCollection<HorseFieldRow> _rows = new();

    /// <summary>Parameterless constructor required by Avalonia's XAML loader/previewer.
    /// Not used by the running app, which always supplies a real HorseRecord.</summary>
    public HorseEditWindow() : this(new HorseRecord { FilePath = string.Empty, Root = new LingoPropertyList() }, null) { }

    /// <summary>currentPlayer is optional - pass it when opened from the normal Edit Horse flow
    /// so saves can auto-sync the player's #horses list/stall boxes; pass null if there's no
    /// active player context (keeps the window usable standalone).</summary>
    public HorseEditWindow(HorseRecord horse, PlayerRecord? currentPlayer = null)
    {
        InitializeComponent();
        _root = horse.Root;
        _filePath = horse.FilePath;
        _currentPlayer = currentPlayer;
        FieldsGrid.ItemsSource = _rows;

        SaveFileText.Text = $"Editing: {horse.Name}  ({_filePath})";
        PopulateRows();
    }

    private void OnWindowOpened(object? sender, EventArgs e) => WindowSizing.ClampToScreen(this);

    private void PopulateRows()
    {
        var rows = new List<HorseFieldRow>();
        HorseFieldRow.Flatten(_root, "", rows);
        _rows.Clear();
        foreach (var r in rows) _rows.Add(r);
        StatusText.Text = $"Loaded {_rows.Count} fields";
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string outputPath;

            if (OverwriteCheckBox.IsChecked == true)
            {
                outputPath = _filePath;
            }
            else
            {
                string newGuid = "{" + Guid.NewGuid().ToString().ToUpperInvariant() + "}";
                var guidEntry = _root.Entries.FirstOrDefault(x => x.Key == "GUID");
                if (guidEntry?.Value is LingoString guidStr)
                    guidStr.Value = newGuid;

                string folder = Path.GetDirectoryName(_filePath)!;
                string fileName = newGuid + ".dat";
                outputPath = Path.Combine(folder, fileName);

                PopulateRows(); // refresh so the new GUID shows immediately
            }

            // Auto-sync: if this horse now belongs to the active player, make sure the
            // player file's #horses list and a stall box both reflect that before saving -
            // this is what silently drifted out of sync before and made a horse disappear
            // in-game despite its own file being perfectly valid.
            string ownerGuid = LingoPathHelper.GetString(_root, "owner")?.Trim('{', '}') ?? "";
            string thisHorseGuid = LingoPathHelper.GetString(_root, "GUID")?.Trim('{', '}') ?? "";
            if (_currentPlayer is not null && ownerGuid == _currentPlayer.Guid && thisHorseGuid.Length > 0)
            {
                PlayerRepairService.EnsureRegisteredAndPlaced(_currentPlayer.FilePath, _currentPlayer.Guid, thisHorseGuid, _root);
            }

            PferdehofFile.SerializeAndEncrypt(_root, outputPath);

            _filePath = outputPath;
            SaveFileText.Text = $"Editing: {_filePath}";
            StatusText.Text = $"Saved to {outputPath}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error saving file: {ex.Message}";
        }
    }
}
