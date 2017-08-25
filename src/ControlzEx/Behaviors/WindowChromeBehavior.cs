#pragma warning disable 618
using System;
using System.Linq;
using System.Management;
using System.Security;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Interop;
using Microsoft.Windows.Shell;

namespace ControlzEx.Behaviors
{
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Threading;
    using ControlzEx;
    using ControlzEx.Native;
    using JetBrains.Annotations;
    using Standard;

    /// <summary>
    /// With this class we can make custom window styles.
    /// </summary>
    public class WindowChromeBehavior : Behavior<Window>    
    {
        private IntPtr handle;
        private HwndSource hwndSource;
        private WindowChrome windowChrome;
        private PropertyChangeNotifier topMostChangeNotifier;
        private bool savedTopMost;
        private bool isWindwos10OrHigher;

        #region Mirror properties for WindowChrome

        /// <summary>
        /// Mirror property for <see cref="WindowChrome.ResizeBorderThickness"/>.
        /// </summary>
        public Thickness ResizeBorderThickness
        {
            get { return (Thickness)this.GetValue(ResizeBorderThicknessProperty); }
            set { this.SetValue(ResizeBorderThicknessProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="ResizeBorderThickness"/>.
        /// </summary>
        public static readonly DependencyProperty ResizeBorderThicknessProperty =
            DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(WindowChromeBehavior), new PropertyMetadata(GetDefaultResizeBorderThickness()));

        /// <summary>
        /// Mirror property for <see cref="WindowChrome.GlassFrameThickness"/>.
        /// </summary>
        public Thickness GlassFrameThickness
        {
            get { return (Thickness)this.GetValue(GlassFrameThicknessProperty); }
            set { this.SetValue(GlassFrameThicknessProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="GlassFrameThickness"/>.
        /// </summary>
        public static readonly DependencyProperty GlassFrameThicknessProperty =
            DependencyProperty.Register(nameof(GlassFrameThickness), typeof(Thickness), typeof(WindowChromeBehavior), new PropertyMetadata(default(Thickness), OnGlassFrameThicknessChanged));

        #endregion

        /// <summary>
        /// Defines if the Taskbar should be ignored when maximizing a Window.
        /// This only works with WindowStyle = None.
        /// </summary>
        public bool IgnoreTaskbarOnMaximize
        {
            get { return (bool)this.GetValue(IgnoreTaskbarOnMaximizeProperty); }
            set { this.SetValue(IgnoreTaskbarOnMaximizeProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="IgnoreTaskbarOnMaximize"/>.
        /// </summary>
        public static readonly DependencyProperty IgnoreTaskbarOnMaximizeProperty =
            DependencyProperty.Register(nameof(IgnoreTaskbarOnMaximize), typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(false, IgnoreTaskbarOnMaximizePropertyChangedCallback));

        private static bool IsWindows10OrHigher()
        {
            var version = Standard.NtDll.RtlGetVersion();
            if (default(Version) == version)
            {
                // Snippet from Koopakiller https://dotnet-snippets.de/snippet/os-version-name-mit-wmi/4929
                using (var mos = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem"))
                {
                    var attribs = mos.Get().OfType<ManagementObject>();
                    //caption = attribs.FirstOrDefault().GetPropertyValue("Caption").ToString() ?? "Unknown";
                    version = new Version((attribs.FirstOrDefault()?.GetPropertyValue("Version") ?? "0.0.0.0").ToString());
                }
            }
            return version >= new Version(10, 0);
        }

        /// <inheritdoc />
        protected override void OnAttached()
        {
            this.isWindwos10OrHigher = IsWindows10OrHigher();

            this.InitializeWindowChrome();            

            // no transparany, because it hase more then one unwanted issues
            if (this.AssociatedObject.AllowsTransparency
                && this.AssociatedObject.IsLoaded == false 
                && new WindowInteropHelper(this.AssociatedObject).Handle == IntPtr.Zero)
            {
                try
                {
                    this.AssociatedObject.AllowsTransparency = false;
                }
                catch (Exception)
                {
                    //For some reason, we can't determine if the window has loaded or not, so we swallow the exception.
                }
            }
            this.AssociatedObject.WindowStyle = WindowStyle.None;

            this.savedTopMost = this.AssociatedObject.Topmost;
            this.topMostChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Window.TopmostProperty);
            this.topMostChangeNotifier.ValueChanged += this.TopMostChangeNotifierOnValueChanged;

            var topmostHack = new Action(() =>
                                           {
                                               if (this.AssociatedObject.Topmost)
                                               {
                                                   var raiseValueChanged = this.topMostChangeNotifier.RaiseValueChanged;
                                                   this.topMostChangeNotifier.RaiseValueChanged = false;
                                                   this.AssociatedObject.Topmost = false;
                                                   this.AssociatedObject.Topmost = true;
                                                   this.topMostChangeNotifier.RaiseValueChanged = raiseValueChanged;
                                               }
                                           });
            this.AssociatedObject.LostFocus += (sender, args) => { topmostHack(); };
            this.AssociatedObject.Deactivated += (sender, args) => { topmostHack(); };

            this.AssociatedObject.Loaded += this.AssociatedObject_Loaded;
            this.AssociatedObject.Unloaded += this.AssociatedObjectUnloaded;
            this.AssociatedObject.Closed += this.AssociatedObjectClosed;
            this.AssociatedObject.SourceInitialized += this.AssociatedObject_SourceInitialized;
            this.AssociatedObject.StateChanged += this.OnAssociatedObjectHandleMaximize;

            base.OnAttached();
        }

        private void InitializeWindowChrome()
        {
            this.windowChrome = new WindowChrome();

            BindingOperations.SetBinding(this.windowChrome, WindowChrome.ResizeBorderThicknessProperty, new Binding { Path = new PropertyPath(ResizeBorderThicknessProperty), Source = this });
            BindingOperations.SetBinding(this.windowChrome, WindowChrome.GlassFrameThicknessProperty, new Binding { Path = new PropertyPath(GlassFrameThicknessProperty), Source = this });
            this.windowChrome.CaptionHeight = 0;
            this.windowChrome.CornerRadius = default(CornerRadius);
            this.windowChrome.UseAeroCaptionButtons = false;

            this.AssociatedObject.SetValue(WindowChrome.WindowChromeProperty, this.windowChrome);
        }

        /// <summary>
        /// Gets the default resize border thicknes from the system parameters.
        /// </summary>
        public static Thickness GetDefaultResizeBorderThickness()
        {
#if NET45 || NET462
            return SystemParameters.WindowResizeBorderThickness;
#else
            return Microsoft.Windows.Shell.SystemParameters2.Current.WindowResizeBorderThickness;
#endif
        }

        private void TopMostChangeNotifierOnValueChanged(object sender, EventArgs e)
        {
            // It's bad if the window is null at this point, but we check this here to prevent the possible occurred exception
            var window = this.AssociatedObject;
            if (window != null)
            {
                this.savedTopMost = window.Topmost;
            }
        }

        private static void OnGlassFrameThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowChromeBehavior)d;

            if (behavior.AssociatedObject == null)
            {
                return;
            }

            behavior.AssociatedObject.SetValue(WindowChrome.WindowChromeProperty, null);
            behavior.InitializeWindowChrome();
        }

        private static void IgnoreTaskbarOnMaximizePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowChromeBehavior)sender;
            if (behavior.windowChrome != null)
            {
                if (!Equals(behavior.windowChrome.IgnoreTaskbarOnMaximize, behavior.IgnoreTaskbarOnMaximize))
                {
                    // another special hack to avoid nasty resizing
                    // repro
                    // ResizeMode="NoResize"
                    // WindowState="Maximized"
                    // IgnoreTaskbarOnMaximize="True"
                    // this only happens if we change this at runtime
                    behavior.windowChrome.IgnoreTaskbarOnMaximize = behavior.IgnoreTaskbarOnMaximize;

                    if (behavior.AssociatedObject.WindowState == WindowState.Maximized)
                    {
                        behavior.AssociatedObject.WindowState = WindowState.Normal;
                        behavior.AssociatedObject.WindowState = WindowState.Maximized;
                    }
                }
            }
        }

        private bool isCleanedUp;

        private void Cleanup()
        {
            if (!this.isCleanedUp)
            {
                this.isCleanedUp = true;

                var handleTaskbar = GetHandleTaskbar(this.AssociatedObject);

                if (handleTaskbar != IntPtr.Zero
                    && this.isWindwos10OrHigher)
                {
                    this.DeactivateTaskbarFix(handleTaskbar);
                }

                // clean up events
                this.AssociatedObject.Loaded -= this.AssociatedObject_Loaded;
                this.AssociatedObject.Unloaded -= this.AssociatedObjectUnloaded;
                this.AssociatedObject.Closed -= this.AssociatedObjectClosed;
                this.AssociatedObject.SourceInitialized -= this.AssociatedObject_SourceInitialized;
                this.AssociatedObject.StateChanged -= this.OnAssociatedObjectHandleMaximize;
                this.hwndSource?.RemoveHook(this.WindowProc);
                this.windowChrome = null;
            }
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            this.Cleanup();
            base.OnDetaching();
        }

        private void AssociatedObjectUnloaded(object sender, RoutedEventArgs e)
        {
            this.Cleanup();
        }

        private void AssociatedObjectClosed(object sender, EventArgs e)
        {
            this.Cleanup();
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var returnval = IntPtr.Zero;

            switch (msg)
            {
                case (int)WM.NCPAINT:
                    handled = this.GlassFrameThickness == default(Thickness) && this.GlowBrush == null;
                    break;

                case (int)WM.WINDOWPOSCHANGING:
                    {
                        var pos = (WINDOWPOS)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
                        if ((pos.flags & Standard.SWP.NOMOVE) != 0)
                        {
                            return IntPtr.Zero;
                        }

                        var wnd = this.AssociatedObject;
                        if (wnd == null || this.hwndSource?.CompositionTarget == null)
                        {
                            return IntPtr.Zero;
                        }

                        var changedPos = false;

                        // Convert the original to original size based on DPI setting. Need for x% screen DPI.
                        var matrix = this.hwndSource.CompositionTarget.TransformToDevice;

                        var minWidth = wnd.MinWidth * matrix.M11;
                        var minHeight = wnd.MinHeight * matrix.M22;
                        if (pos.cx < minWidth) { pos.cx = (int)minWidth; changedPos = true; }
                        if (pos.cy < minHeight) { pos.cy = (int)minHeight; changedPos = true; }

                        var maxWidth = wnd.MaxWidth * matrix.M11;
                        var maxHeight = wnd.MaxHeight * matrix.M22;
                        if (pos.cx > maxWidth && maxWidth > 0) { pos.cx = (int)Math.Round(maxWidth); changedPos = true; }
                        if (pos.cy > maxHeight && maxHeight > 0) { pos.cy = (int)Math.Round(maxHeight); changedPos = true; }

                        if (!changedPos)
                        {
                            return IntPtr.Zero;
                        }

                        System.Runtime.InteropServices.Marshal.StructureToPtr(pos, lParam, true);
                        handled = true;
                    }
                    break;
            }

            return returnval;
        }

        private void OnAssociatedObjectHandleMaximize(object sender, EventArgs e)
        {
            this.HandleMaximize();
        }

        private void HandleMaximize()
        {
            var raiseValueChanged = this.topMostChangeNotifier.RaiseValueChanged;
            this.topMostChangeNotifier.RaiseValueChanged = false;

            if (this.AssociatedObject.WindowState == WindowState.Maximized)
            {
                var ignoreTaskBar = this.IgnoreTaskbarOnMaximize;
                if (this.handle != IntPtr.Zero)
                {
                    // WindowChrome handles the size false if the main monitor is lesser the monitor where the window is maximized
                    // so set the window pos/size twice
                    var monitor = UnsafeNativeMethods.MonitorFromWindow(this.handle, MonitorOptions.MONITOR_DEFAULTTONEAREST);
                    if (monitor != IntPtr.Zero)
                    {
                        var monitorInfo = NativeMethods.GetMonitorInfo(monitor);
                        var monitorRect = ignoreTaskBar ? monitorInfo.rcMonitor : monitorInfo.rcWork;

                        var x = monitorRect.Left;
                        var y = monitorRect.Top;
                        var cx = monitorRect.Width;
                        var cy = monitorRect.Height;

                        if (ignoreTaskBar && this.isWindwos10OrHigher)
                        {
                            this.ActivateTaskbarFix(monitor);
                        }

                        NativeMethods.SetWindowPos(this.handle, new IntPtr(-2), x, y, cx, cy, SWP.SHOWWINDOW);
                    }
                }
            }
            else
            {
                // #2694 make sure the window is not on top after restoring window
                // this issue was introduced after fixing the windows 10 bug with the taskbar and a maximized window that ignores the taskbar
                if (GetHandleTaskbar(this.AssociatedObject) != IntPtr.Zero
                    && this.isWindwos10OrHigher)
                {
                    var monitor = UnsafeNativeMethods.MonitorFromWindow(this.handle, MonitorOptions.MONITOR_DEFAULTTONEAREST);

                    this.DeactivateTaskbarFix(monitor);
                }
            }

            // fix nasty TopMost bug
            // - set TopMost="True"
            // - start mahapps demo
            // - TopMost works
            // - maximize window and back to normal
            // - TopMost is gone
            //
            // Problem with minimize animation when window is maximized #1528
            // 1. Activate another application (such as Google Chrome).
            // 2. Run the demo and maximize it.
            // 3. Minimize the demo by clicking on the taskbar button.
            // Note that the minimize animation in this case does actually run, but somehow the other
            // application (Google Chrome in this example) is instantly switched to being the top window,
            // and so blocking the animation view.
            this.AssociatedObject.Topmost = false;
            this.AssociatedObject.Topmost = this.AssociatedObject.WindowState == WindowState.Minimized || this.savedTopMost;

            this.topMostChangeNotifier.RaiseValueChanged = raiseValueChanged;
        }

        private void ActivateTaskbarFix(IntPtr monitor)
        {
            var trayWndHandle = NativeMethods.GetTaskBarHandleForMonitor(monitor);

            if (trayWndHandle != IntPtr.Zero)
            {
                SetHandleTaskbar(this.AssociatedObject, trayWndHandle);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HWND_BOTTOM, 0, 0, 0, 0, SWP.TOPMOST);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HWND_TOP, 0, 0, 0, 0, SWP.TOPMOST);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HWND_NOTOPMOST, 0, 0, 0, 0, SWP.TOPMOST);
            }
        }

        private void DeactivateTaskbarFix(IntPtr trayWndHandle)
        {
            if (trayWndHandle != IntPtr.Zero)
            {
                SetHandleTaskbar(this.AssociatedObject, IntPtr.Zero);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HWND_BOTTOM, 0, 0, 0, 0, SWP.TOPMOST);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HWND_TOP, 0, 0, 0, 0, SWP.TOPMOST);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HWND_TOPMOST, 0, 0, 0, 0, SWP.TOPMOST);
            }
        }

        private static readonly DependencyProperty HandleTaskbarProperty
            = DependencyProperty.RegisterAttached(
                "HandleTaskbar",
                typeof(IntPtr),
                typeof(WindowChromeBehavior), new FrameworkPropertyMetadata(IntPtr.Zero));

        private static IntPtr GetHandleTaskbar(UIElement element)
        {
            return (IntPtr)element.GetValue(HandleTaskbarProperty);
        }

        private static void SetHandleTaskbar(UIElement element, IntPtr value)
        {
            element.SetValue(HandleTaskbarProperty, value);
        }

        private void AssociatedObject_SourceInitialized(object sender, EventArgs e)
        {
            this.handle = new WindowInteropHelper(this.AssociatedObject).Handle;
            if (IntPtr.Zero == this.handle)
            {
                throw new Exception("Uups, at this point we really need the Handle from the associated object!");
            }

            if (this.AssociatedObject.SizeToContent != SizeToContent.Manual && this.AssociatedObject.WindowState == WindowState.Normal)
            {
                // Another try to fix SizeToContent
                // without this we get nasty glitches at the borders
                Invoke(this.AssociatedObject, () =>
                    {
                        this.AssociatedObject.InvalidateMeasure();
                        RECT rect;
                        if (UnsafeNativeMethods.GetWindowRect(this.handle, out rect))
                        {
                            var flags = SWP.SHOWWINDOW;
                            if (!this.AssociatedObject.ShowActivated)
                            {
                                flags |= SWP.NOACTIVATE;
                            }
                            NativeMethods.SetWindowPos(this.handle, new IntPtr(-2), rect.Left, rect.Top, rect.Width, rect.Height, flags);
                        }
                    });
            }

            this.hwndSource = HwndSource.FromHwnd(this.handle);
            this.hwndSource?.AddHook(this.WindowProc);

            // handle the maximized state here too (to handle the border in a correct way)
            this.HandleMaximize();
        }

        /// <summary>
        /// Is called when the associated object of this instance is loaded
        /// </summary>
        protected virtual void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="GlowBrush"/>.
        /// </summary>
        public static readonly DependencyProperty GlowBrushProperty = DependencyProperty.Register(nameof(GlowBrush), typeof(Brush), typeof(WindowChromeBehavior), new PropertyMetadata());

        /// <summary>
        /// Mirror property for GlowBrush from MetroWindow.
        /// </summary>
        public Brush GlowBrush
        {
            get { return (Brush)this.GetValue(GlowBrushProperty); }
            set { this.SetValue(GlowBrushProperty, value); }
        }

        private static void Invoke([NotNull] DispatcherObject dispatcherObject, [NotNull] Action invokeAction)
        {
            if (dispatcherObject == null)
            {
                throw new ArgumentNullException(nameof(dispatcherObject));
            }
            if (invokeAction == null)
            {
                throw new ArgumentNullException(nameof(invokeAction));
            }
            if (dispatcherObject.Dispatcher.CheckAccess())
            {
                invokeAction();
            }
            else
            {
                dispatcherObject.Dispatcher.Invoke(invokeAction);
            }
        }
    }
}