#pragma warning disable 1591, 1573, 618, CA1060, SA1602, CA1028, CA1008
// ReSharper disable once CheckNamespace
namespace ControlzEx.Standard
{
    using System;

    public static class DwmHelper
    {
        public static bool IsCompositionEnabled()
        {
            var pfEnabled = 0;
            var result = NativeMethods.DwmIsCompositionEnabled(ref pfEnabled);
            return pfEnabled == 1;
        }

        public static bool SetWindowAttributeValue(IntPtr hWnd, DWMWINDOWATTRIBUTE attribute, int attributeValue)
        {
            return SetWindowAttribute(hWnd, attribute, ref attributeValue);
        }

        public static bool SetWindowAttribute(IntPtr hWnd, DWMWINDOWATTRIBUTE attribute, ref int attributeValue)
        {
            var result = NativeMethods.DwmSetWindowAttribute(hWnd, attribute, ref attributeValue, sizeof(int));
            return result == 0;
        }

        public static bool WindowExtendIntoClientArea(IntPtr hWnd, MARGINS margins)
        {
            // Extend frame on the bottom of client area
            var result = NativeMethods.DwmExtendFrameIntoClientArea(hWnd, ref margins);
            return result == 0;
        }
    }
}