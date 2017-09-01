#pragma warning disable 1591, 618
namespace ControlzEx.Standard
{
    using System;

    internal static class MonitorHelper
    {
        /// <summary>
        ///     Gets the monitor information from the current cursor position.
        /// </summary>
        /// <returns></returns>
        public static MONITORINFO GetMonitorInfoFromPoint()
        {
            var cursorPos = NativeMethods.GetCursorPos();
            var monitor = NativeMethods.MonitorFromPoint(cursorPos, MonitorOptions.MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = NativeMethods.GetMonitorInfo(monitor);
                return monitorInfo;
            }
            return new MONITORINFO();
        }
    }
}