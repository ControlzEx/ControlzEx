namespace ControlzEx.Behaviors
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Interactivity;
    using System.Windows.Interop;
    using System.Windows.Threading;
    using ControlzEx.Controls;
    using ControlzEx.Native;
    using ControlzEx.Standard;

    public class ShadowWindowBehavior : Behavior<Window>
    {
        private ShadowWindow shadowWindow;
        private ResizeBorderWindow resizeBorderWindow;
        private IntPtr handle;
        private HwndSource hwndSource;
        private PropertyChangeNotifier resizeModeChangeNotifier;

        public static readonly DependencyProperty ResizeBorderThicknessProperty = DependencyProperty.Register(
            nameof(ResizeBorderThickness),
            typeof(Thickness),
            typeof(ShadowWindowBehavior),
            new PropertyMetadata(WindowChromeBehavior.GetDefaultResizeBorderThickness()));

        public Thickness ResizeBorderThickness
        {
            get { return (Thickness)this.GetValue(ResizeBorderThicknessProperty); }
            set { this.SetValue(ResizeBorderThicknessProperty, value); }
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
            this.AssociatedObject.StateChanged -= this.AssociatedObjectStateChanged;

            this.hwndSource?.RemoveHook(this.AssociatedObjectWindowProc);

            if (this.resizeModeChangeNotifier != null)
            {
                this.resizeModeChangeNotifier.ValueChanged -= this.ResizeModeChanged;
                this.resizeModeChangeNotifier.Dispose();
            }

            this.DestroyResizeBorder();

            this.Close();

            base.OnDetaching();
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

            this.resizeModeChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Window.ResizeModeProperty);
            this.resizeModeChangeNotifier.ValueChanged += this.ResizeModeChanged;

            this.shadowWindow = new ShadowWindow(this.AssociatedObject);
            this.shadowWindow.Show();

            if (this.AssociatedObject.ResizeMode == ResizeMode.CanResize
                || this.AssociatedObject.ResizeMode == ResizeMode.CanResizeWithGrip)
            {
                this.resizeBorderWindow = this.CreateResizeBorder();
                this.resizeBorderWindow.Show();
            }

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
            var canUpdateShadowCore = this.shadowWindow?.CanUpdateCore() == true;
            var canUpdateResizeBorder = this.resizeBorderWindow?.CanUpdateCore() == true;

            if (canUpdateShadowCore)
            {
                if (this.handle != IntPtr.Zero
                    && UnsafeNativeMethods.GetWindowRect(this.handle, out var rect))
                {
                    this.shadowWindow.UpdateCore(rect);
                    if (canUpdateResizeBorder)
                    {
                        this.resizeBorderWindow.UpdateCore(rect);
                    }
                }
            }
        }
#pragma warning restore 618

        private void Update()
        {
            this.shadowWindow?.Update();
            this.resizeBorderWindow?.Update();
        }

        private void Close()
        {
            this.shadowWindow?.InternalClose();
            this.DestroyResizeBorder();
        }

        private void DestroyResizeBorder()
        {
            if (this.resizeBorderWindow != null)
            {
                this.resizeBorderWindow.InternalClose();
                this.resizeBorderWindow = null;
            }
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

        private void ResizeModeChanged(object sender, EventArgs e)
        {
            if (this.AssociatedObject.ResizeMode == ResizeMode.NoResize
                || this.AssociatedObject.ResizeMode == ResizeMode.CanMinimize)
            {
                this.DestroyResizeBorder();
            }
            else
            {
                this.resizeBorderWindow = this.CreateResizeBorder();
                this.resizeBorderWindow.Show();
                this.resizeBorderWindow.Update();
            }
        }

        private ResizeBorderWindow CreateResizeBorder()
        {
            var resizeBorder = new ResizeBorderWindow(this.AssociatedObject);
            var resizeBorderThicknessBinding = new Binding
            {
                Path = new PropertyPath(ResizeBorderThicknessProperty),
                Mode = BindingMode.OneWay,
                Source = this
            };
            resizeBorder.SetBinding(ResizeBorderWindow.ResizeBorderThicknessProperty, resizeBorderThicknessBinding);
            return resizeBorder;
        }
    }
}
