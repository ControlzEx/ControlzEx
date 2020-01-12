namespace ControlzEx.Theming
{
    using System;
    using System.Windows.Media;
    using ControlzEx.Standard;

    /// <summary>
    /// This struct represent a Color in HSL (Hue, Saturation, Luminance)
    /// 
    /// Idea taken from here http://ciintelligence.blogspot.com/2012/02/converting-excel-theme-color-and-tint.html
    /// </summary>
    public struct HSLColor
    {
        /// <summary>
        /// Creates a new HSL Color
        /// </summary>
        /// <param name="rgbColor">Any System.Windows.Media.Color</param>
        public HSLColor(Color rgbColor)
        {
            // Init Parameters
            this.A = 0;
            this.H = 0;
            this.L = 0;
            this.S = 0;

            var r = rgbColor.R / 255d;
            var g = rgbColor.G / 255d;
            var b = rgbColor.B / 255d;
            var a = rgbColor.A / 255d;

            var min = Math.Min(r, Math.Min(g, b));
            var max = Math.Max(r, Math.Max(g, b));

            var delta = max - min;

            if (DoubleUtilities.AreClose(max, min))
            {
                this.H = 0;
                this.S = 0;
                this.L = max;
            }
            else
            {
                this.L = (min + max) / 2;

                if (this.L < 0.5)
                {
                    this.S = delta / (max + min);
                }
                else
                {
                    this.S = delta / (2.0 - max - min);
                }

                if (DoubleUtilities.AreClose(r, max))
                {
                    this.H = (g - b) / delta;
                }

                if (DoubleUtilities.AreClose(g, max))
                {
                    this.H = 2.0 + (b - r) / delta;
                }

                if (DoubleUtilities.AreClose(b, max))
                {
                    this.H = 4.0 + (r - g) / delta;
                }

                this.H *= 60;

                if (this.H < 0)
                {
                    this.H += 360;
                }

                this.A = a;
            }
        }

        /// <summary>
        /// Creates a new HSL Color
        /// </summary>
        /// <param name="a">Alpha Channel [0;1]</param>
        /// <param name="h">Hue Channel [0;1]</param>
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
            Color rgbColor;

            if (DoubleUtilities.AreClose(this.S, 0))
            {
                rgbColor = Color.FromArgb((byte)(this.A * 255), (byte)(this.L * 255), (byte)(this.L * 255), (byte)(this.L * 255));
            }
            else
            {
                double t1;

                if (this.L < 0.5)
                {
                    t1 = this.L * (1.0 + this.S);
                }

                else
                {
                    t1 = this.L + this.S - this.L * this.S;
                }

                var t2 = 2.0 * this.L - t1;
                var h = this.H / 360d;
                var tR = h + 1.0 / 3.0;
                var r = GetColorComponent(t1, t2, tR);
                var tG = h;
                var g = GetColorComponent(t1, t2, tG);
                var tB = h - 1.0 / 3.0;
                var b = GetColorComponent(t1, t2, tB);
                rgbColor = Color.FromArgb((byte)(this.A * 255), (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
            }

            return rgbColor;
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
                lum *= (1.0 + tint);
            }
            else
            {
                lum = lum * (1.0 - tint) + (255 - 255 * (1.0 - tint));
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

        private static double GetColorComponent(double t1, double t2, double t3)
        {
            if (t3 < 0)
            {
                t3 += 1.0;
            }

            if (t3 > 1)
            {
                t3 -= 1.0;
            }

            double color;

            if (6.0 * t3 < 1)
            {
                color = t2 + (t1 - t2) * 6.0 * t3;
            }
            else if (2.0 * t3 < 1)
            {
                color = t1;
            }
            else if (3.0 * t3 < 2)
            {
                color = t2 + (t1 - t2) * (2.0 / 3.0 - t3) * 6.0;
            }
            else
            {
                color = t2;
            }

            return color;
        }
    }
}