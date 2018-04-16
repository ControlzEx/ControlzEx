namespace ControlzEx.Behaviors
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interactivity;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Threading;
    using ControlzEx.Controls;
    using ControlzEx.Native;
    using ControlzEx.Standard;

    public class GlowWindowBehavior : Behavior<Window>
    {
        private static readonly TimeSpan glowTimerDelay = TimeSpan.FromMilliseconds(200); //200 ms delay, the same as in visual studio
        private GlowWindow left, right, top, bottom;
        private DispatcherTimer makeGlowVisibleTimer;
        private IntPtr handle;
        private HwndSource hwndSource;

        public static readonly DependencyProperty GlowBrushProperty = DependencyProperty.RegisterAttached("GlowBrush", typeof(Brush), typeof(GlowWindowBehavior), new PropertyMetadata(default(Brush)));

        public static void SetGlowBrush(DependencyObject element, Brush value)
        {
            element.SetValue(GlowBrushProperty, value);
        }

        public static Brush GetGlowBrush(DependencyObject element)
        {
            return (Brush)element.GetValue(GlowBrushProperty);
        }

        public static readonly DependencyProperty NonActiveGlowBrushProperty = DependencyProperty.RegisterAttached("NonActiveGlowBrush", typeof(Brush), typeof(GlowWindowBehavior), new PropertyMetadata(default(Brush)));

        public static void SetNonActiveGlowBrush(DependencyObject element, Brush value)
        {
            element.SetValue(NonActiveGlowBrushProperty, value);
        }

        public static Brush GetNonActiveGlowBrush(DependencyObject element)
        {
            return (Brush)element.GetValue(NonActiveGlowBrushProperty);
        }

        public static readonly DependencyProperty IsGlowTransitionEnabledProperty = DependencyProperty.RegisterAttached("IsGlowTransitionEnabled", typeof(bool), typeof(GlowWindowBehavior), new PropertyMetadata(default(bool)));

        public static void SetIsGlowTransitionEnabled(DependencyObject element, bool value)
        {
            element.SetValue(IsGlowTransitionEnabledProperty, value);
        }

        public static bool GetIsGlowTransitionEnabled(DependencyObject element)
        {
            return (bool)element.GetValue(IsGlowTransitionEnabledProperty);
        }

        private bool IsGlowDisabled
        {
            get { return GetGlowBrush(this.AssociatedObject) == null && GetGlowBrush(this) == null; }
        }

        private bool IsGlowTransitionEnabled
        {
            get { return GetIsGlowTransitionEnabled(this.AssociatedObject) || GetIsGlowTransitionEnabled(this); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.SourceInitialized += (o, args) =>
                {
                    this.handle = new WindowInteropHelper(this.AssociatedObject).Handle;
                    this.hwndSource = HwndSource.FromHwnd(this.handle);
                    this.hwndSource?.AddHook(this.AssociatedObjectWindowProc);
                };
            this.AssociatedObject.Loaded += this.AssociatedObjectOnLoaded;
            this.AssociatedObject.Unloaded += this.AssociatedObjectUnloaded;
        }

        private void AssociatedObjectStateChanged(object sender, EventArgs e)
        {
            this.makeGlowVisibleTimer?.Stop();

            if(this.AssociatedObject.WindowState == WindowState.Normal)
            {
                //var metroWindow = this.AssociatedObject as MetroWindow;
                //var ignoreTaskBar = metroWindow != null && metroWindow.IgnoreTaskbarOnMaximize;
                //if (this.makeGlowVisibleTimer != null && SystemParameters.MinimizeAnimation && !ignoreTaskBar)
                //{
                //    this.makeGlowVisibleTimer.Start();
                //}
                //else
                {
                    this.RestoreGlow();
                }
            }
            else
            {
                this.HideGlow();
            }
        }

        private void AssociatedObjectUnloaded(object sender, RoutedEventArgs e)
        {
            if (this.makeGlowVisibleTimer == null)
            {
                return;
            }

            this.makeGlowVisibleTimer.Stop();
            this.makeGlowVisibleTimer.Tick -= this.GlowVisibleTimerOnTick;
            this.makeGlowVisibleTimer = null;
        }

        private void GlowVisibleTimerOnTick(object sender, EventArgs e)
        {
            this.makeGlowVisibleTimer?.Stop();
            this.RestoreGlow();
        }

        private void RestoreGlow()
        {
            if (this.left != null) this.left.IsGlowing = true;
            if (this.top != null) this.top.IsGlowing = true;
            if (this.right != null) this.right.IsGlowing = true;
            if (this.bottom != null) this.bottom.IsGlowing = true;

            this.Update();
        }

        private void HideGlow()
        {
            if (this.left != null) this.left.IsGlowing = false;
            if (this.top != null) this.top.IsGlowing = false;
            if (this.right != null) this.right.IsGlowing = false;
            if (this.bottom != null) this.bottom.IsGlowing = false;

            this.Update();
        }

        private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            // No glow effect if GlowBrush not set.
            if (this.IsGlowDisabled)
            {
                return;
            }

            this.AssociatedObject.StateChanged -= this.AssociatedObjectStateChanged;
            this.AssociatedObject.StateChanged += this.AssociatedObjectStateChanged;

            if (this.makeGlowVisibleTimer == null)
            {
                this.makeGlowVisibleTimer = new DispatcherTimer { Interval = glowTimerDelay };
                this.makeGlowVisibleTimer.Tick += this.GlowVisibleTimerOnTick;
            }

            this.left = new GlowWindow(this.AssociatedObject, GlowDirection.Left);
            this.right = new GlowWindow(this.AssociatedObject, GlowDirection.Right);
            this.top = new GlowWindow(this.AssociatedObject, GlowDirection.Top);
            this.bottom = new GlowWindow(this.AssociatedObject, GlowDirection.Bottom);

            this.Show();
            this.Update();

            if (!this.IsGlowTransitionEnabled)
            {
                // no storyboard so set opacity to 1
                this.AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => this.SetOpacityTo(1)));
            }
            else
            {
                // start the opacity storyboard 0->1
                this.StartOpacityStoryboard();
                // hide the glows if window get invisible state
                this.AssociatedObject.IsVisibleChanged += this.AssociatedObjectIsVisibleChanged;
                // closing always handled
                this.AssociatedObject.Closing += (o, args) =>
                {
                    if (!args.Cancel)
                    {
                        this.AssociatedObject.IsVisibleChanged -= this.AssociatedObjectIsVisibleChanged;
                    }
                };
            }
        }

