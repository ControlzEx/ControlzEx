namespace ControlzEx.Internal
{
    using System;
    using global::Windows.Win32;
    using global::Windows.Win32.Foundation;

    internal static class WindowHelper
    {
        public static bool IsWindowHandleValid(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero
#pragma warning disable 618
                   && PInvoke.IsWindow(new HWND(windowHandle));
#pragma warning restore 618
        }
    }
}