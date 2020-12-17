#pragma warning disable 1591, 1573, 618
namespace ControlzEx.Standard
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Media;

    internal static class DpiHelper
    {
        [ThreadStatic]
        private static Matrix transformToDevice;
        [ThreadStatic]
        private static Matrix transformToDip;

        /// <summary>
        /// Convert a point in device independent pixels (1/96") to a point in the system coordinates.
        /// </summary>
        /// <param name="logicalPoint">A point in the logical coordinate system.</param>
        /// <returns>Returns the parameter converted to the system's coordinates.</returns>
        public static Point LogicalPixelsToDevice(Point logicalPoint, double dpiScaleX, double dpiScaleY)
        {
            transformToDevice = Matrix.Identity;
            transformToDevice.Scale(dpiScaleX, dpiScaleY);
            return transformToDevice.Transform(logicalPoint);
        }

        /// <summary>
        /// Convert a point in system coordinates to a point in device independent pixels (1/96").
        /// </summary>
        /// <param name="devicePoint">A point in the physical coordinate system.</param>
        /// <returns>Returns the parameter converted to the device independent coordinate system.</returns>
        public static Point DevicePixelsToLogical(Point devicePoint, double dpiScaleX, double dpiScaleY)
        {
            transformToDip = Matrix.Identity;
            transformToDip.Scale(1d / dpiScaleX, 1d / dpiScaleY);
            return transformToDip.Transform(devicePoint);
        }

        public static Rect LogicalRectToDevice(Rect logicalRectangle, double dpiScaleX, double dpiScaleY)
        {
            Point topLeft = LogicalPixelsToDevice(new Point(logicalRectangle.Left, logicalRectangle.Top), dpiScaleX, dpiScaleY);
            Point bottomRight = LogicalPixelsToDevice(new Point(logicalRectangle.Right, logicalRectangle.Bottom), dpiScaleX, dpiScaleY);

            return new Rect(topLeft, bottomRight);
        }

        public static Rect DeviceRectToLogical(Rect deviceRectangle, double dpiScaleX, double dpiScaleY)
        {
            Point topLeft = DevicePixelsToLogical(new Point(deviceRectangle.Left, deviceRectangle.Top), dpiScaleX, dpiScaleY);
            Point bottomRight = DevicePixelsToLogical(new Point(deviceRectangle.Right, deviceRectangle.Bottom), dpiScaleX, dpiScaleY);

            return new Rect(topLeft, bottomRight);
        }

        public static Size LogicalSizeToDevice(Size logicalSize, double dpiScaleX, double dpiScaleY)
        {
            Point pt = LogicalPixelsToDevice(new Point(logicalSize.Width, logicalSize.Height), dpiScaleX, dpiScaleY);

            return new Size { Width = pt.X, Height = pt.Y };
        }

        public static Size DeviceSizeToLogical(Size deviceSize, double dpiScaleX, double dpiScaleY)
        {
            Point pt = DevicePixelsToLogical(new Point(deviceSize.Width, deviceSize.Height), dpiScaleX, dpiScaleY);

            return new Size(pt.X, pt.Y);
        }

        public static Thickness LogicalThicknessToDevice(Thickness logicalThickness, double dpiScaleX, double dpiScaleY)
        {
            Point topLeft = LogicalPixelsToDevice(new Point(logicalThickness.Left, logicalThickness.Top), dpiScaleX, dpiScaleY);
            Point bottomRight = LogicalPixelsToDevice(new Point(logicalThickness.Right, logicalThickness.Bottom), dpiScaleX, dpiScaleY);

            return new Thickness(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
        }

        public static double TransformToDeviceY(Visual visual, double y, double dpiScaleY)
        {
            var source = PresentationSource.FromVisual(visual);
            if (source?.CompositionTarget is not null)
            {
                return y * source.CompositionTarget.TransformToDevice.M22;
            }

            return TransformToDeviceY(y, dpiScaleY);
        }

        public static double TransformToDeviceX(Visual visual, double x, double dpiScaleX)
        {
            var source = PresentationSource.FromVisual(visual);
            if (source?.CompositionTarget is not null)
            {
                return x * source.CompositionTarget.TransformToDevice.M11;
            }

            return TransformToDeviceX(x, dpiScaleX);
        }

        public static double TransformToDeviceY(double y, double dpiScaleY)
        {
            return y * dpiScaleY / 96;
        }

        public static double TransformToDeviceX(double x, double dpiScaleX)
        {
            return x * dpiScaleX / 96;
        }

        #region Per monitor dpi support

        public static DpiScale GetDpi(this Visual visual)
        {
#if OWNDPISCALE
            return new DpiScale(1, 1);
#else
            return VisualTreeHelper.GetDpi(visual);
#endif
        }

        internal static DpiScale GetDpi(this Window window)
        {
            return GetDpi((Visual)window);
        }

        #endregion Per monitor dpi support
    }

#if OWNDPISCALE
    /// <summary>Stores DPI information from which a <see cref="T:System.Windows.Media.Visual" /> or <see cref="T:System.Windows.UIElement" /> is rendered.</summary>
    public struct DpiScale
    {
        private readonly double dpiScaleX;
        private readonly double dpiScaleY;

        /// <summary>Gets the DPI scale on the X axis.</summary>
        /// <returns>The DPI scale for the X axis.</returns>
        public double DpiScaleX
        {
            get
            {
                return this.dpiScaleX;
            }
        }

        /// <summary>Gets the DPI scale on the Yaxis.</summary>
        /// <returns>The DPI scale for the Y axis.</returns>
        public double DpiScaleY
        {
            get
            {
                return this.dpiScaleY;
            }
        }

        /// <summary>Get or sets the PixelsPerDip at which the text should be rendered.</summary>
        /// <returns>The current <see cref="P:System.Windows.DpiScale.PixelsPerDip" /> value.</returns>
        public double PixelsPerDip
        {
            get
            {
                return this.dpiScaleY;
            }
        }

        /// <summary>Gets the DPI along X axis.</summary>
        /// <returns>The DPI along the X axis.</returns>
        public double PixelsPerInchX
        {
            get
            {
                return 96.0 * this.dpiScaleX;
            }
        }

        /// <summary>Gets the DPI along Y axis.</summary>
        /// <returns>The DPI along the Y axis.</returns>
        public double PixelsPerInchY
        {
            get
            {
                return 96.0 * this.dpiScaleY;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Windows.DpiScale" /> structure.</summary>
        /// <param name="dpiScaleX">The DPI scale on the X axis.</param>
        /// <param name="dpiScaleY">The DPI scale on the Y axis. </param>
        public DpiScale(double dpiScaleX, double dpiScaleY)
        {
            this.dpiScaleX = dpiScaleX;
            this.dpiScaleY = dpiScaleY;
        }
    }
#endif
}