namespace ControlzEx
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using ControlzEx.Internal;
    using global::Windows.Win32;
    using global::Windows.Win32.Foundation;
    using global::Windows.Win32.UI.WindowsAndMessaging;

    [Obsolete(DesignerConstants.Win32ElementWarning)]
    public static class SystemCommands
    {
        public static RoutedCommand CloseWindowCommand { get; }

        public static RoutedCommand MaximizeWindowCommand { get; }

        public static RoutedCommand MinimizeWindowCommand { get; }

        public static RoutedCommand RestoreWindowCommand { get; }

        public static RoutedCommand ShowSystemMenuCommand { get; }

        static SystemCommands()
        {
            CloseWindowCommand = new RoutedCommand("CloseWindow", typeof(SystemCommands));
            MaximizeWindowCommand = new RoutedCommand("MaximizeWindow", typeof(SystemCommands));
            MinimizeWindowCommand = new RoutedCommand("MinimizeWindow", typeof(SystemCommands));
            RestoreWindowCommand = new RoutedCommand("RestoreWindow", typeof(SystemCommands));
            ShowSystemMenuCommand = new RoutedCommand("ShowSystemMenu", typeof(SystemCommands));
        }

        [SecurityCritical]
        private static void PostSystemCommand(Window window, SC command)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (WindowHelper.IsWindowHandleValid(hwnd) == false)
            {
                return;
            }

            PInvoke.PostMessage(new HWND(hwnd), WM.SYSCOMMAND, (nuint)command, default);
        }

        [SecuritySafeCritical]
        public static void CloseWindow(Window window)
        {
            PostSystemCommand(window, SC.CLOSE);
        }

        [SecuritySafeCritical]
        public static void MaximizeWindow(Window window)
        {
            PostSystemCommand(window, SC.MAXIMIZE);
        }

        [SecuritySafeCritical]
        public static void MinimizeWindow(Window window)
        {
            PostSystemCommand(window, SC.MINIMIZE);
        }

        [SecuritySafeCritical]
        public static void RestoreWindow(Window window)
        {
            PostSystemCommand(window, SC.RESTORE);
        }

        /// <summary>
        /// Shows the system menu at the current mouse position.
        /// </summary>
        /// <param name="window">The window for which the system menu should be shown.</param>
        /// <param name="e">The mouse event args.</param>
        [SecuritySafeCritical]
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
        public static void ShowSystemMenu(Visual visual, Point elementPoint)
        {
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
            var hwndSource = (HwndSource?)PresentationSource.FromVisual(visual);

            if (hwndSource is null)
            {
                return;
            }

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
            ShowSystemMenuPhysicalCoordinates(new HWND(source.Handle), physicalScreenLocation);
        }

        /// <summary>Display the system menu at a specified location.</summary>
        /// <param name="hwnd">The window for which the system menu should be displayed.</param>
        /// <param name="physicalScreenLocation">The location to display the system menu, in physical screen coordinates.</param>
        [SecuritySafeCritical]
        internal static void ShowSystemMenuPhysicalCoordinates(HWND hwnd, Point physicalScreenLocation)
        {
            // SM_MENUDROPALIGNMENT is non-zero if drop-down menus are right-aligned with the corresponding menu-bar item.
            var alignmentFlags = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_MENUDROPALIGNMENT) != 0
                ? TRACK_POPUP_MENU_FLAGS.TPM_RIGHTALIGN
                : TRACK_POPUP_MENU_FLAGS.TPM_LEFTALIGN;

            ShowSystemMenuPhysicalCoordinates(hwnd, physicalScreenLocation, alignmentFlags);
        }

        /// <summary>Display the system menu at a specified location.</summary>
        /// <param name="hwnd">The window for which the system menu should be displayed.</param>
        /// <param name="physicalScreenLocation">The location to display the system menu, in physical screen coordinates.</param>
        /// <param name="alignmentFlags">The flags used to align the menu relative to <paramref name="physicalScreenLocation"/>.</param>
        [SecurityCritical]
        internal static unsafe void ShowSystemMenuPhysicalCoordinates(HWND hwnd, Point physicalScreenLocation, TRACK_POPUP_MENU_FLAGS alignmentFlags)
        {
            if (WindowHelper.IsWindowHandleValid(hwnd) == false)
            {
                return;
            }

            var hmenu = PInvoke.GetSystemMenu(hwnd, false);
            if (hmenu.IsNull)
            {
                // If we couldn't get a menu, we have to enable the system menu style if it's not present
                var dwStyle = PInvoke.GetWindowStyle(hwnd);
                if (dwStyle.HasFlag(WINDOW_STYLE.WS_SYSMENU))
                {
                    return;
                }

                var dwNewStyle = dwStyle | WINDOW_STYLE.WS_SYSMENU;
                // Enable system menu
                PInvoke.SetWindowStyle(hwnd, dwNewStyle);
                hmenu = PInvoke.GetSystemMenu(hwnd, false);
                // Enable system menu if it wasn't present before
                PInvoke.SetWindowStyle(hwnd, dwStyle);

                if (hmenu.IsNull)
                {
                    return;
                }
            }

            var tpmparams = new TPMPARAMS
            {
                cbSize = (uint)Marshal.SizeOf<TPMPARAMS>()
            };
            var cmd = PInvoke.TrackPopupMenuEx(hmenu, (uint)(TRACK_POPUP_MENU_FLAGS.TPM_LEFTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD | alignmentFlags), (int)physicalScreenLocation.X, (int)physicalScreenLocation.Y, hwnd, &tpmparams);
            if (cmd.Value != 0)
            {
                PInvoke.PostMessage(hwnd, WM.SYSCOMMAND, (nuint)cmd.Value, default);
            }
        }
    }
}