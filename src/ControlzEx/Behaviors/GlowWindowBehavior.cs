namespace ControlzEx.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Threading;
    using ControlzEx.Controls;
    using ControlzEx.Internal;
    using ControlzEx.Native;
    using ControlzEx.Standard;
    using Microsoft.Xaml.Behaviors;

    public class GlowWindowBehavior : Behavior<Window>
    {
        private static readonly TimeSpan glowTimerDelay = TimeSpan.FromMilliseconds(200); //200 ms delay, the same as in visual studio
        private GlowWindow? left;
        private GlowWindow? right;
        private GlowWindow? top;
        private GlowWindow? bottom;
        private DispatcherTimer? makeGlowVisibleTimer;
        private IntPtr windowHandle;
        private HwndSource? hwndSource;

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="GlowBrush"/>.
        /// </summary>
        public static readonly DependencyProperty GlowBrushProperty = DependencyProperty.Register(nameof(GlowBrush), typeof(Brush), typeof(GlowWindowBehavior), new PropertyMetadata(default(Brush), OnGlowBrushChanged));

        private static void OnGlowBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is null
                || e.NewValue is null)
            {
                ((GlowWindowBehavior)d).Update();
            }
        }

        /// <summary>
        /// Gets or sets a brush which is used as the glow when the window is active.
        /// </summary>
        public Brush? GlowBrush
        {
            get => (Brush?)this.GetValue(GlowBrushProperty);
            set => this.SetValue(GlowBrushProperty, value);
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="NonActiveGlowBrush"/>.
        /// </summary>
        public static readonly DependencyProperty NonActiveGlowBrushProperty = DependencyProperty.Register(nameof(NonActiveGlowBrush), typeof(Brush), typeof(GlowWindowBehavior), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets a brush which is used as the glow when the window is not active.
        /// </summary>
        public Brush? NonActiveGlowBrush
        {
            get => (Brush?)this.GetValue(NonActiveGlowBrushProperty);
            set => this.SetValue(NonActiveGlowBrushProperty, value);
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="IsGlowTransitionEnabled"/>.
        /// </summary>
        public static readonly DependencyProperty IsGlowTransitionEnabledProperty = DependencyProperty.Register(nameof(IsGlowTransitionEnabled), typeof(bool), typeof(GlowWindowBehavior), new PropertyMetadata(default(bool)));

        /// <summary>
        /// Defines whether glow transitions should be used or not.
        /// </summary>
        public bool IsGlowTransitionEnabled
        {
            get => (bool)this.GetValue(IsGlowTransitionEnabledProperty);
            set => this.SetValue(IsGlowTransitionEnabledProperty, value);
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="ResizeBorderThickness"/>.
        /// </summary>
        public static readonly DependencyProperty ResizeBorderThicknessProperty = DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(GlowWindowBehavior), new PropertyMetadata(default(Thickness)));

        /// <summary>
        /// Gets or sets resize border thickness.
        /// </summary>
        public Thickness ResizeBorderThickness
        {
            get => (Thickness)this.GetValue(ResizeBorderThicknessProperty);
            set => this.SetValue(ResizeBorderThicknessProperty, value);
        }

        private bool IsActiveGlowDisabled => this.GlowBrush is null;

        private bool IsNoneActiveGlowDisabled => this.NonActiveGlowBrush is null;

        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.SourceInitialized += this.AssociatedObjectSourceInitialized;
            this.AssociatedObject.Loaded += this.AssociatedObjectOnLoaded;
            this.AssociatedObject.Unloaded += this.AssociatedObjectUnloaded;

            if (this.AssociatedObject.IsLoaded)
            {
                this.AssociatedObjectOnLoaded(this.AssociatedObject, new RoutedEventArgs());
                this.AssociatedObjectSourceInitialized(this.AssociatedObject, EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            this.AssociatedObject.SourceInitialized -= this.AssociatedObjectSourceInitialized;
            this.AssociatedObject.Loaded -= this.AssociatedObjectOnLoaded;
            this.AssociatedObject.Unloaded -= this.AssociatedObjectUnloaded;

            this.hwndSource?.RemoveHook(this.AssociatedObjectWindowProc);

            this.AssociatedObject.Activated -= this.AssociatedObjectActivatedOrDeactivated;
            this.AssociatedObject.Deactivated -= this.AssociatedObjectActivatedOrDeactivated;
            this.AssociatedObject.StateChanged -= this.AssociatedObjectStateChanged;
            this.AssociatedObject.IsVisibleChanged -= this.AssociatedObjectIsVisibleChanged;
            this.AssociatedObject.Closing -= this.AssociatedObjectOnClosing;

            this.DestroyGlowVisibleTimer();

            this.Close();

            base.OnDetaching();
        }

        private void AssociatedObjectSourceInitialized(object? sender, EventArgs e)
        {
            this.windowHandle = new WindowInteropHelper(this.AssociatedObject).Handle;
            this.hwndSource = HwndSource.FromHwnd(this.windowHandle);
            this.hwndSource?.AddHook(this.AssociatedObjectWindowProc);
        }

        private void AssociatedObjectStateChanged(object? sender, EventArgs e)
        {
            this.makeGlowVisibleTimer?.Stop();

            if (this.AssociatedObject.WindowState == WindowState.Normal)
            {
                var ignoreTaskBar = Interaction.GetBehaviors(this.AssociatedObject).OfType<WindowChromeBehavior>().FirstOrDefault()?.IgnoreTaskbarOnMaximize == true;
                if (this.makeGlowVisibleTimer is not null
                    && SystemParameters.MinimizeAnimation
                    && !ignoreTaskBar)
                {
                    this.makeGlowVisibleTimer.Start();
                }
                else
                {
                    this.RestoreGlow();
                }
            }
            else
            {
                this.HideGlow();
            }
        }

        private void AssociatedObjectUnloaded(object? sender, RoutedEventArgs e)
        {
            this.DestroyGlowVisibleTimer();
        }

        private void DestroyGlowVisibleTimer()
        {
            if (this.makeGlowVisibleTimer is null)
            {
                return;
            }

            this.makeGlowVisibleTimer.Stop();
            this.makeGlowVisibleTimer.Tick -= this.GlowVisibleTimerOnTick;
            this.makeGlowVisibleTimer = null;
        }

        private void GlowVisibleTimerOnTick(object? sender, EventArgs e)
        {
            this.makeGlowVisibleTimer?.Stop();
            this.RestoreGlow();
        }

        private void RestoreGlow()
        {
            if (this.left is not null)
            {
                this.left.IsGlowing = true;
            }

            if (this.top is not null)
            {
                this.top.IsGlowing = true;
            }

            if (this.right is not null)
            {
                this.right.IsGlowing = true;
            }

            if (this.bottom is not null)
            {
                this.bottom.IsGlowing = true;
            }

            this.Update();
        }

        private void HideGlow()
        {
            if (this.left is not null)
            {
                this.left.IsGlowing = false;
            }

            if (this.top is not null)
            {
                this.top.IsGlowing = false;
            }

            if (this.right is not null)
            {
                this.right.IsGlowing = false;
            }

            if (this.bottom is not null)
            {
                this.bottom.IsGlowing = false;
            }

            this.Update();
        }

        private void AssociatedObjectOnLoaded(object? sender, RoutedEventArgs routedEventArgs)
        {
            // No glow effect if GlowBrush not set.
            if (this.IsActiveGlowDisabled)
            {
                return;
            }

            this.AssociatedObject.Activated -= this.AssociatedObjectActivatedOrDeactivated;
            this.AssociatedObject.Activated += this.AssociatedObjectActivatedOrDeactivated;
            this.AssociatedObject.Deactivated -= this.AssociatedObjectActivatedOrDeactivated;
            this.AssociatedObject.Deactivated += this.AssociatedObjectActivatedOrDeactivated;

            this.AssociatedObject.StateChanged -= this.AssociatedObjectStateChanged;
            this.AssociatedObject.StateChanged += this.AssociatedObjectStateChanged;

            if (this.makeGlowVisibleTimer is null)
            {
                this.makeGlowVisibleTimer = new DispatcherTimer { Interval = glowTimerDelay };
                this.makeGlowVisibleTimer.Tick += this.GlowVisibleTimerOnTick;
            }

            this.left = new GlowWindow(this.AssociatedObject, this, GlowDirection.Left);
            this.right = new GlowWindow(this.AssociatedObject, this, GlowDirection.Right);
            this.top = new GlowWindow(this.AssociatedObject, this, GlowDirection.Top);
            this.bottom = new GlowWindow(this.AssociatedObject, this, GlowDirection.Bottom);

            this.loadedGlowWindows = new[]
                                     {
                                         this.left,
                                         this.top,
                                         this.right,
                                         this.bottom
                                     };

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
                this.AssociatedObject.Closing += this.AssociatedObjectOnClosing;
            }
        }

        private void AssociatedObjectOnClosing(object o, CancelEventArgs args)
        {
            if (!args.Cancel)
            {
                this.AssociatedObject.IsVisibleChanged -= this.AssociatedObjectIsVisibleChanged;
            }
        }

#pragma warning disable 618
        private WINDOWPOS prevWindowPos;
        private GlowWindow[] loadedGlowWindows = new GlowWindow[0];
        private bool updatingZOrder;

        private IntPtr AssociatedObjectWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (this.hwndSource?.RootVisual is null)
            {
                return IntPtr.Zero;
            }

            switch ((WM)msg)
            {
                // Z-Index must NOT be updated when WINDOWPOSCHANGING
                case WM.WINDOWPOSCHANGING:
                    {
                        Assert.IsNotDefault(lParam);
                        var wp = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS))!;
                        if (wp.SizeAndPositionEquals(this.prevWindowPos) == false)
                        {
                            this.UpdateCore();
                            this.prevWindowPos = wp;
                        }
                    }

                    break;

                // Z-Index must be updated when WINDOWPOSCHANGED
                case WM.WINDOWPOSCHANGED:
                    {
                        Assert.IsNotDefault(lParam);
                        var wp = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS))!;
                        if (wp.SizeAndPositionEquals(this.prevWindowPos) == false)
                        {
                            this.UpdateCore();
                            this.prevWindowPos = wp;
                        }

                        // todo: direct z-order
                        //foreach (GlowWindow loadedGlowWindow in this.loadedGlowWindows)
                        //{
                        //    var glowWindowHandle = new WindowInteropHelper(loadedGlowWindow).Handle;
                        //    NativeMethods.SetWindowPos(glowWindowHandle, this.windowHandle, 0, 0, 0, 0, SWP.NOSIZE | SWP.NOMOVE | SWP.NOACTIVATE);
                        //}

                        this.UpdateZOrderOfThisAndOwner();
                    }

                    break;

                case WM.SIZE:
                case WM.SIZING:
                    this.UpdateCore();
                    break;
            }

            return IntPtr.Zero;
        }

        #region Z-Order

        private void UpdateZOrderOfThisAndOwner()
        {
            if (this.updatingZOrder)
            {
                return;
            }

            try
            {
                this.updatingZOrder = true;
                var windowInteropHelper = new WindowInteropHelper(this.AssociatedObject);
                var currentHandle = windowInteropHelper.Handle;

                if (currentHandle != this.windowHandle
                    || WindowHelper.IsWindowHandleValid(currentHandle) == false)
                {
                    return;
                }

                foreach (var loadedGlowWindow in this.loadedGlowWindows)
                {
                    var glowWindowHandle = new WindowInteropHelper(loadedGlowWindow).Handle;

                    var window = NativeMethods.GetWindow(glowWindowHandle, GW.HWNDPREV);
                    if (window != currentHandle)
                    {
                        if (WindowHelper.IsWindowHandleValid(glowWindowHandle)
                            && WindowHelper.IsWindowHandleValid(currentHandle))
                        {
                            NativeMethods.SetWindowPos(glowWindowHandle, currentHandle, 0, 0, 0, 0, SWP.NOSIZE | SWP.NOMOVE | SWP.NOACTIVATE);
                        }
                    }

                    currentHandle = glowWindowHandle;
                }

                this.UpdateZOrderOfOwner(currentHandle, windowInteropHelper.Owner);
            }
            finally
            {
                this.updatingZOrder = false;
            }
        }

        private void UpdateZOrderOfOwner(IntPtr hwndWindow, IntPtr hwndOwner)
        {
            if (WindowHelper.IsWindowHandleValid(hwndWindow) == false
                || WindowHelper.IsWindowHandleValid(hwndOwner) == false)
            {
                return;
            }

            var lastOwnedWindow = IntPtr.Zero;
            NativeMethods.EnumThreadWindows(NativeMethods.GetCurrentThreadId(), delegate(IntPtr hwnd, IntPtr lParam)
                                                                                {
                                                                                    if (NativeMethods.GetWindow(hwnd, GW.OWNER) == hwndOwner)
                                                                                    {
                                                                                        lastOwnedWindow = hwnd;
                                                                                    }

                                                                                    return true;
                                                                                }, IntPtr.Zero);
            if (WindowHelper.IsWindowHandleValid(hwndOwner)
                && WindowHelper.IsWindowHandleValid(lastOwnedWindow)
                && NativeMethods.GetWindow(hwndOwner, GW.HWNDPREV) != lastOwnedWindow)
            {
                NativeMethods.SetWindowPos(hwndOwner, lastOwnedWindow, 0, 0, 0, 0, SWP.NOSIZE | SWP.NOMOVE | SWP.NOACTIVATE);
            }
        }
