namespace ControlzEx.Showcase
{
    using System;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Input;
    using ControlzEx.Native;
    using ControlzEx.Standard;

    public partial class MainWindow
    {
        public MainWindow()
        {
            //this.ShowActivated = false;
            //this.MaxWidth = 1000;
            //this.MaxHeight = 700;
            //this.SizeToContent = SizeToContent.WidthAndHeight;

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

#pragma warning disable 618
                if (this.WindowState == WindowState.Normal)
                {
                    ControlzEx.Windows.Shell.SystemCommands.MaximizeWindow(this);
                }
                else
                {
                    ControlzEx.Windows.Shell.SystemCommands.RestoreWindow(this);
                }
#pragma warning restore 618
            }
        }

        private void TitleBarGrid_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
#pragma warning disable 618
            ControlzEx.Windows.Shell.SystemCommands.ShowSystemMenu(this, e);
#pragma warning restore 618
        }
    }
}