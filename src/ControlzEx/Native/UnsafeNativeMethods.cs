#pragma warning disable SA1604 // Element documentation should have summary
namespace ControlzEx.Native
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using ControlzEx.Standard;

    /// <devdoc>http://msdn.microsoft.com/en-us/library/ms182161.aspx</devdoc>
    [SuppressUnmanagedCodeSecurity]
    [Obsolete(DesignerConstants.Win32ElementWarning)]
    public static class UnsafeNativeMethods
    {
        /// <devdoc>http://msdn.microsoft.com/en-us/library/dd145064%28v=VS.85%29.aspx</devdoc>
        [DllImport("user32")]
        internal static extern IntPtr MonitorFromWindow([In] IntPtr handle, [In] MonitorOptions flags);

        /// <devdoc>http://msdn.microsoft.com/en-us/library/windows/desktop/ms647486%28v=vs.85%29.aspx</devdoc>
        [DllImport("user32", CharSet = CharSet.Unicode, ExactSpelling = true, EntryPoint = "LoadStringW", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [CLSCompliant(false)]
        public static extern int LoadString([In][Optional] SafeLibraryHandle hInstance, [In] uint uID, [Out] StringBuilder lpBuffer, [In] int nBufferMax);

        /// <devdoc>http://msdn.microsoft.com/en-us/library/windows/desktop/ms633528(v=vs.85).aspx</devdoc>
        [DllImport("user32", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern bool IsWindow([In][Optional] IntPtr hWnd);

        /// <devdoc>http://msdn.microsoft.com/en-us/library/windows/desktop/ms684175%28v=vs.85%29.aspx</devdoc>
        [DllImport("kernel32", CharSet = CharSet.Unicode, ExactSpelling = true, EntryPoint = "LoadLibraryW", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern SafeLibraryHandle LoadLibrary([In][MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        /// <devdoc>http://msdn.microsoft.com/en-us/library/windows/desktop/ms683152%28v=vs.85%29.aspx</devdoc>
        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary([In] IntPtr hModule);

        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetKeyNameText(int lParam, [MarshalAs(UnmanagedType.LPWStr), Out] StringBuilder str, int size);
    }
}