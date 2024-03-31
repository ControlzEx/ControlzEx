#pragma warning disable CA1060, SA1602

namespace ControlzEx.Theming
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Interop;
    using System.Windows.Media;
    using ControlzEx.Internal;
    using global::Windows.Win32.Foundation;

    [Obsolete("This class uses undocumented OS theming methods. Thus this might break at any time.")]
    public class PopupBackdropManager : DependencyObject
    {
        private PopupBackdropManager()
        {
        }

        public static readonly DependencyProperty BackdropTypeProperty = DependencyProperty.RegisterAttached("BackdropType", typeof(PopupBackdropType), typeof(PopupBackdropManager), new PropertyMetadata(PopupBackdropType.None, OnBackdropTypeChanged));

        public static void SetBackdropType(Popup element, PopupBackdropType value)
        {
            element.SetValue(BackdropTypeProperty, value);
        }

        [AttachedPropertyBrowsableForType(typeof(Popup))]
        public static PopupBackdropType GetBackdropType(Popup element)
        {
            return (PopupBackdropType)element.GetValue(BackdropTypeProperty);
        }

        private static void OnBackdropTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateBackdrop((Popup)d);
        }

        // ReSharper disable once InconsistentNaming
        private static readonly DependencyPropertyKey CurrentBackdropTypePropertyKey = DependencyProperty.RegisterAttachedReadOnly("CurrentBackdropType", typeof(PopupBackdropType), typeof(PopupBackdropManager), new PropertyMetadata(PopupBackdropType.None));

        public static readonly DependencyProperty CurrentBackdropTypeProperty = CurrentBackdropTypePropertyKey.DependencyProperty;

        private static void SetCurrentBackdropType(Popup element, PopupBackdropType value)
        {
            element.SetValue(CurrentBackdropTypePropertyKey, value);
        }

        [AttachedPropertyBrowsableForType(typeof(Window))]
        [AttachedPropertyBrowsableForType(typeof(Popup))]
        public static PopupBackdropType GetCurrentBackdropType(Popup element)
        {
            return (PopupBackdropType)element.GetValue(CurrentBackdropTypeProperty);
        }

        public static readonly DependencyProperty LightTintColorProperty = DependencyProperty.RegisterAttached(
            "LightTintColor", typeof(Color), typeof(PopupBackdropManager), new PropertyMetadata(ToColor(0x99FFFFFF)));

        public static void SetLightTintColor(DependencyObject element, Color value)
        {
            element.SetValue(LightTintColorProperty, value);
        }

        public static Color GetLightTintColor(DependencyObject element)
        {
            return (Color)element.GetValue(LightTintColorProperty);
        }

        public static readonly DependencyProperty DarkTintColorProperty = DependencyProperty.RegisterAttached(
            "DarkTintColor", typeof(Color), typeof(PopupBackdropManager), new PropertyMetadata(ToColor(0x99000000)));

        public static void SetDarkTintColor(DependencyObject element, Color value)
        {
            element.SetValue(DarkTintColorProperty, value);
        }

        public static Color GetDarkTintColor(DependencyObject element)
        {
            return (Color)element.GetValue(DarkTintColorProperty);
        }

        private static Color ToColor(uint value)
        {
            var color = System.Drawing.Color.FromArgb((int)value);
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private static uint FromColor(Color value)
        {
            return (uint)System.Drawing.Color.FromArgb(value.A, value.R, value.G, value.B).ToArgb();
        }

        public static bool UpdateBackdrop(Popup target)
        {
            return UpdateBackdrop(target, DwmHelper.HasDarkTheme(target));
        }

        public static bool UpdateBackdrop(Popup target, bool isDarkTheme)
        {
            return UpdateBackdrop(target, GetBackdropType(target), isDarkTheme);
        }

        public static bool UpdateBackdrop(Popup target, PopupBackdropType popupBackdropType, bool isDarkTheme)
        {
            if (popupBackdropType == GetCurrentBackdropType(target))
            {
                return true;
            }

            if (popupBackdropType is PopupBackdropType.None
                || FeatureSupport.IsPopupBackdropSupported is false)
            {
                SetCurrentBackdropType(target, PopupBackdropType.None);
                return false;
            }

            if (target is { AllowsTransparency: true })
            {
                if (target.IsOpen)
                {
                    SetCurrentBackdropType(target, PopupBackdropType.None);
                    return false;
                }

                // todo: Do we really want to hard change this?
                target.AllowsTransparency = false;
            }

            if (target.IsOpen is false)
            {
                target.Opened -= HandlePopupOpened;
                target.Opened += HandlePopupOpened;

                return false;
            }

            if (target.Child is not null
                && PresentationSource.FromVisual(target.Child) is HwndSource hwndSource)
            {
                var handle = hwndSource.Handle;
                if (hwndSource.CompositionTarget is not null)
                {
                    hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;
                }

                // todo: Do we really want to hard change this?
                target.Child.SetValue(Panel.BackgroundProperty, Brushes.Transparent);

                DwmHelper.ExtendFrameIntoClientArea(handle, new(-1));

                var result = UpdateBackdrop(handle, target, popupBackdropType, isDarkTheme);

                SetCurrentBackdropType(target, result ? popupBackdropType : PopupBackdropType.None);

                return result;
            }

            return false;
        }

        private static void HandlePopupOpened(object? sender, EventArgs e)
        {
            var popup = (Popup)sender!;

            popup.Closed -= HandlePopupClosed;
            popup.Closed += HandlePopupClosed;
            UpdateBackdrop(popup);
        }

        private static void HandlePopupClosed(object? sender, EventArgs e)
        {
            var popup = (Popup)sender!;
            popup.Closed -= HandlePopupClosed;
            SetCurrentBackdropType(popup, PopupBackdropType.None);
        }

        public static bool UpdateBackdrop(IntPtr handle, Popup target, PopupBackdropType popupBackdropType, bool isDarkTheme)
        {
            if (FeatureSupport.IsPopupBackdropSupported is false)
            {
                return false;
            }

            return SetBackdropType(handle, target, popupBackdropType, isDarkTheme);
        }

        private static bool SetBackdropType(IntPtr handle, Popup target, PopupBackdropType popupBackdropType, bool isDarkTheme)
        {
            if (popupBackdropType is PopupBackdropType.None)
            {
                return SetAccentPolicy(handle, popupBackdropType, 0);
            }

            // Set dark mode before applying the material, otherwise you'll get an ugly flash when displaying the window.
            if (DwmHelper.SetImmersiveDarkMode(handle, isDarkTheme) is false)
            {
                return false;
            }

            return SetAccentPolicy(handle, popupBackdropType, FromColor(isDarkTheme ? GetDarkTintColor(target) : GetLightTintColor(target)));
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public AccentFlags AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        [Flags]
        private enum AccentFlags
        {
            DrawLeftBorder = 0x20,
            DrawTopBorder = 0x40,
            DrawRightBorder = 0x80,
            DrawBottomBorder = 0x100,
            DrawAllBorders = DrawLeftBorder | DrawTopBorder | DrawRightBorder | DrawBottomBorder
        }

        [DllImport("user32.dll")]
        private static extern HRESULT SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private static bool SetAccentPolicy(IntPtr handle, PopupBackdropType popupBackdropType, uint gradienColor)
        {
            var accent = default(AccentPolicy);
            var accentStructSize = Marshal.SizeOf(accent);
            accent.AccentState = (AccentState)popupBackdropType;
            accent.AccentFlags = AccentFlags.DrawAllBorders;
            accent.GradientColor = gradienColor;  // Tint Color

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = default(WindowCompositionAttributeData);
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            var result = SetWindowCompositionAttribute(handle, ref data);
            //PInvoke.DwmSetWindowAttribute((HWND)handle, (DWMWINDOWATTRIBUTE)19, &accentPtr, (uint)Marshal.SizeOf(accent));

            Marshal.FreeHGlobal(accentPtr);
            return result.Succeeded;
        }
    }
}