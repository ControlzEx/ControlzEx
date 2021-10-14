#pragma warning disable 1591, 1573, 618, CA1060, SA1602, CA1028, CA1008
// ReSharper disable once CheckNamespace
namespace ControlzEx.Standard
{
    using System;

    [CLSCompliant(false)]
    public static class DwmHelper
    {
        public static bool IsCompositionEnabled()
        {
            var pfEnabled = 0;
            var result = NativeMethods.DwmIsCompositionEnabled(ref pfEnabled);
            return pfEnabled == 1;
        }

        public static bool WindowSetAttribute(IntPtr hWnd, NativeMethods.DWMWINDOWATTRIBUTE attribute, uint attributeValue)
        {
            var result = NativeMethods.DwmSetWindowAttribute(hWnd, attribute, ref attributeValue, sizeof(uint));
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