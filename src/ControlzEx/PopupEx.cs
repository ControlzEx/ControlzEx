#pragma warning disable CA1060
namespace ControlzEx
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Interop;
    using ControlzEx.Internal.KnownBoxes;
    using global::Windows.Win32;
    using global::Windows.Win32.Foundation;
    using global::Windows.Win32.UI.WindowsAndMessaging;

    /// <summary>
    /// This custom popup can be used by validation error templates or something else.
    /// It provides some additional nice features:
    ///     - repositioning if host-window size or location changed
    ///     - repositioning if host-window gets maximized and vice versa
    ///     - it's only topmost if the host-window is activated
    /// </summary>
    public class PopupEx : Popup
    {
        /// <summary>Identifies the <see cref="CloseOnMouseLeftButtonDown"/> dependency property.</summary>
        public static readonly DependencyProperty CloseOnMouseLeftButtonDownProperty
            = DependencyProperty.Register(nameof(CloseOnMouseLeftButtonDown),
                                          typeof(bool),
                                          typeof(PopupEx),
                                          new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Gets or sets if the popup can be closed by left mouse button down.
        /// </summary>
        public bool CloseOnMouseLeftButtonDown
        {
            get { return (bool)this.GetValue(CloseOnMouseLeftButtonDownProperty); }
            set { this.SetValue(CloseOnMouseLeftButtonDownProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>Identifies the <see cref="AllowTopMost"/> dependency property.</summary>
        public static readonly DependencyProperty AllowTopMostProperty
            = DependencyProperty.Register(nameof(AllowTopMost),
                                          typeof(bool),
                                          typeof(PopupEx),
                                          new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Gets or sets whether if the Popup should be always on top.
        /// </summary>
        public bool AllowTopMost
        {
            get { return (bool)this.GetValue(AllowTopMostProperty); }
            set { this.SetValue(AllowTopMostProperty, BooleanBoxes.Box(value)); }
        }

        public PopupEx()
        {
            this.Loaded += this.PopupEx_Loaded;
            this.Opened += this.PopupEx_Opened;
        }

        /// <summary>
        /// Causes the popup to update it's position according to it's current settings.
        /// </summary>
        public void RefreshPosition()
        {
            var offset = this.HorizontalOffset;
            // "bump" the offset to cause the popup to reposition itself on its own
            this.SetCurrentValue(HorizontalOffsetProperty, offset + 1);
            this.SetCurrentValue(HorizontalOffsetProperty, offset);
        }

        private void PopupEx_Loaded(object? sender, RoutedEventArgs e)
        {
            var target = this.PlacementTarget as FrameworkElement;
            if (target is null)
            {
                return;
            }

            this.hostWindow = Window.GetWindow(target);
            if (this.hostWindow is null)
            {
                return;
            }

            this.hostWindow.LocationChanged -= this.HostWindow_SizeOrLocationChanged;
            this.hostWindow.LocationChanged += this.HostWindow_SizeOrLocationChanged;
            this.hostWindow.SizeChanged -= this.HostWindow_SizeOrLocationChanged;
            this.hostWindow.SizeChanged += this.HostWindow_SizeOrLocationChanged;
            target.SizeChanged -= this.HostWindow_SizeOrLocationChanged;
            target.SizeChanged += this.HostWindow_SizeOrLocationChanged;
            this.hostWindow.StateChanged -= this.HostWindow_StateChanged;
            this.hostWindow.StateChanged += this.HostWindow_StateChanged;
            this.hostWindow.Activated -= this.HostWindow_Activated;
            this.hostWindow.Activated += this.HostWindow_Activated;
            this.hostWindow.Deactivated -= this.HostWindow_Deactivated;
            this.hostWindow.Deactivated += this.HostWindow_Deactivated;

            this.Unloaded -= this.PopupEx_Unloaded;
            this.Unloaded += this.PopupEx_Unloaded;
        }

        private void PopupEx_Opened(object? sender, EventArgs e)
        {
            this.SetTopmostState(this.hostWindow?.IsActive ?? true);
        }

        private void HostWindow_Activated(object? sender, EventArgs e)
        {
            this.SetTopmostState(true);
        }

        private void HostWindow_Deactivated(object? sender, EventArgs e)
        {
            this.SetTopmostState(false);
        }

        private void PopupEx_Unloaded(object? sender, RoutedEventArgs e)
        {
            var target = this.PlacementTarget as FrameworkElement;
            if (target is not null)
            {
                target.SizeChanged -= this.HostWindow_SizeOrLocationChanged;
            }

            if (this.hostWindow is not null)
            {
                this.hostWindow.LocationChanged -= this.HostWindow_SizeOrLocationChanged;
                this.hostWindow.SizeChanged -= this.HostWindow_SizeOrLocationChanged;
                this.hostWindow.StateChanged -= this.HostWindow_StateChanged;
                this.hostWindow.Activated -= this.HostWindow_Activated;
                this.hostWindow.Deactivated -= this.HostWindow_Deactivated;
            }

            this.Unloaded -= this.PopupEx_Unloaded;
            this.Opened -= this.PopupEx_Opened;
            this.hostWindow = null;
        }

        private void HostWindow_StateChanged(object? sender, EventArgs e)
        {
            if (this.hostWindow is not null && this.hostWindow.WindowState != WindowState.Minimized)
            {
                // special handling for validation popup
                var holder = this.PlacementTarget is FrameworkElement target ? target.DataContext as AdornedElementPlaceholder : null;
                var adornedElement = holder?.AdornedElement;
                if (adornedElement is not null)
                {
                    this.SetCurrentValue(PopupAnimationProperty, PopupAnimation.None);
                    this.SetCurrentValue(IsOpenProperty, BooleanBoxes.FalseBox);
                    var errorTemplate = adornedElement.GetValue(Validation.ErrorTemplateProperty);
                    adornedElement.SetCurrentValue(Validation.ErrorTemplateProperty, null);
                    adornedElement.SetCurrentValue(Validation.ErrorTemplateProperty, errorTemplate);
                }
            }
        }

        private void HostWindow_SizeOrLocationChanged(object? sender, EventArgs e)
        {
            this.RefreshPosition();
        }

        private unsafe void SetTopmostState(bool isTop)
        {
            isTop &= this.AllowTopMost;

            // Don’t apply state if it’s the same as incoming state
            if (this.appliedTopMost.HasValue && this.appliedTopMost == isTop)
            {
                return;
            }

            if (this.Child is null)
            {
                return;
            }

            var hwndSource = PresentationSource.FromVisual(this.Child) as HwndSource;
            if (hwndSource is null)
            {
                return;
            }

            var hwnd = new HWND(hwndSource.Handle);

            RECT rect;
            if (!PInvoke.GetWindowRect(hwnd, &rect))
            {
                return;
            }

            //Debug.WriteLine("setting z-order " + isTop);

            const SET_WINDOW_POS_FLAGS SWP_TOPMOST = SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOREDRAW | SET_WINDOW_POS_FLAGS.SWP_NOSENDCHANGING;

            var left = rect.left;
            var top = rect.top;
            var width = rect.GetWidth();
            var height = rect.GetHeight();
            if (isTop)
            {
                PInvoke.SetWindowPos(hwnd, HWND_TOPMOST, left, top, width, height, SWP_TOPMOST);
            }
            else
            {
                // Z-Order would only get refreshed/reflected if clicking the
                // the titlebar (as opposed to other parts of the external
                // window) unless I first set the popup to HWND_BOTTOM
                // then HWND_TOP before HWND_NOTOPMOST
                PInvoke.SetWindowPos(hwnd, HWND_BOTTOM, left, top, width, height, SWP_TOPMOST);
                PInvoke.SetWindowPos(hwnd, HWND_TOP, left, top, width, height, SWP_TOPMOST);
                PInvoke.SetWindowPos(hwnd, HWND_NOTOPMOST, left, top, width, height, SWP_TOPMOST);
            }

            this.appliedTopMost = isTop;
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (this.CloseOnMouseLeftButtonDown)
            {
                this.SetCurrentValue(IsOpenProperty, BooleanBoxes.FalseBox);
            }
        }

        private Window? hostWindow;
        private bool? appliedTopMost;

        #pragma warning disable SA1310
        private static readonly HWND HWND_TOPMOST = new(new IntPtr(-1));
        private static readonly HWND HWND_NOTOPMOST = new(new IntPtr(-2));
        private static readonly HWND HWND_TOP = new(IntPtr.Zero);
        private static readonly HWND HWND_BOTTOM = new(new IntPtr(1));

#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter
    }
}