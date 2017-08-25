namespace ControlzEx.Showcase
{
    using System;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Input;
    using ControlzEx.Native;
    using Standard;
#pragma warning disable 618
    using SystemCommands = Microsoft.Windows.Shell.SystemCommands;
#pragma warning restore 618

    public partial class MainWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private static readonly PropertyInfo criticalHandlePropertyInfo = typeof(Window).GetProperty("CriticalHandle", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly object[] emptyObjectArray = new object[0];

        private void TitleBarGrid_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                e.Handled = true;

                // taken from DragMove internal code
                this.VerifyAccess();

                // for the touch usage
#pragma warning disable 618
                UnsafeNativeMethods.ReleaseCapture();

                var criticalHandle = (IntPtr)criticalHandlePropertyInfo.GetValue(this, emptyObjectArray);
                // DragMove works too, but not on maximized windows
                NativeMethods.SendMessage(criticalHandle, WM.SYSCOMMAND, (IntPtr)SC.MOUSEMOVE, IntPtr.Zero);
                NativeMethods.SendMessage(criticalHandle, WM.LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
#pragma warning restore 618
            }
            else if (e.ClickCount == 2
                     && this.ResizeMode != ResizeMode.NoResize)
            {
                e.Handled = true;
                this.WindowState = this.WindowState == WindowState.Maximized
                                         ? WindowState.Normal
                                         : WindowState.Maximized;
            }
        }

        private void TitleBarGrid_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
#pragma warning disable 618
            SystemCommands.ShowSystemMenu(this, e);
#pragma warning restore 618
        }
    }
}