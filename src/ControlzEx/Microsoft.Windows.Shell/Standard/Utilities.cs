#pragma warning disable 1591, 618
// This file contains general utilities to aid in development.
// Classes here generally shouldn't be exposed publicly since
// they're not particular to any library functionality.
// Because the classes here are internal, it's likely this file
// might be included in multiple assemblies.
namespace ControlzEx.Standard
{
    using System;

    internal static partial class Utility
    {
        // See: http://stackoverflow.com/questions/7913325/win-api-in-c-get-hi-and-low-word-from-intptr/7913393#7913393
        public static System.Windows.Point GetPoint(IntPtr ptr)
        {
            var xy = unchecked(Environment.Is64BitProcess ? (uint)ptr.ToInt64() : (uint)ptr.ToInt32());
            var x = unchecked((short)xy);
            var y = unchecked((short)(xy >> 16));
            return new System.Windows.Point(x, y);
        }

        public static int GET_X_LPARAM(IntPtr lParam)
        {
            return LOWORD(lParam.ToInt32());
        }

        public static int GET_Y_LPARAM(IntPtr lParam)
        {
            return HIWORD(lParam.ToInt32());
        }

        public static int HIWORD(int i)
        {
            return (short)(i >> 16);
        }

        public static int LOWORD(int i)
        {
            return (short)(i & 0xFFFF);
        }

        public static bool IsFlagSet(int value, int mask)
        {
            return (value & mask) != 0;
        }

        public static void SafeDispose<T>(ref T? disposable)
            where T : class, IDisposable
        {
            // Dispose can safely be called on an object multiple times.
            var t = disposable;
            disposable = null;
            t?.Dispose();
        }
    }
}
