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
                                          new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets if the popup can be closed by left mouse button down.
        /// </summary>
        public bool CloseOnMouseLeftButtonDown
        {
            get { return (bool)this.GetValue(CloseOnMouseLeftButtonDownProperty); }
            set { this.SetValue(CloseOnMouseLeftButtonDownProperty, value); }
        }

        /// <summary>Identifies the <see cref="AllowTopMost"/> dependency property.</summary>
        public static readonly DependencyProperty AllowTopMostProperty
            = DependencyProperty.Register(nameof(AllowTopMost),
                                          typeof(bool),
                                          typeof(PopupEx),
                                          new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether if the Popup should be always on top.
        /// </summary>
        public bool AllowTopMost
        {
            get { return (bool)this.GetValue(AllowTopMostProperty); }
            set { this.SetValue(AllowTopMostProperty, value); }
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
                    this.SetCurrentValue(IsOpenProperty, false);
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

        private void SetTopmostState(bool isTop)
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

            var hwnd = hwndSource.Handle;

            RECT rect;
            if (!GetWindowRect(hwnd, out rect))
            {
                return;
            }

            //Debug.WriteLine("setting z-order " + isTop);

            var left = rect.Left;
            var top = rect.Top;
            var width = rect.Width;
            var height = rect.Height;
            if (isTop)
            {
                SetWindowPos(hwnd, HWND_TOPMOST, left, top, width, height, SWP.TOPMOST);
            }
            else
            {
                // Z-Order would only get refreshed/reflected if clicking the
                // the titlebar (as opposed to other parts of the external
                // window) unless I first set the popup to HWND_BOTTOM
                // then HWND_TOP before HWND_NOTOPMOST
                SetWindowPos(hwnd, HWND_BOTTOM, left, top, width, height, SWP.TOPMOST);
                SetWindowPos(hwnd, HWND_TOP, left, top, width, height, SWP.TOPMOST);
                SetWindowPos(hwnd, HWND_NOTOPMOST, left, top, width, height, SWP.TOPMOST);
            }

            this.appliedTopMost = isTop;
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (this.CloseOnMouseLeftButtonDown)
            {
                this.SetCurrentValue(IsOpenProperty, false);
            }
        }

        private Window? hostWindow;
        private bool? appliedTopMost;
        
#pragma warning disable SA1310 // Field names should not contain underscore
        private static readonly IntPtr HWND_TOPMOST = new(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new(-2);
        private static readonly IntPtr HWND_TOP = new(0);
        private static readonly IntPtr HWND_BOTTOM = new(1);
#pragma warning restore SA1310 // Field names should not contain underscore

#pragma warning disable SA1602 // Enumeration items should be documented
        /// <summary>
        /// SetWindowPos options
        /// </summary>
        [Flags]
        internal enum SWP
        {
            ASYNCWINDOWPOS = 0x4000,
            DEFERERASE = 0x2000,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            HIDEWINDOW = 0x0080,
            NOACTIVATE = 0x0010,
            NOCOPYBITS = 0x0100,
            NOMOVE = 0x0002,
            NOOWNERZORDER = 0x0200,
            NOREDRAW = 0x0008,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            NOSIZE = 0x0001,
            NOZORDER = 0x0004,
            SHOWWINDOW = 0x0040,
            TOPMOST = NOACTIVATE | NOOWNERZORDER | NOSIZE | NOMOVE | NOREDRAW | NOSENDCHANGING,
        }
#pragma warning restore SA1602 // Enumeration items should be documented

        internal static int LOWORD(int i)
        {
            return (short)(i & 0xFFFF);
        }

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SIZE
        {
            public int cx;
            public int cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            private int left;
            private int top;
            private int right;
            private int bottom;

            public void Offset(int dx, int dy)
            {
                this.left += dx;
                this.top += dy;
                this.right += dx;
                this.bottom += dy;
            }

            public int Left
            {
                get { return this.left; }
                set { this.left = value; }
            }

            public int Right
            {
                get { return this.right; }
                set { this.right = value; }
            }

            public int Top
            {
                get { return this.top; }
                set { this.top = value; }
            }

            public int Bottom
            {
                get { return this.bottom; }
                set { this.bottom = value; }
            }

            public int Width
            {
                get { return this.right - this.left; }
            }

            public int Height
            {
                get { return this.bottom - this.top; }
            }

            public POINT Position
            {
                get { return new POINT { x = this.left, y = this.top }; }
            }

            public SIZE Size
            {
                get { return new SIZE { cx = this.Width, cy = this.Height }; }
            }

            public static RECT Union(RECT rect1, RECT rect2)
            {
                return new RECT
                {
                    Left = Math.Min(rect1.Left, rect2.Left),
                    Top = Math.Min(rect1.Top, rect2.Top),
                    Right = Math.Max(rect1.Right, rect2.Right),
                    Bottom = Math.Max(rect1.Bottom, rect2.Bottom),
                };
            }

            public override bool Equals(object? obj)
            {
                try
                {
                    var rc = (RECT)obj!;
                    return rc.bottom == this.bottom
                           && rc.left == this.left
                           && rc.right == this.right
                           && rc.top == this.top;
                }
                catch (InvalidCastException)
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return (this.left << 16 | LOWORD(this.right)) ^ (this.top << 16 | LOWORD(this.bottom));
            }
        }
        
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

        [SecurityCritical]
        [DllImport("user32.dll", EntryPoint = "GetWindowRect", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [SecurityCritical]
        [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
#pragma warning disable SA1300 // Element should begin with upper-case letter
        private static extern bool _SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SWP uFlags);
#pragma warning restore SA1300 // Element should begin with upper-case letter

        [SecurityCritical]
        private static bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SWP uFlags)
        {
            if (!_SetWindowPos(hWnd, hWndInsertAfter, x, y, cx, cy, uFlags))
            {
                // If this fails it's never worth taking down the process.  Let the caller deal with the error if they want.
                return false;
            }

            return true;
        }
    }
}