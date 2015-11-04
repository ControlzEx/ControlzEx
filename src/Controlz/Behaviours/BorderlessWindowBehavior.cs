using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Interop;
using Controlz.Helper;
using Controlz.Microsoft.Windows.Shell;

namespace Controlz.Behaviours
{
    using Standard;

    /// <summary>
    /// With this class we can make custom window styles.
    /// </summary>
    public class BorderlessWindowBehavior : Behavior<Window>
    {
        private IntPtr handle;
        private HwndSource hwndSource;
        private WindowChrome windowChrome;
        private PropertyChangeNotifier borderThicknessChangeNotifier;
        private Thickness? savedBorderThickness;
        private PropertyChangeNotifier topMostChangeNotifier;
        private bool savedTopMost;

        protected override void OnAttached()
        {
            windowChrome = new WindowChrome
            {
#if NET45
                ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness, 
#else
                ResizeBorderThickness = SystemParameters2.Current.WindowResizeBorderThickness,
#endif
                CaptionHeight = 0, 
                CornerRadius = new CornerRadius(0), 
                GlassFrameThickness = new Thickness(0), 
                UseAeroCaptionButtons = false
            };

            var metroWindow = AssociatedObject as MetroWindow;
            if (metroWindow != null)
            {
                windowChrome.IgnoreTaskbarOnMaximize = metroWindow.IgnoreTaskbarOnMaximize;
                windowChrome.UseNoneWindowStyle = metroWindow.UseNoneWindowStyle;
                System.ComponentModel.DependencyPropertyDescriptor.FromProperty(MetroWindow.IgnoreTaskbarOnMaximizeProperty, typeof(MetroWindow))
                      .AddValueChanged(AssociatedObject, IgnoreTaskbarOnMaximizePropertyChangedCallback);
                System.ComponentModel.DependencyPropertyDescriptor.FromProperty(MetroWindow.UseNoneWindowStyleProperty, typeof(MetroWindow))
                      .AddValueChanged(AssociatedObject, UseNoneWindowStylePropertyChangedCallback);
            }

            AssociatedObject.SetValue(WindowChrome.WindowChromeProperty, windowChrome);

            // no transparany, because it hase more then one unwanted issues
            var windowHandle = new WindowInteropHelper(AssociatedObject).Handle;
            if (!AssociatedObject.IsLoaded && windowHandle == IntPtr.Zero)
            {
                try
                {
                    AssociatedObject.AllowsTransparency = false;
                }
                catch (Exception)
                {
                    //For some reason, we can't determine if the window has loaded or not, so we swallow the exception.
                }
            }
            AssociatedObject.WindowStyle = WindowStyle.None;

            savedBorderThickness = AssociatedObject.BorderThickness;
            borderThicknessChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Control.BorderThicknessProperty);
            borderThicknessChangeNotifier.ValueChanged += BorderThicknessChangeNotifierOnValueChanged;

            savedTopMost = AssociatedObject.Topmost;
            topMostChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Window.TopmostProperty);
            topMostChangeNotifier.ValueChanged += TopMostChangeNotifierOnValueChanged;

            AssociatedObject.Loaded += OnAssociatedObjectLoaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
            AssociatedObject.SourceInitialized += OnAssociatedObjectSourceInitialized;
            AssociatedObject.StateChanged += OnAssociatedObjectHandleMaximize;

            // handle the maximized state here too (to handle the border in a correct way)
            this.HandleMaximize();

