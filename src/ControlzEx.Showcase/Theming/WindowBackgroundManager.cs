#pragma warning disable CS0618, CA1060, CA1815, CA1008, CA1045, CA1401

namespace ControlzEx.Theming
{
    using System;
    using System.Windows;
    using System.Windows.Interop;
    using ControlzEx.Helpers;
    using ControlzEx.Internal;
    using ControlzEx.Native;
    using ControlzEx.Showcase.Theming;
    using global::Windows.Win32;
    using global::Windows.Win32.Foundation;
    using global::Windows.Win32.Graphics.Dwm;
    using global::Windows.Win32.UI.Controls;
    using global::Windows.Win32.UI.WindowsAndMessaging;

    internal static class WindowBackgroundManager
    {
        public static readonly DependencyProperty BackdropTypeProperty = DependencyProperty.RegisterAttached(
            "BackdropType", typeof(WindowBackdropType), typeof(WindowBackgroundManager), new PropertyMetadata(WindowBackdropType.Mica));

        public static void SetBackdropType(Window element, WindowBackdropType value)
        {
            element.SetValue(BackdropTypeProperty, value);
        }

        [AttachedPropertyBrowsableForType(typeof(Window))]
        public static WindowBackdropType GetBackdropType(Window element)
        {
            return (WindowBackdropType)element.GetValue(BackdropTypeProperty);
        }

        public static readonly DependencyProperty CurrentBackdropTypeProperty = DependencyProperty.RegisterAttached(
            "CurrentBackdropType", typeof(WindowBackdropType), typeof(WindowBackgroundManager), new PropertyMetadata(WindowBackdropType.None));

        public static void SetCurrentBackdropType(Window element, WindowBackdropType value)
        {
            element.SetValue(CurrentBackdropTypeProperty, value);
        }

        [AttachedPropertyBrowsableForType(typeof(Window))]
        public static WindowBackdropType GetCurrentBackdropType(Window element)
        {
            return (WindowBackdropType)element.GetValue(CurrentBackdropTypeProperty);
        }

        public static bool UpdateWindowEffect(Window window)
        {
            return UpdateWindowEffect(window, GetBackdropType(window));
        }

        public static bool UpdateWindowEffect(Window window, WindowBackdropType backdropType)
        {
            var result = UpdateWindowEffect(new WindowInteropHelper(window).EnsureHandle(), backdropType);

            SetCurrentBackdropType(window, result ? backdropType : WindowBackdropType.None);
            return result;
        }

        public static bool UpdateWindowEffect(IntPtr handle, WindowBackdropType backdropType)
        {
            if (OSVersionHelper.IsWindows11_OrGreater is false)
            {
                return false;
            }

            var isDarkTheme = WindowsThemeHelper.AppsUseLightTheme() is false;

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