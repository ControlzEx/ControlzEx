#pragma warning disable 1591, 618
namespace ControlzEx.Standard
{
    using System;
    using System.Diagnostics;
    using System.Windows;

    internal static class MonitorHelper
    {
        public static Rect GetOnScreenPosition(Rect rect, bool ignoreTaskbar)
        {
            FindMaximumSingleMonitorRectangle(rect, out var screenSubRect, out var monitorRect);

            if (screenSubRect.Width == 0.0
                || screenSubRect.Height == 0.0)
            {
                if (TryGetMonitorInfoFromPoint(out var monitorInfo))
                {
                    var workAreaRect = ignoreTaskbar ? monitorInfo.rcMonitor : monitorInfo.rcWork;

                    if (rect.Width > workAreaRect.Width)
                    {
                        rect.Width = workAreaRect.Width;
                    }

                    if (rect.Height > workAreaRect.Height)
                    {
                        rect.Height = workAreaRect.Height;
                    }

                    if (rect.Right > workAreaRect.Right)
                    {
                        rect.X = workAreaRect.Right - rect.Width;
                    }

                    if (rect.Left < workAreaRect.Left)
                    {
                        rect.X = workAreaRect.Left;
                    }

                    if (rect.Bottom > workAreaRect.Bottom)
                    {
                        rect.Y = workAreaRect.Bottom - rect.Height;
                    }

                    if (rect.Top < workAreaRect.Top)
                    {
                        rect.Y = workAreaRect.Top;
                    }
                }
            }

            return rect;
        }

        internal static void FindMaximumSingleMonitorRectangle(Rect windowRect, out Rect screenSubRect, out Rect monitorRect)
        {
            var windowRect2 = new RECT(windowRect);
            FindMaximumSingleMonitorRectangle(windowRect2, out var screenSubRect2, out var monitorRect2);
            screenSubRect = new Rect(screenSubRect2.Position, screenSubRect2.Size);
            monitorRect = new Rect(monitorRect2.Position, monitorRect2.Size);
        }

        internal static void FindMaximumSingleMonitorRectangle(RECT windowRect, out RECT screenSubRect, out RECT monitorRect)
        {
            var rect = new RECT
            {
                Left = 0,
                Right = 0,
                Top = 0,
                Bottom = 0
            };

            screenSubRect = rect;

            rect = new RECT
            {
                Left = 0,
                Right = 0,
                Top = 0,
                Bottom = 0
            };

            monitorRect = rect;

            var monitorFromRect = NativeMethods.MonitorFromRect(ref windowRect, MonitorOptions.MONITOR_DEFAULTTONEAREST);
            if (monitorFromRect != IntPtr.Zero)
            {
                var monitorInfo = NativeMethods.GetMonitorInfo(monitorFromRect);
                var lprcSrc = monitorInfo.rcWork;
                NativeMethods.IntersectRect(out var lprcDst, ref lprcSrc, ref windowRect);
                screenSubRect = lprcDst;
                monitorRect = monitorInfo.rcWork;
            }
        }

        public static MONITORINFO MonitorInfoFromWindow(IntPtr hWnd)
        {
            var hMonitor = NativeMethods.MonitorFromWindow(hWnd, MonitorOptions.MONITOR_DEFAULTTONEAREST);
            var monitorInfo = NativeMethods.GetMonitorInfo(hMonitor);
            return monitorInfo;
        }

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