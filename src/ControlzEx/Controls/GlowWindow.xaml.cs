#pragma warning disable 618
namespace ControlzEx.Controls
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Interop;
    using System.Windows.Media.Animation;
    using ControlzEx.Standard;
    using ControlzEx.Native;
    using ControlzEx.Behaviors;
    using JetBrains.Annotations;

    partial class GlowWindow
    {
        private readonly Func<Point, RECT, HT> getHitTestValue;
        private readonly Func<RECT, double> getLeft;
        private readonly Func<RECT, double> getTop;
        private readonly Func<RECT, double> getWidth;
        private readonly Func<RECT, double> getHeight;
        
        private IntPtr handle;
        private IntPtr ownerHandle;
        private bool closing;
        private HwndSource hwndSource;
        private PropertyChangeNotifier resizeModeChangeNotifier;

        private readonly Window owner;

        #region PInvoke
        
        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, IDC_SIZE_CURSORS cursor);

        [DllImport("user32.dll")]
        private static extern IntPtr SetCursor(IntPtr cursor);

        private enum IDC_SIZE_CURSORS {
            IDC_SIZENWSE = 32642,
            IDC_SIZENESW = 32643,
            IDC_SIZEWE = 32644,
            IDC_SIZENS = 32645,
        }

        #endregion

        public GlowWindow(Window owner, GlowWindowBehavior behavior, GlowDirection direction)
        {
            this.InitializeComponent();

            this.Title = $"GlowWindow_{direction}";
            this.Name = this.Title;

            this.Owner = owner;
            this.owner = owner;

            this.IsGlowing = true;
            this.AllowsTransparency = true;
            this.Closing += (sender, e) => e.Cancel = !this.closing;

            this.ShowInTaskbar = false;
            this.glow.Visibility = Visibility.Collapsed;

            this.glow.Direction = direction;

            {
                var b = new Binding
                        {
                            Path = new PropertyPath(nameof(this.ActualWidth)),
                            Source = this,
                            Mode = BindingMode.OneWay
                        };
                this.glow.SetBinding(WidthProperty, b);
            }

            {
                var b = new Binding
                        {
                            Path = new PropertyPath(nameof(this.ActualHeight)),
                            Source = this,
                            Mode = BindingMode.OneWay
                        };
                this.glow.SetBinding(HeightProperty, b);
            }

            {
                var b = new Binding
                        {
                            Path = new PropertyPath(GlowWindowBehavior.GlowBrushProperty),
                            Source = behavior,
                            Mode = BindingMode.OneWay
                        };
                this.glow.SetBinding(Glow.GlowBrushProperty, b);
            }

            {
                var b = new Binding
                    {
                        Path = new PropertyPath(GlowWindowBehavior.NonActiveGlowBrushProperty),
                        Source = behavior,
                        Mode = BindingMode.OneWay
                    };
                this.glow.SetBinding(Glow.NonActiveGlowBrushProperty, b);
            }

            {
                var b = new Binding
                        {
                            Path = new PropertyPath(BorderThicknessProperty),
                            Source = owner,
                            Mode = BindingMode.OneWay
                        };
                this.glow.SetBinding(BorderThicknessProperty, b);
            }

            {
                var b = new Binding
                        {
                            Path = new PropertyPath(GlowWindowBehavior.ResizeBorderThicknessProperty),
                            Source = behavior
                        };
                this.SetBinding(ResizeBorderThicknessProperty, b);
            }            

            switch (direction)
            {
                case GlowDirection.Left:
                    this.glow.Orientation = Orientation.Vertical;
                    this.glow.HorizontalAlignment = HorizontalAlignment.Right;
                    this.getLeft = (rect) => rect.Left - this.ResizeBorderThickness.Left + 1;
                    this.getTop = (rect) => rect.Top - this.ResizeBorderThickness.Top / 2; 
                    this.getWidth = (rect) => this.ResizeBorderThickness.Left;
                    this.getHeight = (rect) => rect.Height + this.ResizeBorderThickness.Top; 
                    this.getHitTestValue = (p, rect) => new Rect(0, 0, rect.Width, this.ResizeBorderThickness.Top * 2).Contains(p)
                        ? HT.TOPLEFT
                        : new Rect(0, rect.Height - this.ResizeBorderThickness.Bottom, rect.Width, this.ResizeBorderThickness.Bottom * 2).Contains(p)
                            ? HT.BOTTOMLEFT
                            : HT.LEFT;
                    break;

                case GlowDirection.Right:
                    this.glow.Orientation = Orientation.Vertical;
                    this.glow.HorizontalAlignment = HorizontalAlignment.Left;
                    this.getLeft = (rect) => rect.Right - 1;
                    this.getTop = (rect) => rect.Top - this.ResizeBorderThickness.Top / 2; 
                    this.getWidth = (rect) => this.ResizeBorderThickness.Right;
                    this.getHeight = (rect) => rect.Height + this.ResizeBorderThickness.Top; 
                    this.getHitTestValue = (p, rect) => new Rect(0, 0, rect.Width, this.ResizeBorderThickness.Top * 2).Contains(p)
                        ? HT.TOPRIGHT
                        : new Rect(0, rect.Height - this.ResizeBorderThickness.Bottom, rect.Width, this.ResizeBorderThickness.Bottom * 2).Contains(p)
                            ? HT.BOTTOMRIGHT
                            : HT.RIGHT;
                    break;

                case GlowDirection.Top:
                    this.PreviewMouseDoubleClick += (sender, e) =>
                        {
                            if (this.ownerHandle != IntPtr.Zero)
                            {
                                NativeMethods.SendMessage(this.ownerHandle, WM.NCLBUTTONDBLCLK, (IntPtr)HT.TOP, IntPtr.Zero);
                            }
                        };
                    this.glow.Orientation = Orientation.Horizontal;
                    this.glow.VerticalAlignment = VerticalAlignment.Bottom;
                    this.getLeft = (rect) => rect.Left - this.ResizeBorderThickness.Left / 2; 
                    this.getTop = (rect) => rect.Top - this.ResizeBorderThickness.Top + 1;
                    this.getWidth = (rect) => rect.Width + this.ResizeBorderThickness.Left; 
                    this.getHeight = (rect) => this.ResizeBorderThickness.Top;
                    this.getHitTestValue = (p, rect) => new Rect(0, 0, this.ResizeBorderThickness.Left * 2, rect.Height).Contains(p)
                        ? HT.TOPLEFT
                        : new Rect(rect.Width - this.ResizeBorderThickness.Right, 0, this.ResizeBorderThickness.Right * 2, rect.Height).Contains(p)
                            ? HT.TOPRIGHT
                            : HT.TOP;
                    break;

                case GlowDirection.Bottom:
                    this.PreviewMouseDoubleClick += (sender, e) =>
                        {
                            if (this.ownerHandle != IntPtr.Zero)
                            {
                                NativeMethods.SendMessage(this.ownerHandle, WM.NCLBUTTONDBLCLK, (IntPtr)HT.BOTTOM, IntPtr.Zero);
                            }
                        };
                    this.glow.Orientation = Orientation.Horizontal;
                    this.glow.VerticalAlignment = VerticalAlignment.Top;
                    this.getLeft = (rect) => rect.Left - this.ResizeBorderThickness.Left / 2; 
                    this.getTop = (rect) => rect.Bottom - 1;
                    this.getWidth = (rect) => rect.Width + this.ResizeBorderThickness.Left; 
                    this.getHeight = (rect) => this.ResizeBorderThickness.Bottom;
                    this.getHitTestValue = (p, rect) => new Rect(0, 0, this.ResizeBorderThickness.Left * 2, rect.Height).Contains(p)
                        ? HT.BOTTOMLEFT
                        : new Rect(rect.Width - this.ResizeBorderThickness.Right, 0, this.ResizeBorderThickness.Right * 2, rect.Height).Contains(p)
                            ? HT.BOTTOMRIGHT
                            : HT.BOTTOM;
                    break;
            }

            owner.ContentRendered += (sender, e) => this.glow.Visibility = Visibility.Visible;
            owner.Activated += (sender, e) =>
                {
                    this.Update();

                    this.glow.IsGlow = true;
                };
            owner.Deactivated += (sender, e) =>
                {
                    this.glow.IsGlow = false;
                };
            owner.StateChanged += (sender, e) => this.Update();
            owner.IsVisibleChanged += (sender, e) => this.Update();
            owner.Closed += (sender, e) =>
                {
                    this.closing = true;
                    this.Close();
                };
        }

        public Storyboard OpacityStoryboard { get; set; }

        public static readonly DependencyProperty ResizeBorderThicknessProperty = DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(GlowWindow), new PropertyMetadata(WindowChromeBehavior.GetDefaultResizeBorderThickness(), OnResizeBorderThicknessChanged));

        public Thickness ResizeBorderThickness
        {
            get { return (Thickness)this.GetValue(ResizeBorderThicknessProperty); }
            set { this.SetValue(ResizeBorderThicknessProperty, value); }
        }

        private static void OnResizeBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var glowWindow = (GlowWindow)d;

            // Add padding to the edges, otherwise the borders/glows overlap too much
            switch (glowWindow.glow.Direction)
            {
                case GlowDirection.Left:
                case GlowDirection.Right:
                    glowWindow.glow.Padding = new Thickness(0, glowWindow.ResizeBorderThickness.Top / 4, 0, glowWindow.ResizeBorderThickness.Bottom / 4);
                    break;

                case GlowDirection.Top:
                case GlowDirection.Bottom:
                    glowWindow.glow.Padding = new Thickness(glowWindow.ResizeBorderThickness.Left / 4, 0, glowWindow.ResizeBorderThickness.Right / 4, 0);
                    break;
            }

            glowWindow.Update();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.OpacityStoryboard = this.TryFindResource("OpacityStoryboard") as Storyboard;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            this.hwndSource = (HwndSource)PresentationSource.FromVisual(this);
            if (this.hwndSource == null) return;

            var ws = NativeMethods.GetWindowStyle(this.hwndSource.Handle);
            var wsex = NativeMethods.GetWindowStyleEx(this.hwndSource.Handle);

            ws |= WS.POPUP;

            wsex &= ~WS_EX.APPWINDOW;
            wsex |= WS_EX.TOOLWINDOW;

            if (this.owner.ResizeMode == ResizeMode.NoResize || this.owner.ResizeMode == ResizeMode.CanMinimize)
            {
                wsex |= WS_EX.TRANSPARENT;
            }

            NativeMethods.SetWindowStyle(this.hwndSource.Handle, ws);
            NativeMethods.SetWindowStyleEx(this.hwndSource.Handle, wsex);
            this.hwndSource.AddHook(this.WndProc);

            this.handle = this.hwndSource.Handle;
            this.ownerHandle = new WindowInteropHelper(this.owner).Handle;

            this.resizeModeChangeNotifier = new PropertyChangeNotifier(this.owner, ResizeModeProperty);
            this.resizeModeChangeNotifier.ValueChanged += this.ResizeModeChanged;
        }

        private void ResizeModeChanged(object sender, EventArgs e)
        {
            var wsex = NativeMethods.GetWindowStyleEx(this.hwndSource.Handle);
            if (this.owner.ResizeMode == ResizeMode.NoResize || this.owner.ResizeMode == ResizeMode.CanMinimize)
            {
                wsex |= WS_EX.TRANSPARENT;
            }
            else
            {
                wsex ^= WS_EX.TRANSPARENT;
            }
            NativeMethods.SetWindowStyleEx(this.hwndSource.Handle, wsex);
        }

        public void Update()
        {
            if (this.closing)
            {
                return;
            }

            RECT rect;
            if (this.owner.Visibility == Visibility.Hidden)
            {
                this.Invoke(() => this.glow.Visibility = Visibility.Collapsed);
                this.Invoke(() => this.Visibility = Visibility.Collapsed);
                //Standard.NativeMethods.ShowWindow(this.handle, Standard.SW.HIDE);
                if (this.IsGlowing && this.ownerHandle != IntPtr.Zero && UnsafeNativeMethods.GetWindowRect(this.ownerHandle, out rect))
                {
                    this.UpdateCore(rect);
                }
            }
            else if (this.owner.WindowState == WindowState.Normal)
            {
                this.Invoke(() => this.glow.Visibility = this.IsGlowing ? Visibility.Visible : Visibility.Collapsed);
                this.Invoke(() => this.Visibility = this.IsGlowing ? Visibility.Visible : Visibility.Collapsed);
//                if (this.IsGlowing)
//                {
//                    Standard.NativeMethods.ShowWindow(this.handle, Standard.SW.SHOWNOACTIVATE);
//                }
//                else
//                {
//                    Standard.NativeMethods.ShowWindow(this.handle, Standard.SW.HIDE);
//                }
                if (this.IsGlowing && this.ownerHandle != IntPtr.Zero && UnsafeNativeMethods.GetWindowRect(this.ownerHandle, out rect))
                {
                    this.UpdateCore(rect);
                }
            }
            else
            {
                this.Invoke(() => this.glow.Visibility = Visibility.Collapsed);
                this.Invoke(() => this.Visibility = Visibility.Collapsed);
                //Standard.NativeMethods.ShowWindow(this.handle, Standard.SW.HIDE);
            }
        }

        public bool IsGlowing { set; get; }

        internal bool CanUpdateCore()
        {
            return this.ownerHandle != IntPtr.Zero && this.handle != IntPtr.Zero;
        }

        internal void UpdateCore(RECT rect)
        {
            // we can handle this._owner.WindowState == WindowState.Normal
            // or use NOZORDER too
            NativeMethods.SetWindowPos(this.handle, this.ownerHandle, 
                                       (int)(this.getLeft(rect)),
                                       (int)(this.getTop(rect)),
                                       (int)(this.getWidth(rect)),
                                       (int)(this.getHeight(rect)),
                                       SWP.NOACTIVATE | SWP.NOZORDER);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((WM)msg)
            {
                case WM.SHOWWINDOW:
                    if ((int)lParam == 3 && this.Visibility != Visibility.Visible) // 3 == SW_PARENTOPENING
                    {
                        handled = true; //handle this message so window isn't shown until we want it to       
                    }
                    break;
                case WM.MOUSEACTIVATE:
                    handled = true;
                    if (this.ownerHandle != IntPtr.Zero)
                    {
                        NativeMethods.SendMessage(this.ownerHandle, WM.ACTIVATE, wParam, lParam);
                    }
                    return new IntPtr(3);
                case WM.NCLBUTTONDOWN:
                    if (this.ownerHandle != IntPtr.Zero)
                    {
                        // Forward message to owner
                        NativeMethods.PostMessage(this.ownerHandle, WM.NCLBUTTONDOWN, wParam, IntPtr.Zero);
                    }
                    break;
                case WM.NCHITTEST:
                    if (this.owner.ResizeMode == ResizeMode.CanResize 
                        || this.owner.ResizeMode == ResizeMode.CanResizeWithGrip)
                    {
                        if (this.ownerHandle != IntPtr.Zero && UnsafeNativeMethods.GetWindowRect(this.ownerHandle, out var rect))
                        {
                            if (NativeMethods.TryGetRelativeMousePosition(this.handle, out var pt))
                            {
                                var hitTestValue = this.getHitTestValue(pt, rect);
                                handled = true;
                                return new IntPtr((int)hitTestValue);
                            }
                        }
                    }
                    break;

                case WM.SETCURSOR:
                    switch ((HT)Utility.LOWORD((int)lParam))
                    {
                        case HT.LEFT:
                        case HT.RIGHT:
                            handled = true;
                            SetCursor(LoadCursor(IntPtr.Zero, IDC_SIZE_CURSORS.IDC_SIZEWE));
                            break;

                        case HT.TOP:
                        case HT.BOTTOM:
                            handled = true;
                            SetCursor(LoadCursor(IntPtr.Zero, IDC_SIZE_CURSORS.IDC_SIZENS));
                            break;

                        case HT.TOPLEFT:
                        case HT.BOTTOMRIGHT:
                            handled = true;
                            SetCursor(LoadCursor(IntPtr.Zero, IDC_SIZE_CURSORS.IDC_SIZENWSE));
                            break;

                        case HT.TOPRIGHT:             
                        case HT.BOTTOMLEFT:                       
                            handled = true;
                            SetCursor(LoadCursor(IntPtr.Zero, IDC_SIZE_CURSORS.IDC_SIZENESW));
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void Invoke([NotNull] Action invokeAction)
        {
            if (this.Dispatcher.CheckAccess())
            {
                invokeAction();
            }
            else
            {
                this.Dispatcher.Invoke(invokeAction);
            }
        }
    }
}