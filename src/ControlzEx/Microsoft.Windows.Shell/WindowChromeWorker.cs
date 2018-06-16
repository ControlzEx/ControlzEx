#pragma warning disable 1591, 618
/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace ControlzEx.Windows.Shell
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Threading;
    using ControlzEx.Standard;
    using HANDLE_MESSAGE = System.Collections.Generic.KeyValuePair<Standard.WM, Standard.MessageHandler>;

    internal class WindowChromeWorker : DependencyObject
    {
        // Delegate signature used for Dispatcher.BeginInvoke.
        private delegate void Action();

        #region Fields

        private const SWP SwpFlags = SWP.FRAMECHANGED | SWP.NOSIZE | SWP.NOMOVE | SWP.NOZORDER | SWP.NOOWNERZORDER | SWP.NOACTIVATE;

        private readonly List<HANDLE_MESSAGE> _messageTable;

        /// <summary>The Window that's chrome is being modified.</summary>
        private Window _window;

        /// <summary>Underlying HWND for the _window.</summary>
        /// <SecurityNote>
        ///   Critical : Critical member
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _hwnd;

        /// <summary>Underlying HWND for the _window.</summary>
        /// <SecurityNote>
        ///   Critical : Critical member provides access to HWND's window messages which are critical
        /// </SecurityNote>
        [SecurityCritical]
        private HwndSource _hwndSource;

        private bool _isHooked;

        // These fields are for tracking workarounds for WPF 3.5SP1 behaviors.
        private bool _isFixedUp;
        private bool _isUserResizing;
        private bool _hasUserMovedWindow;
        private Point _windowPosAtStartOfUserMove = default(Point);

        /// <summary>Object that describes the current modifications being made to the chrome.</summary>
        private WindowChrome _chromeInfo;

        // Keep track of this so we can detect when we need to apply changes.  Tracking these separately
        // as I've seen using just one cause things to get enough out of [....] that occasionally the caption will redraw.
        private WindowState _lastRoundingState;
        private WindowState _lastMenuState;
        private bool _isGlassEnabled;

        private WINDOWPOS _previousWp;

        #endregion

        /// <SecurityNote>
        ///   Critical : Store critical methods in critical callback table
        ///   Safe     : Demands full trust permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name="FullTrust")]
        public WindowChromeWorker()
        {
            this._messageTable = new List<HANDLE_MESSAGE>
            {
                new HANDLE_MESSAGE(WM.NCUAHDRAWCAPTION, this._HandleNCUAHDrawCaption),
                new HANDLE_MESSAGE(WM.SETTEXT, this._HandleSetTextOrIcon),
                new HANDLE_MESSAGE(WM.SETICON, this._HandleSetTextOrIcon),
                new HANDLE_MESSAGE(WM.SYSCOMMAND, this._HandleRestoreWindow),
                new HANDLE_MESSAGE(WM.NCACTIVATE, this._HandleNCActivate),
                new HANDLE_MESSAGE(WM.NCCALCSIZE, this._HandleNCCalcSize),
                new HANDLE_MESSAGE(WM.NCHITTEST, this._HandleNCHitTest),
                new HANDLE_MESSAGE(WM.NCRBUTTONUP, this._HandleNCRButtonUp),
                new HANDLE_MESSAGE(WM.SIZE, this._HandleSize),
                new HANDLE_MESSAGE(WM.WINDOWPOSCHANGING, this._HandleWindowPosChanging),   
                new HANDLE_MESSAGE(WM.WINDOWPOSCHANGED, this._HandleWindowPosChanged),
                new HANDLE_MESSAGE(WM.GETMINMAXINFO, this._HandleGetMinMaxInfo),
                new HANDLE_MESSAGE(WM.DWMCOMPOSITIONCHANGED, this._HandleDwmCompositionChanged),
                new HANDLE_MESSAGE(WM.ENTERSIZEMOVE, this._HandleEnterSizeMoveForAnimation),
                new HANDLE_MESSAGE(WM.MOVE, this._HandleMoveForRealSize),
                new HANDLE_MESSAGE(WM.EXITSIZEMOVE, this._HandleExitSizeMoveForAnimation),
            };

            if (Utility.IsPresentationFrameworkVersionLessThan4)
            {
                this._messageTable.AddRange(new[]
                {
                   new HANDLE_MESSAGE(WM.SETTINGCHANGE, this._HandleSettingChange),
                   new HANDLE_MESSAGE(WM.ENTERSIZEMOVE, this._HandleEnterSizeMove),
                   new HANDLE_MESSAGE(WM.EXITSIZEMOVE, this._HandleExitSizeMove),
                   new HANDLE_MESSAGE(WM.MOVE, this._HandleMove),
                });
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        ///   Safe     : Demands full trust permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name="FullTrust")]
        public void SetWindowChrome(WindowChrome newChrome)
        {
            this.VerifyAccess();
            Assert.IsNotNull(this._window);

            if (Equals(newChrome, this._chromeInfo))
            {
                // Nothing's changed.
                return;
            }

            if (this._chromeInfo != null)
            {
                this._chromeInfo.PropertyChangedThatRequiresRepaint -= this._OnChromePropertyChangedThatRequiresRepaint;
            }

            this._chromeInfo = newChrome;
            if (this._chromeInfo != null)
            {
                this._chromeInfo.PropertyChangedThatRequiresRepaint += this._OnChromePropertyChangedThatRequiresRepaint;
            }

            this._ApplyNewCustomChrome();
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        ///   Safe     : Demands full trust permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name="FullTrust")]
        private void _OnChromePropertyChangedThatRequiresRepaint(object sender, EventArgs e)
        {
            this._UpdateFrameState(true);
        }

        public static readonly DependencyProperty WindowChromeWorkerProperty = DependencyProperty.RegisterAttached(
            "WindowChromeWorker",
            typeof(WindowChromeWorker),
            typeof(WindowChromeWorker),
            new PropertyMetadata(null, _OnChromeWorkerChanged));

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        ///   Safe     : Demands full trust permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name="FullTrust")]
        private static void _OnChromeWorkerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var w = (Window)d;
            var cw = (WindowChromeWorker)e.NewValue;

            // The WindowChromeWorker object should only be set on the window once, and never to null.
            Assert.IsNotNull(w);
            Assert.IsNotNull(cw);
            Assert.IsNull(cw._window);

            cw._SetWindow(w);
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _SetWindow(Window window)
        {
            Assert.IsNull(this._window);
            Assert.IsNotNull(window);

            this.UnsubscribeWindowEvents();

            this._window = window;

            // There are potentially a couple funny states here.
            // The window may have been shown and closed, in which case it's no longer usable.
            // We shouldn't add any hooks in that case, just exit early.
            // If the window hasn't yet been shown, then we need to make sure to remove hooks after it's closed.
            this._hwnd = new WindowInteropHelper(this._window).Handle;

            // On older versions of the framework the client size of the window is incorrectly calculated.
            // We need to modify the template to fix this on behalf of the user.

            // This should only be required on older versions of the framework, but because of a DWM bug in Windows 7 we're exposing
            // the SacrificialEdge property which requires this kind of fixup to be a bit more ubiquitous.
            Utility.AddDependencyPropertyChangeListener(this._window, Control.TemplateProperty, this._OnWindowPropertyChangedThatRequiresTemplateFixup);
            Utility.AddDependencyPropertyChangeListener(this._window, FrameworkElement.FlowDirectionProperty, this._OnWindowPropertyChangedThatRequiresTemplateFixup);

            this._window.Closed += this._UnsetWindow;

            // Use whether we can get an HWND to determine if the Window has been loaded.
            if (IntPtr.Zero != this._hwnd)
            {
                // We've seen that the HwndSource can't always be retrieved from the HWND, so cache it early.
                // Specifically it seems to sometimes disappear when the OS theme is changing.
                this._hwndSource = HwndSource.FromHwnd(this._hwnd);
                Assert.IsNotNull(this._hwndSource);
                this._window.ApplyTemplate();

                if (this._chromeInfo != null)
                {
                    this._ApplyNewCustomChrome();
                }
            }
            else
            {
                this._window.SourceInitialized += this._WindowSourceInitialized;
            }
        }

        /// <SecurityNote>
        ///   Critical : Store critical methods in critical callback table
        ///   Safe     : Demands full trust permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name="FullTrust")]
        private void _WindowSourceInitialized(object sender, EventArgs e)
        {
            this._hwnd = new WindowInteropHelper(this._window).Handle;
            Assert.IsNotDefault(this._hwnd);
            this._hwndSource = HwndSource.FromHwnd(this._hwnd);
            Assert.IsNotNull(this._hwndSource);

            if (this._chromeInfo != null)
            {
                this._ApplyNewCustomChrome();
            }
        }

        /// <SecurityNote>
        ///   Critical : References critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void UnsubscribeWindowEvents()
        {
            if (this._window != null)
            {
                Utility.RemoveDependencyPropertyChangeListener(this._window, Control.TemplateProperty, this._OnWindowPropertyChangedThatRequiresTemplateFixup);
                Utility.RemoveDependencyPropertyChangeListener(this._window, FrameworkElement.FlowDirectionProperty, this._OnWindowPropertyChangedThatRequiresTemplateFixup);
                this._window.SourceInitialized -= this._WindowSourceInitialized;
                this._window.StateChanged -= this._FixupRestoreBounds;
                this._window.Closed -= this._UnsetWindow; 
            }
        }

        /// <SecurityNote>
        ///   Critical : Store critical methods in critical callback table
        ///   Safe     : Demands full trust permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name="FullTrust")]
        private void _UnsetWindow(object sender, EventArgs e)
        {
            this.UnsubscribeWindowEvents();

            if (this._chromeInfo != null)
            {
                this._chromeInfo.PropertyChangedThatRequiresRepaint -= this._OnChromePropertyChangedThatRequiresRepaint;
            }

            this._RestoreStandardChromeState(true);
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static WindowChromeWorker GetWindowChromeWorker(Window window)
        {
            Verify.IsNotNull(window, "window");
            return (WindowChromeWorker)window.GetValue(WindowChromeWorkerProperty);
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void SetWindowChromeWorker(Window window, WindowChromeWorker chrome)
        {
            Verify.IsNotNull(window, "window");
            window.SetValue(WindowChromeWorkerProperty, chrome);
        }

        /// <SecurityNote>
        ///   Critical : Accesses critical _hwnd field
        ///   Safe     : Demands full trust permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name="FullTrust")]
        private void _OnWindowPropertyChangedThatRequiresTemplateFixup(object sender, EventArgs e)
        {
            if (this._chromeInfo != null && this._hwnd != IntPtr.Zero)
            {
                // Assume that when the template changes it's going to be applied.
                // We don't have a good way to externally hook into the template
                // actually being applied, so we asynchronously post the fixup operation
                // at Loaded priority, so it's expected that the visual tree will be
                // updated before _FixupTemplateIssues is called.
                this._window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action) this._FixupTemplateIssues);
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _ApplyNewCustomChrome()
        {
            if (this._hwnd == IntPtr.Zero || this._hwndSource.IsDisposed)
            {
                // Not yet hooked.
                return;
            }

            if (this._chromeInfo == null)
            {
                this._RestoreStandardChromeState(false);
                return;
            }

            if (!this._isHooked)
            {
                this._hwndSource.AddHook(this._WndProc);
                this._isHooked = true;
            }

            if (this.MinimizeAnimation)
            {
                // allow animation
                this._ModifyStyle(0, WS.CAPTION);
            }

            this._FixupTemplateIssues();

            // Force this the first time.
            this._UpdateSystemMenu(this._window.WindowState);
            this._UpdateFrameState(true);

            if (this._hwndSource.IsDisposed)
            {
                // If the window got closed very early
                this._UnsetWindow(this._window, EventArgs.Empty);
                return;
            }

            NativeMethods.SetWindowPos(this._hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpFlags);
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _FixupTemplateIssues()
        {
            Assert.IsNotNull(this._chromeInfo);
            Assert.IsNotNull(this._window);

            if (this._window.Template == null)
            {
                // Nothing to fixup yet.  This will get called again when a template does get set.
                return;
            }

            // Guard against the visual tree being empty.
            if (VisualTreeHelper.GetChildrenCount(this._window) == 0)
            {
                // The template isn't null, but we don't have a visual tree.
                // Hope that ApplyTemplate is in the queue and repost this, because there's not much we can do right now.
                this._window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action) this._FixupTemplateIssues);
                return;
            }

            Thickness templateFixupMargin = default(Thickness);

            var rootElement = (FrameworkElement)VisualTreeHelper.GetChild(this._window, 0);

            if (this._chromeInfo.SacrificialEdge != SacrificialEdge.None)
            {
                if (Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Top))
                {
#if NET45 || NET462
                    templateFixupMargin.Top -= SystemParameters.WindowResizeBorderThickness.Top;
#else
                    templateFixupMargin.Top -= SystemParameters2.Current.WindowResizeBorderThickness.Top;
#endif
                }
                if (Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Left))
                {
#if NET45 || NET462
                    templateFixupMargin.Left -= SystemParameters.WindowResizeBorderThickness.Left;
#else
                    templateFixupMargin.Left -= SystemParameters2.Current.WindowResizeBorderThickness.Left;
#endif
                }
                if (Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Bottom))
                {
#if NET45 || NET462
                    templateFixupMargin.Bottom -= SystemParameters.WindowResizeBorderThickness.Bottom;
#else
                    templateFixupMargin.Bottom -= SystemParameters2.Current.WindowResizeBorderThickness.Bottom;
#endif
                }
                if (Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Right))
                {
#if NET45 || NET462
                    templateFixupMargin.Right -= SystemParameters.WindowResizeBorderThickness.Right;
#else
                    templateFixupMargin.Right -= SystemParameters2.Current.WindowResizeBorderThickness.Right;
#endif
                }
            }

            if (Utility.IsPresentationFrameworkVersionLessThan4)
            {
                DpiScale dpi = this._window.GetDpi();
                RECT rcWindow = NativeMethods.GetWindowRect(this._hwnd);
                RECT rcAdjustedClient = this._GetAdjustedWindowRect(rcWindow);

                Rect rcLogicalWindow = DpiHelper.DeviceRectToLogical(new Rect(rcWindow.Left, rcWindow.Top, rcWindow.Width, rcWindow.Height), dpi.DpiScaleX, dpi.DpiScaleY);
                Rect rcLogicalClient = DpiHelper.DeviceRectToLogical(new Rect(rcAdjustedClient.Left, rcAdjustedClient.Top, rcAdjustedClient.Width, rcAdjustedClient.Height), dpi.DpiScaleX, dpi.DpiScaleY);

                if (!Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Left))
                {
#if NET45 || NET462
                    templateFixupMargin.Right -= SystemParameters.WindowResizeBorderThickness.Left;
#else
                    templateFixupMargin.Right -= SystemParameters2.Current.WindowResizeBorderThickness.Left;
#endif
                }

                if (!Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Right))
                {
#if NET45 || NET462
                    templateFixupMargin.Right -= SystemParameters.WindowResizeBorderThickness.Right;
#else
                    templateFixupMargin.Right -= SystemParameters2.Current.WindowResizeBorderThickness.Right;
#endif
                }

                if (!Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Top))
                {
#if NET45 || NET462
                    templateFixupMargin.Bottom -= SystemParameters.WindowResizeBorderThickness.Top;
#else
                    templateFixupMargin.Bottom -= SystemParameters2.Current.WindowResizeBorderThickness.Top;
#endif
                }

                if (!Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Bottom))
                {
#if NET45 || NET462
                    templateFixupMargin.Bottom -= SystemParameters.WindowResizeBorderThickness.Bottom;
#else
                    templateFixupMargin.Bottom -= SystemParameters2.Current.WindowResizeBorderThickness.Bottom;
#endif
                }

