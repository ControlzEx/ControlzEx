#pragma warning disable 618
#pragma warning disable SA1300 // Element should begin with upper-case letter
// ReSharper disable once CheckNamespace
namespace ControlzEx.Behaviors
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using ControlzEx.Helpers;
    using ControlzEx.Internal;
    using ControlzEx.Native;
    using ControlzEx.Theming;
    using global::Windows.Win32;
    using global::Windows.Win32.Foundation;
    using global::Windows.Win32.Graphics.Gdi;
    using global::Windows.Win32.UI.Input.KeyboardAndMouse;
    using global::Windows.Win32.UI.WindowsAndMessaging;
    using Point = System.Windows.Point;

    public partial class WindowChromeBehavior
    {
        #region Fields

        private const SET_WINDOW_POS_FLAGS SwpFlags = SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;

        private bool handledERASEBKGNDOnce;

        private WindowState lastMenuState;
        private WINDOWPOS lastWindowpos;

#pragma warning disable 414
        private bool isDragging;
#pragma warning restore 414

        private NonClientControlManager? nonClientControlManager;

        #endregion

        /// <summary>Create a new instance.</summary>
        /// <SecurityNote>
        ///   Critical : Store critical methods in critical callback table
        ///   Safe     : Demands full trust permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        public WindowChromeBehavior()
        {
            // Effective default values for some of these properties are set to be bindings
            // that set them to system defaults.
            // A more correct way to do this would be to Coerce the value if the source of the DP was the default value.
            // Unfortunately with the current property system we can't detect whether the value being applied at the time
            // of the coercion is the default.
            foreach (var bp in boundProperties)
            {
                // This list must be declared after the DP's are assigned.
                BindingOperations.SetBinding(
                    this,
                    bp.DependencyProperty,
                    new Binding
                    {
                        Path = new PropertyPath("(SystemParameters." + bp.SystemParameterPropertyName + ")"),
                        Mode = BindingMode.OneWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    });
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        ///   Safe     : Demands full trust permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        private void _OnChromePropertyChangedThatRequiresRepaint()
        {
            this.ForceNativeWindowRedraw();
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _ApplyNewCustomChrome()
        {
            if (this.windowHandle == IntPtr.Zero
                || this.hwndSource is null
                || this.hwndSource.IsDisposed
                || this.hwndSource.CompositionTarget is null)
            {
                // Not yet hooked.
                return;
            }

            // Force this the first time.
            this._UpdateSystemMenu(this.AssociatedObject.WindowState);
            this.UpdateMinimizeSystemMenu(this.EnableMinimize);
            this.UpdateMaxRestoreSystemMenu(this.EnableMaxRestore);
            this.UpdateWindowStyle();

            // Mitigation for https://github.com/dotnet/wpf/issues/5853
            // This forces WPF to render a bit earlier, which reduces the time we see a blank white window on show
            PInvoke.SetWindowPos(this.windowHandle, HWND.Null, 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_DRAWFRAME | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

            if (this.hwndSource.IsDisposed)
            {
                // If the window got closed very early
                this.Cleanup(true);
            }
        }

        #region WindowProc and Message Handlers

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wparam, nint lparam, ref bool handled)
        {
            return this.WindowProc(hwnd, msg, (nuint)wparam.ToInt64(), lparam, ref handled);
        }

        /// <SecurityNote>
        ///   Critical : Accesses critical _hwnd
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr WindowProc(IntPtr hwnd, int msg, nuint wParam, nint lParam, ref bool handled)
        {
            var message = (WM)msg;

            //System.Diagnostics.Trace.WriteLine($"{DateTime.Now} {hwnd} {message} {wParam} {lParam}");

            switch (message)
            {
                case WM.NCUAHDRAWCAPTION:
                    return this._HandleNCUAHDRAWCAPTION(message, wParam, lParam, out handled);
                case WM.SETTEXT:
                case WM.SETICON:
                    return this._HandleSETICONOrSETTEXT(message, wParam, lParam, out handled);
                case WM.SYSCOMMAND:
                    return this._HandleSYSCOMMAND(message, wParam, lParam, out handled);
                case WM.NCACTIVATE:
                    return this._HandleNCACTIVATE(message, wParam, lParam, out handled);
                case WM.NCCALCSIZE:
                    return this._HandleNCCALCSIZE(message, wParam, lParam, out handled);
                case WM.NCHITTEST:
                    return this._HandleNCHITTEST(message, wParam, lParam, out handled);
                case WM.NCPAINT:
                    return this._HandleNCPAINT(message, wParam, lParam, out handled);
                case WM.ERASEBKGND:
                    return this._HandleERASEBKGND(message, wParam, lParam, out handled);
                case WM.NCRBUTTONUP:
                    return this._HandleNCRBUTTONUP(message, wParam, lParam, out handled);
                case WM.SIZE:
                    return this._HandleSIZE(message, wParam, lParam, out handled);
                case WM.WINDOWPOSCHANGING:
                    return this._HandleWINDOWPOSCHANGING(message, wParam, lParam, out handled);
                case WM.WINDOWPOSCHANGED:
                    return this._HandleWINDOWPOSCHANGED(message, wParam, lParam, out handled);
                case WM.GETMINMAXINFO:
                    return this._HandleGETMINMAXINFO(message, wParam, lParam, out handled);
                case WM.ENTERSIZEMOVE:
                    this.isDragging = true;
                    break;
                case WM.EXITSIZEMOVE:
                    this.isDragging = false;
                    break;
                case WM.MOVE:
                    return this._HandleMOVEForRealSize(message, wParam, lParam, out handled);
                case WM.STYLECHANGING:
                    return this._HandleStyleChanging(message, wParam, lParam, out handled);
                case WM.DESTROY:
                    return this._HandleDestroy(message, wParam, lParam, out handled);

                case WM.NCMOUSEMOVE:
                    return this._HandleNCMOUSEMOVE(message, wParam, lParam, out handled);
                case WM.NCLBUTTONDOWN:
                    return this._HandleNCLBUTTONDOWN(message, wParam, lParam, out handled);
                case WM.NCLBUTTONUP:
                    return this._HandleNCLBUTTONUP(message, wParam, lParam, out handled);
                case WM.NCRBUTTONDOWN:
                case WM.NCRBUTTONDBLCLK:
                    return this._HandleNCRBUTTONMessages(message, wParam, lParam, out handled);
                case WM.MOUSEMOVE:
                    return this._HandleMOUSEMOVE(message, wParam, lParam, out handled);
                case WM.NCMOUSELEAVE:
                    return this._HandleNCMOUSELEAVE(message, wParam, lParam, out handled);
                case WM.MOUSELEAVE:
                    return this._HandleMOUSELEAVE(message, wParam, lParam, out handled);
            }

            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleNCUAHDRAWCAPTION(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            if (this.AssociatedObject.ShowInTaskbar == false
                && this._GetHwndState() == WindowState.Minimized)
            {
                // Minimize the window with ShowInTaskbar == false might cause Windows to redraw the caption.
                using (new SuppressRedrawScope(this.windowHandle))
                {
                    handled = true;
                    return PInvoke.DefWindowProc(this.windowHandle, (uint)uMsg, wParam, lParam);
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleSETICONOrSETTEXT(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            // Setting the caption text and icon cause Windows to redraw the caption.
            using (new SuppressRedrawScope(this.windowHandle))
            {
                handled = true;
                return PInvoke.DefWindowProc(this.windowHandle, (uint)uMsg, wParam, lParam);
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleSYSCOMMAND(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            handled = false;

            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleNCACTIVATE(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            // Despite MSDN's documentation of lParam not being used,
            // calling DefWindowProc with lParam set to -1 causes Windows not to draw over the caption.

            // Directly call DefWindowProc with a custom parameter
            // which bypasses any other handling of the message.
            var lRet = PInvoke.DefWindowProc(this.windowHandle, (uint)WM.NCACTIVATE, wParam, new IntPtr(-1));
            // We don't have any non client area, so we can just discard this message by handling it
            handled = true;

            this.IsNCActive = wParam != 0;

            if (this.IsNCActive == false)
            {
                this.nonClientControlManager?.ClearTrackedControl();
            }

            return lRet;
        }

        /// <summary>
        /// This method handles the window size if the taskbar is set to auto-hide.
        /// </summary>
        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private static RECT AdjustWorkingAreaForAutoHide(IntPtr monitorContainingApplication, RECT area)
        {
            var hwnd = PInvoke.GetTaskBarHandleForMonitor(new HMONITOR(monitorContainingApplication));

            if (hwnd == IntPtr.Zero)
            {
                return area;
            }

            var abd = default(PInvoke.APPBARDATA);
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = hwnd;
            PInvoke.SHAppBarMessage((int)PInvoke.ABMsg.ABM_GETTASKBARPOS, ref abd);
            var autoHide = Convert.ToBoolean(PInvoke.SHAppBarMessage((int)PInvoke.ABMsg.ABM_GETSTATE, ref abd));

            if (autoHide == false)
            {
                return area;
            }

            switch (abd.uEdge)
            {
                case (int)PInvoke.ABEdge.ABE_LEFT:
                    area.left += 2;
                    break;
                case (int)PInvoke.ABEdge.ABE_RIGHT:
                    area.right -= 2;
                    break;
                case (int)PInvoke.ABEdge.ABE_TOP:
                    area.top += 2;
                    break;
                case (int)PInvoke.ABEdge.ABE_BOTTOM:
                    area.bottom -= 2;
                    break;
                default:
                    return area;
            }

            return area;
        }

        // Black Border Workaround
        //
        // 762437 - DWM: Windows that have both clip and alpha margins are drawn without respecting alpha
        // There was a regression in DWM in Windows 7 with regard to handling WM_NCCALCSIZE to effect custom chrome.
        // When windows with glass are maximized on a multi-monitor setup, the glass frame tends to turn black.
        // Also, when windows are resized they tend to flicker black, sometimes staying that way until resized again.
        //
        // At least on RTM Win7 we can avoid the problem by making the client area not exactly match the non-client
        // area, so we added the SacrificialEdge property.
        [SecurityCritical]
        private IntPtr _HandleNCCALCSIZE(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            // lParam is an [in, out] that can be either a RECT* (wParam == FALSE) or an NCCALCSIZE_PARAMS*.
            // Since the first field of NCCALCSIZE_PARAMS is a RECT and is the only field we care about
            // we can unconditionally treat it as a RECT.

            handled = true;

            var hwndState = this._GetHwndState();

            if (hwndState == WindowState.Maximized)
            {
                // We have to get the monitor preferably from the window position as the info for the window handle might not yet be updated.
                // As we update lastWindowpos in WINDOWPOSCHANGING we have the right "future" position and thus can get the correct monitor from that.
                var monitor = MonitorHelper.MonitorFromWindowPosOrWindow(this.lastWindowpos, this.windowHandle);
                var monitorInfo = PInvoke.GetMonitorInfo(monitor);
                //System.Diagnostics.Trace.WriteLine(monitorInfo.rcWork);

                var monitorRect = this.IgnoreTaskbarOnMaximize
                    ? monitorInfo.rcMonitor
                    : monitorInfo.rcWork;

                var rc = Marshal.PtrToStructure<RECT>(lParam);
                rc.left = monitorRect.left;
                rc.top = monitorRect.top;
                rc.right = monitorRect.right;
                rc.bottom = monitorRect.bottom;

                // monitor and work area will be equal if taskbar is hidden
                if (this.IgnoreTaskbarOnMaximize == false
                    && monitorInfo.rcMonitor.GetHeight() == monitorInfo.rcWork.GetHeight()
                    && monitorInfo.rcMonitor.GetWidth() == monitorInfo.rcWork.GetWidth())
                {
                    rc = AdjustWorkingAreaForAutoHide(monitor, rc);
                }

                Marshal.StructureToPtr(rc, lParam, true);
            }
            else if (OSVersionHelper.IsWindows10_OrGreater
                     && PInvoke.GetWindowStyle(this.windowHandle).HasFlag(WINDOW_STYLE.WS_CAPTION))
            {
                var rcBefore = Marshal.PtrToStructure<RECT>(lParam);
                PInvoke.DefWindowProc(this.windowHandle, (uint)uMsg, wParam, lParam);
                var rc = Marshal.PtrToStructure<RECT>(lParam);
                rc.top = rcBefore.top; // Remove titlebar
                Marshal.StructureToPtr(rc, lParam, true);
            }
            else if (this.AssociatedObject.AllowsTransparency == false
                     && this._GetHwndState() == WindowState.Normal
                     && wParam != 0)
            {
                var rc = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT))!;

                // We have to add or remove one pixel on any side of the window to force a flicker free resize.
                // Removing pixels would result in a smaller client area.
                // Adding pixels does not seem to really increase the client area.
                rc.bottom += 1;

                Marshal.StructureToPtr(rc, lParam, true);
            }

            // Per MSDN for NCCALCSIZE, always return 0 when wParam == FALSE
            //
            // Returning 0 when wParam == TRUE is not appropriate - it will preserve
            // the old client area and align it with the upper-left corner of the new
            // client area. So we simply ask for a redraw (WVR_REDRAW)

            var retVal = IntPtr.Zero;
            if (wParam != 0) // wParam == TRUE
            {
                // Using the combination of WVR.VALIDRECTS and WVR.REDRAW gives the smoothest
                // resize behavior we can achieve here.
                retVal = new IntPtr((int)(WVR.VALIDRECTS | WVR.REDRAW));
            }

            return retVal;
        }

        private HT _GetHTFromResizeGripDirection(ResizeGripDirection direction)
        {
            var isRightToLeft = this.AssociatedObject.FlowDirection == FlowDirection.RightToLeft;
            return direction switch
            {
                ResizeGripDirection.Bottom => HT.BOTTOM,
                ResizeGripDirection.BottomLeft => isRightToLeft
                    ? HT.BOTTOMRIGHT
                    : HT.BOTTOMLEFT,
                ResizeGripDirection.BottomRight => isRightToLeft
                    ? HT.BOTTOMLEFT
                    : HT.BOTTOMRIGHT,
                ResizeGripDirection.Left => isRightToLeft
                    ? HT.RIGHT
                    : HT.LEFT,
                ResizeGripDirection.Right => isRightToLeft
                    ? HT.LEFT
                    : HT.RIGHT,
                ResizeGripDirection.Top => HT.TOP,
                ResizeGripDirection.TopLeft => isRightToLeft
                    ? HT.TOPRIGHT
                    : HT.TOPLEFT,
                ResizeGripDirection.TopRight => isRightToLeft
                    ? HT.TOPLEFT
                    : HT.TOPRIGHT,
                ResizeGripDirection.Caption => HT.CAPTION,
                _ => HT.NOWHERE
            };
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleNCPAINT(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        // Mitigation for https://github.com/dotnet/wpf/issues/5853
        [SecurityCritical]
        private IntPtr _HandleERASEBKGND(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            handled = false;

            // We handle ERASEBKGND once to paint the window background in the desired theme color.
            // This also prevents users from seeing a white flash during show.
            // Handling it always causes issues with WPF rendering.
            if (this.handledERASEBKGNDOnce == false)
            {
                this.handledERASEBKGNDOnce = true;

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

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleNCHITTEST(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            // We always want to handle hit-testing
            handled = true;

            var hitTestResult = this.GetHitTestResult(lParam);

            return new IntPtr((int)hitTestResult);
        }

        private HT GetHitTestResult(nint lParam)
        {
            if (NonClientControlManager.GetControlUnderMouse(this.AssociatedObject, lParam, out var htFromNcControlManager) is not null
                && htFromNcControlManager != HT.CAPTION)
            {
                return htFromNcControlManager;
            }

            var dpi = this.AssociatedObject.GetDpi();

            // Let the system know if we consider the mouse to be in our effective non-client area.
            var mousePosScreen = Utility.GetPoint(lParam);
            var windowRect = this._GetWindowRect();

            var preventResize = this._GetHwndState() == WindowState.Maximized || this.AssociatedObject.ResizeMode is ResizeMode.NoResize;
            var htFromTestNca = preventResize
                ? HT.CLIENT
                : this._HitTestNca(DpiHelper.DeviceRectToLogical(windowRect, dpi.DpiScaleX, dpi.DpiScaleY),
                                   DpiHelper.DevicePixelsToLogical(mousePosScreen, dpi.DpiScaleX, dpi.DpiScaleY));

            if (htFromTestNca != HT.CLIENT
                || this.AssociatedObject.ResizeMode == ResizeMode.CanResizeWithGrip)
            {
                var mousePosWindow = this.AssociatedObject.PointFromScreen(mousePosScreen);

                // If the app is asking for content to be treated as client then that takes precedence over _everything_, even DWM caption buttons.
                // This allows apps to set the glass frame to be non-empty, still cover it with WPF content to hide all the glass,
                // yet still get DWM to draw a drop shadow.
                var inputElement = this.AssociatedObject.InputHitTest(mousePosWindow);
                if (inputElement is not null)
                {
                    if (WindowChrome.GetIsHitTestVisibleInChrome(inputElement))
                    {
                        return HT.CLIENT;
                    }

                    if (this.AssociatedObject.ResizeMode == ResizeMode.CanResizeWithGrip)
                    {
                        var direction = WindowChrome.GetResizeGripDirection(inputElement);
                        if (direction != ResizeGripDirection.None)
                        {
                            return this._GetHTFromResizeGripDirection(direction);
                        }
                    }
                }
            }

            if (htFromNcControlManager != HT.NOWHERE
                && htFromTestNca == HT.CLIENT)
            {
                return htFromNcControlManager;
            }

            return htFromTestNca;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical method
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleNCRBUTTONUP(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            handled = false;

            // Emulate the system behavior of clicking the right mouse button over the caption area
            // to bring up the system menu.
            var hitTest = (HT)wParam;
            if (hitTest is HT.CAPTION or HT.MINBUTTON or HT.MAXBUTTON or HT.CLOSE)
            {
                handled = true;

                ControlzEx.SystemCommands.ShowSystemMenuPhysicalCoordinates(this.AssociatedObject, Utility.GetPoint(lParam));
            }

            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical method
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleSIZE(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            const int SIZE_MAXIMIZED = 2;

            // Whenever an interesting size change takes place, there may be a corresponding rounding change.
            // if (NativeMethods.DwmGetWindowAttribute(this.windowHandle, DWMWINDOWATTRIBUTE.WINDOW_CORNER_RADIUS, out var radius, sizeof(int)) == HRESULT.S_OK)
            // {
            //     this.DWMCornerRadius = radius;
            // }

            // Force when maximized.
            // We can tell what's happening right now, but the Window doesn't yet know it's
            // maximized.  Not forcing this update will eventually cause the
            // default caption to be drawn.
            WindowState? state = null;
            if (wParam == SIZE_MAXIMIZED)
            {
                state = WindowState.Maximized;
            }

            this._UpdateSystemMenu(state);

            // Still let the default WndProc handle this.
            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical Marshal.PtrToStructure
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleWINDOWPOSCHANGING(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            this.UpdateWindowStyle();

            var windowpos = Marshal.PtrToStructure<WINDOWPOS>(lParam);

            //System.Diagnostics.Trace.WriteLine(windowpos);

            this.lastWindowpos = windowpos;

            // we don't do bitwise operations cuz we're checking for this flag being the only one there
            // I have no clue why this works, I tried this because VS2013 has this flag removed on fullscreen window moves
            if (this.IgnoreTaskbarOnMaximize
                && this._GetHwndState() == WindowState.Maximized
                && windowpos.flags == SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED)
            {
                windowpos.flags = 0;
                Marshal.StructureToPtr(windowpos, lParam, true);

                this.lastWindowpos = windowpos;

                handled = true;
                return IntPtr.Zero;
            }

            if ((windowpos.flags & SET_WINDOW_POS_FLAGS.SWP_NOMOVE) != 0)
            {
                handled = false;
                return IntPtr.Zero;
            }

            var wnd = this.AssociatedObject;
            if (wnd is null
                || this.hwndSource?.CompositionTarget is null)
            {
                handled = false;
                return IntPtr.Zero;
            }

            if ((windowpos.flags & SET_WINDOW_POS_FLAGS.SWP_NOMOVE) == 0
                && (windowpos.flags & SET_WINDOW_POS_FLAGS.SWP_NOSIZE) == 0
                && windowpos.cx > 0
                && windowpos.cy > 0)
            {
                var rect = new Rect(windowpos.x, windowpos.y, windowpos.cx, windowpos.cy);

                var finalRect = this.isDragging
                    ? rect
                    : MonitorHelper.GetOnScreenPosition(rect, this.windowHandle, this.IgnoreTaskbarOnMaximize);
                windowpos.x = (int)finalRect.X;
                windowpos.y = (int)finalRect.Y;
                windowpos.cx = (int)finalRect.Width;
                windowpos.cy = (int)finalRect.Height;
                Marshal.StructureToPtr(windowpos, lParam, fDeleteOld: true);

                this.lastWindowpos = windowpos;
            }

            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical Marshal.PtrToStructure
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleWINDOWPOSCHANGED(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            // http://blogs.msdn.com/oldnewthing/archive/2008/01/15/7113860.aspx
            // The WM_WINDOWPOSCHANGED message is sent at the end of the window
            // state change process. It sort of combines the other state change
            // notifications, WM_MOVE, WM_SIZE, and WM_SHOWWINDOW. But it doesn't
            // suffer from the same limitations as WM_SHOWWINDOW, so you can
            // reliably use it to react to the window being shown or hidden.

            this._UpdateSystemMenu(null);

            // Still want to pass this to DefWndProc
            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleGETMINMAXINFO(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            /*
             * This is a workaround for wrong windows behaviour.
             * If a Window sets the WindoStyle to None and WindowState to maximized and we have a multi monitor system
             * we can move the Window only one time. After that it's not possible to move the Window back to the
             * previous monitor.
             * This fix is not really a full fix. Moving the Window back gives us the wrong size, because
             * MonitorFromWindow gives us the wrong (old) monitor! This is fixed in _HandleMoveForRealSize.
             */
            if (this.IgnoreTaskbarOnMaximize
                && PInvoke.IsZoomed(this.windowHandle))
            {
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

                var monitor = PInvoke.MonitorFromWindow(this.windowHandle, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    var monitorInfo = PInvoke.GetMonitorInfo(monitor);
                    var rcWorkArea = monitorInfo.rcWork;
                    var rcMonitorArea = monitorInfo.rcMonitor;

                    mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                    mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);

                    mmi.ptMaxSize.X = Math.Abs(monitorInfo.rcMonitor.GetWidth());
                    mmi.ptMaxSize.Y = Math.Abs(monitorInfo.rcMonitor.GetHeight());
                    mmi.ptMaxTrackSize.X = mmi.ptMaxSize.X;
                    mmi.ptMaxTrackSize.Y = mmi.ptMaxSize.Y;
                }

                Marshal.StructureToPtr(mmi, lParam, true);
            }

            /* Setting handled to false enables the application to process it's own Min/Max requirements,
             * as mentioned by jason.bullard (comment from September 22, 2011) on http://gallery.expression.microsoft.com/ZuneWindowBehavior/ */
            handled = false;

            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleMOVEForRealSize(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            /*
             * This is a workaround for wrong windows behaviour (with multi monitor system).
             * If a Window sets the WindowStyle to None and WindowState to maximized
             * we can move the Window to different monitor with maybe different dimension.
             * But after moving to the previous monitor we got a wrong size (from the old monitor dimension).
             */
            var state = this._GetHwndState();
            if (state == WindowState.Maximized)
            {
                var monitorFromWindow = PInvoke.MonitorFromWindow(this.windowHandle, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
                if (monitorFromWindow != IntPtr.Zero)
                {
                    var ignoreTaskBar = this.IgnoreTaskbarOnMaximize;
                    var monitorInfo = PInvoke.GetMonitorInfo(monitorFromWindow);
                    var rcMonitorArea = ignoreTaskBar ? monitorInfo.rcMonitor : monitorInfo.rcWork;
                    /*
                     * ASYNCWINDOWPOS
                     * If the calling thread and the thread that owns the window are attached to different input queues,
                     * the system posts the request to the thread that owns the window. This prevents the calling thread
                     * from blocking its execution while other threads process the request.
                     * 
                     * FRAMECHANGED
                     * Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window,
                     * even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only
                     * when the window's size is being changed.
                     * 
                     * NOCOPYBITS
                     * Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client
                     * area are saved and copied back into the client area after the window is sized or repositioned.
                     * 
                     */
                    PInvoke.SetWindowPos(this.windowHandle, default, rcMonitorArea.left, rcMonitorArea.top, rcMonitorArea.GetWidth(), rcMonitorArea.GetHeight(), SET_WINDOW_POS_FLAGS.SWP_ASYNCWINDOWPOS | SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOCOPYBITS);
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        private bool isTrackingMouseEvents;

        private unsafe IntPtr _HandleNCMOUSEMOVE(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            if (this.isTrackingMouseEvents == false)
            {
                var settings = new TRACKMOUSEEVENT
                {
                    cbSize = (uint)Marshal.SizeOf<TRACKMOUSEEVENT>(),
                    dwFlags = TRACKMOUSEEVENT_FLAGS.TME_LEAVE | TRACKMOUSEEVENT_FLAGS.TME_NONCLIENT,
                    hwndTrack = this.windowHandle
                };
                PInvoke.TrackMouseEvent(&settings);
                this.isTrackingMouseEvents = true;
            }

            handled = this.nonClientControlManager?.HoverTrackedControl(lParam) == true;

            return IntPtr.Zero;
        }

        private IntPtr _HandleMOUSEMOVE(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            this.nonClientControlManager?.ClearTrackedControl();
            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleNCMOUSELEAVE(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            this.isTrackingMouseEvents = false;
            this.nonClientControlManager?.ClearTrackedControl();
            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleMOUSELEAVE(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            this.nonClientControlManager?.ClearTrackedControl();
            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleStyleChanging(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            handled = false;

            if ((WINDOW_LONG_PTR_INDEX)wParam == WINDOW_LONG_PTR_INDEX.GWL_STYLE)
            {
                var structure = Marshal.PtrToStructure<STYLESTRUCT>(lParam);

                if (this.IgnoreTaskbarOnMaximize
                    && this._GetHwndState() == WindowState.Maximized)
                {
                    structure.styleNew |= (uint)WINDOW_STYLE.WS_OVERLAPPED;
                    //structure.styleNew &= (uint)~WINDOW_STYLE.WS_CAPTION;
                    //structure.styleNew &= (uint)~WINDOW_STYLE.WS_SYSMENU; // todo: must be removed for mica effect
                }
                else
                {
                    //structure.styleNew |= (uint)(WINDOW_STYLE.WS_OVERLAPPED | WINDOW_STYLE.WS_CAPTION);
                    //structure.styleNew &= (uint)~WINDOW_STYLE.WS_SYSMENU; // todo: must be removed for mica effect
                }

                Marshal.StructureToPtr(structure, lParam, fDeleteOld: true);
            }

            return IntPtr.Zero;
        }

        private IntPtr _HandleDestroy(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            handled = false;

            this.Detach();

            return IntPtr.Zero;
        }

        private IntPtr _HandleNCLBUTTONDOWN(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            handled = this.nonClientControlManager?.PressTrackedControl(lParam) == true;

            return IntPtr.Zero;
        }

        private IntPtr _HandleNCLBUTTONUP(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            handled = this.nonClientControlManager?.ClickTrackedControl(lParam) == true;

            return IntPtr.Zero;
        }

        private IntPtr _HandleNCRBUTTONMessages(WM uMsg, nuint wParam, nint lParam, out bool handled)
        {
            handled = true;
            PInvoke.RaiseNonClientMouseMessageAsClient(this.windowHandle, uMsg, wParam, lParam);

            return IntPtr.Zero;
        }

        #endregion

        /// <summary>Add and remove a native WindowStyle from the HWND.</summary>
        /// <param name="removeStyle">The styles to be removed.  These can be bitwise combined.</param>
        /// <param name="addStyle">The styles to be added.  These can be bitwise combined.</param>
        /// <returns>Whether the styles of the HWND were modified as a result of this call.</returns>
        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private bool _ModifyStyle(WINDOW_STYLE removeStyle, WINDOW_STYLE addStyle)
        {
            var dwStyle = PInvoke.GetWindowStyle(this.windowHandle);
            var dwNewStyle = (dwStyle & ~removeStyle) | addStyle;
            if (dwStyle == dwNewStyle)
            {
                return false;
            }

            PInvoke.SetWindowStyle(this.windowHandle, dwNewStyle);
            return true;
        }

        /// <summary>
        /// Get the WindowState as the native HWND knows it to be.  This isn't necessarily the same as what Window thinks.
        /// </summary>
        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private WindowState _GetHwndState()
        {
            var wpl = PInvoke.GetWindowPlacement(this.windowHandle);
            switch (wpl.showCmd)
            {
                case SHOW_WINDOW_CMD.SW_SHOWMINIMIZED:
                    return WindowState.Minimized;

                case SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED:
                    return WindowState.Maximized;
            }

            return WindowState.Normal;
        }

        /// <summary>
        /// Get the bounding rectangle for the window in physical coordinates.
        /// </summary>
        /// <returns>The bounding rectangle for the window.</returns>
        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private Rect _GetWindowRect()
        {
            // Get the window rectangle.
            var windowPosition = PInvoke.GetWindowRect(this.windowHandle);
            return new Rect(windowPosition.left, windowPosition.top, windowPosition.GetWidth(), windowPosition.GetHeight());
        }

        /// <summary>
        /// Update the items in the system menu based on the current, or assumed, WindowState.
        /// </summary>
        /// <param name="assumeState">
        /// The state to assume that the Window is in.  This can be null to query the Window's state.
        /// </param>
        /// <remarks>
        /// We want to update the menu while we have some control over whether the caption will be repainted.
        /// </remarks>
        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _UpdateSystemMenu(WindowState? assumeState)
        {
            const MENU_ITEM_FLAGS MF_ENABLED = MENU_ITEM_FLAGS.MF_ENABLED | MENU_ITEM_FLAGS.MF_BYCOMMAND;
            const MENU_ITEM_FLAGS MF_DISABLED = MENU_ITEM_FLAGS.MF_GRAYED | MENU_ITEM_FLAGS.MF_DISABLED | MENU_ITEM_FLAGS.MF_BYCOMMAND;

            var state = assumeState ?? this._GetHwndState();

            if (assumeState is not null
                || this.lastMenuState != state)
            {
                this.lastMenuState = state;

                var menuHandle = PInvoke.GetSystemMenu(this.windowHandle, false);
                if (menuHandle != IntPtr.Zero)
                {
                    var dwStyle = PInvoke.GetWindowStyle(this.windowHandle);

                    var canMinimize = Utility.IsFlagSet((int)dwStyle, (int)WINDOW_STYLE.WS_MINIMIZEBOX);
                    var canMaximize = Utility.IsFlagSet((int)dwStyle, (int)WINDOW_STYLE.WS_MAXIMIZEBOX);
                    var canSize = Utility.IsFlagSet((int)dwStyle, (int)WINDOW_STYLE.WS_THICKFRAME);

                    switch (state)
                    {
                        case WindowState.Maximized:
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.RESTORE, canMaximize ? MF_ENABLED : MF_DISABLED);
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.MOVE, MF_DISABLED);
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.SIZE, MF_DISABLED);
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.MINIMIZE, canMinimize ? MF_ENABLED : MF_DISABLED);
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.MAXIMIZE, MF_DISABLED);
                            break;

                        case WindowState.Minimized:
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.RESTORE, MF_ENABLED);
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.MOVE, MF_DISABLED);
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.SIZE, MF_DISABLED);
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.MINIMIZE, MF_DISABLED);
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.MAXIMIZE, canMaximize ? MF_ENABLED : MF_DISABLED);
                            break;

                        default:
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.RESTORE, MF_DISABLED);
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.MOVE, MF_ENABLED);
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.SIZE, canSize ? MF_ENABLED : MF_DISABLED);
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.MINIMIZE, canMinimize ? MF_ENABLED : MF_DISABLED);
                            PInvoke.EnableMenuItem(menuHandle, (uint)SC.MAXIMIZE, canMaximize ? MF_ENABLED : MF_DISABLED);
                            break;
                    }
                }
            }
        }

        private void UpdateWindowStyle()
        {
            if (this.IgnoreTaskbarOnMaximize
                && this._GetHwndState() == WindowState.Maximized)
            {
                this._ModifyStyle(WINDOW_STYLE.WS_CAPTION, 0);
            }
            else
            {
                //this._ModifyStyle(WINDOW_STYLE.WS_SYSMENU, WINDOW_STYLE.WS_CAPTION);
                //this._ModifyStyle(0, WINDOW_STYLE.WS_CAPTION); // todo: mica
            }
        }

        /// <summary>
        /// Matrix of the HT values to return when responding to NC window messages.
        /// </summary>
        private static readonly HT[,] hitTestBorders =
                                                       {
                                                            { HT.TOPLEFT,    HT.TOP,     HT.TOPRIGHT },
                                                            { HT.LEFT,       HT.CLIENT,  HT.RIGHT },
                                                            { HT.BOTTOMLEFT, HT.BOTTOM,  HT.BOTTOMRIGHT },
                                                       };

        private HT _HitTestNca(Rect windowRect, Point mousePosition)
        {
            // Determine if hit test is for resizing, default middle (1,1).
            var uRow = 1;
            var uCol = 1;
            var onTopResizeBorder = false;

            // Only get this once from the property to improve performance
            var resizeBorderThickness = this.ResizeBorderThickness;

            // Allow resize of up to some pixels inside the window itself
            onTopResizeBorder = mousePosition.Y <= (windowRect.Top + this.ResizeBorderThickness.Top);

            // Determine if the point is at the top or bottom of the window.
            uRow = GetHTRow(windowRect, mousePosition, resizeBorderThickness);

            // Determine if the point is at the left or right of the window.
            uCol = GetHTColumn(windowRect, mousePosition, resizeBorderThickness);

            // If the cursor is in one of the top edges by the caption bar, but below the top resize border,
            // then resize left-right rather than diagonally.
            if (uRow == 0
                && uCol != 1
                && onTopResizeBorder == false)
            {
                uRow = 1;
            }

            if (uRow != 1
                && uCol == 1)
            {
                uCol = GetHTColumn(windowRect, mousePosition, this.cornerGripThickness);
            }
            else if (uCol != 1
                     && uRow == 1)
            {
                uRow = GetHTRow(windowRect, mousePosition, this.cornerGripThickness);
            }

            var ht = hitTestBorders[uRow, uCol];

            if ((ht is HT.TOPLEFT or HT.TOP or HT.TOPRIGHT)
                && onTopResizeBorder == false)
            {
                ht = HT.CAPTION;
            }

            return ht;

            static int GetHTRow(Rect windowRect, Point mousePosition, Thickness resizeBorderThickness)
            {
                if (mousePosition.Y >= windowRect.Top
                    && mousePosition.Y < windowRect.Top + resizeBorderThickness.Top)
                {
                    return 0; // top (caption or resize border)
                }

                if (mousePosition.Y < windowRect.Bottom
                    && mousePosition.Y >= windowRect.Bottom - resizeBorderThickness.Bottom)
                {
                    return 2; // bottom
                }

                return 1;
            }

            static int GetHTColumn(Rect windowRect, Point mousePosition, Thickness resizeBorderThickness)
            {
                if (mousePosition.X >= windowRect.Left
                    && mousePosition.X < windowRect.Left + resizeBorderThickness.Left)
                {
                    return 0; // left side
                }

                if (mousePosition.X < windowRect.Right
                    && mousePosition.X >= windowRect.Right - resizeBorderThickness.Right)
                {
                    return 2; // right side
                }

                return 1;
            }
        }
    }
}