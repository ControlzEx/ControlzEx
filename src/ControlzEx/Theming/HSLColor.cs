#nullable enable
namespace ControlzEx.Theming
{
    using System;
    using System.Windows.Media;
    using ControlzEx.Standard;

    /// <summary>
    /// This struct represent a Color in HSL (Hue, Saturation, Luminance)
    /// 
    /// Idea taken from here http://ciintelligence.blogspot.com/2012/02/converting-excel-theme-color-and-tint.html
    /// and here: https://en.wikipedia.org/wiki/HSL_and_HSV
    /// </summary>
    public struct HSLColor
    {
        /// <summary>
        /// Creates a new HSL Color
        /// </summary>
        /// <param name="color">Any System.Windows.Media.Color</param>
        public HSLColor(Color color)
        {
            // Init Parameters
            this.A = 0;
            this.H = 0;
            this.L = 0;
            this.S = 0;

            var r = color.R;
            var g = color.G;
            var b = color.B;
            var a = color.A;

            var min = Math.Min(r, Math.Min(g, b));
            var max = Math.Max(r, Math.Max(g, b));

            var delta = max - min;

            // Calculate H
            if (delta == 0)
            {
                this.H = 0;
            }
            else if (r == max)
            {
                this.H = 60 * (((double)(g - b) / delta) % 6);
            }
            else if (g == max)
            {
                this.H = 60 * (((double)(b - r) / delta) + 2);
            }
            else if (b == max)
            {
                this.H = 60 * (((double)(r - g) / delta) + 4);
            }

            if (this.H < 0)
            {
                this.H += 360;
            }

            // Calculate L 
            this.L = (1d / 2d * (max + min)) / 255d;

            // Calculate S
            if (DoubleUtilities.AreClose(this.L, 0) || DoubleUtilities.AreClose(this.L, 1))
            {
                this.S = 0;
            }
            else
            {
                this.S = delta / (255d * (1 - Math.Abs((2 * this.L) - 1)));
            }

            // Calculate Alpha
            this.A = a / 255d;
        }

        /// <summary>
        /// Creates a new HSL Color
        /// </summary>
        /// <param name="a">Alpha Channel [0;1]</param>
        /// <param name="h">Hue Channel [0;360]</param>
        /// <param name="s">Saturation Channel [0;1]</param>
        /// <param name="l">Luminance Channel [0;1]</param>
        public HSLColor(double a, double h, double s, double l)
        {
            this.A = a;
            this.H = h;
            this.S = s;
            this.L = l;
        }

        /// <summary>
        /// Gets or sets the Alpha channel.
        /// </summary>
        public double A { get; set; }

        /// <summary>
        /// Gets or sets the Hue channel.
        /// </summary>
        public double H { get; set; }

        /// <summary>
        /// Gets or sets the Saturation channel.
        /// </summary>
        public double S { get; set; }

        /// <summary>
        /// Gets or sets the Luminance channel.
        /// </summary>
        public double L { get; set; }

        /// <summary>
        /// Gets the ARGB-Color for this HSL-Color
        /// </summary>
        /// <returns>System.Windows.Media.Color</returns>
        public Color ToColor()
        {
            var r = this.GetColorComponent(0);
            var g = this.GetColorComponent(8);
            var b = this.GetColorComponent(4);
            return Color.FromArgb((byte)Math.Round(this.A * 255), r, g, b);
        }

        /// <summary>
        /// Gets a lighter / darker color based on a tint value. If <paramref name="tint"/> is > 0 then the returned color is darker, otherwise it will be lighter.
        /// </summary>
        /// <param name="tint">Tint Value in the Range [-1;1].</param>
        /// <returns>a new <see cref="Color"/> which is lighter or darker.</returns>
        public Color GetTintedColor(double tint)
        {
            var lum = this.L * 255;

            if (tint < 0)
            {
                lum *= 1.0 + tint;
            }
            else
            {
                lum = (lum * (1.0 - tint)) + (255 - (255 * (1.0 - tint)));
            }

            return new HSLColor(this.A, this.H, this.S, lum / 255d)
                .ToColor();
        }

        /// <summary>
        /// Gets a lighter / darker color based on a tint value. If <paramref name="tint"/> is > 0 then the returned color is darker, otherwise it will be lighter.
        /// </summary>
        /// <param name="color">The input color which should be tinted.</param>
        /// <param name="tint">Tint Value in the Range [-1;1].</param>
        /// <returns>a new <see cref="Color"/> which is lighter or darker.</returns>
        public static Color GetTintedColor(Color color, double tint)
        {
            return new HSLColor(color)
                .GetTintedColor(tint);
        }

        private byte GetColorComponent(int n)
        {
            double a = this.S * Math.Min(this.L, 1 - this.L);
            double k = (n + (this.H / 30)) % 12;

            return (byte)Math.Round(255 * (this.L - (a * Math.Max(-1, Math.Min(k - 3, Math.Min(9 - k, 1))))));
        }

        public override bool Equals(object? obj)
        {
            return obj is HSLColor color
                   && DoubleUtilities.AreClose(this.A, color.A)
                   && DoubleUtilities.AreClose(this.H, color.H)
                   && DoubleUtilities.AreClose(this.S, color.S)
                   && DoubleUtilities.AreClose(this.L, color.L);
        }

        public override int GetHashCode()
        {
            int hashCode = -1795249040;
            hashCode = (hashCode * -1521134295) + this.A.GetHashCode();
            hashCode = (hashCode * -1521134295) + this.H.GetHashCode();
            hashCode = (hashCode * -1521134295) + this.S.GetHashCode();
            hashCode = (hashCode * -1521134295) + this.L.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(HSLColor x, HSLColor y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(HSLColor x, HSLColor y)
        {
            return !(x == y);
        }
    }
}