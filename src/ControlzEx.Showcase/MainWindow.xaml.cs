namespace ControlzEx.Showcase
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using ControlzEx.Native;
    using ControlzEx.Standard;

    public partial class MainWindow : WindowChromeWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();

            this.Brushes = GetBrushes().ToList();
        }

        public static readonly DependencyProperty BrushesProperty = DependencyProperty.Register(nameof(Brushes), typeof(List<KeyValuePair<string, Brush>>), typeof(MainWindow), new PropertyMetadata(default(List<KeyValuePair<string, Brush>>)));

        public List<KeyValuePair<string, Brush>> Brushes
        {
            get { return (List<KeyValuePair<string, Brush>>)this.GetValue(BrushesProperty); }
            set { this.SetValue(BrushesProperty, value); }
        }

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

        private static readonly PropertyInfo criticalHandlePropertyInfo = typeof(Window).GetProperty("CriticalHandle", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly object[] emptyObjectArray = new object[0];

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
                // DragMove works too, but not on maximized windows
                NativeMethods.SendMessage(criticalHandle, WM.SYSCOMMAND, (IntPtr)SC.MOUSEMOVE, IntPtr.Zero);
                NativeMethods.SendMessage(criticalHandle, WM.LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
            }
            else if (e.ClickCount == 2 && this.ResizeMode != ResizeMode.NoResize)
            {
                e.Handled = true;

                if (this.WindowState == WindowState.Normal && this.ResizeMode != ResizeMode.NoResize && this.ResizeMode != ResizeMode.CanMinimize)
                {
                    SystemCommands.MaximizeWindow(this);
                }
                else
                {
                    SystemCommands.RestoreWindow(this);
                }
            }
        }

        private void TitleBarGrid_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ControlzEx.Windows.Shell.SystemCommands.ShowSystemMenu(this, e);
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

#pragma warning restore 618

        private void ButtonOpenChildWindowOnClick(object sender, RoutedEventArgs e)
        {
            var window = new MainWindow
                         {
                             Owner = this
                         };
            window.Show();
        }

        private void ButtonOpenModalChildWindowOnClick(object sender, RoutedEventArgs e)
        {
            var window = new MainWindow
            {
                Owner = this
            };
            window.ShowDialog();
        }

        private void ButtonResetMinSizesOnClick(object sender, RoutedEventArgs e)
        {
            this.MinWidth = 80;
            this.MinHeight = 60;
        }

        private void ButtonResetMaxSizesOnClick(object sender, RoutedEventArgs e)
        {
            this.MaxWidth = double.NaN;
            this.MaxHeight = double.NaN;
        }

        private void ButtonHideOnClick(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}