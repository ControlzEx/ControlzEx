#pragma warning disable 1591, 618
namespace ControlzEx.Windows.Shell
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using ControlzEx.Native;
    using ControlzEx.Standard;

    [Obsolete(DesignerConstants.Win32ElementWarning)]
    public static class SystemCommands
    {
        public static RoutedCommand CloseWindowCommand { get; private set; }
        public static RoutedCommand MaximizeWindowCommand { get; private set; }
        public static RoutedCommand MinimizeWindowCommand { get; private set; }
        public static RoutedCommand RestoreWindowCommand { get; private set; }
        public static RoutedCommand ShowSystemMenuCommand { get; private set; }

        static SystemCommands()
        {
            CloseWindowCommand = new RoutedCommand("CloseWindow", typeof(SystemCommands));
            MaximizeWindowCommand = new RoutedCommand("MaximizeWindow", typeof(SystemCommands));
            MinimizeWindowCommand = new RoutedCommand("MinimizeWindow", typeof(SystemCommands));
            RestoreWindowCommand = new RoutedCommand("RestoreWindow", typeof(SystemCommands));
            ShowSystemMenuCommand = new RoutedCommand("ShowSystemMenu", typeof(SystemCommands));                 
        }

        private static void _PostSystemCommand(Window window, SC command)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
            {
                return;
            }

            NativeMethods.PostMessage(hwnd, WM.SYSCOMMAND, new IntPtr((int)command), IntPtr.Zero);
        }

        public static void CloseWindow(Window window)
        {
            Verify.IsNotNull(window, "window");
            _PostSystemCommand(window, SC.CLOSE);
        }

        public static void MaximizeWindow(Window window)
        {
            Verify.IsNotNull(window, "window");
            _PostSystemCommand(window, SC.MAXIMIZE);
        }

        public static void MinimizeWindow(Window window)
        {
            Verify.IsNotNull(window, "window");
            _PostSystemCommand(window, SC.MINIMIZE);
        }

        public static void RestoreWindow(Window window)
        {
            Verify.IsNotNull(window, "window");
            _PostSystemCommand(window, SC.RESTORE);
        }

        /// <summary>
        /// Shows the system menu at the current mouse position.
        /// </summary>
        /// <param name="window">The window for which the system menu should be shown.</param>
        /// <param name="e">The mouse event args.</param>
        public static void ShowSystemMenu(Window window, MouseButtonEventArgs e)
        {
            var mousePosition = e.GetPosition(window);
            var physicalScreenLocation = window.PointToScreen(mousePosition);

            ShowSystemMenu(window, physicalScreenLocation);
        }

        /// <summary>Display the system menu at a specified location.</summary>
        /// <param name="window">The MetroWindow</param>
        /// <param name="screenLocation">The location to display the system menu, in logical screen coordinates.</param>
        public static void ShowSystemMenu(Window window, Point screenLocation)
        {
            Verify.IsNotNull(window, "window");

            // Using fixed dpi scaling here because the menu gets placed way off otherwise
            ShowSystemMenuPhysicalCoordinates(window, DpiHelper.LogicalPixelsToDevice(screenLocation, 1, 1));
        }

        internal static void ShowSystemMenuPhysicalCoordinates(Window window, Point physicalScreenLocation)
        {
            Verify.IsNotNull(window, "window");

            var hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
            {
                return;
            }

            var hmenu = NativeMethods.GetSystemMenu(hwnd, false);

            var cmd = NativeMethods.TrackPopupMenuEx(hmenu, Constants.TPM_LEFTBUTTON | Constants.TPM_RETURNCMD, (int)physicalScreenLocation.X, (int)physicalScreenLocation.Y, hwnd, IntPtr.Zero);
            if (0 != cmd)
            {
                NativeMethods.PostMessage(hwnd, WM.SYSCOMMAND, new IntPtr(cmd), IntPtr.Zero);
            }
        }
    }
}