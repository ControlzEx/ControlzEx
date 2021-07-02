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

        [NotNull]
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

        [MustUseReturnValue]
        public static Color? GetWindowsAccentColor()
        {
            try
            {
                var registryValue = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "ColorizationColor", null);

                if (registryValue is null)
                {
                    return null;
                }

                // We get negative values out of the registry, so we have to cast to int from object first.
                // Casting from int to uint works afterwards and converts the number correctly.
                var pp = (uint)(int)registryValue;
                if (pp > 0)
                {
                    var bytes = BitConverter.GetBytes(pp);
                    // We ignore the alpha value of the color as we always expect it to be fully opaque
                    return Color.FromRgb(bytes[2], bytes[1], bytes[0]);
                }

                return null;
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.ToString());
            }

            return null;
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