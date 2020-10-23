﻿#pragma warning disable 1591, 618
namespace ControlzEx.Windows.Shell
{
    using System;
    using System.Security;
    using System.Security.Permissions;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using ControlzEx.Internal;
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

        [SecurityCritical]
        private static void _PostSystemCommand(Window window, SC command)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (WindowHelper.IsWindowHandleValid(hwnd) == false)
            {
                return;
            }

            NativeMethods.PostMessage(hwnd, WM.SYSCOMMAND, new IntPtr((int)command), IntPtr.Zero);
        }

        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void CloseWindow(Window window)
        {
            Verify.IsNotNull(window, "window");
            _PostSystemCommand(window, SC.CLOSE);
        }

        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void MaximizeWindow(Window window)
        {
            Verify.IsNotNull(window, "window");
            _PostSystemCommand(window, SC.MAXIMIZE);
        }

        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void MinimizeWindow(Window window)
        {
            Verify.IsNotNull(window, "window");
            _PostSystemCommand(window, SC.MINIMIZE);
        }

        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
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
        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void ShowSystemMenu(Window window, MouseButtonEventArgs e)
        {
            var mousePosition = e.GetPosition(window);
            var screenLocation = window.PointToScreen(mousePosition);
            e.Handled = true;

            // Location contains already DPI aware coordinates, so we don't need to do this twice
            ShowSystemMenuPhysicalCoordinates(window, screenLocation);
        }

        /// <summary>Display the system menu at a specified location.</summary>
        /// <param name="visual">The visual for which the system menu should be displayed.</param>
        /// <param name="elementPoint">The location to display the system menu, in logical screen coordinates.</param>
        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void ShowSystemMenu(Visual visual, Point elementPoint)
        {
            Verify.IsNotNull(visual, "visual");

            var screenLocation = visual.PointToScreen(elementPoint);

            ShowSystemMenuPhysicalCoordinates(visual, screenLocation);
        }

        /// <summary>Display the system menu at a specified location.</summary>
        /// <param name="visual">The visual for which the system menu should be displayed.</param>
        /// <param name="physicalScreenLocation">The location to display the system menu, in physical screen coordinates.</param>
        /// <remarks>
        /// The dpi of <paramref name="visual"/> is NOT used to calculate the final coordinates.
        /// So you have to pass the final coordinates.
        /// </remarks>
        [SecuritySafeCritical]
        public static void ShowSystemMenuPhysicalCoordinates(Visual visual, Point physicalScreenLocation)
        {
            var hwndSource = (HwndSource)PresentationSource.FromVisual(visual);

            ShowSystemMenuPhysicalCoordinates(hwndSource, physicalScreenLocation);
        }

        /// <summary>Display the system menu at a specified location.</summary>
        /// <param name="source">The source/hwnd for which the system menu should be displayed.</param>
        /// <param name="physicalScreenLocation">The location to display the system menu, in physical screen coordinates.</param>
        /// <remarks>
        /// The dpi of <paramref name="source"/> is NOT used to calculate the final coordinates.
        /// So you have to pass the final coordinates.
        /// </remarks>
        [SecuritySafeCritical]
        public static void ShowSystemMenuPhysicalCoordinates(HwndSource source, Point physicalScreenLocation)
        {
            Verify.IsNotNull(source, "source");

            var hwnd = source.Handle;

            if (WindowHelper.IsWindowHandleValid(hwnd) == false)
            {
                return;
            }

            var hmenu = NativeMethods.GetSystemMenu(hwnd, false);
            var flags = NativeMethods.GetSystemMetrics(SM.MENUDROPALIGNMENT);
            var cmd = NativeMethods.TrackPopupMenuEx(hmenu, Constants.TPM_LEFTBUTTON | Constants.TPM_RETURNCMD | (uint)flags, (int)physicalScreenLocation.X, (int)physicalScreenLocation.Y, hwnd, IntPtr.Zero);
            if (0 != cmd)
            {
                NativeMethods.PostMessage(hwnd, WM.SYSCOMMAND, new IntPtr(cmd), IntPtr.Zero);
            }
        }
    }
}