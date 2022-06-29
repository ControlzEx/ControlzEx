#pragma warning disable CS0618
// ReSharper disable once CheckNamespace
namespace Windows.Win32
{
    using System.Runtime.CompilerServices;
    using System.Windows;
    using Windows.Win32.Foundation;
    using Windows.Win32.UI.WindowsAndMessaging;

    internal static class PInvokeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(this RECT rect)
        {
            return rect.left >= rect.right || rect.top >= rect.bottom;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWidth(this RECT rect)
        {
            return rect.right - rect.left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHeight(this RECT rect)
        {
            return rect.bottom - rect.top;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point GetPosition(this RECT rect)
        {
            return new Point(rect.left, rect.top);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Size GetSize(this RECT rect)
        {
            return new Size(rect.GetWidth(), rect.GetHeight());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RECT ToRECT(this Rect rect)
        {
            return new()
            {
                left = (int)rect.Left,
                top = (int)rect.Top,
                right = (int)rect.Right,
                bottom = (int)rect.Bottom
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RECT ToRECT(this WINDOWPOS windowpos)
        {
            return new()
            {
                left = windowpos.x,
                top = windowpos.y,
                right = windowpos.x + windowpos.cx,
                bottom = windowpos.y + windowpos.cy
            };
        }
    }
}
