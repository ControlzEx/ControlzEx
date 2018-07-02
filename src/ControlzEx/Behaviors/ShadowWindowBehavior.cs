namespace ControlzEx.Behaviors
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interactivity;
    using System.Windows.Interop;
    using System.Windows.Threading;
    using ControlzEx.Controls;
    using ControlzEx.Native;
    using ControlzEx.Standard;

    public class ShadowWindowBehavior : Behavior<Window>
    {
        private ShadowWindow shadowWindow;
        private IntPtr handle;
        private HwndSource hwndSource;

        public static readonly DependencyProperty ResizeBorderThicknessProperty = DependencyProperty.Register(
            nameof(ResizeBorderThickness),
            typeof(Thickness),
            typeof(ShadowWindowBehavior),
            new PropertyMetadata(GetDefaultResizeBorderThickness()));

        public Thickness ResizeBorderThickness
        {
            get { return (Thickness)GetValue(ResizeBorderThicknessProperty); }
            set { SetValue(ResizeBorderThicknessProperty, value); }
        }

        /// <inheritdoc />
        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.SourceInitialized += this.AssociatedObjectSourceInitialized;
            this.AssociatedObject.Loaded += this.AssociatedObjectOnLoaded;

            if (this.AssociatedObject.IsLoaded)
            {
                this.AssociatedObjectOnLoaded(this.AssociatedObject, new RoutedEventArgs());
                this.AssociatedObjectSourceInitialized(this.AssociatedObject, EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            this.AssociatedObject.SourceInitialized -= this.AssociatedObjectSourceInitialized;
            this.AssociatedObject.Loaded -= this.AssociatedObjectOnLoaded;

            this.hwndSource?.RemoveHook(this.AssociatedObjectWindowProc);

            this.AssociatedObject.StateChanged -= this.AssociatedObjectStateChanged;

            this.Close();

            base.OnDetaching();
        }

        private static Thickness GetDefaultResizeBorderThickness()
        {
#if NET45 || NET462
            return SystemParameters.WindowResizeBorderThickness;
#else
            return ControlzEx.Windows.Shell.SystemParameters2.Current.WindowResizeBorderThickness;
#endif
        }

        private void AssociatedObjectSourceInitialized(object sender, EventArgs e)
        {
            this.handle = new WindowInteropHelper(this.AssociatedObject).Handle;
            this.hwndSource = HwndSource.FromHwnd(this.handle);
            this.hwndSource?.AddHook(this.AssociatedObjectWindowProc);
        }

        private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            this.AssociatedObject.StateChanged -= this.AssociatedObjectStateChanged;
            this.AssociatedObject.StateChanged += this.AssociatedObjectStateChanged;

            this.shadowWindow = new ShadowWindow(this.AssociatedObject);
            this.shadowWindow.Show();

            this.Update();

            this.AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => this.SetOpacityTo(1)));
        }

        private void AssociatedObjectStateChanged(object sender, EventArgs e)
        {
            this.Update();
        }

#pragma warning disable 618
        private WINDOWPOS prevWindowPos;

        private IntPtr AssociatedObjectWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (this.hwndSource?.RootVisual is null)
            {
                return IntPtr.Zero;
            }

            switch ((WM)msg)
            {
                case WM.WINDOWPOSCHANGED:
                case WM.WINDOWPOSCHANGING:
                    Assert.IsNotDefault(lParam);
                    var wp = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
                    if (wp.Equals(this.prevWindowPos) == false)
                    {
                        this.UpdateCore();
                        this.prevWindowPos = wp;
                    }
                    break;

                case WM.SIZE:
                case WM.SIZING:
                    this.UpdateCore();
                    break;
            }
            return IntPtr.Zero;
        }

        private void UpdateCore()
        {
            var canUpdateCore = this.shadowWindow?.CanUpdateCore() == true;
            if (canUpdateCore)
            {
                if (this.handle != IntPtr.Zero
                    && UnsafeNativeMethods.GetWindowRect(this.handle, out var rect))
                {
                    this.shadowWindow.UpdateCore(rect);
                }
            }
        }
#pragma warning restore 618

        private void Update()
        {
            this.shadowWindow?.Update();
        }

        private void Close()
        {
            this.shadowWindow?.InternalClose();
        }

        /// <summary>
        /// Sets the opacity of the shadow window.
        /// </summary>
        private void SetOpacityTo(double newOpacity)
        {
            if (this.shadowWindow != null)
            {
                this.shadowWindow.Opacity = newOpacity;
            }
        }
    }
}