            base.OnAttached();
        }

        private void BorderThicknessChangeNotifierOnValueChanged(object sender, EventArgs e)
        {
            savedBorderThickness = AssociatedObject.BorderThickness;
        }

        private void TopMostChangeNotifierOnValueChanged(object sender, EventArgs e)
        {
            savedTopMost = AssociatedObject.Topmost;
        }

        private void UseNoneWindowStylePropertyChangedCallback(object sender, EventArgs e)
        {
            var metroWindow = sender as MetroWindow;
            if (metroWindow != null && windowChrome != null)
            {
                if (!Equals(windowChrome.UseNoneWindowStyle, metroWindow.UseNoneWindowStyle))
                {
                    windowChrome.UseNoneWindowStyle = metroWindow.UseNoneWindowStyle;
                    this.ForceRedrawWindowFromPropertyChanged();
                }
            }
        }

        private void IgnoreTaskbarOnMaximizePropertyChangedCallback(object sender, EventArgs e)
        {
            var metroWindow = sender as MetroWindow;
            if (metroWindow != null && windowChrome != null)
            {
                if (!Equals(windowChrome.IgnoreTaskbarOnMaximize, metroWindow.IgnoreTaskbarOnMaximize))
                {
                    // another special hack to avoid nasty resizing
                    // repro
                    // ResizeMode="NoResize"
                    // WindowState="Maximized"
                    // IgnoreTaskbarOnMaximize="True"
                    // this only happens if we change this at runtime
                    var removed = this.handle._ModifyStyle(WS.MAXIMIZEBOX | WS.MINIMIZEBOX | WS.THICKFRAME, 0);
                    windowChrome.IgnoreTaskbarOnMaximize = metroWindow.IgnoreTaskbarOnMaximize;
                    this.ForceRedrawWindowFromPropertyChanged();
                    if (removed)
                    {
                        this.handle._ModifyStyle(0, WS.MAXIMIZEBOX | WS.MINIMIZEBOX | WS.THICKFRAME);
                    }
                }
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

        protected bool isCleanedUp;

        protected virtual void Cleanup()
        {
            if (!isCleanedUp)
            {
                isCleanedUp = true;

                // clean up events
                if (AssociatedObject is MetroWindow)
                {
                    System.ComponentModel.DependencyPropertyDescriptor.FromProperty(MetroWindow.IgnoreTaskbarOnMaximizeProperty, typeof(MetroWindow))
                          .RemoveValueChanged(AssociatedObject, IgnoreTaskbarOnMaximizePropertyChangedCallback);
                    System.ComponentModel.DependencyPropertyDescriptor.FromProperty(MetroWindow.UseNoneWindowStyleProperty, typeof(MetroWindow))
                          .RemoveValueChanged(AssociatedObject, UseNoneWindowStylePropertyChangedCallback);
                }
                AssociatedObject.Loaded -= OnAssociatedObjectLoaded;
                AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
                AssociatedObject.SourceInitialized -= OnAssociatedObjectSourceInitialized;
                AssociatedObject.StateChanged -= OnAssociatedObjectHandleMaximize;
                if (hwndSource != null)
                {
                    hwndSource.RemoveHook(WindowProc);
                }
                windowChrome = null;
            }
        }

        protected override void OnDetaching()
        {
            Cleanup();
            base.OnDetaching();
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            Cleanup();
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
            HandleMaximize();
        }

        protected virtual void HandleMaximize()
        {
            borderThicknessChangeNotifier.ValueChanged -= BorderThicknessChangeNotifierOnValueChanged;
            topMostChangeNotifier.ValueChanged -= TopMostChangeNotifierOnValueChanged;

            var metroWindow = AssociatedObject as MetroWindow;
            var enableDWMDropShadow = metroWindow != null && metroWindow.GlowBrush == null;
            
            if (AssociatedObject.WindowState == WindowState.Maximized)
            {
                // remove resize border and window border, so we can move the window from top monitor position
                // note (punker76): check this, maybe we doesn't need this anymore
                windowChrome.ResizeBorderThickness = new Thickness(0);
                AssociatedObject.BorderThickness = new Thickness(0);

                var ignoreTaskBar = metroWindow != null && metroWindow.IgnoreTaskbarOnMaximize;
                if (ignoreTaskBar && handle != IntPtr.Zero)
                {
                    // WindowChrome handles the size false if the main monitor is lesser the monitor where the window is maximized
                    // so set the window pos/size twice
                    IntPtr monitor = NativeMethods.MonitorFromWindow(this.handle, (uint)MonitorOptions.MONITOR_DEFAULTTONEAREST);
                    if (monitor != IntPtr.Zero)
                    {
                        MONITORINFO monitorInfo = NativeMethods.GetMonitorInfoW(monitor);

                        //ignoreTaskBar = metroWindow.IgnoreTaskbarOnMaximize || metroWindow.UseNoneWindowStyle;
                        var x = ignoreTaskBar ? monitorInfo.rcMonitor.Left : monitorInfo.rcWork.Left;
                        var y = ignoreTaskBar ? monitorInfo.rcMonitor.Top : monitorInfo.rcWork.Top;
                        var cx = ignoreTaskBar ? Math.Abs(monitorInfo.rcMonitor.Right - x) : Math.Abs(monitorInfo.rcWork.Right - x);
                        var cy = ignoreTaskBar ? Math.Abs(monitorInfo.rcMonitor.Bottom - y) : Math.Abs(monitorInfo.rcWork.Bottom - y);
                        NativeMethods.SetWindowPos(handle, new IntPtr(-2), x, y, cx, cy, (SWP)0x0040);
                    }
                }
            }
            else
            {
                // note (punker76): check this, maybe we doesn't need this anymore
#if NET45
                windowChrome.ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness;
#else
                windowChrome.ResizeBorderThickness = SystemParameters2.Current.WindowResizeBorderThickness;
#endif
                if (!enableDWMDropShadow)
                {
                    AssociatedObject.BorderThickness = savedBorderThickness.GetValueOrDefault(new Thickness(0));
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
            AssociatedObject.Topmost = false;
            AssociatedObject.Topmost = AssociatedObject.WindowState == WindowState.Minimized || savedTopMost;
            
            borderThicknessChangeNotifier.ValueChanged += BorderThicknessChangeNotifierOnValueChanged;
            topMostChangeNotifier.ValueChanged += TopMostChangeNotifierOnValueChanged;
        }

        protected virtual void OnAssociatedObjectSourceInitialized(object sender, EventArgs e)
        {
            handle = new WindowInteropHelper(AssociatedObject).Handle;
            if (null == handle)
            {
                throw new Exception("Uups, at this point we really need the Handle from the associated object!");
            }
            hwndSource = HwndSource.FromHwnd(handle);
            if (hwndSource != null)
            {
                hwndSource.AddHook(WindowProc);
            }

            if (AssociatedObject.ResizeMode != ResizeMode.NoResize)
            {
                // handle size to content (thanks @lynnx).
                // This is necessary when ResizeMode != NoResize. Without this workaround,
                // black bars appear at the right and bottom edge of the window.
                var sizeToContent = AssociatedObject.SizeToContent;
                var snapsToDevicePixels = AssociatedObject.SnapsToDevicePixels;
                AssociatedObject.SnapsToDevicePixels = true;
                AssociatedObject.SizeToContent = sizeToContent == SizeToContent.WidthAndHeight ? SizeToContent.Height : SizeToContent.Manual;
                AssociatedObject.SizeToContent = sizeToContent;
                AssociatedObject.SnapsToDevicePixels = snapsToDevicePixels;
            }
        }

        protected virtual void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e)
        {
            // nothing here
        }
    }
}
