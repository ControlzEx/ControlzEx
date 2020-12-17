#pragma warning disable 1591, 618
namespace ControlzEx.Standard
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using ControlzEx.Internal;

    internal static partial class Utility
    {
        private static readonly Version osVersion = Environment.OSVersion.Version;

        public static void SafeDestroyIcon(ref IntPtr hicon)
        {
            IntPtr p = hicon;
            hicon = IntPtr.Zero;
            if (p != IntPtr.Zero)
            {
                NativeMethods.DestroyIcon(p);
            }
        }

        /// <summary>GDI's DeleteObject</summary>
        public static void SafeDeleteObject(ref IntPtr gdiObject)
        {
            IntPtr p = gdiObject;
            gdiObject = IntPtr.Zero;
            if (p != IntPtr.Zero)
            {
                NativeMethods.DeleteObject(p);
            }
        }

        public static void SafeDestroyWindow(ref IntPtr hwnd)
        {
            IntPtr p = hwnd;
            hwnd = IntPtr.Zero;
            if (WindowHelper.IsWindowHandleValid(p))
            {
                NativeMethods.DestroyWindow(p);
            }
        }

        /// <summary>GDI+'s DisposeImage</summary>
        public static void SafeDisposeImage(ref IntPtr gdipImage)
        {
            IntPtr p = gdipImage;
            gdipImage = IntPtr.Zero;
            if (p != IntPtr.Zero)
            {
                NativeMethods.GdipDisposeImage(p);
            }
        }

        public static void SafeCoTaskMemFree(ref IntPtr ptr)
        {
            IntPtr p = ptr;
            ptr = IntPtr.Zero;
            if (p != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(p);
            }
        }

        public static void SafeFreeHGlobal(ref IntPtr hglobal)
        {
            IntPtr p = hglobal;
            hglobal = IntPtr.Zero;
            if (p != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(p);
            }
        }

        public static void SafeRelease<T>(ref T? comObject)
            where T : class
        {
            var t = comObject;
            comObject = null;
            if (t is not null)
            {
                Assert.IsTrue(Marshal.IsComObject(t));
                Marshal.ReleaseComObject(t);
            }
        }

        public static bool IsOSVistaOrNewer
        {
            get { return new Version(6, 0) <= osVersion; }
        }

        public static bool IsOSWindows7OrNewer
        {
            get { return new Version(6, 1) <= osVersion; }
        }
    }
}
