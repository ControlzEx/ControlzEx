#nullable enable
namespace ControlzEx.Theming
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media;
    using JetBrains.Annotations;
    using Microsoft.Win32;

    public static class WindowsThemeHelper
    {
        private static readonly Color baseGrayColor = Color.FromRgb(217, 217, 217);

        public static bool IsHighContrastEnabled()
        {
            return SystemParameters.HighContrast;
        }

        [MustUseReturnValue]
        public static bool AppsUseLightTheme()
        {
            try
            {
                var registryValue = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", null);

                if (registryValue is null)
                {
                    return true;
                }

                return Convert.ToBoolean(registryValue);
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.ToString());
            }

            return true;
        }

        // Titlebars and window borders = HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\DWM\ColorPrevalence = 0 (no), 1 = yes
        [MustUseReturnValue]
        public static bool ShowAccentColorOnTitleBarsAndWindowBorders()
        {
            try
            {
                var registryValue = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\DWM", "ColorPrevalence", null);

                if (registryValue is null)
                {
                    return true;
                }

                return Convert.ToBoolean(registryValue);
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.ToString());
            }

            return true;
        }

        [MustUseReturnValue]
        public static string GetWindowsBaseColor()
        {
            string baseColor;

            var isHighContrast = IsHighContrastEnabled();
            if (isHighContrast)
            {
                var windowColor = SystemColors.WindowBrush.Color;
                var brightness = System.Drawing.Color.FromArgb(windowColor.R, windowColor.G, windowColor.B).GetBrightness();

                baseColor = brightness < .5
                    ? ThemeManager.BaseColorDark
                    : ThemeManager.BaseColorLight;
            }
            else
            {
                baseColor = AppsUseLightTheme()
                    ? ThemeManager.BaseColorLight
                    : ThemeManager.BaseColorDark;
            }

            return baseColor;
        }

        // Thanks @https://stackoverflow.com/users/3137337/emoacht for providing the correct code on how to use ColorizationColorBalance in https://stackoverflow.com/questions/24555827/how-to-get-title-bar-color-of-wpf-window-in-windows-8-1/24600956
        [MustUseReturnValue]
        public static Color? GetWindowsAccentColor()
        {
            try
            {
                var colorizationColorRegistryValue = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "ColorizationColor", null);

                if (colorizationColorRegistryValue is null)
                {
                    return null;
                }

                var colorizationColorTypedRegistryValue = (uint)(int)colorizationColorRegistryValue;

                // Convert colorization color to Color ignoring alpha channel.
                var colorizationColor = Color.FromRgb((byte)(colorizationColorTypedRegistryValue >> 16),
                                                      (byte)(colorizationColorTypedRegistryValue >> 8),
                                                      (byte)colorizationColorTypedRegistryValue);

                return colorizationColor;
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.ToString());
            }

            return null;
        }

        [MustUseReturnValue]
        public static Color? GetBlendedWindowsAccentColor()
        {
            var colorizationColor = GetWindowsAccentColor();

            if (colorizationColor is null)
            {
                return null;
            }

            var colorizationColorBalanceRegistryValue = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "ColorizationColorBalance", null);

            var colorizationColorBalance = 0D;

            if (colorizationColorBalanceRegistryValue is not null)
            {
                colorizationColorBalance = (int)colorizationColorBalanceRegistryValue;
            }

            return GetBlendedWindowsAccentColor(colorizationColor.Value, baseGrayColor, 100 - colorizationColorBalance);
        }

        [MustUseReturnValue]
        public static Color GetBlendedWindowsAccentColor(Color colorizationColor, double colorizationColorBalance)
        {
            return GetBlendedWindowsAccentColor(colorizationColor, baseGrayColor, 100 - colorizationColorBalance);
        }

        [MustUseReturnValue]
        public static Color GetBlendedWindowsAccentColor(Color colorizationColor, Color baseColor, double colorizationColorBalance)
        {
            // Blend the two colors using colorization color balance parameter.
            return BlendColor(colorizationColor, baseColor, 100 - colorizationColorBalance);
        }

        private static Color BlendColor(Color color1, Color color2, double color2Percentage)
        {
            if (color2Percentage is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(color2Percentage));
            }

            return Color.FromRgb(BlendColorChannel(color1.R, color2.R, color2Percentage),
                                 BlendColorChannel(color1.G, color2.G, color2Percentage),
                                 BlendColorChannel(color1.B, color2.B, color2Percentage));
        }

        private static byte BlendColorChannel(double channel1, double channel2, double channel2Percentage)
        {
            var buff = channel1 + ((channel2 - channel1) * channel2Percentage / 100D);
            return Math.Min((byte)Math.Round(buff), (byte)255);
        }

        //public static Color? GetWindowsAccentColorFromUWP()
        //{
        //    try
        //    {
        //        var uiSettings = new global::Windows.UI.ViewManagement.UISettings();

        //        Color accentColor = ConvertColor(uiSettings.GetColorValue(UIColorType.Accent));
        //        Color accentColor2;
        //        Color accentColor3;
        //        Color accentColor4;
        //        if (!darkTheme)
        //        {
        //            accentColor2 = ConvertColor(uiSettings.GetColorValue(UIColorType.AccentLight1));
        //            accentColor3 = ConvertColor(uiSettings.GetColorValue(UIColorType.AccentLight2));
        //            accentColor4 = ConvertColor(uiSettings.GetColorValue(UIColorType.AccentLight3));
        //        }
        //        else
        //        {
        //            accentColor2 = ConvertColor(uiSettings.GetColorValue(UIColorType.AccentDark1));
        //            accentColor3 = ConvertColor(uiSettings.GetColorValue(UIColorType.AccentDark2));
        //            accentColor4 = ConvertColor(uiSettings.GetColorValue(UIColorType.AccentDark3));
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        Trace.WriteLine(exception);
        //    }
        //}

        //private static Color ConvertColor(global::Windows.UI.Color color)
        //{
        //    //Convert the specified UWP color to a WPF color
        //    return Color.FromArgb(color.A, color.R, color.G, color.B);
        //}
    }
}