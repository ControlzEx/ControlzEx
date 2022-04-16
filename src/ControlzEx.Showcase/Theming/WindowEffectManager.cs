#pragma warning disable CS0618, CA1060, CA1815, CA1008, CA1045, CA1401

namespace ControlzEx.Theming
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using ControlzEx.Helpers;
    using ControlzEx.Internal;
    using global::Windows.Win32;
    using global::Windows.Win32.Graphics.Dwm;
    using global::Windows.Win32.UI.Controls;

    internal static class WindowEffectManager
    {
        public static void UpdateWindowEffect(Window window, bool isWindowActive = true)
        {
            UpdateWindowEffect(new WindowInteropHelper(window).EnsureHandle(), isWindowActive);
        }

        public static void UpdateWindowEffect(IntPtr windowHandle, bool isWindowActive = false)
        {
            var isDarkTheme = WindowsThemeHelper.AppsUseLightTheme() == false;

            // {
            //     var wtaOptions = new WTA_OPTIONS
            //     {
            //         dwFlags = WTNCA.NODRAWCAPTION | WTNCA.NODRAWICON | WTNCA.NOSYSMENU | WTNCA.NOMIRRORHELP
            //     };
            //     wtaOptions.dwMask = wtaOptions.dwFlags;
            //     NativeMethods.SetWindowThemeAttribute(windowHandle, WINDOWTHEMEATTRIBUTETYPE.WTA_NONCLIENT, ref wtaOptions, (uint)Marshal.SizeOf(typeof(WTA_OPTIONS)));
            // }

            if (OSVersionHelper.IsWindows11_OrGreater)
            {
                EnableMicaEffect(windowHandle, isDarkTheme);
            }
            else
            {
                SetAccentPolicy(windowHandle, isWindowActive, isDarkTheme);
            }
        }

        private static void EnableMicaEffect(IntPtr windowHandle, bool isDarkTheme)
        {
            DwmHelper.WindowExtendIntoClientArea(windowHandle, new MARGINS { cxLeftWidth = -1, cyTopHeight = -1, cxRightWidth = -1, cyBottomHeight = -1 });
            //var value = NativeMethods.DwmGetWindowAttribute(windowHandle, DWMWINDOWATTRIBUTE.CAPTION_BUTTON_BOUNDS, out RECT rect, Marshal.SizeOf<RECT>());

            var trueValue = 0x01;
            var falseValue = 0x00;

            // Set dark mode before applying the material, otherwise you'll get an ugly flash when displaying the window.
            if (isDarkTheme)
            {
                DwmHelper.SetWindowAttributeValue(windowHandle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, trueValue);
            }
            else
            {
                DwmHelper.SetWindowAttributeValue(windowHandle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, falseValue);
            }

            //MICA_EFFECT = 1029,                   // [set] BOOL, Enables or disables the Mica window effect
            DwmHelper.SetWindowAttributeValue(windowHandle, (DWMWINDOWATTRIBUTE)1029, trueValue);
        }

        private static void SetAccentPolicy(IntPtr windowHandle, bool isWindowActive, bool isDarkTheme)
        {
            //DwmHelper.WindowExtendIntoClientArea(windowHandle, new MARGINS(-1, -1, -1, -1));
            DwmHelper.WindowExtendIntoClientArea(windowHandle, new MARGINS { cyBottomHeight = 1 });
            //DwmHelper.SetWindowAttributeValue(windowHandle, DWMWINDOWATTRIBUTE.VISIBLE_FRAME_BORDER_THICKNESS, 0);

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

            accentPolicy.GradientColor = isDarkTheme ? 0x99000000 : 0x99FFFFFF; //ResourceHelper.GetResource<uint>(ResourceToken.BlurGradientValue);

            var accentPtr = Marshal.AllocHGlobal(accentPolicySize);
            Marshal.StructureToPtr(accentPolicy, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentPolicySize,
                Data = accentPtr
            };
            SetWindowCompositionAttribute(windowHandle, ref data);
            Marshal.FreeHGlobal(accentPtr);
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
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
    }
}