#pragma warning disable 618
namespace ControlzEx.Controls
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Threading;
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
        
        private IntPtr windowHandle;
        private IntPtr ownerWindowHandle;
        private bool closing;
        private HwndSource hwndSource;
        private PropertyChangeNotifier resizeModeChangeNotifier;

        private readonly Window owner;

        #region PInvoke
        
        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, IDC_SIZE_CURSORS cursor);

        [DllImport("user32.dll")]
        private static extern IntPtr SetCursor(IntPtr cursor);

        private enum IDC_SIZE_CURSORS 
        {
            SIZENWSE = 32642,
            SIZENESW = 32643,
            SIZEWE = 32644,
            SIZENS = 32645,
        }

        #endregion

        static GlowWindow()
        {
            AllowsTransparencyProperty.OverrideMetadata(typeof(GlowWindow), new FrameworkPropertyMetadata(true));
            BackgroundProperty.OverrideMetadata(typeof(GlowWindow), new FrameworkPropertyMetadata(Brushes.Transparent));
            ResizeModeProperty.OverrideMetadata(typeof(GlowWindow), new FrameworkPropertyMetadata(ResizeMode.NoResize));
            ShowActivatedProperty.OverrideMetadata(typeof(GlowWindow), new FrameworkPropertyMetadata(false));
            ShowInTaskbarProperty.OverrideMetadata(typeof(GlowWindow), new FrameworkPropertyMetadata(false));
            SnapsToDevicePixelsProperty.OverrideMetadata(typeof(GlowWindow), new FrameworkPropertyMetadata(true));
            WindowStyleProperty.OverrideMetadata(typeof(GlowWindow), new FrameworkPropertyMetadata(WindowStyle.None));
        }

        public GlowWindow(Window owner, GlowWindowBehavior behavior, GlowDirection direction)
        {
            this.InitializeComponent();

            this.Title = $"GlowWindow_{direction}";
            this.Name = this.Title;

            this.Owner = owner;
            this.owner = owner;

            this.IsGlowing = true;
            this.Closing += (sender, e) => e.Cancel = !this.closing;


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
                    this.getLeft = rect => rect.Left - this.ResizeBorderThickness.Left + 1;
                    this.getTop = rect => rect.Top - this.ResizeBorderThickness.Top / 2; 
                    this.getWidth = rect => this.ResizeBorderThickness.Left;
                    this.getHeight = rect => rect.Height + this.ResizeBorderThickness.Top; 
                    this.getHitTestValue = (p, rect) => new Rect(0, 0, rect.Width, this.ResizeBorderThickness.Top * 2).Contains(p)
                        ? HT.TOPLEFT
                        : new Rect(0, rect.Height - this.ResizeBorderThickness.Bottom, rect.Width, this.ResizeBorderThickness.Bottom * 2).Contains(p)
                            ? HT.BOTTOMLEFT
                            : HT.LEFT;
                    break;

                case GlowDirection.Right:
                    this.glow.Orientation = Orientation.Vertical;
                    this.glow.HorizontalAlignment = HorizontalAlignment.Left;
                    this.getLeft = rect => rect.Right - 1;
                    this.getTop = rect => rect.Top - this.ResizeBorderThickness.Top / 2; 
                    this.getWidth = rect => this.ResizeBorderThickness.Right;
                    this.getHeight = rect => rect.Height + this.ResizeBorderThickness.Top; 
                    this.getHitTestValue = (p, rect) => new Rect(0, 0, rect.Width, this.ResizeBorderThickness.Top * 2).Contains(p)
                        ? HT.TOPRIGHT
                        : new Rect(0, rect.Height - this.ResizeBorderThickness.Bottom, rect.Width, this.ResizeBorderThickness.Bottom * 2).Contains(p)
                            ? HT.BOTTOMRIGHT
                            : HT.RIGHT;
                    break;

                case GlowDirection.Top:
                    this.PreviewMouseDoubleClick += (sender, e) =>
                        {
                            if (this.IsOwnerHandleValid())
                            {
                                NativeMethods.SendMessage(this.ownerWindowHandle, WM.NCLBUTTONDBLCLK, (IntPtr)HT.TOP, IntPtr.Zero);
                            }
                        };
                    this.glow.Orientation = Orientation.Horizontal;
                    this.glow.VerticalAlignment = VerticalAlignment.Bottom;
                    this.getLeft = rect => rect.Left - this.ResizeBorderThickness.Left / 2; 
                    this.getTop = rect => rect.Top - this.ResizeBorderThickness.Top + 1;
                    this.getWidth = rect => rect.Width + this.ResizeBorderThickness.Left; 
                    this.getHeight = rect => this.ResizeBorderThickness.Top;
                    this.getHitTestValue = (p, rect) => new Rect(0, 0, this.ResizeBorderThickness.Left * 2, rect.Height).Contains(p)
                        ? HT.TOPLEFT
                        : new Rect(rect.Width - this.ResizeBorderThickness.Right, 0, this.ResizeBorderThickness.Right * 2, rect.Height).Contains(p)
                            ? HT.TOPRIGHT
                            : HT.TOP;
                    break;

                case GlowDirection.Bottom:
                    this.PreviewMouseDoubleClick += (sender, e) =>
                        {
                            if (this.IsOwnerHandleValid())
                            {
                                NativeMethods.SendMessage(this.ownerWindowHandle, WM.NCLBUTTONDBLCLK, (IntPtr)HT.BOTTOM, IntPtr.Zero);
                            }
                        };
                    this.glow.Orientation = Orientation.Horizontal;
                    this.glow.VerticalAlignment = VerticalAlignment.Top;
                    this.getLeft = rect => rect.Left - this.ResizeBorderThickness.Left / 2; 
                    this.getTop = rect => rect.Bottom - 1;
                    this.getWidth = rect => rect.Width + this.ResizeBorderThickness.Left; 
                    this.getHeight = rect => this.ResizeBorderThickness.Bottom;
                    this.getHitTestValue = (p, rect) => new Rect(0, 0, this.ResizeBorderThickness.Left * 2, rect.Height).Contains(p)
                        ? HT.BOTTOMLEFT
                        : new Rect(rect.Width - this.ResizeBorderThickness.Right, 0, this.ResizeBorderThickness.Right * 2, rect.Height).Contains(p)
                            ? HT.BOTTOMRIGHT
                            : HT.BOTTOM;
                    break;
            }

            owner.Activated += (sender, e) =>
                {
                    this.Update();

                    this.glow.IsGlow = true;
                };
            owner.Deactivated += (sender, e) =>
                {
                    this.glow.IsGlow = false;
                };
            owner.IsVisibleChanged += (sender, e) => this.Update();
            owner.Closed += (sender, e) => this.InternalClose();
        }

        public bool IsGlowing { set; get; }

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

            this.OpacityStoryboard = this.TryFindResource("ControlzEx.GlowWindow.OpacityStoryboard") as Storyboard;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            this.hwndSource = (HwndSource)PresentationSource.FromVisual(this);

            if (this.hwndSource is null)
            {
                return;
            }

            this.windowHandle = this.hwndSource.Handle;
            this.ownerWindowHandle = new WindowInteropHelper(this.owner).Handle;

            var ws = NativeMethods.GetWindowStyle(this.windowHandle);
            var wsex = NativeMethods.GetWindowStyleEx(this.windowHandle);

            ws |= WS.POPUP;

            wsex &= ~WS_EX.APPWINDOW;
            wsex |= WS_EX.TOOLWINDOW;
            wsex |= WS_EX.NOACTIVATE;

            if (this.owner.ResizeMode == ResizeMode.NoResize || this.owner.ResizeMode == ResizeMode.CanMinimize)
            {
                wsex |= WS_EX.TRANSPARENT;
            }

            NativeMethods.SetWindowStyle(this.windowHandle, ws);
            NativeMethods.SetWindowStyleEx(this.windowHandle, wsex);

            this.hwndSource.AddHook(this.WndProc);

            this.resizeModeChangeNotifier = new PropertyChangeNotifier(this.owner, ResizeModeProperty);
            this.resizeModeChangeNotifier.ValueChanged += this.ResizeModeChanged;
        }

        private void ResizeModeChanged(object sender, EventArgs e)
        {
            var wsex = NativeMethods.GetWindowStyleEx(this.windowHandle);

            if (this.owner.ResizeMode == ResizeMode.NoResize || this.owner.ResizeMode == ResizeMode.CanMinimize)
            {
                wsex |= WS_EX.TRANSPARENT;
            }
            else
            {
                wsex &= ~WS_EX.TRANSPARENT;
            }

            NativeMethods.SetWindowStyleEx(this.windowHandle, wsex);
        }

        public void Update()
        {
            if (this.closing
                || this.CanUpdateCore() == false)
            {
                return;
            }

            RECT rect;
            if (this.owner.Visibility == Visibility.Hidden)
            {
                this.Invoke(() => 
                            { 
                                this.glow.Visibility = Visibility.Collapsed;
                                this.Visibility = Visibility.Collapsed;
                            });

                if (this.IsGlowing 
                    && UnsafeNativeMethods.GetWindowRect(this.ownerWindowHandle, out rect))
                {
                    this.UpdateCore(rect);
                }
            }
            else if (this.owner.WindowState == WindowState.Normal)
            {
                var newVisibility = this.IsGlowing
                                        ? Visibility.Visible
                                        : Visibility.Collapsed;

                this.Invoke(() =>
                            {
                                this.glow.Visibility = newVisibility;
                                this.Visibility = newVisibility;
                            });

                
                if (this.IsGlowing 
                    && UnsafeNativeMethods.GetWindowRect(this.ownerWindowHandle, out rect))
                {
                    this.UpdateCore(rect);                    
                }
            }
            else
            {
                this.Invoke(() =>
                            {
                                this.glow.Visibility = Visibility.Collapsed;
                                this.Visibility = Visibility.Collapsed;
                            });                
            }
        }

        private bool IsWindowHandleValid()
        {
            return this.windowHandle != IntPtr.Zero
                   && NativeMethods.IsWindow(this.windowHandle);
        }

        private bool IsOwnerHandleValid()
        {
            return this.ownerWindowHandle != IntPtr.Zero 
                   && NativeMethods.IsWindow(this.ownerWindowHandle);
        }

        internal bool CanUpdateCore()
        {
            return this.IsWindowHandleValid()
                && this.IsOwnerHandleValid();
        }

        internal void UpdateCore(RECT rect)
        {
            // we can handle this._owner.WindowState == WindowState.Normal
            // or use NOZORDER too
            NativeMethods.SetWindowPos(this.windowHandle, this.ownerWindowHandle, 
                                       (int)this.getLeft(rect),
                                       (int)this.getTop(rect),
                                       (int)this.getWidth(rect),
                                       (int)this.getHeight(rect),
                                       SWP.NOACTIVATE | SWP.NOZORDER);
        }

        internal void InternalClose()
        {
            this.closing = true;

            if (this.resizeModeChangeNotifier != null)
            {
                this.resizeModeChangeNotifier.ValueChanged -= this.ResizeModeChanged;
                this.resizeModeChangeNotifier.Dispose();
            }

            if (this.hwndSource != null)
            {
                this.hwndSource.RemoveHook(this.WndProc);
                this.hwndSource.Dispose();
            }

            this.Close();
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
                    else if (this.CanUpdateCore())
                    {
                        // this fixes #58
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(FixWindowZOrder));
                    }
                    break;

                case WM.ACTIVATE:
                    if (wParam.ToInt32() != 0
                        && this.IsOwnerHandleValid())
                    {
                        handled = true;
                        // We have to activate the owner async. Otherwise the active window on the taskbar is wrong.
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => NativeMethods.SetActiveWindow(this.ownerWindowHandle)));
                    }
                    break;

                case WM.MOUSEACTIVATE:
                    handled = true;
                    if (this.IsOwnerHandleValid())
                    {
                        NativeMethods.SendMessage(this.ownerWindowHandle, WM.ACTIVATE, wParam, lParam);
                    }
                    return new IntPtr(3) /* MA_NOACTIVATE */;

                case WM.NCLBUTTONDOWN:
                case WM.NCLBUTTONDBLCLK:
                case WM.NCRBUTTONDOWN:
                case WM.NCRBUTTONDBLCLK:
                case WM.NCMBUTTONDOWN:
                case WM.NCMBUTTONDBLCLK:
                case WM.NCXBUTTONDOWN:
                case WM.NCXBUTTONDBLCLK:
                    if (this.IsOwnerHandleValid())
                    {
                        // WA_CLICKACTIVE = 2
                        NativeMethods.SendMessage(this.ownerWindowHandle, WM.ACTIVATE, new IntPtr(2), IntPtr.Zero);
                        // Forward message to owner
                        NativeMethods.PostMessage(this.ownerWindowHandle, (WM)msg, wParam, IntPtr.Zero);
                    }
                    break;

                case WM.NCHITTEST:
                    if (this.owner.ResizeMode == ResizeMode.CanResize 
                        || this.owner.ResizeMode == ResizeMode.CanResizeWithGrip)
                    {
                        if (this.IsOwnerHandleValid() 
                            && UnsafeNativeMethods.GetWindowRect(this.ownerWindowHandle, out var rect))
                        {
                            if (NativeMethods.TryGetRelativeMousePosition(this.windowHandle, out var pt))
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
                            SetCursor(LoadCursor(IntPtr.Zero, IDC_SIZE_CURSORS.SIZEWE));
                            break;

                        case HT.TOP:
                        case HT.BOTTOM:
                            handled = true;
                            SetCursor(LoadCursor(IntPtr.Zero, IDC_SIZE_CURSORS.SIZENS));
                            break;

                        case HT.TOPLEFT:
                        case HT.BOTTOMRIGHT:
                            handled = true;
                            SetCursor(LoadCursor(IntPtr.Zero, IDC_SIZE_CURSORS.SIZENWSE));
                            break;

                        case HT.TOPRIGHT:             
                        case HT.BOTTOMLEFT:                       
                            handled = true;
                            SetCursor(LoadCursor(IntPtr.Zero, IDC_SIZE_CURSORS.SIZENESW));
                            break;
                    }
                    break;
            }

            return IntPtr.Zero;

            void FixWindowZOrder()
            {
                if (this.CanUpdateCore() == false)
                {
                    return;
                }

                NativeMethods.SetWindowPos(this.windowHandle, this.ownerWindowHandle, 0, 0, 0, 0, SWP.NOMOVE | SWP.NOSIZE | SWP.NOACTIVATE);
            }
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

        private void InvokeAsync(DispatcherPriority dispatcherPriority, Action invokeAction)
        {
            this.Dispatcher.BeginInvoke(dispatcherPriority, invokeAction);
        }
    }
}