#nullable enable

#pragma warning disable 618
// ReSharper disable once CheckNamespace
namespace ControlzEx.Behaviors
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Threading;
    using ControlzEx.Controls;
    using ControlzEx.Controls.Internal;
    using ControlzEx.Internal.KnownBoxes;
    using ControlzEx.Standard;
    using Microsoft.Xaml.Behaviors;

    public class GlowWindowBehavior : Behavior<Window>
    {
        private static readonly TimeSpan glowTimerDelay = TimeSpan.FromMilliseconds(200); //200 ms delay, the same as in visual studio
        private DispatcherTimer? makeGlowVisibleTimer;
        private WindowInteropHelper? windowHelper;
        private IntPtr windowHandle;
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
            behavior.UpdateGlowVisibility(true);
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

        protected override void OnAttached()
        {
            base.OnAttached();

            if (this.AssociatedObject.IsLoaded)
            {
                this.AssociatedObjectSourceInitialized(this.AssociatedObject, EventArgs.Empty);
                this.UpdateGlowWindowPositions(true);
            }
            else
            {
                this.AssociatedObject.SourceInitialized += this.AssociatedObjectSourceInitialized;
            }
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            this.AssociatedObject.SourceInitialized -= this.AssociatedObjectSourceInitialized;

            this.hwndSource?.RemoveHook(this.AssociatedObjectWindowProc);

            this.AssociatedObject.Closed -= this.AssociatedObjectOnClosed;

            this.AssociatedObject.Activated -= this.AssociatedObjectActivatedOrDeactivated;
            this.AssociatedObject.Deactivated -= this.AssociatedObjectActivatedOrDeactivated;

            this.StopTimer();

            this.DestroyGlowWindows();

            this.windowHelper = null;
            this.windowHandle = IntPtr.Zero;

            base.OnDetaching();
        }

        private void AssociatedObjectActivatedOrDeactivated(object? sender, EventArgs e)
        {
            this.UpdateGlowActiveState();
        }

        private void AssociatedObjectSourceInitialized(object? sender, EventArgs e)
        {
            this.windowHelper = new WindowInteropHelper(this.AssociatedObject);
            this.windowHandle = this.windowHelper.EnsureHandle();
            this.hwndSource = HwndSource.FromHwnd(this.windowHandle);
            this.hwndSource?.AddHook(this.AssociatedObjectWindowProc);

            this.AssociatedObject.Closed += this.AssociatedObjectOnClosed;

            this.AssociatedObject.Activated += this.AssociatedObjectActivatedOrDeactivated;
            this.AssociatedObject.Deactivated += this.AssociatedObjectActivatedOrDeactivated;

            this.CreateGlowWindowHandles();
            this.UpdateDWMBorder();
        }

        private void AssociatedObjectOnClosed(object? o, EventArgs args)
        {
            this.AssociatedObject.Closed -= this.AssociatedObjectOnClosed;

            // todo: detach here????

            this.StopTimer();
            this.DestroyGlowWindows();
        }

        private bool updatingZOrder;

#pragma warning disable SA1401
        public int DeferGlowChangesCount;
#pragma warning restore SA1401

        private bool positionUpdateRequired;

        private IntPtr AssociatedObjectWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (this.hwndSource?.RootVisual is null)
            {
                return IntPtr.Zero;
            }

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch ((WM)msg)
            {
                case WM.CLOSE:
                    this.StopTimer();
                    this.DestroyGlowWindows();
                    break;

                case WM.MOVE:
                {
                    this.positionUpdateRequired = true;
                    NativeMethods.PostMessage(hwnd, WM.USER, IntPtr.Zero, IntPtr.Zero);
                    break;
                }

                // Z-Index must be updated when WINDOWPOSCHANGED
                case WM.WINDOWPOSCHANGED:
                    this.UpdateZOrderOfThisAndOwner();
                    break;

                case WM.SIZE:
                {
                    this.positionUpdateRequired = true;
                    NativeMethods.PostMessage(hwnd, WM.USER, IntPtr.Zero, IntPtr.Zero);
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

        #region Z-Order

        private void UpdateZOrderOfThisAndOwner(IntPtr ownerHandle)
        {
            if (this.updatingZOrder)
            {
                return;
            }

            try
            {
                this.updatingZOrder = true;
                if (this.windowHandle != IntPtr.Zero)
                {
                    var windowPosInfo = NativeMethods.BeginDeferWindowPos(this.glowWindows.Length);

                    var currentWindowHandle = this.windowHandle;
                    foreach (var glowWindow in this.glowWindows)
                    {
                        if (glowWindow is null)
                        {
                            continue;
                        }

                        var previousWindow = NativeMethods.GetWindow(glowWindow.Handle, GW.HWNDPREV);
                        if (previousWindow != currentWindowHandle
                            && previousWindow != IntPtr.Zero)
                        {
                            NativeMethods.DeferWindowPos(windowPosInfo, currentWindowHandle, glowWindow.Handle, 0, 0, 0, 0, SWP.NOSIZE | SWP.NOMOVE | SWP.NOACTIVATE);
                        }

                        currentWindowHandle = glowWindow.Handle;
                    }

                    NativeMethods.EndDeferWindowPos(windowPosInfo);
                }

                if (ownerHandle != IntPtr.Zero)
                {
                    this.UpdateZOrderOfOwner(ownerHandle);
                }
            }
            finally
            {
                this.updatingZOrder = false;
            }
        }

        private void UpdateZOrderOfOwner(IntPtr hwndOwner)
        {
            var lastOwnedWindow = IntPtr.Zero;
            NativeMethods.EnumThreadWindows(NativeMethods.GetCurrentThreadId(), delegate(IntPtr hwnd, IntPtr _)
            {
                if (NativeMethods.GetWindow(hwnd, GW.OWNER) == hwndOwner)
                {
                    lastOwnedWindow = hwnd;
                }

                return true;
            }, IntPtr.Zero);

            if (lastOwnedWindow == IntPtr.Zero
                || NativeMethods.GetWindow(hwndOwner, GW.HWNDPREV) == lastOwnedWindow)
            {
                return;
            }

            if (this.IsGlowVisible
                && this.windowHandle != IntPtr.Zero
                && lastOwnedWindow == this.windowHandle)
            {
                var glowWindow = this.glowWindows[this.glowWindows.Length - 1];
                if (glowWindow is not null)
                {
                    lastOwnedWindow = glowWindow.Handle;
                }
            }

            NativeMethods.SetWindowPos(hwndOwner, lastOwnedWindow, 0, 0, 0, 0, SWP.NOSIZE | SWP.NOMOVE | SWP.NOACTIVATE);
        }

        #endregion

        private void DestroyGlowWindows()
        {
            for (var i = 0; i < this.glowWindows.Length; i++)
            {
                this.glowWindows[i]?.Dispose();
                this.glowWindows[i] = null;
            }
        }

        public void EndDeferGlowChanges()
        {
            var windowPosInfo = NativeMethods.BeginDeferWindowPos(this.glowWindows.Length);

            foreach (var glowWindow in this.glowWindows)
            {
                glowWindow?.CommitChanges(windowPosInfo);
            }

            NativeMethods.EndDeferWindowPos(windowPosInfo);
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
                var orCreateGlowWindow = this.GetOrCreateGlowWindow(i);
                orCreateGlowWindow.EnsureHandle();
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
                if (NativeMethods.IsWindowVisible(handle)
                    && !NativeMethods.IsIconic(handle)
                    && !NativeMethods.IsZoomed(handle))
                {
                    var result = this.AssociatedObject is not null
                           && this.AssociatedObject.ResizeMode != ResizeMode.NoResize
                           && this.GlowDepth > 0;
                    if (result == false)
                    {
                        return false;
                    }

                    if (this.DWMSupportsBorderColor
                        && this.PreferDWMBorderColor)
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
            this.UpdateGlowWindowPositions(NativeMethods.IsWindowVisible(this.windowHandle));

            this.UpdateZOrderOfThisAndOwner();
        }

        private void UpdateZOrderOfThisAndOwner()
        {
            this.UpdateZOrderOfThisAndOwner(this.windowHelper?.Owner ?? IntPtr.Zero);
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
                this.UpdateGlowVisibility(false);

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
                this.UpdateGlowVisibility(false);

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
                        ? (int)new NativeMethods.COLORREF(color!.Value).dwColor
                        : -2;
            this.DWMSupportsBorderColor = DwmHelper.SetWindowAttributeValue(this.windowHandle, DWMWINDOWATTRIBUTE.BORDER_COLOR, attrValue);

            return this.DWMSupportsBorderColor;
        }

        private void UpdateGlowDepth()
        {
            using (this.DeferGlowChanges())
            {
                this.UpdateGlowVisibility(false);

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
                }
                else
                {
                    this.makeGlowVisibleTimer.Stop();
                }

                this.makeGlowVisibleTimer.Start();
            }
            else
            {
                this.StopTimer();
                this.IsGlowVisible = shouldShowGlow;
            }
        }

        private void StopTimer()
        {
            if (this.makeGlowVisibleTimer is null)
            {
                return;
            }

            this.makeGlowVisibleTimer.Stop();
            this.makeGlowVisibleTimer.Tick -= this.OnDelayedVisibilityTimerTick;
            this.makeGlowVisibleTimer = null;
        }

        private void OnDelayedVisibilityTimerTick(object? sender, EventArgs e)
        {
            this.StopTimer();
            this.UpdateGlowWindowPositions(false);
        }
    }
}