#pragma warning disable CA1060, CS0618, SA1307, SA1602
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Windows.Win32
{
    using global::System;
    using global::System.ComponentModel;
    using global::System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;
    using global::System.Windows;
    using global::System.Windows.Media;

    using Windows.Win32.Foundation;
    using Windows.Win32.Graphics.Gdi;
    using Windows.Win32.UI.WindowsAndMessaging;

    using DrawingPoint = global::System.Drawing.Point;

    internal partial class PInvoke
    {
        public static unsafe bool GetMappedClientRect(HWND hwnd, out RECT rectResult)
        {
            RECT rect = default;
            rectResult = rect;

            if (GetClientRect(hwnd, &rect))
            {
                MapWindowPoints(hwnd, default, &rect, 2);
                rectResult = rect;
                return true;
            }

            return false;
        }

        [DllImport("User32", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern unsafe int MapWindowPoints(HWND hWndFrom, HWND hWndTo, RECT* lpPoints, uint cPoints);

        internal static DeleteDCSafeHandle CreateCompatibleDC(SafeHandle hdc)
        {
            var hdcAddRef = false;
            try
            {
                HDC hdcLocal;
                if (hdc is object)
                {
                    hdc.DangerousAddRef(ref hdcAddRef);
                    hdcLocal = (HDC)hdc.DangerousGetHandle();
                }
                else
                {
                    hdcLocal = default;
                }

                var result = CreateCompatibleDC(hdcLocal);
                return new(result, true);
            }
            finally
            {
                if (hdcAddRef)
                {
                    hdc!.DangerousRelease();
                }
            }
        }

        public static void SelectObject(SafeHandle hdc, SafeHandle handle)
        {
            SelectObject(hdc, new HGDIOBJ(handle.DangerousGetHandle()));
        }

        internal static HGDIOBJ SelectObject(SafeHandle hdc, HGDIOBJ h)
        {
            var hdcAddRef = false;
            try
            {
                HDC hdcLocal;
                if (hdc is object)
                {
                    hdc.DangerousAddRef(ref hdcAddRef);
                    hdcLocal = (HDC)hdc.DangerousGetHandle();
                }
                else
                {
                    hdcLocal = default;
                }

                var result = SelectObject(hdcLocal, h);
                return result;
            }
            finally
            {
                if (hdcAddRef)
                {
                    hdc!.DangerousRelease();
                }
            }
        }

        internal static BOOL AlphaBlend(SafeHandle hdcDest, int xoriginDest, int yoriginDest, int wDest, int hDest, SafeHandle hdcSrc, int xoriginSrc, int yoriginSrc, int wSrc, int hSrc, BLENDFUNCTION ftn)
        {
            var hdcDestAddRef = false;
            var hdcSrcAddRef = false;
            try
            {
                HDC hdcDestLocal;
                if (hdcDest is object)
                {
                    hdcDest.DangerousAddRef(ref hdcDestAddRef);
                    hdcDestLocal = (HDC)hdcDest.DangerousGetHandle();
                }
                else
                {
                    hdcDestLocal = default;
                }

                HDC hdcSrcLocal;
                if (hdcSrc is object)
                {
                    hdcSrc.DangerousAddRef(ref hdcSrcAddRef);
                    hdcSrcLocal = (HDC)hdcSrc.DangerousGetHandle();
                }
                else
                {
                    hdcSrcLocal = default;
                }

                var result = AlphaBlend(hdcDestLocal, xoriginDest, yoriginDest, wDest, hDest, hdcSrcLocal, xoriginSrc, yoriginSrc, wSrc, hSrc, ftn);
                return result;
            }
            finally
            {
                if (hdcDestAddRef)
                {
                    hdcDest!.DangerousRelease();
                }

                if (hdcSrcAddRef)
                {
                    hdcSrc!.DangerousRelease();
                }
            }
        }

        public static void SendMessage(IntPtr hWnd, WM msg, nuint wParam, IntPtr lParam)
        {
            SendMessage(new(hWnd), (uint)msg, new(wParam), new(lParam));
        }

        public static BOOL PostMessage(IntPtr hWnd, WM msg, nuint wParam, IntPtr lParam)
        {
            return PostMessage(new(hWnd), (uint)msg, new(wParam), new(lParam));
        }

        public static unsafe MONITORINFO GetMonitorInfo(IntPtr monitor)
        {
            MONITORINFO monitorInfo;
            monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFO>();

            if (GetMonitorInfo(new HMONITOR(monitor), &monitorInfo))
            {
                return monitorInfo;
            }

            return default;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            /// <summary>
            /// initialize this field using: Marshal.SizeOf(typeof(APPBARDATA));
            /// </summary>
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public bool lParam;
        }

        public enum ABEdge
        {
            ABE_LEFT = 0,
            ABE_TOP = 1,
            ABE_RIGHT = 2,
            ABE_BOTTOM = 3
        }

        public enum ABMsg
        {
            ABM_NEW = 0,
            ABM_REMOVE = 1,
            ABM_QUERYPOS = 2,
            ABM_SETPOS = 3,
            ABM_GETSTATE = 4,
            ABM_GETTASKBARPOS = 5,
            ABM_ACTIVATE = 6,
            ABM_GETAUTOHIDEBAR = 7,
            ABM_SETAUTOHIDEBAR = 8,
            ABM_WINDOWPOSCHANGED = 9,
            ABM_SETSTATE = 10
        }

        [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

        [StructLayout(LayoutKind.Sequential)]
        public struct RTL_OSVERSIONINFOEX
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwRevision;
            public uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
        }

        [DllImport("ntdll.dll")]
        public static extern int RtlGetVersion(out RTL_OSVERSIONINFOEX lpVersionInformation);

        [DllImport("Gdi32", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern unsafe HBITMAP CreateDIBSection(HDC hdc, BITMAPINFO* pbmi, DIB_USAGE usage, out IntPtr ppvBits, HANDLE hSection, uint offset);

        public static unsafe RECT GetWindowRect(HWND hWnd)
        {
            var rect = default(RECT);
            var result = GetWindowRect(hWnd, &rect);
            return rect;
        }

        public static unsafe DrawingPoint GetCursorPos()
        {
            var rect = default(DrawingPoint);
            var result = GetCursorPos(&rect);
            return rect;
        }

        public static IntPtr GetTaskBarHandleForMonitor(HMONITOR monitor)
        {
            // maybe we can use ReBarWindow32 instead Shell_TrayWnd
            var hwnd = FindWindow("Shell_TrayWnd", null);
            var windowRect = GetWindowRect(hwnd);
            var monitorWithTaskbarOnIt = MonitorFromPoint(new() { X = windowRect.left, Y = windowRect.top }, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

            if (!monitor.Equals(monitorWithTaskbarOnIt))
            {
                hwnd = FindWindow("Shell_SecondaryTrayWnd", null);
                windowRect = GetWindowRect(hwnd);
                monitorWithTaskbarOnIt = MonitorFromPoint(new() { X = windowRect.left, Y = windowRect.top }, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

                if (!monitor.Equals(monitorWithTaskbarOnIt))
                {
                    return IntPtr.Zero;
                }
            }

            return hwnd;
        }

        public static void RaiseNonClientMouseMessageAsClient(IntPtr hWnd, WM msg, nuint wParam, nint lParam)
        {
            var newWindowMessage = msg + 513 - 161;

            RaiseMouseMessage(hWnd, newWindowMessage, wParam, lParam);
        }

        public static unsafe void RaiseMouseMessage(IntPtr hWnd, WM msg, nuint wParam, nint lParam, bool send = true)
        {
            var mousePoint = default(DrawingPoint);
            mousePoint.X = GetXLParam((int)lParam);
            mousePoint.Y = GetYLParam((int)lParam);
            var point = mousePoint;
            ScreenToClient(new(hWnd), &point);

            if (send)
            {
                SendMessage(hWnd, msg, PressedMouseButtons, MakeParam(point.X, point.Y));
            }
            else
            {
                PostMessage(hWnd, msg, PressedMouseButtons, MakeParam(point.X, point.Y));
            }
        }

        private static nuint PressedMouseButtons
        {
            get
            {
                nuint num = 0;
                if (IsKeyPressed(1))
                {
                    num |= 1;
                }

                if (IsKeyPressed(2))
                {
                    num |= 2;
                }

                if (IsKeyPressed(4))
                {
                    num |= 0x10;
                }

                if (IsKeyPressed(5))
                {
                    num |= 0x20;
                }

                if (IsKeyPressed(6))
                {
                    num |= 0x40;
                }

                return num;
            }
        }

        public static bool IsKeyPressed(int vKey)
        {
            return GetKeyState(vKey) < 0;
        }

        public static int ToInt32Unchecked(this IntPtr value)
        {
            return (int)value.ToInt64();
        }

        public static IntPtr MakeParam(int lowWord, int highWord)
        {
            return new IntPtr((lowWord & 0xFFFF) | (highWord << 16));
        }

        public static IntPtr MakeParam(Point pt)
        {
            return MakeParam((int)pt.X, (int)pt.Y);
        }

        public static int GetXLParam(int lParam)
        {
            return LoWord(lParam);
        }

        public static int GetYLParam(int lParam)
        {
            return HiWord(lParam);
        }

        public static int HiWord(int value)
        {
            return (short)(value >> 16);
        }

        public static int HiWord(long value)
        {
            return (short)((value >> 16) & 0xFFFF);
        }

        public static int HiWord(IntPtr value)
        {
            if (IntPtr.Size == 8)
            {
                return HiWord(value.ToInt64());
            }

            return HiWord(value.ToInt32());
        }

        public static int LoWord(int value)
        {
            return (short)(value & 0xFFFF);
        }

        public static int LoWord(long value)
        {
            return (short)(value & 0xFFFF);
        }

        public static int LoWord(IntPtr value)
        {
            if (IntPtr.Size == 8)
            {
                return LoWord(value.ToInt64());
            }

            return LoWord(value.ToInt32());
        }

        public static WINDOW_STYLE GetWindowStyle(HWND hWnd)
        {
            return (WINDOW_STYLE)(long)GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        }

        public static WINDOW_EX_STYLE GetWindowStyleEx(HWND hWnd)
        {
            return (WINDOW_EX_STYLE)(long)GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        }

        public static WINDOW_STYLE SetWindowStyle(HWND hWnd, WINDOW_STYLE dwNewLong)
        {
            return (WINDOW_STYLE)SetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, new IntPtr((long)dwNewLong));
        }

        public static WINDOW_EX_STYLE SetWindowStyleEx(HWND hWnd, WINDOW_EX_STYLE dwNewLong)
        {
            return (WINDOW_EX_STYLE)SetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, new IntPtr((long)dwNewLong));
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, WINDOW_LONG_PTR_INDEX nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, WINDOW_LONG_PTR_INDEX nIndex);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IntPtr GetWindowLongPtr(IntPtr hwnd, WINDOW_LONG_PTR_INDEX nIndex)
        {
            var ret = IntPtr.Size == 8
                ? GetWindowLongPtr64(hwnd, nIndex)
                : GetWindowLongPtr32(hwnd, nIndex);

            if (ret == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return ret;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        private static extern int SetWindowLongPtr32(IntPtr hWnd, WINDOW_LONG_PTR_INDEX nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, WINDOW_LONG_PTR_INDEX nIndex, IntPtr dwNewLong);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IntPtr SetWindowLongPtr(IntPtr hwnd, WINDOW_LONG_PTR_INDEX nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr64(hwnd, nIndex, dwNewLong);
            }

            return new IntPtr(SetWindowLongPtr32(hwnd, nIndex, dwNewLong.ToInt32()));
        }

        public static unsafe WINDOWPLACEMENT GetWindowPlacement(HWND hWnd)
        {
            var lpwndpl = new WINDOWPLACEMENT
            {
                length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>()
            };

            if (GetWindowPlacement(hWnd, &lpwndpl))
            {
                return lpwndpl;
            }

            return default;
        }

        public enum PreferredAppMode
        {
            Default = 0,
            AllowDark = 1,
            ForceDark = 2,
            ForceLight = 3,
            Max = 4
        }

        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int SetPreferredAppMode(PreferredAppMode preferredAppMode);

        [DllImport("uxtheme.dll", EntryPoint = "#136", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern void FlushMenuThemes();
    }

    internal struct COLORREF
    {
        public uint dwColor;

        public COLORREF(uint dwColor)
        {
            this.dwColor = dwColor;
        }

        public COLORREF(Color color)
        {
            this.dwColor = (uint)(color.R + (color.G << 8) + (color.B << 16));
        }

        public Color GetMediaColor()
        {
            return Color.FromRgb((byte)(0xFFu & this.dwColor), (byte)((0xFF00 & this.dwColor) >> 8), (byte)((0xFF0000 & this.dwColor) >> 16));
        }
    }

    [Flags]
    internal enum WVR
    {
        ALIGNTOP = 0x0010,
        ALIGNLEFT = 0x0020,
        ALIGNBOTTOM = 0x0040,
        ALIGNRIGHT = 0x0080,
        HREDRAW = 0x0100,
        VREDRAW = 0x0200,
        VALIDRECTS = 0x0400,
        REDRAW = HREDRAW | VREDRAW,
    }

    internal class DeleteDCSafeHandle : SafeHandle
    {
#pragma warning disable SA1310
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1L);
#pragma warning restore SA1310

        internal DeleteDCSafeHandle()
            : base(INVALID_HANDLE_VALUE, true)
        {
        }

        internal DeleteDCSafeHandle(IntPtr preexistingHandle, bool ownsHandle = true)
            : base(INVALID_HANDLE_VALUE, ownsHandle)
        {
            this.SetHandle(preexistingHandle);
        }

        public override bool IsInvalid => this.handle.ToInt64() == -1L || this.handle.ToInt64() == 0L;

        protected override bool ReleaseHandle() => PInvoke.DeleteDC((HDC)this.handle);
    }
}
