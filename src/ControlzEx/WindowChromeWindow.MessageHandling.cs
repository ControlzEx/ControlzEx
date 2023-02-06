namespace ControlzEx
{
    using System;
    using System.Security;
    using System.Windows;
    using System.Windows.Interop;
    using ControlzEx.Theming;
    using Windows.Win32;
    using Windows.Win32.Foundation;
    using Windows.Win32.Graphics.Gdi;

    public partial class WindowChromeWindow
    {
        private HWND windowHandle;
        private HwndSource? hwndSource;
        private bool handleERASEBKGND = true;
        private bool isHandlingERASEBKGND;

        private void InitializeMessageHandling()
        {
            this.windowHandle = new HWND(new WindowInteropHelper(this).Handle);
            this.hwndSource = HwndSource.FromHwnd(this.windowHandle);
            this.hwndSource?.AddHook(this.WindowProc);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            this.handleERASEBKGND = false;
            this.isHandlingERASEBKGND = false;

            base.OnContentRendered(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            this.hwndSource?.RemoveHook(this.WindowProc);
            this.windowHandle = default;
            this.hwndSource = null;

            base.OnClosed(e);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wparam, nint lparam, ref bool handled)
        {
            return this.WindowProc(hwnd, msg, (nuint)wparam.ToInt64(), lparam, ref handled);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, nuint wParam, nint lParam, ref bool handled)
        {
            if (this.windowHandle == HWND.Null)
            {
                return IntPtr.Zero;
            }

            var message = (WM)msg;

            switch (message)
            {
                case WM.ERASEBKGND:
                    return this.HandleERASEBKGND(message, wParam, lParam, out handled);
            }

            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        // Mitigation for https://github.com/dotnet/wpf/issues/5853
        [SecurityCritical]
        private IntPtr HandleERASEBKGND(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            handled = false;

            // We handle ERASEBKGND till the real window content is rendered to paint the window background in the desired theme color.
            // This also prevents users from seeing a white flash during show.
            // Handling it always causes issues with WPF rendering.
            if (this.handleERASEBKGND)
            {
                if (this.isHandlingERASEBKGND == false)
                {
                    this.isHandlingERASEBKGND = true;
                }

                unsafe
                {
                    RECT rect;
                    if (PInvoke.GetClientRect(this.windowHandle, &rect) == true)
                    {
                        var brush = WindowsThemeHelper.AppsUseLightTheme()
                            ? new HBRUSH(PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_BRUSH))
                            : new HBRUSH(PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.BLACK_BRUSH));
                        var dc = PInvoke.GetDC(this.windowHandle);

                        if (PInvoke.FillRect(dc, &rect, brush) != 0)
                        {
                            handled = true;
                            return new IntPtr(1);
                        }
                    }
                }
            }

            return IntPtr.Zero;
        }
    }
}