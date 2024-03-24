#pragma warning disable 1591, 1573, 618, CA1060, SA1602, CA1028, CA1008
// ReSharper disable once CheckNamespace
namespace ControlzEx.Internal
{
    using System;
    using System.Runtime.InteropServices;
    using ControlzEx.Native;
    using global::Windows.Win32;
    using global::Windows.Win32.Foundation;
    using global::Windows.Win32.Graphics.Dwm;
    using global::Windows.Win32.UI.Controls;

    internal static class DwmHelper
    {
        public static bool SetWindowAttributeValue(IntPtr hWnd, DWMWINDOWATTRIBUTE attribute, int attributeValue)
        {
            return SetWindowAttribute(hWnd, attribute, ref attributeValue);
        }

        public static unsafe bool SetWindowAttribute(IntPtr hWnd, DWMWINDOWATTRIBUTE attribute, ref int attributeValue)
        {
            fixed (void* value = &attributeValue)
            {
                var result = PInvoke.DwmSetWindowAttribute(new HWND(hWnd), attribute, value, sizeof(int));
                return result.Succeeded;
            }
        }

        public static bool SetWindowAttributeValue(IntPtr hWnd, DWMWINDOWATTRIBUTE attribute, uint attributeValue)
        {
            return SetWindowAttribute(hWnd, attribute, ref attributeValue);
        }

        public static unsafe bool SetWindowAttribute(IntPtr hWnd, DWMWINDOWATTRIBUTE attribute, ref uint attributeValue)
        {
            fixed (void* value = &attributeValue)
            {
                var result = PInvoke.DwmSetWindowAttribute(new HWND(hWnd), attribute, value, sizeof(uint));
                return result.Succeeded;
            }
        }

        public static unsafe bool WindowExtendIntoClientArea(IntPtr hWnd, MARGINS margins)
        {
            // Extend frame on the bottom of client area
            var result = PInvoke.DwmExtendFrameIntoClientArea(new HWND(hWnd), &margins);
            return result.Succeeded;
        }

        public static bool SetBackdropType(IntPtr hWnd, DWMSBT backdropType)
        {
            const DWMWINDOWATTRIBUTE DWMWA_SYSTEMBACKDROP_TYPE = (DWMWINDOWATTRIBUTE)38;
            return SetWindowAttributeValue(hWnd, DWMWA_SYSTEMBACKDROP_TYPE, (int)backdropType);
        }
    }
}