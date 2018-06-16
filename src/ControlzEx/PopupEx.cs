using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;

namespace ControlzEx
{
    /// <summary>
    /// This custom popup can be used by validation error templates or something else.
    /// It provides some additional nice features:
    ///     - repositioning if host-window size or location changed
    ///     - repositioning if host-window gets maximized and vice versa
    ///     - it's only topmost if the host-window is activated
    /// </summary>
    public class PopupEx : Popup
    {
        public static readonly DependencyProperty CloseOnMouseLeftButtonDownProperty
            = DependencyProperty.Register(nameof(CloseOnMouseLeftButtonDown),
                                          typeof(bool),
                                          typeof(PopupEx),
                                          new PropertyMetadata(false));

        /// <summary>
        /// Gets/sets if the popup can be closed by left mouse button down.
        /// </summary>
        public bool CloseOnMouseLeftButtonDown
        {
            get => (bool) this.GetValue(CloseOnMouseLeftButtonDownProperty);
            set => this.SetValue(CloseOnMouseLeftButtonDownProperty, value);
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

        private void PopupEx_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(this.PlacementTarget is FrameworkElement target))
            {
                return;
            }

            this._hostWindow = Window.GetWindow(target);
            if (this._hostWindow == null)
            {
                return;
            }

            this._hostWindow.LocationChanged -= this.hostWindow_SizeOrLocationChanged;
            this._hostWindow.LocationChanged += this.hostWindow_SizeOrLocationChanged;
            this._hostWindow.SizeChanged -= this.hostWindow_SizeOrLocationChanged;
            this._hostWindow.SizeChanged += this.hostWindow_SizeOrLocationChanged;
            target.SizeChanged -= this.hostWindow_SizeOrLocationChanged;
            target.SizeChanged += this.hostWindow_SizeOrLocationChanged;
            this._hostWindow.StateChanged -= this.hostWindow_StateChanged;
            this._hostWindow.StateChanged += this.hostWindow_StateChanged;
            this._hostWindow.Activated -= this.hostWindow_Activated;
            this._hostWindow.Activated += this.hostWindow_Activated;
            this._hostWindow.Deactivated -= this.hostWindow_Deactivated;
            this._hostWindow.Deactivated += this.hostWindow_Deactivated;

            this.Unloaded -= this.PopupEx_Unloaded;
            this.Unloaded += this.PopupEx_Unloaded;
        }

        private void PopupEx_Opened(object sender, EventArgs e)
        {
            this.SetTopmostState(true);
        }

        private void hostWindow_Activated(object sender, EventArgs e)
        {
            this.SetTopmostState(true);
        }

        private void hostWindow_Deactivated(object sender, EventArgs e)
        {
            this.SetTopmostState(false);
        }

