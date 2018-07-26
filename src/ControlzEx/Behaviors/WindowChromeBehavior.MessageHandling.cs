#pragma warning disable 618
namespace ControlzEx.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Windows;
    using System.Windows.Data;
    using ControlzEx.Standard;
    using ControlzEx.Windows.Shell;
    using HANDLE_MESSAGE = System.Collections.Generic.KeyValuePair<Standard.WM, Standard.MessageHandler>;

    public partial class WindowChromeBehavior
    {
        #region Fields

        private const SWP SwpFlags = SWP.FRAMECHANGED | SWP.NOSIZE | SWP.NOMOVE | SWP.NOZORDER | SWP.NOOWNERZORDER | SWP.NOACTIVATE;

        private readonly List<HANDLE_MESSAGE> messageTable;

        // Keep track of this so we can detect when we need to apply changes.  Tracking these separately
        // as I've seen using just one cause things to get enough out of [....] that occasionally the caption will redraw.
        private WindowState lastMenuState;

        private WINDOWPOS previousWp;

        #endregion

        /// <SecurityNote>
        ///   Critical : Store critical methods in critical callback table
        ///   Safe     : Demands full trust permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public WindowChromeBehavior()
        {
            // Effective default values for some of these properties are set to be bindings
            // that set them to system defaults.
            // A more correct way to do this would be to Coerce the value iff the source of the DP was the default value.
            // Unfortunately with the current property system we can't detect whether the value being applied at the time
            // of the coersion is the default.
            foreach (var bp in _BoundProperties)
            {
                // This list must be declared after the DP's are assigned.
                Assert.IsNotNull(bp.DependencyProperty);
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

            this.messageTable = new List<HANDLE_MESSAGE>
                            {
                                new HANDLE_MESSAGE(WM.NCUAHDRAWCAPTION,       this._HandleNCUAHDRAWCAPTION),
                                new HANDLE_MESSAGE(WM.SETTEXT,                this._HandleSETICONOrSETTEXT),
                                new HANDLE_MESSAGE(WM.SETICON,                this._HandleSETICONOrSETTEXT),
                                new HANDLE_MESSAGE(WM.SYSCOMMAND,             this._HandleSYSCOMMAND),
                                new HANDLE_MESSAGE(WM.NCACTIVATE,             this._HandleNCACTIVATE),
                                new HANDLE_MESSAGE(WM.NCCALCSIZE,             this._HandleNCCALCSIZE),
                                new HANDLE_MESSAGE(WM.NCHITTEST,              this._HandleNCHITTEST),
                                new HANDLE_MESSAGE(WM.NCPAINT,                this._HandleNCPAINT),
                                new HANDLE_MESSAGE(WM.NCRBUTTONUP,            this._HandleNCRBUTTONUP),
                                new HANDLE_MESSAGE(WM.SIZE,                   this._HandleSIZE),
                                new HANDLE_MESSAGE(WM.WINDOWPOSCHANGING,      this._HandleWINDOWPOSCHANGING),
                                new HANDLE_MESSAGE(WM.WINDOWPOSCHANGED,       this._HandleWINDOWPOSCHANGED),
                                new HANDLE_MESSAGE(WM.GETMINMAXINFO,          this._HandleGETMINMAXINFO),
                                new HANDLE_MESSAGE(WM.DWMCOMPOSITIONCHANGED,  this._HandleDWMCOMPOSITIONCHANGED),
                                new HANDLE_MESSAGE(WM.ENTERSIZEMOVE,          this._HandleENTERSIZEMOVEForAnimation),
                                new HANDLE_MESSAGE(WM.EXITSIZEMOVE,           this._HandleEXITSIZEMOVEForAnimation),
                                new HANDLE_MESSAGE(WM.MOVE,                   this._HandleMOVEForRealSize),                                
                                new HANDLE_MESSAGE(WM.DPICHANGED,             this._HandleDPICHANGED)
                            };
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        ///   Safe     : Demands full trust permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void _OnChromePropertyChangedThatRequiresRepaint()
        {
            this._UpdateFrameState(true);
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _ApplyNewCustomChrome()
        {
            if (this.windowHandle == IntPtr.Zero 
                || this.hwndSource.IsDisposed)
            {
                // Not yet hooked.
                return;
            }

            // Force this the first time.
            this._UpdateSystemMenu(this.AssociatedObject.WindowState);
            this._UpdateFrameState(true);

            if (this.hwndSource.IsDisposed)
            {
                // If the window got closed very early
                this.Cleanup(true);
                return;
            }
        }

        // A borderless window lost his animation, with this we bring it back.
        private bool MinimizeAnimation => SystemParameters.MinimizeAnimation && this.IgnoreTaskbarOnMaximize == false;

        #region WindowProc and Message Handlers

        /// <SecurityNote>
        ///   Critical : Accesses critical _hwnd
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Only expecting messages for our cached HWND.
            Assert.AreEqual(hwnd, this.windowHandle);

            // Check if window has a RootVisual to workaround issue #13 (Win32Exception on closing window).
            // RootVisual gets cleared when the window is closing. This happens in CloseWindowFromWmClose of the Window class.
            if (this.hwndSource?.RootVisual is null)
            {
                return IntPtr.Zero;
            }

            var message = (WM)msg;
            foreach (var handlePair in this.messageTable)
            {
                if (handlePair.Key == message)
                {
                    return handlePair.Value(message, wParam, lParam, out handled);
                }
            }
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleNCUAHDRAWCAPTION(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            if (false == this.AssociatedObject.ShowInTaskbar 
                && this._GetHwndState() == WindowState.Minimized)
            {
                var modified = this._ModifyStyle(WS.VISIBLE, 0);

                // Minimize the window with ShowInTaskbar == false cause Windows to redraw the caption.
                // Letting the default WndProc handle the message without the WS_VISIBLE
                // style applied bypasses the redraw.
                var lRet = NativeMethods.DefWindowProc(this.windowHandle, uMsg, wParam, lParam);

                // Put back the style we removed.
                if (modified)
                {
                    this._ModifyStyle(0, WS.VISIBLE);
                }
                handled = true;
                return lRet;
            }
            else
            {
                handled = false;
                return IntPtr.Zero;
            }
        }

        private IntPtr _HandleSETICONOrSETTEXT(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            var modified = this._ModifyStyle(WS.VISIBLE, 0);

            // Setting the caption text and icon cause Windows to redraw the caption.
            // Letting the default WndProc handle the message without the WS_VISIBLE
            // style applied bypasses the redraw.
            var lRet = NativeMethods.DefWindowProc(this.windowHandle, uMsg, wParam, lParam);

            // Put back the style we removed.
            if (modified)
            {
                this._ModifyStyle(0, WS.VISIBLE);
            }
            handled = true;
            return lRet;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleSYSCOMMAND(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            var wpl = NativeMethods.GetWindowPlacement(this.windowHandle);
            var currentState = wpl.showCmd;
            var systemCommand = (SC)(Environment.Is64BitProcess ? wParam.ToInt64() : wParam.ToInt32());

            if (this.MinimizeAnimation
                && systemCommand == SC.RESTORE 
                && currentState == SW.SHOWMAXIMIZED)
            {
                var modified = this._ModifyStyle(WS.SYSMENU, 0);

                var lRet = NativeMethods.DefWindowProc(this.windowHandle, uMsg, wParam, lParam);

                // Put back the style we removed.
                if (modified)
                {
                    this._ModifyStyle(0, WS.SYSMENU);
                }

                handled = true;
                return lRet;
            }
            else
            {
                handled = false;
                return IntPtr.Zero;
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleNCACTIVATE(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // Despite MSDN's documentation of lParam not being used, 
            // calling DefWindowProc with lParam set to -1 causes Windows not to draw over the caption. 
 
            // Directly call DefWindowProc with a custom parameter 
            // which bypasses any other handling of the message. 
            var lRet = NativeMethods.DefWindowProc(this.windowHandle, WM.NCACTIVATE, wParam, new IntPtr(-1)); 
            // We don't have any non client area, so we can just discard this message by handling it 
            handled = true; 
 
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
            var hwnd = NativeMethods.GetTaskBarHandleForMonitor(monitorContainingApplication);

            if (hwnd == IntPtr.Zero)
            {
                return area;
            }

            var abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = hwnd;
            NativeMethods.SHAppBarMessage((int)ABMsg.ABM_GETTASKBARPOS, ref abd);
            var autoHide = Convert.ToBoolean(NativeMethods.SHAppBarMessage((int)ABMsg.ABM_GETSTATE, ref abd));

            if (autoHide == false)
            {
                return area;
            }

            switch (abd.uEdge)
            {
                case (int)ABEdge.ABE_LEFT:
                    area.Left += 2;
                    break;
                case (int)ABEdge.ABE_RIGHT:
                    area.Right -= 2;
                    break;
                case (int)ABEdge.ABE_TOP:
                    area.Top += 2;
                    break;
                case (int)ABEdge.ABE_BOTTOM:
                    area.Bottom -= 2;
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
        // At least on RTM Win7 we can avoid the problem by making the client area not extactly match the non-client
        // area, so we added the SacrificialEdge property.
        /// <SecurityNote>
        ///   Critical : Calls critical Marshal.PtrToStructure
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleNCCALCSIZE(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // lParam is an [in, out] that can be either a RECT* (wParam == FALSE) or an NCCALCSIZE_PARAMS*.
            // Since the first field of NCCALCSIZE_PARAMS is a RECT and is the only field we care about
            // we can unconditionally treat it as a RECT.

            if (this.dpiChanged)
            {
                this.dpiChanged = false;
                handled = this.IgnoreTaskbarOnMaximize == false;
                return IntPtr.Zero;
            }

            if (this.MinimizeAnimation
                && this._GetHwndState() == WindowState.Maximized)
            {
                var monitor = NativeMethods.MonitorFromWindow(this.windowHandle, MonitorOptions.MONITOR_DEFAULTTONEAREST);
                var monitorInfo = NativeMethods.GetMonitorInfo(monitor);
                var monitorRect = this.IgnoreTaskbarOnMaximize ? monitorInfo.rcMonitor : monitorInfo.rcWork;

                var rc = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
                rc.Left = monitorRect.Left;
                rc.Top = monitorRect.Top;
                rc.Right = monitorRect.Right;
                rc.Bottom = monitorRect.Bottom;

                // monitor and work area will be equal if taskbar is hidden
                if (monitorInfo.rcMonitor.Height == monitorInfo.rcWork.Height
                    && monitorInfo.rcMonitor.Width == monitorInfo.rcWork.Width)
                {
                    rc = AdjustWorkingAreaForAutoHide(monitor, rc);
                }

                Marshal.StructureToPtr(rc, lParam, true);
            }
            else if (this.TryToBeFlickerFree
                     && this._GetHwndState() == WindowState.Normal
                     && wParam.ToInt32() != 0)
            {
                var rc = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));

                // We have to add or remove one pixel on any side of the window to force a flicker free resize.
                // Removing pixels would result in a smaller client area.
                // Adding pixels does not seem to really increase the client area.
                rc.Bottom += 1;

                Marshal.StructureToPtr(rc, lParam, true);
            }

            handled = true;            

            // Per MSDN for NCCALCSIZE, always return 0 when wParam == FALSE
            // 
            // Returning 0 when wParam == TRUE is not appropriate - it will preserve
            // the old client area and align it with the upper-left corner of the new 
            // client area. So we simply ask for a redraw (WVR_REDRAW)

            var retVal = IntPtr.Zero;
            if (wParam.ToInt32() != 0) // wParam == TRUE
            {
                // Using the combination of WVR.VALIDRECTS and WVR.REDRAW gives the smoothest
                // resize behavior we can achieve here.
                retVal = new IntPtr((int)(WVR.VALIDRECTS | WVR.REDRAW));
            }

            return retVal;
        }

        private HT _GetHTFromResizeGripDirection(ResizeGripDirection direction)
        {
            var compliment = this.AssociatedObject.FlowDirection == FlowDirection.RightToLeft;
            switch (direction)
            {
                case ResizeGripDirection.Bottom:
                    return HT.BOTTOM;
                case ResizeGripDirection.BottomLeft:
                    return compliment ? HT.BOTTOMRIGHT : HT.BOTTOMLEFT;
                case ResizeGripDirection.BottomRight:
                    return compliment ? HT.BOTTOMLEFT : HT.BOTTOMRIGHT;
                case ResizeGripDirection.Left:
                    return compliment ? HT.RIGHT : HT.LEFT;
                case ResizeGripDirection.Right:
                    return compliment ? HT.LEFT : HT.RIGHT;
                case ResizeGripDirection.Top:
                    return HT.TOP;
                case ResizeGripDirection.TopLeft:
                    return compliment ? HT.TOPRIGHT : HT.TOPLEFT;
                case ResizeGripDirection.TopRight:
                    return compliment ? HT.TOPLEFT : HT.TOPRIGHT;
                case ResizeGripDirection.Caption:
                    return HT.CAPTION;
                default:
                    return HT.NOWHERE;
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleNCPAINT(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            handled = false;

            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleNCHITTEST(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            var dpi = this.AssociatedObject.GetDpi();

            // Let the system know if we consider the mouse to be in our effective non-client area.
            var mousePosScreen = Utility.GetPoint(lParam); //new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam));
            var windowPosition = this._GetWindowRect();

            var mousePosWindow = mousePosScreen;
            mousePosWindow.Offset(-windowPosition.X, -windowPosition.Y);
            mousePosWindow = DpiHelper.DevicePixelsToLogical(mousePosWindow, dpi.DpiScaleX, dpi.DpiScaleY);

            // If the app is asking for content to be treated as client then that takes precedence over _everything_, even DWM caption buttons.
            // This allows apps to set the glass frame to be non-empty, still cover it with WPF content to hide all the glass,
            // yet still get DWM to draw a drop shadow.
            var inputElement = this.AssociatedObject.InputHitTest(mousePosWindow);
            if (inputElement != null)
            {
                if (WindowChrome.GetIsHitTestVisibleInChrome(inputElement))
                {
                    handled = true;
                    return new IntPtr((int)HT.CLIENT);
                }

                var direction = WindowChrome.GetResizeGripDirection(inputElement);
                if (direction != ResizeGripDirection.None)
                {
                    handled = true;
                    return new IntPtr((int)this._GetHTFromResizeGripDirection(direction));
                }
            }

            var ht = this._HitTestNca(DpiHelper.DeviceRectToLogical(windowPosition, dpi.DpiScaleX, dpi.DpiScaleY),
                                      DpiHelper.DevicePixelsToLogical(mousePosScreen, dpi.DpiScaleX, dpi.DpiScaleY));

            handled = true;
            return new IntPtr((int)ht);
        }

        /// <SecurityNote>
        ///   Critical : Calls critical method
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleNCRBUTTONUP(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // Emulate the system behavior of clicking the right mouse button over the caption area
            // to bring up the system menu.
            if (HT.CAPTION == (HT)(Environment.Is64BitProcess ? wParam.ToInt64() : wParam.ToInt32()))
            {
                //SystemCommands.ShowSystemMenuPhysicalCoordinates(_window, new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam)));
                Windows.Shell.SystemCommands.ShowSystemMenuPhysicalCoordinates(this.AssociatedObject, Utility.GetPoint(lParam));
            }
            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical method
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleSIZE(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            const int SIZE_MAXIMIZED = 2;

            // Force when maximized.
            // We can tell what's happening right now, but the Window doesn't yet know it's
            // maximized.  Not forcing this update will eventually cause the
            // default caption to be drawn.
            WindowState? state = null;
            if ((Environment.Is64BitProcess ? wParam.ToInt64() : wParam.ToInt32()) == SIZE_MAXIMIZED)
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
        private IntPtr _HandleWINDOWPOSCHANGING(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            Assert.IsNotDefault(lParam);
            var wp = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

            // we don't do bitwise operations cuz we're checking for this flag being the only one there
            // I have no clue why this works, I tried this because VS2013 has this flag removed on fullscreen window movws
            if (this.IgnoreTaskbarOnMaximize 
                && this._GetHwndState() == WindowState.Maximized 
                && wp.flags == SWP.FRAMECHANGED)
            {
                wp.flags = 0;
                Marshal.StructureToPtr(wp, lParam, true);

                handled = true;
                return IntPtr.Zero;
            }            

            if ((wp.flags & SWP.NOMOVE) != 0)
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

            var changedPos = false;

            // Convert the original to original size based on DPI setting. Need for x% screen DPI.
            var matrix = this.hwndSource.CompositionTarget.TransformToDevice;

            var minWidth = wnd.MinWidth * matrix.M11;
            var minHeight = wnd.MinHeight * matrix.M22;
            if (wp.cx < minWidth) { wp.cx = (int)minWidth; changedPos = true; }
            if (wp.cy < minHeight) { wp.cy = (int)minHeight; changedPos = true; }

            var maxWidth = wnd.MaxWidth * matrix.M11;
            var maxHeight = wnd.MaxHeight * matrix.M22;
            if (wp.cx > maxWidth && maxWidth > 0) { wp.cx = (int)Math.Round(maxWidth); changedPos = true; }
            if (wp.cy > maxHeight && maxHeight > 0) { wp.cy = (int)Math.Round(maxHeight); changedPos = true; }

            if (changedPos == false)
            {
                handled = false;
                return IntPtr.Zero;
            }

            Marshal.StructureToPtr(wp, lParam, true);

            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical Marshal.PtrToStructure
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleWINDOWPOSCHANGED(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // http://blogs.msdn.com/oldnewthing/archive/2008/01/15/7113860.aspx
            // The WM_WINDOWPOSCHANGED message is sent at the end of the window
            // state change process. It sort of combines the other state change
            // notifications, WM_MOVE, WM_SIZE, and WM_SHOWWINDOW. But it doesn't
            // suffer from the same limitations as WM_SHOWWINDOW, so you can
            // reliably use it to react to the window being shown or hidden.

            this._UpdateSystemMenu(null);

            Assert.IsNotDefault(lParam);
            var wp = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

            if (wp.Equals(this.previousWp) == false)
            {
                this.previousWp = wp;
                this._SetRegion(wp);
            }

            this.previousWp = wp;

            //                if (wp.Equals(_previousWP) && wp.flags.Equals(_previousWP.flags))
            //                {
            //                    handled = true;
            //                    return IntPtr.Zero;
            //                }

            // Still want to pass this to DefWndProc
            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleGETMINMAXINFO(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
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
                && NativeMethods.IsZoomed(this.windowHandle))
            {
                var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
                var monitor = NativeMethods.MonitorFromWindow(this.windowHandle, MonitorOptions.MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    var monitorInfo = NativeMethods.GetMonitorInfoW(monitor);
                    var rcWorkArea = monitorInfo.rcWork;
                    var rcMonitorArea = monitorInfo.rcMonitor;

                    mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                    mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);

                    mmi.ptMaxSize.X = Math.Abs(monitorInfo.rcMonitor.Width);
                    mmi.ptMaxSize.Y = Math.Abs(monitorInfo.rcMonitor.Height);
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
        private IntPtr _HandleDWMCOMPOSITIONCHANGED(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            this._UpdateFrameState(false);

            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleENTERSIZEMOVEForAnimation(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            if (this.MinimizeAnimation)// && _GetHwndState() != WindowState.Minimized)
            {
                /* we only need to remove DLGFRAME ( CAPTION = BORDER | DLGFRAME )
                 * to prevent nasty drawing
                 * removing border will cause a 1 off error on the client rect size
                 * when maximizing via aero snapping, because max by aero snapping
                 * will call this method, resulting in a 2px black border on the side
                 * when maximized.
                 */
                this._ModifyStyle(WS.CAPTION, 0);
            }
            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleEXITSIZEMOVEForAnimation(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            if (this.MinimizeAnimation)
            {
                // restore DLGFRAME
                this._ModifyStyle(0, WS.CAPTION);
            }

            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleMOVEForRealSize(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            /*
             * This is a workaround for wrong windows behaviour (with multi monitor system).
             * If a Window sets the WindoStyle to None and WindowState to maximized
             * we can move the Window to different monitor with maybe different dimension.
             * But after moving to the previous monitor we got a wrong size (from the old monitor dimension).
             */
            var state = this._GetHwndState();
            if (state == WindowState.Maximized)
            {
                var monitorFromWindow = NativeMethods.MonitorFromWindow(this.windowHandle, MonitorOptions.MONITOR_DEFAULTTONEAREST);
                if (monitorFromWindow != IntPtr.Zero)
                {
                    var ignoreTaskBar = this.IgnoreTaskbarOnMaximize;
                    var monitorInfo = NativeMethods.GetMonitorInfoW(monitorFromWindow);
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
                    NativeMethods.SetWindowPos(this.windowHandle, IntPtr.Zero, rcMonitorArea.Left, rcMonitorArea.Top, rcMonitorArea.Width, rcMonitorArea.Height, SWP.ASYNCWINDOWPOS | SWP.FRAMECHANGED | SWP.NOCOPYBITS);
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleDPICHANGED(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            this.dpiChanged = true;

            if (this._GetHwndState() == WindowState.Normal)
            {
                var rect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
                rect.Bottom += 1;
                Marshal.StructureToPtr(rect, lParam, true);
            }

            handled = false;
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
        private bool _ModifyStyle(WS removeStyle, WS addStyle)
        {
            Assert.IsNotDefault(this.windowHandle);
            var dwStyle = NativeMethods.GetWindowStyle(this.windowHandle);
            var dwNewStyle = (dwStyle & ~removeStyle) | addStyle;
            if (dwStyle == dwNewStyle)
            {
                return false;
            }

            NativeMethods.SetWindowStyle(this.windowHandle, dwNewStyle);
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
            var wpl = NativeMethods.GetWindowPlacement(this.windowHandle);
            switch (wpl.showCmd)
            {
                case SW.SHOWMINIMIZED: 
                    return WindowState.Minimized;

                case SW.SHOWMAXIMIZED: 
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
            var windowPosition = NativeMethods.GetWindowRect(this.windowHandle);
            return new Rect(windowPosition.Left, windowPosition.Top, windowPosition.Width, windowPosition.Height);
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
            const MF MF_ENABLED = MF.ENABLED | MF.BYCOMMAND;
            const MF MF_DISABLED = MF.GRAYED | MF.DISABLED | MF.BYCOMMAND;

            var state = assumeState ?? this._GetHwndState();

            if (null != assumeState 
                || this.lastMenuState != state)
            {
                this.lastMenuState = state;

                var hmenu = NativeMethods.GetSystemMenu(this.windowHandle, false);
                if (IntPtr.Zero != hmenu)
                {
                    var dwStyle = NativeMethods.GetWindowStyle(this.windowHandle);

                    var canMinimize = Utility.IsFlagSet((int)dwStyle, (int)WS.MINIMIZEBOX);
                    var canMaximize = Utility.IsFlagSet((int)dwStyle, (int)WS.MAXIMIZEBOX);
                    var canSize = Utility.IsFlagSet((int)dwStyle, (int)WS.THICKFRAME);

                    switch (state)
                    {
                        case WindowState.Maximized:
                            NativeMethods.EnableMenuItem(hmenu, SC.RESTORE, MF_ENABLED);
                            NativeMethods.EnableMenuItem(hmenu, SC.MOVE, MF_DISABLED);
                            NativeMethods.EnableMenuItem(hmenu, SC.SIZE, MF_DISABLED);
                            NativeMethods.EnableMenuItem(hmenu, SC.MINIMIZE, canMinimize ? MF_ENABLED : MF_DISABLED);
                            NativeMethods.EnableMenuItem(hmenu, SC.MAXIMIZE, MF_DISABLED);
                            break;

                        case WindowState.Minimized:
                            NativeMethods.EnableMenuItem(hmenu, SC.RESTORE, MF_ENABLED);
                            NativeMethods.EnableMenuItem(hmenu, SC.MOVE, MF_DISABLED);
                            NativeMethods.EnableMenuItem(hmenu, SC.SIZE, MF_DISABLED);
                            NativeMethods.EnableMenuItem(hmenu, SC.MINIMIZE, MF_DISABLED);
                            NativeMethods.EnableMenuItem(hmenu, SC.MAXIMIZE, canMaximize ? MF_ENABLED : MF_DISABLED);
                            break;

                        default:
                            NativeMethods.EnableMenuItem(hmenu, SC.RESTORE, MF_DISABLED);
                            NativeMethods.EnableMenuItem(hmenu, SC.MOVE, MF_ENABLED);
                            NativeMethods.EnableMenuItem(hmenu, SC.SIZE, canSize ? MF_ENABLED : MF_DISABLED);
                            NativeMethods.EnableMenuItem(hmenu, SC.MINIMIZE, canMinimize ? MF_ENABLED : MF_DISABLED);
                            NativeMethods.EnableMenuItem(hmenu, SC.MAXIMIZE, canMaximize ? MF_ENABLED : MF_DISABLED);
                            break;
                    }
                }
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _UpdateFrameState(bool force)
        {
            if (IntPtr.Zero == this.windowHandle 
                || this.hwndSource.IsDisposed)
            {
                return;
            }

            if (force)
            {
                if (this.hwndSource.IsDisposed)
                {
                    // If the window got closed very early
                    return;
                }

                if (this.MinimizeAnimation)
                {
                    // allow animation
                    this._ModifyStyle(0, WS.CAPTION);
                }
                else
                {
                    // no animation
                    this._ModifyStyle(WS.CAPTION, 0);
                }

                NativeMethods.SetWindowPos(this.windowHandle, IntPtr.Zero, 0, 0, 0, 0, SwpFlags);
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _ClearRegion()
        {
            NativeMethods.SetWindowRgn(this.windowHandle, IntPtr.Zero, NativeMethods.IsWindowVisible(this.windowHandle));
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private RECT _GetClientRectRelativeToWindowRect(IntPtr hWnd)
        {
            var windowRect = NativeMethods.GetWindowRect(hWnd);
            var clientRect = NativeMethods.GetClientRect(hWnd);

            var test = new POINT { X = 0, Y = 0 };
            NativeMethods.ClientToScreen(hWnd, ref test);
            if (this.AssociatedObject.FlowDirection == FlowDirection.RightToLeft)
            {
                clientRect.Offset(windowRect.Right - test.X, test.Y - windowRect.Top);
            }
            else
            {
                clientRect.Offset(test.X - windowRect.Left, test.Y - windowRect.Top);
            }
            return clientRect;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _SetRegion(WINDOWPOS? wp)
        {
            // We're early - WPF hasn't necessarily updated the state of the window.
            // Need to query it ourselves.
            var wpl = NativeMethods.GetWindowPlacement(this.windowHandle);

            if (wpl.showCmd == SW.SHOWMAXIMIZED)
            {
                RECT rcMax;
                if (this.MinimizeAnimation)
                {
                    rcMax = this._GetClientRectRelativeToWindowRect(this.windowHandle);
                }
                else
                {
                    int left;
                    int top;

                    if (wp.HasValue)
                    {
                        left = wp.Value.x;
                        top = wp.Value.y;
                    }
                    else
                    {
                        var r = this._GetWindowRect();
                        left = (int)r.Left;
                        top = (int)r.Top;
                    }

                    var hMon = NativeMethods.MonitorFromWindow(this.windowHandle, MonitorOptions.MONITOR_DEFAULTTONEAREST);

                    var mi = NativeMethods.GetMonitorInfo(hMon);
                    rcMax = this.IgnoreTaskbarOnMaximize
                                ? mi.rcMonitor
                                : mi.rcWork;
                    // The location of maximized window takes into account the border that Windows was
                    // going to remove, so we also need to consider it.
                    rcMax.Offset(-left, -top);
                }

                var hrgn = IntPtr.Zero;
                try
                {
                    hrgn = NativeMethods.CreateRectRgnIndirect(rcMax);
                    NativeMethods.SetWindowRgn(this.windowHandle, hrgn, NativeMethods.IsWindowVisible(this.windowHandle));
                    hrgn = IntPtr.Zero;
                }
                finally
                {
                    Utility.SafeDeleteObject(ref hrgn);
                }
            }
            else
            {
                this._ClearRegion();
            }
        }

        /// <summary>
        /// Matrix of the HT values to return when responding to NC window messages.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
        private static readonly HT[,] hitTestBorders = {
                                                            { HT.TOPLEFT,    HT.TOP,     HT.TOPRIGHT    },
                                                            { HT.LEFT,       HT.CLIENT,  HT.RIGHT       },
                                                            { HT.BOTTOMLEFT, HT.BOTTOM,  HT.BOTTOMRIGHT },
                                                       };

        private HT _HitTestNca(Rect windowPosition, Point mousePosition)
        {
            // Determine if hit test is for resizing, default middle (1,1).
            var uRow = 1;
            var uCol = 1;
            var onResizeBorder = false;

            // Determine if the point is at the top or bottom of the window.
            if (mousePosition.Y >= windowPosition.Top 
                && mousePosition.Y < windowPosition.Top + this.ResizeBorderThickness.Top)
            {
                onResizeBorder = (mousePosition.Y < (windowPosition.Top + this.ResizeBorderThickness.Top));
                uRow = 0; // top (caption or resize border)
            }
            else if (mousePosition.Y < windowPosition.Bottom 
                     && mousePosition.Y >= windowPosition.Bottom - (int)this.ResizeBorderThickness.Bottom)
            {
                uRow = 2; // bottom
            }

            // Determine if the point is at the left or right of the window.
            if (mousePosition.X >= windowPosition.Left 
                && mousePosition.X < windowPosition.Left + (int)this.ResizeBorderThickness.Left)
            {
                uCol = 0; // left side
            }
            else if (mousePosition.X < windowPosition.Right 
                     && mousePosition.X >= windowPosition.Right - this.ResizeBorderThickness.Right)
            {
                uCol = 2; // right side
            }

            // If the cursor is in one of the top edges by the caption bar, but below the top resize border,
            // then resize left-right rather than diagonally.
            if (uRow == 0 
                && uCol != 1 
                && onResizeBorder == false)
            {
                uRow = 1;
            }

            var ht = hitTestBorders[uRow, uCol];

            if (ht == HT.TOP 
                && onResizeBorder == false)
            {
                ht = HT.CAPTION;
            }

            return ht;
        }

        #region Remove Custom Chrome Methods

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _RestoreStandardChromeState(bool isClosing)
        {
            this.VerifyAccess();

            if (isClosing == true
                || this.hwndSource.IsDisposed == true
                || this.hwndSource.RootVisual is null)
            {
                return;
            }

            this._RestoreHrgn();

            this.AssociatedObject.InvalidateMeasure();
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _RestoreHrgn()
        {
            this._ClearRegion();
            NativeMethods.SetWindowPos(this.windowHandle, IntPtr.Zero, 0, 0, 0, 0, SwpFlags);
        }

        #endregion
    }
}