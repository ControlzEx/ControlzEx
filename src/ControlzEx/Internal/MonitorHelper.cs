#pragma warning disable 1591, 618
namespace ControlzEx.Internal
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using global::Windows.Win32;
    using global::Windows.Win32.Foundation;
    using global::Windows.Win32.Graphics.Gdi;
    using global::Windows.Win32.UI.WindowsAndMessaging;

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

            var monitor = MonitorFromRectOrWindow(rect.ToRECT(), windowHandle);
            if (monitor == IntPtr.Zero)
            {
                return rect;
            }

            var monitorInfo = PInvoke.GetMonitorInfo(monitor);

            var workAreaRect = ignoreTaskbar
                ? monitorInfo.rcMonitor
                : monitorInfo.rcWork;

            if (rect.Width > workAreaRect.GetWidth())
            {
                rect.Width = workAreaRect.GetWidth();
            }

            if (rect.Height > workAreaRect.GetHeight())
            {
                rect.Height = workAreaRect.GetHeight();
            }

            if (rect.Right > workAreaRect.right)
            {
                rect.X = workAreaRect.right - rect.Width;
            }

            if (rect.Left < workAreaRect.left)
            {
                rect.X = workAreaRect.left;
            }

            if (rect.Bottom > workAreaRect.bottom)
            {
                rect.Y = workAreaRect.bottom - rect.Height;
            }

            if (rect.Top < workAreaRect.top)
            {
                rect.Y = workAreaRect.top;
            }

            return rect;
        }

        private static void FindMaximumSingleMonitorRectangle(Rect windowRect, out Rect screenSubRect, out Rect monitorRect)
        {
            var windowRect2 = windowRect.ToRECT();
            FindMaximumSingleMonitorRectangle(windowRect2, out var screenSubRect2, out var monitorRect2);
            screenSubRect = new(screenSubRect2.GetPosition(), screenSubRect2.GetSize());
            monitorRect = new(monitorRect2.GetPosition(), monitorRect2.GetSize());
        }

        private static unsafe void FindMaximumSingleMonitorRectangle(RECT windowRect, out RECT screenSubRect, out RECT monitorRect)
        {
            var rect = new RECT
            {
                left = 0,
                right = 0,
                top = 0,
                bottom = 0
            };

            screenSubRect = rect;

            rect = new()
            {
                left = 0,
                right = 0,
                top = 0,
                bottom = 0
            };

            monitorRect = rect;

            var monitorFromRect = PInvoke.MonitorFromRect(&windowRect, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            if (monitorFromRect != IntPtr.Zero)
            {
                var monitorInfo = PInvoke.GetMonitorInfo(monitorFromRect);
                var lprcSrc = monitorInfo.rcWork;
                RECT lprcDst;
                PInvoke.IntersectRect(&lprcDst, &lprcSrc, &windowRect);
                screenSubRect = lprcDst;
                monitorRect = monitorInfo.rcWork;
            }
        }

        public static IntPtr MonitorFromWindowPosOrWindow(WINDOWPOS windowpos, IntPtr hwnd)
        {
            var windowRect = windowpos.ToRECT();

            return MonitorFromRectOrWindow(windowRect, hwnd);
        }

        public static unsafe IntPtr MonitorFromRectOrWindow(RECT windowRect, IntPtr hwnd)
        {
            var monitorFromWindow = PInvoke.MonitorFromWindow(new(hwnd), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

            if (windowRect.IsEmpty())
            {
                return monitorFromWindow;
            }

            var monitorFromRect = PInvoke.MonitorFromRect(&windowRect, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

            return monitorFromRect;
        }

        public static MONITORINFO MonitorInfoFromWindow(IntPtr hWnd)
        {
            var hMonitor = PInvoke.MonitorFromWindow(new(hWnd), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            var monitorInfo = PInvoke.GetMonitorInfo(hMonitor);
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
                var cursorPos = PInvoke.GetCursorPos();
                var monitor = PInvoke.MonitorFromPoint(cursorPos, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    monitorInfo = PInvoke.GetMonitorInfo(monitor);
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