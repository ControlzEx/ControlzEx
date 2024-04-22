namespace ControlzEx.Theming
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media;
    using JetBrains.Annotations;
    using Microsoft.Win32;

    public static partial class WindowsThemeHelper
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
            if (IsHighContrastEnabled() is false)
            {
                return AppsUseLightTheme()
                    ? ThemeManager.BaseColorLight
                    : ThemeManager.BaseColorDark;
            }

            var windowColor = SystemColors.WindowBrush.Color;
            var brightness = System.Drawing.Color.FromArgb(windowColor.R, windowColor.G, windowColor.B).GetBrightness();

            return brightness < .5
                ? ThemeManager.BaseColorDark
                : ThemeManager.BaseColorLight;
        }

        [MustUseReturnValue]
        public static Color? GetWindowsAccentColor()
        {
            return GetWindowsAccentColorFromAccentPalette()
                ?? GetWindowsColorizationColor();
        }

        [MustUseReturnValue]
        public static Color? GetWindowsAccentColorFromAccentPalette()
        {
            var accentPaletteRegistryValue = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Accent", "AccentPalette", null);

            if (accentPaletteRegistryValue is null)
            {
                return null;
            }

            try
            {
                var bin = (byte[])accentPaletteRegistryValue;

                return Color.FromRgb(bin[0x0C], bin[0x0D], bin[0x0E]);
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.ToString());
            }

            return null;
        }

        [MustUseReturnValue]
        public static Color? GetWindowsColorizationColor()
        {
            var colorizationColorRegistryValue = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "ColorizationColor", null);

            if (colorizationColorRegistryValue is null)
            {
                return null;
            }

            try
            {
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

            return GetBlendedColor(colorizationColor.Value);
        }

        // Thanks @https://stackoverflow.com/users/3137337/emoacht for providing the correct code on how to use ColorizationColorBalance in https://stackoverflow.com/questions/24555827/how-to-get-title-bar-color-of-wpf-window-in-windows-8-1/24600956
        [MustUseReturnValue]
        public static Color? GetBlendedColor(Color color)
        {
            var colorizationColorBalanceRegistryValue = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "ColorizationColorBalance", null);

            var colorizationColorBalance = 0D;

            if (colorizationColorBalanceRegistryValue is not null)
            {
                colorizationColorBalance = (int)colorizationColorBalanceRegistryValue;
            }

            return GetBlendedColor(color, baseGrayColor, 100 - colorizationColorBalance);
        }

        [MustUseReturnValue]
        public static Color GetBlendedColor(Color color, double colorBalance)
        {
            return GetBlendedColor(color, baseGrayColor, 100 - colorBalance);
        }

        [MustUseReturnValue]
        public static Color GetBlendedColor(Color color, Color baseColor, double colorBalance)
        {
            // Blend the two colors using colorization color balance parameter.
            return BlendColor(color, baseColor, 100 - colorBalance);
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
            // ReSharper disable once ArrangeRedundantParentheses
            var buff = channel1 + ((channel2 - channel1) * channel2Percentage / 100D);
            return Math.Min((byte)Math.Round(buff), (byte)255);
        }
    }
}