#pragma warning disable 618
        private WINDOWPOS prevWindowPos;

        private IntPtr AssociatedObjectWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (this.hwndSource?.RootVisual == null)
            {
                return IntPtr.Zero;
            }

            switch ((WM)msg)
            {
                case WM.WINDOWPOSCHANGED:
                case WM.WINDOWPOSCHANGING:
                    Assert.IsNotDefault(lParam);
                    var wp = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
                    if (!wp.Equals(this.prevWindowPos))
                    {
                        this.UpdateCore();
                    }
                    this.prevWindowPos = wp;
                    break;
                case WM.SIZE:
                case WM.SIZING:
                    this.UpdateCore();
                    break;
            }
            return IntPtr.Zero;
        }
#pragma warning restore 618

        private void AssociatedObjectIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!this.AssociatedObject.IsVisible)
            {
                // the associated owner got invisible so set opacity to 0 to start the storyboard by 0 for the next visible state
                this.SetOpacityTo(0);
            }
            else
            {
                this.StartOpacityStoryboard();
            }
        }

        /// <summary>
        /// Updates all glow windows (visible, hidden, collapsed)
        /// </summary>
        private void Update()
        {
            var canUpdate = this.left != null && this.right != null && this.top != null && this.bottom != null;
            if (canUpdate)
            {
                this.left.Update();
                this.right.Update();
                this.top.Update();
                this.bottom.Update();
            }
        }

#pragma warning disable 618
        private void UpdateCore()
        {
            var canUpdateCore = this.left != null && this.right != null && this.top != null && this.bottom != null
                                && this.left.CanUpdateCore() && this.right.CanUpdateCore() && this.top.CanUpdateCore() && this.bottom.CanUpdateCore();
            if (canUpdateCore)
            {
                RECT rect;
                if (this.handle != IntPtr.Zero && UnsafeNativeMethods.GetWindowRect(this.handle, out rect))
                {
                    this.left.UpdateCore(rect);
                    this.right.UpdateCore(rect);
                    this.top.UpdateCore(rect);
                    this.bottom.UpdateCore(rect);
                }
            }
        }
#pragma warning restore 618

        /// <summary>
        /// Sets the opacity to all glow windows
        /// </summary>
        private void SetOpacityTo(double newOpacity)
        {
            var canSetOpacity = this.left != null && this.right != null && this.top != null && this.bottom != null;
            if (canSetOpacity)
            {
                this.left.Opacity = newOpacity;
                this.right.Opacity = newOpacity;
                this.top.Opacity = newOpacity;
                this.bottom.Opacity = newOpacity;
            }
        }

        /// <summary>
        /// Starts the opacity storyboard 0 -> 1
        /// </summary>
        private void StartOpacityStoryboard()
        {
            var canStartOpacityStoryboard = this.left != null && this.right != null && this.top != null && this.bottom != null
                                            && this.left.OpacityStoryboard != null && this.right.OpacityStoryboard != null && this.top.OpacityStoryboard != null && this.bottom.OpacityStoryboard != null;
            if (canStartOpacityStoryboard)
            {
                this.left.BeginStoryboard(this.left.OpacityStoryboard);
                this.right.BeginStoryboard(this.right.OpacityStoryboard);
                this.top.BeginStoryboard(this.top.OpacityStoryboard);
                this.bottom.BeginStoryboard(this.bottom.OpacityStoryboard);
            }
        }

        /// <summary>
        /// Shows all glow windows
        /// </summary>
        private void Show()
        {
            this.left?.Show();
            this.right?.Show();
            this.top?.Show();
            this.bottom?.Show();
        }
    }
}
