#pragma warning disable 618
namespace ControlzEx.Controls
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using ControlzEx.Behaviors;
    using ControlzEx.Native;
    using ControlzEx.Standard;
    using JetBrains.Annotations;

    /// <summary>
    /// Interaction logic for ResizeBorderWindow.xaml
    /// </summary>
    internal partial class ResizeBorderWindow
    {
        internal static readonly DependencyProperty ResizeBorderThicknessProperty = DependencyProperty.Register(
            nameof(ResizeBorderThickness),
            typeof(Thickness),
            typeof(ResizeBorderWindow),
            new PropertyMetadata(WindowChromeBehavior.GetDefaultResizeBorderThickness(), OnResizeBorderThicknessChanged));

        private readonly Window owner;

        private readonly Func<RECT, int> getLeft;
        private readonly Func<RECT, int> getTop;
        private readonly Func<RECT, int> getWidth;
        private readonly Func<RECT, int> getHeight;

        private IntPtr windowHandle;
        private IntPtr ownerWindowHandle;
        private HwndSource hwndSource;
        private bool closing;

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

        internal ResizeBorderWindow([NotNull] Window owner)
        {
            this.InitializeComponent();

            this.Owner = owner;
            this.owner = owner;

            this.AllowsTransparency = true;
            this.ShowActivated = false;
            this.ShowInTaskbar = false;
            this.UseLayoutRounding = true;
            this.Background = null;

            this.Closing += (sender, e) => e.Cancel = !this.closing;

            this.getLeft = rect => (int)(rect.Left - this.ResizeBorderThickness.Left);
            this.getTop = rect => (int)(rect.Top - this.ResizeBorderThickness.Top);
            this.getWidth = rect => (int)(rect.Width + this.ResizeBorderThickness.Left + this.ResizeBorderThickness.Right);
            this.getHeight = rect => (int)(rect.Height + this.ResizeBorderThickness.Top + this.ResizeBorderThickness.Bottom);

            owner.StateChanged += this.OnOwnerStateChanged;
            owner.IsVisibleChanged += this.OnOwnerIsVisibleChanged;
            owner.Closed += this.OnOwnerClosed;
        }

        internal Thickness ResizeBorderThickness
        {
            get { return (Thickness)this.GetValue(ResizeBorderThicknessProperty); }
            set { this.SetValue(ResizeBorderThicknessProperty, value); }
        }

        internal void Update()
        {
            if (this.closing)
            {
                return;
            }

            if (this.owner.WindowState == WindowState.Normal)
            {
                this.Invoke(() => this.Visibility = Visibility.Visible);

                if (this.CanUpdateCore()
                    && UnsafeNativeMethods.GetWindowRect(this.ownerWindowHandle, out var rect))
                {
                    this.UpdateCore(rect);
                }
            }
            else
            {
                this.Invoke(() => this.Visibility = Visibility.Collapsed);
            }
        }

        internal bool CanUpdateCore()
        {
            return this.ownerWindowHandle != IntPtr.Zero
                   && this.windowHandle != IntPtr.Zero;
        }

        internal void UpdateCore(RECT rect)
        {
            NativeMethods.SetWindowPos(this.windowHandle, this.ownerWindowHandle,
                this.getLeft(rect),
                this.getTop(rect),
                this.getWidth(rect),
                this.getHeight(rect),
                SWP.NOACTIVATE | SWP.NOZORDER);
        }

        internal void InternalClose()
        {
            this.closing = true;

            this.owner.StateChanged -= this.OnOwnerStateChanged;
            this.owner.IsVisibleChanged -= this.OnOwnerIsVisibleChanged;
            this.owner.Closing -= this.OnOwnerClosed;

            if (this.hwndSource != null)
            {
                this.hwndSource.RemoveHook(this.WndProc);
                this.hwndSource.Dispose();
            }

            this.Close();
        }

        /// <inheritdoc />
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

            NativeMethods.SetWindowStyle(this.windowHandle, ws);
            NativeMethods.SetWindowStyleEx(this.windowHandle, wsex);

            this.hwndSource.AddHook(this.WndProc);
        }

        private static void OnResizeBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var transparentResizeFrame = (ResizeBorderWindow)d;
            transparentResizeFrame.Update();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((WM)msg)
            {
                case WM.MOUSEACTIVATE:
                    handled = true;
                    if (this.ownerWindowHandle != IntPtr.Zero)
                    {
                        NativeMethods.SendMessage(this.ownerWindowHandle, WM.ACTIVATE, wParam, lParam);
                    }
                    return new IntPtr(3);

                case WM.NCLBUTTONDOWN:
                case WM.NCLBUTTONDBLCLK:
                case WM.NCRBUTTONDOWN:
                case WM.NCRBUTTONDBLCLK:
                case WM.NCMBUTTONDOWN:
                case WM.NCMBUTTONDBLCLK:
                case WM.NCXBUTTONDOWN:
                case WM.NCXBUTTONDBLCLK:
                    if (this.ownerWindowHandle != IntPtr.Zero)
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
                        if (this.ownerWindowHandle != IntPtr.Zero && UnsafeNativeMethods.GetWindowRect(this.ownerWindowHandle, out var rect))
                        {
                            if (NativeMethods.TryGetRelativeMousePosition(this.windowHandle, out var pt))
                            {
                                var hitTestValue = this.GetHitTestValue(pt, rect);
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
        }

        private HT GetHitTestValue(Point point, RECT rect)
        {
            if (point.X < this.ResizeBorderThickness.Left * 2.0 && point.Y < this.ResizeBorderThickness.Top * 2.0)
            {
                return HT.TOPLEFT;
            }

            if (point.X < this.ResizeBorderThickness.Left * 2.0 && point.Y > rect.Height - this.ResizeBorderThickness.Bottom)
            {
                return HT.BOTTOMLEFT;
            }

            if (point.X < this.ResizeBorderThickness.Left)
            {
                return HT.LEFT;
            }

            if (point.X > rect.Width - this.ResizeBorderThickness.Right && point.Y < this.ResizeBorderThickness.Top * 2.0)
            {
                return HT.TOPRIGHT;
            }

            if (point.X > rect.Width - this.ResizeBorderThickness.Right && point.Y > rect.Height - this.ResizeBorderThickness.Bottom)
            {
                return HT.BOTTOMRIGHT;
            }

            if (point.X > rect.Width - this.ResizeBorderThickness.Right)
            {
                return HT.RIGHT;
            }

            if (point.Y < this.ResizeBorderThickness.Top)
            {
                return HT.TOP;
            }

            if (point.Y > rect.Height - this.ResizeBorderThickness.Bottom)
            {
                return HT.BOTTOM;
            }

            return HT.TRANSPARENT;
        }

        private void OnOwnerStateChanged(object sender, EventArgs e)
        {
            this.Update();
        }

        private void OnOwnerIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.Update();
        }

        private void OnOwnerClosed(object sender, EventArgs e)
        {
            this.InternalClose();
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
#pragma warning restore 618
