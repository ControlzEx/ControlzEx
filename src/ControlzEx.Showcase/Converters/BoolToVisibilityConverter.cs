namespace ControlzEx.Showcase.Converters;

using System;
using System.Globalization;
using System.Windows.Data;
using ControlzEx.Internal.KnownBoxes;

public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = (bool)value;

        if (parameter is true
            || (parameter is string stringParam && bool.TryParse(stringParam, out var boolParam) && boolParam))
        {
            boolValue = !boolValue;
        }

        return boolValue
            ? VisibilityBoxes.Visible
            : VisibilityBoxes.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}