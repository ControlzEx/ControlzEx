namespace ControlzEx.Theming
{
    using System;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Interop;
    using System.Windows.Media;
    using ControlzEx.Helpers;
    using ControlzEx.Internal;
    using global::Windows.Win32;
    using global::Windows.Win32.Foundation;
    using global::Windows.Win32.UI.WindowsAndMessaging;

    public class WindowBackdropManager : DependencyObject
    {
        private WindowBackdropManager()
        {
        }

        public static readonly DependencyProperty BackdropTypeProperty = DependencyProperty.RegisterAttached("BackdropType", typeof(BackdropType), typeof(WindowBackdropManager), new PropertyMetadata(BackdropType.None, OnBackdropTypeChanged));

        public static void SetBackdropType(FrameworkElement element, BackdropType value)
        {
            element.SetValue(BackdropTypeProperty, value);
        }

        [AttachedPropertyBrowsableForType(typeof(Window))]
        [AttachedPropertyBrowsableForType(typeof(Popup))]
        public static BackdropType GetBackdropType(FrameworkElement element)
        {
            return (BackdropType)element.GetValue(BackdropTypeProperty);
        }

        private static void OnBackdropTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateBackdrop((FrameworkElement)d);
        }

        // ReSharper disable once InconsistentNaming
        private static readonly DependencyPropertyKey CurrentBackdropTypePropertyKey = DependencyProperty.RegisterAttachedReadOnly("CurrentBackdropType", typeof(BackdropType), typeof(WindowBackdropManager), new PropertyMetadata(BackdropType.None));

        public static readonly DependencyProperty CurrentBackdropTypeProperty = CurrentBackdropTypePropertyKey.DependencyProperty;

        private static void SetCurrentBackdropType(FrameworkElement element, BackdropType value)
        {
            element.SetValue(CurrentBackdropTypePropertyKey, value);
        }

        [AttachedPropertyBrowsableForType(typeof(Window))]
        [AttachedPropertyBrowsableForType(typeof(Popup))]
        public static BackdropType GetCurrentBackdropType(FrameworkElement element)
        {
            return (BackdropType)element.GetValue(CurrentBackdropTypeProperty);
        }

        public static bool UpdateBackdrop(FrameworkElement target)
        {
            return UpdateBackdrop(target, GetBackdropType(target), DwmHelper.HasDarkTheme(target));
        }

        public static bool UpdateBackdrop(FrameworkElement target, bool isDarkTheme)
        {
            return UpdateBackdrop(target, GetBackdropType(target), isDarkTheme);
        }

        public static bool UpdateBackdrop(FrameworkElement target, BackdropType backdropType, bool isDarkTheme)
        {
            if (backdropType == GetCurrentBackdropType(target))
            {
                return true;
            }

            if (backdropType is BackdropType.None)
            {
                SetCurrentBackdropType(target, BackdropType.None);
                return false;
            }

            if (target is Window { AllowsTransparency: true })
            {
                SetCurrentBackdropType(target, BackdropType.None);
                return false;
            }

            if (target is Popup popup)
            {
                if (popup.AllowsTransparency)
                {
                    SetCurrentBackdropType(target, BackdropType.None);
                    return false;
                }

                if (popup.IsOpen is false)
                {
                    popup.Opened += PopupOnOpened;

                    return false;

                    void PopupOnOpened(object? sender, EventArgs e)
                    {
                        UpdateBackdrop(popup);
                    }
                }
            }

            if (PresentationSource.FromVisual(target) is HwndSource hwndSource)
            {
                var result = UpdateBackdrop(hwndSource.Handle, backdropType, isDarkTheme);

                SetCurrentBackdropType(target, result ? backdropType : BackdropType.None);

                if (result
                    && target is Popup)
                {
                    DwmHelper.ExtendFrameIntoClientArea(hwndSource.Handle, new(-1));
                }

                return result;
            }

            return false;
        }

        public static bool UpdateBackdrop(IntPtr handle, BackdropType backdropType, bool isDarkTheme)
        {
            if (OSVersionHelper.IsWindows11_22H2_OrGreater is false)
            {
                return false;
            }

            return SetBackdropType(handle, backdropType, isDarkTheme);
        }

        private static bool SetBackdropType(IntPtr handle, BackdropType backdropType, bool isDarkTheme)
        {
            if (backdropType is BackdropType.None)
            {
                return DwmHelper.SetBackdropType(handle, backdropType);
            }

            // Set dark mode before applying the material, otherwise you'll get an ugly flash when displaying the window.
            if (DwmHelper.SetImmersiveDarkMode(handle, isDarkTheme) is false)
            {
                return false;
            }

            if (HwndSource.FromHwnd(handle) is { CompositionTarget: not null } hwndSource)
            {
                hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;
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