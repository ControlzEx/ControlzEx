#nullable enable

#pragma warning disable 618
namespace ControlzEx.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Threading;
    using ControlzEx.Controls.Internal;
    using ControlzEx.Standard;
    using Microsoft.Xaml.Behaviors;

    public class GlowWindowBehavior : Behavior<Window>
    {
        private static readonly TimeSpan glowTimerDelay = TimeSpan.FromMilliseconds(200); //200 ms delay, the same as in visual studio
        private DispatcherTimer? makeGlowVisibleTimer;
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        private WindowInteropHelper windowHelper;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        private HwndSource? hwndSource;

        private readonly GlowWindow?[] glowWindows = new GlowWindow[4];

        private IEnumerable<GlowWindow> LoadedGlowWindows => this.glowWindows.Where(w => w != null)!;

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="GlowBrush"/>.
        /// </summary>
        public static readonly DependencyProperty GlowBrushProperty = DependencyProperty.Register(nameof(GlowBrush), typeof(Brush), typeof(GlowWindowBehavior), new PropertyMetadata(default(Brush), OnGlowBrushChanged));

        private static void OnGlowBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GlowWindowBehavior)d).UpdateGlowColors();
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
        public static readonly DependencyProperty NonActiveGlowBrushProperty = DependencyProperty.Register(nameof(NonActiveGlowBrush), typeof(Brush), typeof(GlowWindowBehavior), new PropertyMetadata(default(Brush), OnNonActiveGlowBrushChanged));

        private static void OnNonActiveGlowBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GlowWindowBehavior)d).UpdateGlowColors();
        }

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
        public static readonly DependencyProperty IsGlowTransitionEnabledProperty = DependencyProperty.Register(nameof(IsGlowTransitionEnabled), typeof(bool), typeof(GlowWindowBehavior), new PropertyMetadata(true));

        /// <summary>
        /// Defines whether glow transitions should be used or not.
        /// </summary>
        public bool IsGlowTransitionEnabled
        {
            get => (bool)this.GetValue(IsGlowTransitionEnabledProperty);
            set => this.SetValue(IsGlowTransitionEnabledProperty, value);
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
        /// Gets or sets resize border thickness.
        /// </summary>
        public int GlowDepth
        {
            get => (int)this.GetValue(GlowDepthProperty);
            set => this.SetValue(GlowDepthProperty, value);
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

            base.OnDetaching();
        }

        private void AssociatedObjectActivatedOrDeactivated(object? sender, EventArgs e)
        {
            this.UpdateGlowActiveState();
        }

        private void AssociatedObjectSourceInitialized(object? sender, EventArgs e)
        {
            this.windowHelper = new WindowInteropHelper(this.AssociatedObject);
            this.hwndSource = HwndSource.FromHwnd(this.windowHelper.Handle);
            this.hwndSource?.AddHook(this.AssociatedObjectWindowProc);

            this.AssociatedObject.Closed += this.AssociatedObjectOnClosed;

            this.AssociatedObject.Activated += this.AssociatedObjectActivatedOrDeactivated;
            this.AssociatedObject.Deactivated += this.AssociatedObjectActivatedOrDeactivated;

            this.CreateGlowWindowHandles();
        }

        private void AssociatedObjectOnClosed(object? o, EventArgs args)
        {
            this.AssociatedObject.Closed -= this.AssociatedObjectOnClosed;

            // todo: detach here????

            this.StopTimer();
            this.DestroyGlowWindows();
        }

#pragma warning disable 618, SA1401
        private bool updatingZOrder;

        public int DeferGlowChangesCount;

        private IntPtr AssociatedObjectWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (this.hwndSource?.RootVisual is null)
            {
                return IntPtr.Zero;
            }

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch ((WM)msg)
            {
                // Z-Index must be updated when WINDOWPOSCHANGED
                case WM.WINDOWPOSCHANGED:
                    {
                        var wp = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS))!;
                        this.UpdateGlowWindowPositions((wp.flags & SWP.SHOWWINDOW) == 0);

                        this.UpdateZOrderOfThisAndOwner();
                    }

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
                var handle = this.windowHelper.Handle;
                foreach (var loadedGlowWindow in this.LoadedGlowWindows)
                {
                    var window = NativeMethods.GetWindow(loadedGlowWindow.Handle, GW.HWNDPREV);
                    if (window != handle)
                    {
                        NativeMethods.SetWindowPos(loadedGlowWindow.Handle, handle, 0, 0, 0, 0, SWP.NOSIZE | SWP.NOMOVE | SWP.NOACTIVATE);
                    }

                    handle = loadedGlowWindow.Handle;
                }

                var owner = this.windowHelper.Owner;
                if (owner != IntPtr.Zero)
                {
                    this.UpdateZOrderOfOwner(owner);
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
            NativeMethods.EnumThreadWindows(NativeMethods.GetCurrentThreadId(), delegate(IntPtr hwnd, IntPtr lParam)
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
                && lastOwnedWindow == this.windowHelper.Handle)
            {
                var glowWindow = this.LoadedGlowWindows.LastOrDefault();
                if (glowWindow != null)
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
            foreach (var loadedGlowWindow in this.LoadedGlowWindows)
            {
                loadedGlowWindow.CommitChanges();
            }
        }

        private GlowWindow GetOrCreateGlowWindow(int index)
        {
            if (this.glowWindows[index] is null)
            {
                this.glowWindows[index] = new GlowWindow(this.AssociatedObject, this, (Dock)index)
                {
                    ActiveGlowColor = ((SolidColorBrush?)this.GlowBrush)?.Color ?? Colors.Transparent,
                    InactiveGlowColor = ((SolidColorBrush?)this.NonActiveGlowBrush)?.Color ?? Colors.Transparent,
                    IsActive = this.AssociatedObject.IsActive,
                    GlowDepth = this.GlowDepth
                };
            }

            return this.glowWindows[index]!;
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
                var handle = this.windowHelper.Handle;
                if (NativeMethods.IsWindowVisible(handle)
                    && !NativeMethods.IsIconic(handle)
                    && !NativeMethods.IsZoomed(handle))
                {
                    return this.AssociatedObject.ResizeMode != ResizeMode.NoResize;
                }

                return false;
            }
        }

        private void UpdateGlowWindowPositions(bool delayIfNecessary)
        {
            using (this.DeferGlowChanges())
            {
                this.UpdateGlowVisibility(delayIfNecessary);

                foreach (var loadedGlowWindow in this.LoadedGlowWindows)
                {
                    loadedGlowWindow.UpdateWindowPos();
                }
            }
        }

        private void UpdateGlowColors()
        {
            using (this.DeferGlowChanges())
            {
                foreach (var loadedGlowWindow in this.LoadedGlowWindows)
                {
                    loadedGlowWindow.ActiveGlowColor = ((SolidColorBrush?)this.GlowBrush)?.Color ?? Colors.Transparent;
                    loadedGlowWindow.InactiveGlowColor = ((SolidColorBrush?)this.NonActiveGlowBrush)?.Color ?? Colors.Transparent;
                }
            }
        }

        private void UpdateGlowActiveState()
        {
            using (this.DeferGlowChanges())
            {
                foreach (var loadedGlowWindow in this.LoadedGlowWindows)
                {
                    loadedGlowWindow.IsActive = this.AssociatedObject.IsActive;
                }
            }
        }

        private void UpdateGlowDepth()
        {
            using (this.DeferGlowChanges())
            {
                foreach (var loadedGlowWindow in this.LoadedGlowWindows)
                {
                    loadedGlowWindow.GlowDepth = this.GlowDepth;
                    loadedGlowWindow.UpdateWindowPos();
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