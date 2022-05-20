#nullable enable

#pragma warning disable 618
// ReSharper disable once CheckNamespace
namespace ControlzEx.Behaviors
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Threading;
    using ControlzEx.Controls;
    using ControlzEx.Controls.Internal;
    using ControlzEx.Internal;
    using ControlzEx.Internal.KnownBoxes;
    using global::Windows.Win32;
    using global::Windows.Win32.Foundation;
    using global::Windows.Win32.Graphics.Dwm;
    using global::Windows.Win32.UI.WindowsAndMessaging;
    using Microsoft.Xaml.Behaviors;

    public class GlowWindowBehavior : Behavior<Window>
    {
        private static readonly TimeSpan glowTimerDelay = TimeSpan.FromMilliseconds(200); // 200 ms delay, the same as regular window animations
        private DispatcherTimer? makeGlowVisibleTimer;
        private WindowInteropHelper? windowHelper;
        private HWND windowHandle;
        private HwndSource? hwndSource;

        private readonly IGlowWindow?[] glowWindows = new IGlowWindow[4];

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="GlowColor"/>.
        /// </summary>
        public static readonly DependencyProperty GlowColorProperty = DependencyProperty.Register(nameof(GlowColor), typeof(Color?), typeof(GlowWindowBehavior), new PropertyMetadata(default(Color?), OnGlowColorChanged));

        private static void OnGlowColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GlowWindowBehavior)d).UpdateGlowColors();
        }

        /// <summary>
        /// Gets or sets a brush which is used as the glow when the window is active.
        /// </summary>
        public Color? GlowColor
        {
            get => (Color?)this.GetValue(GlowColorProperty);
            set => this.SetValue(GlowColorProperty, value);
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="NonActiveGlowColor"/>.
        /// </summary>
        public static readonly DependencyProperty NonActiveGlowColorProperty = DependencyProperty.Register(nameof(NonActiveGlowColor), typeof(Color?), typeof(GlowWindowBehavior), new PropertyMetadata(default(Color?), OnNonActiveGlowColorChanged));

        private static void OnNonActiveGlowColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GlowWindowBehavior)d).UpdateGlowColors();
        }

        /// <summary>
        /// Gets or sets a brush which is used as the glow when the window is not active.
        /// </summary>
        public Color? NonActiveGlowColor
        {
            get => (Color?)this.GetValue(NonActiveGlowColorProperty);
            set => this.SetValue(NonActiveGlowColorProperty, value);
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="IsGlowTransitionEnabled"/>.
        /// </summary>
        public static readonly DependencyProperty IsGlowTransitionEnabledProperty = DependencyProperty.Register(nameof(IsGlowTransitionEnabled), typeof(bool), typeof(GlowWindowBehavior), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Defines whether glow transitions should be used or not.
        /// </summary>
        public bool IsGlowTransitionEnabled
        {
            get => (bool)this.GetValue(IsGlowTransitionEnabledProperty);
            set => this.SetValue(IsGlowTransitionEnabledProperty, BooleanBoxes.Box(value));
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="GlowDepth"/>.
        /// </summary>
        public static readonly DependencyProperty GlowDepthProperty = DependencyProperty.Register(nameof(GlowDepth), typeof(int), typeof(GlowWindowBehavior), new PropertyMetadata(9, OnGlowDepthChanged));

        private static void OnGlowDepthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GlowWindowBehavior)d).UpdateGlowDepth();
        }

        /// <summary>
        /// Gets or sets the glow depth.
        /// </summary>
        public int GlowDepth
        {
            get => (int)this.GetValue(GlowDepthProperty);
            set => this.SetValue(GlowDepthProperty, value);
        }

        /// <summary>Identifies the <see cref="UseRadialGradientForCorners"/> dependency property.</summary>
        public static readonly DependencyProperty UseRadialGradientForCornersProperty = DependencyProperty.Register(
            nameof(UseRadialGradientForCorners), typeof(bool), typeof(GlowWindowBehavior), new PropertyMetadata(BooleanBoxes.TrueBox, OnUseRadialGradientForCornersChanged));

        /// <summary>
        /// Gets or sets whether to use a radial gradient for the corners or not.
        /// </summary>
        public bool UseRadialGradientForCorners
        {
            get => (bool)this.GetValue(UseRadialGradientForCornersProperty);
            set => this.SetValue(UseRadialGradientForCornersProperty, BooleanBoxes.Box(value));
        }

        private static void OnUseRadialGradientForCornersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GlowWindowBehavior)d).UpdateUseRadialGradientForCorners();
        }

        /// <summary>Identifies the <see cref="PreferDWMBorderColor"/> dependency property.</summary>
        public static readonly DependencyProperty PreferDWMBorderColorProperty =
            DependencyProperty.Register(nameof(PreferDWMBorderColor), typeof(bool), typeof(GlowWindowBehavior), new PropertyMetadata(BooleanBoxes.TrueBox, OnPreferDWMBorderColorChanged));

        private static void OnPreferDWMBorderColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (GlowWindowBehavior)d;
            behavior.UpdateDWMBorder();

            if (behavior.IsUsingDWMBorder)
            {
                behavior.DestroyGlowWindows();
            }
            else
            {
                behavior.UpdateGlowVisibility(true);
            }
        }

        /// <summary>
        /// Gets or sets whether the DWM border should be preferred instead of showing glow windows.
        /// </summary>
        public bool PreferDWMBorderColor
        {
            get => (bool)this.GetValue(PreferDWMBorderColorProperty);
            set => this.SetValue(PreferDWMBorderColorProperty, BooleanBoxes.Box(value));
        }

        /// <summary>Identifies the <see cref="DWMSupportsBorderColor"/> dependency property key.</summary>
        // ReSharper disable once InconsistentNaming
        private static readonly DependencyPropertyKey DWMSupportsBorderColorPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(DWMSupportsBorderColor), typeof(bool), typeof(GlowWindowBehavior), new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>Identifies the <see cref="DWMSupportsBorderColor"/> dependency property.</summary>
        public static readonly DependencyProperty DWMSupportsBorderColorProperty = DWMSupportsBorderColorPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether DWM supports a border color or not.
        /// </summary>
        public bool DWMSupportsBorderColor
        {
            get => (bool)this.GetValue(DWMSupportsBorderColorProperty);
            private set => this.SetValue(DWMSupportsBorderColorPropertyKey, BooleanBoxes.Box(value));
        }

        public bool IsUsingDWMBorder => this.DWMSupportsBorderColor
                                        && this.PreferDWMBorderColor;

        protected override void OnAttached()
        {
            base.OnAttached();

            this.Initialize();
            this.UpdateGlowWindowPositions(true);
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            this.hwndSource?.RemoveHook(this.AssociatedObjectWindowProc);

            this.AssociatedObject.Closed -= this.AssociatedObjectOnClosed;

            this.AssociatedObject.Activated -= this.AssociatedObjectActivatedOrDeactivated;
            this.AssociatedObject.Deactivated -= this.AssociatedObjectActivatedOrDeactivated;

            this.StopTimer();

            this.DestroyGlowWindows();

            this.windowHelper = null;
            this.windowHandle = default;

            base.OnDetaching();
        }

        private void AssociatedObjectActivatedOrDeactivated(object? sender, EventArgs e)
        {
            this.UpdateGlowActiveState();
        }

        private void Initialize()
        {
            this.windowHelper = new WindowInteropHelper(this.AssociatedObject);
            this.windowHandle = new HWND(this.windowHelper.EnsureHandle());
            this.hwndSource = HwndSource.FromHwnd(this.windowHandle);
            this.hwndSource?.AddHook(this.AssociatedObjectWindowProc);

            this.AssociatedObject.Closed += this.AssociatedObjectOnClosed;

            this.AssociatedObject.Activated += this.AssociatedObjectActivatedOrDeactivated;
            this.AssociatedObject.Deactivated += this.AssociatedObjectActivatedOrDeactivated;

            this.UpdateDWMBorder();

            if (this.IsUsingDWMBorder == false)
            {
                this.CreateGlowWindowHandles();
            }
        }

        private void AssociatedObjectOnClosed(object? o, EventArgs args)
        {
            this.AssociatedObject.Closed -= this.AssociatedObjectOnClosed;

            // todo: detach here????

            this.StopTimer();
            this.DestroyGlowWindows();
        }

