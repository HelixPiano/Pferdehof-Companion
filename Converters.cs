using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PferdehofGUI;

public static class Converters
{
    public static readonly IValueConverter ErrorToBrush =
        new FuncValueConverter<string?, IBrush>(err =>
            string.IsNullOrEmpty(err) ? Brushes.Transparent : Brushes.Red);

    /// <summary>Used on the (invisible-when-not-a-dropdown-row) ComboBox's SelectedItem binding.
    /// Its ItemsSource is empty for plain text rows, so SelectedItem naturally evaluates to null -
    /// without this, that null gets written straight back through the TwoWay binding and silently
    /// blanks the row's real value. Returning UnsetValue tells Avalonia to skip the write entirely,
    /// so only an actual dropdown selection can ever update Value.</summary>
    public static readonly IValueConverter SuppressNullWriteback = new NullSuppressingConverter();

    private class NullSuppressingConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value ?? AvaloniaProperty.UnsetValue;
    }
}
