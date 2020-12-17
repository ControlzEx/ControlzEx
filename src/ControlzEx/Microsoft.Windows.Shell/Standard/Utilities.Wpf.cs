#pragma warning disable 1591, 618
// This file contains general utilities to aid in development.
// Classes here generally shouldn't be exposed publicly since
// they're not particular to any library functionality.
// Because the classes here are internal, it's likely this file
// might be included in multiple assemblies.
namespace ControlzEx.Standard
{
    using System.Windows;
    using System.Windows.Media;

    internal static partial class Utility
    {
        /// <summary>Convert a native integer that represent a color with an alpha channel into a Color struct.</summary>
        /// <param name="color">The integer that represents the color.  Its bits are of the format 0xAARRGGBB.</param>
        /// <returns>A Color representation of the parameter.</returns>
        public static Color ColorFromArgbDword(uint color)
        {
            return Color.FromArgb(
                (byte)((color & 0xFF000000) >> 24),
                (byte)((color & 0x00FF0000) >> 16),
                (byte)((color & 0x0000FF00) >> 8),
                (byte)((color & 0x000000FF) >> 0));
        }

        #pragma warning disable WPF0024
        public static bool IsNonNegative(this Thickness thickness)
        {
            if (!thickness.Top.IsFiniteAndNonNegative())
            {
                return false;
            }

            if (!thickness.Left.IsFiniteAndNonNegative())
            {
                return false;
            }

            if (!thickness.Bottom.IsFiniteAndNonNegative())
            {
                return false;
            }

            if (!thickness.Right.IsFiniteAndNonNegative())
            {
                return false;
            }

            return true;
        }

        public static bool IsValid(this CornerRadius cornerRadius)
        {
            if (!cornerRadius.TopLeft.IsFiniteAndNonNegative())
            {
                return false;
            }

            if (!cornerRadius.TopRight.IsFiniteAndNonNegative())
            {
                return false;
            }

            if (!cornerRadius.BottomLeft.IsFiniteAndNonNegative())
            {
                return false;
            }

            if (!cornerRadius.BottomRight.IsFiniteAndNonNegative())
            {
                return false;
            }

            return true;
        }
    }
}
