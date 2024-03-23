#pragma warning disable CS0618, CA1060, CA1815, CA1008, CA1045, CA1401

namespace ControlzEx.Theming
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using ControlzEx.Helpers;
    using ControlzEx.Internal;
    using ControlzEx.Native;
    using ControlzEx.Showcase.Theming;
    using global::Windows.Win32.Graphics.Dwm;
    using global::Windows.Win32.UI.Controls;

    internal static class WindowEffectManager
    {
        public static readonly DependencyProperty BackdropTypeProperty = DependencyProperty.RegisterAttached(
            "BackdropType", typeof(WindowBackdropType), typeof(WindowEffectManager), new PropertyMetadata(WindowBackdropType.Mica));

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
            "CurrentBackdropType", typeof(WindowBackdropType), typeof(WindowEffectManager), new PropertyMetadata(WindowBackdropType.None));

        public static void SetCurrentBackdropType(Window element, WindowBackdropType value)
        {
            element.SetValue(CurrentBackdropTypeProperty, value);
        }

        [AttachedPropertyBrowsableForType(typeof(Window))]
        public static WindowBackdropType GetCurrentBackdropType(Window element)
        {
            return (WindowBackdropType)element.GetValue(CurrentBackdropTypeProperty);
        }

        public static bool UpdateWindowEffect(Window window, bool isWindowActive = true)
        {
            return UpdateWindowEffect(window, GetBackdropType(window), isWindowActive);
        }

        public static bool UpdateWindowEffect(Window window, WindowBackdropType backdropType, bool isWindowActive = true)
        {
            var result = UpdateWindowEffect(new WindowInteropHelper(window).EnsureHandle(), backdropType, isWindowActive);

            SetCurrentBackdropType(window, result ? backdropType : WindowBackdropType.None);
            return result;
        }

        public static bool UpdateWindowEffect(IntPtr handle, WindowBackdropType backdropType, bool isWindowActive = true)
        {
            var isDarkTheme = WindowsThemeHelper.AppsUseLightTheme() is false;

            // {
            //     var wtaOptions = new WTA_OPTIONS
            //     {
            //         dwFlags = WTNCA.NODRAWCAPTION | WTNCA.NODRAWICON | WTNCA.NOSYSMENU | WTNCA.NOMIRRORHELP
            //     };
            //     wtaOptions.dwMask = wtaOptions.dwFlags;
            //     NativeMethods.SetWindowThemeAttribute(windowHandle, WINDOWTHEMEATTRIBUTETYPE.WTA_NONCLIENT, ref wtaOptions, (uint)Marshal.SizeOf(typeof(WTA_OPTIONS)));
            // }
            return OSVersionHelper.IsWindows11_OrGreater
                ? SetBackdropType(handle, backdropType, isDarkTheme)
                : false;
            //: SetAccentPolicy(windowHandle, isWindowActive, isDarkTheme);
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

            return DwmHelper.SetBackdropType(handle, (DWMSBT)backdropType);
        }

        private static bool SetAccentPolicy(IntPtr windowHandle, bool isWindowActive, bool isDarkTheme)
        {
            if (DwmHelper.WindowExtendIntoClientArea(windowHandle, new MARGINS { cxLeftWidth = -1, cyTopHeight = -1, cxRightWidth = -1, cyBottomHeight = -1 }) is false)
            {
                return false;
            }

            var accentPolicy = default(AccentPolicy);
            var accentPolicySize = Marshal.SizeOf(accentPolicy);

            accentPolicy.AccentFlags = 2;

            accentPolicy.AccentState = isWindowActive switch
            {
                true when OSVersionHelper.IsWindows10_1803_OrGreater => AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                true when OSVersionHelper.IsWindows10_OrGreater => AccentState.ACCENT_ENABLE_BLURBEHIND,
                true => AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT,
                false => AccentState.ACCENT_ENABLE_HOSTBACKDROP
            };

            if (HwndSource.FromHwnd(windowHandle)?.RootVisual is Window window)
            {
                window.Title = accentPolicy.AccentState.ToString();
            }

            accentPolicy.GradientColor = isDarkTheme ? 0x99000000 : 0x99FFFFFF; //ResourceHelper.GetResource<uint>(ResourceToken.BlurGradientValue);

            var accentPtr = Marshal.AllocHGlobal(accentPolicySize);
            try
            {
                Marshal.StructureToPtr(accentPolicy, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentPolicySize,
                    Data = accentPtr
                };

                return SetWindowCompositionAttribute(windowHandle, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }

#pragma warning disable SA1602
        public enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4, // RS4 1803
            ACCENT_ENABLE_HOSTBACKDROP = 5, // RS5 1809
            ACCENT_INVALID_STATE = 6
        }
#pragma warning restore SA1602

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

#pragma warning disable SA1602
        public enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }
#pragma warning restore SA1602

        [DllImport("user32.dll")]
        public static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
    }
}