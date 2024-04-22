namespace ControlzEx.Theming;

using System;
using ControlzEx.Helpers;
using Windows.Win32;

[Obsolete("This class uses undocumented OS theming methods. Thus this might break at any time.")]
public static class AppModeHelper
{
    public static void SyncAppMode()
    {
        var detectedTheme = ThemeManager.Current.DetectTheme();
        if (detectedTheme is not null)
        {
            SetAppMode(detectedTheme.BaseColorScheme is ThemeManager.BaseColorDarkConst);
        }
        else
        {
            AllowDarkAppMode();
        }
    }

    public static void AllowDarkAppMode()
    {
        if (OSVersionHelper.IsWindows10_1903_OrGreater is false)
        {
            return;
        }

        PInvoke.SetPreferredAppMode(PInvoke.PreferredAppMode.AllowDark);
        PInvoke.FlushMenuThemes();
    }

    public static void SetAppMode(bool isDark)
    {
        if (OSVersionHelper.IsWindows10_1903_OrGreater is false)
        {
            return;
        }

        PInvoke.SetPreferredAppMode(isDark ? PInvoke.PreferredAppMode.ForceDark : PInvoke.PreferredAppMode.ForceLight);
        PInvoke.FlushMenuThemes();
    }
}