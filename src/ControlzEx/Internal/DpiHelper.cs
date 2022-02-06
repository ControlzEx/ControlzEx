#pragma warning disable 1591, 1573, 618, CA1060
namespace ControlzEx.Internal
{
    using System;
    using System.Windows;
    using System.Windows.Media;

    internal static class DpiHelper
    {
        [ThreadStatic]
        private static Matrix transformToDevice;

        [ThreadStatic]
        private static Matrix transformToDip;

        /// <summary>
        ///     Convert a point in device independent pixels (1/96") to a point in the system coordinates.
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
        ///     Convert a point in system coordinates to a point in device independent pixels (1/96").
        /// </summary>
        /// <param name="devicePoint">A point in the physical coordinate system.</param>
        /// <returns>Returns the parameter converted to the device independent coordinate system.</returns>
        public static Point DevicePixelsToLogical(Point devicePoint, double dpiX, double dpiY)
        {
            transformToDip = Matrix.Identity;
            transformToDip.Scale(1d / dpiX, 1d / dpiY);
            return transformToDip.Transform(devicePoint);
        }

        public static Rect LogicalRectToDevice(Rect logicalRectangle, double dpiX, double dpiY)
        {
            var topLeft = LogicalPixelsToDevice(new Point(logicalRectangle.Left, logicalRectangle.Top), dpiX, dpiY);
            var bottomRight = LogicalPixelsToDevice(new Point(logicalRectangle.Right, logicalRectangle.Bottom), dpiX, dpiY);

            return new Rect(topLeft, bottomRight);
        }

        public static Rect DeviceRectToLogical(Rect deviceRectangle, double dpiX, double dpiY)
        {
            var topLeft = DevicePixelsToLogical(new Point(deviceRectangle.Left, deviceRectangle.Top), dpiX, dpiY);
            var bottomRight = DevicePixelsToLogical(new Point(deviceRectangle.Right, deviceRectangle.Bottom), dpiX, dpiY);

            return new Rect(topLeft, bottomRight);
        }

        public static Size LogicalSizeToDevice(Size logicalSize, double dpiX, double dpiY)
        {
            var pt = LogicalPixelsToDevice(new Point(logicalSize.Width, logicalSize.Height), dpiX, dpiY);

            return new Size { Width = pt.X, Height = pt.Y };
        }

        public static Size DeviceSizeToLogical(Size deviceSize, double dpiX, double dpiY)
        {
            var pt = DevicePixelsToLogical(new Point(deviceSize.Width, deviceSize.Height), dpiX, dpiY);

            return new Size(pt.X, pt.Y);
        }

        public static Thickness LogicalThicknessToDevice(Thickness logicalThickness, DpiScale dpiScale)
        {
            return LogicalThicknessToDevice(logicalThickness, dpiScale.DpiScaleX, dpiScale.DpiScaleY);
        }

        public static Thickness LogicalThicknessToDevice(Thickness logicalThickness, double dpiScaleX, double dpiScaleY)
        {
            var topLeft = LogicalPixelsToDevice(new Point(logicalThickness.Left, logicalThickness.Top), dpiScaleX, dpiScaleY);
            var bottomRight = LogicalPixelsToDevice(new Point(logicalThickness.Right, logicalThickness.Bottom), dpiScaleX, dpiScaleY);

            return new Thickness(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
        }

        public static double TransformToDeviceY(Visual visual, double y, double dpiY)
        {
            var source = PresentationSource.FromVisual(visual);
            if (source?.CompositionTarget is not null)
            {
                return y * source.CompositionTarget.TransformToDevice.M22;
            }

            return TransformToDeviceY(y, dpiY);
        }

        public static double TransformToDeviceX(Visual visual, double x, double dpiX)
        {
            var source = PresentationSource.FromVisual(visual);
            if (source?.CompositionTarget is not null)
            {
                return x * source.CompositionTarget.TransformToDevice.M11;
            }

            return TransformToDeviceX(x, dpiX);
        }

        public static double TransformToDeviceY(double y, double dpiY)
        {
            return y * dpiY / 96;
        }

        public static double TransformToDeviceX(double x, double dpiX)
        {
            return x * dpiX / 96;
        }

        #region Per monitor dpi support

        public static DpiScale GetDpi(this Visual visual)
        {
            return VisualTreeHelper.GetDpi(visual);
        }

        internal static DpiScale GetDpi(this Window window)
        {
            return GetDpi((Visual)window);
        }

        #endregion Per monitor dpi support
    }
}