#pragma warning restore 618

        #endregion

        private void AssociatedObjectActivatedOrDeactivated(object? sender, EventArgs e)
        {
            this.UpdateCore();
        }

        private void AssociatedObjectIsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
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
            this.left?.Update();
            this.right?.Update();
            this.top?.Update();
            this.bottom?.Update();
        }

#pragma warning disable 618
        private void UpdateCore()
        {
            if ((this.IsActiveGlowDisabled && this.AssociatedObject.IsActive)
                || (this.IsNoneActiveGlowDisabled && this.AssociatedObject.IsActive == false)
                || WindowHelper.IsWindowHandleValid(this.windowHandle) == false
                || NativeMethods.IsWindowVisible(this.windowHandle) == false)
            {
                return;
            }

            if (UnsafeNativeMethods.GetWindowRect(this.windowHandle, out var rect))
            {
                this.left?.UpdateCore(rect);
                this.right?.UpdateCore(rect);
                this.top?.UpdateCore(rect);
                this.bottom?.UpdateCore(rect);
            }
        }
#pragma warning restore 618

        /// <summary>
        /// Sets the opacity to all glow windows
        /// </summary>
        private void SetOpacityTo(double newOpacity)
        {
            var canSetOpacity = this.left is not null
                                && this.right is not null
                                && this.top is not null
                                && this.bottom is not null;

            if (canSetOpacity)
            {
                this.left!.Opacity = newOpacity;
                this.right!.Opacity = newOpacity;
                this.top!.Opacity = newOpacity;
                this.bottom!.Opacity = newOpacity;
            }
        }

        /// <summary>
        /// Starts the opacity storyboard 0 -> 1
        /// </summary>
        private void StartOpacityStoryboard()
        {
            var canStartOpacityStoryboard = this.left?.OpacityStoryboard is not null
                                            && this.right?.OpacityStoryboard is not null
                                            && this.top?.OpacityStoryboard is not null
                                            && this.bottom?.OpacityStoryboard is not null;

            if (canStartOpacityStoryboard)
            {
                this.left!.BeginStoryboard(this.left.OpacityStoryboard!);
                this.right!.BeginStoryboard(this.right.OpacityStoryboard!);
                this.top!.BeginStoryboard(this.top.OpacityStoryboard!);
                this.bottom!.BeginStoryboard(this.bottom.OpacityStoryboard!);
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

        /// <summary>
        /// Closes all glow windows
        /// </summary>
        private void Close()
        {
            this.left?.InternalClose();
            this.right?.InternalClose();
            this.top?.InternalClose();
            this.bottom?.InternalClose();
        }
    }
}