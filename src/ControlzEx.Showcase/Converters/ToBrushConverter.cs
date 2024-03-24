namespace ControlzEx.Showcase.Converters;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

public class ToBrushConverter : IValueConverter
{
    public static readonly ToBrushConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Brush)
        {
            return value;
        }

        if (value is Color colorValue)
        {
            return new SolidColorBrush(colorValue);
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}