#pragma warning disable 618, CA1001

// ReSharper disable once CheckNamespace
namespace ControlzEx.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Security;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Threading;
    using ControlzEx;
    using ControlzEx.Internal.KnownBoxes;
    using ControlzEx.Native;
    using ControlzEx.Standard;
    using Microsoft.Xaml.Behaviors;

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
        private HwndSource? hwndSource;

        private PropertyChangeNotifier? borderThicknessChangeNotifier;
        private Thickness? savedBorderThickness;

        private bool isCleanedUp;

        private readonly Thickness cornerGripThickness = new(Constants.ResizeCornerGripThickness);

        private struct SystemParameterBoundProperty
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

        /// <summary>Identifies the <see cref="ResizeBorderThickness"/> dependency property.</summary>
        public static readonly DependencyProperty ResizeBorderThicknessProperty =
            DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(WindowChromeBehavior), new PropertyMetadata(GetDefaultResizeBorderThickness(), OnResizeBorderThicknessChanged), (value) => ((Thickness)value).IsNonNegative());

        private static void OnResizeBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowChromeBehavior)d;
            behavior._OnChromePropertyChangedThatRequiresRepaint();
        }

        /// <summary>
        /// Defines if the Taskbar should be ignored when maximizing a Window.
        /// This only works with WindowStyle = None.
        /// </summary>
        public bool IgnoreTaskbarOnMaximize
        {
            get { return (bool)this.GetValue(IgnoreTaskbarOnMaximizeProperty); }
            set { this.SetValue(IgnoreTaskbarOnMaximizeProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="IgnoreTaskbarOnMaximize"/>.
        /// </summary>
        public static readonly DependencyProperty IgnoreTaskbarOnMaximizeProperty =
            DependencyProperty.Register(nameof(IgnoreTaskbarOnMaximize), typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(BooleanBoxes.FalseBox, OnIgnoreTaskbarOnMaximizeChanged));

        /// <summary>
        /// Gets/sets if the border thickness value should be kept on maximize
        /// if the MaxHeight/MaxWidth of the window is less than the monitor resolution.
        /// </summary>
        public bool KeepBorderOnMaximize
        {
            get { return (bool)this.GetValue(KeepBorderOnMaximizeProperty); }
            set { this.SetValue(KeepBorderOnMaximizeProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="KeepBorderOnMaximize"/>.
        /// </summary>
        public static readonly DependencyProperty KeepBorderOnMaximizeProperty = DependencyProperty.Register(nameof(KeepBorderOnMaximize), typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(BooleanBoxes.TrueBox, OnKeepBorderOnMaximizeChanged));

        /// <summary>
        /// Gets or sets whether the resizing of the window should be tried in a way that does not cause flicker/jitter, especially when resizing from the left side.
        /// </summary>
        /// <remarks>
        /// Please note that setting this to <c>true</c> may cause resize lag and black areas appearing on some systems.
        /// </remarks>
        public bool TryToBeFlickerFree
        {
            get { return (bool)this.GetValue(TryToBeFlickerFreeProperty); }
            set { this.SetValue(TryToBeFlickerFreeProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="TryToBeFlickerFree"/>.
        /// </summary>
        public static readonly DependencyProperty TryToBeFlickerFreeProperty = DependencyProperty.Register(nameof(TryToBeFlickerFree), typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(BooleanBoxes.FalseBox, OnTryToBeFlickerFreeChanged));

        private static readonly DependencyPropertyKey IsNCActivePropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsNCActive), typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="IsNCActive"/>.
        /// </summary>
        public static readonly DependencyProperty IsNCActiveProperty = IsNCActivePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether the non-client area is active or not.
        /// </summary>
        public bool IsNCActive
        {
            get { return (bool)this.GetValue(IsNCActiveProperty); }
            private set { this.SetValue(IsNCActivePropertyKey, BooleanBoxes.Box(value)); }
        }

        public static readonly DependencyProperty EnableMinimizeProperty = DependencyProperty.Register(nameof(EnableMinimize), typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(BooleanBoxes.TrueBox, OnEnableMinimizeChanged));

        private static void OnEnableMinimizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue && e.NewValue is bool showMinButton)
            {
                var behavior = (WindowChromeBehavior)d;

                behavior.UpdateMinimizeSystemMenu(showMinButton);
            }
        }

        private void UpdateMinimizeSystemMenu(bool isVisible)
        {
            if (this.windowHandle != IntPtr.Zero)
            {
                if (this.hwndSource?.IsDisposed == true || this.hwndSource?.RootVisual is null)
                {
                    return;
                }

                if (isVisible)
                {
                    this._ModifyStyle(0, WS.MINIMIZEBOX);
                }
                else
                {
                    this._ModifyStyle(WS.MINIMIZEBOX, 0);
                }

                this._UpdateSystemMenu(this.AssociatedObject?.WindowState);
            }
        }

        /// <summary>
        /// Gets or sets whether if the minimize button is visible and the minimize system menu is enabled.
        /// </summary>
        public bool EnableMinimize
        {
            get { return (bool)this.GetValue(EnableMinimizeProperty); }
            set { this.SetValue(EnableMinimizeProperty, BooleanBoxes.Box(value)); }
        }

        public static readonly DependencyProperty EnableMaxRestoreProperty = DependencyProperty.Register(nameof(EnableMaxRestore), typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(BooleanBoxes.TrueBox, OnEnableMaxRestoreChanged));

        private static void OnEnableMaxRestoreChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue && e.NewValue is bool showMaxRestoreButton)
            {
                var behavior = (WindowChromeBehavior)d;

                behavior.UpdateMaxRestoreSystemMenu(showMaxRestoreButton);
            }
        }

        private void UpdateMaxRestoreSystemMenu(bool isVisible)
        {
            if (this.windowHandle != IntPtr.Zero)
            {
                if (this.hwndSource?.IsDisposed == true || this.hwndSource?.RootVisual is null)
                {
                    return;
                }

                if (isVisible)
                {
                    this._ModifyStyle(0, WS.MAXIMIZEBOX);
                }
                else
                {
                    this._ModifyStyle(WS.MAXIMIZEBOX, 0);
                }

                this._UpdateSystemMenu(this.AssociatedObject?.WindowState);
            }
        }

        /// <summary>
        /// Gets or sets whether if the maximize/restore button is visible and the maximize/restore system menu is enabled.
        /// </summary>
        public bool EnableMaxRestore
        {
            get { return (bool)this.GetValue(EnableMaxRestoreProperty); }
            set { this.SetValue(EnableMaxRestoreProperty, BooleanBoxes.Box(value)); }
        }

        public static readonly DependencyProperty CornerPreferenceProperty = 
            DependencyProperty.Register(nameof(CornerPreference), typeof(DWM_WINDOW_CORNER_PREFERENCE), typeof(WindowChromeBehavior), new PropertyMetadata(DWM_WINDOW_CORNER_PREFERENCE.DEFAULT, OnCornerPreferenceChanged));

        public DWM_WINDOW_CORNER_PREFERENCE CornerPreference
        {
            get => (DWM_WINDOW_CORNER_PREFERENCE)this.GetValue(CornerPreferenceProperty);
            set => this.SetValue(CornerPreferenceProperty, value);
        }

        private static void OnCornerPreferenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowChromeBehavior)d;

            behavior.UpdateDWMCornerPreference((DWM_WINDOW_CORNER_PREFERENCE)e.NewValue);
        }

        /// <inheritdoc />
        protected override void OnAttached()
        {
            // no transparency, because it has more then one unwanted issues
            if (this.AssociatedObject.AllowsTransparency
                && this.AssociatedObject.IsLoaded == false
                && new WindowInteropHelper(this.AssociatedObject).Handle == IntPtr.Zero)
            {
                try
                {
                    this.AssociatedObject.SetCurrentValue(Window.AllowsTransparencyProperty, BooleanBoxes.FalseBox);
                }
                catch (Exception)
                {
                    //For some reason, we can't determine if the window has loaded or not, so we swallow the exception.
                }
            }

            this.savedBorderThickness = this.AssociatedObject.BorderThickness;
            this.borderThicknessChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Control.BorderThicknessProperty);
            this.borderThicknessChangeNotifier.ValueChanged += this.BorderThicknessChangeNotifierOnValueChanged;

            this.AssociatedObject.SourceInitialized += this.AssociatedObject_SourceInitialized;
            this.AssociatedObject.Loaded += this.AssociatedObject_Loaded;
            this.AssociatedObject.Unloaded += this.AssociatedObject_Unloaded;
            this.AssociatedObject.Closed += this.AssociatedObject_Closed;
            this.AssociatedObject.StateChanged += this.AssociatedObject_StateChanged;

            base.OnAttached();
        }

        /// <summary>
        /// Gets the default resize border thickness from the system parameters.
        /// </summary>
        public static Thickness GetDefaultResizeBorderThickness()
        {
            var dpiX = NativeMethods.GetDeviceCaps(SafeDC.GetDesktop(), DeviceCap.LOGPIXELSX);
            var dpiY = NativeMethods.GetDeviceCaps(SafeDC.GetDesktop(), DeviceCap.LOGPIXELSY);
            var xframe = NativeMethods.GetSystemMetrics(SM.CXFRAME);
            var yframe = NativeMethods.GetSystemMetrics(SM.CYFRAME);
            var padding = NativeMethods.GetSystemMetrics(SM.CXPADDEDBORDER);
            xframe += padding;
            yframe += padding;
            var logical = DpiHelper.DeviceSizeToLogical(new Size(xframe, yframe), dpiX / 96.0, dpiY / 96.0);
            return new Thickness(logical.Width, logical.Height, logical.Width, logical.Height);
        }

        private void BorderThicknessChangeNotifierOnValueChanged(object? sender, EventArgs e)
        {
            // It's bad if the window is null at this point, but we check this here to prevent the possible occurred exception
            var window = this.AssociatedObject;
            if (window is not null)
            {
                this.savedBorderThickness = window.BorderThickness;
            }
        }

        private static void OnIgnoreTaskbarOnMaximizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowChromeBehavior)d;

            // A few things to consider when removing the below hack
            // - ResizeMode="NoResize"
            //   WindowState="Maximized"
            //   IgnoreTaskbarOnMaximize="True"
            // - Changing IgnoreTaskbarOnMaximize while window is maximized

            // Changing the WindowState solves all, known, issues with changing IgnoreTaskbarOnMaximize.
            // Since IgnoreTaskbarOnMaximize is not changed all the time this hack seems to be less risky than anything else.
            if (behavior.AssociatedObject?.WindowState == WindowState.Maximized)
            {
                behavior._OnChromePropertyChangedThatRequiresRepaint();

                behavior.AssociatedObject.SetCurrentValue(Window.WindowStateProperty, WindowState.Normal);
                behavior.AssociatedObject.SetCurrentValue(Window.WindowStateProperty, WindowState.Maximized);
            }
        }

        private static void OnKeepBorderOnMaximizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowChromeBehavior)d;

            behavior.HandleStateChanged();
        }

        private static void OnTryToBeFlickerFreeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowChromeBehavior)d;

            behavior._OnChromePropertyChangedThatRequiresRepaint();
        }

        [SecuritySafeCritical]
        private void Cleanup(bool isClosing)
        {
            if (this.isCleanedUp)
            {
                return;
            }

            this.isCleanedUp = true;

            this.OnCleanup();

            // clean up events
            this.AssociatedObject.SourceInitialized -= this.AssociatedObject_SourceInitialized;
            this.AssociatedObject.Loaded -= this.AssociatedObject_Loaded;
            this.AssociatedObject.Unloaded -= this.AssociatedObject_Unloaded;
            this.AssociatedObject.Closed -= this.AssociatedObject_Closed;
            this.AssociatedObject.StateChanged -= this.AssociatedObject_StateChanged;

            this.hwndSource?.RemoveHook(this.WindowProc);

            this._RestoreStandardChromeState(isClosing);
        }

        /// <summary>
        /// Occurs during the cleanup of this behavior.
        /// </summary>
        protected virtual void OnCleanup()
        {
            // nothing here
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            this.Cleanup(false);

            base.OnDetaching();
        }

        private void AssociatedObject_SourceInitialized(object? sender, EventArgs e)
        {
            this.windowHandle = new WindowInteropHelper(this.AssociatedObject).Handle;
            this.nonClientControlManager = new NonClientControlManager(this.AssociatedObject);

            if (this.windowHandle == IntPtr.Zero)
            {
                throw new Exception("Uups, at this point we really need the Handle from the associated object!");
            }

            if (this.AssociatedObject.SizeToContent != SizeToContent.Manual
                && this.AssociatedObject.WindowState == WindowState.Normal)
            {
                // Another try to fix SizeToContent
                // without this we get nasty glitches at the borders
                Invoke(this.AssociatedObject, () =>
                                              {
                                                  this.AssociatedObject.InvalidateMeasure();
                                              });
            }

            this.hwndSource = HwndSource.FromHwnd(this.windowHandle);
            this.hwndSource?.AddHook(this.WindowProc);

            this._ApplyNewCustomChrome();

            // handle the maximized state here too (to handle the border in a correct way)
            this.HandleStateChanged();
        }

