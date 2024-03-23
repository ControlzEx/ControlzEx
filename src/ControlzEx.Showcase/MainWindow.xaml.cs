#pragma warning disable WPF0015

namespace ControlzEx.Showcase
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using ControlzEx.Theming;
    using global::Windows.Win32;
    using global::Windows.Win32.Foundation;
    using global::Windows.Win32.UI.WindowsAndMessaging;

    public partial class MainWindow
    {
        private static readonly PropertyInfo criticalHandlePropertyInfo = typeof(Window).GetProperty("CriticalHandle", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly object[] emptyObjectArray = Array.Empty<object>();

        public static readonly DependencyProperty ColorsProperty = DependencyProperty.Register(nameof(Colors), typeof(List<Color>), typeof(MainWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty BrushesProperty = DependencyProperty.Register(nameof(Brushes), typeof(List<Brush>), typeof(MainWindow), new PropertyMetadata(null));

        public MainWindow()
        {
            this.InitializeComponent();

            this.Colors = GetColors().ToList();
            this.Brushes = GetBrushes().ToList();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            WindowBackgroundManager.UpdateWindowEffect(this);
        }

        public List<Color> Colors
        {
            get => (List<Color>)this.GetValue(ColorsProperty);
            set => this.SetValue(ColorsProperty, value);
        }

        public List<Brush> Brushes
        {
            get => (List<Brush>)this.GetValue(BrushesProperty);
            set => this.SetValue(BrushesProperty, value);
        }

        public int LoadedCount { get; set; }

        public static IEnumerable<Color> GetColors()
        {
            return typeof(Colors)
                   .GetProperties()
                   .Where(prop => typeof(Color).IsAssignableFrom(prop.PropertyType))
                   .Select(prop => (Color)prop.GetValue(null, null));
        }

        public static IEnumerable<Brush> GetBrushes()
        {
            var brushes = typeof(Brushes)
                          .GetProperties()
                          .Where(prop => typeof(Brush).IsAssignableFrom(prop.PropertyType))
                          .Select(prop => (Brush)prop.GetValue(null, null));

            return new Brush[] { null }.Concat(brushes);
        }

        private MainWindow CreateNewWindow()
        {
            var window = new MainWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                AllowsTransparency = this.chkAllowsTransparency.IsChecked == true,
                WindowStyle = this.chkAllowsTransparency.IsChecked == true ? WindowStyle.None : this.WindowStyle,
                Background = this.chkAllowsTransparency.IsChecked == true ? System.Windows.Media.Brushes.Transparent : this.Background,
                SetOwner =
                {
                    IsChecked = this.SetOwner.IsChecked
                },
                IsGlowTransitionEnabled = this.IsGlowTransitionEnabled,
                PreferDWMBorderColor = this.PreferDWMBorderColor,
                CornerPreference = this.CornerPreference,
                UseRadialGradientForCorners = this.UseRadialGradientForCorners,
                GlowDepth = this.GlowDepth,
                GlowColor = this.GlowColor,
                NonActiveGlowColor = this.NonActiveGlowColor,
                NCNonActiveBrush = this.NCNonActiveBrush
            };

            return window;
        }

        private void ButtonOpenChildWindowOnClick(object sender, RoutedEventArgs e)
        {
            var window = this.CreateNewWindow();

            if (this.SetOwner.IsChecked == true)
            {
                window.Owner = this;
            }

            window.Show();
        }

        private void ButtonOpenModalChildWindowOnClick(object sender, RoutedEventArgs e)
        {
            var window = this.CreateNewWindow();

            if (this.SetOwner.IsChecked == true)
            {
                window.Owner = this;
            }

            window.ShowDialog();
        }

        private void ButtonOpenPseudoModalChildWindowOnClick(object sender, RoutedEventArgs e)
        {
            var window = this.CreateNewWindow();
            window.Owner = this; // for this to work we always have to set the owner

            // We have to use closing, otherwise the owner window won't be activated.
            window.Closing += this.PseudoModalWindow_Closing;

            var ownerHandle = new WindowInteropHelper(window.Owner).Handle;
#pragma warning disable 618
            var windowStyle = PInvoke.GetWindowStyle(new HWND(ownerHandle));
            PInvoke.SetWindowStyle(new HWND(ownerHandle), windowStyle | WINDOW_STYLE.WS_DISABLED);
#pragma warning restore 618

            window.Show();
        }

        private void ButtonSleepOnClick(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        private void PseudoModalWindow_Closing(object sender, CancelEventArgs e)
        {
            if (e.Cancel)
            {
                return;
            }

            var ownerHandle = new WindowInteropHelper(((Window)sender).Owner).Handle;
#pragma warning disable 618
            var windowStyle = PInvoke.GetWindowStyle(new HWND(ownerHandle));
            PInvoke.SetWindowStyle(new HWND(ownerHandle), windowStyle & ~WINDOW_STYLE.WS_DISABLED);
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
            // if (e.ClickCount == 1)
            // {
            //     e.Handled = true;
            //
            //     // taken from DragMove internal code
            //     this.VerifyAccess();
            //
            //     // for the touch usage
            //     UnsafeNativeMethods.ReleaseCapture();
            //
            //     var criticalHandle = (IntPtr)criticalHandlePropertyInfo.GetValue(this, emptyObjectArray);
            //
            //     // these lines are from DragMove
            //     // NativeMethods.SendMessage(criticalHandle, WM.SYSCOMMAND, (IntPtr)SC.MOUSEMOVE, IntPtr.Zero);
            //     // NativeMethods.SendMessage(criticalHandle, WM.LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
            //
            //     var wpfPoint = this.PointToScreen(Mouse.GetPosition(this));
            //     var x = (int)wpfPoint.X;
            //     var y = (int)wpfPoint.Y;
            //     NativeMethods.SendMessage(criticalHandle, WM.NCLBUTTONDOWN, (IntPtr)HT.CAPTION, new IntPtr(x | (y << 16)));
            // }
            // else if (e.ClickCount == 2
            //          && this.ResizeMode != ResizeMode.NoResize)
            // {
            //     e.Handled = true;
            //
            //     if (this.WindowState == WindowState.Normal
            //         && this.ResizeMode != ResizeMode.NoResize
            //         && this.ResizeMode != ResizeMode.CanMinimize)
            //     {
            //         SystemCommands.MaximizeWindow(this);
            //     }
            //     else
            //     {
            //         SystemCommands.RestoreWindow(this);
            //     }
            // }
        }

        private void TitleBarGrid_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Windows.Shell.SystemCommands.ShowSystemMenu(this, e);
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

        private Storyboard glowDepthStoryboard;

        private void AnimateGlowDepth_OnChecked(object sender, RoutedEventArgs e)
        {
            if (this.glowDepthStoryboard is null)
            {
                var glowDepthAnimation = new Int32Animation(1, 60, TimeSpan.FromSeconds(2.5));

                this.glowDepthStoryboard = new Storyboard();
                this.glowDepthStoryboard.Children.Add(glowDepthAnimation);
                this.glowDepthStoryboard.RepeatBehavior = RepeatBehavior.Forever;
                this.glowDepthStoryboard.AutoReverse = true;

                Storyboard.SetTargetProperty(this.glowDepthStoryboard, new PropertyPath(nameof(this.GlowDepth)));
            }

            this.glowDepthStoryboard.Begin(this, true);
        }

        private void AnimateGlowDepth_OnUnchecked(object sender, RoutedEventArgs e)
        {
            this.glowDepthStoryboard.Stop(this);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.Handled)
            {
                return;
            }

            if (e.Key == Key.F3)
            {
                this.ButtonOpenChildWindowOnClick(this, null);
            }
        }

        private void HandleClearGlowColorClick(object sender, RoutedEventArgs e)
        {
            this.glowColorComboBox.Text = null;
        }

        private void HandleClearNonActiveGlowColorClick(object sender, RoutedEventArgs e)
        {
            this.nonActiveGlowColorComboBox.Text = null;
        }
    }
}