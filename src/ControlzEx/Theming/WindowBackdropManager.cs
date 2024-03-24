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
            "BackdropType", typeof(WindowBackdropType), typeof(WindowBackdropManager), new PropertyMetadata(WindowBackdropType.None, OnBackdropTypeChanged));

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
            return UpdateWindowEffect(window, GetBackdropType(window), DwmHelper.HasDarkTheme(window));
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

            return SetBackdropType(handle, backdropType, isDarkTheme);
        }

        private static bool SetBackdropType(IntPtr handle, WindowBackdropType backdropType, bool isDarkTheme)
        {
            if (backdropType is WindowBackdropType.None)
            {
                return DwmHelper.SetBackdropType(handle, backdropType);
            }

            // Set dark mode before applying the material, otherwise you'll get an ugly flash when displaying the window.
            if (DwmHelper.SetImmersiveDarkMode(handle, isDarkTheme) is false)
            {
                return false;
            }

            var result = DwmHelper.SetBackdropType(handle, backdropType);

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