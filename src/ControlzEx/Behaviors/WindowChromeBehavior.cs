namespace ControlzEx.Behaviors
{
    using System;
    using System.Windows;
    using System.Windows.Interactivity;
    using System.Windows.Interop;
    using ControlzEx.Helper;
    using System.Windows.Data;
    using Standard;

    //using WindowChrome = System.Windows.Shell.WindowChrome;
    using WindowChrome = ControlzEx.Microsoft.Windows.Shell.WindowChrome;
    using Microsoft.Windows.Shell;
    using System.Windows.Threading;

    /// <summary>
    /// With this class we can make custom window styles.
    /// </summary>
    public class WindowChromeBehavior : Behavior<Window>
    {
        private IntPtr handle;
        private HwndSource hwndSource;
        private WindowChrome windowChrome;

        private bool isWindwos10OrHigher;

        private PropertyChangeNotifier windowStyleChangeNotifier;
        private PropertyChangeNotifier resizeModeChangeNotifier;

        protected bool IsCleanedUp;

        #region Mirror properties for WindowChrome

        public Thickness ResizeBorderThickness
        {
            get { return (Thickness)this.GetValue(ResizeBorderThicknessProperty); }
            set { this.SetValue(ResizeBorderThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ResizeBorderThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResizeBorderThicknessProperty =
            DependencyProperty.Register("ResizeBorderThickness", typeof(Thickness), typeof(WindowChromeBehavior), new PropertyMetadata(GetDefaultResizeBorderThickness()));

        public double CaptionHeight
        {
            get { return (double)this.GetValue(CaptionHeightProperty); }
            set { this.SetValue(CaptionHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CaptionHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CaptionHeightProperty =
            DependencyProperty.Register("CaptionHeight", typeof(double), typeof(WindowChromeBehavior), new PropertyMetadata(SystemParameters.WindowCaptionHeight));

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)this.GetValue(CornerRadiusProperty); }
            set { this.SetValue(CornerRadiusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CornerRadius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(WindowChromeBehavior), new PropertyMetadata(new CornerRadius(0D)));

        public Thickness GlassFrameThickness
        {
            get { return (Thickness)this.GetValue(GlassFrameThicknessProperty); }
            set { this.SetValue(GlassFrameThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GlassFrameThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GlassFrameThicknessProperty =
            DependencyProperty.Register("GlassFrameThickness", typeof(Thickness), typeof(WindowChromeBehavior), new PropertyMetadata(SystemParameters2.Current.WindowNonClientFrameThickness));

        public bool UseAeroCaptionButtons
        {
            get { return (bool)this.GetValue(UseAeroCaptionButtonsProperty); }
            set { this.SetValue(UseAeroCaptionButtonsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UseAeroCaptionButtons.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UseAeroCaptionButtonsProperty =
            DependencyProperty.Register("UseAeroCaptionButtons", typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(true));

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

        // Using a DependencyProperty as the backing store for IgnoreTaskbarOnMaximize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IgnoreTaskbarOnMaximizeProperty =
            DependencyProperty.Register("IgnoreTaskbarOnMaximize", typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(false, IgnoreTaskbarOnMaximizePropertyChangedCallback, CoerceIgnoreTaskbarOnMaximize));

        protected override void OnAttached()
        {
            base.OnAttached();

            // Versions can be taken from https://msdn.microsoft.com/library/windows/desktop/ms724832.aspx
            this.isWindwos10OrHigher = Environment.OSVersion.Version >= new Version(10, 0);

            this.InitializeWindowChrome();

            this.AssociatedObject.SetValue(WindowChrome.WindowChromeProperty, this.windowChrome);

            // no transparany, because it hase more then one unwanted issues            
            var windowHandle = new WindowInteropHelper(this.AssociatedObject).Handle;

            if (this.AssociatedObject.IsLoaded == false
                && windowHandle == IntPtr.Zero)
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

            this.windowStyleChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Window.WindowStyleProperty);
            this.windowStyleChangeNotifier.ValueChanged += this.OnPropertyChangedThatRequiresForceRedrawWindow;

            this.resizeModeChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Window.ResizeModeProperty);
            this.resizeModeChangeNotifier.ValueChanged += this.OnPropertyChangedThatRequiresForceRedrawWindow;

            this.AssociatedObject.Loaded += this.OnAssociatedObjectLoaded;
            this.AssociatedObject.Unloaded += this.AssociatedObject_Unloaded;
            this.AssociatedObject.StateChanged += this.OnAssociatedObjectHandleWindowStateChanged;

            // If Window is already initialized
            if (PresentationSource.FromVisual(this.AssociatedObject) != null)
            {
                this.HandleSourceInitialized();
            }
            else
            {
                this.AssociatedObject.SourceInitialized += this.OnAssociatedObjectSourceInitialized;
            }

            // handle the maximized state here too (to handle the border in a correct way)
            this.FixMaximizedWindow();
        }

        private void InitializeWindowChrome()
        {
            this.windowChrome = new WindowChrome();

            BindingOperations.SetBinding(this.windowChrome, WindowChrome.ResizeBorderThicknessProperty, new Binding { Path = new PropertyPath(ResizeBorderThicknessProperty), Source = this });
            BindingOperations.SetBinding(this.windowChrome, WindowChrome.CaptionHeightProperty, new Binding { Path = new PropertyPath(CaptionHeightProperty), Source = this });
            BindingOperations.SetBinding(this.windowChrome, WindowChrome.CornerRadiusProperty, new Binding { Path = new PropertyPath(CornerRadiusProperty), Source = this });
            BindingOperations.SetBinding(this.windowChrome, WindowChrome.GlassFrameThicknessProperty, new Binding { Path = new PropertyPath(GlassFrameThicknessProperty), Source = this });
            BindingOperations.SetBinding(this.windowChrome, WindowChrome.UseAeroCaptionButtonsProperty, new Binding { Path = new PropertyPath(UseAeroCaptionButtonsProperty), Source = this });

            BindingOperations.SetBinding(this.windowChrome, WindowChrome.IgnoreTaskbarOnMaximizeProperty, new Binding { Path = new PropertyPath(IgnoreTaskbarOnMaximizeProperty), Source = this });
        }

        private static Thickness GetDefaultResizeBorderThickness()
        {
#if NET45
            return SystemParameters.WindowResizeBorderThickness;
#else
            return ControlzEx.Microsoft.Windows.Shell.SystemParameters2.Current.WindowResizeBorderThickness;
#endif
        }

        private void OnPropertyChangedThatRequiresForceRedrawWindow(object sender, EventArgs e)
        {
            this.ForceRedrawWindow();
        }

        private static object CoerceIgnoreTaskbarOnMaximize(DependencyObject d, object baseValue)
        {
            var behavior = (WindowChromeBehavior)d;

            // Only works with WindowStyle = None
            if (behavior.AssociatedObject == null
                || behavior.AssociatedObject.WindowStyle == WindowStyle.None)
            {
                return baseValue;
            }

            return false;
        }

        private static void IgnoreTaskbarOnMaximizePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowChromeBehavior)sender;

            // Async because WindowChromeWorker has to be able to react to the change before we can fix anything
            behavior.ForceRedrawWindowAsync();
        }

        private void ForceRedrawWindowAsync()
        {
            if (this.AssociatedObject == null)
            {
                return;
            }

            this.AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() => this.ForceRedrawWindow()));
        }

        private void ForceRedrawWindow()
        {
            this.windowChrome?._OnPropertyChangedThatRequiresRepaint();
            this.FixMaximizedWindow();

            if (this.handle != IntPtr.Zero)
            {
                NativeMethods.RedrawWindow(this.handle, IntPtr.Zero, IntPtr.Zero, RedrawWindowFlags.Invalidate | RedrawWindowFlags.Frame);
            }
        }

        protected virtual void Cleanup()
        {
            if (this.IsCleanedUp)
            {
                return;
            }

            this.IsCleanedUp = true;

            // clean up events
            this.AssociatedObject.Loaded -= this.OnAssociatedObjectLoaded;
            this.AssociatedObject.Unloaded -= this.AssociatedObject_Unloaded;
            this.AssociatedObject.SourceInitialized -= this.OnAssociatedObjectSourceInitialized;
            this.AssociatedObject.StateChanged -= this.OnAssociatedObjectHandleWindowStateChanged;

            this.windowStyleChangeNotifier.ValueChanged -= this.OnPropertyChangedThatRequiresForceRedrawWindow;
            this.resizeModeChangeNotifier.ValueChanged -= this.OnPropertyChangedThatRequiresForceRedrawWindow;

            this.hwndSource?.RemoveHook(this.WindowProc);

            this.windowChrome = null;
        }

        protected override void OnDetaching()
        {
            this.Cleanup();
            base.OnDetaching();
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Cleanup();
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var returnval = IntPtr.Zero;

            var message = (WM)msg;
            switch (message)
            {
                //case WM.NCPAINT:
                //    handled = this.AssociatedObject.WindowStyle == WindowStyle.None;
                //    break;

                case WM.NCACTIVATE:
                    /* As per http://msdn.microsoft.com/en-us/library/ms632633(VS.85).aspx , "-1" lParam "does not repaint the nonclient area to reflect the state change." */
                    returnval = NativeMethods.DefWindowProc(hwnd, message, wParam, new IntPtr(-1));
                    handled = true;
                    break;
            }

            return returnval;
        }

        private void OnAssociatedObjectHandleWindowStateChanged(object sender, EventArgs e)
        {
            this.ForceRedrawWindowAsync();
        }

        protected virtual void FixMaximizedWindow()
        {
            if (this.AssociatedObject == null
                || this.AssociatedObject.WindowState != WindowState.Maximized
                || this.handle == IntPtr.Zero)
            {
                return;
            }

            // WindowChrome handles the size false if the main monitor is lesser the monitor where the window is maximized
            // so set the window pos/size twice
            var monitor = NativeMethods.MonitorFromWindow(this.handle, (uint)MonitorOptions.MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero)
            {
                return;
            }

            var monitorInfo = NativeMethods.GetMonitorInfoW(monitor);
            var rcMonitorArea = this.IgnoreTaskbarOnMaximize ? monitorInfo.rcMonitor : monitorInfo.rcWork;

            var x = rcMonitorArea.Left;
            var y = rcMonitorArea.Top;

            // This fixes a bug with multiple monitors on Windows 7 and Windows 8. Without this workaround the WindowChrome turns black.
            // This only has to be done on Windows 7 and Windows 8.
            if (this.isWindwos10OrHigher == false)
            {
                // Removing, redrawing and adding DLGFRAME forces windows to draw correctly
                var style = WS.DLGFRAME;
                var modified = this.handle._ModifyStyle(style, 0);

                NativeMethods.SetWindowPos(this.handle, IntPtr.Zero, x, y, 0, 0, SWP.NOSIZE | SWP.SHOWWINDOW);

                if (modified)
                {
                    this.handle._ModifyStyle(0, style);
                }
            }
            else
            {
                NativeMethods.SetWindowPos(this.handle, IntPtr.Zero, x, y, 0, 0, SWP.NOSIZE | SWP.SHOWWINDOW);
            }
        }

        protected virtual void HandleSourceInitialized()
        {
            this.handle = new WindowInteropHelper(this.AssociatedObject).Handle;

            if (null == this.handle)
            {
                throw new Exception("Uups, at this point we really need the Handle from the associated object!");
            }

            this.hwndSource = HwndSource.FromHwnd(this.handle);

            if (this.hwndSource != null)
            {
                this.hwndSource.AddHook(this.WindowProc);
            }

            if (this.AssociatedObject.ResizeMode != ResizeMode.NoResize)
            {
                // handle size to content (thanks @lynnx).
                // This is necessary when ResizeMode != NoResize. Without this workaround,
                // black bars appear at the right and bottom edge of the window.
                var sizeToContent = this.AssociatedObject.SizeToContent;
                var snapsToDevicePixels = this.AssociatedObject.SnapsToDevicePixels;
                this.AssociatedObject.SnapsToDevicePixels = true;
                this.AssociatedObject.SizeToContent = sizeToContent == SizeToContent.WidthAndHeight
                                                          ? SizeToContent.Height
                                                          : SizeToContent.Manual;
                this.AssociatedObject.SizeToContent = sizeToContent;
                this.AssociatedObject.SnapsToDevicePixels = snapsToDevicePixels;
            }
        }

        private void OnAssociatedObjectSourceInitialized(object sender, EventArgs e)
        {
            this.HandleSourceInitialized();
        }

        protected virtual void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e)
        {
            // nothing here
        }
    }
}