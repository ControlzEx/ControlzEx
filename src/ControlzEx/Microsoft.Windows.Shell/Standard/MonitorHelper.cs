#pragma warning disable 1591, 618
namespace ControlzEx.Standard
{
    using System;
    using System.Diagnostics;
    using System.Windows;

    internal static class MonitorHelper
    {
        public static Rect GetOnScreenPosition(Rect rect, IntPtr windowHandle, bool ignoreTaskbar)
        {
            FindMaximumSingleMonitorRectangle(rect, out var screenSubRect, out var _);

            if (screenSubRect.Width.AreClose(0) == false
                && screenSubRect.Height.AreClose(0) == false)
            {
                return rect;
            }

            var monitor = MonitorFromRectOrWindow(new RECT(rect), windowHandle);
            if (monitor == IntPtr.Zero)
            {
                return rect;
            }

            var monitorInfo = NativeMethods.GetMonitorInfo(monitor);

            var workAreaRect = ignoreTaskbar
                ? monitorInfo.rcMonitor
                : monitorInfo.rcWork;

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

            return rect;
        }

        private static void FindMaximumSingleMonitorRectangle(Rect windowRect, out Rect screenSubRect, out Rect monitorRect)
        {
            var windowRect2 = new RECT(windowRect);
            FindMaximumSingleMonitorRectangle(windowRect2, out var screenSubRect2, out var monitorRect2);
            screenSubRect = new Rect(screenSubRect2.Position, screenSubRect2.Size);
            monitorRect = new Rect(monitorRect2.Position, monitorRect2.Size);
        }

        private static void FindMaximumSingleMonitorRectangle(RECT windowRect, out RECT screenSubRect, out RECT monitorRect)
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

        public static IntPtr MonitorFromWindowPosOrWindow(WINDOWPOS windowpos, IntPtr hwnd)
        {
            var windowRect = new RECT(windowpos);

            return MonitorFromRectOrWindow(windowRect, hwnd);
        }

        public static IntPtr MonitorFromRectOrWindow(RECT windowRect, IntPtr hwnd)
        {
            var monitorFromWindow = NativeMethods.MonitorFromWindow(hwnd, MonitorOptions.MONITOR_DEFAULTTONEAREST);

            if (windowRect.IsEmpty)
            {
                return monitorFromWindow;
            }

            var monitorFromRect = NativeMethods.MonitorFromRect(ref windowRect, MonitorOptions.MONITOR_DEFAULTTONEAREST);

            return monitorFromRect;
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