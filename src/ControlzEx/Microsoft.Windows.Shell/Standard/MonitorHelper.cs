#pragma warning disable 1591, 618
namespace ControlzEx.Standard
{
    using System;
    using System.Diagnostics;

    internal static class MonitorHelper
    {
        /// <summary>
        /// Gets the monitor information from the current cursor position.
        /// </summary>
        /// <returns>The monitor information.</returns>
        public static MONITORINFO GetMonitorInfoFromPoint()
        {
            if (TryGetMonitorInfoFromPoint(out var mi))
            {
                return mi;
            }

            return default;
        }

        /// <summary>
        /// Gets the monitor information from the current cursor position.
        /// </summary>
        /// <returns>True when getting the monitor information was successful.</returns>
        public static bool TryGetMonitorInfoFromPoint(out MONITORINFO monitorInfo)
        {
            try
            {
                var cursorPos = NativeMethods.GetCursorPos();
                var monitor = NativeMethods.MonitorFromPoint(cursorPos, MonitorOptions.MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    monitorInfo = NativeMethods.GetMonitorInfo(monitor);
                    return true;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Trace.TraceError("Could not get the monitor info with the current cursor position: {0}", ex);
            }

            monitorInfo = default;
            return false;
        }
    }
}