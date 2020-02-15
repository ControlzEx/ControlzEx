﻿namespace ControlzEx.Theming
{
    using System;
    using System.Diagnostics;
    using System.Windows.Media;
    using ControlzEx.Internal;
    using Microsoft.Win32;

    public static class WindowsThemeHelper
    {
        public static bool AppsUseLightTheme()
        {
            try
            {
                var registryValue = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", null);

                if (registryValue.IsNull())
                {
                    return true;
                }

                return Convert.ToBoolean(registryValue);
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception);
            }

            return true;
        }

        public static string GetWindowsBaseColor()
        {
            var baseColor = AppsUseLightTheme()
                ? ThemeManager.BaseColorLight
                : ThemeManager.BaseColorDark;

            return baseColor;
        }

        public static Color? GetWindowsAccentColor()
        {
            Color? accentColor = null;

            try
            {
                var registryValue = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "ColorizationColor", null);

                if (registryValue.IsNull())
                {
                    return null;
                }

                // We get negative values out of the registry, so we have to cast to int from object first.
                // Casting from int to uint works afterwards and converts the number correctly.
                var pp = (uint)(int)registryValue;
                if (pp > 0)
                {
                    var bytes = BitConverter.GetBytes(pp);
                    accentColor = Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
                }

                return accentColor;
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception);
            }

            return accentColor;
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