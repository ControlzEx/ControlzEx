#pragma warning disable 618
namespace ControlzEx.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management;
    using System.Security;
    using System.Security.Permissions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interactivity;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Threading;
    using ControlzEx;
    using ControlzEx.Native;
    using ControlzEx.Standard;
    using JetBrains.Annotations;    

    /// <summary>
    /// With this class we can make custom window styles.
    /// </summary>
    public partial class WindowChromeBehavior : Behavior<Window>    
    {
        /// <summary>Underlying HWND for the _window.</summary>
        /// <SecurityNote>
        ///   Critical : Critical member
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr windowHandle;

        /// <summary>Underlying HWND for the _window.</summary>
        /// <SecurityNote>
        ///   Critical : Critical member provides access to HWND's window messages which are critical
        /// </SecurityNote>
        [SecurityCritical]
        private HwndSource hwndSource;

        private PropertyChangeNotifier topMostChangeNotifier;
        private PropertyChangeNotifier borderThicknessChangeNotifier;
        private PropertyChangeNotifier resizeBorderThicknessChangeNotifier;
        private Thickness? savedBorderThickness;
        private Thickness? savedResizeBorderThickness;
        private bool savedTopMost;
        private bool isWindwos10OrHigher;

        private bool isCleanedUp;
        private IntPtr taskbarHandle;

        // Named property available for fully extending the glass frame.
        public static Thickness GlassFrameCompleteThickness { get { return new Thickness(-1); } }

        private struct _SystemParameterBoundProperty
        {
            public string SystemParameterPropertyName { get; set; }
            public DependencyProperty DependencyProperty { get; set; }
        }

        /// <summary>
        /// Mirror property for <see cref="ResizeBorderThickness"/>.
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
            DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(WindowChromeBehavior), new PropertyMetadata(GetDefaultResizeBorderThickness()), (value) => ((Thickness)value).IsNonNegative());

        /// <summary>
        /// Mirror property for <see cref="GlassFrameThickness"/>.
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
            DependencyProperty.Register(nameof(GlassFrameThickness), typeof(Thickness), typeof(WindowChromeBehavior), new PropertyMetadata(default(Thickness), OnGlassFrameThicknessChanged, (d, o) => _CoerceGlassFrameThickness((Thickness)o)));

        private static object _CoerceGlassFrameThickness(Thickness thickness)
        {
            // If it's explicitly set, but set to a thickness with at least one negative side then 
            // coerce the value to the stock GlassFrameCompleteThickness.
            if (!thickness.IsNonNegative())
            {
                return GlassFrameCompleteThickness;
            }

            return thickness;
        }

        public static readonly DependencyProperty CaptionHeightProperty = DependencyProperty.Register(nameof(CaptionHeight), typeof(double), typeof(WindowChromeBehavior), new PropertyMetadata(0d, (d, e) => ((WindowChromeBehavior)d)._OnChromePropertyChangedThatRequiresRepaint()), value => (double)value >= 0d);

        /// <summary>The extent of the top of the window to treat as the caption.</summary>
        public double CaptionHeight
        {
            get { return (double)this.GetValue(CaptionHeightProperty); }
            set { this.SetValue(CaptionHeightProperty, value); }
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
            DependencyProperty.Register(nameof(IgnoreTaskbarOnMaximize), typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(false, OnIgnoreTaskbarOnMaximizePropertyChanged));

        /// <summary>
        /// Gets/sets if the border thickness value should be kept on maximize
        /// if the MaxHeight/MaxWidth of the window is less than the monitor resolution.
        /// </summary>
        public bool KeepBorderOnMaximize
        {
            get { return (bool)this.GetValue(KeepBorderOnMaximizeProperty); }
            set { this.SetValue(KeepBorderOnMaximizeProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="KeepBorderOnMaximize"/>.
        /// </summary>
        public static readonly DependencyProperty KeepBorderOnMaximizeProperty = DependencyProperty.Register(nameof(KeepBorderOnMaximize), typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(true, OnKeepBorderOnMaximizeChanged));

        private static bool IsWindows10OrHigher()
        {
            var version = NtDll.RtlGetVersion();
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

            this.savedBorderThickness = this.AssociatedObject.BorderThickness;
            this.borderThicknessChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Control.BorderThicknessProperty);
            this.borderThicknessChangeNotifier.ValueChanged += this.BorderThicknessChangeNotifierOnValueChanged;

            this.savedResizeBorderThickness = this.ResizeBorderThickness;
            this.resizeBorderThicknessChangeNotifier = new PropertyChangeNotifier(this, ResizeBorderThicknessProperty);
            this.resizeBorderThicknessChangeNotifier.ValueChanged += this.ResizeBorderThicknessChangeNotifierOnValueChanged;

            this.savedTopMost = this.AssociatedObject.Topmost;
            this.topMostChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Window.TopmostProperty);
            this.topMostChangeNotifier.ValueChanged += this.TopMostChangeNotifierOnValueChanged;

            this.AssociatedObject.SourceInitialized += this.AssociatedObject_SourceInitialized;
            this.AssociatedObject.Loaded += this.AssociatedObject_Loaded;
            this.AssociatedObject.Unloaded += this.AssociatedObject_Unloaded;
            this.AssociatedObject.Closed += this.AssociatedObject_Closed;
            this.AssociatedObject.StateChanged += this.AssociatedObject_StateChanged;
            this.AssociatedObject.LostFocus += this.AssociatedObject_LostFocus;
            this.AssociatedObject.Deactivated += this.AssociatedObject_Deactivated;

            base.OnAttached();
        }

        private void TopMostHack()
        {
            if (this.AssociatedObject.Topmost)
            {
                var raiseValueChanged = this.topMostChangeNotifier.RaiseValueChanged;
                this.topMostChangeNotifier.RaiseValueChanged = false;
                this.AssociatedObject.Topmost = false;
                this.AssociatedObject.Topmost = true;
                this.topMostChangeNotifier.RaiseValueChanged = raiseValueChanged;
            }
        }

        /// <summary>
        /// Gets the default resize border thicknes from the system parameters.
        /// </summary>
        public static Thickness GetDefaultResizeBorderThickness()
        {
#if NET45 || NET462
            return SystemParameters.WindowResizeBorderThickness;
#else
            return SystemParameters2.Current.WindowResizeBorderThickness;
#endif
        }

        private void BorderThicknessChangeNotifierOnValueChanged(object sender, EventArgs e)
        {
            // It's bad if the window is null at this point, but we check this here to prevent the possible occurred exception
            var window = this.AssociatedObject;
            if (window != null)
            {
                this.savedBorderThickness = window.BorderThickness;
            }
        }

        private void ResizeBorderThicknessChangeNotifierOnValueChanged(object sender, EventArgs e)
        {
            this.savedResizeBorderThickness = this.ResizeBorderThickness;
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

            behavior._OnChromePropertyChangedThatRequiresRepaint();
        }

        private static void OnIgnoreTaskbarOnMaximizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowChromeBehavior)d;
            behavior._OnChromePropertyChangedThatRequiresRepaint();

            // A few things to consider when removing the below hack
            // - ResizeMode="NoResize"
            //   WindowState="Maximized"
            //   IgnoreTaskbarOnMaximize="True"
            // - Changing IgnoreTaskbarOnMaximize while window is maximized

            // Changing the WindowState solves all, known, issues with changing IgnoreTaskbarOnMaximize.
            // Since IgnoreTaskbarOnMaximize is not changed all the time this hack seems to be less risky than anything else.
            if (behavior.AssociatedObject.WindowState == WindowState.Maximized)
            {
                behavior.AssociatedObject.WindowState = WindowState.Normal;
                behavior.AssociatedObject.WindowState = WindowState.Maximized;
            }
        }

        private static void OnKeepBorderOnMaximizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowChromeBehavior)d;

            behavior.HandleMaximize();
        }

        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void Cleanup(bool isClosing)
        {
            if (!this.isCleanedUp)
            {
                this.isCleanedUp = true;

                if (this.taskbarHandle != IntPtr.Zero
                    && this.isWindwos10OrHigher)
                {
                    this.DeactivateTaskbarFix(this.taskbarHandle);
                }

                // clean up events
                this.AssociatedObject.SourceInitialized -= this.AssociatedObject_SourceInitialized;
                this.AssociatedObject.Loaded -= this.AssociatedObject_Loaded;
                this.AssociatedObject.Unloaded -= this.AssociatedObject_Unloaded;
                this.AssociatedObject.Closed -= this.AssociatedObject_Closed;
                this.AssociatedObject.StateChanged -= this.AssociatedObject_StateChanged;
                this.AssociatedObject.LostFocus -= this.AssociatedObject_LostFocus;
                this.AssociatedObject.Deactivated -= this.AssociatedObject_Deactivated;

                this.hwndSource?.RemoveHook(this.WindowProc);

                this._RestoreStandardChromeState(isClosing);
            }
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            this.Cleanup(false);

            base.OnDetaching();
        }

        private void AssociatedObject_SourceInitialized(object sender, EventArgs e)
        {
            this.windowHandle = new WindowInteropHelper(this.AssociatedObject).Handle;

            if (IntPtr.Zero == this.windowHandle)
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
                                                  if (UnsafeNativeMethods.GetWindowRect(this.windowHandle, out rect))
                                                  {
                                                      var flags = SWP.SHOWWINDOW;
                                                      if (!this.AssociatedObject.ShowActivated)
                                                      {
                                                          flags |= SWP.NOACTIVATE;
                                                      }
                                                      NativeMethods.SetWindowPos(this.windowHandle, Constants.HWND_NOTOPMOST, rect.Left, rect.Top, rect.Width, rect.Height, flags);
                                                  }
                                              });
            }

            this.hwndSource = HwndSource.FromHwnd(this.windowHandle);
            this.hwndSource?.AddHook(this.WindowProc);

            this._ApplyNewCustomChrome();

            // handle the maximized state here too (to handle the border in a correct way)
            this.HandleMaximize();
        }

        /// <summary>
        /// Is called when the associated object of this instance is loaded
        /// </summary>
        protected virtual void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Cleanup(false);
        }

        private void AssociatedObject_Closed(object sender, EventArgs e)
        {
            this.Cleanup(true);
        }

        private void AssociatedObject_StateChanged(object sender, EventArgs e)
        {
            this.HandleMaximize();
        }

        private void AssociatedObject_Deactivated(object sender, EventArgs e)
        {
            this.TopMostHack();
        }

        private void AssociatedObject_LostFocus(object sender, RoutedEventArgs e)
        {
            this.TopMostHack();
        }

        private void HandleMaximize()
        {
            var raiseValueChanged = this.topMostChangeNotifier.RaiseValueChanged;
            this.topMostChangeNotifier.RaiseValueChanged = false;

            this.HandleBorderAndResizeBorderThicknessDuringMaximize();

            if (this.AssociatedObject.WindowState == WindowState.Maximized)
            {
                if (this.windowHandle != IntPtr.Zero)
                {
                    // WindowChrome handles the size false if the main monitor is lesser the monitor where the window is maximized
                    // so set the window pos/size twice
                    var monitor = UnsafeNativeMethods.MonitorFromWindow(this.windowHandle, MonitorOptions.MONITOR_DEFAULTTONEAREST);
                    if (monitor != IntPtr.Zero)
                    {
                        var monitorInfo = NativeMethods.GetMonitorInfo(monitor);
                        var monitorRect = this.IgnoreTaskbarOnMaximize ? monitorInfo.rcMonitor : monitorInfo.rcWork;

                        var x = monitorRect.Left;
                        var y = monitorRect.Top;
                        var cx = monitorRect.Width;
                        var cy = monitorRect.Height;

                        if (this.IgnoreTaskbarOnMaximize
                            && this.isWindwos10OrHigher)
                        {
                            this.ActivateTaskbarFix(monitor);
                        }

                        NativeMethods.SetWindowPos(this.windowHandle, Constants.HWND_NOTOPMOST, x, y, cx, cy, SWP.SHOWWINDOW);
                    }
                }
            }
            else
            {
                // #2694 make sure the window is not on top after restoring window
                // this issue was introduced after fixing the windows 10 bug with the taskbar and a maximized window that ignores the taskbar
                if (this.taskbarHandle != IntPtr.Zero
                    && this.isWindwos10OrHigher)
                {
                    this.DeactivateTaskbarFix(this.taskbarHandle);
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

        /// <summary>
        /// This fix is needed because style triggers don't work if someone sets the value locally/directly on the window.
        /// </summary>
        private void HandleBorderAndResizeBorderThicknessDuringMaximize()
        {
            this.borderThicknessChangeNotifier.RaiseValueChanged = false;
            this.resizeBorderThicknessChangeNotifier.RaiseValueChanged = false;

            if (this.AssociatedObject.WindowState == WindowState.Maximized)
            {
                var monitor = IntPtr.Zero;

                if (this.windowHandle != IntPtr.Zero)
                {
                    monitor = UnsafeNativeMethods.MonitorFromWindow(this.windowHandle, MonitorOptions.MONITOR_DEFAULTTONEAREST);
                }

                if (monitor != IntPtr.Zero)
                {
                    var monitorInfo = NativeMethods.GetMonitorInfo(monitor);
                    var monitorRect = this.IgnoreTaskbarOnMaximize ? monitorInfo.rcMonitor : monitorInfo.rcWork;

                    var rightBorderThickness = 0D;
                    var bottomBorderThickness = 0D;

                    if (this.KeepBorderOnMaximize
                        && this.savedBorderThickness.HasValue)
                    {
                        // If the maximized window will have a width less than the monitor size, show the right border.
                        if (this.AssociatedObject.MaxWidth < monitorRect.Width)
                        {
                            rightBorderThickness = this.savedBorderThickness.Value.Right;
                        }

                        // If the maximized window will have a height less than the monitor size, show the bottom border.
                        if (this.AssociatedObject.MaxHeight < monitorRect.Height)
                        {
                            bottomBorderThickness = this.savedBorderThickness.Value.Bottom;
                        }
                    }

                    // set window border, so we can move the window from top monitor position
                    this.AssociatedObject.BorderThickness = new Thickness(0, 0, rightBorderThickness, bottomBorderThickness);
                }
                else // Can't get monitor info, so just remove all border thickness
                {
                    this.AssociatedObject.BorderThickness = new Thickness(0);
                }

                this.ResizeBorderThickness = new Thickness(0);
            }
            else
            {
                this.AssociatedObject.BorderThickness = this.savedBorderThickness.GetValueOrDefault(new Thickness(0));

                var resizeBorderThickness = this.savedResizeBorderThickness.GetValueOrDefault(new Thickness(0));

                if (this.ResizeBorderThickness != resizeBorderThickness)
                {
                    this.ResizeBorderThickness = resizeBorderThickness;
                }
            }

            this.borderThicknessChangeNotifier.RaiseValueChanged = true;
            this.resizeBorderThicknessChangeNotifier.RaiseValueChanged = true;
        }

        private void ActivateTaskbarFix(IntPtr monitor)
        {
            var trayWndHandle = NativeMethods.GetTaskBarHandleForMonitor(monitor);

            if (trayWndHandle != IntPtr.Zero)
            {
                this.taskbarHandle = trayWndHandle;
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HWND_BOTTOM, 0, 0, 0, 0, SWP.TOPMOST);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HWND_TOP, 0, 0, 0, 0, SWP.TOPMOST);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HWND_NOTOPMOST, 0, 0, 0, 0, SWP.TOPMOST);
            }
        }

        private void DeactivateTaskbarFix(IntPtr trayWndHandle)
        {
            if (trayWndHandle != IntPtr.Zero)
            {
                this.taskbarHandle = IntPtr.Zero;
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HWND_BOTTOM, 0, 0, 0, 0, SWP.TOPMOST);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HWND_TOP, 0, 0, 0, 0, SWP.TOPMOST);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HWND_TOPMOST, 0, 0, 0, 0, SWP.TOPMOST);
            }
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

        private static readonly List<_SystemParameterBoundProperty> _BoundProperties = new List<_SystemParameterBoundProperty>
                                                                                       {
                                                                                           new _SystemParameterBoundProperty { DependencyProperty = CaptionHeightProperty, SystemParameterPropertyName = "WindowCaptionHeight" },
                                                                                           new _SystemParameterBoundProperty { DependencyProperty = ResizeBorderThicknessProperty, SystemParameterPropertyName = "WindowResizeBorderThickness" },
                                                                                           new _SystemParameterBoundProperty { DependencyProperty = GlassFrameThicknessProperty, SystemParameterPropertyName = "WindowNonClientFrameThickness" },
                                                                                       };
    }
}