#if NET45 || NET462
                templateFixupMargin.Bottom -= SystemParameters.WindowCaptionHeight;
#else
                templateFixupMargin.Bottom -= SystemParameters2.Current.WindowCaptionHeight;
#endif

                // The negative thickness on the margin doesn't properly get applied in RTL layouts.
                // The width is right, but there is a black bar on the right.
                // To fix this we just add an additional RenderTransform to the root element.
                // This works fine, but if the window is dynamically changing its FlowDirection then this can have really bizarre side effects.
                // This will mostly work if the FlowDirection is dynamically changed, but there aren't many real scenarios that would call for
                // that so I'm not addressing the rest of the quirkiness.
                Transform templateFixupTransform;
                if (this._window.FlowDirection == FlowDirection.RightToLeft)
                {
                    Thickness nonClientThickness = new Thickness(
                       rcLogicalWindow.Left - rcLogicalClient.Left,
                       rcLogicalWindow.Top - rcLogicalClient.Top,
                       rcLogicalClient.Right - rcLogicalWindow.Right,
                       rcLogicalClient.Bottom - rcLogicalWindow.Bottom);

                    templateFixupTransform = new MatrixTransform(1, 0, 0, 1, -(nonClientThickness.Left + nonClientThickness.Right), 0);
                }
                else
                {
                    templateFixupTransform = null;
                }

                rootElement.RenderTransform = templateFixupTransform;
            }

            rootElement.Margin = templateFixupMargin;

            if (Utility.IsPresentationFrameworkVersionLessThan4)
            {
                if (!this._isFixedUp)
                {
                    this._hasUserMovedWindow = false;
                    this._window.StateChanged += this._FixupRestoreBounds;

                    this._isFixedUp = true;
                }
            }
        }

        /// <SecurityNote>
        ///   Critical : Store critical methods in critical callback table
        ///   Safe     : Demands full trust permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name="FullTrust")]
        private void _FixupRestoreBounds(object sender, EventArgs e)
        {
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            if (this._window.WindowState == WindowState.Maximized || this._window.WindowState == WindowState.Minimized)
            {
                // Old versions of WPF sometimes force their incorrect idea of the Window's location
                // on the Win32 restore bounds.  If we have reason to think this is the case, then
                // try to undo what WPF did after it has done its thing.
                if (this._hasUserMovedWindow)
                {
                    DpiScale dpi = this._window.GetDpi();
                    this._hasUserMovedWindow = false;
                    WINDOWPLACEMENT wp = NativeMethods.GetWindowPlacement(this._hwnd);

                    RECT adjustedDeviceRc = this._GetAdjustedWindowRect(new RECT { Bottom = 100, Right = 100 });
                    Point adjustedTopLeft = DpiHelper.DevicePixelsToLogical(
                        new Point(
                            wp.normalPosition.Left - adjustedDeviceRc.Left,
                            wp.normalPosition.Top - adjustedDeviceRc.Top),
                        dpi.DpiScaleX, dpi.DpiScaleY);

                    this._window.Top = adjustedTopLeft.Y;
                    this._window.Left = adjustedTopLeft.X;
                }
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private RECT _GetAdjustedWindowRect(RECT rcWindow)
        {
            // This should only be used to work around issues in the Framework that were fixed in 4.0
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);

            var style = (WS)NativeMethods.GetWindowLongPtr(this._hwnd, GWL.STYLE);
            var exstyle = (WS_EX)NativeMethods.GetWindowLongPtr(this._hwnd, GWL.EXSTYLE);

            return NativeMethods.AdjustWindowRectEx(rcWindow, style, false, exstyle);
        }

        // Windows tries hard to hide this state from applications.
        // Generally you can tell that the window is in a docked position because the restore bounds from GetWindowPlacement
        // don't match the current window location and it's not in a maximized or minimized state.
        // Because this isn't doced or supported, it's also not incredibly consistent.  Sometimes some things get updated in
        // different orders, so this isn't absolutely reliable.
        /// <SecurityNote>
        ///   Critical : Calls critical method
        /// </SecurityNote>
        private bool IsWindowDocked
        {
            [SecurityCritical]
            get
            {
                // We're only detecting this state to work around .Net 3.5 issues.
                // This logic won't work correctly when those issues are fixed.
                Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);

                if (this._window.WindowState != WindowState.Normal)
                {
                    return false;
                }

                DpiScale dpi = this._window.GetDpi();

                RECT adjustedOffset = this._GetAdjustedWindowRect(new RECT { Bottom = 100, Right = 100 });
                Point windowTopLeft = new Point(this._window.Left, this._window.Top);
                windowTopLeft -= (Vector)DpiHelper.DevicePixelsToLogical(new Point(adjustedOffset.Left, adjustedOffset.Top), dpi.DpiScaleX, dpi.DpiScaleY);

                return this._window.RestoreBounds.Location != windowTopLeft;
            }
        }

        /// A borderless window lost his animation, with this we bring it back.
        private bool MinimizeAnimation => SystemParameters.MinimizeAnimation && this._chromeInfo.IgnoreTaskbarOnMaximize == false;

        #region WindowProc and Message Handlers

        /// <SecurityNote>
        ///   Critical : Accesses critical _hwnd
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Only expecting messages for our cached HWND.
            Assert.AreEqual(hwnd, this._hwnd);

            // Check if window has a RootVisual to workaround issue #13 (Win32Exception on closing window).
            // RootVisual gets cleared when the window is closing. This happens in CloseWindowFromWmClose of the Window class.
            if (this._hwndSource?.RootVisual == null)
            {
                return IntPtr.Zero;
            }

            var message = (WM)msg;
            foreach (var handlePair in this._messageTable)
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
        private IntPtr _HandleNCUAHDrawCaption(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            if (false == this._window.ShowInTaskbar && this._GetHwndState() == WindowState.Minimized)
            {
                bool modified = this._ModifyStyle(WS.VISIBLE, 0);

                // Minimize the window with ShowInTaskbar == false cause Windows to redraw the caption.
                // Letting the default WndProc handle the message without the WS_VISIBLE
                // style applied bypasses the redraw.
                IntPtr lRet = NativeMethods.DefWindowProc(this._hwnd, uMsg, wParam, lParam);

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

        private IntPtr _HandleSetTextOrIcon(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            bool modified = this._ModifyStyle(WS.VISIBLE, 0);

            // Setting the caption text and icon cause Windows to redraw the caption.
            // Letting the default WndProc handle the message without the WS_VISIBLE
            // style applied bypasses the redraw.
            IntPtr lRet = NativeMethods.DefWindowProc(this._hwnd, uMsg, wParam, lParam);

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
        private IntPtr _HandleRestoreWindow(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            WINDOWPLACEMENT wpl = NativeMethods.GetWindowPlacement(this._hwnd);
            var sc = (SC)(Environment.Is64BitProcess ? wParam.ToInt64() : wParam.ToInt32());
            if (SC.RESTORE == sc && wpl.showCmd == SW.SHOWMAXIMIZED && this.MinimizeAnimation)
            {
                var modified = this._ModifyStyle(WS.SYSMENU, 0);

                IntPtr lRet = NativeMethods.DefWindowProc(this._hwnd, uMsg, wParam, lParam);

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
        private IntPtr _HandleNCActivate(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // Despite MSDN's documentation of lParam not being used,
            // calling DefWindowProc with lParam set to -1 causes Windows not to draw over the caption.

            // Directly call DefWindowProc with a custom parameter
            // which bypasses any other handling of the message.
            IntPtr lRet = NativeMethods.DefWindowProc(this._hwnd, WM.NCACTIVATE, wParam, new IntPtr(-1));
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
            bool autoHide = Convert.ToBoolean(NativeMethods.SHAppBarMessage((int)ABMsg.ABM_GETSTATE, ref abd));

            if (!autoHide)
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
        private IntPtr _HandleNCCalcSize(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // lParam is an [in, out] that can be either a RECT* (wParam == FALSE) or an NCCALCSIZE_PARAMS*.
            // Since the first field of NCCALCSIZE_PARAMS is a RECT and is the only field we care about
            // we can unconditionally treat it as a RECT.

            if (NativeMethods.GetWindowPlacement(this._hwnd).showCmd == SW.MAXIMIZE && this.MinimizeAnimation)
            {
                var monitor = NativeMethods.MonitorFromWindow(this._hwnd, MonitorOptions.MonitorDefaulttonearest);
                var monitorInfo = NativeMethods.GetMonitorInfo(monitor);
                var monitorRect = this._chromeInfo.IgnoreTaskbarOnMaximize ? monitorInfo.rcMonitor : monitorInfo.rcWork;

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

            if (this._chromeInfo.SacrificialEdge != SacrificialEdge.None)
            {
                DpiScale dpi = this._window.GetDpi();
#if NET45 || NET462
                Thickness windowResizeBorderThicknessDevice = DpiHelper.LogicalThicknessToDevice(SystemParameters.WindowResizeBorderThickness, dpi.DpiScaleX, dpi.DpiScaleY);
#else
                Thickness windowResizeBorderThicknessDevice = DpiHelper.LogicalThicknessToDevice(SystemParameters2.Current.WindowResizeBorderThickness, dpi.DpiScaleX, dpi.DpiScaleY);
#endif
                var rcClientArea = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
                if (Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Top))
                {
                    rcClientArea.Top += (int)windowResizeBorderThicknessDevice.Top;
                }
                if (Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Left))
                {
                    rcClientArea.Left += (int)windowResizeBorderThicknessDevice.Left;
                }
                if (Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Bottom))
                {
                    rcClientArea.Bottom -= (int)windowResizeBorderThicknessDevice.Bottom;
                }
                if (Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Right))
                {
                    rcClientArea.Right -= (int)windowResizeBorderThicknessDevice.Right;
                }

                Marshal.StructureToPtr(rcClientArea, lParam, false);
            }

            handled = true;

            // Per MSDN for NCCALCSIZE, always return 0 when wParam == FALSE
            // 
            // Returning 0 when wParam == TRUE is not appropriate - it will preserve
            // the old client area and align it with the upper-left corner of the new 
            // client area. So we simply ask for a redraw (WVR_REDRAW)

            IntPtr retVal = IntPtr.Zero;
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
            bool compliment = this._window.FlowDirection == FlowDirection.RightToLeft;
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
        private IntPtr _HandleNCHitTest(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            DpiScale dpi = this._window.GetDpi();

            // Let the system know if we consider the mouse to be in our effective non-client area.
            var mousePosScreen = Utility.GetPoint(lParam); //new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam));
            Rect windowPosition = this._GetWindowRect();

            Point mousePosWindow = mousePosScreen;
            mousePosWindow.Offset(-windowPosition.X, -windowPosition.Y);
            mousePosWindow = DpiHelper.DevicePixelsToLogical(mousePosWindow, dpi.DpiScaleX, dpi.DpiScaleY);

            // If the app is asking for content to be treated as client then that takes precedence over _everything_, even DWM caption buttons.
            // This allows apps to set the glass frame to be non-empty, still cover it with WPF content to hide all the glass,
            // yet still get DWM to draw a drop shadow.
            IInputElement inputElement = this._window.InputHitTest(mousePosWindow);
            if (inputElement != null)
            {
                if (WindowChrome.GetIsHitTestVisibleInChrome(inputElement))
                {
                    handled = true;
                    return new IntPtr((int)HT.CLIENT);
                }

                ResizeGripDirection direction = WindowChrome.GetResizeGripDirection(inputElement);
                if (direction != ResizeGripDirection.None)
                {
                    handled = true;
                    return new IntPtr((int) this._GetHTFromResizeGripDirection(direction));
                }
            }

            // It's not opted out, so offer up the hittest to DWM, then to our custom non-client area logic.
            if (this._chromeInfo.UseAeroCaptionButtons)
            {
                if (Utility.IsOsVistaOrNewer && this._chromeInfo.GlassFrameThickness != default(Thickness) && this._isGlassEnabled)
                {
                    // If we're on Vista, give the DWM a chance to handle the message first.
                    handled = NativeMethods.DwmDefWindowProc(this._hwnd, uMsg, wParam, lParam, out IntPtr lRet);

                    if (IntPtr.Zero != lRet)
                    {
                        // If DWM claims to have handled this, then respect their call.
                        return lRet;
                    }
                }
            }

            HT ht = this._HitTestNca(
                DpiHelper.DeviceRectToLogical(windowPosition, dpi.DpiScaleX, dpi.DpiScaleY),
                DpiHelper.DevicePixelsToLogical(mousePosScreen, dpi.DpiScaleX, dpi.DpiScaleY));

            handled = true;
            return new IntPtr((int)ht);
        }

        /// <SecurityNote>
        ///   Critical : Calls critical method
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleNCRButtonUp(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // Emulate the system behavior of clicking the right mouse button over the caption area
            // to bring up the system menu.
            if (HT.CAPTION == (HT)(Environment.Is64BitProcess ? wParam.ToInt64() : wParam.ToInt32()))
            {
                //SystemCommands.ShowSystemMenuPhysicalCoordinates(_window, new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam)));
                SystemCommands.ShowSystemMenuPhysicalCoordinates(this._window, Utility.GetPoint(lParam));
            }
            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical method
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleSize(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            const int sizeMaximized = 2;

            // Force when maximized.
            // We can tell what's happening right now, but the Window doesn't yet know it's
            // maximized.  Not forcing this update will eventually cause the
            // default caption to be drawn.
            WindowState? state = null;
            if ((Environment.Is64BitProcess ? wParam.ToInt64() : wParam.ToInt32()) == sizeMaximized)
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
        private IntPtr _HandleWindowPosChanging(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            if (!this._isGlassEnabled)
            {
                Assert.IsNotDefault(lParam);
                var wp = (WINDOWPOS) Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                // we don't do bitwise operations cuz we're checking for this flag being the only one there
                // I have no clue why this works, I tried this because VS2013 has this flag removed on fullscreen window movws
                if (this._chromeInfo.IgnoreTaskbarOnMaximize && this._GetHwndState() == WindowState.Maximized && wp.flags == SWP.FRAMECHANGED)
                {
                    wp.flags = 0;
                    Marshal.StructureToPtr(wp, lParam, true);
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical Marshal.PtrToStructure
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleWindowPosChanged(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // http://blogs.msdn.com/oldnewthing/archive/2008/01/15/7113860.aspx
            // The WM_WINDOWPOSCHANGED message is sent at the end of the window
            // state change process. It sort of combines the other state change
            // notifications, WM_MOVE, WM_SIZE, and WM_SHOWWINDOW. But it doesn't
            // suffer from the same limitations as WM_SHOWWINDOW, so you can
            // reliably use it to react to the window being shown or hidden.

            this._UpdateSystemMenu(null);

            if (!this._isGlassEnabled)
            {
                Assert.IsNotDefault(lParam);
                var wp = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                if (!wp.Equals(this._previousWp))
                {
                    this._previousWp = wp;
                    this._SetRoundingRegion(wp);
                }
                this._previousWp = wp;

//                if (wp.Equals(_previousWP) && wp.flags.Equals(_previousWP.flags))
//                {
//                    handled = true;
//                    return IntPtr.Zero;
//                }
            }

            // Still want to pass this to DefWndProc
            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleGetMinMaxInfo(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            /*
             * This is a workaround for wrong windows behaviour.
             * If a Window sets the WindoStyle to None and WindowState to maximized and we have a multi monitor system
             * we can move the Window only one time. After that it's not possible to move the Window back to the
             * previous monitor.
             * This fix is not really a full fix. Moving the Window back gives us the wrong size, because
             * MonitorFromWindow gives us the wrong (old) monitor! This is fixed in _HandleMoveForRealSize.
             */
            var ignoreTaskBar = this._chromeInfo.IgnoreTaskbarOnMaximize;
            if (ignoreTaskBar && NativeMethods.IsZoomed(this._hwnd))
            {
                MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
                IntPtr monitor = NativeMethods.MonitorFromWindow(this._hwnd, MonitorOptions.MonitorDefaulttonearest);
                if (monitor != IntPtr.Zero)
                {
                    Monitorinfo monitorInfo = NativeMethods.GetMonitorInfoW(monitor);
                    RECT rcWorkArea = monitorInfo.rcWork;
                    RECT rcMonitorArea = monitorInfo.rcMonitor;
                    
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
        private IntPtr _HandleDwmCompositionChanged(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            this._UpdateFrameState(false);

            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleSettingChange(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // There are several settings that can cause fixups for the template to become invalid when changed.
            // These shouldn't be required on the v4 framework.
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);

            this._FixupTemplateIssues();

            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleEnterSizeMove(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // This is only intercepted to deal with bugs in Window in .Net 3.5 and below.
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);

            this._isUserResizing = true;

            // On Win7 if the user is dragging the window out of the maximized state then we don't want to use that location
            // as a restore point.
            Assert.Implies(this._window.WindowState == WindowState.Maximized, Utility.IsOsWindows7OrNewer);
            if (this._window.WindowState != WindowState.Maximized)
            {
                // Check for the docked window case.  The window can still be restored when it's in this position so
                // try to account for that and not update the start position.
                if (!this.IsWindowDocked)
                {
                    this._windowPosAtStartOfUserMove = new Point(this._window.Left, this._window.Top);
                }
                // Realistically we also don't want to update the start position when moving from one docked state to another (or to and from maximized),
                // but it's tricky to detect and this is already a workaround for a bug that's fixed in newer versions of the framework.
                // Not going to try to handle all cases.
            }

            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleEnterSizeMoveForAnimation(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
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
        private IntPtr _HandleMoveForRealSize(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            /*
             * This is a workaround for wrong windows behaviour (with multi monitor system).
             * If a Window sets the WindoStyle to None and WindowState to maximized
             * we can move the Window to different monitor with maybe different dimension.
             * But after moving to the previous monitor we got a wrong size (from the old monitor dimension).
             */
            WindowState state = this._GetHwndState();
            if (state == WindowState.Maximized) {
                IntPtr monitorFromWindow = NativeMethods.MonitorFromWindow(this._hwnd, MonitorOptions.MonitorDefaulttonearest);
                if (monitorFromWindow != IntPtr.Zero)
                {
                    var ignoreTaskBar = this._chromeInfo.IgnoreTaskbarOnMaximize;
                    Monitorinfo monitorInfo = NativeMethods.GetMonitorInfoW(monitorFromWindow);
                    RECT rcMonitorArea = ignoreTaskBar ? monitorInfo.rcMonitor : monitorInfo.rcWork;
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
                    NativeMethods.SetWindowPos(this._hwnd, IntPtr.Zero, rcMonitorArea.Left, rcMonitorArea.Top, rcMonitorArea.Width, rcMonitorArea.Height, SWP.ASYNCWINDOWPOS | SWP.FRAMECHANGED | SWP.NOCOPYBITS);
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _HandleExitSizeMoveForAnimation(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            if (this.MinimizeAnimation)
            {
                // restore DLGFRAME
                if (this._ModifyStyle(0, WS.CAPTION))
                {
                    //_UpdateFrameState(true);
                    NativeMethods.SetWindowPos(this._hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpFlags);
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleExitSizeMove(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // This is only intercepted to deal with bugs in Window in .Net 3.5 and below.
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);

            this._isUserResizing = false;

            // On Win7 the user can change the Window's state by dragging the window to the top of the monitor.
            // If they did that, then we need to try to update the restore bounds or else WPF will put the window at the maximized location (e.g. (-8,-8)).
            if (this._window.WindowState == WindowState.Maximized)
            {
                Assert.IsTrue(Utility.IsOsWindows7OrNewer);
                this._window.Top = this._windowPosAtStartOfUserMove.Y;
                this._window.Left = this._windowPosAtStartOfUserMove.X;
            }

            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleMove(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // This is only intercepted to deal with bugs in Window in .Net 3.5 and below.
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);

            if (this._isUserResizing)
            {
                this._hasUserMovedWindow = true;
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
            Assert.IsNotDefault(this._hwnd);
            var intPtr = NativeMethods.GetWindowLongPtr(this._hwnd, GWL.STYLE);
            var dwStyle = (WS)(Environment.Is64BitProcess ? intPtr.ToInt64() : intPtr.ToInt32());
            var dwNewStyle = (dwStyle & ~removeStyle) | addStyle;
            if (dwStyle == dwNewStyle)
            {
                return false;
            }

            NativeMethods.SetWindowLongPtr(this._hwnd, GWL.STYLE, new IntPtr((int)dwNewStyle));
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
            var wpl = NativeMethods.GetWindowPlacement(this._hwnd);
            switch (wpl.showCmd)
            {
                case SW.SHOWMINIMIZED: return WindowState.Minimized;
                case SW.SHOWMAXIMIZED: return WindowState.Maximized;
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
            RECT windowPosition = NativeMethods.GetWindowRect(this._hwnd);
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
            const MF mfEnabled = MF.ENABLED | MF.BYCOMMAND;
            const MF mfDisabled = MF.GRAYED | MF.DISABLED | MF.BYCOMMAND;

            WindowState state = assumeState ?? this._GetHwndState();

            if (null != assumeState || this._lastMenuState != state)
            {
                this._lastMenuState = state;

                IntPtr hmenu = NativeMethods.GetSystemMenu(this._hwnd, false);
                if (IntPtr.Zero != hmenu)
                {
                    var intPtr = NativeMethods.GetWindowLongPtr(this._hwnd, GWL.STYLE);
                    var dwStyle = (WS)(Environment.Is64BitProcess ? intPtr.ToInt64() : intPtr.ToInt32());

                    bool canMinimize = Utility.IsFlagSet((int)dwStyle, (int)WS.MINIMIZEBOX);
                    bool canMaximize = Utility.IsFlagSet((int)dwStyle, (int)WS.MAXIMIZEBOX);
                    bool canSize = Utility.IsFlagSet((int)dwStyle, (int)WS.THICKFRAME);

                    switch (state)
                    {
                        case WindowState.Maximized:
                            NativeMethods.EnableMenuItem(hmenu, SC.RESTORE, mfEnabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MOVE, mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.SIZE, mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MINIMIZE, canMinimize ? mfEnabled : mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MAXIMIZE, mfDisabled);
                            break;
                        case WindowState.Minimized:
                            NativeMethods.EnableMenuItem(hmenu, SC.RESTORE, mfEnabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MOVE, mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.SIZE, mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MINIMIZE, mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MAXIMIZE, canMaximize ? mfEnabled : mfDisabled);
                            break;
                        default:
                            NativeMethods.EnableMenuItem(hmenu, SC.RESTORE, mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MOVE, mfEnabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.SIZE, canSize ? mfEnabled : mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MINIMIZE, canMinimize ? mfEnabled : mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MAXIMIZE, canMaximize ? mfEnabled : mfDisabled);
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
            if (IntPtr.Zero == this._hwnd || this._hwndSource.IsDisposed)
            {
                return;
            }

            // Don't rely on SystemParameters for this, just make the check ourselves.
            bool frameState = NativeMethods.DwmIsCompositionEnabled();

            if (force || frameState != this._isGlassEnabled)
            {
                this._isGlassEnabled = frameState && this._chromeInfo.GlassFrameThickness != default(Thickness);

                if (!this._isGlassEnabled)
                {
                    this._SetRoundingRegion(null);
                }
                else
                {
                    this._ClearRoundingRegion();
                    this._ExtendGlassFrame();
                }

                if (this._hwndSource.IsDisposed)
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

                NativeMethods.SetWindowPos(this._hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpFlags);
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _ClearRoundingRegion()
        {
            NativeMethods.SetWindowRgn(this._hwnd, IntPtr.Zero, NativeMethods.IsWindowVisible(this._hwnd));
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private RECT _GetClientRectRelativeToWindowRect(IntPtr hWnd)
        {
            RECT windowRect = NativeMethods.GetWindowRect(hWnd);
            RECT clientRect = NativeMethods.GetClientRect(hWnd);

            POINT test = new POINT() { X = 0, Y = 0 };
            NativeMethods.ClientToScreen(hWnd, ref test);
            if (this._window.FlowDirection == FlowDirection.RightToLeft)
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
        private void _SetRoundingRegion(WINDOWPOS? wp)
        {
            // We're early - WPF hasn't necessarily updated the state of the window.
            // Need to query it ourselves.
            WINDOWPLACEMENT wpl = NativeMethods.GetWindowPlacement(this._hwnd);

            if (wpl.showCmd == SW.SHOWMAXIMIZED)
            {
                RECT rcMax;
                if (this.MinimizeAnimation)
                {
                    rcMax = this._GetClientRectRelativeToWindowRect(this._hwnd);
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
                        Rect r = this._GetWindowRect();
                        left = (int)r.Left;
                        top = (int)r.Top;
                    }

                    IntPtr hMon = NativeMethods.MonitorFromWindow(this._hwnd, MonitorOptions.MonitorDefaulttonearest);

                    Monitorinfo mi = NativeMethods.GetMonitorInfo(hMon);
                    rcMax = this._chromeInfo.IgnoreTaskbarOnMaximize ? mi.rcMonitor : mi.rcWork;
                    // The location of maximized window takes into account the border that Windows was
                    // going to remove, so we also need to consider it.
                    rcMax.Offset(-left, -top);
                }

                IntPtr hrgn = IntPtr.Zero;
                try
                {
                    hrgn = NativeMethods.CreateRectRgnIndirect(rcMax);
                    NativeMethods.SetWindowRgn(this._hwnd, hrgn, NativeMethods.IsWindowVisible(this._hwnd));
                    hrgn = IntPtr.Zero;
                }
                finally
                {
                    Utility.SafeDeleteObject(ref hrgn);
                }
            }
            else
            {
                Size windowSize;

                // Use the size if it's specified.
                if (null != wp && !Utility.IsFlagSet((int)wp.Value.flags, (int)SWP.NOSIZE))
                {
                    windowSize = new Size(wp.Value.cx, wp.Value.cy);
                }
                else if (null != wp && (this._lastRoundingState == this._window.WindowState))
                {
                    return;
                }
                else
                {
                    windowSize = this._GetWindowRect().Size;
                }

                this._lastRoundingState = this._window.WindowState;

                IntPtr hrgn = IntPtr.Zero;
                try
                {
                    DpiScale dpi = this._window.GetDpi();

                    double shortestDimension = Math.Min(windowSize.Width, windowSize.Height);

                    double topLeftRadius = DpiHelper.LogicalPixelsToDevice(new Point(this._chromeInfo.CornerRadius.TopLeft, 0), dpi.DpiScaleX, dpi.DpiScaleY).X;
                    topLeftRadius = Math.Min(topLeftRadius, shortestDimension / 2);

                    if (_IsUniform(this._chromeInfo.CornerRadius))
                    {
                        // RoundedRect HRGNs require an additional pixel of padding.
                        hrgn = _CreateRoundRectRgn(new Rect(windowSize), topLeftRadius);
                    }
                    else
                    {
                        // We need to combine HRGNs for each of the corners.
                        // Create one for each quadrant, but let it overlap into the two adjacent ones
                        // by the radius amount to ensure that there aren't corners etched into the middle
                        // of the window.
                        hrgn = _CreateRoundRectRgn(new Rect(0, 0, windowSize.Width / 2 + topLeftRadius, windowSize.Height / 2 + topLeftRadius), topLeftRadius);

                        double topRightRadius = DpiHelper.LogicalPixelsToDevice(new Point(this._chromeInfo.CornerRadius.TopRight, 0), dpi.DpiScaleX, dpi.DpiScaleY).X;
                        topRightRadius = Math.Min(topRightRadius, shortestDimension / 2);
                        Rect topRightRegionRect = new Rect(0, 0, windowSize.Width / 2 + topRightRadius, windowSize.Height / 2 + topRightRadius);
                        topRightRegionRect.Offset(windowSize.Width / 2 - topRightRadius, 0);
                        Assert.AreEqual(topRightRegionRect.Right, windowSize.Width);

                        _CreateAndCombineRoundRectRgn(hrgn, topRightRegionRect, topRightRadius);

                        double bottomLeftRadius = DpiHelper.LogicalPixelsToDevice(new Point(this._chromeInfo.CornerRadius.BottomLeft, 0), dpi.DpiScaleX, dpi.DpiScaleY).X;
                        bottomLeftRadius = Math.Min(bottomLeftRadius, shortestDimension / 2);
                        Rect bottomLeftRegionRect = new Rect(0, 0, windowSize.Width / 2 + bottomLeftRadius, windowSize.Height / 2 + bottomLeftRadius);
                        bottomLeftRegionRect.Offset(0, windowSize.Height / 2 - bottomLeftRadius);
                        Assert.AreEqual(bottomLeftRegionRect.Bottom, windowSize.Height);

                        _CreateAndCombineRoundRectRgn(hrgn, bottomLeftRegionRect, bottomLeftRadius);

                        double bottomRightRadius = DpiHelper.LogicalPixelsToDevice(new Point(this._chromeInfo.CornerRadius.BottomRight, 0), dpi.DpiScaleX, dpi.DpiScaleY).X;
                        bottomRightRadius = Math.Min(bottomRightRadius, shortestDimension / 2);
                        Rect bottomRightRegionRect = new Rect(0, 0, windowSize.Width / 2 + bottomRightRadius, windowSize.Height / 2 + bottomRightRadius);
                        bottomRightRegionRect.Offset(windowSize.Width / 2 - bottomRightRadius, windowSize.Height / 2 - bottomRightRadius);
                        Assert.AreEqual(bottomRightRegionRect.Right, windowSize.Width);
                        Assert.AreEqual(bottomRightRegionRect.Bottom, windowSize.Height);

                        _CreateAndCombineRoundRectRgn(hrgn, bottomRightRegionRect, bottomRightRadius);
                    }

                    NativeMethods.SetWindowRgn(this._hwnd, hrgn, NativeMethods.IsWindowVisible(this._hwnd));
                    hrgn = IntPtr.Zero;
                }
                finally
                {
                    // Free the memory associated with the HRGN if it wasn't assigned to the HWND.
                    Utility.SafeDeleteObject(ref hrgn);
                }
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private static IntPtr _CreateRoundRectRgn(Rect region, double radius)
        {
            // Round outwards.

            if (DoubleUtilities.AreClose(0, radius))
            {
                return NativeMethods.CreateRectRgn(
                    (int)Math.Floor(region.Left),
                    (int)Math.Floor(region.Top),
                    (int)Math.Ceiling(region.Right),
                    (int)Math.Ceiling(region.Bottom));
            }

            // RoundedRect HRGNs require an additional pixel of padding on the bottom right to look correct.
            return NativeMethods.CreateRoundRectRgn(
                (int)Math.Floor(region.Left),
                (int)Math.Floor(region.Top),
                (int)Math.Ceiling(region.Right) + 1,
                (int)Math.Ceiling(region.Bottom) + 1,
                (int)Math.Ceiling(radius),
                (int)Math.Ceiling(radius));
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "HRGNs")]
        private static void _CreateAndCombineRoundRectRgn(IntPtr hrgnSource, Rect region, double radius)
        {
            IntPtr hrgn = IntPtr.Zero;
            try
            {
                hrgn = _CreateRoundRectRgn(region, radius);
                CombineRgnResult result = NativeMethods.CombineRgn(hrgnSource, hrgnSource, hrgn, RGN.OR);
                if (result == CombineRgnResult.ERROR)
                {
                    throw new InvalidOperationException("Unable to combine two HRGNs.");
                }
            }
            catch
            {
                Utility.SafeDeleteObject(ref hrgn);
                throw;
            }
        }

        private static bool _IsUniform(CornerRadius cornerRadius)
        {
            if (!DoubleUtilities.AreClose(cornerRadius.BottomLeft, cornerRadius.BottomRight))
            {
                return false;
            }

            if (!DoubleUtilities.AreClose(cornerRadius.TopLeft, cornerRadius.TopRight))
            {
                return false;
            }

            if (!DoubleUtilities.AreClose(cornerRadius.BottomLeft, cornerRadius.TopRight))
            {
                return false;
            }

            return true;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _ExtendGlassFrame()
        {
            Assert.IsNotNull(this._window);

            // Expect that this might be called on OSes other than Vista.
            if (!Utility.IsOsVistaOrNewer)
            {
                // Not an error.  Just not on Vista so we're not going to get glass.
                return;
            }

            if (IntPtr.Zero == this._hwnd)
            {
                // Can't do anything with this call until the Window has been shown.
                return;
            }

            if (this._hwndSource.IsDisposed)
            {
                // If the window got closed very early
                return;
            }

            // Ensure standard HWND background painting when DWM isn't enabled.
            if (!NativeMethods.DwmIsCompositionEnabled())
            {
                // Apply the transparent background to the HWND for disabled DwmIsComposition too
                // but only if the window has the flag AllowsTransparency turned on
                if (this._window.AllowsTransparency)
                {
                    var hwndSourceCompositionTarget = this._hwndSource.CompositionTarget;
                    if (hwndSourceCompositionTarget != null)
                        hwndSourceCompositionTarget.BackgroundColor = Colors.Transparent;
                }
                else
                {
                    var hwndSourceCompositionTarget = this._hwndSource.CompositionTarget;
                    if (hwndSourceCompositionTarget != null)
                        hwndSourceCompositionTarget.BackgroundColor = SystemColors.WindowColor;
                }
            }
            else
            {
                DpiScale dpi = this._window.GetDpi();

                // This makes the glass visible at a Win32 level so long as nothing else is covering it.
                // The Window's Background needs to be changed independent of this.

                // Apply the transparent background to the HWND
                // but only if the window has the flag AllowsTransparency turned on
                if (this._window.AllowsTransparency)
                {
                    var hwndSourceCompositionTarget = this._hwndSource.CompositionTarget;
                    if (hwndSourceCompositionTarget != null)
                        hwndSourceCompositionTarget.BackgroundColor = Colors.Transparent;
                }

                // Thickness is going to be DIPs, need to convert to system coordinates.
                Thickness deviceGlassThickness = DpiHelper.LogicalThicknessToDevice(this._chromeInfo.GlassFrameThickness, dpi.DpiScaleX, dpi.DpiScaleY);

                if (this._chromeInfo.SacrificialEdge != SacrificialEdge.None)
                {
#if NET45 || NET462
                    Thickness windowResizeBorderThicknessDevice = DpiHelper.LogicalThicknessToDevice(SystemParameters.WindowResizeBorderThickness, dpi.DpiScaleX, dpi.DpiScaleY);
#else
                    Thickness windowResizeBorderThicknessDevice = DpiHelper.LogicalThicknessToDevice(SystemParameters2.Current.WindowResizeBorderThickness, dpi.DpiScaleX, dpi.DpiScaleY);
#endif
                    if (Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Top))
                    {
                        deviceGlassThickness.Top -= windowResizeBorderThicknessDevice.Top;
                        deviceGlassThickness.Top = Math.Max(0, deviceGlassThickness.Top);
                    }
                    if (Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Left))
                    {
                        deviceGlassThickness.Left -= windowResizeBorderThicknessDevice.Left;
                        deviceGlassThickness.Left = Math.Max(0, deviceGlassThickness.Left);
                    }
                    if (Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Bottom))
                    {
                        deviceGlassThickness.Bottom -= windowResizeBorderThicknessDevice.Bottom;
                        deviceGlassThickness.Bottom = Math.Max(0, deviceGlassThickness.Bottom);
                    }
                    if (Utility.IsFlagSet((int) this._chromeInfo.SacrificialEdge, (int)SacrificialEdge.Right))
                    {
                        deviceGlassThickness.Right -= windowResizeBorderThicknessDevice.Right;
                        deviceGlassThickness.Right = Math.Max(0, deviceGlassThickness.Right);
                    }
                }

                var dwmMargin = new MARGINS
                {
                    // err on the side of pushing in glass an extra pixel.
                    cxLeftWidth = (int)Math.Ceiling(deviceGlassThickness.Left),
                    cxRightWidth = (int)Math.Ceiling(deviceGlassThickness.Right),
                    cyTopHeight = (int)Math.Ceiling(deviceGlassThickness.Top),
                    cyBottomHeight = (int)Math.Ceiling(deviceGlassThickness.Bottom),
                };

                NativeMethods.DwmExtendFrameIntoClientArea(this._hwnd, ref dwmMargin);
            }
        }

        /// <summary>
        /// Matrix of the HT values to return when responding to NC window messages.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
        private static readonly HT[,] HitTestBorders = new[,]
        {
            { HT.TOPLEFT,    HT.TOP,     HT.TOPRIGHT    },
            { HT.LEFT,       HT.CLIENT,  HT.RIGHT       },
            { HT.BOTTOMLEFT, HT.BOTTOM,  HT.BOTTOMRIGHT },
        };

        private HT _HitTestNca(Rect windowPosition, Point mousePosition)
        {
            // Determine if hit test is for resizing, default middle (1,1).
            int uRow = 1;
            int uCol = 1;
            bool onResizeBorder = false;

            // Determine if the point is at the top or bottom of the window.
            if (mousePosition.Y >= windowPosition.Top && mousePosition.Y < windowPosition.Top + this._chromeInfo.ResizeBorderThickness.Top + this._chromeInfo.CaptionHeight)
            {
                onResizeBorder = (mousePosition.Y < (windowPosition.Top + this._chromeInfo.ResizeBorderThickness.Top));
                uRow = 0; // top (caption or resize border)
            }
            else if (mousePosition.Y < windowPosition.Bottom && mousePosition.Y >= windowPosition.Bottom - (int) this._chromeInfo.ResizeBorderThickness.Bottom)
            {
                uRow = 2; // bottom
            }

            // Determine if the point is at the left or right of the window.
            if (mousePosition.X >= windowPosition.Left && mousePosition.X < windowPosition.Left + (int) this._chromeInfo.ResizeBorderThickness.Left)
            {
                uCol = 0; // left side
            }
            else if (mousePosition.X < windowPosition.Right && mousePosition.X >= windowPosition.Right - this._chromeInfo.ResizeBorderThickness.Right)
            {
                uCol = 2; // right side
            }

            // If the cursor is in one of the top edges by the caption bar, but below the top resize border,
            // then resize left-right rather than diagonally.
            if (uRow == 0 && uCol != 1 && !onResizeBorder)
            {
                uRow = 1;
            }

            HT ht = HitTestBorders[uRow, uCol];

            if (ht == HT.TOP && !onResizeBorder)
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

            this._UnhookCustomChrome();

            if (!isClosing && !this._hwndSource.IsDisposed)
            {
                this._RestoreFrameworkIssueFixups();
                this._RestoreGlassFrame();
                this._RestoreHrgn();

                this._window.InvalidateMeasure();
            }
        }

        /// <SecurityNote>
        ///   Critical : Unsubscribes event handler from critical _hwndSource
        /// </SecurityNote>
        [SecurityCritical]
        private void _UnhookCustomChrome()
        {
            //Assert.IsNotDefault(_hwnd);
            Assert.IsNotNull(this._window);

            if (this._isHooked)
            {
                Assert.IsNotDefault(this._hwnd);
                this._hwndSource.RemoveHook(this._WndProc);
                this._isHooked = false;
            }
        }

        /// <SecurityNote>
        ///   Critical : Unsubscribes critical event handler
        /// </SecurityNote>
        [SecurityCritical]
        private void _RestoreFrameworkIssueFixups()
        {
            var rootElement = (FrameworkElement)VisualTreeHelper.GetChild(this._window, 0);

            // Undo anything that was done before.
            rootElement.Margin = new Thickness();

            // This margin is only necessary if the client rect is going to be calculated incorrectly by WPF.
            // This bug was fixed in V4 of the framework.
            if (Utility.IsPresentationFrameworkVersionLessThan4)
            {
                Assert.IsTrue(this._isFixedUp);
                this._window.StateChanged -= this._FixupRestoreBounds;
                this._isFixedUp = false;
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _RestoreGlassFrame()
        {
            Assert.IsNull(this._chromeInfo);
            Assert.IsNotNull(this._window);

            // Expect that this might be called on OSes other than Vista
            // and if the window hasn't yet been shown, then we don't need to undo anything.
            if (!Utility.IsOsVistaOrNewer || this._hwnd == IntPtr.Zero)
            {
                return;
            }

            var hwndSourceCompositionTarget = this._hwndSource.CompositionTarget;
            if (hwndSourceCompositionTarget != null)
                hwndSourceCompositionTarget.BackgroundColor = SystemColors.WindowColor;

            if (NativeMethods.DwmIsCompositionEnabled())
            {
                // If glass is enabled, push it back to the normal bounds.
                var dwmMargin = new MARGINS();
                NativeMethods.DwmExtendFrameIntoClientArea(this._hwnd, ref dwmMargin);
            }
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// </SecurityNote>
        [SecurityCritical]
        private void _RestoreHrgn()
        {
            this._ClearRoundingRegion();
            NativeMethods.SetWindowPos(this._hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpFlags);
        }

        #endregion
    }
}