#pragma warning disable SA1401
        public int DeferGlowChangesCount;
#pragma warning restore SA1401

        private bool positionUpdateRequired;

        private bool parentWindowWasClosed;

        private IntPtr AssociatedObjectWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var message = (WM)msg;

            if (this.IsUsingDWMBorder
                || this.parentWindowWasClosed)
            {
                return IntPtr.Zero;
            }

            //System.Diagnostics.Trace.WriteLine($"{DateTime.Now} {hwnd} {message} {wParam} {lParam}");

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (message)
            {
                case WM.CLOSE:
                    this.StopTimer();
                    this.DestroyGlowWindows();
                    this.parentWindowWasClosed = true;
                    break;

                case WM.WINDOWPOSCHANGING:
                case WM.WINDOWPOSCHANGED:
                {
                    // If the owner is TopMost we don't receive the regular move message, so we must check that here...
                    var windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                    if (windowPos.flags.HasFlag(SET_WINDOW_POS_FLAGS.SWP_HIDEWINDOW)
                        || windowPos.flags.HasFlag(SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW))
                    {
                        using (this.DeferGlowChanges())
                        {
                            this.UpdateGlowVisibility(PInvoke.IsWindowVisible(this.windowHandle));
                        }
                    }

                    break;
                }

                case WM.MOVE:
                case WM.SIZE:
                {
                    if (this.positionUpdateRequired == false)
                    {
                        this.positionUpdateRequired = true;
                        PInvoke.PostMessage(hwnd, WM.USER, default, default);
                    }

                    break;
                }

                case WM.USER:
                {
                    if (this.positionUpdateRequired)
                    {
                        this.positionUpdateRequired = false;
                        this.UpdateGlowWindowPositions();
                    }

                    break;
                }
            }

            return IntPtr.Zero;
        }

        private void DestroyGlowWindows()
        {
            for (var i = 0; i < this.glowWindows.Length; i++)
            {
                this.glowWindows[i]?.Dispose();
                this.glowWindows[i] = null;
            }

            this.isGlowVisible = false;
        }

        public void EndDeferGlowChanges()
        {
            var windowPosInfo = IntPtr.Zero; // PInvoke.BeginDeferWindowPos(this.glowWindows.Length);

            foreach (var glowWindow in this.glowWindows)
            {
                glowWindow?.CommitChanges(windowPosInfo);
            }

            //PInvoke.EndDeferWindowPos(windowPosInfo);
        }

        private IGlowWindow GetOrCreateGlowWindow(int index)
        {
            return this.GetOrCreateGlowWindow((Dock)index);
        }

        private IGlowWindow GetOrCreateGlowWindow(Dock orientation)
        {
            var index = (int)orientation;
            this.glowWindows[index] ??= this.CreateGlowWindow(orientation);

            return this.glowWindows[index]!;
        }

        protected virtual IGlowWindow CreateGlowWindow(Dock orientation)
        {
            return this.SetupGlowWindow(new GlowWindow(this.AssociatedObject, this, orientation));
        }

        protected virtual IGlowWindow SetupGlowWindow(IGlowWindow glowWindow)
        {
            glowWindow.ActiveGlowColor = this.GlowColor ?? Colors.Transparent;
            glowWindow.InactiveGlowColor = this.NonActiveGlowColor ?? Colors.Transparent;
            glowWindow.IsActive = this.AssociatedObject.IsActive;
            glowWindow.GlowDepth = this.GlowDepth;
            glowWindow.UseRadialGradientForCorners = this.UseRadialGradientForCorners;

            return glowWindow;
        }

        private void CreateGlowWindowHandles()
        {
            for (var i = 0; i < this.glowWindows.Length; i++)
            {
                var gowWindow = this.GetOrCreateGlowWindow(i);
                gowWindow.EnsureHandle();
            }
        }

        private bool isGlowVisible;

        private bool IsGlowVisible
        {
            get => this.isGlowVisible;
            set
            {
                if (this.isGlowVisible != value)
                {
                    this.isGlowVisible = value;

                    for (var i = 0; i < this.glowWindows.Length; i++)
                    {
                        this.GetOrCreateGlowWindow(i).IsVisible = value;
                    }
                }
            }
        }

        protected virtual bool ShouldShowGlow
        {
            get
            {
                if (this.windowHandle == IntPtr.Zero)
                {
                    return false;
                }

                var handle = this.windowHandle;
                if (PInvoke.IsWindowVisible(handle)
                    && PInvoke.IsIconic(handle) == false
                    && PInvoke.IsZoomed(handle) == false)
                {
                    var result = this.AssociatedObject is not null
                           && this.AssociatedObject.ResizeMode != ResizeMode.NoResize
                           && this.GlowDepth > 0
                           && this.GlowColor is not null
                           && this.NonActiveGlowColor is not null;
                    if (result == false)
                    {
                        return false;
                    }

                    if (this.IsUsingDWMBorder)
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }
        }

        private void UpdateGlowWindowPositions()
        {
            this.UpdateGlowWindowPositions(PInvoke.IsWindowVisible(this.windowHandle));
        }

        private void UpdateGlowWindowPositions(bool delayIfNecessary)
        {
            using (this.DeferGlowChanges())
            {
                this.UpdateGlowVisibility(delayIfNecessary);

                foreach (var glowWindow in this.glowWindows)
                {
                    glowWindow?.UpdateWindowPos();
                }
            }
        }

        private void UpdateGlowColors()
        {
            this.UpdateDWMBorder();

            using (this.DeferGlowChanges())
            {
                this.UpdateGlowVisibility(true);

                foreach (var glowWindow in this.glowWindows)
                {
                    if (glowWindow is null)
                    {
                        continue;
                    }

                    glowWindow.ActiveGlowColor = this.GlowColor ?? Colors.Transparent;
                    glowWindow.InactiveGlowColor = this.NonActiveGlowColor ?? Colors.Transparent;
                }
            }
        }

        private void UpdateGlowActiveState()
        {
            if (this.AssociatedObject is null)
            {
                return;
            }

            this.UpdateDWMBorder();

            var isWindowActive = this.AssociatedObject.IsActive;

            using (this.DeferGlowChanges())
            {
                this.UpdateGlowVisibility(true);

                foreach (var glowWindow in this.glowWindows)
                {
                    if (glowWindow is null)
                    {
                        continue;
                    }

                    glowWindow.IsActive = isWindowActive;
                }
            }
        }

        private bool UpdateDWMBorder()
        {
            if (this.AssociatedObject is null
                || this.windowHandle == IntPtr.Zero)
            {
                return false;
            }

            var isWindowActive = this.AssociatedObject.IsActive;
            var color = isWindowActive ? this.GlowColor : this.NonActiveGlowColor;
            var useColor = this.AssociatedObject.WindowState != WindowState.Maximized
                           && color.HasValue;
            var attrValue = useColor && this.PreferDWMBorderColor
                        ? (int)new COLORREF(color!.Value).dwColor
                        : -2;
            this.DWMSupportsBorderColor = DwmHelper.SetWindowAttributeValue(this.windowHandle, DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR, attrValue);

            return this.DWMSupportsBorderColor;
        }

        private void UpdateGlowDepth()
        {
            using (this.DeferGlowChanges())
            {
                this.UpdateGlowVisibility(true);

                foreach (var glowWindow in this.glowWindows)
                {
                    if (glowWindow is null)
                    {
                        continue;
                    }

                    glowWindow.GlowDepth = this.GlowDepth;
                    glowWindow.UpdateWindowPos();
                }
            }
        }

        private void UpdateUseRadialGradientForCorners()
        {
            using (this.DeferGlowChanges())
            {
                foreach (var glowWindow in this.glowWindows)
                {
                    if (glowWindow is null)
                    {
                        continue;
                    }

                    glowWindow.UseRadialGradientForCorners = this.UseRadialGradientForCorners;
                }
            }
        }

        private IDisposable DeferGlowChanges()
        {
            return new ChangeScope(this);
        }

        private void UpdateGlowVisibility(bool delayIfNecessary)
        {
            var shouldShowGlow = this.ShouldShowGlow;
            if (shouldShowGlow == this.IsGlowVisible)
            {
                return;
            }

            if ((shouldShowGlow && this.IsGlowTransitionEnabled && SystemParameters.MinimizeAnimation) & delayIfNecessary)
            {
                if (this.makeGlowVisibleTimer is null)
                {
                    this.makeGlowVisibleTimer = new DispatcherTimer
                    {
                        Interval = glowTimerDelay
                    };

                    this.makeGlowVisibleTimer.Tick += this.OnDelayedVisibilityTimerTick;

                    // If we are early, wait for the window content to be rendered
                    if (this.AssociatedObject.IsLoaded == false)
                    {
                        this.AssociatedObject.ContentRendered += this.AssociatedObjectOnContentRendered;
                        return;
                    }
                }

                // If we are early, wait for the window content to be rendered
                if (this.AssociatedObject.IsLoaded
                    && this.makeGlowVisibleTimer.IsEnabled == false)
                {
                    this.makeGlowVisibleTimer.Start();
                }
            }
            else
            {
                this.StopTimer();
                this.IsGlowVisible = shouldShowGlow;
            }
        }

        private void AssociatedObjectOnContentRendered(object? sender, EventArgs e)
        {
            this.AssociatedObject.ContentRendered -= this.AssociatedObjectOnContentRendered;
            this.UpdateGlowVisibility(true);
        }

        private void StopTimer()
        {
            this.makeGlowVisibleTimer?.Stop();
        }

        private void OnDelayedVisibilityTimerTick(object? sender, EventArgs e)
        {
            this.StopTimer();
            this.UpdateGlowWindowPositions(false);
        }
    }
}