        private void PopupEx_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.PlacementTarget is FrameworkElement target)
            {
                target.SizeChanged -= this.hostWindow_SizeOrLocationChanged;
            }
            if (this._hostWindow != null)
            {
                this._hostWindow.LocationChanged -= this.hostWindow_SizeOrLocationChanged;
                this._hostWindow.SizeChanged -= this.hostWindow_SizeOrLocationChanged;
                this._hostWindow.StateChanged -= this.hostWindow_StateChanged;
                this._hostWindow.Activated -= this.hostWindow_Activated;
                this._hostWindow.Deactivated -= this.hostWindow_Deactivated;
            }
            this.Unloaded -= this.PopupEx_Unloaded;
            this.Opened -= this.PopupEx_Opened;
            this._hostWindow = null;
        }

        private void hostWindow_StateChanged(object sender, EventArgs e)
        {
            if (this._hostWindow != null && this._hostWindow.WindowState != WindowState.Minimized)
            {
                // special handling for validation popup
                var holder = this.PlacementTarget is FrameworkElement target ? target.DataContext as AdornedElementPlaceholder : null;
                if (holder?.AdornedElement != null)
                {
                    this.PopupAnimation = PopupAnimation.None;
                    this.IsOpen = false;
                    var errorTemplate = holder.AdornedElement.GetValue(Validation.ErrorTemplateProperty);
                    holder.AdornedElement.SetValue(Validation.ErrorTemplateProperty, null);
                    holder.AdornedElement.SetValue(Validation.ErrorTemplateProperty, errorTemplate);
                }
            }
        }

        private void hostWindow_SizeOrLocationChanged(object sender, EventArgs e)
        {
            this.RefreshPosition();
        }

        private void SetTopmostState(bool isTop)
        {
            // Don’t apply state if it’s the same as incoming state
            if (this._appliedTopMost.HasValue && this._appliedTopMost == isTop)
            {
                return;
            }

            if (this.Child == null)
            {
                return;
            }

            if (!((PresentationSource.FromVisual(this.Child)) is HwndSource hwndSource))
            {
                return;
            }
            var hwnd = hwndSource.Handle;

            if (!GetWindowRect(hwnd, out Rect rect))
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
                SetWindowPos(hwnd, HwndTopmost, left, top, width, height, Swp.Topmost);
            }
            else
            {
                // Z-Order would only get refreshed/reflected if clicking the
                // the titlebar (as opposed to other parts of the external
                // window) unless I first set the popup to HWND_BOTTOM
                // then HWND_TOP before HWND_NOTOPMOST
                SetWindowPos(hwnd, HwndBottom, left, top, width, height, Swp.Topmost);
                SetWindowPos(hwnd, HwndTop, left, top, width, height, Swp.Topmost);
                SetWindowPos(hwnd, HwndNotopmost, left, top, width, height, Swp.Topmost);
            }

            this._appliedTopMost = isTop;
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (this.CloseOnMouseLeftButtonDown)
            {
                this.IsOpen = false;
            }
        }

        private Window _hostWindow;
        private bool? _appliedTopMost;
        static readonly IntPtr HwndTopmost = new IntPtr(-1);
        static readonly IntPtr HwndNotopmost = new IntPtr(-2);
        static readonly IntPtr HwndTop = new IntPtr(0);
        static readonly IntPtr HwndBottom = new IntPtr(1);

        /// <summary>
        /// SetWindowPos options
        /// </summary>
        [Flags]
        internal enum Swp
        {
            Asyncwindowpos = 0x4000,
            Defererase = 0x2000,
            Drawframe = 0x0020,
            Framechanged = 0x0020,
            Hidewindow = 0x0080,
            Noactivate = 0x0010,
            Nocopybits = 0x0100,
            Nomove = 0x0002,
            Noownerzorder = 0x0200,
            Noredraw = 0x0008,
            Noreposition = 0x0200,
            Nosendchanging = 0x0400,
            Nosize = 0x0001,
            Nozorder = 0x0004,
            Showwindow = 0x0040,
            Topmost = Noactivate | Noownerzorder | Nosize | Nomove | Noredraw | Nosendchanging,
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static int Loword(int i)
        {
            return (short)(i & 0xFFFF);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Size
        {
            public int cx;
            public int cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            private int _left;
            private int _top;
            private int _right;
            private int _bottom;

/*
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void Offset(int dx, int dy)
            {
                this._left += dx;
                this._top += dy;
                this._right += dx;
                this._bottom += dy;
            }
*/

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public int Left
            {
                get => this._left;
                set => this._left = value;
            }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public int Right
            {
                get => this._right;
                set => this._right = value;
            }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public int Top
            {
                get => this._top;
                set => this._top = value;
            }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public int Bottom
            {
                get => this._bottom;
                set => this._bottom = value;
            }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public int Width => this._right - this._left;

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public int Height => this._bottom - this._top;

/*
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            private Point Position => new Point { x = this._left, y = this._top };
*/

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public Size Size => new Size { cx = this.Width, cy = this.Height };

            public static Rect Union(Rect rect1, Rect rect2)
            {
                return new Rect {
                    Left = Math.Min(rect1.Left, rect2.Left),
                    Top = Math.Min(rect1.Top, rect2.Top),
                    Right = Math.Max(rect1.Right, rect2.Right),
                    Bottom = Math.Max(rect1.Bottom, rect2.Bottom),
                };
            }

            public override bool Equals(object obj)
            {
                try
                {
                    if (obj != null)
                    {
                        var rc = (Rect)obj;
                        return rc._bottom == this._bottom
                               && rc._left == this._left
                               && rc._right == this._right
                               && rc._top == this._top;
                    }
                }
                catch (InvalidCastException)
                {
                    return false;
                }

                return false;
            }

            public override int GetHashCode()
            {
                return (this._left << 16 | Loword(this._right)) ^ (this._top << 16 | Loword(this._bottom));
            }
        }

        [SecurityCritical]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DllImport("user32.dll", EntryPoint = "GetWindowRect", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        [SecurityCritical]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, Swp uFlags);

        [SecurityCritical]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static void SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, Swp uFlags)
        {
            _SetWindowPos(hWnd, hWndInsertAfter, x, y, cx, cy, uFlags);
        }
    }
}