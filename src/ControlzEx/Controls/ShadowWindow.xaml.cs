#pragma warning disable 618
namespace ControlzEx.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Interop;
    using ControlzEx.Native;
    using ControlzEx.Standard;
    using JetBrains.Annotations;

    /// <summary>
    /// Interaction logic for ShadowWindow.xaml
    /// </summary>
    internal partial class ShadowWindow
    {
        private readonly Func<RECT, int> getLeft = rect => rect.Left - 17;
        private readonly Func<RECT, int> getTop = rect => rect.Top - 16;
        private readonly Func<RECT, int> getWidth = rect => rect.Width + 38;
        private readonly Func<RECT, int> getHeight = rect => rect.Height + 38;

        private IntPtr windowHandle;
        private IntPtr ownerWindowHandle;
        private HwndSource hwndSource;
        private bool closing;

        internal ShadowWindow(Window owner)
        {
            this.InitializeComponent();

            this.Owner = owner;

            this.AllowsTransparency = true;
            this.ShowActivated = false;
            this.ShowInTaskbar = false;
            this.UseLayoutRounding = true;
            this.Background = null;

            this.Closing += (sender, e) => e.Cancel = !this.closing;

            owner.Activated += this.OnOwnerActivated;
            owner.Deactivated += this.OnOwnerDeactivated;
            owner.StateChanged += this.OnOwnerStateChanged;
            owner.IsVisibleChanged += this.OnOwnerIsVisibleChanged;
            owner.Closed += this.OnOwnerClosed;
        }

        internal void Update()
        {
            if (this.closing)
            {
                return;
            }

            if (this.Owner.WindowState == WindowState.Normal)
            {
                this.Invoke(() => this.Visibility = Visibility.Visible);

                if (this.ownerWindowHandle != IntPtr.Zero
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

            var owner = this.Owner;
            if (owner != null)
            {
                owner.Activated -= this.OnOwnerActivated;
                owner.Deactivated -= this.OnOwnerDeactivated;
                owner.StateChanged -= this.OnOwnerStateChanged;
                owner.IsVisibleChanged -= this.OnOwnerIsVisibleChanged;
                owner.Closing -= this.OnOwnerClosed;
            }

            this.hwndSource?.Dispose();
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
            this.ownerWindowHandle = new WindowInteropHelper(this.Owner).Handle;

            var ws = NativeMethods.GetWindowStyle(this.windowHandle);
            var wsex = NativeMethods.GetWindowStyleEx(this.windowHandle);

            ws |= WS.POPUP;

            wsex &= ~WS_EX.APPWINDOW;
            wsex |= WS_EX.TOOLWINDOW;
            wsex |= WS_EX.TRANSPARENT;

            NativeMethods.SetWindowStyle(this.windowHandle, ws);
            NativeMethods.SetWindowStyleEx(this.windowHandle, wsex);
        }

        private void OnOwnerActivated(object sender, EventArgs e)
        {
            this.Invoke(() => this.Opacity = 1.0);
        }

        private void OnOwnerDeactivated(object sender, EventArgs args)
        {
            this.Invoke(() => this.Opacity = 0.7);
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
