namespace Controlz.Behaviours
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Interactivity;
    using System.Windows.Interop;
    using Controlz.Helper;
    using System.Windows.Data;
    using Standard;

    //using WindowChrome = System.Windows.Shell.WindowChrome;
    using WindowChrome = Controlz.Microsoft.Windows.Shell.WindowChrome;

    /// <summary>
    /// With this class we can make custom window styles.
    /// </summary>
    /// <remarks>
    /// - ResizeBorderThickness manipulation in Behavior can be replaced with:
    //<Style.Triggers>
    //    <Trigger Property = "WindowState"
    //             Value="Maximized">
    //        <Setter Property = "ResizeBorderThickness"
    //                Value="0" />
    //    </Trigger>
    //</Style.Triggers>
    /// </remarks>
    public class WindowChromeBehavior : Behavior<Window>
    {
        private IntPtr handle;
        private HwndSource hwndSource;
        private WindowChrome windowChrome;
        //private PropertyChangeNotifier borderThicknessChangeNotifier;
        //private Thickness? savedBorderThickness;
        private PropertyChangeNotifier topMostChangeNotifier;
        private bool savedTopMost;

        protected bool isCleanedUp;

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
            DependencyProperty.Register("CaptionHeight", typeof(double), typeof(WindowChromeBehavior), new PropertyMetadata(0D));

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
            DependencyProperty.Register("GlassFrameThickness", typeof(Thickness), typeof(WindowChromeBehavior), new PropertyMetadata(new Thickness(0D)));

        public bool UseAeroCaptionButtons
        {
            get { return (bool)this.GetValue(UseAeroCaptionButtonsProperty); }
            set { this.SetValue(UseAeroCaptionButtonsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UseAeroCaptionButtons.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UseAeroCaptionButtonsProperty =
            DependencyProperty.Register("UseAeroCaptionButtons", typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(false));

        #endregion

        public bool IgnoreTaskbarOnMaximize
        {
            get { return (bool)this.GetValue(IgnoreTaskbarOnMaximizeProperty); }
            set { this.SetValue(IgnoreTaskbarOnMaximizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IgnoreTaskbarOnMaximize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IgnoreTaskbarOnMaximizeProperty =
            DependencyProperty.Register("IgnoreTaskbarOnMaximize", typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(false, IgnoreTaskbarOnMaximizePropertyChangedCallback));

        public bool UseNoneWindowStyle
        {
            get { return (bool)this.GetValue(UseNoneWindowStyleProperty); }
            set { this.SetValue(UseNoneWindowStyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UseNoneWindowStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UseNoneWindowStyleProperty =
            DependencyProperty.Register("UseNoneWindowStyle", typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(false, UseNoneWindowStylePropertyChangedCallback));

        protected override void OnAttached()
        {
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

            //this.savedBorderThickness = this.AssociatedObject.BorderThickness;
            //this.borderThicknessChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Control.BorderThicknessProperty);
            //this.borderThicknessChangeNotifier.ValueChanged += this.BorderThicknessChangeNotifierOnValueChanged;

            this.savedTopMost = this.AssociatedObject.Topmost;
            this.topMostChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Window.TopmostProperty);
            this.topMostChangeNotifier.ValueChanged += this.TopMostChangeNotifierOnValueChanged;

            this.AssociatedObject.Loaded += this.OnAssociatedObjectLoaded;
            this.AssociatedObject.Unloaded += this.AssociatedObject_Unloaded;
            this.AssociatedObject.SourceInitialized += this.OnAssociatedObjectSourceInitialized;
            this.AssociatedObject.StateChanged += this.OnAssociatedObjectHandleMaximize;

            // handle the maximized state here too (to handle the border in a correct way)
            this.HandleMaximize();

            base.OnAttached();
        }

        private void InitializeWindowChrome()
        {
            this.windowChrome = new WindowChrome();

            BindingOperations.SetBinding(this.windowChrome, WindowChrome.ResizeBorderThicknessProperty, new Binding("ResizeBorderThickness") { Source = this });
            BindingOperations.SetBinding(this.windowChrome, WindowChrome.CaptionHeightProperty, new Binding("CaptionHeight") { Source = this });
            BindingOperations.SetBinding(this.windowChrome, WindowChrome.CornerRadiusProperty, new Binding("CornerRadius") { Source = this });
            BindingOperations.SetBinding(this.windowChrome, WindowChrome.GlassFrameThicknessProperty, new Binding("GlassFrameThickness") { Source = this });
            BindingOperations.SetBinding(this.windowChrome, WindowChrome.UseAeroCaptionButtonsProperty, new Binding("UseAeroCaptionButtons") { Source = this });

            BindingOperations.SetBinding(this.windowChrome, WindowChrome.IgnoreTaskbarOnMaximizeProperty, new Binding("IgnoreTaskbarOnMaximize") { Source = this });
            BindingOperations.SetBinding(this.windowChrome, WindowChrome.UseNoneWindowStyleProperty, new Binding("UseNoneWindowStyle") { Source = this });
        }

        //private void BorderThicknessChangeNotifierOnValueChanged(object sender, EventArgs e)
        //{
        //    this.savedBorderThickness = this.AssociatedObject.BorderThickness;
        //}

        private void TopMostChangeNotifierOnValueChanged(object sender, EventArgs e)
        {
            this.savedTopMost = this.AssociatedObject.Topmost;
        }

        private static Thickness GetDefaultResizeBorderThickness()
        {
#if NET45
                return SystemParameters.WindowResizeBorderThickness;
#else
            return Controlz.Microsoft.Windows.Shell.SystemParameters2.Current.WindowResizeBorderThickness;
#endif
        }

        private static void UseNoneWindowStylePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = sender as WindowChromeBehavior;

            if (behavior == null)
            {
                return;
            }

            behavior.ForceRedrawWindowFromPropertyChanged();
        }

        private static void IgnoreTaskbarOnMaximizePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = sender as WindowChromeBehavior;

            if (behavior == null)
            {
                return;
            }

            // another special hack to avoid nasty resizing
            // repro
            // ResizeMode="NoResize"
            // WindowState="Maximized"
            // IgnoreTaskbarOnMaximize="True"
            // this only happens if we change this at runtime
            var removed = behavior.handle._ModifyStyle(WS.MAXIMIZEBOX | WS.MINIMIZEBOX | WS.THICKFRAME, 0);
            behavior.ForceRedrawWindowFromPropertyChanged();
            if (removed)
            {
                behavior.handle._ModifyStyle(0, WS.MAXIMIZEBOX | WS.MINIMIZEBOX | WS.THICKFRAME);
            }
        }

        private void ForceRedrawWindowFromPropertyChanged()
        {
            this.HandleMaximize();
            if (this.handle != IntPtr.Zero)
            {
                NativeMethods.RedrawWindow(this.handle, IntPtr.Zero, IntPtr.Zero, RedrawWindowFlags.Invalidate | RedrawWindowFlags.Frame);
            }
        }

        protected virtual void Cleanup()
        {
            if (this.isCleanedUp)
            {
                return;
            }

            this.isCleanedUp = true;

            // clean up events
            this.AssociatedObject.Loaded -= this.OnAssociatedObjectLoaded;
            this.AssociatedObject.Unloaded -= this.AssociatedObject_Unloaded;
            this.AssociatedObject.SourceInitialized -= this.OnAssociatedObjectSourceInitialized;
            this.AssociatedObject.StateChanged -= this.OnAssociatedObjectHandleMaximize;

            //this.borderThicknessChangeNotifier.ValueChanged -= this.BorderThicknessChangeNotifierOnValueChanged;
            this.topMostChangeNotifier.ValueChanged -= this.TopMostChangeNotifierOnValueChanged;

            if (this.hwndSource != null)
            {
                this.hwndSource.RemoveHook(this.WindowProc);
            }

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
                case WM.NCPAINT:
                    handled = true;
                    break;
                case WM.NCACTIVATE:
                    /* As per http://msdn.microsoft.com/en-us/library/ms632633(VS.85).aspx , "-1" lParam "does not repaint the nonclient area to reflect the state change." */
                    returnval = NativeMethods.DefWindowProc(hwnd, message, wParam, new IntPtr(-1));
                    handled = true;
                    break;
            }

            return returnval;
        }

        private void OnAssociatedObjectHandleMaximize(object sender, EventArgs e)
        {
            this.HandleMaximize();
        }

        protected virtual void HandleMaximize()
        {
            //this.borderThicknessChangeNotifier.ValueChanged -= this.BorderThicknessChangeNotifierOnValueChanged;
            this.topMostChangeNotifier.ValueChanged -= this.TopMostChangeNotifierOnValueChanged;

            // todo batzen: Handle enableDWMDropShadow
            //var metroWindow = this.AssociatedObject as MetroWindow;
            //var enableDWMDropShadow = metroWindow != null && metroWindow.GlowBrush == null;

            if (this.AssociatedObject.WindowState == WindowState.Maximized)
            {
                // remove resize border and window border, so we can move the window from top monitor position
                // note (punker76): check this, maybe we doesn't need this anymore
                // todo batzen: this breaks binding on those properties
                //this.windowChrome.ResizeBorderThickness = new Thickness(0);
                //this.AssociatedObject.BorderThickness = new Thickness(0);

                if (this.IgnoreTaskbarOnMaximize
                    && this.handle != IntPtr.Zero)
                {
                    // WindowChrome handles the size false if the main monitor is lesser the monitor where the window is maximized
                    // so set the window pos/size twice
                    var monitor = NativeMethods.MonitorFromWindow(this.handle, (uint)MonitorOptions.MONITOR_DEFAULTTONEAREST);
                    if (monitor != IntPtr.Zero)
                    {
                        var monitorInfo = NativeMethods.GetMonitorInfoW(monitor);

                        var x = this.IgnoreTaskbarOnMaximize ? monitorInfo.rcMonitor.Left : monitorInfo.rcWork.Left;
                        var y = this.IgnoreTaskbarOnMaximize ? monitorInfo.rcMonitor.Top : monitorInfo.rcWork.Top;
                        var cx = this.IgnoreTaskbarOnMaximize ? Math.Abs(monitorInfo.rcMonitor.Right - x) : Math.Abs(monitorInfo.rcWork.Right - x);
                        var cy = this.IgnoreTaskbarOnMaximize ? Math.Abs(monitorInfo.rcMonitor.Bottom - y) : Math.Abs(monitorInfo.rcWork.Bottom - y);
                        NativeMethods.SetWindowPos(this.handle, new IntPtr(-2), x, y, cx, cy, (SWP)0x0040);
                    }
                }
            }
            else
            {
                // note (punker76): check this, maybe we doesn't need this anymore
//#if NET45
//                windowChrome.ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness;
//#else
//                this.windowChrome.ResizeBorderThickness = SystemParameters2.Current.WindowResizeBorderThickness;
//#endif
                // todo batzen: Handle enableDWMDropShadow
                //if (!enableDWMDropShadow)
                //{
                //    this.AssociatedObject.BorderThickness = this.savedBorderThickness.GetValueOrDefault(new Thickness(0));
                //}
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

            //this.borderThicknessChangeNotifier.ValueChanged += this.BorderThicknessChangeNotifierOnValueChanged;
            this.topMostChangeNotifier.ValueChanged += this.TopMostChangeNotifierOnValueChanged;
        }

        protected virtual void OnAssociatedObjectSourceInitialized(object sender, EventArgs e)
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
                this.AssociatedObject.SizeToContent = sizeToContent == SizeToContent.WidthAndHeight ? SizeToContent.Height : SizeToContent.Manual;
                this.AssociatedObject.SizeToContent = sizeToContent;
                this.AssociatedObject.SnapsToDevicePixels = snapsToDevicePixels;
            }
        }

        protected virtual void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e)
        {
            // nothing here
        }
    }
}