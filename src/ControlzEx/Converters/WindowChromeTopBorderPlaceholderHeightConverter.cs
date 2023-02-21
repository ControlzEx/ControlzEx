namespace ControlzEx.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;
    using ControlzEx.Internal.KnownBoxes;

    public sealed class WindowChromeTopBorderPlaceholderHeightConverter : IMultiValueConverter
    {
        public static readonly WindowChromeTopBorderPlaceholderHeightConverter Default = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var glowColor = (Color?)values[0];
            var nonActiveGlowColor = (Color?)values[1];
            var dwmSupportsBorderColor = (bool)values[2];
            var preferDWMBorderColor = (bool)values[3];

            // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            return (glowColor.HasValue || nonActiveGlowColor.HasValue)
                && (dwmSupportsBorderColor == false || preferDWMBorderColor == false)
                // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                ? IntBoxes.One
                : IntBoxes.Zero;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}