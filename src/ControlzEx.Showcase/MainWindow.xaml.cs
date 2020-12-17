namespace ControlzEx.Showcase
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using ControlzEx.Native;
    using ControlzEx.Standard;

    public partial class MainWindow
    {
        private static readonly PropertyInfo criticalHandlePropertyInfo = typeof(Window).GetProperty("CriticalHandle", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly object[] emptyObjectArray = new object[0];

        public static readonly DependencyProperty BrushesProperty = DependencyProperty.Register(nameof(Brushes), typeof(List<KeyValuePair<string, Brush>>), typeof(MainWindow), new PropertyMetadata(default(List<KeyValuePair<string, Brush>>)));

        public MainWindow()
        {
            this.InitializeComponent();

            this.Brushes = GetBrushes().ToList();
        }

        public List<KeyValuePair<string, Brush>> Brushes
        {
            get => (List<KeyValuePair<string, Brush>>)this.GetValue(BrushesProperty);
            set => this.SetValue(BrushesProperty, value);
        }

        public int LoadedCount { get; set; }

        public static IEnumerable<KeyValuePair<string, Color>> GetColors()
        {
            return typeof(Colors)
                   .GetProperties()
                   .Where(prop => typeof(Color).IsAssignableFrom(prop.PropertyType))
                   .Select(prop => new KeyValuePair<string, Color>(prop.Name, (Color)prop.GetValue(null, null)));
        }

        public static IEnumerable<KeyValuePair<string, Brush>> GetBrushes()
        {
            var brushes = typeof(Brushes)
                          .GetProperties()
                          .Where(prop => typeof(Brush).IsAssignableFrom(prop.PropertyType))
                          .Select(prop => new KeyValuePair<string, Brush>(prop.Name, (Brush)prop.GetValue(null, null)));

            return new[] { new KeyValuePair<string, Brush>("None", null) }.Concat(brushes);
        }

        private void ButtonOpenChildWindowOnClick(object sender, RoutedEventArgs e)
        {
            var window = new MainWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            if (this.SetOwner.IsChecked == true)
            {
                window.Owner = this;
            }

            window.Show();
        }

        private void ButtonOpenModalChildWindowOnClick(object sender, RoutedEventArgs e)
        {
            var window = new MainWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            if (this.SetOwner.IsChecked == true)
            {
                window.Owner = this;
            }

            window.ShowDialog();
        }

        private void ButtonOpenPseudoModalChildWindowOnClick(object sender, RoutedEventArgs e)
        {
            var window = new MainWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Owner = this // for this to work we always have to set the owner
            };

            // We have to use closing, otherwise the owner window won't be activated.
            window.Closing += this.PseudoModalWindow_Closing;

            var ownerHandle = new WindowInteropHelper(window.Owner).Handle;
#pragma warning disable 618
            var windowStyle = NativeMethods.GetWindowStyle(ownerHandle);
            NativeMethods.SetWindowStyle(ownerHandle, windowStyle | WS.DISABLED);
#pragma warning restore 618

            window.Show();
        }

        private void PseudoModalWindow_Closing(object sender, CancelEventArgs e)
        {
            if (e.Cancel)
            {
                return;
            }

            var ownerHandle = new WindowInteropHelper(((Window)sender).Owner).Handle;
#pragma warning disable 618
            var windowStyle = NativeMethods.GetWindowStyle(ownerHandle);
            NativeMethods.SetWindowStyle(ownerHandle, windowStyle & ~WS.DISABLED);
#pragma warning restore 618
        }

        private void ButtonResetMinSizesOnClick(object sender, RoutedEventArgs e)
        {
            this.MinWidth = 80;
            this.MinHeight = 60;
        }

        private void ButtonResetMaxSizesOnClick(object sender, RoutedEventArgs e)
        {
            this.MaxWidth = double.PositiveInfinity;
            this.MaxHeight = double.PositiveInfinity;
        }

        private void ButtonHideOnClick(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void LoadedCountTextBlock_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.LoadedCountTextBlock.Text = $"Load-Count: {++this.LoadedCount}";
        }

        private void LoadedCountTextBlock_OnUnloaded(object sender, RoutedEventArgs e)
        {
            //this.LoadedCountTextBlock.Text = $"Load-Count: {--this.LoadedCount}";
        }

        private void BadgedButtonOnClick(object sender, RoutedEventArgs e)
        {
            this.BadgedButton.Badge = DateTime.Now;
        }

#pragma warning disable 618
        private void TitleBarGrid_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                e.Handled = true;

                // taken from DragMove internal code
                this.VerifyAccess();

                // for the touch usage
                UnsafeNativeMethods.ReleaseCapture();

                var criticalHandle = (IntPtr)criticalHandlePropertyInfo.GetValue(this, emptyObjectArray);

                // these lines are from DragMove
                // NativeMethods.SendMessage(criticalHandle, WM.SYSCOMMAND, (IntPtr)SC.MOUSEMOVE, IntPtr.Zero);
                // NativeMethods.SendMessage(criticalHandle, WM.LBUTTONUP, IntPtr.Zero, IntPtr.Zero);

                var wpfPoint = this.PointToScreen(Mouse.GetPosition(this));
                var x = (int)wpfPoint.X;
                var y = (int)wpfPoint.Y;
                NativeMethods.SendMessage(criticalHandle, WM.NCLBUTTONDOWN, (IntPtr)HT.CAPTION, new IntPtr(x | (y << 16)));
            }
            else if (e.ClickCount == 2
                     && this.ResizeMode != ResizeMode.NoResize)
            {
                e.Handled = true;

                if (this.WindowState == WindowState.Normal
                    && this.ResizeMode != ResizeMode.NoResize
                    && this.ResizeMode != ResizeMode.CanMinimize)
                {
                    SystemCommands.MaximizeWindow(this);
                }
                else
                {
                    SystemCommands.RestoreWindow(this);
                }
            }
        }

        private void TitleBarGrid_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Windows.Shell.SystemCommands.ShowSystemMenu(this, e);
        }

        private void ButtonMinimizeOnClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void ButtonMaximizeOnClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        private void ButtonRestoreOnClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void RestoreOrMaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

#pragma warning restore 618
    }
}