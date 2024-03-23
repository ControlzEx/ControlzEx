#pragma warning disable CS0618, CA1060, CA1815, CA1008, CA1045, CA1401

namespace ControlzEx.Theming
{
    using System;
    using System.Windows;
    using System.Windows.Interop;
    using ControlzEx.Helpers;
    using ControlzEx.Internal;
    using ControlzEx.Native;
    using global::Windows.Win32;
    using global::Windows.Win32.Foundation;
    using global::Windows.Win32.Graphics.Dwm;
    using global::Windows.Win32.UI.Controls;
    using global::Windows.Win32.UI.WindowsAndMessaging;

    public class WindowBackdropManager : DependencyObject
    {
        private WindowBackdropManager()
        {
        }

        public static readonly DependencyProperty BackdropTypeProperty = DependencyProperty.RegisterAttached(
            "BackdropType", typeof(WindowBackdropType), typeof(WindowBackdropManager), new PropertyMetadata(WindowBackdropType.Mica, OnBackdropTypeChanged));

        public static void SetBackdropType(Window element, WindowBackdropType value)
        {
            element.SetValue(BackdropTypeProperty, value);
        }

        [AttachedPropertyBrowsableForType(typeof(Window))]
        public static WindowBackdropType GetBackdropType(Window element)
        {
            return (WindowBackdropType)element.GetValue(BackdropTypeProperty);
        }

        private static void OnBackdropTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateWindowEffect((Window)d);
        }

        // ReSharper disable once InconsistentNaming
        private static readonly DependencyPropertyKey CurrentBackdropTypePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "CurrentBackdropType", typeof(WindowBackdropType), typeof(WindowBackdropManager), new PropertyMetadata(WindowBackdropType.None));

        public static readonly DependencyProperty CurrentBackdropTypeProperty = CurrentBackdropTypePropertyKey.DependencyProperty;

        private static void SetCurrentBackdropType(Window element, WindowBackdropType value)
        {
            element.SetValue(CurrentBackdropTypePropertyKey, value);
        }

        [AttachedPropertyBrowsableForType(typeof(Window))]
        public static WindowBackdropType GetCurrentBackdropType(Window element)
        {
            return (WindowBackdropType)element.GetValue(CurrentBackdropTypeProperty);
        }

        public static bool UpdateWindowEffect(Window window)
        {
            bool isDarkTheme;
            if (ThemeManager.Current.DetectTheme(window) is { } theme)
            {
                isDarkTheme = theme.BaseColorScheme is ThemeManager.BaseColorDarkConst;
            }
            else
            {
                isDarkTheme = WindowsThemeHelper.AppsUseLightTheme() is false;
            }

            return UpdateWindowEffect(window, GetBackdropType(window), isDarkTheme);
        }

        public static bool UpdateWindowEffect(Window window, bool isDarkTheme)
        {
            return UpdateWindowEffect(window, GetBackdropType(window), isDarkTheme);
        }

        public static bool UpdateWindowEffect(Window window, WindowBackdropType backdropType, bool isDarkTheme)
        {
            if (window.AllowsTransparency)
            {
                SetCurrentBackdropType(window, WindowBackdropType.None);
                return false;
            }

            var result = UpdateWindowEffect(new WindowInteropHelper(window).EnsureHandle(), backdropType, isDarkTheme);

            SetCurrentBackdropType(window, result ? backdropType : WindowBackdropType.None);
            return result;
        }

        public static bool UpdateWindowEffect(IntPtr handle, WindowBackdropType backdropType, bool isDarkTheme)
        {
            if (OSVersionHelper.IsWindows11_22H2_OrGreater is false)
            {
                return false;
            }

            // {
            //     var wtaOptions = new WTA_OPTIONS
            //     {
            //         dwFlags = WTNCA.NODRAWCAPTION | WTNCA.NODRAWICON | WTNCA.NOSYSMENU | WTNCA.NOMIRRORHELP
            //     };
            //     wtaOptions.dwMask = wtaOptions.dwFlags;
            //     NativeMethods.SetWindowThemeAttribute(windowHandle, WINDOWTHEMEATTRIBUTETYPE.WTA_NONCLIENT, ref wtaOptions, (uint)Marshal.SizeOf(typeof(WTA_OPTIONS)));
            // }
            return SetBackdropType(handle, backdropType, isDarkTheme);
        }

        private static bool SetBackdropType(IntPtr handle, WindowBackdropType backdropType, bool isDarkTheme)
        {
            if (DwmHelper.WindowExtendIntoClientArea(handle, new MARGINS { cxLeftWidth = -1, cyTopHeight = -1, cxRightWidth = -1, cyBottomHeight = -1 }) is false)
            {
                return false;
            }

            //var value = NativeMethods.DwmGetWindowAttribute(windowHandle, DWMWINDOWATTRIBUTE.CAPTION_BUTTON_BOUNDS, out RECT rect, Marshal.SizeOf<RECT>());

            // Set dark mode before applying the material, otherwise you'll get an ugly flash when displaying the window.
            if (DwmHelper.SetWindowAttributeValue(handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, isDarkTheme
                                                      ? DWMAttributeValues.True
                                                      : DWMAttributeValues.False) is false)
            {
                return false;
            }

            var result = DwmHelper.SetBackdropType(handle, (DWMSBT)backdropType);

            // We need to disable SYSMENU. Otherwise the snap menu on the maximize button won't work.
            if (result)
            {
                var style = PInvoke.GetWindowStyle((HWND)handle);
                style &= ~WINDOW_STYLE.WS_SYSMENU;
                PInvoke.SetWindowStyle((HWND)handle, style);
            }

            return result;
        }
    }
}