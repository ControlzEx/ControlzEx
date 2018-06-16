#pragma warning disable 618
namespace ControlzEx.Behaviors
{
    using System;
    using System.Linq;
    using System.Management;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Interactivity;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Threading;
    using ControlzEx;
    using ControlzEx.Native;
    using ControlzEx.Standard;
    using ControlzEx.Windows.Shell;
    using JetBrains.Annotations;    

    /// <summary>
    /// With this class we can make custom window styles.
    /// </summary>
    public class WindowChromeBehavior : Behavior<Window>    
    {
        private IntPtr _handle;
        private HwndSource _hwndSource;
        private WindowChrome _windowChrome;
        private PropertyChangeNotifier _topMostChangeNotifier;
        private PropertyChangeNotifier _borderThicknessChangeNotifier;
        private PropertyChangeNotifier _resizeBorderThicknessChangeNotifier;
        private Thickness? _savedBorderThickness;
        private Thickness? _savedResizeBorderThickness;
        private bool _savedTopMost;
        private bool _isWindwos10OrHigher;

        #region Mirror properties for WindowChrome

        /// <summary>
        /// Mirror property for <see cref="WindowChrome.ResizeBorderThickness"/>.
        /// </summary>
        public Thickness ResizeBorderThickness
        {
            get => (Thickness)this.GetValue(ResizeBorderThicknessProperty);
            set => this.SetValue(ResizeBorderThicknessProperty, value);
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
            get => (Thickness)this.GetValue(GlassFrameThicknessProperty);
            set => this.SetValue(GlassFrameThicknessProperty, value);
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="GlassFrameThickness"/>.
        /// </summary>
        public static readonly DependencyProperty GlassFrameThicknessProperty =
            DependencyProperty.Register(nameof(GlassFrameThickness), typeof(Thickness), typeof(WindowChromeBehavior), new PropertyMetadata(default(Thickness), OnGlassFrameThicknessChanged));

        #endregion

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="GlowBrush"/>.
        /// </summary>
        public static readonly DependencyProperty GlowBrushProperty = DependencyProperty.Register(nameof(GlowBrush), typeof(Brush), typeof(WindowChromeBehavior), new PropertyMetadata());

        /// <summary>
        /// Mirror property for GlowBrush from MetroWindow.
        /// </summary>
        public Brush GlowBrush
        {
            get => (Brush)this.GetValue(GlowBrushProperty);
            set => this.SetValue(GlowBrushProperty, value);
        }

        /// <summary>
        /// Defines if the Taskbar should be ignored when maximizing a Window.
        /// This only works with WindowStyle = None.
        /// </summary>
        public bool IgnoreTaskbarOnMaximize
        {
            get => (bool)this.GetValue(IgnoreTaskbarOnMaximizeProperty);
            set => this.SetValue(IgnoreTaskbarOnMaximizeProperty, value);
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
            get => (bool)this.GetValue(KeepBorderOnMaximizeProperty);
            set => this.SetValue(KeepBorderOnMaximizeProperty, value);
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
            this._isWindwos10OrHigher = IsWindows10OrHigher();

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

            this._savedBorderThickness = this.AssociatedObject.BorderThickness;
            this._borderThicknessChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Control.BorderThicknessProperty);
            this._borderThicknessChangeNotifier.ValueChanged += this.BorderThicknessChangeNotifierOnValueChanged;

            this._savedResizeBorderThickness = this.ResizeBorderThickness;
            this._resizeBorderThicknessChangeNotifier = new PropertyChangeNotifier(this, ResizeBorderThicknessProperty);
            this._resizeBorderThicknessChangeNotifier.ValueChanged += this.ResizeBorderThicknessChangeNotifierOnValueChanged;

            this._savedTopMost = this.AssociatedObject.Topmost;
            this._topMostChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Window.TopmostProperty);
            this._topMostChangeNotifier.ValueChanged += this.TopMostChangeNotifierOnValueChanged;

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
                var raiseValueChanged = this._topMostChangeNotifier.RaiseValueChanged;
                this._topMostChangeNotifier.RaiseValueChanged = false;
                this.AssociatedObject.Topmost = false;
                this.AssociatedObject.Topmost = true;
                this._topMostChangeNotifier.RaiseValueChanged = raiseValueChanged;
            }
        }

        private void InitializeWindowChrome()
        {
            this._windowChrome = new WindowChrome();

            BindingOperations.SetBinding(this._windowChrome, WindowChrome.ResizeBorderThicknessProperty, new Binding { Path = new PropertyPath(ResizeBorderThicknessProperty), Source = this });
            BindingOperations.SetBinding(this._windowChrome, WindowChrome.GlassFrameThicknessProperty, new Binding { Path = new PropertyPath(GlassFrameThicknessProperty), Source = this });
            this._windowChrome.CaptionHeight = 0;
            this._windowChrome.CornerRadius = default(CornerRadius);
            this._windowChrome.UseAeroCaptionButtons = false;

            this.AssociatedObject.SetValue(WindowChrome.WindowChromeProperty, this._windowChrome);
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
                this._savedBorderThickness = window.BorderThickness;
            }
        }

        private void ResizeBorderThicknessChangeNotifierOnValueChanged(object sender, EventArgs e)
        {
            this._savedResizeBorderThickness = this.ResizeBorderThickness;
        }

        private void TopMostChangeNotifierOnValueChanged(object sender, EventArgs e)
        {
            // It's bad if the window is null at this point, but we check this here to prevent the possible occurred exception
            var window = this.AssociatedObject;
            if (window != null)
            {
                this._savedTopMost = window.Topmost;
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

        private static void OnIgnoreTaskbarOnMaximizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowChromeBehavior)d;
            if (behavior._windowChrome != null)
            {
                if (!Equals(behavior._windowChrome.IgnoreTaskbarOnMaximize, behavior.IgnoreTaskbarOnMaximize))
                {
                    // another special hack to avoid nasty resizing
                    // repro
                    // ResizeMode="NoResize"
                    // WindowState="Maximized"
                    // IgnoreTaskbarOnMaximize="True"
                    // this only happens if we change this at runtime
                    behavior._windowChrome.IgnoreTaskbarOnMaximize = behavior.IgnoreTaskbarOnMaximize;

                    if (behavior.AssociatedObject.WindowState == WindowState.Maximized)
                    {
                        behavior.AssociatedObject.WindowState = WindowState.Normal;
                        behavior.AssociatedObject.WindowState = WindowState.Maximized;
                    }
                }
            }
        }

        private static void OnKeepBorderOnMaximizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowChromeBehavior)d;

            behavior.HandleMaximize();
        }

        private bool _isCleanedUp;
        private IntPtr _taskbarHandle;

        private void Cleanup()
        {
            if (!this._isCleanedUp)
            {
                this._isCleanedUp = true;

                if (this._taskbarHandle != IntPtr.Zero
                    && this._isWindwos10OrHigher)
                {
                    this.DeactivateTaskbarFix(this._taskbarHandle);
                }

                // clean up events
                this.AssociatedObject.SourceInitialized -= this.AssociatedObject_SourceInitialized;
                this.AssociatedObject.Loaded -= this.AssociatedObject_Loaded;
                this.AssociatedObject.Unloaded -= this.AssociatedObject_Unloaded;
                this.AssociatedObject.Closed -= this.AssociatedObject_Closed;
                this.AssociatedObject.StateChanged -= this.AssociatedObject_StateChanged;
                this.AssociatedObject.LostFocus -= this.AssociatedObject_LostFocus;
                this.AssociatedObject.Deactivated -= this.AssociatedObject_Deactivated;

                this._hwndSource?.RemoveHook(this.WindowProc);
                this._windowChrome = null;
            }
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            this.Cleanup();

            base.OnDetaching();
        }

        private void AssociatedObject_SourceInitialized(object sender, EventArgs e)
        {
            this._handle = new WindowInteropHelper(this.AssociatedObject).Handle;

            if (IntPtr.Zero == this._handle)
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
                                                  if (UnsafeNativeMethods.GetWindowRect(this._handle, out RECT rect))
                                                  {
                                                      var flags = SWP.SHOWWINDOW;
                                                      if (!this.AssociatedObject.ShowActivated)
                                                      {
                                                          flags |= SWP.NOACTIVATE;
                                                      }
                                                      NativeMethods.SetWindowPos(this._handle, Constants.HwndNotopmost, rect.Left, rect.Top, rect.Width, rect.Height, flags);
                                                  }
                                              });
            }

            this._hwndSource = HwndSource.FromHwnd(this._handle);
            this._hwndSource?.AddHook(this.WindowProc);

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
            this.Cleanup();
        }

        private void AssociatedObject_Closed(object sender, EventArgs e)
        {
            this.Cleanup();
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
                    if ((pos.flags & SWP.NOMOVE) != 0)
                    {
                        return IntPtr.Zero;
                    }

                    var wnd = this.AssociatedObject;
                    if (wnd == null || this._hwndSource?.CompositionTarget == null)
                    {
                        return IntPtr.Zero;
                    }

                    var changedPos = false;

                    // Convert the original to original size based on DPI setting. Need for x% screen DPI.
                    var matrix = this._hwndSource.CompositionTarget.TransformToDevice;

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

        private void HandleMaximize()
        {
            var raiseValueChanged = this._topMostChangeNotifier.RaiseValueChanged;
            this._topMostChangeNotifier.RaiseValueChanged = false;

            this.HandleBorderAndResizeBorderThicknessDuringMaximize();

            if (this.AssociatedObject.WindowState == WindowState.Maximized)
            {
                if (this._handle != IntPtr.Zero)
                {
                    // WindowChrome handles the size false if the main monitor is lesser the monitor where the window is maximized
                    // so set the window pos/size twice
                    var monitor = UnsafeNativeMethods.MonitorFromWindow(this._handle, MonitorOptions.MonitorDefaulttonearest);
                    if (monitor != IntPtr.Zero)
                    {
                        var monitorInfo = NativeMethods.GetMonitorInfo(monitor);
                        var monitorRect = this.IgnoreTaskbarOnMaximize ? monitorInfo.rcMonitor : monitorInfo.rcWork;

                        var x = monitorRect.Left;
                        var y = monitorRect.Top;
                        var cx = monitorRect.Width;
                        var cy = monitorRect.Height;

                        if (this.IgnoreTaskbarOnMaximize
                            && this._isWindwos10OrHigher)
                        {
                            this.ActivateTaskbarFix(monitor);
                        }

                        NativeMethods.SetWindowPos(this._handle, Constants.HwndNotopmost, x, y, cx, cy, SWP.SHOWWINDOW);
                    }
                }
            }
            else
            {
                // #2694 make sure the window is not on top after restoring window
                // this issue was introduced after fixing the windows 10 bug with the taskbar and a maximized window that ignores the taskbar
                if (this._taskbarHandle != IntPtr.Zero
                    && this._isWindwos10OrHigher)
                {
                    this.DeactivateTaskbarFix(this._taskbarHandle);
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
            this.AssociatedObject.Topmost = this.AssociatedObject.WindowState == WindowState.Minimized || this._savedTopMost;

            this._topMostChangeNotifier.RaiseValueChanged = raiseValueChanged;
        }

        /// <summary>
        /// This fix is needed because style triggers don't work if someone sets the value locally/directly on the window.
        /// </summary>
        private void HandleBorderAndResizeBorderThicknessDuringMaximize()
        {
            this._borderThicknessChangeNotifier.RaiseValueChanged = false;
            this._resizeBorderThicknessChangeNotifier.RaiseValueChanged = false;

            if (this.AssociatedObject.WindowState == WindowState.Maximized)
            {
                var monitor = IntPtr.Zero;

                if (this._handle != IntPtr.Zero)
                {
                    monitor = UnsafeNativeMethods.MonitorFromWindow(this._handle, MonitorOptions.MonitorDefaulttonearest);
                }

                if (monitor != IntPtr.Zero)
                {
                    var monitorInfo = NativeMethods.GetMonitorInfo(monitor);
                    var monitorRect = this.IgnoreTaskbarOnMaximize ? monitorInfo.rcMonitor : monitorInfo.rcWork;

                    var rightBorderThickness = 0D;
                    var bottomBorderThickness = 0D;

                    if (this.KeepBorderOnMaximize
                        && this._savedBorderThickness.HasValue)
                    {
                        // If the maximized window will have a width less than the monitor size, show the right border.
                        if (this.AssociatedObject.MaxWidth < monitorRect.Width)
                        {
                            rightBorderThickness = this._savedBorderThickness.Value.Right;
                        }

                        // If the maximized window will have a height less than the monitor size, show the bottom border.
                        if (this.AssociatedObject.MaxHeight < monitorRect.Height)
                        {
                            bottomBorderThickness = this._savedBorderThickness.Value.Bottom;
                        }
                    }

                    // set window border, so we can move the window from top monitor position
                    this.AssociatedObject.BorderThickness = new Thickness(0, 0, rightBorderThickness, bottomBorderThickness);
                }
                else // Can't get monitor info, so just remove all border thickness
                {
                    this.AssociatedObject.BorderThickness = new Thickness(0);
                }

                this._windowChrome.ResizeBorderThickness = new Thickness(0);
            }
            else
            {
                this.AssociatedObject.BorderThickness = this._savedBorderThickness.GetValueOrDefault(new Thickness(0));

                var resizeBorderThickness = this._savedResizeBorderThickness.GetValueOrDefault(new Thickness(0));

                if (this._windowChrome.ResizeBorderThickness != resizeBorderThickness)
                {
                    this._windowChrome.ResizeBorderThickness = resizeBorderThickness;
                }
            }

            this._borderThicknessChangeNotifier.RaiseValueChanged = true;
            this._resizeBorderThicknessChangeNotifier.RaiseValueChanged = true;
        }

        private void ActivateTaskbarFix(IntPtr monitor)
        {
            var trayWndHandle = NativeMethods.GetTaskBarHandleForMonitor(monitor);

            if (trayWndHandle != IntPtr.Zero)
            {
                this._taskbarHandle = trayWndHandle;
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HwndBottom, 0, 0, 0, 0, SWP.TOPMOST);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HwndTop, 0, 0, 0, 0, SWP.TOPMOST);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HwndNotopmost, 0, 0, 0, 0, SWP.TOPMOST);
            }
        }

        private void DeactivateTaskbarFix(IntPtr trayWndHandle)
        {
            if (trayWndHandle != IntPtr.Zero)
            {
                this._taskbarHandle = IntPtr.Zero;
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HwndBottom, 0, 0, 0, 0, SWP.TOPMOST);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HwndTop, 0, 0, 0, 0, SWP.TOPMOST);
                NativeMethods.SetWindowPos(trayWndHandle, Constants.HwndTopmost, 0, 0, 0, 0, SWP.TOPMOST);
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
    }
}