#pragma warning disable CA2109
        /// <summary>
        /// Is called when the associated object of this instance is loaded
        /// </summary>
        protected virtual void AssociatedObject_Loaded(object? sender, RoutedEventArgs e)
        {
            //this._UpdateFrameState(true);
        }
#pragma warning restore CA2109

        private void AssociatedObject_Unloaded(object? sender, RoutedEventArgs e)
        {
            this.Cleanup(false);
        }

        private void AssociatedObject_Closed(object? sender, EventArgs e)
        {
            this.Cleanup(true);
        }

        private void AssociatedObject_StateChanged(object? sender, EventArgs e)
        {
            this.HandleStateChanged();
        }

        private void HandleStateChanged()
        {
            this.HandleBorderThicknessDuringMaximize();

            if (this.AssociatedObject.WindowState == WindowState.Maximized)
            {
                // Workaround for:
                // MaxWidth="someValue"
                // SizeToContent = "WidthAndHeight"
                // Dragging the window to the top with those things set does not change the height of the Window
                if (this.AssociatedObject.SizeToContent != SizeToContent.Manual)
                {
                    this.AssociatedObject.SetCurrentValue(Window.SizeToContentProperty, SizeToContent.Manual);
                }
            }
            else if (this.AssociatedObject.WindowState == WindowState.Normal
                     && this.IgnoreTaskbarOnMaximize)
            {
                // Required to fix wrong NC area rendering.
                this.ForceNativeWindowRedraw();
            }
        }

        private void ForceNativeWindowRedraw()
        {
            if (this.windowHandle == IntPtr.Zero
                || this.hwndSource is null
                || this.hwndSource.IsDisposed)
            {
                return;
            }

            NativeMethods.SetWindowPos(this.windowHandle, IntPtr.Zero, 0, 0, 0, 0, SwpFlags);
        }

        /// <summary>
        /// This fix is needed because style triggers don't work if someone sets the value locally/directly on the window.
        /// </summary>
        private void HandleBorderThicknessDuringMaximize()
        {
            this.borderThicknessChangeNotifier!.RaiseValueChanged = false;

            if (this.AssociatedObject.WindowState == WindowState.Maximized)
            {
                var monitor = IntPtr.Zero;

                if (this.windowHandle != IntPtr.Zero)
                {
                    monitor = UnsafeNativeMethods.MonitorFromWindow(this.windowHandle, MonitorOptions.MONITOR_DEFAULTTONEAREST);
                }

                if (monitor != IntPtr.Zero)
                {
                    var rightBorderThickness = 0D;
                    var bottomBorderThickness = 0D;

                    if (this.KeepBorderOnMaximize
                        && this.savedBorderThickness.HasValue)
                    {
                        var monitorInfo = NativeMethods.GetMonitorInfo(monitor);
                        var monitorRect = this.IgnoreTaskbarOnMaximize ? monitorInfo.rcMonitor : monitorInfo.rcWork;

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
                    this.AssociatedObject.SetCurrentValue(Control.BorderThicknessProperty, new Thickness(0, 0, rightBorderThickness, bottomBorderThickness));
                }
                else // Can't get monitor info, so just remove all border thickness
                {
                    this.AssociatedObject.SetCurrentValue(Control.BorderThicknessProperty, new Thickness(0));
                }
            }
            else
            {
                this.AssociatedObject.SetCurrentValue(Control.BorderThicknessProperty, this.savedBorderThickness.GetValueOrDefault(new Thickness(0)));
            }

            this.borderThicknessChangeNotifier.RaiseValueChanged = true;
        }

        private bool UpdateDWMCornerPreference(DWM_WINDOW_CORNER_PREFERENCE cornerPreference)
        {
            if (this.windowHandle == IntPtr.Zero)
            {
                return false;
            }

            return DwmHelper.SetWindowAttributeValue(this.windowHandle, DWMWINDOWATTRIBUTE.WINDOW_CORNER_PREFERENCE, (int)cornerPreference);
        }

        private static void Invoke(DispatcherObject dispatcherObject, Action invokeAction)
        {
            if (dispatcherObject is null)
            {
                throw new ArgumentNullException(nameof(dispatcherObject));
            }

            if (invokeAction is null)
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

        private static readonly List<SystemParameterBoundProperty> boundProperties = new()
        {
            new SystemParameterBoundProperty { DependencyProperty = ResizeBorderThicknessProperty, SystemParameterPropertyName = nameof(SystemParameters.WindowResizeBorderThickness) },
        };
    }
}