namespace ControlzEx.Theming
{
    using System;
    using System.Windows;
    using System.Windows.Interop;
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

        public static readonly DependencyProperty BackdropTypeProperty = DependencyProperty.RegisterAttached("BackdropType", typeof(WindowBackdropType), typeof(WindowBackdropManager), new PropertyMetadata(WindowBackdropType.None, OnBackdropTypeChanged));

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
            UpdateBackdrop((Window)d);
        }

        // ReSharper disable once InconsistentNaming
        private static readonly DependencyPropertyKey CurrentBackdropTypePropertyKey = DependencyProperty.RegisterAttachedReadOnly("CurrentBackdropType", typeof(WindowBackdropType), typeof(WindowBackdropManager), new PropertyMetadata(WindowBackdropType.None));

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

        public static bool UpdateBackdrop(Window target)
        {
            return UpdateBackdrop(target, GetBackdropType(target));
        }

        public static bool UpdateBackdrop(Window target, WindowBackdropType windowBackdropType)
        {
            if (windowBackdropType == GetCurrentBackdropType(target))
            {
                return true;
            }

            if (target is { AllowsTransparency: true })
            {
                SetCurrentBackdropType(target, WindowBackdropType.None);
                return false;
            }

            if (PresentationSource.FromVisual(target) is HwndSource hwndSource)
            {
                var handle = hwndSource.Handle;

                var result = UpdateBackdrop(handle, windowBackdropType);

                SetCurrentBackdropType(target, result ? windowBackdropType : WindowBackdropType.None);

                return result;
            }

            return false;
        }

        public static bool UpdateBackdrop(IntPtr handle, WindowBackdropType windowBackdropType)
        {
            if (OSVersionHelper.IsWindows11_22H2_OrGreater is false)
            {
                return false;
            }

            return SetBackdropType(handle, windowBackdropType);
        }

        private static bool SetBackdropType(IntPtr handle, WindowBackdropType windowBackdropType)
        {
            if (windowBackdropType is WindowBackdropType.None)
            {
                return DwmHelper.SetBackdropType(handle, windowBackdropType);
            }

            var result = DwmHelper.SetBackdropType(handle, windowBackdropType);

            // We need to disable SYSMENU. Otherwise the snap menu on a potential custom maximize button won't work.
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